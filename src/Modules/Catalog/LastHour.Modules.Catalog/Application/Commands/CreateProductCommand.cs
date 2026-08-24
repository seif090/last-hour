using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.Modules.Catalog.Domain.Products;

namespace LastHour.Modules.Catalog.Application.Commands;

/// <summary>
/// Creates a new product in the catalog.
/// </summary>
public sealed record CreateProductCommand : ICommand<ProductId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProductCommand"/> class.
    /// </summary>
    /// <param name="sku">The product Stock Keeping Unit (SKU).</param>
    /// <param name="name">The product name.</param>
    /// <param name="priceAmount">The base price amount.</param>
    /// <param name="priceCurrency">The ISO 4217 currency code.</param>
    /// <param name="categoryId">The identifier of the category this product belongs to.</param>
    /// <param name="description">An optional product description.</param>
    public CreateProductCommand(
        string sku,
        string name,
        decimal priceAmount,
        string priceCurrency,
        Guid categoryId,
        string? description = null)
    {
        Sku = sku;
        Name = name;
        PriceAmount = priceAmount;
        PriceCurrency = priceCurrency;
        CategoryId = categoryId;
        Description = description;
    }

    /// <summary>
    /// Gets the product SKU.
    /// </summary>
    public string Sku { get; }

    /// <summary>
    /// Gets the product name.
    /// </summary>
    public string Name { get; }

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

    /// <summary>
    /// Gets the optional product description.
    /// </summary>
    public string? Description { get; }
}
