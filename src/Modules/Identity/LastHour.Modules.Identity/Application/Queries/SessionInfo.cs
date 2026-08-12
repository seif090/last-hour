namespace LastHour.Modules.Identity.Application.Queries;

/// <summary>
/// The safe metadata of one session exposed by the session-inspection endpoint. It carries no
/// token material, hashes, family identifiers or any internal state — only the session's id and
/// lifecycle timestamps. There is no device metadata to expose because the session model
/// deliberately stores none.
/// </summary>
public sealed record SessionInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionInfo"/> class.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="createdAtUtc">The moment the session was created, in UTC.</param>
    /// <param name="expiresAtUtc">The moment the session expires, in UTC.</param>
    /// <param name="isActive">Whether the session can currently be used to refresh.</param>
    public SessionInfo(Guid sessionId, DateTime createdAtUtc, DateTime expiresAtUtc, bool isActive)
    {
        SessionId = sessionId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        IsActive = isActive;
    }

    /// <summary>
    /// Gets the session identifier.
    /// </summary>
    public Guid SessionId { get; }

    /// <summary>
    /// Gets the moment the session was created, in UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; }

    /// <summary>
    /// Gets the moment the session expires, in UTC.
    /// </summary>
    public DateTime ExpiresAtUtc { get; }

    /// <summary>
    /// Gets a value indicating whether the session can currently be used to refresh.
    /// </summary>
    public bool IsActive { get; }
}
