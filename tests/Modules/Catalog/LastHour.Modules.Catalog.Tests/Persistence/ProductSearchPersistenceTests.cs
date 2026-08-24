using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.BuildingBlocks.Infrastructure.Persistence.Repositories;
using LastHour.Modules.Catalog.Application.Persistence;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Domain.Products;
using LastHour.Modules.Catalog.Tests.Persistence.TestSupport;
using Microsoft.Data.Sqlite;

namespace LastHour.Modules.Catalog.Tests.Persistence;

public class ProductSearchPersistenceTests
{
    [Fact]
    public async Task Search_SortedByNameAcrossPages_ReturnsStableOrderedSlices()
    {
        using SqliteConnection connection = TestCatalogPersistence.CreateOpenConnection();
        await using (LastHourDbContext context = await TestCatalogPersistence.CreateContextAsync(connection))
        {
            Category category = Category.Create("Hardware");
            context.Set<Category>().Add(category);
            await context.SaveChangesAsync();

            AddProduct(context, category.Id, "MON-01", "Alpha Monitor", 50m);
            AddProduct(context, category.Id, "CABLE-01", "Bravo Cable", 40m);
            AddProduct(context, category.Id, "PAD-01", "Charlie Pad", 30m);
            AddProduct(context, category.Id, "MOUSE-01", "Delta Mouse", 20m);
            AddProduct(context, category.Id, "KEY-01", "Echo Keyboard", 10m);
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestCatalogPersistence.CreateSqliteContext(connection))
        {
            ProductRepository repository = CreateRepository(context);

            IReadOnlyList<Product> first = await repository.SearchAsync(
                null, null, ProductSortField.Name, ProductSortDirection.Asc, 1, 2, CancellationToken.None);
            IReadOnlyList<Product> second = await repository.SearchAsync(
                null, null, ProductSortField.Name, ProductSortDirection.Asc, 2, 2, CancellationToken.None);
            int total = await repository.CountAsync(null, null, CancellationToken.None);

            first.Select(product => product.Name).Should().Equal("Alpha Monitor", "Bravo Cable");
            second.Select(product => product.Name).Should().Equal("Charlie Pad", "Delta Mouse");
            total.Should().Be(5);
        }
    }

    [Fact]
    public async Task Search_WithTermAndCategoryFilter_ReturnsOnlyMatchingProducts()
    {
        using SqliteConnection connection = TestCatalogPersistence.CreateOpenConnection();
        CategoryId electronics;
        CategoryId books;

        await using (LastHourDbContext context = await TestCatalogPersistence.CreateContextAsync(connection))
        {
            Category electronicCategory = Category.Create("Electronics");
            Category bookCategory = Category.Create("Books");
            context.Set<Category>().AddRange(electronicCategory, bookCategory);
            await context.SaveChangesAsync();
            electronics = electronicCategory.Id;
            books = bookCategory.Id;

            AddProduct(context, electronics, "KEY-01", "Mechanical Keyboard", 149.99m);
            AddProduct(context, electronics, "MOUSE-01", "Gaming Mouse", 79.99m);
            AddProduct(context, books, "BOOK-01", "Keyboard Shortcuts Guide", 19.99m);
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestCatalogPersistence.CreateSqliteContext(connection))
        {
            ProductRepository repository = CreateRepository(context);

            IReadOnlyList<Product> matches = await repository.SearchAsync(
                "Keyboard", electronics, ProductSortField.Name, ProductSortDirection.Asc, 1, 20, CancellationToken.None);
            int total = await repository.CountAsync("Keyboard", electronics, CancellationToken.None);

            matches.Should().ContainSingle(product => product.Sku.Value == "KEY-01");
            matches.Should().NotContain(product => product.Sku.Value == "BOOK-01");
            total.Should().Be(1);
        }
    }

    [Fact]
    public async Task Search_SkuTerm_MatchesCaseInsensitivelyViaNormalizedSku()
    {
        using SqliteConnection connection = TestCatalogPersistence.CreateOpenConnection();
        await using (LastHourDbContext context = await TestCatalogPersistence.CreateContextAsync(connection))
        {
            Category category = Category.Create("Hardware");
            context.Set<Category>().Add(category);
            await context.SaveChangesAsync();

            AddProduct(context, category.Id, "ABC-123", "Widget", 10m);
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestCatalogPersistence.CreateSqliteContext(connection))
        {
            ProductRepository repository = CreateRepository(context);

            IReadOnlyList<Product> matches = await repository.SearchAsync(
                "abc", null, ProductSortField.Name, ProductSortDirection.Asc, 1, 20, CancellationToken.None);

            matches.Should().ContainSingle(product => product.Sku.Value == "ABC-123");
        }
    }

    [Fact]
    public async Task Search_ExcludesSoftDeletedProductsFromCountAndResults()
    {
        using SqliteConnection connection = TestCatalogPersistence.CreateOpenConnection();
        ProductId deletedId;

        await using (LastHourDbContext context = await TestCatalogPersistence.CreateContextAsync(connection))
        {
            Category category = Category.Create("Peripherals");
            context.Set<Category>().Add(category);
            await context.SaveChangesAsync();

            Product live = AddProduct(context, category.Id, "MOUSE-01", "Gaming Mouse", 79.99m);
            Product deleted = AddProduct(context, category.Id, "KEY-01", "Mechanical Keyboard", 149.99m);
            await context.SaveChangesAsync();
            deletedId = deleted.Id;

            context.Set<Product>().Remove(deleted);
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext context = TestCatalogPersistence.CreateSqliteContext(connection))
        {
            ProductRepository repository = CreateRepository(context);

            IReadOnlyList<Product> results = await repository.SearchAsync(
                null, null, ProductSortField.Name, ProductSortDirection.Asc, 1, 20, CancellationToken.None);
            int total = await repository.CountAsync(null, null, CancellationToken.None);
            Product? deleted = await repository.GetByIdAsync(deletedId, CancellationToken.None);

            results.Should().ContainSingle(product => product.Sku.Value == "MOUSE-01");
            results.Should().NotContain(product => product.Sku.Value == "KEY-01");
            total.Should().Be(1);
            deleted.Should().BeNull();
        }
    }

    private static Product AddProduct(
        LastHourDbContext context,
        CategoryId categoryId,
        string sku,
        string name,
        decimal priceAmount)
    {
        Product product = Product.Create(
            Sku.From(sku),
            name,
            Money.Create(priceAmount, "EUR"),
            categoryId);
        context.Set<Product>().Add(product);
        return product;
    }

    private static ProductRepository CreateRepository(LastHourDbContext context)
        => new ProductRepository(
            new EfRepository<Product>(context),
            new EfReadRepository<Product, ProductId>(context),
            new EfTrackedRepository<Product, ProductId>(context));
}
