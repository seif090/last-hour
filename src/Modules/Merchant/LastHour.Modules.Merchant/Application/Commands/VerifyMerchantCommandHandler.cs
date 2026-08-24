using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Merchant.Domain.Merchants;
using Microsoft.Extensions.Logging;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Application.Commands;

/// <summary>
/// Handles verifying a pending merchant aggregate root.
/// </summary>
public sealed class VerifyMerchantCommandHandler : ICommandHandler<VerifyMerchantCommand>
{
    private static readonly Action<ILogger, Guid, Exception?> MerchantVerified =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(5004, nameof(VerifyMerchantCommandHandler)),
            "Merchant {MerchantId} verified.");

    private readonly IMerchantRepository _merchants;
    private readonly ILogger<VerifyMerchantCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="VerifyMerchantCommandHandler"/> class.
    /// </summary>
    /// <param name="merchants">The merchant repository.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public VerifyMerchantCommandHandler(
        IMerchantRepository merchants,
        ILogger<VerifyMerchantCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(merchants);
        ArgumentNullException.ThrowIfNull(logger);
        _merchants = merchants;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        VerifyMerchantCommand command,
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
            merchant.Verify();
        }
        catch (BusinessRuleValidationException exception)
        {
            return Result.Failure(
                Error.Validation("Merchants.InvalidStatusTransition", exception.Message));
        }

        MerchantVerified(_logger, merchant.Id.Value, null);
        return Result.Success();
    }
}
