using LastHour.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace LastHour.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Marks a message that performs a state-changing operation and produces a value of type
/// <typeparamref name="TValue"/>. A command is handled by an
/// <see cref="ICommandHandler{TCommand, TValue}"/> whose outcome is a
/// <see cref="Result{TValue}"/>. Commands of this shape are committed by the same pipeline
/// behavior as value-less commands (see <see cref="ICommandBase"/>).
/// </summary>
/// <typeparam name="TValue">The type of the value produced on success.</typeparam>
public interface ICommand<TValue> : ICommandBase, IRequest<Result<TValue>>
{
}
