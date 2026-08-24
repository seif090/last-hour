using FluentValidation;

namespace LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.Queries;

/// <summary>
/// Validates <see cref="GetOutboxMessagesQuery"/> parameters.
/// </summary>
public sealed class GetOutboxMessagesQueryValidator : AbstractValidator<GetOutboxMessagesQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetOutboxMessagesQueryValidator"/> class.
    /// </summary>
    public GetOutboxMessagesQueryValidator()
    {
        RuleFor(q => q.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page number must be greater than or equal to 1.");

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, GetOutboxMessagesQuery.MaxPageSize)
            .WithMessage($"Page size must be between 1 and {GetOutboxMessagesQuery.MaxPageSize}.");

        RuleFor(q => q)
            .Must(q => !q.OccurredFromUtc.HasValue || !q.OccurredToUtc.HasValue || q.OccurredFromUtc.Value <= q.OccurredToUtc.Value)
            .WithMessage("OccurredFromUtc must be less than or equal to OccurredToUtc.")
            .WithName("OccurredFromUtc");
    }
}
