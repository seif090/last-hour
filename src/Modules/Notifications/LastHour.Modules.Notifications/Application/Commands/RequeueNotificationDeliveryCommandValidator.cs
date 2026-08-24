using FluentValidation;

namespace LastHour.Modules.Notifications.Application.Commands;

/// <summary>
/// Validator for <see cref="RequeueNotificationDeliveryCommand"/>.
/// </summary>
public sealed class RequeueNotificationDeliveryCommandValidator : AbstractValidator<RequeueNotificationDeliveryCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequeueNotificationDeliveryCommandValidator"/> class.
    /// </summary>
    public RequeueNotificationDeliveryCommandValidator()
    {
        RuleFor(c => c.DeliveryId)
            .NotEmpty()
            .WithMessage("DeliveryId cannot be empty.");

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
