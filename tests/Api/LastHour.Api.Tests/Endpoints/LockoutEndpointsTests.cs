using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LastHour.Api.Tests.Support;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.Modules.Identity.Domain.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LastHour.Api.Tests.Endpoints;

/// <summary>
/// Exercises the account lockout behavior over the full HTTP → pipeline → database path:
/// repeated failed logins lock the account (with the failure response staying uniform), the
/// lockout is visible only in persisted security state — never in the public payload — and
/// simultaneous failed attempts cannot lose increments or bypass the threshold.
/// </summary>
[Collection("ApiEndpoints")]
public class LockoutEndpointsTests
{
    private const string ValidPassword = "correct horse battery staple";

    private static string CreateConnectionString()
        => new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(Path.GetTempPath(), $"lasthour-test-{Guid.NewGuid():N}.db"),
        }.ToString();

    private static SqliteApiFactory CreateFactory()
    {
        var factory = new SqliteApiFactory(CreateConnectionString());
        factory.EnsureSchemaCreated();
        return factory;
    }

    private static async Task<User> LoadUserAsync(SqliteApiFactory factory, string email)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        LastHourDbContext context = scope.ServiceProvider.GetRequiredService<LastHourDbContext>();
        return (await context.Set<User>()
            .AsNoTracking()
            .SingleAsync(user => user.Email.NormalizedValue == Email.From(email).NormalizedValue))!;
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password)
        => await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });

    [Fact]
    public async Task Lockout_RepeatedFailures_LockAccountWithUniformResponses()
    {
        using SqliteApiFactory factory = CreateFactory();
        await UserSeeder.SeedUserAsync(factory, "user@example.com", ValidPassword);
        HttpClient client = factory.CreateClient();

        const int maxFailedAttempts = 5;
        for (int attempt = 0; attempt < maxFailedAttempts; attempt++)
        {
            HttpResponseMessage response = await LoginAsync(client, "user@example.com", "wrong-password-value");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            document.RootElement.GetProperty("code").GetString().Should().Be("AuthenticationFailed");
        }

        // Even the correct password is rejected while locked, with the identical public payload.
        HttpResponseMessage lockedResponse = await LoginAsync(client, "user@example.com", ValidPassword);
        lockedResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        using JsonDocument lockedDocument = JsonDocument.Parse(await lockedResponse.Content.ReadAsStringAsync());
        lockedDocument.RootElement.GetProperty("code").GetString().Should().Be("AuthenticationFailed");

        // The lockout exists only in persisted security state — never in the response.
        User user = await LoadUserAsync(factory, "user@example.com");
        user.FailedAccessCount.Should().Be(maxFailedAttempts);
        user.IsLockedOut(user.LockoutUntilUtc!.Value.AddMinutes(-1)).Should().BeTrue();
    }

    [Fact]
    public async Task Lockout_ConcurrentFailures_EveryIncrementLandsAndThresholdHolds()
    {
        using SqliteApiFactory factory = CreateFactory();
        await UserSeeder.SeedUserAsync(factory, "user@example.com", ValidPassword);
        HttpClient client = factory.CreateClient();

        // Five simultaneous wrong-password attempts: the tracked retry loop ensures every
        // increment lands, so the final count reaches the threshold exactly — no lost updates,
        // no bypass, and all responses stay uniform 401s.
        Task<HttpResponseMessage>[] attempts = Enumerable.Range(0, 5)
            .Select(_ => LoginAsync(client, "user@example.com", "wrong-password-value"))
            .ToArray();
        HttpResponseMessage[] responses = await Task.WhenAll(attempts);

        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.Unauthorized);

        User user = await LoadUserAsync(factory, "user@example.com");
        user.FailedAccessCount.Should().Be(5, "no concurrent increment may be lost");
        user.IsLockedOut(user.LockoutUntilUtc!.Value.AddMinutes(-1)).Should().BeTrue();
    }
}
