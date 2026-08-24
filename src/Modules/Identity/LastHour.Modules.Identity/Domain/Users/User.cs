using LastHour.BuildingBlocks.Contracts.Events;
using LastHour.BuildingBlocks.SharedKernel.Domain;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;
using LastHour.Modules.Identity.Domain.Roles;

namespace LastHour.Modules.Identity.Domain.Users;

/// <summary>
/// The identity aggregate root of the LastHour platform. It protects the invariants of a
/// user account: a valid, normalized email address, a controlled account status lifecycle, an
/// immutable identifier, a password credential that exists only as an opaque hash, and the
/// temporary authentication throttle (failed-attempt counter and lockout window) that guards
/// the credential. The aggregate deliberately contains no authentication behavior (hashing,
/// sign-in, token issuance) — those belong to the application layer; this type only guarantees
/// that identity and security state stays consistent.
/// </summary>
public sealed class User : AggregateRoot<UserId>, IAuditableEntity
{
    private readonly List<UserRole> _userRoles = new List<UserRole>();

    private User()
    {
        // EF Core materialization only.
    }

    private User(UserId id, Email email, AccountStatus status, PasswordCredential? passwordCredential)
    {
        Id = id;
        Email = email;
        Status = status;
        PasswordCredential = passwordCredential;
    }

    /// <summary>
    /// Gets the account email address. The original case is preserved for display; equality
    /// and lookups use <see cref="Email.NormalizedValue"/>.
    /// </summary>
    public Email Email { get; private set; } = null!;

    /// <summary>
    /// Gets the account lifecycle status.
    /// </summary>
    public AccountStatus Status { get; private set; }

    /// <summary>
    /// Gets the password credential (opaque hash only), or <see langword="null"/> when no
    /// credential has been established yet. Never expose this value outside persistence.
    /// </summary>
    public PasswordCredential? PasswordCredential { get; private set; }

    /// <summary>
    /// Gets the optimistic concurrency token. Every mutating operation increments it; EF Core
    /// includes the original value in update predicates so lost updates surface as
    /// <c>DbUpdateConcurrencyException</c>. The aggregate is the only writer of this value.
    /// </summary>
    public long Version { get; private set; }

    /// <summary>
    /// Gets the number of consecutive failed authentication attempts since the last successful
    /// authentication (or lockout expiry). Reset by <see cref="RecordSuccessfulLogin"/> and
    /// <see cref="MarkPasswordChanged"/>. Authentication behavior — not account status — owns
    /// this value: it is temporary throttling state, never a permanent lifecycle property.
    /// </summary>
    public int FailedAccessCount { get; private set; }

    /// <summary>
    /// Gets the moment a temporary authentication lockout ends, in UTC, or <see langword="null"/>
    /// while the account is not locked. A locked account cannot authenticate even with the
    /// correct password. This is deliberately separate from <see cref="AccountStatus"/>: the
    /// status models permanent or semi-permanent lifecycle state, lockout is a time-boxed
    /// throttle that resets on success or expiry.
    /// </summary>
    public DateTime? LockoutUntilUtc { get; private set; }

    /// <summary>
    /// Gets the moment the password was last changed, in UTC, or <see langword="null"/> when the
    /// credential has never been replaced since creation. Recorded for security auditing and to
    /// anchor the session-invalidation window after a password change.
    /// </summary>
    public DateTime? LastPasswordChangedAtUtc { get; private set; }

    /// <summary>
    /// Gets the roles assigned to this account, as read-only association records. Assignment
    /// is a set membership owned by this aggregate: a role can be added only through
    /// <see cref="AssignRole"/> and removed only through <see cref="RemoveRole"/>, and the
    /// database composite key additionally prevents duplicate assignments.
    /// </summary>
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles;

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public string? CreatedBy { get; set; }

    /// <inheritdoc/>
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc/>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Creates a new user account in the <see cref="AccountStatus.Pending"/> state and raises <see cref="UserRegisteredDomainEvent"/>.
    /// The pending state is chosen because an account must not be usable before its identity is
    /// established (for example, the address has been verified); the activation flow is a
    /// future sprint concern.
    /// </summary>
    /// <param name="email">The account email address.</param>
    /// <param name="passwordCredential">The password credential, when one is established at
    /// creation time. May be <see langword="null"/> until the registration flow sets it.</param>
    /// <returns>A new <see cref="User"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="email"/> is <see langword="null"/>.</exception>
    public static User Create(Email email, PasswordCredential? passwordCredential = null)
    {
        ArgumentNullException.ThrowIfNull(email);
        var user = new User(UserId.New(), email, AccountStatus.Pending, passwordCredential);
        user.RaiseDomainEvent(new UserRegisteredDomainEvent(user.Id.Value, user.Email.Value));
        return user;
    }

