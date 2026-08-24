using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Merchant.Domain.Merchants;
using LastHour.Modules.Merchant.Domain.Merchants.Exceptions;
using Microsoft.Extensions.Logging;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Application.Commands;

/// <summary>
/// Handles updating the profile of an existing merchant.
/// </summary>
public sealed class UpdateMerchantProfileCommandHandler : ICommandHandler<UpdateMerchantProfileCommand>
{
    private static readonly Action<ILogger, Guid, Exception?> ProfileUpdated =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(5003, nameof(UpdateMerchantProfileCommandHandler)),
            "Merchant {MerchantId} profile updated.");

    private static readonly Action<ILogger, string, Exception?> DuplicateNameRejected =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(5004, nameof(UpdateMerchantProfileCommandHandler)),
            "Merchant profile update rejected because name {Name} already exists.");

    private readonly IMerchantRepository _merchants;
    private readonly ILogger<UpdateMerchantProfileCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateMerchantProfileCommandHandler"/> class.
    /// </summary>
    /// <param name="merchants">The merchant repository.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public UpdateMerchantProfileCommandHandler(
        IMerchantRepository merchants,
        ILogger<UpdateMerchantProfileCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(merchants);
        ArgumentNullException.ThrowIfNull(logger);
        _merchants = merchants;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> Handle(
        UpdateMerchantProfileCommand command,
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

        if (merchant.OwnerId != command.CallerUserId)
        {
            return Result.Failure(Error.Forbidden(
                "Merchants.Forbidden",
                "You do not own this merchant account."));
        }

        MerchantName merchantName;
        try
        {
            merchantName = MerchantName.From(command.Name);
        }
        catch (MerchantNameInvalidException exception)
        {
            return Result.Failure(
                Error.Validation("InvalidMerchantName", exception.Message));
        }

        if (merchant.Name != merchantName)
        {
            bool nameExists = await _merchants.ExistsByNameAsync(
                merchantName,
                cancellationToken).ConfigureAwait(false);

            if (nameExists)
            {
                DuplicateNameRejected(_logger, command.Name, null);
                return Result.Failure(
                    Error.Conflict(
                        "Merchants.NameAlreadyExists",
                        "A merchant with this name already exists."));
            }
        }

        Address address;
        try
        {
            address = Address.Create(
                command.Street,
                command.City,
                command.StateOrProvince,
                command.PostalCode,
                command.Country);
        }
        catch (InvalidAddressException exception)
        {
            return Result.Failure(
                Error.Validation("InvalidAddress", exception.Message));
        }

        PhoneNumber phone;
        try
        {
            phone = PhoneNumber.From(command.Phone);
        }
        catch (InvalidPhoneNumberException exception)
        {
            return Result.Failure(
                Error.Validation("InvalidPhoneNumber", exception.Message));
        }

        bool isUpdated = merchant.UpdateProfile(merchantName, address, phone);
        if (isUpdated)
        {
            _merchants.Update(merchant);
        }

        ProfileUpdated(_logger, merchant.Id.Value, null);
        return Result.Success();
    }
}
