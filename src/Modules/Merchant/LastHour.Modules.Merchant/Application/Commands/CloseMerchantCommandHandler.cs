using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Merchant.Domain.Merchants;
using Microsoft.Extensions.Logging;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Application.Commands;

/// <summary>
/// Handles closing a merchant aggregate root permanently.
/// </summary>
public sealed class CloseMerchantCommandHandler : ICommandHandler<CloseMerchantCommand>
{
    private static readonly Action<ILogger, Guid, Exception?> MerchantClosed =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(5007, nameof(CloseMerchantCommandHandler)),
            "Merchant {MerchantId} closed.");

    private readonly IMerchantRepository _merchants;
    private readonly ILogger<CloseMerchantCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CloseMerchantCommandHandler"/> class.
    /// </summary>
    /// <param name="merchants">The merchant repository.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public CloseMerchantCommandHandler(
        IMerchantRepository merchants,
        ILogger<CloseMerchantCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(merchants);
        ArgumentNullException.ThrowIfNull(logger);
        _merchants = merchants;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        CloseMerchantCommand command,
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
            merchant.Close();
        }
        catch (BusinessRuleValidationException exception)
        {
            return Result.Failure(
                Error.Validation("Merchants.InvalidStatusTransition", exception.Message));
        }

        MerchantClosed(_logger, merchant.Id.Value, null);
        return Result.Success();
    }
}
