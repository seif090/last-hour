namespace LastHour.Modules.Catalog.Application.Models;

/// <summary>
/// The safe projection of a product returned to read callers. The monetary value is flattened
/// into its amount and ISO currency code, and the SKU into its display value. Internal audit
/// and soft-delete state is never exposed.
/// </summary>
public sealed record ProductDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProductDto"/> class.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="sku">The product SKU display value.</param>
    /// <param name="name">The product name.</param>
    /// <param name="description">An optional description.</param>
    /// <param name="priceAmount">The base price amount.</param>
    /// <param name="priceCurrency">The ISO 4217 currency code.</param>
    /// <param name="categoryId">The category identifier.</param>
    public ProductDto(
        Guid id,
        string sku,
        string name,
        string? description,
        decimal priceAmount,
        string priceCurrency,
        Guid categoryId)
    {
        Id = id;
        Sku = sku;
        Name = name;
        Description = description;
        PriceAmount = priceAmount;
        PriceCurrency = priceCurrency;
        CategoryId = categoryId;
    }

    /// <summary>
    /// Gets the product identifier.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Gets the product SKU display value.
    /// </summary>
    public string Sku { get; }

    /// <summary>
    /// Gets the product name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the optional description.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// Gets the base price amount.
    /// </summary>
    public decimal PriceAmount { get; }

    /// <summary>
    /// Gets the ISO 4217 currency code.
    /// </summary>
    public string PriceCurrency { get; }

    /// <summary>
    /// Gets the category identifier.
    /// </summary>
    public Guid CategoryId { get; }
}
