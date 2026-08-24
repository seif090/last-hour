using FluentValidation;

namespace LastHour.Modules.Notifications.Application.Commands;

/// <summary>
/// Validates the structural parameters of the <see cref="MarkNotificationAsReadCommand"/>.
/// </summary>
public sealed class MarkNotificationAsReadCommandValidator : AbstractValidator<MarkNotificationAsReadCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkNotificationAsReadCommandValidator"/> class.
    /// </summary>
    public MarkNotificationAsReadCommandValidator()
    {
        RuleFor(command => command.NotificationId)
            .NotEmpty()
            .WithErrorCode("NotificationIdRequired")
            .WithMessage("Notification identifier is required.");

        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithErrorCode("UserIdRequired")
            .WithMessage("User identifier is required.");
    }
}
