using LastHour.BuildingBlocks.Application.Contracts;

namespace LastHour.Modules.Catalog.Domain.Categories;

/// <summary>
/// The persistence contract of the <see cref="Category"/> aggregate.
/// </summary>
public interface ICategoryRepository : IRepository<Category>
{
    /// <summary>
    /// Retrieves the category with the specified identifier, or <see langword="null"/> when no such category exists.
    /// </summary>
    /// <param name="id">The category identifier to look up.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching category, or <see langword="null"/> when not found.</returns>
    Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the category with the specified identifier and places the aggregate in the change tracker,
    /// or <see langword="null"/> when no such category exists.
    /// </summary>
    /// <param name="id">The category identifier to look up.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The matching, tracked category, or <see langword="null"/> when not found.</returns>
    Task<Category?> GetByIdTrackedAsync(CategoryId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a category with the specified identifier exists.
    /// </summary>
    /// <param name="id">The category identifier to check.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns><see langword="true"/> when the category exists; otherwise <see langword="false"/>.</returns>
    Task<bool> ExistsByIdAsync(CategoryId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all categories.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A read-only list of all categories.</returns>
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);
}
