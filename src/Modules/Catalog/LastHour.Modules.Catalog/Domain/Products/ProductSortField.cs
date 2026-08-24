namespace LastHour.Modules.Catalog.Domain.Products;

/// <summary>
/// Identifies the product attribute used to order search results. Only a small, stable set of
/// sortable attributes is supported so ordering is always expressed as an explicit mapping
/// rather than arbitrary dynamic expressions.
/// </summary>
public enum ProductSortField
{
    /// <summary>
    /// Orders by the product name.
    /// </summary>
    Name,

    /// <summary>
    /// Orders by the base price amount.
    /// </summary>
    Price,

    /// <summary>
    /// Orders by the product SKU.
    /// </summary>
    Sku,
}
