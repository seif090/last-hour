using FluentAssertions;
using LastHour.BuildingBlocks.Contracts.Events;
using LastHour.BuildingBlocks.SharedKernel.Domain.Events;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Merchant.Application.Commands;
using LastHour.Modules.Merchant.Tests.Application.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Tests.Application.Commands;

public class UpdateMerchantProfileCommandHandlerTests
{
    private static UpdateMerchantProfileCommandHandler CreateHandler(FakeMerchantRepository merchants)
        => new(merchants, NullLogger<UpdateMerchantProfileCommandHandler>.Instance);

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Action act = () => _ = new UpdateMerchantProfileCommandHandler(null!, NullLogger<UpdateMerchantProfileCommandHandler>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("merchants");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var merchants = new FakeMerchantRepository();
        Action act = () => _ = new UpdateMerchantProfileCommandHandler(merchants, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task Handle_ExistingMerchant_UpdatesProfileSuccessfully()
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Old Name", "Old City");
        UpdateMerchantProfileCommandHandler handler = CreateHandler(merchants);

        var command = new UpdateMerchantProfileCommand(
            merchant.Id.Value,
            merchant.OwnerId,
            "Updated Name",
            "500 New Street",
            "Denver",
            "CO",
            "80202",
            "USA",
            "+1-303-555-0150");

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        merchant.Name.Value.Should().Be("Updated Name");
        merchant.Address.Street.Should().Be("500 New Street");
        merchant.Address.City.Should().Be("Denver");
        merchant.Address.StateOrProvince.Should().Be("CO");
        merchant.Address.PostalCode.Should().Be("80202");
        merchant.Address.Country.Should().Be("USA");
        merchant.Phone.Value.Should().Be("+1-303-555-0150");
    }

    [Fact]
    public async Task Handle_ExistingMerchant_EmitsMerchantProfileUpdatedDomainEvent()
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Original Store");
        merchant.ClearDomainEvents();
        UpdateMerchantProfileCommandHandler handler = CreateHandler(merchants);

        var command = new UpdateMerchantProfileCommand(
            merchant.Id.Value,
            merchant.OwnerId,
            "Updated Store",
            "500 New Street",
            "Denver",
            "CO",
            "80202",
            "USA",
            "+1-303-555-0150");

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().ContainSingle();

        MerchantProfileUpdatedDomainEvent domainEvent = domainEvents.OfType<MerchantProfileUpdatedDomainEvent>().Single();
        domainEvent.MerchantId.Should().Be(merchant.Id.Value);
        domainEvent.Name.Should().Be("Updated Store");
    }

    [Fact]
    public async Task Handle_NonOwner_ReturnsForbiddenError()
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Initial Store");
        UpdateMerchantProfileCommandHandler handler = CreateHandler(merchants);

        var command = new UpdateMerchantProfileCommand(
            merchant.Id.Value,
            Guid.NewGuid(),
            "Updated Store",
            "500 New Street",
            "Denver",
            "CO",
            "80202",
            "USA",
            "+1-303-555-0150");

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("Merchants.Forbidden");
        result.FirstError!.Type.Should().Be(ErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_MissingMerchant_ReturnsNotFoundError()
    {
        FakeMerchantRepository merchants = new();
        UpdateMerchantProfileCommandHandler handler = CreateHandler(merchants);

        var command = new UpdateMerchantProfileCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Nonexistent Name",
            "500 New Street",
            "Denver",
            "CO",
            "80202",
            "USA",
            "+1-303-555-0150");

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("Merchants.NotFound");
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_InvalidMerchantName_ReturnsValidationError()
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Initial Store");
        UpdateMerchantProfileCommandHandler handler = CreateHandler(merchants);

        var command = new UpdateMerchantProfileCommand(
            merchant.Id.Value,
            merchant.OwnerId,
            string.Empty,
            "500 New Street",
            "Denver",
            "CO",
            "80202",
            "USA",
            "+1-303-555-0150");

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("InvalidMerchantName");
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_InvalidAddress_ReturnsValidationError()
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Initial Store");
        UpdateMerchantProfileCommandHandler handler = CreateHandler(merchants);

        var command = new UpdateMerchantProfileCommand(
            merchant.Id.Value,
            merchant.OwnerId,
            "Valid Name",
            string.Empty,
            "Denver",
            "CO",
            "80202",
            "USA",
            "+1-303-555-0150");

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("InvalidAddress");
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_InvalidPhoneNumber_ReturnsValidationError()
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Initial Store");
        UpdateMerchantProfileCommandHandler handler = CreateHandler(merchants);

        var command = new UpdateMerchantProfileCommand(
            merchant.Id.Value,
            merchant.OwnerId,
            "Valid Name",
            "500 New Street",
            "Denver",
            "CO",
            "80202",
            "USA",
            string.Empty);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("InvalidPhoneNumber");
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_NameChangedToExistingDuplicate_ReturnsConflictError()
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchantA = merchants.AddMerchant("Store Alpha");
        MerchantAggregate merchantB = merchants.AddMerchant("Store Beta");
        UpdateMerchantProfileCommandHandler handler = CreateHandler(merchants);

        var command = new UpdateMerchantProfileCommand(
            merchantA.Id.Value,
            merchantA.OwnerId,
            "Store Beta",
            "500 New Street",
            "Denver",
            "CO",
            "80202",
            "USA",
            "+1-303-555-0150");

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("Merchants.NameAlreadyExists");
        result.FirstError!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_SameNameUnchanged_SucceedsWithoutDuplicateConflict()
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Existing Store");
        UpdateMerchantProfileCommandHandler handler = CreateHandler(merchants);

        var command = new UpdateMerchantProfileCommand(
            merchant.Id.Value,
            merchant.OwnerId,
            "Existing Store",
            "500 New Street",
            "Denver",
            "CO",
            "80202",
            "USA",
            "+1-303-555-0150");

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        merchant.Address.Street.Should().Be("500 New Street");
        merchants.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ProfileUnchanged_ReturnsSuccess()
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Existing Store");
        UpdateMerchantProfileCommandHandler handler = CreateHandler(merchants);

        var command = new UpdateMerchantProfileCommand(
            merchant.Id.Value,
            merchant.OwnerId,
            merchant.Name.Value,
            merchant.Address.Street,
            merchant.Address.City,
            merchant.Address.StateOrProvince,
            merchant.Address.PostalCode,
            merchant.Address.Country,
            merchant.Phone.Value);

        Result result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        merchants.UpdateCount.Should().Be(0);
    }
}
