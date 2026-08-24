using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using LastHour.Api.Tests.Support;
using Microsoft.Data.Sqlite;

namespace LastHour.Api.Tests.Endpoints;

/// <summary>
/// Exercises the catalog endpoints end to end: category and product CRUD, search/pagination,
/// specialized mutations (price, SKU, category re-assignment) and soft delete. The matrix
/// covers the full authorization surface — anonymous, authenticated-without-permission and
/// authenticated-with-permission — matching the pattern established by
/// <see cref="AuthorizationEndpointsTests"/>.
/// </summary>
[Collection("ApiEndpoints")]
public class CatalogEndpointsTests
{
    private const string ValidPassword = "correct horse battery staple";

    private sealed record LoginResponse(string AccessToken, string RefreshToken, string TokenType, int ExpiresIn);

    private sealed record CategoryCreatedResponse(Guid CategoryId, string Name);

    private sealed record ProductCreatedResponse(Guid ProductId, string Sku);

    private sealed record CategoryDto(Guid Id, string Name, string? Description, Guid? ParentCategoryId);

    private sealed record ProductDto(
        Guid Id,
        string Sku,
        string Name,
        string? Description,
        decimal PriceAmount,
        string PriceCurrency,
        Guid CategoryId);

    private sealed record PagedResult<T>(
        IReadOnlyList<T> Items,
        int PageNumber,
        int PageSize,
        int TotalCount,
        int TotalPages);

    // ------------------------------------------------------------------
    // Factory helpers
    // ------------------------------------------------------------------

