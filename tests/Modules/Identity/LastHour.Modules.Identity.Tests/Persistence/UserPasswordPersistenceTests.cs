using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.Modules.Identity.Domain.Users;
using LastHour.Modules.Identity.Tests.Persistence.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Identity.Tests.Persistence;

public class UserPasswordPersistenceTests
{
    [Fact]
    public async Task ChangePassword_PersistsNewHash_AcrossContextReload()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();
        UserId persistedId;

        await using (LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            User user = User.Create(
                Email.From("user@example.com"),
                PasswordCredential.FromHash("old-hash"));
            context.Set<User>().Add(user);
            await context.SaveChangesAsync();
            persistedId = user.Id;
        }

        await using (LastHourDbContext context = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            User? loaded = await context.Set<User>().SingleAsync(user => user.Id == persistedId);
            loaded.SetPasswordCredential(PasswordCredential.FromHash("new-hash"));
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext reloaded = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            User? final = await reloaded.Set<User>().SingleOrDefaultAsync(user => user.Id == persistedId);

            final.Should().NotBeNull();
            final!.PasswordCredential.Should().NotBeNull();
            final.PasswordCredential!.Hash.Should().Be("new-hash");
            // Create assigns the initial credential without a version bump (the aggregate is
            // new); the single SetPasswordCredential during the change is one mutation.
            final.Version.Should().Be(1, "SetPasswordCredential bumps the concurrency token once");
        }
    }

    [Fact]
    public async Task PasswordCredential_IsOpaqueAndNeverDerivesPlaintext()
    {
        using SqliteConnection connection = TestIdentityPersistence.CreateOpenConnection();
        UserId persistedId;

        await using (LastHourDbContext context = await TestIdentityPersistence.CreateContextAsync(connection))
        {
            User user = User.Create(
                Email.From("user@example.com"),
                PasswordCredential.FromHash("$argon2id$v=19$m=65536,t=3,p=4$c2FsdHNhbHRzYWx0c2FsdA$c2FsdHNhbHQ"));
            context.Set<User>().Add(user);
            await context.SaveChangesAsync();
            persistedId = user.Id;
        }

        await using (LastHourDbContext reloaded = TestIdentityPersistence.CreateSqliteContext(connection))
        {
            User? loaded = await reloaded.Set<User>().SingleOrDefaultAsync(user => user.Id == persistedId);

            // The persisted column holds the opaque hash only — the plaintext never exists in
            // the aggregate, the context or the database.
            loaded!.PasswordCredential!.Hash.Should().Be("$argon2id$v=19$m=65536,t=3,p=4$c2FsdHNhbHRzYWx0c2FsdA$c2FsdHNhbHQ");
            loaded.PasswordCredential.Hash.Should().NotContain("plaintext");
        }
    }
}