    /// <summary>
    /// Activates the account. Valid from <see cref="AccountStatus.Pending"/> (initial
    /// activation after the identity is established) and from <see cref="AccountStatus.Suspended"/>
    /// (reinstatement after review).
    /// </summary>
    /// <exception cref="BusinessRuleValidationException">The current status cannot transition
    /// to <see cref="AccountStatus.Active"/>.</exception>
    public void Activate()
    {
        EnsureStatusTransition(AccountStatus.Active);
        Status = AccountStatus.Active;
        BumpVersion();
    }

    /// <summary>
    /// Suspends the account. Valid only from <see cref="AccountStatus.Active"/>.
    /// </summary>
    /// <exception cref="BusinessRuleValidationException">The current status is not
    /// <see cref="AccountStatus.Active"/>.</exception>
    public void Suspend()
    {
        EnsureStatusTransition(AccountStatus.Suspended);
        Status = AccountStatus.Suspended;
        BumpVersion();
    }

    /// <summary>
    /// Permanently disables the account. Valid from <see cref="AccountStatus.Pending"/>,
    /// <see cref="AccountStatus.Active"/> and <see cref="AccountStatus.Suspended"/>.
    /// </summary>
    /// <exception cref="BusinessRuleValidationException">The account is already
    /// <see cref="AccountStatus.Disabled"/>.</exception>
    public void Disable()
    {
        EnsureStatusTransition(AccountStatus.Disabled);
        Status = AccountStatus.Disabled;
        BumpVersion();
    }

    /// <summary>
    /// Changes the account email address. The address of a disabled account cannot be changed
    /// (its identity is permanently frozen), and setting the same address is rejected.
    /// </summary>
    /// <param name="newEmail">The new address.</param>
    /// <exception cref="ArgumentNullException"><paramref name="newEmail"/> is <see langword="null"/>.</exception>
    /// <exception cref="BusinessRuleValidationException">The account is disabled or the
    /// normalized address is unchanged.</exception>
    public void ChangeEmail(Email newEmail)
    {
        ArgumentNullException.ThrowIfNull(newEmail);

        if (Status == AccountStatus.Disabled)
        {
            throw new BusinessRuleValidationException(
                new UserEmailChangeRule("The email address of a disabled user cannot be changed."));
        }

        if (Email.NormalizedValue == newEmail.NormalizedValue)
        {
            throw new BusinessRuleValidationException(
                new UserEmailChangeRule("The new email address is the same as the current address."));
        }

        Email = newEmail;
        BumpVersion();
    }

