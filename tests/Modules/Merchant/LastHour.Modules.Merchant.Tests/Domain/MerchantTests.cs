using System.Text.Json;
using FluentAssertions;
using LastHour.BuildingBlocks.Contracts.Events;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.SharedKernel.Domain.Events;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;
using LastHour.Modules.Merchant.Domain.Merchants;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Tests.Domain;

public class MerchantTests
{
    private static readonly Guid DefaultOwnerId = Guid.NewGuid();
    private static readonly MerchantName DefaultName = MerchantName.From("Acme Supplies");
    private static readonly Address DefaultAddress = Address.Create("123 Market Street", "Seattle", "WA", "98101", "USA");
    private static readonly PhoneNumber DefaultPhone = PhoneNumber.From("+1-206-555-0100");

    [Fact]
    public void Register_ValidArguments_CreatesMerchantInPendingStatusAndRaisesCreatedEvent()
    {
        MerchantAggregate merchant = MerchantAggregate.Register(
            DefaultOwnerId,
            DefaultName,
            DefaultAddress,
            DefaultPhone);

        merchant.Id.Should().NotBeNull();
        merchant.Id.Value.Should().NotBeEmpty();
        merchant.OwnerId.Should().Be(DefaultOwnerId);
        merchant.Name.Should().Be(DefaultName);
        merchant.Address.Should().Be(DefaultAddress);
        merchant.Phone.Should().Be(DefaultPhone);
        merchant.Status.Should().Be(MerchantStatus.Pending);

        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().HaveCount(1);

        IDomainEvent domainEvent = domainEvents.Single();
        domainEvent.Should().BeOfType<MerchantCreatedDomainEvent>();

        var createdEvent = (MerchantCreatedDomainEvent)domainEvent;
        createdEvent.MerchantId.Should().Be(merchant.Id.Value);
        createdEvent.Name.Should().Be(DefaultName.Value);
        createdEvent.Address.Should().Be(DefaultAddress.ToString());
        createdEvent.Phone.Should().Be(DefaultPhone.Value);
    }

    [Fact]
    public void Register_EmptyOwnerId_ThrowsArgumentException()
    {
        Action act = () => MerchantAggregate.Register(
            Guid.Empty,
            DefaultName,
            DefaultAddress,
            DefaultPhone);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("ownerId");
    }

