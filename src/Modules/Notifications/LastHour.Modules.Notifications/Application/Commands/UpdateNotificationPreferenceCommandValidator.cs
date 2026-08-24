using FluentValidation;

namespace LastHour.Modules.Notifications.Application.Commands;

/// <summary>
/// FluentValidation validator for <see cref="UpdateNotificationPreferenceCommand"/>.
/// </summary>
public sealed class UpdateNotificationPreferenceCommandValidator : AbstractValidator<UpdateNotificationPreferenceCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateNotificationPreferenceCommandValidator"/> class.
    /// </summary>
    public UpdateNotificationPreferenceCommandValidator()
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
