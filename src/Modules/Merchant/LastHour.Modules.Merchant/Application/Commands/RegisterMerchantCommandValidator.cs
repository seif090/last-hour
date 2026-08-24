using FluentValidation;
using LastHour.Modules.Merchant.Domain.Merchants;

namespace LastHour.Modules.Merchant.Application.Commands;

/// <summary>
/// Validates the structural shape of a merchant registration request.
/// </summary>
public sealed class RegisterMerchantCommandValidator : AbstractValidator<RegisterMerchantCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterMerchantCommandValidator"/> class.
    /// </summary>
    public RegisterMerchantCommandValidator()
    {
        RuleFor(command => command.OwnerId)
            .NotEmpty()
            .WithErrorCode("OwnerIdRequired")
            .WithMessage("An owner identifier is required.");

        RuleFor(command => command.Name)
            .NotEmpty()
            .WithErrorCode("MerchantNameRequired")
            .WithMessage("A merchant name is required.")
            .MaximumLength(MerchantName.MaxLength)
            .WithErrorCode("MerchantNameTooLong")
            .WithMessage($"A merchant name cannot exceed {MerchantName.MaxLength} characters.");

        RuleFor(command => command.Street)
            .NotEmpty()
            .WithErrorCode("StreetRequired")
            .WithMessage("A street address is required.")
            .MaximumLength(200)
            .WithErrorCode("StreetTooLong")
            .WithMessage("A street address cannot exceed 200 characters.");

        RuleFor(command => command.City)
            .NotEmpty()
            .WithErrorCode("CityRequired")
            .WithMessage("A city is required.")
            .MaximumLength(100)
            .WithErrorCode("CityTooLong")
            .WithMessage("A city cannot exceed 100 characters.");

        RuleFor(command => command.StateOrProvince)
            .MaximumLength(100)
            .WithErrorCode("StateOrProvinceTooLong")
            .WithMessage("A state or province cannot exceed 100 characters.");

        RuleFor(command => command.PostalCode)
            .MaximumLength(20)
            .WithErrorCode("PostalCodeTooLong")
            .WithMessage("A postal code cannot exceed 20 characters.");

        RuleFor(command => command.Country)
            .NotEmpty()
            .WithErrorCode("CountryRequired")
            .WithMessage("A country is required.")
            .MaximumLength(100)
            .WithErrorCode("CountryTooLong")
            .WithMessage("A country cannot exceed 100 characters.");

        RuleFor(command => command.Phone)
            .NotEmpty()
            .WithErrorCode("PhoneNumberRequired")
            .WithMessage("A phone number is required.")
            .MaximumLength(PhoneNumber.MaxLength)
            .WithErrorCode("PhoneNumberTooLong")
            .WithMessage($"A phone number cannot exceed {PhoneNumber.MaxLength} characters.");
    }
}
