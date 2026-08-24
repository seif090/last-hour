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

public class ActivateMerchantCommandHandlerTests
{
    private static ActivateMerchantCommandHandler CreateHandler(FakeMerchantRepository merchants)
        => new(merchants, NullLogger<ActivateMerchantCommandHandler>.Instance);

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Action act = () => _ = new ActivateMerchantCommandHandler(null!, NullLogger<ActivateMerchantCommandHandler>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("merchants");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var merchants = new FakeMerchantRepository();
        Action act = () => _ = new ActivateMerchantCommandHandler(merchants, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task Handle_SuspendedMerchant_ActivatesSuccessfully()
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Suspended Store");
        merchant.Verify();
        merchant.Suspend();
        ActivateMerchantCommandHandler handler = CreateHandler(merchants);

        Result result = await handler.Handle(
            new ActivateMerchantCommand(merchant.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        merchant.Status.Should().Be(MerchantStatus.Active);
    }

    [Fact]
    public async Task Handle_SuspendedMerchant_EmitsMerchantStatusChangedDomainEvent()
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Suspended Store");
        merchant.Verify();
        merchant.Suspend();
        merchant.ClearDomainEvents();
        ActivateMerchantCommandHandler handler = CreateHandler(merchants);

        Result result = await handler.Handle(
            new ActivateMerchantCommand(merchant.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().ContainSingle();

        MerchantStatusChangedDomainEvent domainEvent = domainEvents.OfType<MerchantStatusChangedDomainEvent>().Single();
        domainEvent.MerchantId.Should().Be(merchant.Id.Value);
        domainEvent.PreviousStatus.Should().Be("Suspended");
        domainEvent.CurrentStatus.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_MissingMerchant_ReturnsNotFoundError()
    {
        FakeMerchantRepository merchants = new();
        ActivateMerchantCommandHandler handler = CreateHandler(merchants);

        Result result = await handler.Handle(
            new ActivateMerchantCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("Merchants.NotFound");
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
    }

    [Theory]
    [InlineData(MerchantStatus.Active)]
    [InlineData(MerchantStatus.Closed)]
    public async Task Handle_InvalidStateTransition_ReturnsValidationError(MerchantStatus currentStatus)
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Target Store");

        if (currentStatus == MerchantStatus.Active)
        {
            merchant.Verify();
        }
        else if (currentStatus == MerchantStatus.Closed)
        {
            merchant.Close();
        }

        ActivateMerchantCommandHandler handler = CreateHandler(merchants);

        Result result = await handler.Handle(
            new ActivateMerchantCommand(merchant.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("Merchants.InvalidStatusTransition");
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
    }
}
