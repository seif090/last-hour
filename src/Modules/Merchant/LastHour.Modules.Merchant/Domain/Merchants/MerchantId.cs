using LastHour.BuildingBlocks.SharedKernel.Domain.StronglyTypedIds;

namespace LastHour.Modules.Merchant.Domain.Merchants;

/// <summary>
/// The stable, immutable identifier of a merchant. Wraps a <see cref="Guid"/> and
/// is persisted as a PostgreSQL <c>uuid</c> column through the shared strongly typed id
/// value converters.
/// </summary>
public sealed class MerchantId : StronglyTypedId<Guid, MerchantId>, IStronglyTypedId<Guid, MerchantId>
{
    private MerchantId(Guid value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a merchant id that wraps <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The <see cref="Guid"/> value to wrap.</param>
    /// <returns>A new <see cref="MerchantId"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is <see cref="Guid.Empty"/>.</exception>
    public static MerchantId Create(Guid value) => new MerchantId(value);

    /// <summary>
    /// Creates a merchant id wrapping a newly generated <see cref="Guid"/>.
    /// </summary>
    /// <returns>A new <see cref="MerchantId"/>.</returns>
    public static MerchantId New() => Create(Guid.NewGuid());
}
