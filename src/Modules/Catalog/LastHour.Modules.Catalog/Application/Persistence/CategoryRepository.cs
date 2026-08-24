using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Catalog.Domain.Categories;

namespace LastHour.Modules.Catalog.Application.Persistence;

/// <summary>
/// The <see cref="ICategoryRepository"/> implementation, composed purely from the shared repository contracts.
/// </summary>
public sealed class CategoryRepository : ICategoryRepository
{
    private readonly IRepository<Category> _repository;
    private readonly IReadRepository<Category, CategoryId> _readRepository;
    private readonly ITrackedRepository<Category, CategoryId> _trackedRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryRepository"/> class.
    /// </summary>
    /// <param name="repository">The shared write repository for the <see cref="Category"/> aggregate.</param>
    /// <param name="readRepository">The shared read repository for <see cref="Category"/> entities.</param>
    /// <param name="trackedRepository">The shared tracked repository for loading <see cref="Category"/> into the change tracker.</param>
    public CategoryRepository(
        IRepository<Category> repository,
        IReadRepository<Category, CategoryId> readRepository,
        ITrackedRepository<Category, CategoryId> trackedRepository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(readRepository);
        ArgumentNullException.ThrowIfNull(trackedRepository);
        _repository = repository;
        _readRepository = readRepository;
        _trackedRepository = trackedRepository;
    }

    /// <inheritdoc/>
    public void Add(Category aggregateRoot) => _repository.Add(aggregateRoot);

    /// <inheritdoc/>
    public void Update(Category aggregateRoot) => _repository.Update(aggregateRoot);

    /// <inheritdoc/>
    public void Remove(Category aggregateRoot) => _repository.Remove(aggregateRoot);

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<Category> aggregateRoots) => _repository.RemoveRange(aggregateRoots);

    /// <inheritdoc/>
    public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default)
        => _readRepository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<Category?> GetByIdTrackedAsync(CategoryId id, CancellationToken cancellationToken = default)
        => _trackedRepository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> ExistsByIdAsync(CategoryId id, CancellationToken cancellationToken = default)
        => _readRepository.ExistsAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        => _readRepository.ListAsync(cancellationToken);
}
