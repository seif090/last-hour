using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.Infrastructure.DependencyInjection;
using LastHour.BuildingBlocks.Infrastructure.Transactions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Transactions;

public class TransactionBehaviorTests
{
    [Fact]
    public async Task Handle_CommandSuccess_CommitsPendingChanges()
    {
        var unitOfWork = new TrackingUnitOfWork();
        var behavior = new TransactionBehavior<TestCommand, Result>(unitOfWork);

        Result response = await behavior.Handle(
            new TestCommand("value"),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_CommandFailure_DoesNotCommit()
    {
        var unitOfWork = new TrackingUnitOfWork();
        var behavior = new TransactionBehavior<TestCommand, Result>(unitOfWork);

        Result response = await behavior.Handle(
            new TestCommand("value"),
            _ => Task.FromResult(Result.Failure(Error.Validation("Failed", "boom"))),
            CancellationToken.None);

        Assert.True(response.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ThrowingCommand_DoesNotCommitAndRethrows()
    {
        var unitOfWork = new TrackingUnitOfWork();
        var behavior = new TransactionBehavior<TestCommand, Result>(unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => behavior.Handle(
                new TestCommand("value"),
                _ => throw new InvalidOperationException("boom"),
                CancellationToken.None));

        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_Query_PassesThroughWithoutCommitting()
    {
        var unitOfWork = new TrackingUnitOfWork();
        var behavior = new TransactionBehavior<TestQuery, Result<int>>(unitOfWork);

        Result<int> response = await behavior.Handle(
            new TestQuery(42),
            _ => Task.FromResult(Result<int>.Success(42)),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(42, response.Value);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_CommandCommit_NpgsqlUniqueViolation_ReturnsConflictResult()
    {
        var unitOfWork = new ThrowingUnitOfWork(new DbUpdateException(
            "duplicate key",
            new PostgresException(
                "duplicate key value violates unique constraint \"IX_users_normalized_email\"",
                "ERROR",
                "ERROR",
                "23505")));
        var behavior = new TransactionBehavior<TestCommand, Result>(unitOfWork);

        Result response = await behavior.Handle(
            new TestCommand("value"),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(response.IsFailure);
        Assert.Equal(ErrorType.Conflict, response.ErrorType);
        Assert.Equal("ResourceConflict", response.FirstError!.Code);
    }

    [Fact]
    public async Task Handle_CommandCommit_SqliteUniqueViolation_ReturnsConflictResult()
    {
        // The SQLite provider is used only by the test suite; its stable error message is the
        // classification signal (typed Npgsql detection cannot apply here).
        var unitOfWork = new ThrowingUnitOfWork(new DbUpdateException(
            "duplicate key",
            new SqliteException("SQLite Error 19: 'UNIQUE constraint failed: users.NormalizedEmail'.", 19)));
        var behavior = new TransactionBehavior<TestCommand, Result>(unitOfWork);

        Result response = await behavior.Handle(
            new TestCommand("value"),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(response.IsFailure);
        Assert.Equal(ErrorType.Conflict, response.ErrorType);
    }

    [Fact]
    public async Task Handle_CommandCommit_ConcurrencyConflict_ReturnsConflictResult()
    {
        // A losing optimistic-concurrency write (another request committed the same aggregate
        // first) must fail safely as a conflict, not surface as an internal server error.
        var unitOfWork = new ThrowingUnitOfWork(new DbUpdateConcurrencyException(
            "Database operation expected to affect 1 row(s) but actually affected 0 row(s)."));
        var behavior = new TransactionBehavior<TestCommand, Result>(unitOfWork);

        Result response = await behavior.Handle(
            new TestCommand("value"),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        Assert.True(response.IsFailure);
        Assert.Equal(ErrorType.Conflict, response.ErrorType);
        Assert.Equal("ConcurrencyConflict", response.FirstError!.Code);
    }

    [Fact]
    public async Task Handle_CommandCommit_NonUniqueDatabaseError_Rethrows()
    {
        var unitOfWork = new ThrowingUnitOfWork(new DbUpdateException(
            "constraint failed",
            new PostgresException("null value in column", "ERROR", "ERROR", "23502")));
        var behavior = new TransactionBehavior<TestCommand, Result>(unitOfWork);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => behavior.Handle(
                new TestCommand("value"),
                _ => Task.FromResult(Result.Success()),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ValueReturningCommandSuccess_CommitsPendingChanges()
    {
        var unitOfWork = new TrackingUnitOfWork();
        var behavior = new TransactionBehavior<TestValueCommand, Result<int>>(unitOfWork);

        Result<int> response = await behavior.Handle(
            new TestValueCommand(),
            _ => Task.FromResult(Result<int>.Success(42)),
            CancellationToken.None);

        Assert.True(response.IsSuccess);
        Assert.Equal(42, response.Value);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public void AddCqrs_RegistersTransactionBehavior()
    {
        var services = new ServiceCollection();

        services.AddCqrs();

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>)
                          && descriptor.ImplementationType == typeof(TransactionBehavior<,>));
    }

    [Fact]
    public async Task Send_ThroughPipeline_CommitsSuccessfulCommand()
    {
        var unitOfWork = new TrackingUnitOfWork();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCqrs(null, typeof(TestCommandHandler).Assembly);
        services.AddPerformanceBehaviorOptions(options => options.SlowRequestThreshold = TimeSpan.FromMinutes(1));
        services.AddScoped<IUnitOfWork>(_ => unitOfWork);
        using ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        Result result = await mediator.Send(new TestCommand("value"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Send_ThroughPipeline_DoesNotCommitFailedCommand()
    {
        var unitOfWork = new TrackingUnitOfWork();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCqrs(null, typeof(FailingCommandHandler).Assembly);
        services.AddPerformanceBehaviorOptions(options => options.SlowRequestThreshold = TimeSpan.FromMinutes(1));
        services.AddScoped<IUnitOfWork>(_ => unitOfWork);
        using ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        Result result = await mediator.Send(new FailingCommand());

        Assert.True(result.IsFailure);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Send_ThroughPipeline_QueryNeverTouchesUnitOfWork()
    {
        var unitOfWork = new TrackingUnitOfWork();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCqrs(null, typeof(TestQueryHandler).Assembly);
        services.AddPerformanceBehaviorOptions(options => options.SlowRequestThreshold = TimeSpan.FromMinutes(1));
        services.AddScoped<IUnitOfWork>(_ => unitOfWork);
        using ServiceProvider provider = services.BuildServiceProvider();
        IMediator mediator = provider.GetRequiredService<IMediator>();

        Result<int> result = await mediator.Send(new TestQuery(42));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private sealed record TestValueCommand : ICommand<int>;

    private sealed class ThrowingUnitOfWork : IUnitOfWork
    {
        private readonly Exception _exception;

        public ThrowingUnitOfWork(Exception exception)
        {
            _exception = exception;
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => throw _exception;

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed record TestCommand(string Value) : ICommand;

    private sealed class TestCommandHandler : ICommandHandler<TestCommand>
    {
        public Task<Result> Handle(TestCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());
    }

    private sealed record FailingCommand : ICommand;

    private sealed class FailingCommandHandler : ICommandHandler<FailingCommand>
    {
        public Task<Result> Handle(FailingCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Result.Failure(Error.Validation("Failed", "boom")));
    }

    private sealed record TestQuery(int Id) : IQuery<int>;

    private sealed class TestQueryHandler : IQueryHandler<TestQuery, int>
    {
        public Task<Result<int>> Handle(TestQuery request, CancellationToken cancellationToken)
            => Task.FromResult(Result<int>.Success(request.Id));
    }

    private sealed class TrackingUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task CommitTransactionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
