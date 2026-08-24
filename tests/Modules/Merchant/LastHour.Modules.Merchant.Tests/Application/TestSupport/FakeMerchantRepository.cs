using LastHour.Modules.Merchant.Application.Models;
using LastHour.Modules.Merchant.Domain.Merchants;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Tests.Application.TestSupport;

/// <summary>
/// Deterministic in-memory stand-in for the merchant repository.
/// </summary>
public sealed class FakeMerchantRepository : IMerchantRepository
{
    private readonly Dictionary<MerchantId, MerchantAggregate> _merchants = new();

    /// <summary>
    /// Gets all stored merchants, for assertions.
    /// </summary>
    public IReadOnlyCollection<MerchantAggregate> All => _merchants.Values;

    /// <summary>
    /// Creates and stores a merchant with the given details.
    /// </summary>
    /// <param name="name">The merchant name.</param>
    /// <param name="city">The city name.</param>
    /// <param name="ownerId">An optional owner identifier.</param>
    /// <param name="street">An optional street address.</param>
    /// <param name="stateOrProvince">An optional state or province.</param>
    /// <param name="postalCode">An optional postal code.</param>
    /// <param name="country">An optional country.</param>
    /// <param name="phone">An optional phone number.</param>
    /// <returns>The created merchant.</returns>
    public MerchantAggregate AddMerchant(
        string name,
        string city = "New York",
        Guid? ownerId = null,
        string street = "123 Main St",
        string? stateOrProvince = "NY",
        string? postalCode = "10001",
        string country = "USA",
        string phone = "+1-555-0100")
    {
        MerchantAggregate merchant = MerchantAggregate.Register(
            ownerId ?? Guid.NewGuid(),
            MerchantName.From(name),
            Address.Create(street, city, stateOrProvince, postalCode, country),
            PhoneNumber.From(phone));

        Add(merchant);
        return merchant;
    }

    /// <summary>
    /// Gets the number of times Update was called.
    /// </summary>
    public int UpdateCount { get; private set; }

    /// <inheritdoc/>
    public void Add(MerchantAggregate aggregateRoot) => _merchants[aggregateRoot.Id] = aggregateRoot;

    /// <inheritdoc/>
    public void Update(MerchantAggregate aggregateRoot)
    {
        UpdateCount++;
        _merchants[aggregateRoot.Id] = aggregateRoot;
    }

    /// <inheritdoc/>
    public void Remove(MerchantAggregate aggregateRoot) => _merchants.Remove(aggregateRoot.Id);

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<MerchantAggregate> aggregateRoots)
    {
        foreach (MerchantAggregate merchant in aggregateRoots)
        {
            Remove(merchant);
        }
    }

    /// <inheritdoc/>
    public Task<MerchantAggregate?> GetByIdAsync(MerchantId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_merchants.GetValueOrDefault(id));

    /// <inheritdoc/>
    public Task<MerchantAggregate?> GetByIdTrackedAsync(MerchantId id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> ExistsByIdAsync(MerchantId id, CancellationToken cancellationToken = default)
        => Task.FromResult(_merchants.ContainsKey(id));

    /// <inheritdoc/>
    public Task<bool> ExistsByNameAsync(MerchantName name, CancellationToken cancellationToken = default)
        => Task.FromResult(_merchants.Values.Any(m => m.Name.Value.Equals(name.Value, StringComparison.OrdinalIgnoreCase)));

    /// <inheritdoc/>
    public Task<int> CountAsync(
        string? searchTerm,
        MerchantStatus? status,
        CancellationToken cancellationToken = default)
        => Task.FromResult(ApplySearchFilters(searchTerm, status).Count());

    /// <inheritdoc/>
    public Task<IReadOnlyList<MerchantAggregate>> SearchAsync(
        string? searchTerm,
        MerchantStatus? status,
        MerchantSortField sortBy,
        MerchantSortDirection sortDirection,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<MerchantAggregate> query = ApplySearchFilters(searchTerm, status);

        query = sortBy switch
        {
            MerchantSortField.Name => sortDirection == MerchantSortDirection.Asc
                ? query.OrderBy(m => m.Name.Value, StringComparer.OrdinalIgnoreCase)
                : query.OrderByDescending(m => m.Name.Value, StringComparer.OrdinalIgnoreCase),
            MerchantSortField.Status => sortDirection == MerchantSortDirection.Asc
                ? query.OrderBy(m => m.Status)
                : query.OrderByDescending(m => m.Status),
            MerchantSortField.CreatedAt => sortDirection == MerchantSortDirection.Asc
                ? query.OrderBy(m => m.CreatedAt)
                : query.OrderByDescending(m => m.CreatedAt),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported merchant sort field."),
        };

        MerchantAggregate[] page = query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return Task.FromResult<IReadOnlyList<MerchantAggregate>>(page);
    }

    private IEnumerable<MerchantAggregate> ApplySearchFilters(string? searchTerm, MerchantStatus? status)
    {
        string? trimmedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

        IEnumerable<MerchantAggregate> query = _merchants.Values;

        if (status.HasValue)
        {
            MerchantStatus targetStatus = status.Value;
            query = query.Where(m => m.Status == targetStatus);
        }

        if (trimmedSearch is not null)
        {
            string term = trimmedSearch;
            query = query.Where(m =>
                m.Name.Value.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                m.Address.City.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return query;
    }
}
