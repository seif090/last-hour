using FluentAssertions;
using LastHour.BuildingBlocks.Contracts.Events;
using LastHour.BuildingBlocks.SharedKernel.Domain.Events;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Merchant.Application.Commands;
using LastHour.Modules.Merchant.Domain.Merchants;
using LastHour.Modules.Merchant.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Tests.Application.Commands;

public class RegisterMerchantCommandHandlerTests
{
    private static RegisterMerchantCommandHandler CreateHandler(FakeMerchantRepository merchants)
        => new(merchants, NullLogger<RegisterMerchantCommandHandler>.Instance);

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Action act = () => _ = new RegisterMerchantCommandHandler(null!, NullLogger<RegisterMerchantCommandHandler>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("merchants");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var merchants = new FakeMerchantRepository();
        Action act = () => _ = new RegisterMerchantCommandHandler(merchants, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task Handle_ValidCommand_RegistersMerchantSuccessfully()
    {
        FakeMerchantRepository merchants = new();
        RegisterMerchantCommandHandler handler = CreateHandler(merchants);
        Guid ownerId = Guid.NewGuid();

        var command = new RegisterMerchantCommand(
            ownerId,
            "Fresh Bakery",
            "123 Baker St",
            "Seattle",
            "WA",
            "98101",
            "USA",
            "+1-206-555-0100");

        Result<MerchantId> result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Value.Should().NotBe(Guid.Empty);

        MerchantAggregate created = merchants.All.Should().ContainSingle().Subject;
        created.Id.Should().Be(result.Value);
        created.OwnerId.Should().Be(ownerId);
        created.Name.Value.Should().Be("Fresh Bakery");
        created.Address.Street.Should().Be("123 Baker St");
        created.Address.City.Should().Be("Seattle");
        created.Address.StateOrProvince.Should().Be("WA");
        created.Address.PostalCode.Should().Be("98101");
        created.Address.Country.Should().Be("USA");
        created.Phone.Value.Should().Be("+1-206-555-0100");
        created.Status.Should().Be(MerchantStatus.Pending);
    }

    [Fact]
    public async Task Handle_ValidCommand_EmitsMerchantCreatedDomainEvent()
    {
        FakeMerchantRepository merchants = new();
        RegisterMerchantCommandHandler handler = CreateHandler(merchants);
        Guid ownerId = Guid.NewGuid();

        var command = new RegisterMerchantCommand(
            ownerId,
            "Artisan Coffee",
            "789 Pine St",
            "Portland",
            "OR",
            "97201",
            "USA",
            "+1-503-555-0120");

        Result<MerchantId> result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        MerchantAggregate created = merchants.All.Should().ContainSingle().Subject;

        IReadOnlyCollection<IDomainEvent> domainEvents = created.GetDomainEvents();
        domainEvents.Should().ContainSingle();

        MerchantCreatedDomainEvent domainEvent = domainEvents.OfType<MerchantCreatedDomainEvent>().Single();
        domainEvent.MerchantId.Should().Be(created.Id.Value);
        domainEvent.Name.Should().Be("Artisan Coffee");
        domainEvent.Address.Should().Be(created.Address.ToString());
        domainEvent.Phone.Should().Be("+1-503-555-0120");
    }

    [Fact]
    public async Task Handle_DuplicateMerchantName_ReturnsConflictError()
    {
        FakeMerchantRepository merchants = new();
        merchants.AddMerchant("Unique Coffee");
        RegisterMerchantCommandHandler handler = CreateHandler(merchants);

        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Unique Coffee",
            "100 Main St",
            "Austin",
            "TX",
            "73301",
            "USA",
            "+1-512-555-0130");

        Result<MerchantId> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("Merchants.NameAlreadyExists");
        result.FirstError!.Type.Should().Be(ErrorType.Conflict);
        merchants.All.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_InvalidMerchantName_ReturnsValidationError()
    {
        FakeMerchantRepository merchants = new();
        RegisterMerchantCommandHandler handler = CreateHandler(merchants);

        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            string.Empty,
            "100 Main St",
            "Austin",
            "TX",
            "73301",
            "USA",
            "+1-512-555-0130");

        Result<MerchantId> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("InvalidMerchantName");
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        merchants.All.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_InvalidAddress_ReturnsValidationError()
    {
        FakeMerchantRepository merchants = new();
        RegisterMerchantCommandHandler handler = CreateHandler(merchants);

        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Name",
            string.Empty,
            "Austin",
            "TX",
            "73301",
            "USA",
            "+1-512-555-0130");

        Result<MerchantId> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("InvalidAddress");
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        merchants.All.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_InvalidPhoneNumber_ReturnsValidationError()
    {
        FakeMerchantRepository merchants = new();
        RegisterMerchantCommandHandler handler = CreateHandler(merchants);

        var command = new RegisterMerchantCommand(
            Guid.NewGuid(),
            "Valid Name",
            "100 Main St",
            "Austin",
            "TX",
            "73301",
            "USA",
            string.Empty);

        Result<MerchantId> result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("InvalidPhoneNumber");
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
        merchants.All.Should().BeEmpty();
    }
}
