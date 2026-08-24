using FluentAssertions;
using LastHour.BuildingBlocks.Infrastructure.Persistence;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Domain.Products;
using LastHour.Modules.Catalog.Tests.Persistence.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LastHour.Modules.Catalog.Tests.Persistence;

public class CatalogPersistenceTests
{
    [Fact]
    public async Task SaveAndLoad_Category_RoundTripsAllColumns()
    {
        using SqliteConnection connection = TestCatalogPersistence.CreateOpenConnection();
        CategoryId parentId;
        CategoryId childId;

        await using (LastHourDbContext context = await TestCatalogPersistence.CreateContextAsync(connection))
        {
            Category parent = Category.Create("Electronics", "Electronic Devices");
            context.Set<Category>().Add(parent);
            await context.SaveChangesAsync();
            parentId = parent.Id;

            Category child = Category.Create("Smartphones", "Mobile phones", parent.Id);
            context.Set<Category>().Add(child);
            await context.SaveChangesAsync();
            childId = child.Id;
        }

        await using (LastHourDbContext reloaded = TestCatalogPersistence.CreateSqliteContext(connection))
        {
            Category? loadedChild = await reloaded.Set<Category>().SingleOrDefaultAsync(c => c.Id == childId);

            loadedChild.Should().NotBeNull();
            loadedChild!.Id.Should().Be(childId);
            loadedChild.Name.Should().Be("Smartphones");
            loadedChild.Description.Should().Be("Mobile phones");
            loadedChild.ParentCategoryId.Should().Be(parentId);
            loadedChild.CreatedAt.Should().Be(TestCatalogPersistence.FixedUtcNow);
            loadedChild.CreatedBy.Should().Be(TestCatalogPersistence.CurrentUserId);
        }
    }

    [Fact]
    public async Task SaveAndLoad_Product_RoundTripsAllColumnsAndMoney()
    {
        using SqliteConnection connection = TestCatalogPersistence.CreateOpenConnection();
        CategoryId categoryId;
        ProductId productId;

        await using (LastHourDbContext context = await TestCatalogPersistence.CreateContextAsync(connection))
        {
            Category category = Category.Create("Hardware");
            context.Set<Category>().Add(category);
            await context.SaveChangesAsync();
            categoryId = category.Id;

            Product product = Product.Create(
                Sku.From("PROD-001"),
                "Mechanical Keyboard",
                Money.Create(149.99m, "EUR"),
                category.Id,
                "RGB Mechanical Gaming Keyboard");

            context.Set<Product>().Add(product);
            await context.SaveChangesAsync();
            productId = product.Id;
        }

        await using (LastHourDbContext reloaded = TestCatalogPersistence.CreateSqliteContext(connection))
        {
            Product? loaded = await reloaded.Set<Product>().SingleOrDefaultAsync(p => p.Id == productId);

            loaded.Should().NotBeNull();
            loaded!.Id.Should().Be(productId);
            loaded.Sku.Value.Should().Be("PROD-001");
            loaded.Sku.NormalizedValue.Should().Be("PROD-001");
            loaded.Name.Should().Be("Mechanical Keyboard");
            loaded.Description.Should().Be("RGB Mechanical Gaming Keyboard");
            loaded.BasePrice.Amount.Should().Be(149.99m);
            loaded.BasePrice.Currency.Should().Be("EUR");
            loaded.CategoryId.Should().Be(categoryId);
            loaded.CreatedAt.Should().Be(TestCatalogPersistence.FixedUtcNow);
            loaded.CreatedBy.Should().Be(TestCatalogPersistence.CurrentUserId);
            loaded.IsDeleted.Should().BeFalse();
        }
    }

    [Fact]
    public async Task DuplicateNormalizedSku_DatabaseRejectsSecondInsert()
    {
        using SqliteConnection connection = TestCatalogPersistence.CreateOpenConnection();
        await using LastHourDbContext context = await TestCatalogPersistence.CreateContextAsync(connection);

        Category category = Category.Create("Books");
        context.Set<Category>().Add(category);
        await context.SaveChangesAsync();

        Product first = Product.Create(Sku.From("SKU-100"), "First Product", Money.Create(10m, "EUR"), category.Id);
        context.Set<Product>().Add(first);
        await context.SaveChangesAsync();

        Product second = Product.Create(Sku.From("sku-100"), "Second Product", Money.Create(20m, "EUR"), category.Id);
        context.Set<Product>().Add(second);

        Func<Task> act = () => context.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>()
            .Where(exception => HasConstraintFailure(exception));
    }