    private static string CreateConnectionString()
        => new SqliteConnectionStringBuilder
        {
            DataSource = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"lasthour-catalog-test-{Guid.NewGuid():N}.db"),
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
        Guid adminId = await UserSeeder.SeedUserAsync(factory, "admin@catalog-test.com", ValidPassword);
        await RoleSeeder.AssignRoleAsync(factory, adminId, LastHour.Modules.Identity.Application.Permissions.BootstrapRoles.Administrator);
        return adminId;
    }

    private static async Task<Guid> SeedRegularUserAsync(SqliteApiFactory factory)
        => await UserSeeder.SeedUserAsync(factory, "user@catalog-test.com", ValidPassword);

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

    // ------------------------------------------------------------------
    // Shared arrange helpers
    // ------------------------------------------------------------------

    private static async Task<Guid> CreateCategoryAsync(HttpClient client, string token, string name = "Electronics")
    {
        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/categories",
            token,
            new { name, description = (string?)null, parentCategoryId = (Guid?)null }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        CategoryCreatedResponse? payload = await response.Content.ReadFromJsonAsync<CategoryCreatedResponse>();
        return payload!.CategoryId;
    }

    private static async Task<Guid> CreateProductAsync(
        HttpClient client,
        string token,
        Guid categoryId,
        string sku = "PROD-001",
        string name = "Laptop")
    {
        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/products",
            token,
            new
            {
                sku,
                name,
                priceAmount = 999.99m,
                priceCurrency = "USD",
                categoryId,
                description = (string?)null,
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ProductCreatedResponse? payload = await response.Content.ReadFromJsonAsync<ProductCreatedResponse>();
        return payload!.ProductId;
    }

    // ==================================================================
    // AUTHORIZATION TESTS — CATEGORY ENDPOINTS
    // ==================================================================

    [Fact]
    public async Task CreateCategory_Anonymous_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/categories",
            new { name = "Electronics" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCategories_Anonymous_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/categories");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCategoryById_Anonymous_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/api/v1/categories/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateCategory_Anonymous_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            $"/api/v1/categories/{Guid.NewGuid()}",
            new { name = "New Name" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateCategory_AuthenticatedWithoutPermission_ReturnsForbidden()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedRegularUserAsync(factory);
        string token = await LoginAsync(client, "user@catalog-test.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/categories",
            token,
            new { name = "Electronics" }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        problem!["status"].ToString().Should().Be("403");
        problem["title"].ToString().Should().Be("Forbidden");
        problem["code"].ToString().Should().Be("AuthorizationFailed");
    }

    [Fact]
    public async Task GetCategories_AuthenticatedWithoutReadPermission_ReturnsForbidden()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedRegularUserAsync(factory);
        string token = await LoginAsync(client, "user@catalog-test.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            "/api/v1/categories",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCategoryById_AuthenticatedWithoutReadPermission_ReturnsForbidden()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedRegularUserAsync(factory);
        string token = await LoginAsync(client, "user@catalog-test.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            $"/api/v1/categories/{Guid.NewGuid()}",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateCategory_AuthenticatedWithoutManagePermission_ReturnsForbidden()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedRegularUserAsync(factory);
        string token = await LoginAsync(client, "user@catalog-test.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Put,
            $"/api/v1/categories/{Guid.NewGuid()}",
            token,
            new { name = "New Name" }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ==================================================================
    // CATEGORY CRUD TESTS
    // ==================================================================

    [Fact]
    public async Task CreateCategory_ValidRequest_ReturnsCreatedWithStableId()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/categories",
            token,
            new { name = "Electronics", description = "Consumer electronics", parentCategoryId = (Guid?)null }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        CategoryCreatedResponse? payload = await response.Content.ReadFromJsonAsync<CategoryCreatedResponse>();
        payload.Should().NotBeNull();
        payload!.CategoryId.Should().NotBeEmpty();
        payload.Name.Should().Be("Electronics");
    }

    [Fact]
    public async Task CreateCategory_EmptyName_ReturnsBadRequest()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/categories",
            token,
            new { name = string.Empty }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetCategories_WithData_ReturnsOkWithCollection()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);
        await CreateCategoryAsync(client, token, "Electronics");

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            "/api/v1/categories",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<CategoryDto>? categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>();
        categories.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetCategoryById_ExistingCategory_ReturnsOk()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);
        Guid categoryId = await CreateCategoryAsync(client, token, "Electronics");

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            $"/api/v1/categories/{categoryId}",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CategoryDto? category = await response.Content.ReadFromJsonAsync<CategoryDto>();
        category.Should().NotBeNull();
        category!.Id.Should().Be(categoryId);
        category.Name.Should().Be("Electronics");
    }

    [Fact]
    public async Task GetCategoryById_NonexistentCategory_ReturnsNotFound()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            $"/api/v1/categories/{Guid.NewGuid()}",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCategory_ValidRequest_ReturnsNoContent()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);
        Guid categoryId = await CreateCategoryAsync(client, token, "Electronics");

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Put,
            $"/api/v1/categories/{categoryId}",
            token,
            new { name = "Consumer Electronics", description = "Updated description", parentCategoryId = (Guid?)null }));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateCategory_NonexistentCategory_ReturnsNotFound()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Put,
            $"/api/v1/categories/{Guid.NewGuid()}",
            token,
            new { name = "Ghost Category", description = (string?)null, parentCategoryId = (Guid?)null }));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ==================================================================
    // AUTHORIZATION TESTS — PRODUCT ENDPOINTS
    // ==================================================================

    [Fact]
    public async Task CreateProduct_Anonymous_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/products",
            new { sku = "SKU-001", name = "Laptop", priceAmount = 999m, priceCurrency = "USD", categoryId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProducts_Anonymous_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProductById_Anonymous_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/api/v1/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteProduct_Anonymous_ReturnsUnauthorized()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.DeleteAsync($"/api/v1/products/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProduct_AuthenticatedWithoutPermission_ReturnsForbidden()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedRegularUserAsync(factory);
        string token = await LoginAsync(client, "user@catalog-test.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/products",
            token,
            new { sku = "SKU-001", name = "Laptop", priceAmount = 999m, priceCurrency = "USD", categoryId = Guid.NewGuid() }));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetProducts_AuthenticatedWithoutReadPermission_ReturnsForbidden()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedRegularUserAsync(factory);
        string token = await LoginAsync(client, "user@catalog-test.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            "/api/v1/products",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteProduct_AuthenticatedWithoutManagePermission_ReturnsForbidden()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedRegularUserAsync(factory);
        string token = await LoginAsync(client, "user@catalog-test.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Delete,
            $"/api/v1/products/{Guid.NewGuid()}",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ==================================================================
    // PRODUCT CRUD TESTS
    // ==================================================================

    [Fact]
    public async Task CreateProduct_ValidRequest_ReturnsCreatedWithStableId()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);
        Guid categoryId = await CreateCategoryAsync(client, token);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/products",
            token,
            new
            {
                sku = "LAPTOP-PRO-001",
                name = "Laptop Pro",
                priceAmount = 1299.99m,
                priceCurrency = "USD",
                categoryId,
                description = "High-end laptop",
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        ProductCreatedResponse? payload = await response.Content.ReadFromJsonAsync<ProductCreatedResponse>();
        payload.Should().NotBeNull();
        payload!.ProductId.Should().NotBeEmpty();
        payload.Sku.Should().Be("LAPTOP-PRO-001");
    }

    [Fact]
    public async Task CreateProduct_EmptySku_ReturnsBadRequest()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);
        Guid categoryId = await CreateCategoryAsync(client, token);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            "/api/v1/products",
            token,
            new { sku = string.Empty, name = "Laptop", priceAmount = 999m, priceCurrency = "USD", categoryId }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProductById_ExistingProduct_ReturnsOk()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);
        Guid categoryId = await CreateCategoryAsync(client, token);
        Guid productId = await CreateProductAsync(client, token, categoryId, "SKU-GET-001", "Test Laptop");

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            $"/api/v1/products/{productId}",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ProductDto? product = await response.Content.ReadFromJsonAsync<ProductDto>();
        product.Should().NotBeNull();
        product!.Id.Should().Be(productId);
        product.Name.Should().Be("Test Laptop");
        product.Sku.Should().Be("SKU-GET-001");
    }

    [Fact]
    public async Task GetProductById_NonexistentProduct_ReturnsNotFound()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            $"/api/v1/products/{Guid.NewGuid()}",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchProducts_ReturnsOkWithPagedResult()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);
        Guid categoryId = await CreateCategoryAsync(client, token);
        await CreateProductAsync(client, token, categoryId, "SKU-SRCH-001", "Search Laptop");
        await CreateProductAsync(client, token, categoryId, "SKU-SRCH-002", "Search Desktop");

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            "/api/v1/products?pageNumber=1&pageSize=10",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<ProductDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        paged.Should().NotBeNull();
        paged!.PageNumber.Should().Be(1);
        paged.PageSize.Should().Be(10);
        paged.Items.Should().HaveCount(2);
        paged.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task SearchProducts_WithSearchTerm_FiltersResults()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);
        Guid categoryId = await CreateCategoryAsync(client, token);
        await CreateProductAsync(client, token, categoryId, "SKU-FILT-001", "Laptop Alpha");
        await CreateProductAsync(client, token, categoryId, "SKU-FILT-002", "Desktop Beta");

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            "/api/v1/products?search=Laptop",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<ProductDto>? paged = await response.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        paged.Should().NotBeNull();
        paged!.Items.Should().ContainSingle()
            .Which.Name.Should().Be("Laptop Alpha");
    }

    [Fact]
    public async Task UpdateProduct_ValidRequest_ReturnsNoContent()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);
        Guid categoryId = await CreateCategoryAsync(client, token);
        Guid productId = await CreateProductAsync(client, token, categoryId, "SKU-UPD-001", "Old Name");

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Put,
            $"/api/v1/products/{productId}",
            token,
            new { name = "New Name", description = "Updated description" }));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangeProductPrice_ValidRequest_ReturnsNoContent()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);
        Guid categoryId = await CreateCategoryAsync(client, token);
        Guid productId = await CreateProductAsync(client, token, categoryId, "SKU-PRC-001", "Priceable");

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Put,
            $"/api/v1/products/{productId}/price",
            token,
            new { amount = 799.99m, currency = "USD" }));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangeProductSku_ValidRequest_ReturnsNoContent()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);
        Guid categoryId = await CreateCategoryAsync(client, token);
        Guid productId = await CreateProductAsync(client, token, categoryId, "SKU-OLD-001", "SkuChanger");

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Put,
            $"/api/v1/products/{productId}/sku",
            token,
            new { sku = "SKU-NEW-001" }));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ChangeProductCategory_ValidRequest_ReturnsNoContent()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);
        Guid categoryId1 = await CreateCategoryAsync(client, token, "Category One");
        Guid categoryId2 = await CreateCategoryAsync(client, token, "Category Two");
        Guid productId = await CreateProductAsync(client, token, categoryId1, "SKU-CAT-001", "CategoryMover");

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Put,
            $"/api/v1/products/{productId}/category",
            token,
            new { categoryId = categoryId2 }));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProduct_ExistingProduct_ReturnsNoContent()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);
        Guid categoryId = await CreateCategoryAsync(client, token);
        Guid productId = await CreateProductAsync(client, token, categoryId, "SKU-DEL-001", "DeleteMe");

        HttpResponseMessage response = await client.SendAsync(CreateRequest(
            HttpMethod.Delete,
            $"/api/v1/products/{productId}",
            token));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteProduct_DeletedProductNotReturnedByGetById()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);
        Guid categoryId = await CreateCategoryAsync(client, token);
        Guid productId = await CreateProductAsync(client, token, categoryId, "SKU-DEL-GHOST-001", "GhostProduct");

        // Delete the product.
        HttpResponseMessage deleteResponse = await client.SendAsync(CreateRequest(
            HttpMethod.Delete,
            $"/api/v1/products/{productId}",
            token));
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // The product must no longer be accessible.
        HttpResponseMessage getResponse = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            $"/api/v1/products/{productId}",
            token));
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_DeletedProductNotReturnedBySearch()
    {
        using SqliteApiFactory factory = await CreateFactoryAsync();
        HttpClient client = factory.CreateClient();
        await SeedAdminAsync(factory);
        string token = await LoginAsync(client, "admin@catalog-test.com", ValidPassword);
        Guid categoryId = await CreateCategoryAsync(client, token);
        Guid productId = await CreateProductAsync(client, token, categoryId, "SKU-DEL-SRCH-001", "HiddenProduct");

        // Delete the product.
        await client.SendAsync(CreateRequest(
            HttpMethod.Delete,
            $"/api/v1/products/{productId}",
            token));

        // The product must not appear in search results.
        HttpResponseMessage searchResponse = await client.SendAsync(CreateRequest(
            HttpMethod.Get,
            "/api/v1/products?search=HiddenProduct",
            token));
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResult<ProductDto>? paged = await searchResponse.Content.ReadFromJsonAsync<PagedResult<ProductDto>>();
        paged!.Items.Should().BeEmpty();
    }
}
