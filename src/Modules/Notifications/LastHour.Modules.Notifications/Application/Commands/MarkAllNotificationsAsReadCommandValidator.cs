using FluentValidation;

namespace LastHour.Modules.Notifications.Application.Commands;

/// <summary>
/// Validates the structural parameters of the <see cref="MarkAllNotificationsAsReadCommand"/>.
/// </summary>
public sealed class MarkAllNotificationsAsReadCommandValidator : AbstractValidator<MarkAllNotificationsAsReadCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MarkAllNotificationsAsReadCommandValidator"/> class.
    /// </summary>
    public MarkAllNotificationsAsReadCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithErrorCode("UserIdRequired")
            .WithMessage("User identifier is required.");
    }
}