    [Fact]
    public async Task CategorySelfReference_DeleteParentWithChild_ThrowsDbUpdateException()
    {
        using SqliteConnection connection = TestCatalogPersistence.CreateOpenConnection();
        CategoryId parentId;

        await using (LastHourDbContext context = await TestCatalogPersistence.CreateContextAsync(connection))
        {
            Category parent = Category.Create("Root");
            context.Set<Category>().Add(parent);
            await context.SaveChangesAsync();
            parentId = parent.Id;

            Category child = Category.Create("Child", parentCategoryId: parent.Id);
            context.Set<Category>().Add(child);
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext deleteContext = TestCatalogPersistence.CreateSqliteContext(connection))
        {
            Category parent = await deleteContext.Set<Category>().SingleAsync(c => c.Id == parentId);
            deleteContext.Set<Category>().Remove(parent);

            Func<Task> act = () => deleteContext.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateException>()
                .Where(exception => HasConstraintFailure(exception));
        }
    }

    [Fact]
    public async Task ProductCategory_DeleteCategoryWithAssociatedProduct_ThrowsDbUpdateException()
    {
        using SqliteConnection connection = TestCatalogPersistence.CreateOpenConnection();
        CategoryId categoryId;

        await using (LastHourDbContext context = await TestCatalogPersistence.CreateContextAsync(connection))
        {
            Category category = Category.Create("Components");
            context.Set<Category>().Add(category);
            await context.SaveChangesAsync();
            categoryId = category.Id;

            Product product = Product.Create(Sku.From("CPU-01"), "Processor", Money.Create(300m, "USD"), category.Id);
            context.Set<Product>().Add(product);
            await context.SaveChangesAsync();
        }

        await using (LastHourDbContext deleteContext = TestCatalogPersistence.CreateSqliteContext(connection))
        {
            Category category = await deleteContext.Set<Category>().SingleAsync(c => c.Id == categoryId);
            deleteContext.Set<Category>().Remove(category);

            Func<Task> act = () => deleteContext.SaveChangesAsync();

            await act.Should().ThrowAsync<DbUpdateException>()
                .Where(exception => HasConstraintFailure(exception));
        }
    }

    [Fact]
    public async Task Product_SoftDelete_ExcludedFromNormalQueriesAndStampsFields()
    {
        using SqliteConnection connection = TestCatalogPersistence.CreateOpenConnection();
        ProductId productId;

        await using (LastHourDbContext context = await TestCatalogPersistence.CreateContextAsync(connection))
        {
            Category category = Category.Create("Peripherals");
            context.Set<Category>().Add(category);
            await context.SaveChangesAsync();

            Product product = Product.Create(Sku.From("MOUSE-01"), "Mouse", Money.Create(50m, "EUR"), category.Id);
            context.Set<Product>().Add(product);
            await context.SaveChangesAsync();
            productId = product.Id;
        }

        await using (LastHourDbContext deleteContext = TestCatalogPersistence.CreateSqliteContext(connection))
        {
            Product product = await deleteContext.Set<Product>().SingleAsync(p => p.Id == productId);
            deleteContext.Set<Product>().Remove(product);
            await deleteContext.SaveChangesAsync();
        }

        await using (LastHourDbContext queryContext = TestCatalogPersistence.CreateSqliteContext(connection))
        {
            Product? normalQuery = await queryContext.Set<Product>().SingleOrDefaultAsync(p => p.Id == productId);
            normalQuery.Should().BeNull();

            Product? ignoredFilterQuery = await queryContext.Set<Product>()
                .IgnoreQueryFilters()
                .SingleOrDefaultAsync(p => p.Id == productId);

            ignoredFilterQuery.Should().NotBeNull();
            ignoredFilterQuery!.IsDeleted.Should().BeTrue();
            ignoredFilterQuery.DeletedAt.Should().Be(TestCatalogPersistence.FixedUtcNow);
            ignoredFilterQuery.DeletedBy.Should().Be(TestCatalogPersistence.CurrentUserId);
        }
    }

    private static bool HasConstraintFailure(DbUpdateException exception)
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
