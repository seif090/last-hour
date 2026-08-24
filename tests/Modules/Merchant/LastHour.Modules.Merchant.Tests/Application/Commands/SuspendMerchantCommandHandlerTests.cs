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

public class SuspendMerchantCommandHandlerTests
{
    private static SuspendMerchantCommandHandler CreateHandler(FakeMerchantRepository merchants)
        => new(merchants, NullLogger<SuspendMerchantCommandHandler>.Instance);

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Action act = () => _ = new SuspendMerchantCommandHandler(null!, NullLogger<SuspendMerchantCommandHandler>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("merchants");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var merchants = new FakeMerchantRepository();
        Action act = () => _ = new SuspendMerchantCommandHandler(merchants, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task Handle_ActiveMerchant_SuspendsSuccessfully()
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Active Store");
        merchant.Verify();
        SuspendMerchantCommandHandler handler = CreateHandler(merchants);

        Result result = await handler.Handle(
            new SuspendMerchantCommand(merchant.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        merchant.Status.Should().Be(MerchantStatus.Suspended);
    }

    [Fact]
    public async Task Handle_ActiveMerchant_EmitsMerchantStatusChangedDomainEvent()
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Active Store");
        merchant.Verify();
        merchant.ClearDomainEvents();
        SuspendMerchantCommandHandler handler = CreateHandler(merchants);

        Result result = await handler.Handle(
            new SuspendMerchantCommand(merchant.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().ContainSingle();

        MerchantStatusChangedDomainEvent domainEvent = domainEvents.OfType<MerchantStatusChangedDomainEvent>().Single();
        domainEvent.MerchantId.Should().Be(merchant.Id.Value);
        domainEvent.PreviousStatus.Should().Be("Active");
        domainEvent.CurrentStatus.Should().Be("Suspended");
    }

    [Fact]
    public async Task Handle_MissingMerchant_ReturnsNotFoundError()
    {
        FakeMerchantRepository merchants = new();
        SuspendMerchantCommandHandler handler = CreateHandler(merchants);

        Result result = await handler.Handle(
            new SuspendMerchantCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("Merchants.NotFound");
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
    }

    [Theory]
    [InlineData(MerchantStatus.Pending)]
    [InlineData(MerchantStatus.Closed)]
    public async Task Handle_InvalidStateTransition_ReturnsValidationError(MerchantStatus currentStatus)
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Target Store");

        if (currentStatus == MerchantStatus.Closed)
        {
            merchant.Close();
        }

        SuspendMerchantCommandHandler handler = CreateHandler(merchants);

        Result result = await handler.Handle(
            new SuspendMerchantCommand(merchant.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("Merchants.InvalidStatusTransition");
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
    }
}
