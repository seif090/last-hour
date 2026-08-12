using MediatR;

namespace LastHour.BuildingBlocks.Application.Cqrs;

/// <summary>
/// Marks a message as a command. Shared by value-less and value-returning commands so
/// pipeline behaviors (for example the transaction behavior) can recognize every command
/// without reflection.
/// </summary>
public interface ICommandBase : IBaseRequest
{
}
