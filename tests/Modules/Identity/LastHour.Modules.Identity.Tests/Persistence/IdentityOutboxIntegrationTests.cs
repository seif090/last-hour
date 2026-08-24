using System.Text.Json;
using FluentAssertions;
using LastHour.BuildingBlocks.Contracts.Events;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Persistence.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Identity.Tests.Persistence;

public class IdentityOutboxIntegrationTests
{
    private static readonly DateTime FixedNow = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task UserRegistration_PersistsOutboxMessage_WithContractSafePayload()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();
        UserId userId;

        await using (LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            var user = User.Create(
                Email.From("newuser@example.com"),
                PasswordCredential.FromHash("sensitive-argon2-hash-value"));

            userId = user.Id;
            context.Set<User>().Add(user);
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            List<OutboxMessage> messages = await context.Set<OutboxMessage>().ToListAsync();
            messages.Should().ContainSingle();

            OutboxMessage outboxMessage = messages.Single();
            outboxMessage.Type.Should().Be(typeof(UserRegisteredDomainEvent).AssemblyQualifiedName);
            outboxMessage.ProcessedOnUtc.Should().BeNull();

            // Verify payload does not leak password or credential hash
            outboxMessage.Content.Should().NotContain("sensitive-argon2-hash-value");
            outboxMessage.Content.Should().NotContain("PasswordCredential");
            outboxMessage.Content.Should().NotContain("Hash");

            // Verify payload deserializes to UserRegisteredDomainEvent
            var domainEvent = JsonSerializer.Deserialize<UserRegisteredDomainEvent>(outboxMessage.Content, OutboxJson.SerializerOptions);
            domainEvent.Should().NotBeNull();
            domainEvent!.EventId.Should().NotBeEmpty();
            domainEvent.UserId.Should().Be(userId.Value);
            domainEvent.Email.Should().Be("newuser@example.com");
        }
    }

    [Fact]
    public async Task UserPasswordChanged_PersistsOutboxMessage_WithNoCredentialData()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();
        UserId userId;

        await using (LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            var user = User.Create(
                Email.From("existing@example.com"),
                PasswordCredential.FromHash("original-hash"));
            userId = user.Id;
            context.Set<User>().Add(user);
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            User user = await context.Set<User>().SingleAsync(u => u.Id == userId);
            user.SetPasswordCredential(PasswordCredential.FromHash("new-secret-hash"));
            user.MarkPasswordChanged(FixedNow);

            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            List<OutboxMessage> messages = await context.Set<OutboxMessage>().ToListAsync();
            messages.Should().HaveCount(2); // 1: UserRegistered, 2: UserPasswordChanged

            OutboxMessage passwordChangedOutbox = messages.Single(m => m.Type == typeof(UserPasswordChangedDomainEvent).AssemblyQualifiedName);
            passwordChangedOutbox.Content.Should().NotContain("original-hash");
            passwordChangedOutbox.Content.Should().NotContain("new-secret-hash");
            passwordChangedOutbox.Content.Should().NotContain("Password");

            var domainEvent = JsonSerializer.Deserialize<UserPasswordChangedDomainEvent>(passwordChangedOutbox.Content, OutboxJson.SerializerOptions);
            domainEvent.Should().NotBeNull();
            domainEvent!.EventId.Should().NotBeEmpty();
            domainEvent.UserId.Should().Be(userId.Value);
        }
    }

    [Fact]
    public async Task UserLockedOut_ReachingThreshold_PersistsOutboxMessage()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();
        UserId userId;

        await using (LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            var user = User.Create(Email.From("target@example.com"));
            userId = user.Id;
            context.Set<User>().Add(user);
            await context.SaveChangesAsync();
        }

        TimeSpan lockoutDuration = TimeSpan.FromMinutes(15);

        await using (LastHourDbContext context = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            User user = await context.Set<User>().SingleAsync(u => u.Id == userId);
            for (int i = 0; i < 5; i++)
            {
                user.RecordFailedLogin(FixedNow, 5, lockoutDuration);
            }

            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            List<OutboxMessage> messages = await context.Set<OutboxMessage>().ToListAsync();
            messages.Should().HaveCount(2); // 1: UserRegistered, 2: UserLockedOut

            OutboxMessage lockedOutOutbox = messages.Single(m => m.Type == typeof(UserLockedOutDomainEvent).AssemblyQualifiedName);
            var domainEvent = JsonSerializer.Deserialize<UserLockedOutDomainEvent>(lockedOutOutbox.Content, OutboxJson.SerializerOptions);
            domainEvent.Should().NotBeNull();
            domainEvent!.EventId.Should().NotBeEmpty();
            domainEvent.UserId.Should().Be(userId.Value);
            domainEvent.LockoutUntilUtc.Should().Be(FixedNow.Add(lockoutDuration));
        }
    }

    [Fact]
    public async Task UserFailedLogin_BelowThreshold_DoesNotPersistLockoutOutboxMessage()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();
        UserId userId;

        await using (LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            var user = User.Create(Email.From("target2@example.com"));
            userId = user.Id;
            context.Set<User>().Add(user);
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            User user = await context.Set<User>().SingleAsync(u => u.Id == userId);
            user.RecordFailedLogin(FixedNow, 5, TimeSpan.FromMinutes(15)); // Only 1 failure, threshold is 5

            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            List<OutboxMessage> messages = await context.Set<OutboxMessage>().ToListAsync();
            // Should only have the initial registration message, not lockout
            messages.Should().ContainSingle();
            messages.Single().Type.Should().Be(typeof(UserRegisteredDomainEvent).AssemblyQualifiedName);
        }
    }
}
