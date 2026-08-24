using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

namespace LastHour.Modules.Catalog.Domain.Categories;

/// <summary>
/// The stable, immutable identifier of a <see cref="Category"/>. Wraps a <see cref="Guid"/> and
/// is persisted as a PostgreSQL <c>uuid</c> column through the shared strongly typed id
/// value converters.
/// </summary>
public sealed class CategoryId : StronglyTypedId<Guid, CategoryId>, IStronglyTypedId<Guid, CategoryId>
{
    private CategoryId(Guid value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a category id that wraps <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> value to wrap.</param>
    /// <returns>A new <see cref="CategoryId"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static CategoryId Create(Guid value) => new CategoryId(value);

    /// <summary>
    /// Creates a category id wrapping a newly generated <see cref="Guid"/>.
    /// </summary>
    /// <returns>A new <see cref="CategoryId"/>.</returns>
    public static CategoryId New() => Create(Guid.NewGuid());
}