    [Fact]
    public void Register_NullName_ThrowsArgumentNullException()
    {
        Action act = () => MerchantAggregate.Register(
            DefaultOwnerId,
            null!,
            DefaultAddress,
            DefaultPhone);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void Register_NullAddress_ThrowsArgumentNullException()
    {
        Action act = () => MerchantAggregate.Register(
            DefaultOwnerId,
            DefaultName,
            null!,
            DefaultPhone);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("address");
    }

    [Fact]
    public void Register_NullPhone_ThrowsArgumentNullException()
    {
        Action act = () => MerchantAggregate.Register(
            DefaultOwnerId,
            DefaultName,
            DefaultAddress,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("phone");
    }

    [Fact]
    public void UpdateProfile_WhenDetailsChange_UpdatesPropertiesAndRaisesDomainEvent()
    {
        MerchantAggregate merchant = CreatePendingMerchant();
        merchant.ClearDomainEvents();

        MerchantName updatedName = MerchantName.From("New Acme Supplies");
        Address updatedAddress = Address.Create("456 Broad Street", "Tacoma", "WA", "98402", "USA");
        PhoneNumber updatedPhone = PhoneNumber.From("+1-253-555-0200");

        bool result = merchant.UpdateProfile(updatedName, updatedAddress, updatedPhone);

        result.Should().BeTrue();
        merchant.Name.Should().Be(updatedName);
        merchant.Address.Should().Be(updatedAddress);
        merchant.Phone.Should().Be(updatedPhone);

        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().HaveCount(1);

        IDomainEvent domainEvent = domainEvents.Single();
        domainEvent.Should().BeOfType<MerchantProfileUpdatedDomainEvent>();

        var profileUpdatedEvent = (MerchantProfileUpdatedDomainEvent)domainEvent;
        profileUpdatedEvent.MerchantId.Should().Be(merchant.Id.Value);
        profileUpdatedEvent.Name.Should().Be(updatedName.Value);
    }

    [Fact]
    public void UpdateProfile_WhenNoDetailsChange_ReturnsFalseAndDoesNotRaiseDomainEvent()
    {
        MerchantAggregate merchant = CreatePendingMerchant();
        merchant.ClearDomainEvents();

        bool result = merchant.UpdateProfile(DefaultName, DefaultAddress, DefaultPhone);

        result.Should().BeFalse();
        merchant.Name.Should().Be(DefaultName);
        merchant.Address.Should().Be(DefaultAddress);
        merchant.Phone.Should().Be(DefaultPhone);
        merchant.GetDomainEvents().Should().BeEmpty();
    }

    [Fact]
    public void UpdateProfile_WhenOnlyNameChanges_RaisesDomainEvent()
    {
        MerchantAggregate merchant = CreatePendingMerchant();
        merchant.ClearDomainEvents();

        MerchantName updatedName = MerchantName.From("Only Name Changed");

        bool result = merchant.UpdateProfile(updatedName, DefaultAddress, DefaultPhone);

        result.Should().BeTrue();
        merchant.Name.Should().Be(updatedName);
        merchant.Address.Should().Be(DefaultAddress);
        merchant.Phone.Should().Be(DefaultPhone);

        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().HaveCount(1);

        var profileUpdatedEvent = (MerchantProfileUpdatedDomainEvent)domainEvents.Single();
        profileUpdatedEvent.MerchantId.Should().Be(merchant.Id.Value);
        profileUpdatedEvent.Name.Should().Be(updatedName.Value);
    }

    [Fact]
    public void UpdateProfile_WhenOnlyAddressChanges_RaisesDomainEvent()
    {
        MerchantAggregate merchant = CreatePendingMerchant();
        merchant.ClearDomainEvents();

        Address updatedAddress = Address.Create("789 New St", "Spokane", "WA", "99201", "USA");

        bool result = merchant.UpdateProfile(DefaultName, updatedAddress, DefaultPhone);

        result.Should().BeTrue();
        merchant.Name.Should().Be(DefaultName);
        merchant.Address.Should().Be(updatedAddress);
        merchant.Phone.Should().Be(DefaultPhone);

        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().HaveCount(1);

        var profileUpdatedEvent = (MerchantProfileUpdatedDomainEvent)domainEvents.Single();
        profileUpdatedEvent.MerchantId.Should().Be(merchant.Id.Value);
        profileUpdatedEvent.Name.Should().Be(DefaultName.Value);
    }

    [Fact]
    public void UpdateProfile_WhenOnlyPhoneChanges_RaisesDomainEvent()
    {
        MerchantAggregate merchant = CreatePendingMerchant();
        merchant.ClearDomainEvents();

        PhoneNumber updatedPhone = PhoneNumber.From("+1-206-555-9999");

        bool result = merchant.UpdateProfile(DefaultName, DefaultAddress, updatedPhone);

        result.Should().BeTrue();
        merchant.Name.Should().Be(DefaultName);
        merchant.Address.Should().Be(DefaultAddress);
        merchant.Phone.Should().Be(updatedPhone);

        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().HaveCount(1);

        var profileUpdatedEvent = (MerchantProfileUpdatedDomainEvent)domainEvents.Single();
        profileUpdatedEvent.MerchantId.Should().Be(merchant.Id.Value);
        profileUpdatedEvent.Name.Should().Be(DefaultName.Value);
    }

    [Fact]
    public void UpdateProfile_NullName_ThrowsArgumentNullException()
    {
        MerchantAggregate merchant = CreatePendingMerchant();

        Action act = () => merchant.UpdateProfile(null!, DefaultAddress, DefaultPhone);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateProfile_NullAddress_ThrowsArgumentNullException()
    {
        MerchantAggregate merchant = CreatePendingMerchant();

        Action act = () => merchant.UpdateProfile(DefaultName, null!, DefaultPhone);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateProfile_NullPhone_ThrowsArgumentNullException()
    {
        MerchantAggregate merchant = CreatePendingMerchant();

        Action act = () => merchant.UpdateProfile(DefaultName, DefaultAddress, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Verify_FromPending_TransitionsToActiveAndRaisesStatusChangedEvent()
    {
        MerchantAggregate merchant = CreatePendingMerchant();
        merchant.ClearDomainEvents();

        merchant.Verify();

        merchant.Status.Should().Be(MerchantStatus.Active);

        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().HaveCount(1);

        IDomainEvent domainEvent = domainEvents.Single();
        domainEvent.Should().BeOfType<MerchantStatusChangedDomainEvent>();

        var statusChangedEvent = (MerchantStatusChangedDomainEvent)domainEvent;
        statusChangedEvent.MerchantId.Should().Be(merchant.Id.Value);
        statusChangedEvent.PreviousStatus.Should().Be("Pending");
        statusChangedEvent.CurrentStatus.Should().Be("Active");
    }

    [Fact]
    public void Suspend_FromActive_TransitionsToSuspendedAndRaisesStatusChangedEvent()
    {
        MerchantAggregate merchant = CreateActiveMerchant();

        merchant.Suspend();

        merchant.Status.Should().Be(MerchantStatus.Suspended);

        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().HaveCount(1);

        IDomainEvent domainEvent = domainEvents.Single();
        domainEvent.Should().BeOfType<MerchantStatusChangedDomainEvent>();

        var statusChangedEvent = (MerchantStatusChangedDomainEvent)domainEvent;
        statusChangedEvent.MerchantId.Should().Be(merchant.Id.Value);
        statusChangedEvent.PreviousStatus.Should().Be("Active");
        statusChangedEvent.CurrentStatus.Should().Be("Suspended");
    }

    [Fact]
    public void Activate_FromSuspended_TransitionsToActiveAndRaisesStatusChangedEvent()
    {
        MerchantAggregate merchant = CreateSuspendedMerchant();

        merchant.Activate();

        merchant.Status.Should().Be(MerchantStatus.Active);

        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().HaveCount(1);

        IDomainEvent domainEvent = domainEvents.Single();
        domainEvent.Should().BeOfType<MerchantStatusChangedDomainEvent>();

        var statusChangedEvent = (MerchantStatusChangedDomainEvent)domainEvent;
        statusChangedEvent.MerchantId.Should().Be(merchant.Id.Value);
        statusChangedEvent.PreviousStatus.Should().Be("Suspended");
        statusChangedEvent.CurrentStatus.Should().Be("Active");
    }

    [Fact]
    public void Close_FromPending_TransitionsToClosedAndRaisesStatusChangedEvent()
    {
        MerchantAggregate merchant = CreatePendingMerchant();
        merchant.ClearDomainEvents();

        merchant.Close();

        merchant.Status.Should().Be(MerchantStatus.Closed);

        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().HaveCount(1);

        IDomainEvent domainEvent = domainEvents.Single();
        domainEvent.Should().BeOfType<MerchantStatusChangedDomainEvent>();

        var statusChangedEvent = (MerchantStatusChangedDomainEvent)domainEvent;
        statusChangedEvent.MerchantId.Should().Be(merchant.Id.Value);
        statusChangedEvent.PreviousStatus.Should().Be("Pending");
        statusChangedEvent.CurrentStatus.Should().Be("Closed");
    }

    [Fact]
    public void Close_FromActive_TransitionsToClosedAndRaisesStatusChangedEvent()
    {
        MerchantAggregate merchant = CreateActiveMerchant();

        merchant.Close();

        merchant.Status.Should().Be(MerchantStatus.Closed);

        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().HaveCount(1);

        IDomainEvent domainEvent = domainEvents.Single();
        domainEvent.Should().BeOfType<MerchantStatusChangedDomainEvent>();

        var statusChangedEvent = (MerchantStatusChangedDomainEvent)domainEvent;
        statusChangedEvent.MerchantId.Should().Be(merchant.Id.Value);
        statusChangedEvent.PreviousStatus.Should().Be("Active");
        statusChangedEvent.CurrentStatus.Should().Be("Closed");
    }

    [Fact]
    public void Close_FromSuspended_TransitionsToClosedAndRaisesStatusChangedEvent()
    {
        MerchantAggregate merchant = CreateSuspendedMerchant();

        merchant.Close();

        merchant.Status.Should().Be(MerchantStatus.Closed);

        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().HaveCount(1);

        IDomainEvent domainEvent = domainEvents.Single();
        domainEvent.Should().BeOfType<MerchantStatusChangedDomainEvent>();

        var statusChangedEvent = (MerchantStatusChangedDomainEvent)domainEvent;
        statusChangedEvent.MerchantId.Should().Be(merchant.Id.Value);
        statusChangedEvent.PreviousStatus.Should().Be("Suspended");
        statusChangedEvent.CurrentStatus.Should().Be("Closed");
    }

    [Fact]
    public void Verify_WhenAlreadyActive_ThrowsBusinessRuleValidationException()
    {
        MerchantAggregate merchant = CreateActiveMerchant();

        Action act = () => merchant.Verify();

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Suspend_WhenPending_ThrowsBusinessRuleValidationException()
    {
        MerchantAggregate merchant = CreatePendingMerchant();

        Action act = () => merchant.Suspend();

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ThrowsBusinessRuleValidationException()
    {
        MerchantAggregate merchant = CreateActiveMerchant();

        Action act = () => merchant.Activate();

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Activate_WhenClosed_ThrowsBusinessRuleValidationException()
    {
        MerchantAggregate merchant = CreateClosedMerchant();

        Action act = () => merchant.Activate();

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Verify_WhenClosed_ThrowsBusinessRuleValidationException()
    {
        MerchantAggregate merchant = CreateClosedMerchant();

        Action act = () => merchant.Verify();

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Suspend_WhenClosed_ThrowsBusinessRuleValidationException()
    {
        MerchantAggregate merchant = CreateClosedMerchant();

        Action act = () => merchant.Suspend();

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Close_WhenAlreadyClosed_ThrowsBusinessRuleValidationException()
    {
        MerchantAggregate merchant = CreateClosedMerchant();

        Action act = () => merchant.Close();

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Suspend_WhenAlreadySuspended_ThrowsBusinessRuleValidationException()
    {
        MerchantAggregate merchant = CreateSuspendedMerchant();

        Action act = () => merchant.Suspend();

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void DomainEvents_SerializeAndDeserializeThroughOutbox_PreservesEventIdentityAndTimestamp()
    {
        MerchantAggregate merchant = CreatePendingMerchant();
        merchant.UpdateProfile(
            MerchantName.From("New Acme Supplies"),
            Address.Create("456 New Ave", "Seattle", "WA", "98102", "USA"),
            PhoneNumber.From("+1-206-555-0999"));

        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().HaveCount(2);

        var createdEvent = domainEvents.OfType<MerchantCreatedDomainEvent>().Single();
        var updatedEvent = domainEvents.OfType<MerchantProfileUpdatedDomainEvent>().Single();

        string createdJson = JsonSerializer.Serialize(createdEvent, OutboxJson.SerializerOptions);
        var rehydratedCreated = JsonSerializer.Deserialize<MerchantCreatedDomainEvent>(createdJson, OutboxJson.SerializerOptions);

        rehydratedCreated.Should().NotBeNull();
        rehydratedCreated!.EventId.Should().Be(createdEvent.EventId);
        rehydratedCreated.OccurredOn.Should().Be(createdEvent.OccurredOn);
        rehydratedCreated.MerchantId.Should().Be(merchant.Id.Value);
        rehydratedCreated.Name.Should().Be(DefaultName.Value);
        rehydratedCreated.Address.Should().Be(DefaultAddress.ToString());
        rehydratedCreated.Phone.Should().Be(DefaultPhone.Value);

        string updatedJson = JsonSerializer.Serialize(updatedEvent, OutboxJson.SerializerOptions);
        var rehydratedUpdated = JsonSerializer.Deserialize<MerchantProfileUpdatedDomainEvent>(updatedJson, OutboxJson.SerializerOptions);

        rehydratedUpdated.Should().NotBeNull();
        rehydratedUpdated!.EventId.Should().Be(updatedEvent.EventId);
        rehydratedUpdated.OccurredOn.Should().Be(updatedEvent.OccurredOn);
        rehydratedUpdated.MerchantId.Should().Be(merchant.Id.Value);
        rehydratedUpdated.Name.Should().Be("New Acme Supplies");
    }

    private static MerchantAggregate CreatePendingMerchant()
    {
        return MerchantAggregate.Register(DefaultOwnerId, DefaultName, DefaultAddress, DefaultPhone);
    }

    private static MerchantAggregate CreateActiveMerchant()
    {
        MerchantAggregate merchant = CreatePendingMerchant();
        merchant.Verify();
        merchant.ClearDomainEvents();
        return merchant;
    }

    private static MerchantAggregate CreateSuspendedMerchant()
    {
        MerchantAggregate merchant = CreateActiveMerchant();
        merchant.Suspend();
        merchant.ClearDomainEvents();
        return merchant;
    }

    private static MerchantAggregate CreateClosedMerchant()
    {
        MerchantAggregate merchant = CreatePendingMerchant();
        merchant.Close();
        merchant.ClearDomainEvents();
        return merchant;
    }
}
