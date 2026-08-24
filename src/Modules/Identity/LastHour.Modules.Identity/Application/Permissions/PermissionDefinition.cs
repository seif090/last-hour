using System.Text.RegularExpressions;

namespace LastHour.Modules.Identity.Application.Permissions;

/// <summary>
/// A declared application capability, identified by a stable code following the
/// <c>Resource.Action</c> convention (for example <c>Users.Read</c>). Definitions come from
/// the centralized <see cref="PermissionCatalog"/> only — never from client input — and are
/// seeded into the database as <c>Permission</c> entities. The code is the deterministic
/// identity of the capability: it is what authorization checks compare against and what the
/// database enforces as unique.
/// </summary>
public sealed record PermissionDefinition
{
    /// <summary>
    /// The regular expression a permission code must satisfy: one or more dot-separated
    /// segments, each starting with a letter and continuing with letters or digits (for
    /// example <c>Users.Read</c> or <c>Merchant.Offers.Create</c>).
    /// </summary>
    public static readonly Regex CodePattern = new Regex(
        @"^[A-Za-z][A-Za-z0-9]*(\.[A-Za-z][A-Za-z0-9]*)+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionDefinition"/> class.
    /// </summary>
    /// <param name="code">The stable capability code.</param>
    /// <param name="description">An optional human-readable purpose.</param>
    public PermissionDefinition(string code, string? description)
    {
        Code = code;
        Description = description;
    }

    /// <summary>
    /// Gets the stable capability code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the optional human-readable purpose.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Validates the structural shape of a permission code.
    /// </summary>
    /// <returns><see langword="true"/> when the code follows the convention; otherwise <see langword="false"/>.</returns>
    public bool HasValidCode() => CodePattern.IsMatch(Code);
}
