using FluentAssertions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Merchant.Application.Models;
using LastHour.Modules.Merchant.Application.Queries;
using LastHour.Modules.Merchant.Tests.Application.TestSupport;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Tests.Application.Queries;

public class GetMerchantByIdQueryHandlerTests
{
    private static GetMerchantByIdQueryHandler CreateHandler(FakeMerchantRepository merchants)
        => new(merchants);

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Action act = () => _ = new GetMerchantByIdQueryHandler(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("merchants");
    }

    [Fact]
    public async Task Handle_ExistingMerchant_ReturnsSuccessWithMappedDto()
    {
        FakeMerchantRepository merchants = new();
        Guid ownerId = Guid.NewGuid();
        MerchantAggregate merchant = merchants.AddMerchant(
            "Organic Grocer",
            "San Francisco",
            ownerId,
            "456 Market St",
            "CA",
            "94105",
            "USA",
            "+1-555-0200");

        GetMerchantByIdQueryHandler handler = CreateHandler(merchants);

        Result<MerchantDto> result = await handler.Handle(
            new GetMerchantByIdQuery(merchant.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(merchant.Id.Value);
        result.Value.OwnerId.Should().Be(ownerId);
        result.Value.Name.Should().Be("Organic Grocer");
        result.Value.Street.Should().Be("456 Market St");
        result.Value.City.Should().Be("San Francisco");
        result.Value.StateOrProvince.Should().Be("CA");
        result.Value.PostalCode.Should().Be("94105");
        result.Value.Country.Should().Be("USA");
        result.Value.Phone.Should().Be("+1-555-0200");
        result.Value.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Handle_ActiveMerchant_MapsStatusCorrectly()
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Active Bakery");
        merchant.Verify();

        GetMerchantByIdQueryHandler handler = CreateHandler(merchants);

        Result<MerchantDto> result = await handler.Handle(
            new GetMerchantByIdQuery(merchant.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_NonExistentMerchant_ReturnsNotFound()
    {
        FakeMerchantRepository merchants = new();
        GetMerchantByIdQueryHandler handler = CreateHandler(merchants);

        Result<MerchantDto> result = await handler.Handle(
            new GetMerchantByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("Merchants.NotFound");
        result.FirstError!.Description.Should().Be("The merchant could not be found.");
    }
}
