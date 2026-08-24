using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LastHour.Api.Tests.Support;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Identity.Application.Permissions;
using LastHour.Modules.Merchant.Domain.Merchants;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Api.Tests.Endpoints;

/// <summary>
/// Exercises the merchant endpoints end to end over the full HTTP → pipeline → database path:
/// registration, get by identifier, search and pagination, profile updates, full lifecycle
/// state transitions (verify, suspend, activate, close), permission-based authorization,
/// and ownership-aware authorization with authenticated current-user identity binding.
/// All tests execute against isolated per-test SQLite databases.
/// </summary>
[Collection("ApiEndpoints")]
public class MerchantEndpointsTests
{
    private const string ValidPassword = "correct horse battery staple";

    private sealed record LoginResponse(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn);

    private sealed record RegisterRequest(
        string Name,
        string Street,
        string City,
        string? StateOrProvince,
        string? PostalCode,
        string Country,
        string Phone);

    private sealed record CreatedResponse(Guid MerchantId, string Name);

    private sealed record UpdateRequest(
        string Name,
        string Street,
        string City,
        string? StateOrProvince,
        string? PostalCode,
        string Country,
        string Phone);

    private sealed record MerchantDto(
        Guid Id,
        Guid OwnerId,
        string Name,
        string Street,
        string City,
        string? StateOrProvince,
        string? PostalCode,
        string Country,
        string Phone,
        string Status);

    private sealed record PagedResult<T>(
        IReadOnlyList<T> Items,
        int PageNumber,
        int PageSize,
        int TotalCount,
        int TotalPages);

