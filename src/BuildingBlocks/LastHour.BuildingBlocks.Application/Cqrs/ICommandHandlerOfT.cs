using LastHour.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace LastHour.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Handles a command of type <typeparamref name="TCommand"/> and produces a value of type
/// <typeparamref name="TValue"/>. Inherits MediatR's
/// <see cref="IRequestHandler{TRequest, TResponse}"/> contract, so implementations provide
/// <c>Task&lt;Result&lt;TValue&gt;&gt; Handle(TCommand command, CancellationToken cancellationToken)</c>
/// and are discovered automatically by MediatR's assembly scan rather than being registered
/// manually.
/// </summary>
/// <typeparam name="TCommand">The type of the command to handle.</typeparam>
/// <typeparam name="TValue">The type of the value produced on success.</typeparam>
public interface ICommandHandler<in TCommand, TValue> : IRequestHandler<TCommand, Result<TValue>>
    where TCommand : ICommand<TValue>
{
}
