using System.Linq.Expressions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Catalog.Domain.Categories;
using LastHour.Modules.Catalog.Domain.Products;

namespace LastHour.Modules.Catalog.Application.Persistence;

/// <summary>
/// The <see cref="IProductRepository"/> implementation, composed purely from the shared repository contracts.
/// </summary>
public sealed class ProductRepository : IProductRepository
{
    private readonly IRepository<Product> _repository;
    private readonly IReadRepository<Product, ProductId> _readRepository;
    private readonly ITrackedRepository<Product, ProductId> _trackedRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductRepository"/> class.
    /// </summary>
    /// <param name="repository">The shared write repository for the <see cref="Product"/> aggregate.</param>
    /// <param name="readRepository">The shared read repository for <see cref="Product"/> entities.</param>
    /// <param name="trackedRepository">The shared tracked repository for loading <see cref="Product"/> into the change tracker.</param>
    public ProductRepository(
        IRepository<Product> repository,
        IReadRepository<Product, ProductId> readRepository,
        ITrackedRepository<Product, ProductId> trackedRepository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(readRepository);
        ArgumentNullException.ThrowIfNull(trackedRepository);
        _repository = repository;
        _readRepository = readRepository;
        _trackedRepository = trackedRepository;
    }

    /// <inheritdoc/>
    public void Add(Product aggregateRoot) => _repository.Add(aggregateRoot);

    /// <inheritdoc/>
    public void Update(Product aggregateRoot) => _repository.Update(aggregateRoot);

    /// <inheritdoc/>
    public void Remove(Product aggregateRoot) => _repository.Remove(aggregateRoot);

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<Product> aggregateRoots) => _repository.RemoveRange(aggregateRoots);

    /// <inheritdoc/>
    public Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default)
        => _readRepository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<Product?> GetByIdTrackedAsync(ProductId id, CancellationToken cancellationToken = default)
        => _trackedRepository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> ExistsByIdAsync(ProductId id, CancellationToken cancellationToken = default)
        => _readRepository.ExistsAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> ExistsByNormalizedSkuAsync(string normalizedSku, CancellationToken cancellationToken = default)
        => _readRepository.AnyAsync(
            product => product.Sku.NormalizedValue == normalizedSku,
            cancellationToken);

    /// <inheritdoc/>
    public Task<Product?> GetByNormalizedSkuAsync(string normalizedSku, CancellationToken cancellationToken = default)
        => _readRepository.FirstOrDefaultAsync(
            product => product.Sku.NormalizedValue == normalizedSku,
            cancellationToken);

    /// <inheritdoc/>
    public Task<int> CountAsync(
        string? searchTerm,
        CategoryId? categoryId,
        CancellationToken cancellationToken = default)
        => _readRepository.CountAsync(BuildSearchPredicate(searchTerm, categoryId), cancellationToken);

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
        Expression<Func<Product, bool>> predicate = BuildSearchPredicate(searchTerm, categoryId);
        int skip = (pageNumber - 1) * pageSize;
        bool sortDescending = sortDirection == ProductSortDirection.Desc;

        return sortBy switch
        {
            ProductSortField.Name => _readRepository.ListAsync(
                predicate,
                product => product.Name,
                sortDescending,
                skip,
                pageSize,
                cancellationToken),
            ProductSortField.Price => _readRepository.ListAsync(
                predicate,
                product => product.BasePrice.Amount,
                sortDescending,
                skip,
                pageSize,
                cancellationToken),
            ProductSortField.Sku => _readRepository.ListAsync(
                predicate,
                product => product.Sku.Value,
                sortDescending,
                skip,
                pageSize,
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported product sort field."),
        };
    }

    private static Expression<Func<Product, bool>> BuildSearchPredicate(string? searchTerm, CategoryId? categoryId)
    {
        string? trimmedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

        if (categoryId is not null && trimmedSearch is not null)
        {
            CategoryId id = categoryId;
            string name = trimmedSearch;
            string normalized = name.ToUpperInvariant();
            return product => product.CategoryId == id &&
                (product.Name.Contains(name) || product.Sku.NormalizedValue.Contains(normalized));
        }

        if (categoryId is not null)
        {
            CategoryId id = categoryId;
            return product => product.CategoryId == id;
        }

        if (trimmedSearch is not null)
        {
            string name = trimmedSearch;
            string normalized = name.ToUpperInvariant();
            return product => product.Name.Contains(name) || product.Sku.NormalizedValue.Contains(normalized);
        }

        return product => true;
    }
}
