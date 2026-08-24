namespace LastHour.Modules.Identity.Application.Permissions;

/// <summary>
/// The single source of truth for application permissions. Every capability the backend knows
/// about is declared here as a <see cref="PermissionDefinition"/>, seeded into the database,
/// and referenced by future authorization checks — no magic strings anywhere else. The catalog
/// is deliberately focused: only capabilities of existing or immediately planned features are
/// declared, so the list grows with the product instead of anticipating modules that do not
/// exist yet. The static constructor validates the invariants (unique codes, valid
/// <c>Resource.Action</c> format), so a broken catalog fails at load time, not at runtime.
/// </summary>
public static class PermissionCatalog
{
    /// <summary>
    /// The capability to view user accounts.
    /// </summary>
    public static readonly PermissionDefinition UsersRead = new PermissionDefinition(
        "Users.Read",
        "View user accounts.");

    /// <summary>
    /// The capability to manage user accounts (create, update, disable).
    /// </summary>
    public static readonly PermissionDefinition UsersManage = new PermissionDefinition(
        "Users.Manage",
        "Create, update and disable user accounts.");

    /// <summary>
    /// The capability to view roles and their permissions.
    /// </summary>
    public static readonly PermissionDefinition RolesRead = new PermissionDefinition(
        "Roles.Read",
        "View roles and their permissions.");

    /// <summary>
    /// The capability to manage roles and assignments (create roles, assign and revoke
    /// permissions and user assignments).
    /// </summary>
    public static readonly PermissionDefinition RolesManage = new PermissionDefinition(
        "Roles.Manage",
        "Create roles and manage role and permission assignments.");

    /// <summary>
    /// The capability to view categories and products.
    /// </summary>
    public static readonly PermissionDefinition CatalogRead = new PermissionDefinition(
        "Catalog.Read",
        "View categories and products.");

    /// <summary>
    /// The capability to create, update and delete catalog items.
    /// </summary>
    public static readonly PermissionDefinition CatalogManage = new PermissionDefinition(
        "Catalog.Manage",
        "Create, update and delete catalog items.");

    /// <summary>
    /// The capability to view merchants and their profiles.
    /// </summary>
    public static readonly PermissionDefinition MerchantsRead = new PermissionDefinition(
        "Merchants.Read",
        "View merchants and their profiles.");

    /// <summary>
    /// The capability to manage merchant accounts and lifecycle.
    /// </summary>
    public static readonly PermissionDefinition MerchantsManage = new PermissionDefinition(
        "Merchants.Manage",
        "Manage merchant accounts and lifecycle.");

    /// <summary>
    /// The capability to view transactional outbox messages and operational diagnostics.
    /// </summary>
    public static readonly PermissionDefinition OutboxRead = new PermissionDefinition(
        "Outbox.Read",
        "View transactional outbox messages and operational diagnostics.");

    /// <summary>
    /// The capability to manage the transactional outbox pipeline and retry operations.
    /// </summary>
    public static readonly PermissionDefinition OutboxManage = new PermissionDefinition(
        "Outbox.Manage",
        "Manage the transactional outbox pipeline and retry operations.");

    /// <summary>
    /// The capability to view notification deliveries and operational history.
    /// </summary>
    public static readonly PermissionDefinition NotificationsRead = new PermissionDefinition(
        "Notifications.Read",
        "View notification deliveries and operational history.");

    /// <summary>
    /// The capability to manage notification deliveries and trigger administrative requeues.
    /// </summary>
    public static readonly PermissionDefinition NotificationsManage = new PermissionDefinition(
        "Notifications.Manage",
        "Manage notification deliveries and trigger administrative requeues.");

    /// <summary>
    /// Gets every permission in the catalog, in declaration order. The seed bootstrap uses
    /// this list, so the catalog and the database can never drift silently.
    /// </summary>
    public static readonly IReadOnlyList<PermissionDefinition> All = new PermissionDefinition[]
    {
        UsersRead,
        UsersManage,
        RolesRead,
        RolesManage,
        CatalogRead,
        CatalogManage,
        MerchantsRead,
        MerchantsManage,
        OutboxRead,
        OutboxManage,
        NotificationsRead,
        NotificationsManage,
    };

    static PermissionCatalog()
    {
        HashSet<string> codes = new HashSet<string>(StringComparer.Ordinal);
        foreach (PermissionDefinition permission in All)
        {
            if (!permission.HasValidCode())
            {
                throw new InvalidOperationException(
                    $"The permission catalog contains an invalid code '{permission.Code}'. " +
                    "Codes must follow the Resource.Action convention.");
            }

            if (!codes.Add(permission.Code))
            {
                throw new InvalidOperationException(
                    $"The permission catalog defines the code '{permission.Code}' more than once.");
            }
        }
    }
}
