using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.Modules.Identity.Application.RefreshTokens;
using LastHour.Modules.Identity.Domain.RefreshTokens;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.Extensions.DependencyInjection;

namespace LastHour.Api.Tests.Support;

/// <summary>
/// Seeds refresh-token records directly into the test database through the domain aggregate,
/// returning the raw secret the test can present to the API. The hash is computed by the real
/// module service, so the seeded records are indistinguishable from login-issued ones.
/// </summary>
public static class RefreshTokenSeeder
{
    /// <summary>
    /// Creates an active refresh token for a user and returns its raw secret.
    /// </summary>
    /// <param name="factory">The API factory whose database is seeded.</param>
    /// <param name="userId">The identifier of the token's user.</param>
    /// <param name="familyId">The session family identifier; a fresh one when not provided.</param>
    /// <param name="lifetime">The token lifetime; defaults to the configured value.</param>
    /// <returns>The raw refresh token the test can submit.</returns>
    public static async Task<string> SeedRefreshTokenAsync(
        SqliteApiFactory factory,
        Guid userId,
        Guid? familyId = null,
        TimeSpan? lifetime = null)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        LastHourDbContext context = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
        IRefreshTokenService service = scope.ServiceProvider.GetRequiredService<IRefreshTokenService>();

        RefreshTokenSecret secret = service.Generate();
        var token = RefreshToken.Create(
            UserId.Create(userId),
            familyId ?? Guid.NewGuid(),
            secret.TokenHash,
            lifetime ?? TimeSpan.FromDays(14),
            DateTime.UtcNow);

        context.Set<RefreshToken>().Add(token);
        await context.SaveChangesAsync();
        return secret.RawToken;
    }
}
