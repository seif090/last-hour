using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.Extensions.DependencyInjection;

namespace LastHour.Api.Tests.Support;

/// <summary>
/// Seeds users directly into the test database through the domain aggregate. Registration
/// always creates accounts in the pending state, so the login tests need a way to establish
/// active (and other-status) accounts without an activation endpoint; the seeder exercises the
/// same aggregate invariants the application would.
/// </summary>
public static class UserSeeder
{
    /// <summary>
    /// Creates a user with the given status and a real hash of the given password.
    /// </summary>
    /// <param name="factory">The API factory whose database is seeded.</param>
    /// <param name="email">The account email address.</param>
    /// <param name="password">The password whose hash is stored.</param>
    /// <param name="status">The account status to establish.</param>
    /// <returns>The identifier of the created user.</returns>
    public static async Task<Guid> SeedUserAsync(
        SqliteApiFactory factory,
        string email,
        string password,
        AccountStatus status = AccountStatus.Active)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        LastHourDbContext context = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
        IPasswordHasher hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        User user = User.Create(
            Email.From(email),
            PasswordCredential.FromHash(hasher.Hash(password)));

        switch (status)
        {
            case AccountStatus.Active:
                user.Activate();
                break;
            case AccountStatus.Suspended:
                user.Activate();
                user.Suspend();
                break;
            case AccountStatus.Disabled:
                user.Activate();
                user.Disable();
                break;
        }

        context.Set<User>().Add(user);
        await context.SaveChangesAsync();
        return user.Id.Value;
    }
}
