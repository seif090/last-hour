using FluentValidation;

namespace LastHour.Modules.Notifications.Application.Commands;

/// <summary>
/// Validator for <see cref="RequeueNotificationDeliveriesCommand"/>.
/// </summary>
public sealed class RequeueNotificationDeliveriesCommandValidator : AbstractValidator<RequeueNotificationDeliveriesCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequeueNotificationDeliveriesCommandValidator"/> class.
    /// </summary>
    public RequeueNotificationDeliveriesCommandValidator()
    {
        RuleFor(c => c.DeliveryIds)
            .NotEmpty()
            .WithMessage("At least one DeliveryId must be provided.")
            .Must(ids => ids.Count <= 100)
            .WithMessage("Batch requeue size cannot exceed 100 deliveries.")
            .Must(ids => !ids.Contains(Guid.Empty))
            .WithMessage("DeliveryIds collection cannot contain Guid.Empty.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("DeliveryIds collection cannot contain duplicate identifiers.");

        RuleFor(c => c.AdminUserId)
            .NotEmpty()
            .WithMessage("AdminUserId cannot be empty.");

        RuleFor(c => c.Reason)
            .NotEmpty()
            .WithMessage("Reason for requeuing is required.")
            .MaximumLength(500)
            .WithMessage("Reason cannot exceed 500 characters.");

        RuleFor(c => c.CorrelationId)
            .MaximumLength(128)
            .WithMessage("CorrelationId cannot exceed 128 characters.");
    }
}
