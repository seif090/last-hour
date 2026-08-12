using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.Infrastructure.Results;
using LastHour.BuildingBlocks.SharedKernel.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LastHour.BuildingBlocks.Infrastructure.Transactions;

/// <summary>
/// MediatR pipeline behavior that persists command changes atomically: it commits the unit of
/// work only when a command completes successfully and leaves it untouched on failure or
/// exception, so partial changes are never written. Queries pass through without touching the
/// unit of work. A database uniqueness violation raised by the commit (the loser of a
/// concurrent insert race) is translated into a safe conflict result instead of surfacing as
/// an internal server error.
/// </summary>
/// <typeparam name="TRequest">The type of the request being handled.</typeparam>
/// <typeparam name="TResponse">The result type returned by the request handler.</typeparam>
public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    private static readonly Func<Error, TResponse> FailedResultFactory = ResultFailureFactory.Create<TResponse>();

    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="unitOfWork">The unit of work whose pending changes are committed after a successful command.</param>
    public TransactionBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Invokes the handler and commits pending changes only when the request is a command that
    /// completed successfully. Failed results and exceptions leave the unit of work uncommitted,
    /// and queries never reach the unit of work.
    /// </summary>
    /// <param name="request">The request being handled.</param>
    /// <param name="next">The delegate that invokes the next pipeline step.</param>
    /// <param name="cancellationToken">The token used to signal cancellation.</param>
    /// <returns>The handler result, committed to the data store when the request is a successful command.</returns>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICommandBase)
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        TResponse response = await next(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccess)
        {
            return response;
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The command mutated an aggregate whose optimistic concurrency token no longer
            // matches the stored value (another request committed first). The losing request
            // fails safely as a conflict instead of surfacing as an internal error.
            return FailedResultFactory(Error.Conflict(
                "ConcurrencyConflict",
                "The resource was modified by another request."));
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            return FailedResultFactory(Error.Conflict(
                "ResourceConflict",
                "The operation conflicts with an existing resource."));
        }

        return response;
    }

    /// <summary>
    /// Determines whether a database exception is a unique-constraint violation. The production
    /// provider (PostgreSQL) is detected by SQLSTATE; the SQLite provider, used only by the test
    /// suite, is detected by its stable error message.
    /// </summary>
    /// <param name="exception">The database update exception to classify.</param>
    /// <returns><see langword="true"/> when the violation is a duplicate unique value.</returns>
    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        if (exception.InnerException is PostgresException { SqlState: "23505" })
        {
            return true;
        }

        return exception.InnerException?.Message.Contains(
            "UNIQUE constraint failed",
            StringComparison.OrdinalIgnoreCase) == true;
    }
}
