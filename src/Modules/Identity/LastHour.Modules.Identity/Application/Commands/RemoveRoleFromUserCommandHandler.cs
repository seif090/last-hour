using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Removes a role assignment from a user. The user is loaded tracked so the change bumps the
/// optimistic-concurrency token, both sides are validated to exist, and the domain rejects
/// removing a role the user does not hold.
/// </summary>
public sealed class RemoveRoleFromUserCommandHandler : ICommandHandler<RemoveRoleFromUserCommand>
{
    private static readonly Action<ILogger, Guid, Guid, Exception?> RoleRemoved =
        LoggerMessage.Define<Guid, Guid>(
            LogLevel.Information,
            new EventId(3005, nameof(RemoveRoleFromUserCommandHandler)),
            "Role {RoleId} removed from user {UserId}.");

    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly ILogger<RemoveRoleFromUserCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoveRoleFromUserCommandHandler"/> class.
    /// </summary>
    /// <param name="users">The user repository used to load the account for mutation.</param>
    /// <param name="roles">The role repository used to validate the role exists.</param>
    /// <param name="logger">The logger used to record the security event without sensitive values.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public RemoveRoleFromUserCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        ILogger<RemoveRoleFromUserCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(logger);
        _users = users;
        _roles = roles;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(RemoveRoleFromUserCommand command, CancellationToken cancellationToken)
    {
        UserId userId = UserId.Create(command.UserId);
        RoleId roleId = RoleId.Create(command.RoleId);

        User? user = await _users.GetByIdTrackedAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return Result.Failure(Error.NotFound(
                "Users.NotFound",
                "The user could not be found."));
        }

        if (!await _roles.ExistsByIdAsync(roleId, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure(Error.NotFound(
                "Roles.NotFound",
                "The role could not be found."));
        }

        try
        {
            user.RemoveRole(roleId);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Result.Failure(Error.Conflict(
                "Users.RoleNotAssigned",
                exception.Message));
        }

        RoleRemoved(_logger, command.RoleId, command.UserId, null);
        return Result.Success();
    }
}
