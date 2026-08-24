using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Identity.Domain.Roles;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.Identity.Application.Commands;

/// <summary>
/// Creates a role through the aggregate: the name is normalized by the domain's single rule,
/// a duplicate normalized name is rejected before persistence (with the database unique index
/// as the correctness backstop under concurrency), and the aggregate is registered for the
/// transaction pipeline to commit atomically. No privilege data comes from the caller beyond
/// the name and description.
/// </summary>
public sealed class CreateRoleCommandHandler : ICommandHandler<CreateRoleCommand, RoleId>
{
    private static readonly Action<ILogger, Guid, string, Exception?> RoleCreated =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(3001, nameof(CreateRoleCommandHandler)),
            "Role {RoleId} created with normalized name {NormalizedName}.");

    private static readonly Action<ILogger, Exception?> DuplicateRoleRejected =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(3002, nameof(CreateRoleCommandHandler)),
            "Role creation rejected because the normalized name is already in use.");

    private readonly IRoleRepository _roles;
    private readonly ILogger<CreateRoleCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateRoleCommandHandler"/> class.
    /// </summary>
    /// <param name="roles">The role repository used for the duplicate pre-check and persistence.</param>
    /// <param name="logger">The logger used to record the security event without sensitive values.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public CreateRoleCommandHandler(IRoleRepository roles, ILogger<CreateRoleCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(logger);
        _roles = roles;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<RoleId>> Handle(CreateRoleCommand command, CancellationToken cancellationToken)
    {
        Role role;
        try
        {
            role = Role.Create(command.Name, command.Description);
        }
        catch (InvalidRoleNameException exception)
        {
            return Result<RoleId>.Failure(
                Error.Validation("InvalidRoleName", exception.Message));
        }

        if (await _roles.ExistsByNormalizedNameAsync(role.Name.NormalizedValue, cancellationToken).ConfigureAwait(false))
        {
            DuplicateRoleRejected(_logger, null);
            return Result<RoleId>.Failure(Error.Conflict(
                "Roles.NameAlreadyExists",
                "A role with this name already exists."));
        }

        _roles.Add(role);

        RoleCreated(_logger, role.Id.Value, role.Name.NormalizedValue, null);
        return Result<RoleId>.Success(role.Id);
    }
}
