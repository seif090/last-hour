using LastHour.BuildingBlocks.SharedKernel.Domain;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;

namespace LastHour.Modules.Identity.Domain.Users;

/// <summary>
/// The identity aggregate root of the LastHour platform. It protects the invariants of a
/// user account: a valid, normalized email address, a controlled account status lifecycle, an
/// immutable identifier, and a password credential that exists only as an opaque hash. The
/// aggregate deliberately contains no authentication behavior (hashing, sign-in, tokens) —
/// those belong to future sprints; this type only guarantees that identity state stays
/// consistent.
/// </summary>
public sealed class User : AggregateRoot<UserId>, IAuditableEntity
{
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

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public string? CreatedBy { get; set; }

    /// <inheritdoc/>
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc/>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Creates a new user account in the <see cref="AccountStatus.Pending"/> state. The
    /// pending state is chosen because an account must not be usable before its identity is
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
        return new User(UserId.New(), email, AccountStatus.Pending, passwordCredential);
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
    /// accepted; hashing is performed outside the aggregate by the future authentication flow.
    /// </summary>
    /// <param name="credential">The credential to store.</param>
    /// <exception cref="ArgumentNullException"><paramref name="credential"/> is <see langword="null"/>.</exception>
    public void SetPasswordCredential(PasswordCredential credential)
    {
        ArgumentNullException.ThrowIfNull(credential);
        PasswordCredential = credential;
        BumpVersion();
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
}
