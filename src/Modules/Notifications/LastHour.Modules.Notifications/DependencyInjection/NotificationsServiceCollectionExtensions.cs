using FluentValidation;
using LastHour.Modules.Notifications.Application.Delivery;
using LastHour.Modules.Notifications.Application.Persistence;
using LastHour.Modules.Notifications.Application.Queries;
using LastHour.Modules.Notifications.Infrastructure.Delivery;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LastHour.Modules.Notifications.DependencyInjection;

/// <summary>
/// Registers Notifications module services with the dependency injection container.
/// </summary>
public static class NotificationsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Notifications module repositories, delivery pipeline, background processor, and dependencies.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same service collection, so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddLastHourNotifications(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationDeliveryRepository, NotificationDeliveryRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();
        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<INotificationQuietHoursRepository, NotificationQuietHoursRepository>();
        services.AddScoped<INotificationDeliveryAttemptRepository, NotificationDeliveryAttemptRepository>();
        services.AddScoped<INotificationDeliveryAuditRepository, NotificationDeliveryAuditRepository>();

        services.AddScoped<INotificationPreferenceResolver, NotificationPreferenceResolver>();
        services.AddScoped<INotificationTemplateResolver, NotificationTemplateResolver>();
        services.AddSingleton<INotificationTemplateRenderer, NotificationTemplateRenderer>();
        services.AddScoped<INotificationQuietHoursResolver, NotificationQuietHoursResolver>();
        services.AddScoped<INotificationDeliveryPolicyResolver, NotificationDeliveryPolicyResolver>();

        services.AddScoped<INotificationChannelSender, InAppNotificationChannelSender>();
        services.AddScoped<INotificationChannelSender, EmailNotificationChannelSender>();
        services.AddScoped<INotificationChannelSender, SmsNotificationChannelSender>();
        services.AddScoped<INotificationChannelFactory, NotificationChannelFactory>();

        services.AddOptions<NotificationDeliveryOptions>()
            .BindConfiguration("Notifications:Delivery");
        services.AddSingleton<IValidateOptions<NotificationDeliveryOptions>, NotificationDeliveryOptionsValidator>();

        services.AddScoped<LastHour.Modules.Notifications.Application.Retention.INotificationOperationsRetentionService, LastHour.Modules.Notifications.Application.Retention.NotificationOperationsRetentionService>();

        services.AddOptions<LastHour.Modules.Notifications.Application.Retention.NotificationOperationsRetentionOptions>()
            .BindConfiguration("Notifications:Retention");
        services.AddSingleton<IValidateOptions<LastHour.Modules.Notifications.Application.Retention.NotificationOperationsRetentionOptions>, LastHour.Modules.Notifications.Infrastructure.Retention.NotificationOperationsRetentionOptionsValidator>();

        services.AddSingleton<NotificationDeliveryMetrics>();
        services.AddSingleton<LastHour.Modules.Notifications.Application.Retention.INotificationOperationsRetentionMetrics, LastHour.Modules.Notifications.Infrastructure.Retention.NotificationOperationsRetentionMetrics>();

        services.AddHostedService<NotificationDeliveryProcessor>();
        services.AddHostedService<LastHour.Modules.Notifications.Infrastructure.Retention.NotificationOperationsRetentionProcessor>();

        services.AddValidatorsFromAssembly(typeof(GetMyNotificationsQuery).Assembly);

        return services;
    }
}
