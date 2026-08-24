using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Domain.Exceptions;
using LastHour.BuildingBlocks.SharedKernel.Results;
using LastHour.Modules.Merchant.Domain.Merchants;
using LastHour.Modules.Merchant.Domain.Merchants.Exceptions;
using Microsoft.Extensions.Logging;
using MerchantAggregate = LastHour.Modules.Merchant.Domain.Merchants.Merchant;

namespace LastHour.Modules.Merchant.Application.Commands;

/// <summary>
/// Handles registering a new merchant aggregate root.
/// </summary>
public sealed class RegisterMerchantCommandHandler : ICommandHandler<RegisterMerchantCommand, MerchantId>
{
    private static readonly Action<ILogger, Guid, string, Exception?> MerchantRegistered =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Information,
            new EventId(5001, nameof(RegisterMerchantCommandHandler)),
            "Merchant {MerchantId} registered with name {Name}.");

    private static readonly Action<ILogger, string, Exception?> DuplicateNameRejected =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(5002, nameof(RegisterMerchantCommandHandler)),
            "Merchant registration rejected because name {Name} already exists.");

    private readonly IMerchantRepository _merchants;
    private readonly ILogger<RegisterMerchantCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterMerchantCommandHandler"/> class.
    /// </summary>
    /// <param name="merchants">The merchant repository.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public RegisterMerchantCommandHandler(
        IMerchantRepository merchants,
        ILogger<RegisterMerchantCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(merchants);
        ArgumentNullException.ThrowIfNull(logger);
        _merchants = merchants;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<MerchantId>> Handle(
        RegisterMerchantCommand command,
        CancellationToken cancellationToken)
    {
        MerchantName merchantName;
        try
        {
            merchantName = MerchantName.From(command.Name);
        }
        catch (MerchantNameInvalidException exception)
        {
            return Result<MerchantId>.Failure(
                Error.Validation("InvalidMerchantName", exception.Message));
        }

        if (await _merchants.ExistsByNameAsync(merchantName, cancellationToken).ConfigureAwait(false))
        {
            DuplicateNameRejected(_logger, command.Name, null);
            return Result<MerchantId>.Failure(Error.Conflict(
                "Merchants.NameAlreadyExists",
                "A merchant with this name already exists."));
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
            return Result<MerchantId>.Failure(
                Error.Validation("InvalidAddress", exception.Message));
        }

        PhoneNumber phone;
        try
        {
            phone = PhoneNumber.From(command.Phone);
        }
        catch (InvalidPhoneNumberException exception)
        {
            return Result<MerchantId>.Failure(
                Error.Validation("InvalidPhoneNumber", exception.Message));
        }

        MerchantAggregate merchant;
        try
        {
            merchant = MerchantAggregate.Register(
                command.OwnerId,
                merchantName,
                address,
                phone);
        }
        catch (BusinessRuleValidationException exception)
        {
            return Result<MerchantId>.Failure(
                Error.Validation("Merchants.InvalidRegistration", exception.Message));
        }

        _merchants.Add(merchant);

        MerchantRegistered(_logger, merchant.Id.Value, merchant.Name.Value, null);
        return Result<MerchantId>.Success(merchant.Id);
    }
}
