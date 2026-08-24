using FluentAssertions;
using LastHour.Modules.Notifications.Domain.Notifications;

namespace LastHour.Modules.Notifications.Tests.Domain;

public class NotificationDomainTests
{
    private static readonly DateTime TestTime = new(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidParameters_InitializesUnreadNotification()
    {
        Guid eventId = Guid.NewGuid();
        Guid recipientId = Guid.NewGuid();

        Notification notification = Notification.Create(
            eventId,
            recipientId,
            "+12065550100",
            "Welcome",
            "Welcome to LastHour",
            "UserRegistered",
            TestTime);

        notification.Id.Should().NotBeEmpty();
        notification.EventId.Should().Be(eventId);
        notification.RecipientId.Should().Be(recipientId);
        notification.Recipient.Should().Be("+12065550100");
        notification.Title.Should().Be("Welcome");
        notification.Body.Should().Be("Welcome to LastHour");
        notification.Type.Should().Be("UserRegistered");
        notification.CreatedAtUtc.Should().Be(TestTime);
        notification.IsSent.Should().BeFalse();
        notification.SentAtUtc.Should().BeNull();
        notification.IsRead.Should().BeFalse();
        notification.ReadAtUtc.Should().BeNull();
    }

    [Fact]
    public void MarkAsSent_SetsSentAtUtcAndIsSent()
    {
        Notification notification = Notification.Create(
            null,
            Guid.NewGuid(),
            "user@example.com",
            "Title",
            "Body",
            "Type",
            TestTime);

        DateTime sentAt = TestTime.AddMinutes(1);
        notification.MarkAsSent(sentAt);

        notification.IsSent.Should().BeTrue();
        notification.SentAtUtc.Should().Be(sentAt);
    }

    [Fact]
    public void MarkAsRead_WhenUnread_TransitionsToReadAndSetsTimestamp()
    {
        Notification notification = Notification.Create(
            null,
            Guid.NewGuid(),
            "user@example.com",
            "Title",
            "Body",
            "Type",
            TestTime);

        DateTime readAt = TestTime.AddMinutes(5);
        notification.MarkAsRead(readAt);

        notification.IsRead.Should().BeTrue();
        notification.ReadAtUtc.Should().Be(readAt);
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_IsIdempotentAndDoesNotOverwriteReadAtUtc()
    {
        Notification notification = Notification.Create(
            null,
            Guid.NewGuid(),
            "user@example.com",
            "Title",
            "Body",
            "Type",
            TestTime);

        DateTime firstReadAt = TestTime.AddMinutes(5);
        notification.MarkAsRead(firstReadAt);

        DateTime secondReadAt = TestTime.AddMinutes(15);
        notification.MarkAsRead(secondReadAt);

        notification.IsRead.Should().BeTrue();
        notification.ReadAtUtc.Should().Be(firstReadAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithEmptyRecipient_ThrowsArgumentException(string recipient)
    {
        Action act = () => Notification.Create(
            null,
            Guid.NewGuid(),
            recipient,
            "Title",
            "Body",
            "Type",
            TestTime);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyRecipientId_ThrowsArgumentException()
    {
        Action act = () => Notification.Create(
            null,
            Guid.Empty,
            "user@example.com",
            "Title",
            "Body",
            "Type",
            TestTime);

        act.Should().Throw<ArgumentException>();
    }
}
