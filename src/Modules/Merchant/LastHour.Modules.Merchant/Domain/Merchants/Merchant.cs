using LastHour.BuildingBlocks.Contracts.Events;
using LastHour.BuildingBlocks.SharedKernel.Domain;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;

namespace LastHour.Modules.Merchant.Domain.Merchants;

/// <summary>
/// The merchant aggregate root of the merchant module. It represents a registered merchant on the platform,
/// protecting its identity, profile details, ownership, and lifecycle status transitions.
/// </summary>
public sealed class Merchant : AggregateRoot<MerchantId>, IAuditableEntity
{
    private Merchant()
    {
        // EF Core materialization only.
    }

    private Merchant(
        MerchantId id,
        Guid ownerId,
        MerchantName name,
        Address address,
        PhoneNumber phone,
        MerchantStatus status)
    {
        Id = id;
        OwnerId = ownerId;
        Name = name;
        Address = address;
        Phone = phone;
        Status = status;
    }

    /// <summary>
    /// Gets the unique identifier of the user who owns this merchant account.
    /// </summary>
    public Guid OwnerId { get; private set; }

    /// <summary>
    /// Gets the display name of the merchant.
    /// </summary>
    public MerchantName Name { get; private set; } = null!;

    /// <summary>
    /// Gets the physical or mailing address of the merchant.
    /// </summary>
    public Address Address { get; private set; } = null!;

    /// <summary>
    /// Gets the contact phone number of the merchant.
    /// </summary>
    public PhoneNumber Phone { get; private set; } = null!;

    /// <summary>
    /// Gets the current lifecycle status of the merchant.
    /// </summary>
    public MerchantStatus Status { get; private set; }

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public string? CreatedBy { get; set; }

    /// <inheritdoc/>
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc/>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Registers a new merchant with <see cref="MerchantStatus.Pending"/> status and raises <see cref="MerchantCreatedDomainEvent"/>.
    /// </summary>
    /// <param name="ownerId">The user identifier of the owner.</param>
    /// <param name="name">The name of the merchant.</param>
    /// <param name="address">The address of the merchant.</param>
    /// <param name="phone">The contact phone number of the merchant.</param>
    /// <returns>A new <see cref="Merchant"/> instance in <see cref="MerchantStatus.Pending"/> status.</returns>
    /// <exception cref="ArgumentException"><paramref name="ownerId"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">Any other argument is <see langword="null"/>.</exception>
    public static Merchant Register(
        Guid ownerId,
        MerchantName name,
        Address address,
        PhoneNumber phone)
    {
        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("Owner identifier cannot be empty.", nameof(ownerId));
        }

        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(phone);

        var merchant = new Merchant(
            MerchantId.New(),
            ownerId,
            name,
            address,
            phone,
            MerchantStatus.Pending);

        merchant.RaiseDomainEvent(new MerchantCreatedDomainEvent(
            merchant.Id.Value,
            merchant.Name.Value,
            merchant.Address.ToString(),
            merchant.Phone.Value));

        return merchant;
    }

    /// <summary>
    /// Updates the profile information of the merchant.
    /// </summary>
    /// <param name="name">The new merchant name.</param>
    /// <param name="address">The new merchant address.</param>
    /// <param name="phone">The new contact phone number.</param>
    /// <returns><see langword="true"/> if any profile detail was changed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public bool UpdateProfile(
        MerchantName name,
        Address address,
        PhoneNumber phone)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(phone);

        if (Name == name &&
            Address == address &&
            Phone == phone)
        {
            return false;
        }

        Name = name;
        Address = address;
        Phone = phone;

        RaiseDomainEvent(new MerchantProfileUpdatedDomainEvent(Id.Value, Name.Value));
        return true;
    }

    /// <summary>
    /// Verifies and activates the merchant from <see cref="MerchantStatus.Pending"/>.
    /// </summary>
    /// <exception cref="BusinessRuleValidationException">The merchant is not in a valid state to be verified.</exception>
    public void Verify()
    {
        EnsureStatusTransition(MerchantStatus.Active);
        MerchantStatus previousStatus = Status;
        Status = MerchantStatus.Active;

        RaiseDomainEvent(new MerchantStatusChangedDomainEvent(Id.Value, previousStatus.ToString(), Status.ToString()));
    }

    /// <summary>
    /// Suspends the active merchant account.
    /// </summary>
    /// <exception cref="BusinessRuleValidationException">The merchant is not in a valid state to be suspended.</exception>
    public void Suspend()
    {
        EnsureStatusTransition(MerchantStatus.Suspended);
        MerchantStatus previousStatus = Status;
        Status = MerchantStatus.Suspended;

        RaiseDomainEvent(new MerchantStatusChangedDomainEvent(Id.Value, previousStatus.ToString(), Status.ToString()));
    }

    /// <summary>
    /// Reactivates a suspended merchant account.
    /// </summary>
    /// <exception cref="BusinessRuleValidationException">The merchant is not in a valid state to be activated.</exception>
    public void Activate()
    {
        EnsureStatusTransition(MerchantStatus.Active);
        MerchantStatus previousStatus = Status;
        Status = MerchantStatus.Active;

        RaiseDomainEvent(new MerchantStatusChangedDomainEvent(Id.Value, previousStatus.ToString(), Status.ToString()));
    }

    /// <summary>
    /// Permanently closes the merchant account.
    /// </summary>
    /// <exception cref="BusinessRuleValidationException">The merchant is already closed.</exception>
    public void Close()
    {
        EnsureStatusTransition(MerchantStatus.Closed);
        MerchantStatus previousStatus = Status;
        Status = MerchantStatus.Closed;

        RaiseDomainEvent(new MerchantStatusChangedDomainEvent(Id.Value, previousStatus.ToString(), Status.ToString()));
    }

    private static bool CanTransition(MerchantStatus from, MerchantStatus to) => (from, to) switch
    {
        (MerchantStatus.Pending, MerchantStatus.Active) => true,
        (MerchantStatus.Pending, MerchantStatus.Closed) => true,
        (MerchantStatus.Active, MerchantStatus.Suspended) => true,
        (MerchantStatus.Active, MerchantStatus.Closed) => true,
        (MerchantStatus.Suspended, MerchantStatus.Active) => true,
        (MerchantStatus.Suspended, MerchantStatus.Closed) => true,
        _ => false,
    };

    private void EnsureStatusTransition(MerchantStatus target)
    {
        if (!CanTransition(Status, target))
        {
            throw new BusinessRuleValidationException(new MerchantStatusTransitionRule(Status, target));
        }
    }

    private sealed class MerchantStatusTransitionRule : IBusinessRule
    {
        public MerchantStatusTransitionRule(MerchantStatus from, MerchantStatus to)
        {
            Message = $"A merchant cannot move from '{from}' to '{to}'.";
        }

        public bool IsSatisfied => false;

        public string Message { get; }
    }
}