    private static string CreateConnectionString()
        => new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(Path.GetTempPath(), $"lasthour-merchant-test-{Guid.NewGuid():N}.db"),
        }.ToString();

    private static async Task<SqliteApiFactory> CreateFactoryAsync()
    {
        var factory = new SqliteApiFactory(CreateConnectionString());
        factory.EnsureSchemaCreated();
        await RoleSeeder.SeedAuthorizationAsync(factory);
        return factory;
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email, password });
        LoginResponse? payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return payload!.AccessToken;
    }

    private static async Task<Guid> SeedAdminAsync(SqliteApiFactory factory)
    {
        Guid adminId = await UserSeeder.SeedUserAsync(factory, "admin@merchant-test.com", ValidPassword);
        await RoleSeeder.AssignRoleAsync(factory, adminId, BootstrapRoles.Administrator);
        return adminId;
    }

    private static async Task<Guid> SeedRegularUserAsync(SqliteApiFactory factory, string email = "user@merchant-test.com")
        => await UserSeeder.SeedUserAsync(factory, email, ValidPassword);

    private static async Task<Guid> SeedMerchantAsync(
        SqliteApiFactory factory,
        string name = "Seeded Merchant",
        string city = "Seattle",
        Guid? ownerId = null)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        IMerchantRepository repository = scope.ServiceProvider.GetRequiredService<IMerchantRepository>();
        IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        MerchantAggregate merchant = MerchantAggregate.Register(
            ownerId ?? Guid.NewGuid(),
            MerchantName.From(name),
            Address.Create("123 Seeded St", city, "WA", "98101", "USA"),
            PhoneNumber.From("+1-206-555-0100"));

        repository.Add(merchant);
        await unitOfWork.SaveChangesAsync();
        return merchant.Id.Value;
    }

    private sealed class AdminContext : IDisposable
    {
        public AdminContext(SqliteApiFactory factory, HttpClient client, Guid adminId)
        {
            Factory = factory;
            Client = client;
            AdminId = adminId;
        }

        public SqliteApiFactory Factory { get; }

        public HttpClient Client { get; }

        public Guid AdminId { get; }

        public void Dispose()
        {
            Client.Dispose();
            Factory.Dispose();
        }
    }

    private static async Task<AdminContext> CreateAdminContextAsync()
    {
        SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        Guid adminId = await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@merchant-test.com", ValidPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return new AdminContext(factory, client, adminId);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string? token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private static RegisterRequest CreateValidRegisterRequest(string name = "Fresh Bakery", string city = "Seattle")
        => new(
            name,
            "123 Baker Street",
            city,
            "WA",
            "98101",
            "United States",
            "+1-206-555-0100");

    private static async Task<Guid> RegisterMerchantAsync(HttpClient client, RegisterRequest? request = null)
    {
        request ??= CreateValidRegisterRequest();
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/merchants", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        CreatedResponse? created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        return created!.MerchantId;
    }

    // ==================================================================
    // REGISTRATION TESTS
    // ==================================================================

    [Fact]
    public async Task Register_ValidRequest_ReturnsCreatedAndCanBeRetrieved()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        RegisterRequest request = CreateValidRegisterRequest("Artisan Coffee", "Portland");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/merchants", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        CreatedResponse? created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();
        created!.MerchantId.Should().NotBeEmpty();
        created.Name.Should().Be(request.Name);

        HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/merchants/{created.MerchantId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        MerchantDto? merchant = await getResponse.Content.ReadFromJsonAsync<MerchantDto>();
        merchant.Should().NotBeNull();
        merchant!.Id.Should().Be(created.MerchantId);
        merchant.OwnerId.Should().Be(context.AdminId);
        merchant.Name.Should().Be(request.Name);
        merchant.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Register_MissingName_ReturnsBadRequestWithValidationErrors()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        var request = new RegisterRequest(
            string.Empty,
            "123 Main St",
            "Seattle",
            null,
            null,
            "USA",
            "+1-206-555-0100");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/merchants", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("ValidationFailed");
        document.RootElement.GetProperty("errors").EnumerateObject().Should().Contain(p => p.Name.Contains("Name") || p.Name.Contains("MerchantName"));
    }

    [Fact]
    public async Task Register_MissingRequiredAddressFields_ReturnsBadRequestWithValidationErrors()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        var request = new RegisterRequest(
            "Valid Name",
            string.Empty,
            string.Empty,
            null,
            null,
            string.Empty,
            "+1-206-555-0100");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/merchants", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("ValidationFailed");
        document.RootElement.GetProperty("errors").EnumerateObject().Should().Contain(p => p.Name.Contains("Street"));
        document.RootElement.GetProperty("errors").EnumerateObject().Should().Contain(p => p.Name.Contains("City"));
        document.RootElement.GetProperty("errors").EnumerateObject().Should().Contain(p => p.Name.Contains("Country"));
    }

    [Fact]
    public async Task Register_InvalidPhone_ReturnsBadRequestWithValidationErrors()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        var request = new RegisterRequest(
            "Valid Name",
            "123 Main St",
            "Seattle",
            null,
            null,
            "USA",
            string.Empty);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/merchants", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("ValidationFailed");
        document.RootElement.GetProperty("errors").EnumerateObject().Should().Contain(p => p.Name.Contains("Phone"));
    }

    [Fact]
    public async Task Register_DuplicateMerchantName_ReturnsConflict()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        RegisterRequest request1 = CreateValidRegisterRequest("Unique Bakery");
        RegisterRequest request2 = CreateValidRegisterRequest("Unique Bakery");

        HttpResponseMessage response1 = await client.PostAsJsonAsync("/api/v1/merchants", request1);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);

        HttpResponseMessage response2 = await client.PostAsJsonAsync("/api/v1/merchants", request2);
        response2.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using JsonDocument document = JsonDocument.Parse(await response2.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Merchants.NameAlreadyExists");
        document.RootElement.GetProperty("status").GetInt32().Should().Be(409);
    }

    // ==================================================================
    // GET MERCHANT BY ID TESTS
    // ==================================================================

    [Fact]
    public async Task GetById_ExistingMerchant_ReturnsOkWithAllFields()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        RegisterRequest request = CreateValidRegisterRequest("Gourmet Deli", "San Francisco");
        Guid merchantId = await RegisterMerchantAsync(client, request);

        HttpResponseMessage response = await client.GetAsync($"/api/v1/merchants/{merchantId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        MerchantDto? merchant = await response.Content.ReadFromJsonAsync<MerchantDto>();
        merchant.Should().NotBeNull();
        merchant!.Id.Should().Be(merchantId);
        merchant.OwnerId.Should().Be(context.AdminId);
        merchant.Name.Should().Be(request.Name);
        merchant.Street.Should().Be(request.Street);
        merchant.City.Should().Be(request.City);
        merchant.StateOrProvince.Should().Be(request.StateOrProvince);
        merchant.PostalCode.Should().Be(request.PostalCode);
        merchant.Country.Should().Be(request.Country);
        merchant.Phone.Should().Be(request.Phone);
        merchant.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetById_MissingMerchant_ReturnsNotFoundWithProblemDetails()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        Guid nonExistentId = Guid.NewGuid();

        HttpResponseMessage response = await client.GetAsync($"/api/v1/merchants/{nonExistentId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Merchants.NotFound");
        document.RootElement.GetProperty("status").GetInt32().Should().Be(404);
    }

    // ==================================================================
    // SEARCH MERCHANTS TESTS
    // ==================================================================

    [Fact]
    public async Task Search_BasicPagination_ReturnsCorrectPageAndTotals()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Alpha Market", "Seattle"));
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Beta Market", "Seattle"));
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Gamma Market", "Seattle"));

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?pageNumber=1&pageSize=2");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<MerchantDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<MerchantDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().HaveCount(2);
        paged.PageNumber.Should().Be(1);
        paged.PageSize.Should().Be(2);
        paged.TotalCount.Should().Be(3);
        paged.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Search_NameSearch_ReturnsMatchingMerchants()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Seattle Coffee Works", "Seattle"));
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Portland Tea Lounge", "Portland"));

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?searchTerm=Coffee");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<MerchantDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<MerchantDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().ContainSingle();
        paged.Items[0].Name.Should().Be("Seattle Coffee Works");
    }

    [Fact]
    public async Task Search_CitySearch_ReturnsMatchingMerchants()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Store A", "Tacoma"));
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Store B", "Spokane"));

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?searchTerm=Tacoma");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<MerchantDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<MerchantDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().ContainSingle();
        paged.Items[0].City.Should().Be("Tacoma");
    }

    [Fact]
    public async Task Search_StatusFilter_ReturnsMatchingStatus()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        Guid id1 = await RegisterMerchantAsync(client, CreateValidRegisterRequest("Pending Merchant"));
        Guid id2 = await RegisterMerchantAsync(client, CreateValidRegisterRequest("Active Merchant"));
        await client.PostAsync($"/api/v1/merchants/{id2}/verify", null);

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?status=Active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<MerchantDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<MerchantDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().ContainSingle();
        paged.Items[0].Id.Should().Be(id2);
        paged.Items[0].Status.Should().Be("Active");
    }

    [Fact]
    public async Task Search_SortingByName_AscAndDesc_ReturnsCorrectOrder()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        await RegisterMerchantAsync(client, CreateValidRegisterRequest("BBB"));
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("AAA"));
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("CCC"));

        HttpResponseMessage ascResponse = await client.GetAsync("/api/v1/merchants?sortBy=Name&sortDirection=Asc");
        ascResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<MerchantDto>? ascPaged = await responseToPagedListAsync(ascResponse);
        ascPaged!.Items.Select(m => m.Name).Should().ContainInOrder("AAA", "BBB", "CCC");

        HttpResponseMessage descResponse = await client.GetAsync("/api/v1/merchants?sortBy=Name&sortDirection=Desc");
        descResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<MerchantDto>? descPaged = await responseToPagedListAsync(descResponse);
        descPaged!.Items.Select(m => m.Name).Should().ContainInOrder("CCC", "BBB", "AAA");
    }

    private static async Task<PagedResult<MerchantDto>?> responseToPagedListAsync(HttpResponseMessage response)
        => await response.Content.ReadFromJsonAsync<PagedResult<MerchantDto>>();

    [Fact]
    public async Task Search_SortingByCreatedAt_ReturnsCorrectOrder()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        await RegisterMerchantAsync(client, CreateValidRegisterRequest("First Merchant"));
        await Task.Delay(10);
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Second Merchant"));

        HttpResponseMessage descResponse = await client.GetAsync("/api/v1/merchants?sortBy=CreatedAt&sortDirection=Desc");
        descResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<MerchantDto>? descPaged = await descResponse.Content.ReadFromJsonAsync<PagedResult<MerchantDto>>();
        descPaged!.Items.Select(m => m.Name).Should().ContainInOrder("Second Merchant", "First Merchant");
    }

    [Fact]
    public async Task Search_SortingByStatus_ReturnsCorrectOrder()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        Guid id1 = await RegisterMerchantAsync(client, CreateValidRegisterRequest("Merchant 1"));
        Guid id2 = await RegisterMerchantAsync(client, CreateValidRegisterRequest("Merchant 2"));
        await client.PostAsync($"/api/v1/merchants/{id2}/verify", null);

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?sortBy=Status&sortDirection=Asc");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<MerchantDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<MerchantDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Search_InvalidPagination_PageNumberZero_ReturnsBadRequest()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?pageNumber=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("ValidationFailed");
    }

    [Fact]
    public async Task Search_InvalidPagination_PageSizeTooLarge_ReturnsBadRequest()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?pageSize=101");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("ValidationFailed");
    }

    [Fact]
    public async Task Search_PageBeyondAvailableData_ReturnsEmptyItemsWithCorrectTotals()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Single Merchant"));

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?pageNumber=99&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<MerchantDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<MerchantDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().BeEmpty();
        paged.TotalCount.Should().Be(1);
        paged.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Search_DefaultPagination_ReturnsPagedResult()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Default Paging Store"));

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<MerchantDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<MerchantDto>>();
        paged.Should().NotBeNull();
        paged!.PageNumber.Should().Be(1);
        paged.PageSize.Should().Be(20);
        paged.TotalCount.Should().Be(1);
        paged.TotalPages.Should().Be(1);
        paged.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Search_PageTwo_ReturnsCorrectSlice()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Store 1"));
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Store 2"));
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Store 3"));

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?pageNumber=2&pageSize=2&sortBy=Name&sortDirection=Asc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<MerchantDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<MerchantDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().ContainSingle();
        paged.Items[0].Name.Should().Be("Store 3");
        paged.PageNumber.Should().Be(2);
        paged.PageSize.Should().Be(2);
        paged.TotalCount.Should().Be(3);
        paged.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task Search_PageSize_ReturnsCorrectNumberOfItems()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Item 1"));
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Item 2"));
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Item 3"));

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<MerchantDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<MerchantDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().ContainSingle();
        paged.PageSize.Should().Be(1);
        paged.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Search_SearchWithNoMatches_ReturnsEmptyPagedResult()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Existing Store"));

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?searchTerm=NonExistentStoreQuery");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<MerchantDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<MerchantDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().BeEmpty();
        paged.TotalCount.Should().Be(0);
        paged.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Search_InvalidStatus_ReturnsBadRequest()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?status=NotAValidStatus");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_InvalidPagination_PageSizeZero_ReturnsBadRequest()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?pageSize=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("ValidationFailed");
    }

    [Fact]
    public async Task Search_InvalidSortField_ReturnsBadRequest()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?sortBy=InvalidSortField");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_InvalidSortDirection_ReturnsBadRequest()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?sortDirection=InvalidDirection");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_SearchAliases_SearchTakesPrecedenceOverSearchTerm()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Alpha Target Store"));
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Beta Alternative Store"));

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?search=Alpha&searchTerm=Beta");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<MerchantDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<MerchantDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().ContainSingle();
        paged.Items[0].Name.Should().Be("Alpha Target Store");
    }

    [Fact]
    public async Task Search_SearchAliases_SearchParameterWorksIndependently()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Unique Alias Query Store"));

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?search=Unique%20Alias");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<MerchantDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<MerchantDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().ContainSingle();
        paged.Items[0].Name.Should().Be("Unique Alias Query Store");
    }

    [Fact]
    public async Task Search_PaginationAliases_PageTakesPrecedenceOverPageNumber()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Merchant A"));
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Merchant B"));
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Merchant C"));

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?page=2&pageNumber=1&pageSize=2&sortBy=Name&sortDirection=Asc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<MerchantDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<MerchantDto>>();
        paged.Should().NotBeNull();
        paged!.PageNumber.Should().Be(2);
        paged.Items.Should().ContainSingle();
        paged.Items[0].Name.Should().Be("Merchant C");
    }

    [Fact]
    public async Task Search_PaginationAliases_PageParameterWorksIndependently()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Merchant A"));
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Merchant B"));
        await RegisterMerchantAsync(client, CreateValidRegisterRequest("Merchant C"));

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants?page=2&pageSize=2&sortBy=Name&sortDirection=Asc");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<MerchantDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<MerchantDto>>();
        paged.Should().NotBeNull();
        paged!.PageNumber.Should().Be(2);
        paged.Items.Should().ContainSingle();
        paged.Items[0].Name.Should().Be("Merchant C");
    }

    // ==================================================================
    // UPDATE MERCHANT PROFILE TESTS
    // ==================================================================

    [Fact]
    public async Task UpdateProfile_ValidRequest_ReturnsNoContentAndUpdatesDetails()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        Guid merchantId = await RegisterMerchantAsync(client, CreateValidRegisterRequest("Original Name", "Seattle"));

        var updateRequest = new UpdateRequest(
            "Updated Name",
            "456 New Street",
            "Bellevue",
            "WA",
            "98004",
            "USA",
            "+1-425-555-0199");

        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/merchants/{merchantId}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/merchants/{merchantId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        MerchantDto? updated = await getResponse.Content.ReadFromJsonAsync<MerchantDto>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Name");
        updated.Street.Should().Be("456 New Street");
        updated.City.Should().Be("Bellevue");
        updated.PostalCode.Should().Be("98004");
        updated.Phone.Should().Be("+1-425-555-0199");
    }

    [Fact]
    public async Task UpdateProfile_MissingMerchant_ReturnsNotFound()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        var updateRequest = new UpdateRequest(
            "Updated Name",
            "456 New Street",
            "Bellevue",
            "WA",
            "98004",
            "USA",
            "+1-425-555-0199");

        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/merchants/{Guid.NewGuid()}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Merchants.NotFound");
    }

    [Fact]
    public async Task UpdateProfile_InvalidRequest_ReturnsBadRequest()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        Guid merchantId = await RegisterMerchantAsync(client, CreateValidRegisterRequest());

        var updateRequest = new UpdateRequest(
            string.Empty,
            string.Empty,
            "Bellevue",
            null,
            null,
            "USA",
            "+1-425-555-0199");

        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/merchants/{merchantId}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("ValidationFailed");
    }

    [Fact]
    public async Task UpdateProfile_DuplicateMerchantName_ReturnsConflict()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        Guid merchantIdA = await RegisterMerchantAsync(client, CreateValidRegisterRequest("Store Alpha Profile"));
        Guid merchantIdB = await RegisterMerchantAsync(client, CreateValidRegisterRequest("Store Beta Profile"));

        var updateRequest = new UpdateRequest(
            "Store Beta Profile",
            "456 New Street",
            "Bellevue",
            "WA",
            "98004",
            "USA",
            "+1-425-555-0199");

        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/merchants/{merchantIdA}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Merchants.NameAlreadyExists");
        document.RootElement.GetProperty("status").GetInt32().Should().Be(409);
    }

    [Fact]
    public async Task UpdateProfile_SameNameUnchanged_ReturnsNoContent()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        Guid merchantId = await RegisterMerchantAsync(client, CreateValidRegisterRequest("Unchanged Store Name"));

        var updateRequest = new UpdateRequest(
            "Unchanged Store Name",
            "789 Another Street",
            "Bellevue",
            "WA",
            "98004",
            "USA",
            "+1-425-555-0199");

        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/merchants/{merchantId}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ==================================================================
    // LIFECYCLE TESTS (VALID TRANSITIONS)
    // ==================================================================

    [Fact]
    public async Task Verify_PendingMerchant_ReturnsNoContentAndSetsActive()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        Guid merchantId = await RegisterMerchantAsync(client);

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{merchantId}/verify", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/merchants/{merchantId}");
        MerchantDto? merchant = await getResponse.Content.ReadFromJsonAsync<MerchantDto>();
        merchant!.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Suspend_ActiveMerchant_ReturnsNoContentAndSetsSuspended()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        Guid merchantId = await RegisterMerchantAsync(client);
        await client.PostAsync($"/api/v1/merchants/{merchantId}/verify", null);

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{merchantId}/suspend", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/merchants/{merchantId}");
        MerchantDto? merchant = await getResponse.Content.ReadFromJsonAsync<MerchantDto>();
        merchant!.Status.Should().Be("Suspended");
    }

    [Fact]
    public async Task Activate_SuspendedMerchant_ReturnsNoContentAndSetsActive()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        Guid merchantId = await RegisterMerchantAsync(client);
        await client.PostAsync($"/api/v1/merchants/{merchantId}/verify", null);
        await client.PostAsync($"/api/v1/merchants/{merchantId}/suspend", null);

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{merchantId}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/merchants/{merchantId}");
        MerchantDto? merchant = await getResponse.Content.ReadFromJsonAsync<MerchantDto>();
        merchant!.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Close_PendingMerchant_ReturnsNoContentAndSetsClosed()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        Guid merchantId = await RegisterMerchantAsync(client);

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{merchantId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/merchants/{merchantId}");
        MerchantDto? merchant = await getResponse.Content.ReadFromJsonAsync<MerchantDto>();
        merchant!.Status.Should().Be("Closed");
    }

    [Fact]
    public async Task Close_ActiveMerchant_ReturnsNoContentAndSetsClosed()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        Guid merchantId = await RegisterMerchantAsync(client);
        await client.PostAsync($"/api/v1/merchants/{merchantId}/verify", null);

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{merchantId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/merchants/{merchantId}");
        MerchantDto? merchant = await getResponse.Content.ReadFromJsonAsync<MerchantDto>();
        merchant!.Status.Should().Be("Closed");
    }

    [Fact]
    public async Task Close_SuspendedMerchant_ReturnsNoContentAndSetsClosed()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        Guid merchantId = await RegisterMerchantAsync(client);
        await client.PostAsync($"/api/v1/merchants/{merchantId}/verify", null);
        await client.PostAsync($"/api/v1/merchants/{merchantId}/suspend", null);

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{merchantId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await client.GetAsync($"/api/v1/merchants/{merchantId}");
        MerchantDto? merchant = await getResponse.Content.ReadFromJsonAsync<MerchantDto>();
        merchant!.Status.Should().Be("Closed");
    }

    // ==================================================================
    // INVALID LIFECYCLE TRANSITIONS
    // ==================================================================

    [Fact]
    public async Task Verify_WhenActive_ReturnsBadRequest()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        Guid merchantId = await RegisterMerchantAsync(client);
        await client.PostAsync($"/api/v1/merchants/{merchantId}/verify", null);

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{merchantId}/verify", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Merchants.InvalidStatusTransition");
    }

    [Fact]
    public async Task Suspend_WhenPending_ReturnsBadRequest()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        Guid merchantId = await RegisterMerchantAsync(client);

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{merchantId}/suspend", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Merchants.InvalidStatusTransition");
    }

    [Fact]
    public async Task Suspend_WhenSuspended_ReturnsBadRequest()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        Guid merchantId = await RegisterMerchantAsync(client);
        await client.PostAsync($"/api/v1/merchants/{merchantId}/verify", null);
        await client.PostAsync($"/api/v1/merchants/{merchantId}/suspend", null);

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{merchantId}/suspend", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Merchants.InvalidStatusTransition");
    }

    [Fact]
    public async Task Activate_WhenActive_ReturnsBadRequest()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        Guid merchantId = await RegisterMerchantAsync(client);
        await client.PostAsync($"/api/v1/merchants/{merchantId}/verify", null);

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{merchantId}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Merchants.InvalidStatusTransition");
    }

    [Fact]
    public async Task Close_WhenClosed_ReturnsBadRequest()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        Guid merchantId = await RegisterMerchantAsync(client);
        await client.PostAsync($"/api/v1/merchants/{merchantId}/close", null);

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{merchantId}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Merchants.InvalidStatusTransition");
    }

    // ==================================================================
    // NOT FOUND LIFECYCLE TESTS
    // ==================================================================

    [Fact]
    public async Task Verify_MissingMerchant_ReturnsNotFound()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{Guid.NewGuid()}/verify", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Merchants.NotFound");
    }

    [Fact]
    public async Task Suspend_MissingMerchant_ReturnsNotFound()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{Guid.NewGuid()}/suspend", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Merchants.NotFound");
    }

    [Fact]
    public async Task Activate_MissingMerchant_ReturnsNotFound()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{Guid.NewGuid()}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Merchants.NotFound");
    }

    [Fact]
    public async Task Close_MissingMerchant_ReturnsNotFound()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{Guid.NewGuid()}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Merchants.NotFound");
    }

    // ==================================================================
    // AUTHORIZATION TESTS — READ ENDPOINTS (AUTHENTICATION & PERMISSION)
    // ==================================================================

    [Fact]
    public async Task GetMerchants_Anonymous_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/merchants");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMerchantById_Anonymous_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/api/v1/merchants/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMerchants_AuthenticatedWithoutReadPermission_ReturnsForbidden()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedRegularUserAsync(factory, "viewer@example.com");
        string token = await LoginAsync(client, "viewer@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            "/api/v1/merchants",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        problem!["status"].ToString().Should().Be("403");
        problem["title"].ToString().Should().Be("Forbidden");
        problem["code"].ToString().Should().Be("AuthorizationFailed");
    }

    [Fact]
    public async Task GetMerchantById_AuthenticatedWithoutReadPermission_ReturnsForbidden()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedRegularUserAsync(factory, "viewer@example.com");
        string token = await LoginAsync(client, "viewer@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            $"/api/v1/merchants/{Guid.NewGuid()}",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        problem!["status"].ToString().Should().Be("403");
        problem["title"].ToString().Should().Be("Forbidden");
        problem["code"].ToString().Should().Be("AuthorizationFailed");
    }

    [Fact]
    public async Task GetMerchants_AuthenticatedWithReadPermission_ReturnsOk()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        Guid userId = await SeedRegularUserAsync(factory, "reader@example.com");
        await RoleSeeder.CreateRoleWithPermissionsAsync(
            factory,
            "MerchantReader",
            PermissionCatalog.MerchantsRead.Code);
        await RoleSeeder.AssignRoleAsync(factory, userId, "MerchantReader");

        string token = await LoginAsync(client, "reader@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            "/api/v1/merchants",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMerchantById_AuthenticatedWithReadPermission_ReturnsOk()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        Guid merchantId = await SeedMerchantAsync(factory);

        Guid userId = await SeedRegularUserAsync(factory, "reader@example.com");
        await RoleSeeder.CreateRoleWithPermissionsAsync(
            factory,
            "MerchantReader",
            PermissionCatalog.MerchantsRead.Code);
        await RoleSeeder.AssignRoleAsync(factory, userId, "MerchantReader");

        string token = await LoginAsync(client, "reader@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            $"/api/v1/merchants/{merchantId}",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ==================================================================
    // AUTHORIZATION TESTS — MANAGEMENT ENDPOINTS (AUTHENTICATION)
    // ==================================================================

    [Fact]
    public async Task VerifyMerchant_Anonymous_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{Guid.NewGuid()}/verify", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SuspendMerchant_Anonymous_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{Guid.NewGuid()}/suspend", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ActivateMerchant_Anonymous_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{Guid.NewGuid()}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CloseMerchant_Anonymous_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync($"/api/v1/merchants/{Guid.NewGuid()}/close", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ==================================================================
    // AUTHORIZATION TESTS — MANAGEMENT ENDPOINTS (FORBIDDEN & SUCCESS)
    // ==================================================================

    [Fact]
    public async Task VerifyMerchant_AuthenticatedWithoutManagePermission_ReturnsForbidden()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedRegularUserAsync(factory, "unprivileged@example.com");
        string token = await LoginAsync(client, "unprivileged@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            $"/api/v1/merchants/{Guid.NewGuid()}/verify",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        problem!["status"].ToString().Should().Be("403");
        problem["title"].ToString().Should().Be("Forbidden");
        problem["code"].ToString().Should().Be("AuthorizationFailed");
    }

    [Fact]
    public async Task SuspendMerchant_AuthenticatedWithoutManagePermission_ReturnsForbidden()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedRegularUserAsync(factory, "unprivileged@example.com");
        string token = await LoginAsync(client, "unprivileged@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            $"/api/v1/merchants/{Guid.NewGuid()}/suspend",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ActivateMerchant_AuthenticatedWithoutManagePermission_ReturnsForbidden()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedRegularUserAsync(factory, "unprivileged@example.com");
        string token = await LoginAsync(client, "unprivileged@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            $"/api/v1/merchants/{Guid.NewGuid()}/activate",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CloseMerchant_AuthenticatedWithoutManagePermission_ReturnsForbidden()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedRegularUserAsync(factory, "unprivileged@example.com");
        string token = await LoginAsync(client, "unprivileged@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            $"/api/v1/merchants/{Guid.NewGuid()}/close",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task VerifyMerchant_AuthenticatedWithManagePermission_ExecutesSuccessfully()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        Guid merchantId = await SeedMerchantAsync(factory);

        Guid managerId = await SeedRegularUserAsync(factory, "manager@example.com");
        await RoleSeeder.CreateRoleWithPermissionsAsync(
            factory,
            "MerchantManager",
            PermissionCatalog.MerchantsManage.Code);
        await RoleSeeder.AssignRoleAsync(factory, managerId, "MerchantManager");

        string token = await LoginAsync(client, "manager@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            $"/api/v1/merchants/{merchantId}/verify",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SuspendMerchant_AuthenticatedWithManagePermission_ExecutesSuccessfully()
    {
        using var adminContext = await CreateAdminContextAsync();
        HttpClient client = adminContext.Client;
        SqliteApiFactory factory = adminContext.Factory;
        Guid merchantId = await RegisterMerchantAsync(client);
        await client.PostAsync($"/api/v1/merchants/{merchantId}/verify", null);

        Guid managerId = await SeedRegularUserAsync(factory, "manager@example.com");
        await RoleSeeder.CreateRoleWithPermissionsAsync(
            factory,
            "MerchantManager",
            PermissionCatalog.MerchantsManage.Code);
        await RoleSeeder.AssignRoleAsync(factory, managerId, "MerchantManager");

        string token = await LoginAsync(client, "manager@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            $"/api/v1/merchants/{merchantId}/suspend",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ActivateMerchant_AuthenticatedWithManagePermission_ExecutesSuccessfully()
    {
        using var adminContext = await CreateAdminContextAsync();
        HttpClient client = adminContext.Client;
        SqliteApiFactory factory = adminContext.Factory;
        Guid merchantId = await RegisterMerchantAsync(client);
        await client.PostAsync($"/api/v1/merchants/{merchantId}/verify", null);
        await client.PostAsync($"/api/v1/merchants/{merchantId}/suspend", null);

        Guid managerId = await SeedRegularUserAsync(factory, "manager@example.com");
        await RoleSeeder.CreateRoleWithPermissionsAsync(
            factory,
            "MerchantManager",
            PermissionCatalog.MerchantsManage.Code);
        await RoleSeeder.AssignRoleAsync(factory, managerId, "MerchantManager");

        string token = await LoginAsync(client, "manager@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            $"/api/v1/merchants/{merchantId}/activate",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CloseMerchant_AuthenticatedWithManagePermission_ExecutesSuccessfully()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        Guid merchantId = await SeedMerchantAsync(factory);

        Guid managerId = await SeedRegularUserAsync(factory, "manager@example.com");
        await RoleSeeder.CreateRoleWithPermissionsAsync(
            factory,
            "MerchantManager",
            PermissionCatalog.MerchantsManage.Code);
        await RoleSeeder.AssignRoleAsync(factory, managerId, "MerchantManager");

        string token = await LoginAsync(client, "manager@example.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            $"/api/v1/merchants/{merchantId}/close",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ==================================================================
    // AUTHORIZATION & OWNERSHIP TESTS — SPRINT 10.4-C
    // ==================================================================

    [Fact]
    public async Task RegisterMerchant_Anonymous_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        RegisterRequest request = CreateValidRegisterRequest();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/merchants", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterMerchant_AuthenticatedUser_BindsOwnerIdFromCurrentUser()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        Guid userId = await SeedRegularUserAsync(factory, "regular-owner@merchant-test.com");
        string token = await LoginAsync(client, "regular-owner@merchant-test.com", ValidPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        RegisterRequest request = CreateValidRegisterRequest("Owner Bound Bakery", "Boston");

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/merchants", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        CreatedResponse? created = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        created.Should().NotBeNull();

        using IServiceScope scope = factory.Services.CreateScope();
        IMerchantRepository repository = scope.ServiceProvider.GetRequiredService<IMerchantRepository>();
        MerchantAggregate? merchant = await repository.GetByIdAsync(MerchantId.Create(created!.MerchantId));

        merchant.Should().NotBeNull();
        merchant!.OwnerId.Should().Be(userId);
        merchant.Name.Value.Should().Be("Owner Bound Bakery");
    }

    [Fact]
    public async Task UpdateMerchantProfile_Anonymous_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        var updateRequest = new UpdateRequest(
            "Updated Name",
            "456 New Street",
            "Bellevue",
            "WA",
            "98004",
            "USA",
            "+1-425-555-0199");

        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/merchants/{Guid.NewGuid()}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateMerchantProfile_Owner_ReturnsNoContent()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        Guid ownerId = await SeedRegularUserAsync(factory, "owner@example.com");
        string ownerToken = await LoginAsync(client, "owner@example.com", ValidPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        Guid merchantId = await RegisterMerchantAsync(client, CreateValidRegisterRequest("Original Store"));

        var updateRequest = new UpdateRequest(
            "Updated Store Name",
            "789 Renovated Ave",
            "Seattle",
            "WA",
            "98104",
            "USA",
            "+1-206-555-0199");

        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/merchants/{merchantId}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using IServiceScope scope = factory.Services.CreateScope();
        IMerchantRepository repository = scope.ServiceProvider.GetRequiredService<IMerchantRepository>();
        MerchantAggregate? updated = await repository.GetByIdAsync(MerchantId.Create(merchantId));
        updated.Should().NotBeNull();
        updated!.Name.Value.Should().Be("Updated Store Name");
    }

    [Fact]
    public async Task UpdateMerchantProfile_NonOwner_ReturnsForbidden()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        // Register merchant as owner A
        Guid ownerA = await SeedRegularUserAsync(factory, "ownerA@example.com");
        string tokenA = await LoginAsync(client, "ownerA@example.com", ValidPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        Guid merchantId = await RegisterMerchantAsync(client, CreateValidRegisterRequest("Owner A Store"));

        // Authenticate as user B (non-owner)
        Guid userB = await SeedRegularUserAsync(factory, "userB@example.com");
        string tokenB = await LoginAsync(client, "userB@example.com", ValidPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);

        var updateRequest = new UpdateRequest(
            "Hacked Name",
            "999 Intruder St",
            "Seattle",
            "WA",
            "98101",
            "USA",
            "+1-206-555-0999");

        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/merchants/{merchantId}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Merchants.Forbidden");
        document.RootElement.GetProperty("status").GetInt32().Should().Be(403);
    }

    [Fact]
    public async Task UpdateMerchantProfile_MissingMerchant_ReturnsNotFound()
    {
        using var context = await CreateAdminContextAsync();
        HttpClient client = context.Client;
        var updateRequest = new UpdateRequest(
            "Updated Name",
            "456 New Street",
            "Bellevue",
            "WA",
            "98004",
            "USA",
            "+1-425-555-0199");

        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/merchants/{Guid.NewGuid()}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Merchants.NotFound");
        document.RootElement.GetProperty("status").GetInt32().Should().Be(404);
    }

    [Fact]
    public async Task UpdateMerchantProfile_UserWithManagePermission_CannotBypassOwnership_ReturnsForbidden()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        // Register merchant as regular owner
        Guid ownerId = await SeedRegularUserAsync(factory, "regularOwner@example.com");
        string ownerToken = await LoginAsync(client, "regularOwner@example.com", ValidPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        Guid merchantId = await RegisterMerchantAsync(client, CreateValidRegisterRequest("Regular Owner Store"));

        // Authenticate as admin user who holds merchants:manage (and merchants:read)
        await SeedAdminAsync(factory);
        string adminToken = await LoginAsync(client, "admin@merchant-test.com", ValidPassword);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var updateRequest = new UpdateRequest(
            "Admin Overwrite Store",
            "999 Admin Ave",
            "Seattle",
            "WA",
            "98101",
            "USA",
            "+1-206-555-0999");

        HttpResponseMessage response = await client.PutAsJsonAsync($"/api/v1/merchants/{merchantId}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("Merchants.Forbidden");
        document.RootElement.GetProperty("status").GetInt32().Should().Be(403);
    }
}
