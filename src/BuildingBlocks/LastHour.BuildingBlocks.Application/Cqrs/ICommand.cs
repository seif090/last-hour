using LastHour.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace LastHour.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Marks a message that performs a state-changing operation. A command is handled by an
/// <see cref="ICommandHandler{TCommand}"/> whose outcome is a
/// <see cref="Result"/>, distinguishing success from expected, non-exceptional failures.
/// Derives from MediatR's <see cref="IRequest{TResponse}"/> so commands can be dispatched
/// through the MediatR pipeline.
/// </summary>
public interface ICommand : ICommandBase, IRequest<Result>
{
}
