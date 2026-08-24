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

public class CloseMerchantCommandHandlerTests
{
    private static CloseMerchantCommandHandler CreateHandler(FakeMerchantRepository merchants)
        => new(merchants, NullLogger<CloseMerchantCommandHandler>.Instance);

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Action act = () => _ = new CloseMerchantCommandHandler(null!, NullLogger<CloseMerchantCommandHandler>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("merchants");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var merchants = new FakeMerchantRepository();
        Action act = () => _ = new CloseMerchantCommandHandler(merchants, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Theory]
    [InlineData(MerchantStatus.Pending)]
    [InlineData(MerchantStatus.Active)]
    [InlineData(MerchantStatus.Suspended)]
    public async Task Handle_ValidMerchant_ClosesSuccessfully(MerchantStatus initialStatus)
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Target Store");

        if (initialStatus == MerchantStatus.Active)
        {
            merchant.Verify();
        }
        else if (initialStatus == MerchantStatus.Suspended)
        {
            merchant.Verify();
            merchant.Suspend();
        }

        merchant.ClearDomainEvents();
        CloseMerchantCommandHandler handler = CreateHandler(merchants);

        Result result = await handler.Handle(
            new CloseMerchantCommand(merchant.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        merchant.Status.Should().Be(MerchantStatus.Closed);

        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().ContainSingle();

        MerchantStatusChangedDomainEvent domainEvent = domainEvents.OfType<MerchantStatusChangedDomainEvent>().Single();
        domainEvent.MerchantId.Should().Be(merchant.Id.Value);
        domainEvent.PreviousStatus.Should().Be(initialStatus.ToString());
        domainEvent.CurrentStatus.Should().Be("Closed");
    }

    [Fact]
    public async Task Handle_MissingMerchant_ReturnsNotFoundError()
    {
        FakeMerchantRepository merchants = new();
        CloseMerchantCommandHandler handler = CreateHandler(merchants);

        Result result = await handler.Handle(
            new CloseMerchantCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("Merchants.NotFound");
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_AlreadyClosedMerchant_ReturnsInvalidStatusTransitionError()
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Target Store");
        merchant.Close();
        CloseMerchantCommandHandler handler = CreateHandler(merchants);

        Result result = await handler.Handle(
            new CloseMerchantCommand(merchant.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("Merchants.InvalidStatusTransition");
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
    }
}
