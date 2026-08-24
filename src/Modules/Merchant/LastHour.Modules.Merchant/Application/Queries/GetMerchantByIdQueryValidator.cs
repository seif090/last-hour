using FluentValidation;

namespace LastHour.Modules.Merchant.Application.Queries;

/// <summary>
/// Validates the structural shape of a query for a merchant by identifier.
/// </summary>
public sealed class GetMerchantByIdQueryValidator : AbstractValidator<GetMerchantByIdQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetMerchantByIdQueryValidator"/> class.
    /// </summary>
    public GetMerchantByIdQueryValidator()
    {
        RuleFor(query => query.MerchantId)
            .NotEmpty()
            .WithErrorCode("MerchantIdRequired")
            .WithMessage("A merchant identifier is required.");
    }
}
