using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Domain.Products;

namespace LastHour.Modules.Catalog.Tests.Application.TestSupport;

/// <summary>
/// Deterministic in-memory stand-in for the product repository.
/// </summary>
public sealed class FakeProductRepository : IProductRepository
{
    private readonly Dictionary<ProductId, Product> _products = new();
    private readonly Dictionary<string, Product> _byNormalizedSku = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets all stored products, for assertions.
    /// </summary>
    public IReadOnlyCollection<Product> All => _products.Values;

    /// <summary>
    /// Gets the number of times ExistsByNormalizedSkuAsync was invoked.
    /// </summary>
    public int ExistsByNormalizedSkuCallsCount { get; private set; }

    /// <summary>
    /// Gets the number of times Remove was invoked.
    /// </summary>
    public int RemoveCallsCount { get; private set; }

    /// <summary>
    /// Creates and stores a product with the given details.
    /// </summary>
    /// <param name="sku">The product SKU code.</param>
    /// <param name="name">The product name.</param>
    /// <param name="priceAmount">The base price amount.</param>
    /// <param name="priceCurrency">The ISO 4217 currency code.</param>
    /// <param name="categoryId">The category identifier.</param>
    /// <param name="description">An optional description.</param>
    /// <returns>The created product.</returns>
    public Product AddProduct(
        string sku,
        string name,
        decimal priceAmount,
        string priceCurrency,
        CategoryId categoryId,
        string? description = null)
    {
        Product product = Product.Create(
            Sku.From(sku),
            name,
            Money.Create(priceAmount, priceCurrency),
            categoryId,
            description);

        Add(product);
        return product;
    }

    /// <inheritdoc/>
    public void Add(Product aggregateRoot)
    {
        _products[aggregateRoot.Id] = aggregateRoot;
        _byNormalizedSku[aggregateRoot.Sku.NormalizedValue] = aggregateRoot;
    }

    /// <inheritdoc/>
    public void Update(Product aggregateRoot)
    {
        _products[aggregateRoot.Id] = aggregateRoot;
        _byNormalizedSku[aggregateRoot.Sku.NormalizedValue] = aggregateRoot;
    }

    /// <inheritdoc/>
    public void Remove(Product aggregateRoot)
    {
        RemoveCallsCount++;
        _products.Remove(aggregateRoot.Id);
        _byNormalizedSku.Remove(aggregateRoot.Sku.NormalizedValue);
    }

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<Product> aggregateRoots)
    {
        foreach (Product product in aggregateRoots)
        {
            Remove(product);
        }
    }

    /// <inheritdoc/>
    public Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_products.GetValueOrDefault(id) is { IsDeleted: false } product ? product : null);

    /// <inheritdoc/>
    public Task<Product?> GetByIdTrackedAsync(ProductId id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> ExistsByIdAsync(ProductId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_products.GetValueOrDefault(id) is { IsDeleted: false });

    /// <inheritdoc/>
    public Task<bool> ExistsByNormalizedSkuAsync(string normalizedSku, CancellationToken cancellationToken = default)
    {
        ExistsByNormalizedSkuCallsCount++;
        return Task.FromResult(_byNormalizedSku.GetValueOrDefault(normalizedSku) is { IsDeleted: false });
    }

    /// <inheritdoc/>
    public Task<Product?> GetByNormalizedSkuAsync(string normalizedSku, CancellationToken cancellationToken = default)
        => Task.FromResult(_byNormalizedSku.GetValueOrDefault(normalizedSku) is { IsDeleted: false } product ? product : null);

    /// <inheritdoc/>
    public Task<int> CountAsync(
        string? searchTerm,
        CategoryId? categoryId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(ApplySearchFilters(searchTerm, categoryId).Count());

    /// <inheritdoc/>
    public Task<IReadOnlyList<Product>> SearchAsync(
        string? searchTerm,
        CategoryId? categoryId,
        ProductSortField sortBy,
        ProductSortDirection sortDirection,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Product> query = ApplySearchFilters(searchTerm, categoryId);

        query = sortBy switch
        {
            ProductSortField.Name => sortDirection == ProductSortDirection.Asc
                ? query.OrderBy(product => product.Name)
                : query.OrderByDescending(product => product.Name),
            ProductSortField.Price => sortDirection == ProductSortDirection.Asc
                ? query.OrderBy(product => product.BasePrice.Amount)
                : query.OrderByDescending(product => product.BasePrice.Amount),
            ProductSortField.Sku => sortDirection == ProductSortDirection.Asc
                ? query.OrderBy(product => product.Sku.Value)
                : query.OrderByDescending(product => product.Sku.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported product sort field."),
        };

        Product[] page = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return Task.FromResult<IReadOnlyList<Product>>(page);
    }

    private IEnumerable<Product> ApplySearchFilters(string? searchTerm, CategoryId? categoryId)
    {
        string? trimmedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

        IEnumerable<Product> query = _products.Values.Where(product => !product.IsDeleted);

        if (categoryId is not null)
        {
            CategoryId id = categoryId;
            query = query.Where(product => product.CategoryId == id);
        }

        if (trimmedSearch is not null)
        {
            string name = trimmedSearch;
            string normalized = name.ToUpperInvariant();
            query = query.Where(product =>
                product.Name.Contains(name) || product.Sku.NormalizedValue.Contains(normalized));
        }

        return query;
    }
}
