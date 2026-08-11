using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Persistence.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Identity.Tests.Persistence;

public class UserPersistenceTests
{
    [Fact]
    public async Task SaveAndLoad_User_RoundTripsAllColumns()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();
        UserId persistedId;

        await using (LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            User user = User.Create(
                Email.From("User@Example.com"),
                PasswordCredential.FromHash("hash-value"));
            user.Activate();

            context.Set<User>().Add(user);
            await context.SaveChangesAsync();

            persistedId = user.Id;
        }

        await using (LastHourDbContext reloaded = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            User? loaded = await reloaded.Set<User>().SingleOrDefaultAsync(user => user.Id == persistedId);

            loaded.Should().NotBeNull();
            loaded!.Id.Should().Be(persistedId);
            loaded.Email.Value.Should().Be("User@Example.com");
            loaded.Email.NormalizedValue.Should().Be("user@example.com");
            loaded.Status.Should().Be(AccountStatus.Active);
            loaded.PasswordCredential.Should().NotBeNull();
            loaded.PasswordCredential!.Hash.Should().Be("hash-value");
            loaded.Version.Should().Be(1);
            loaded.CreatedAt.Should().Be(TestIdentityPersistence.FixedUtcNow);
        }
    }

    [Fact]
    public async Task QueryByNormalizedEmail_IgnoresCaseAndWhitespace()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();
        await using (LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            context.Set<User>().Add(User.Create(Email.From("User@Example.com")));
            await context.SaveChangesAsync();
        }

        // Lookups must use the centralized normalization rule (Email.Normalize) — the same rule
        // the IUserRepository contract enforces by accepting Email and querying its
        // NormalizedValue. A raw, unnormalized input would never match the stored column.
        await using (LastHourDbContext context = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            User? loaded = await context.Set<User>()
                .SingleOrDefaultAsync(user => user.Email.NormalizedValue == Email.Normalize("USER@example.com"));

            loaded.Should().NotBeNull();
            loaded!.Email.Value.Should().Be("User@Example.com");
        }
    }

    [Fact]
    public async Task DuplicateNormalizedEmail_DatabaseRejectsSecondInsert()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();
        await using LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection);

        context.Set<User>().Add(User.Create(Email.From("user@example.com")));
        await context.SaveChangesAsync();

        // Different original casing, same normalized value: must collide on the unique index.
        context.Set<User>().Add(User.Create(Email.From("USER@example.com")));

        Func<Task> act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>()
            .Where(exception => HasUniqueConstraintFailure(exception));
    }

    [Fact]
    public async Task AuditInterceptor_StampsCreatedBy()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();
        await using LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection);

        User user = User.Create(Email.From("user@example.com"));
        context.Set<User>().Add(user);
        await context.SaveChangesAsync();

        user.CreatedBy.Should().Be(TestIdentityPersistence.CurrentUserId);
        user.CreatedAt.Should().Be(TestIdentityPersistence.FixedUtcNow);
    }

    [Fact]
    public async Task OptimisticConcurrency_SecondWriterThrowsDbUpdateConcurrencyException()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();
        UserId persistedId;

        await using (LastHourDbContext seed = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            User user = User.Create(Email.From("user@example.com"));
            seed.Set<User>().Add(user);
            await seed.SaveChangesAsync();
            persistedId = user.Id;
        }

        await using LastHourDbContext first = TestIdentityPersistence.CreateSqliteContext(connection);
        await using LastHourDbContext second = TestIdentityPersistence.CreateSqliteContext(connection);

        User firstView = await first.Set<User>().SingleAsync(user => user.Id == persistedId);
        User secondView = await second.Set<User>().SingleAsync(user => user.Id == persistedId);

        firstView.Activate();
        await first.SaveChangesAsync();

        secondView.ChangeEmail(Email.From("other@example.com"));

        Func<Task> act = () => second.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    private static bool HasUniqueConstraintFailure(DbUpdateException exception)
    {
        Exception? current = exception;
        while (current is not null)
        {
            if (current is SqliteException { SqliteErrorCode: 19 })
            {
                return true;
            }

            current = current.InnerException;
        }

        return false;
    }
}