    /// <summary>
    /// Establishes or replaces the password credential. Only the opaque hash representation is
    /// accepted; hashing is performed outside the aggregate by the credential service.
    /// </summary>
    /// <param name="credential">The credential to store.</param>
    /// <exception cref="ArgumentNullException"><paramref name="credential"/> is <see langword="null"/>.</exception>
    public void SetPasswordCredential(PasswordCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);
        PasswordCredential = credential;
        BumpVersion();
    }

    /// <summary>
    /// Assigns a role to the account. Assigning the same role twice is rejected so a role is
    /// a set membership, never a count. The change bumps the optimistic concurrency token, so
    /// concurrent assignment attempts surface as a conflict instead of silently duplicating.
    /// </summary>
    /// <param name="roleId">The role to assign.</param>
    /// <exception cref="BusinessRuleValidationException">The role is already assigned.</exception>
    public void AssignRole(RoleId roleId)
    {
        if (HasRole(roleId))
        {
            throw new BusinessRuleValidationException(
                new UserRoleAssignmentRule($"The user already holds role '{roleId}'."));
        }

        _userRoles.Add(UserRole.Create(Id, roleId));
        BumpVersion();
    }

    /// <summary>
    /// Removes a role from the account.
    /// </summary>
    /// <param name="roleId">The role to remove.</param>
    /// <exception cref="BusinessRuleValidationException">The user does not hold the role.</exception>
    public void RemoveRole(RoleId roleId)
    {
        UserRole? assignment = _userRoles.FirstOrDefault(assignment => assignment.RoleId == roleId);
        if (assignment is null)
        {
            throw new BusinessRuleValidationException(
                new UserRoleAssignmentRule($"The user does not hold role '{roleId}'."));
        }

        _userRoles.Remove(assignment);
        BumpVersion();
    }

    /// <summary>
    /// Determines whether the account currently holds the specified role.
    /// </summary>
    /// <param name="roleId">The role to check.</param>
    /// <returns><see langword="true"/> when the role is assigned; otherwise <see langword="false"/>.</returns>
    public bool HasRole(RoleId roleId)
        => _userRoles.Any(assignment => assignment.RoleId == roleId);

    /// <summary>
    /// Determines whether the account is currently locked out.
    /// </summary>
    /// <param name="utcNow">The current instant, in UTC.</param>
    /// <returns><see langword="true"/> when a lockout is active (not yet expired).</returns>
    public bool IsLockedOut(DateTime utcNow)
        => LockoutUntilUtc is DateTime lockoutUntil && lockoutUntil > utcNow;

    /// <summary>
    /// Records a failed authentication attempt and applies the lockout policy. A lockout that
    /// has already expired is treated as finished: the count starts fresh from this attempt. An
    /// active lockout is left untouched (a locked account rejects further attempts without
    /// incrementing). When the count reaches <paramref name="maxFailedAttempts"/> the account
    /// becomes locked until <paramref name="utcNow"/> plus <paramref name="lockoutDuration"/>
    /// and raises <see cref="UserLockedOutDomainEvent"/>.
    /// </summary>
    /// <param name="utcNow">The current instant, in UTC.</param>
    /// <param name="maxFailedAttempts">The number of consecutive failures that triggers a lockout.</param>
    /// <param name="lockoutDuration">How long the lockout lasts.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxFailedAttempts"/> is less than one.</exception>
    public void RecordFailedLogin(DateTime utcNow, int maxFailedAttempts, TimeSpan lockoutDuration)
    {
        if (maxFailedAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFailedAttempts), "At least one failed attempt is required to trigger a lockout.");
        }

        if (LockoutUntilUtc is DateTime lockoutUntil && lockoutUntil <= utcNow)
        {
            // The previous lockout expired: start the counting window fresh.
            FailedAccessCount = 0;
            LockoutUntilUtc = null;
        }

        bool wasLockedOut = IsLockedOut(utcNow);
        FailedAccessCount++;
        if (FailedAccessCount >= maxFailedAttempts)
        {
            LockoutUntilUtc = utcNow.Add(lockoutDuration);
            if (!wasLockedOut && IsLockedOut(utcNow))
            {
                RaiseDomainEvent(new UserLockedOutDomainEvent(Id.Value, LockoutUntilUtc.Value));
            }
        }

        BumpVersion();
    }

    /// <summary>
    /// Records a successful authentication: the failure counter is reset and any active lockout
    /// is lifted. Called only after the password was verified and the account state allows
    /// authentication.
    /// </summary>
    /// <param name="utcNow">The current instant, in UTC.</param>
    public void RecordSuccessfulLogin(DateTime utcNow)
    {
        FailedAccessCount = 0;
        LockoutUntilUtc = null;
        BumpVersion();
    }

    /// <summary>
    /// Records a password change: the timestamp is stored and the authentication throttle is
    /// reset, because the new credential invalidates the failure history that preceded it,
    /// and raises <see cref="UserPasswordChangedDomainEvent"/>.
    /// Session invalidation (revoking outstanding refresh tokens) is orchestrated by the
    /// password-change use case, not by this aggregate.
    /// </summary>
    /// <param name="utcNow">The current instant, in UTC.</param>
    public void MarkPasswordChanged(DateTime utcNow)
    {
        LastPasswordChangedAtUtc = utcNow;
        FailedAccessCount = 0;
        LockoutUntilUtc = null;
        BumpVersion();
        RaiseDomainEvent(new UserPasswordChangedDomainEvent(Id.Value));
    }

    private static bool CanTransition(AccountStatus from, AccountStatus to) => (from, to) switch
    {
        (AccountStatus.Pending, AccountStatus.Active) => true,
        (AccountStatus.Pending, AccountStatus.Disabled) => true,
        (AccountStatus.Active, AccountStatus.Suspended) => true,
        (AccountStatus.Active, AccountStatus.Disabled) => true,
        (AccountStatus.Suspended, AccountStatus.Active) => true,
        (AccountStatus.Suspended, AccountStatus.Disabled) => true,
        _ => false,
    };

    private void EnsureStatusTransition(AccountStatus target)
    {
        if (!CanTransition(Status, target))
        {
            throw new BusinessRuleValidationException(new UserStatusTransitionRule(Status, target));
        }
    }

    private void BumpVersion() => Version++;

    private sealed class UserStatusTransitionRule : IBusinessRule
    {
        public UserStatusTransitionRule(AccountStatus from, AccountStatus to)
        {
            Message = $"A user account cannot move from '{from}' to '{to}'.";
        }

        public bool IsSatisfied => false;

        public string Message { get; }
    }

    private sealed class UserEmailChangeRule : IBusinessRule
    {
        public UserEmailChangeRule(string message)
        {
            Message = message;
        }

        public bool IsSatisfied => false;

        public string Message { get; }
    }

    private sealed class UserRoleAssignmentRule : IBusinessRule
    {
        public UserRoleAssignmentRule(string message)
        {
            Message = message;
        }

        public bool IsSatisfied => false;

        public string Message { get; }
    }
}
