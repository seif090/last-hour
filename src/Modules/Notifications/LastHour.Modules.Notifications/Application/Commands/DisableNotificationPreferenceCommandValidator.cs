using FluentValidation;

namespace LastHour.Modules.Notifications.Application.Commands;

/// <summary>
/// FluentValidation validator for <see cref="DisableNotificationPreferenceCommand"/>.
/// </summary>
public sealed class DisableNotificationPreferenceCommandValidator : AbstractValidator<DisableNotificationPreferenceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DisableNotificationPreferenceCommandValidator"/> class.
    /// </summary>
    public DisableNotificationPreferenceCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User identifier is required.");

        RuleFor(x => x.NotificationType)
            .NotEmpty()
            .MaximumLength(128)
            .WithMessage("Notification type must be specified and cannot exceed 128 characters.");

        RuleFor(x => x.Channel)
            .IsInEnum()
            .WithMessage("Delivery channel must be a valid channel.");
    }
}
