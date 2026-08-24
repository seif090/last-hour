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

public class VerifyMerchantCommandHandlerTests
{
    private static VerifyMerchantCommandHandler CreateHandler(FakeMerchantRepository merchants)
        => new(merchants, NullLogger<VerifyMerchantCommandHandler>.Instance);

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Action act = () => _ = new VerifyMerchantCommandHandler(null!, NullLogger<VerifyMerchantCommandHandler>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("merchants");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var merchants = new FakeMerchantRepository();
        Action act = () => _ = new VerifyMerchantCommandHandler(merchants, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task Handle_PendingMerchant_VerifiesAndActivatesSuccessfully()
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Pending Store");
        VerifyMerchantCommandHandler handler = CreateHandler(merchants);

        Result result = await handler.Handle(
            new VerifyMerchantCommand(merchant.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        merchant.Status.Should().Be(MerchantStatus.Active);
    }

    [Fact]
    public async Task Handle_PendingMerchant_EmitsMerchantStatusChangedDomainEvent()
    {
        FakeMerchantRepository merchants = new();
        MerchantAggregate merchant = merchants.AddMerchant("Pending Store");
        merchant.ClearDomainEvents();
        VerifyMerchantCommandHandler handler = CreateHandler(merchants);

        Result result = await handler.Handle(
            new VerifyMerchantCommand(merchant.Id.Value),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        IReadOnlyCollection<IDomainEvent> domainEvents = merchant.GetDomainEvents();
        domainEvents.Should().ContainSingle();

        MerchantStatusChangedDomainEvent domainEvent = domainEvents.OfType<MerchantStatusChangedDomainEvent>().Single();
        domainEvent.MerchantId.Should().Be(merchant.Id.Value);
        domainEvent.PreviousStatus.Should().Be("Pending");
        domainEvent.CurrentStatus.Should().Be("Active");
    }

    [Fact]
    public async Task Handle_MissingMerchant_ReturnsNotFoundError()
    {
        FakeMerchantRepository merchants = new();
        VerifyMerchantCommandHandler handler = CreateHandler(merchants);

        Result result = await handler.Handle(
            new VerifyMerchantCommand(Guid.NewGuid()),
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

        VerifyMerchantCommandHandler handler = CreateHandler(merchants);

        Result result = await handler.Handle(
            new VerifyMerchantCommand(merchant.Id.Value),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError.Should().NotBeNull();
        result.FirstError!.Code.Should().Be("Merchants.InvalidStatusTransition");
        result.FirstError!.Type.Should().Be(ErrorType.Validation);
    }
}
