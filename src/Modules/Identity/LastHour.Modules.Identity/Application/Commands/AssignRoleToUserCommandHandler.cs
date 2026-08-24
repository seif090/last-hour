using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Domain.Roles;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Assigns a role to a user. The user is loaded tracked (so the assignment bumps the
/// optimistic-concurrency token and concurrent attempts surface as conflicts), both sides are
/// validated to exist, and the domain rejects a duplicate assignment before persistence. The
/// composite key of <c>user_roles</c> is the correctness backstop: a concurrent double-assign
/// fails at the database and is translated into a safe conflict result by the pipeline. The
/// operation is scoped to the identifiers given by a protected management flow — nothing about
/// the target user or role is derived from public input.
/// </summary>
public sealed class AssignRoleToUserCommandHandler : ICommandHandler<AssignRoleToUserCommand>
{
    private static readonly Action<ILogger, Guid, Guid, Exception?> RoleAssigned =
        LoggerMessage.Define<Guid, Guid>(
            LogLevel.Information,
            new EventId(3003, nameof(AssignRoleToUserCommandHandler)),
            "Role {RoleId} assigned to user {UserId}.");

    private static readonly Action<ILogger, Guid, Guid, Exception?> DuplicateAssignmentRejected =
        LoggerMessage.Define<Guid, Guid>(
            LogLevel.Warning,
            new EventId(3004, nameof(AssignRoleToUserCommandHandler)),
            "Duplicate role assignment rejected for user {UserId} and role {RoleId}.");

    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly ILogger<AssignRoleToUserCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AssignRoleToUserCommandHandler"/> class.
    /// </summary>
    /// <param name="users">The user repository used to load the account for mutation.</param>
    /// <param name="roles">The role repository used to validate the role exists.</param>
    /// <param name="logger">The logger used to record the security event without sensitive values.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public AssignRoleToUserCommandHandler(
        IUserRepository users,
        IRoleRepository roles,
        ILogger<AssignRoleToUserCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(logger);
        _users = users;
        _roles = roles;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(AssignRoleToUserCommand command, CancellationToken cancellationToken)
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
            user.AssignRole(roleId);
        }
        catch (BusinessRuleValidationException exception)
        {
            DuplicateAssignmentRejected(_logger, command.UserId, command.RoleId, null);
            return Result.Failure(Error.Conflict(
                "Users.RoleAlreadyAssigned",
                exception.Message));
        }

        RoleAssigned(_logger, command.RoleId, command.UserId, null);
        return Result.Success();
    }
}
