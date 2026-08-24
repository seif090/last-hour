using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

namespace LastHour.Modules.Catalog.Domain.Products;

/// <summary>
/// The stable, immutable identifier of a <see cref="Product"/>. Wraps a <see cref="Guid"/> and
/// is persisted as a PostgreSQL <c>uuid</c> column through the shared strongly typed id
/// value converters.
/// </summary>
public sealed class ProductId : StronglyTypedId<Guid, ProductId>, IStronglyTypedId<Guid, ProductId>
{
    private ProductId(Guid value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a product id that wraps <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> value to wrap.</param>
    /// <returns>A new <see cref="ProductId"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static ProductId Create(Guid value) => new ProductId(value);

    /// <summary>
    /// Creates a product id wrapping a newly generated <see cref="Guid"/>.
    /// </summary>
    /// <returns>A new <see cref="ProductId"/>.</returns>
    public static ProductId New() => Create(Guid.NewGuid());
}
