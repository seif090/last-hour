using System.Reflection;
using FluentAssertions;
using LastHour.BuildingBlocks.Contracts.Events;
using LastHour.BuildingBlocks.SharedKernel.Domain.Events;

namespace LastHour.BuildingBlocks.Contracts.Tests.Events;

public class DomainEventContractsTests
{
    [Fact]
    public void MerchantStatusChangedDomainEvent_ConstructsAndExposesCorrectProperties()
    {
        Guid merchantId = Guid.NewGuid();
        string previousStatus = "Pending";
        string currentStatus = "Active";

        var domainEvent = new MerchantStatusChangedDomainEvent(merchantId, previousStatus, currentStatus);

        domainEvent.Should().BeAssignableTo<BaseDomainEvent>();
        domainEvent.Should().BeAssignableTo<IDomainEvent>();
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        domainEvent.MerchantId.Should().Be(merchantId);
        domainEvent.PreviousStatus.Should().Be(previousStatus);
        domainEvent.CurrentStatus.Should().Be(currentStatus);
    }

    [Fact]
    public void MerchantCreatedDomainEvent_ConstructsAndExposesCorrectProperties()
    {
        Guid merchantId = Guid.NewGuid();
        string name = "Merchant Name";
        string address = "123 Street";
        string phone = "+123456789";

        var domainEvent = new MerchantCreatedDomainEvent(merchantId, name, address, phone);

        domainEvent.Should().BeAssignableTo<BaseDomainEvent>();
        domainEvent.Should().BeAssignableTo<IDomainEvent>();
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        domainEvent.MerchantId.Should().Be(merchantId);
        domainEvent.Name.Should().Be(name);
        domainEvent.Address.Should().Be(address);
        domainEvent.Phone.Should().Be(phone);
    }

    [Fact]
    public void MerchantProfileUpdatedDomainEvent_ConstructsAndExposesCorrectProperties()
    {
        Guid merchantId = Guid.NewGuid();
        string name = "Updated Merchant Name";

        var domainEvent = new MerchantProfileUpdatedDomainEvent(merchantId, name);

        domainEvent.Should().BeAssignableTo<BaseDomainEvent>();
        domainEvent.Should().BeAssignableTo<IDomainEvent>();
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        domainEvent.MerchantId.Should().Be(merchantId);
        domainEvent.Name.Should().Be(name);
    }

    [Fact]
    public void UserRegisteredDomainEvent_ConstructsAndExposesCorrectProperties()
    {
        Guid userId = Guid.NewGuid();
        string email = "user@example.com";

        var domainEvent = new UserRegisteredDomainEvent(userId, email);

        domainEvent.Should().BeAssignableTo<BaseDomainEvent>();
        domainEvent.Should().BeAssignableTo<IDomainEvent>();
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        domainEvent.UserId.Should().Be(userId);
        domainEvent.Email.Should().Be(email);
    }

    [Fact]
    public void UserPasswordChangedDomainEvent_ConstructsAndExposesCorrectProperties()
    {
        Guid userId = Guid.NewGuid();

        var domainEvent = new UserPasswordChangedDomainEvent(userId);

        domainEvent.Should().BeAssignableTo<BaseDomainEvent>();
        domainEvent.Should().BeAssignableTo<IDomainEvent>();
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        domainEvent.UserId.Should().Be(userId);
    }

    [Fact]
    public void UserLockedOutDomainEvent_ConstructsAndExposesCorrectProperties()
    {
        Guid userId = Guid.NewGuid();
        DateTime lockoutUntilUtc = DateTime.UtcNow.AddMinutes(15);

        var domainEvent = new UserLockedOutDomainEvent(userId, lockoutUntilUtc);

        domainEvent.Should().BeAssignableTo<BaseDomainEvent>();
        domainEvent.Should().BeAssignableTo<IDomainEvent>();
        domainEvent.EventId.Should().NotBeEmpty();
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        domainEvent.UserId.Should().Be(userId);
        domainEvent.LockoutUntilUtc.Should().Be(lockoutUntilUtc);
    }

    [Theory]
    [InlineData(typeof(MerchantCreatedDomainEvent))]
    [InlineData(typeof(MerchantStatusChangedDomainEvent))]
    [InlineData(typeof(MerchantProfileUpdatedDomainEvent))]
    [InlineData(typeof(UserRegisteredDomainEvent))]
    [InlineData(typeof(UserPasswordChangedDomainEvent))]
    [InlineData(typeof(UserLockedOutDomainEvent))]
    public void ContractDomainEvents_DoNotExposeModuleInternalTypes(Type eventType)
    {
        eventType.Namespace.Should().Be("LastHour.BuildingBlocks.Contracts.Events");
        eventType.IsSubclassOf(typeof(BaseDomainEvent)).Should().BeTrue();

        PropertyInfo[] properties = eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (PropertyInfo property in properties)
        {
            Type propType = property.PropertyType;
            if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                propType = Nullable.GetUnderlyingType(propType)!;
            }

            bool isAllowedType = propType == typeof(Guid) ||
                                 propType == typeof(string) ||
                                 propType == typeof(DateTime) ||
                                 propType == typeof(int) ||
                                 propType == typeof(long) ||
                                 propType == typeof(bool) ||
                                 propType == typeof(decimal) ||
                                 propType == typeof(double);

            isAllowedType.Should().BeTrue(
                $"Contract event property {eventType.Name}.{property.Name} type '{property.PropertyType.FullName}' must only use contract-safe primitive/framework types.");
        }
    }
}
