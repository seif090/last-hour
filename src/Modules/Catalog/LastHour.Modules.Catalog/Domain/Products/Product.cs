using LastHour.BuildingBlocks.SharedKernel.Domain;
using LastHour.Modules.Catalog.Domain.Categories;

namespace LastHour.Modules.Catalog.Domain.Products;

/// <summary>
/// The product aggregate root of the catalog module. It represents a sellable catalog item
/// with its identity, SKU, name, description, base price, and category classification.
/// The aggregate protects product invariants and supports auditing and soft deletion.
/// </summary>
public sealed class Product : AggregateRoot<ProductId>, IAuditableEntity, ISoftDelete
{
    /// <summary>
    /// The maximum length of a product name.
    /// </summary>
    public const int MaxNameLength = 200;

    /// <summary>
    /// The maximum length of a product description.
    /// </summary>
    public const int MaxDescriptionLength = 2000;

    private Product()
    {
        // EF Core materialization only.
    }

    private Product(
        ProductId id,
        Sku sku,
        string name,
        string? description,
        Money basePrice,
        CategoryId categoryId)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Description = description;
        BasePrice = basePrice;
        CategoryId = categoryId;
    }

    /// <summary>
    /// Gets the Stock Keeping Unit (SKU) of the product.
    /// </summary>
    public Sku Sku { get; private set; } = null!;

    /// <summary>
    /// Gets the name of the product.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the optional description of the product.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the base price of the product.
    /// </summary>
    public Money BasePrice { get; private set; } = null!;

    /// <summary>
    /// Gets the identifier of the category this product belongs to.
    /// </summary>
    public CategoryId CategoryId { get; private set; } = null!;

    /// <inheritdoc/>
    public DateTime CreatedAt { get; set; }

    /// <inheritdoc/>
    public string? CreatedBy { get; set; }

    /// <inheritdoc/>
    public DateTime? UpdatedAt { get; set; }

    /// <inheritdoc/>
    public string? UpdatedBy { get; set; }

    /// <inheritdoc/>
    public bool IsDeleted { get; set; }

    /// <inheritdoc/>
    public DateTime? DeletedAt { get; set; }

    /// <inheritdoc/>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="sku">The product SKU.</param>
    /// <param name="name">The product name.</param>
    /// <param name="basePrice">The base price.</param>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="description">An optional description.</param>
    /// <returns>A new <see cref="Product"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sku"/>, <paramref name="basePrice"/>, or <paramref name="categoryId"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidProductNameException"><paramref name="name"/> is null, empty, or exceeds <see cref="MaxNameLength"/> characters, or description exceeds <see cref="MaxDescriptionLength"/>.</exception>
    public static Product Create(
        Sku sku,
        string name,
        Money basePrice,
        CategoryId categoryId,
        string? description = null)
    {
        ArgumentNullException.ThrowIfNull(sku);
        ArgumentNullException.ThrowIfNull(basePrice);
        ArgumentNullException.ThrowIfNull(categoryId);

        string validatedName = ValidateName(name);
        string? validatedDescription = ValidateDescription(description);

        return new Product(
            ProductId.New(),
            sku,
            validatedName,
            validatedDescription,
            basePrice,
            categoryId);
    }

    /// <summary>
    /// Updates the core descriptive details and category of the product.
    /// </summary>
    /// <param name="name">The updated product name.</param>
    /// <param name="description">The updated optional description.</param>
    /// <param name="categoryId">The updated category identifier.</param>
    /// <exception cref="ArgumentNullException"><paramref name="categoryId"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidProductNameException"><paramref name="name"/> is null, empty, or exceeds <see cref="MaxNameLength"/> characters.</exception>
    public void UpdateDetails(string name, string? description, CategoryId categoryId)
    {
        ArgumentNullException.ThrowIfNull(categoryId);

        Name = ValidateName(name);
        Description = ValidateDescription(description);
        CategoryId = categoryId;
    }

    /// <summary>
    /// Changes the base price of the product.
    /// </summary>
    /// <param name="newPrice">The new base price.</param>
    /// <exception cref="ArgumentNullException"><paramref name="newPrice"/> is <see langword="null"/>.</exception>
    public void ChangePrice(Money newPrice)
    {
        ArgumentNullException.ThrowIfNull(newPrice);
        BasePrice = newPrice;
    }

    /// <summary>
    /// Changes the SKU of the product.
    /// </summary>
    /// <param name="newSku">The new SKU.</param>
    /// <exception cref="ArgumentNullException"><paramref name="newSku"/> is <see langword="null"/>.</exception>
    public void ChangeSku(Sku newSku)
    {
        ArgumentNullException.ThrowIfNull(newSku);
        Sku = newSku;
    }

    /// <summary>
    /// Changes the category of the product.
    /// </summary>
    /// <param name="newCategoryId">The new category identifier.</param>
    /// <exception cref="ArgumentNullException"><paramref name="newCategoryId"/> is <see langword="null"/>.</exception>
    public void ChangeCategory(CategoryId newCategoryId)
    {
        ArgumentNullException.ThrowIfNull(newCategoryId);
        CategoryId = newCategoryId;
    }

    private static string ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidProductNameException("A product name is required.");
        }

        string trimmed = name.Trim();
        if (trimmed.Length > MaxNameLength)
        {
            throw new InvalidProductNameException($"A product name cannot exceed {MaxNameLength} characters.");
        }

        return trimmed;
    }

    private static string? ValidateDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        string trimmed = description.Trim();
        if (trimmed.Length > MaxDescriptionLength)
        {
            throw new InvalidProductNameException($"A product description cannot exceed {MaxDescriptionLength} characters.");
        }

        return trimmed;
    }
}
