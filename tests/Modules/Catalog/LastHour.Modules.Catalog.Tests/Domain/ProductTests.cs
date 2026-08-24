using FluentAssertions;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Domain.Products;

namespace LastHour.Modules.Catalog.Tests.Domain;

public class ProductTests
{
    [Fact]
    public void Create_ValidProduct_SetsAllProperties()
    {
        Sku sku = Sku.From("LAPTOP-001");
        Money price = Money.Create(1299.99m, "USD");
        CategoryId categoryId = CategoryId.New();

        Product product = Product.Create(sku, "Gaming Laptop", price, categoryId, "High performance laptop");

        product.Id.Value.Should().NotBe(Guid.Empty);
        product.Sku.Should().Be(sku);
        product.Name.Should().Be("Gaming Laptop");
        product.Description.Should().Be("High performance laptop");
        product.BasePrice.Should().Be(price);
        product.CategoryId.Should().Be(categoryId);
        product.IsDeleted.Should().BeFalse();
        product.DeletedAt.Should().BeNull();
        product.DeletedBy.Should().BeNull();
    }

    [Fact]
    public void Create_NullSku_ThrowsArgumentNullException()
    {
        Money price = Money.Create(10m, "EUR");
        CategoryId categoryId = CategoryId.New();

        Action act = () => Product.Create(null!, "Name", price, categoryId);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_NullBasePrice_ThrowsArgumentNullException()
    {
        Sku sku = Sku.From("SKU-1");
        CategoryId categoryId = CategoryId.New();

        Action act = () => Product.Create(sku, "Name", null!, categoryId);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_NullCategoryId_ThrowsArgumentNullException()
    {
        Sku sku = Sku.From("SKU-1");
        Money price = Money.Create(10m, "EUR");

        Action act = () => Product.Create(sku, "Name", price, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_NullOrWhiteSpaceName_ThrowsInvalidProductNameException(string? name)
    {
        Sku sku = Sku.From("SKU-1");
        Money price = Money.Create(10m, "EUR");
        CategoryId categoryId = CategoryId.New();

        Action act = () => Product.Create(sku, name!, price, categoryId);

        act.Should().Throw<InvalidProductNameException>()
            .WithMessage("A product name is required.");
    }

    [Fact]
    public void Create_OverlongName_ThrowsInvalidProductNameException()
    {
        Sku sku = Sku.From("SKU-1");
        Money price = Money.Create(10m, "EUR");
        CategoryId categoryId = CategoryId.New();
        string overlongName = new string('A', Product.MaxNameLength + 1);

        Action act = () => Product.Create(sku, overlongName, price, categoryId);

        act.Should().Throw<InvalidProductNameException>()
            .WithMessage($"A product name cannot exceed {Product.MaxNameLength} characters.");
    }

    [Fact]
    public void Create_OverlongDescription_ThrowsInvalidProductNameException()
    {
        Sku sku = Sku.From("SKU-1");
        Money price = Money.Create(10m, "EUR");
        CategoryId categoryId = CategoryId.New();
        string overlongDesc = new string('A', Product.MaxDescriptionLength + 1);

        Action act = () => Product.Create(sku, "Valid Name", price, categoryId, overlongDesc);

        act.Should().Throw<InvalidProductNameException>()
            .WithMessage($"A product description cannot exceed {Product.MaxDescriptionLength} characters.");
    }

    [Fact]
    public void UpdateDetails_ValidData_UpdatesNameDescriptionAndCategory()
    {
        Product product = CreateSampleProduct();
        CategoryId newCategoryId = CategoryId.New();

        product.UpdateDetails("Updated Name", "Updated Description", newCategoryId);

        product.Name.Should().Be("Updated Name");
        product.Description.Should().Be("Updated Description");
        product.CategoryId.Should().Be(newCategoryId);
    }

    [Fact]
    public void UpdateDetails_NullCategoryId_ThrowsArgumentNullException()
    {
        Product product = CreateSampleProduct();

        Action act = () => product.UpdateDetails("Updated Name", null, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ChangePrice_ValidPrice_UpdatesBasePrice()
    {
        Product product = CreateSampleProduct();
        Money newPrice = Money.Create(199.99m, "EUR");

        product.ChangePrice(newPrice);

        product.BasePrice.Should().Be(newPrice);
    }

    [Fact]
    public void ChangePrice_NullPrice_ThrowsArgumentNullException()
    {
        Product product = CreateSampleProduct();

        Action act = () => product.ChangePrice(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ChangeSku_ValidSku_UpdatesSku()
    {
        Product product = CreateSampleProduct();
        Sku newSku = Sku.From("NEW-SKU-999");

        product.ChangeSku(newSku);

        product.Sku.Should().Be(newSku);
    }

    [Fact]
    public void ChangeSku_NullSku_ThrowsArgumentNullException()
    {
        Product product = CreateSampleProduct();

        Action act = () => product.ChangeSku(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ChangeCategory_ValidCategory_UpdatesCategoryId()
    {
        Product product = CreateSampleProduct();
        CategoryId newCategoryId = CategoryId.New();

        product.ChangeCategory(newCategoryId);

        product.CategoryId.Should().Be(newCategoryId);
    }

    [Fact]
    public void ChangeCategory_NullCategory_ThrowsArgumentNullException()
    {
        Product product = CreateSampleProduct();

        Action act = () => product.ChangeCategory(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private static Product CreateSampleProduct()
    {
        return Product.Create(
            Sku.From("PROD-100"),
            "Sample Product",
            Money.Create(49.99m, "EUR"),
            CategoryId.New(),
            "Sample description");
    }
}
