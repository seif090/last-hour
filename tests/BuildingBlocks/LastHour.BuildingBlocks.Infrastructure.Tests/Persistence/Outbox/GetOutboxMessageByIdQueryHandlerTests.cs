using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.Queries;
using LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.TestSupport;
using LastHour.BuildingBlocks.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Persistence.Outbox;

public class GetOutboxMessageByIdQueryHandlerTests
{
    private static DbContextOptions<LastHourDbContext> CreateOptions()
        => new DbContextOptionsBuilder<LastHourDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    [Fact]
    public async Task Handle_ExistingMessage_ReturnsDetailDtoWithDiagnostics()
    {
        var options = CreateOptions();
        DateTime now = new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc);
        var clock = new FixedClock { UtcNow = now };
        Guid claimToken = Guid.NewGuid();
        Guid messageId;

        using (var context = new LastHourDbContext(options))
        {
            var message = OutboxMessage.Create(new TestMessage("detail-test"), now.AddMinutes(-10));
            message.Claim(claimToken, now.AddMinutes(5));
            context.OutboxMessages.Add(message);
            await context.SaveChangesAsync();
            messageId = message.Id;
        }

        using (var context = new LastHourDbContext(options))
        {
            var handler = new GetOutboxMessageByIdQueryHandler(context, clock);
            Result<OutboxMessageDetailDto> result = await handler.Handle(
                new GetOutboxMessageByIdQuery(messageId),
                CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Id.Should().Be(messageId);
            result.Value.EventType.Should().Contain("TestMessage");
            result.Value.Status.Should().Be(OutboxMessageStatus.Processing);
            result.Value.OccurredOnUtc.Should().Be(now.AddMinutes(-10));
            result.Value.ProcessedOnUtc.Should().BeNull();
            result.Value.RetryCount.Should().Be(0);
            result.Value.ClaimToken.Should().Be(claimToken);
            result.Value.LockExpiresAtUtc.Should().Be(now.AddMinutes(5));
            result.Value.Error.Should().BeNull();
        }
    }

    [Fact]
    public async Task Handle_NonExistentMessage_ReturnsNotFoundError()
    {
        var options = CreateOptions();
        var clock = new FixedClock { UtcNow = DateTime.UtcNow };

        using var context = new LastHourDbContext(options);
        var handler = new GetOutboxMessageByIdQueryHandler(context, clock);
        Guid missingId = Guid.NewGuid();

        Result<OutboxMessageDetailDto> result = await handler.Handle(
            new GetOutboxMessageByIdQuery(missingId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.FirstError!.Type.Should().Be(ErrorType.NotFound);
        result.FirstError.Code.Should().Be("Outbox.MessageNotFound");
    }

    [Fact]
    public void OutboxMessageDetailDto_DoesNotContainContentProperty()
    {
        var property = typeof(OutboxMessageDetailDto).GetProperty("Content");
        property.Should().BeNull();
    }
}
