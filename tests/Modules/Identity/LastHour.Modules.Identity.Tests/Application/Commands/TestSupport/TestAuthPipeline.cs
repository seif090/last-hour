using FluentValidation;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Exceptions;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.BuildingBlocks.Infrastructure.Persistence.UnitOfWork;
using LastHour.BuildingBlocks.Infrastructure.Transactions;
using LastHour.BuildingBlocks.Infrastructure.Validation;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Application;
using LastHour.Modules.Identity.Application.Commands;
using LastHour.Modules.Identity.Application.Persistence;
using LastHour.Modules.Identity.Application.RefreshTokens;
using LastHour.Modules.Identity.Domain.RefreshTokens;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Application.TestSupport;
using LastHour.Modules.Identity.Tests.Persistence.TestSupport;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Identity.Tests.Application.Commands.TestSupport;

/// <summary>
/// Boots the real CQRS pipeline — MediatR with the unhandled-exception, transaction and
/// validation behaviors, FluentValidation validators, the module's credential and refresh-token
/// services and repository adapters, and EF Core over a shared-cache SQLite database. This
/// exercises the same composition the API uses, so registration, login, refresh and logout
/// behavior (including the database unique constraint and the rotation concurrency token) is
/// tested against real persistence.
/// </summary>
public sealed class TestAuthPipeline : IAsyncDisposable
{
    private readonly ServiceProvider _provider;
    private readonly SqliteConnection _anchor;

    private TestAuthPipeline(ServiceProvider provider, SqliteConnection anchor)
    {
        _provider = provider;
        _anchor = anchor;
    }

    /// <summary>
    /// Creates the pipeline over a fresh shared-cache in-memory database with a created schema.
    /// </summary>
    /// <returns>A ready-to-use pipeline.</returns>
    public static async Task<TestAuthPipeline> CreateAsync()
    {
        string connectionString = TestIdentityPersistence.CreateSharedConnectionString(out SqliteConnection anchor);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(RegisterUserCommand).Assembly);
            config.Lifetime = ServiceLifetime.Scoped;
            config.AddOpenBehavior(typeof(UnhandledExceptionBehavior<,>));
            config.AddOpenBehavior(typeof(TransactionBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(typeof(RegisterUserCommand).Assembly);
        services.AddSingleton(Options.Create(new PasswordPolicy()));
        services.AddSingleton(Options.Create(new RefreshTokenOptions()));
        services.AddSingleton(Options.Create(new AccountSecurityOptions()));
        services.AddSingleton<IClock>(new FixedClock(new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc)));
        services.AddScoped<IPasswordHasher, StubPasswordHasher>();
        services.AddScoped<IPasswordCredentialService, PasswordCredentialService>();
        services.AddSingleton<IAccessTokenGenerator, StubAccessTokenGenerator>();
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<LastHourDbContext>(_ => TestIdentityPersistence.CreateSqliteContext(connectionString));
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IReadRepository<,>), typeof(EfReadRepository<,>));
        services.AddScoped(typeof(ITrackedRepository<,>), typeof(EfTrackedRepository<,>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        ServiceProvider provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });

        using (IServiceScope scope = provider.CreateScope())
        {
            LastHourDbContext context = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        return new TestAuthPipeline(provider, anchor);
    }

    /// <summary>
    /// Dispatches a registration command through the pipeline and returns its outcome.
    /// </summary>
    /// <param name="email">The email to register.</param>
    /// <param name="password">The password to register.</param>
    /// <returns>The command result.</returns>
    public async Task<Result<UserId>> RegisterAsync(string email, string password)
    {
        using IServiceScope scope = _provider.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send(new RegisterUserCommand(email, password));
    }

    /// <summary>
    /// Dispatches a login command through the pipeline and returns its outcome.
    /// </summary>
    /// <param name="email">The email to authenticate.</param>
    /// <param name="password">The password to authenticate.</param>
    /// <returns>The command result.</returns>
    public async Task<Result<LoginResult>> LoginAsync(string email, string password)
    {
        using IServiceScope scope = _provider.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send(new LoginCommand(email, password));
    }

    /// <summary>
    /// Dispatches a refresh command through the pipeline and returns its outcome.
    /// </summary>
    /// <param name="refreshToken">The raw refresh token to rotate.</param>
    /// <returns>The command result.</returns>
    public async Task<Result<RefreshTokenResult>> RefreshAsync(string refreshToken)
    {
        using IServiceScope scope = _provider.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send(new RefreshTokenCommand(refreshToken));
    }

    /// <summary>
    /// Dispatches a logout command through the pipeline and returns its outcome.
    /// </summary>
    /// <param name="refreshToken">The raw refresh token of the session to end.</param>
    /// <returns>The command result.</returns>
    public async Task<Result> LogoutAsync(string refreshToken)
    {
        using IServiceScope scope = _provider.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        return await sender.Send(new LogoutCommand(refreshToken));
    }

    /// <summary>
    /// Queries a user by its stored normalized email value from a fresh scope.
    /// </summary>
    /// <param name="normalizedEmail">The normalized address to look up.</param>
    /// <returns>The matching user, or <see langword="null"/>.</returns>
    public async Task<User?> FindByNormalizedEmailAsync(string normalizedEmail)
    {
        using IServiceScope scope = _provider.CreateScope();
        LastHourDbContext context = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
        return await context.Set<User>()
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Email.NormalizedValue == normalizedEmail);
    }

    /// <summary>
    /// Activates a registered (pending) user through the real context, which is what the tests
    /// need because registration always creates pending accounts and there is no activation
    /// endpoint yet.
    /// </summary>
    /// <param name="normalizedEmail">The normalized address of the user to activate.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ActivateUserAsync(string normalizedEmail)
    {
        using IServiceScope scope = _provider.CreateScope();
        LastHourDbContext context = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
        User? user = await context.Set<User>()
            .SingleOrDefaultAsync(item => item.Email.NormalizedValue == normalizedEmail);
        if (user is not null)
        {
            user.Activate();
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Queries the stored refresh-token records from a fresh scope.
    /// </summary>
    /// <returns>All refresh-token records.</returns>
    public async Task<IReadOnlyList<RefreshToken>> GetRefreshTokensAsync()
    {
        using IServiceScope scope = _provider.CreateScope();
        LastHourDbContext context = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
        return await context.Set<RefreshToken>().AsNoTracking().ToListAsync();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();
        _anchor.Dispose();
    }
}
