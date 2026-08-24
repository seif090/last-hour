using System.Linq.Expressions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.Modules.Merchant.Application.Models;
using LastHour.Modules.Merchant.Domain.Merchants;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Application.Persistence;

/// <summary>
/// The <see cref="IMerchantRepository"/> implementation, composed purely from the shared repository contracts.
/// </summary>
public sealed class MerchantRepository : IMerchantRepository
{
    private readonly IRepository<MerchantAggregate> _repository;
    private readonly IReadRepository<MerchantAggregate, MerchantId> _readRepository;
    private readonly ITrackedRepository<MerchantAggregate, MerchantId> _trackedRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="MerchantRepository"/> class.
    /// </summary>
    /// <param name="repository">The shared write repository for the <see cref="MerchantAggregate"/> aggregate.</param>
    /// <param name="readRepository">The shared read repository for <see cref="MerchantAggregate"/> entities.</param>
    /// <param name="trackedRepository">The shared tracked repository for loading <see cref="MerchantAggregate"/> into the change tracker.</param>
    public MerchantRepository(
        IRepository<MerchantAggregate> repository,
        IReadRepository<MerchantAggregate, MerchantId> readRepository,
        ITrackedRepository<MerchantAggregate, MerchantId> trackedRepository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(readRepository);
        ArgumentNullException.ThrowIfNull(trackedRepository);
        _repository = repository;
        _readRepository = readRepository;
        _trackedRepository = trackedRepository;
    }

    /// <inheritdoc/>
    public void Add(MerchantAggregate aggregateRoot) => _repository.Add(aggregateRoot);

    /// <inheritdoc/>
    public void Update(MerchantAggregate aggregateRoot) => _repository.Update(aggregateRoot);

    /// <inheritdoc/>
    public void Remove(MerchantAggregate aggregateRoot) => _repository.Remove(aggregateRoot);

    /// <inheritdoc/>
    public void RemoveRange(IEnumerable<MerchantAggregate> aggregateRoots) => _repository.RemoveRange(aggregateRoots);

    /// <inheritdoc/>
    public Task<MerchantAggregate?> GetByIdAsync(MerchantId id, CancellationToken cancellationToken = default)
        => _readRepository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<MerchantAggregate?> GetByIdTrackedAsync(MerchantId id, CancellationToken cancellationToken = default)
        => _trackedRepository.GetByIdAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> ExistsByIdAsync(MerchantId id, CancellationToken cancellationToken = default)
        => _readRepository.ExistsAsync(id, cancellationToken);

    /// <inheritdoc/>
    public Task<bool> ExistsByNameAsync(MerchantName name, CancellationToken cancellationToken = default)
        => _readRepository.AnyAsync(
            merchant => merchant.Name.Value == name.Value,
            cancellationToken);

    /// <inheritdoc/>
    public Task<int> CountAsync(
        string? searchTerm,
        MerchantStatus? status,
        CancellationToken cancellationToken = default)
        => _readRepository.CountAsync(BuildSearchPredicate(searchTerm, status), cancellationToken);

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
        Expression<Func<MerchantAggregate, bool>> predicate = BuildSearchPredicate(searchTerm, status);
        int skip = (pageNumber - 1) * pageSize;
        bool sortDescending = sortDirection == MerchantSortDirection.Desc;

        return sortBy switch
        {
            MerchantSortField.Name => _readRepository.ListAsync(
                predicate,
                merchant => merchant.Name.Value,
                sortDescending,
                skip,
                pageSize,
                cancellationToken),
            MerchantSortField.Status => _readRepository.ListAsync(
                predicate,
                merchant => merchant.Status,
                sortDescending,
                skip,
                pageSize,
                cancellationToken),
            MerchantSortField.CreatedAt => _readRepository.ListAsync(
                predicate,
                merchant => merchant.CreatedAt,
                sortDescending,
                skip,
                pageSize,
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, "Unsupported merchant sort field."),
        };
    }

    private static Expression<Func<MerchantAggregate, bool>> BuildSearchPredicate(
        string? searchTerm,
        MerchantStatus? status)
    {
        string? trimmedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

        if (status.HasValue && trimmedSearch is not null)
        {
            MerchantStatus targetStatus = status.Value;
            string term = trimmedSearch;
            return merchant => merchant.Status == targetStatus &&
                (merchant.Name.Value.Contains(term) || merchant.Address.City.Contains(term));
        }

        if (status.HasValue)
        {
            MerchantStatus targetStatus = status.Value;
            return merchant => merchant.Status == targetStatus;
        }

        if (trimmedSearch is not null)
        {
            string term = trimmedSearch;
            return merchant => merchant.Name.Value.Contains(term) || merchant.Address.City.Contains(term);
        }

        return _ => true;
    }
}
