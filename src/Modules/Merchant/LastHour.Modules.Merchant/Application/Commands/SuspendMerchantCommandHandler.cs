using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Merchant.Domain.Merchants;
using Microsoft.Extensions.Logging;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Application.Commands;

/// <summary>
/// Handles suspending an active merchant aggregate root.
/// </summary>
public sealed class SuspendMerchantCommandHandler : ICommandHandler<SuspendMerchantCommand>
{
    private static readonly Action<ILogger, Guid, Exception?> MerchantSuspended =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(5005, nameof(SuspendMerchantCommandHandler)),
            "Merchant {MerchantId} suspended.");

    private readonly IMerchantRepository _merchants;
    private readonly ILogger<SuspendMerchantCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SuspendMerchantCommandHandler"/> class.
    /// </summary>
    /// <param name="merchants">The merchant repository.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public SuspendMerchantCommandHandler(
        IMerchantRepository merchants,
        ILogger<SuspendMerchantCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(merchants);
        ArgumentNullException.ThrowIfNull(logger);
        _merchants = merchants;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        SuspendMerchantCommand command,
        CancellationToken cancellationToken)
    {
        MerchantId merchantId = MerchantId.Create(command.MerchantId);
        MerchantAggregate? merchant = await _merchants
            .GetByIdTrackedAsync(merchantId, cancellationToken)
            .ConfigureAwait(false);

        if (merchant is null)
        {
            return Result.Failure(Error.NotFound(
                "Merchants.NotFound",
                "The merchant could not be found."));
        }

        try
        {
            merchant.Suspend();
        }
        catch (BusinessRuleValidationException exception)
        {
            return Result.Failure(
                Error.Validation("Merchants.InvalidStatusTransition", exception.Message));
        }

        MerchantSuspended(_logger, merchant.Id.Value, null);
        return Result.Success();
    }
}
