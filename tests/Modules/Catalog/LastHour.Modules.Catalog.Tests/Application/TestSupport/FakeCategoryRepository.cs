using LastHour.Modules.Catalog.Domain.Categories;

namespace LastHour.Modules.Catalog.Tests.Application.TestSupport;

/// <summary>
/// Deterministic in-memory stand-in for the category repository.
/// </summary>
public sealed class FakeCategoryRepository : ICategoryRepository
{
    private readonly Dictionary<CategoryId, Category> _categories = new();

    /// <summary>
    /// Gets all stored categories, for assertions.
    /// </summary>
    public IReadOnlyCollection<Category> All => _categories.Values;

    /// <summary>
    /// Creates and stores a category with the given details.
    /// </summary>
    /// <param name="name">The category name.</param>
    /// <param name="description">An optional description.</param>
    /// <param name="parentCategoryId">An optional parent category identifier.</param>
    /// <returns>The created category.</returns>
    public Category AddCategory(string name, string? description = null, CategoryId? parentCategoryId = null)
    {
        Category category = Category.Create(name, description, parentCategoryId);
        Add(category);
        return category;
    }

    /// <inheritdoc/>
    public void Add(Category aggregateRoot)
    {
        _categories[aggregateRoot.Id] = aggregateRoot;
    }

    /// <inheritdoc/>
    public void Update(Category aggregateRoot)
    {
        _categories[aggregateRoot.Id] = aggregateRoot;
    }

    /// <inheritdoc/>
    public void Remove(Category aggregateRoot)
    {
        _categories.Remove(aggregateRoot.Id);
    }

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<Category> aggregateRoots)
    {
        foreach (Category category in aggregateRoots)
        {
            Remove(category);
        }
    }

    /// <inheritdoc/>
    public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_categories.GetValueOrDefault(id));

    /// <inheritdoc/>
    public Task<Category?> GetByIdTrackedAsync(CategoryId id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> ExistsByIdAsync(CategoryId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_categories.ContainsKey(id));

    /// <inheritdoc/>
    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Category>>(_categories.Values.ToArray());
}
