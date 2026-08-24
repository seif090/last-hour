namespace LastHour.Modules.Identity.Application.Permissions;

/// <summary>
/// The roles the database bootstrap seeds. Only the minimum set required to establish the
/// authorization model is defined here: a single privileged <see cref="Administrator"/> role
/// holding every catalog permission, with no user account attached. The bootstrap never
/// creates accounts or credentials — assigning the role to a real user is a future,
/// protected provisioning flow. No default role is assigned to registrations: the business
/// requirements do not define one, and assigning any role during public registration would
/// be a privilege-escalation surface.
/// </summary>
public static class BootstrapRoles
{
    /// <summary>
    /// The name of the seeded administrator role, which holds every catalog permission.
    /// </summary>
    public const string Administrator = "Administrator";
}
