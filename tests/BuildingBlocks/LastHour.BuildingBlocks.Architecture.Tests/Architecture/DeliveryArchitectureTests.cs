using System.Reflection;
using FluentAssertions;
using LastHour.Modules.Notifications.Application.Delivery;
using LastHour.Modules.Notifications.Application.Persistence;
using NetArchTest.Rules;

namespace LastHour.BuildingBlocks.Architecture.Tests.Architecture;

public class DeliveryArchitectureTests
{
    private static readonly Assembly NotificationsDomainAssembly = typeof(LastHour.Modules.Notifications.Domain.Notifications.Notification).Assembly;
    private static readonly Assembly NotificationsApplicationAssembly = typeof(INotificationDeliveryPolicyResolver).Assembly;

    [Fact]
    public void NotificationsDomain_DoesNotReferenceExternalProviderSdks()
    {
        // Act
        TestResult result = Types.InAssembly(NotificationsDomainAssembly)
            .That()
            .ResideInNamespace("LastHour.Modules.Notifications.Domain")
            .Should()
            .NotHaveDependencyOnAny("SendGrid", "Twilio", "Amazon", "Amazon.SimpleNotificationService", "MailKit", "MimeKit")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Notifications Domain must not depend on third-party provider SDKs.");
    }

    [Fact]
    public void NotificationsApplication_DoesNotReferenceConcreteProviderAdapters()
    {
        // Act
        TestResult result = Types.InAssembly(NotificationsApplicationAssembly)
            .That()
            .ResideInNamespace("LastHour.Modules.Notifications.Application")
            .Should()
            .NotHaveDependencyOnAny("LastHour.Modules.Notifications.Infrastructure")
            .GetResult();

        // Assert
        result.IsSuccessful.Should().BeTrue("Notifications Application must not depend on concrete Infrastructure adapters.");
    }

    [Fact]
    public void ProviderImplementations_ResideOnlyInInfrastructureDelivery()
    {
        Type senderInterface = typeof(INotificationChannelSender);

        List<Type> senders = NotificationsDomainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && senderInterface.IsAssignableFrom(t))
            .ToList();

        senders.Should().NotBeEmpty("There should be registered channel senders.");

        foreach (Type sender in senders)
        {
            sender.Namespace.Should().Be("LastHour.Modules.Notifications.Infrastructure.Delivery",
                $"Provider implementation '{sender.FullName}' must reside strictly in Infrastructure.Delivery.");
        }
    }

    [Fact]
    public void ChannelSenderContracts_CannotExposeProviderSpecificTypes()
    {
        MethodInfo[] methods = typeof(INotificationChannelSender).GetMethods();

        foreach (MethodInfo method in methods)
        {
            foreach (ParameterInfo param in method.GetParameters())
            {
                bool isThirdParty = param.ParameterType.Namespace != null &&
                    (param.ParameterType.Namespace.StartsWith("SendGrid", StringComparison.Ordinal) ||
                     param.ParameterType.Namespace.StartsWith("Twilio", StringComparison.Ordinal) ||
                     param.ParameterType.Namespace.StartsWith("Amazon", StringComparison.Ordinal));

                isThirdParty.Should().BeFalse($"Contract method '{method.Name}' parameter '{param.Name}' must not expose third-party provider types.");
            }

            bool returnTypeIsThirdParty = method.ReturnType.Namespace != null &&
                (method.ReturnType.Namespace.StartsWith("SendGrid", StringComparison.Ordinal) ||
                 method.ReturnType.Namespace.StartsWith("Twilio", StringComparison.Ordinal) ||
                 method.ReturnType.Namespace.StartsWith("Amazon", StringComparison.Ordinal));

            returnTypeIsThirdParty.Should().BeFalse($"Contract method '{method.Name}' return type must not expose third-party provider types.");
        }
    }

    [Fact]
    public void DeliveryContracts_CannotContainCredentials()
    {
        Type deliveryType = typeof(LastHour.Modules.Notifications.Domain.Notifications.NotificationDelivery);
        PropertyInfo[] properties = deliveryType.GetProperties();

        string[] forbiddenSecretWords = new[] { "Password", "Secret", "ApiKey", "Credential", "AuthToken" };

        foreach (PropertyInfo prop in properties)
        {
            foreach (string word in forbiddenSecretWords)
            {
                prop.Name.Contains(word, StringComparison.OrdinalIgnoreCase)
                    .Should().BeFalse($"NotificationDelivery property '{prop.Name}' must not represent raw credential material.");
            }
        }
    }

    [Fact]
    public void MetricsAndLogging_MustNotIncludeIdentityPII()
    {
        Type metricsType = typeof(LastHour.Modules.Notifications.Infrastructure.Delivery.NotificationDeliveryMetrics);
        MethodInfo[] methods = metricsType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        string[] forbiddenPii = new[] { "Email", "Phone", "UserId", "RecipientId", "Secret", "Token" };

        foreach (MethodInfo method in methods)
        {
            foreach (ParameterInfo param in method.GetParameters())
            {
                foreach (string pii in forbiddenPii)
                {
                    string paramName = param.Name ?? string.Empty;
                    paramName.Equals(pii, StringComparison.OrdinalIgnoreCase)
                        .Should().BeFalse($"Metrics recorder '{method.Name}' parameter '{paramName}' violates zero-PII dimension rules.");
                }
            }
        }
    }

    [Fact]
    public void NotificationPreferences_DomainDoesNotReferenceInfrastructure()
    {
        Type prefType = typeof(LastHour.Modules.Notifications.Domain.Notifications.NotificationPreference);
        prefType.Assembly.GetReferencedAssemblies()
            .Should().NotContain(a => a.Name != null && a.Name.Contains("Infrastructure"), "Notifications Domain must not reference Infrastructure.");
    }

    [Fact]
    public void NotificationTemplates_DomainDoesNotReferenceInfrastructure()
    {
        Type templateType = typeof(LastHour.Modules.Notifications.Domain.Notifications.NotificationTemplate);
        templateType.Assembly.GetReferencedAssemblies()
            .Should().NotContain(a => a.Name != null && a.Name.Contains("Infrastructure"), "Notifications Domain must not reference Infrastructure.");
    }

    [Fact]
    public void TemplateRenderer_DoesNotReferenceProviderSdk()
    {
        Type rendererType = typeof(NotificationTemplateRenderer);
        rendererType.Assembly.GetReferencedAssemblies()
            .Should().NotContain(a => a.Name != null && (a.Name.Contains("SendGrid") || a.Name.Contains("Twilio") || a.Name.Contains("Amazon")),
                "Template renderer must remain provider-neutral.");
    }

    [Fact]
    public void PreferenceResolver_DoesNotReferenceProviderSdk()
    {
        Type resolverType = typeof(NotificationPreferenceResolver);
        resolverType.Assembly.GetReferencedAssemblies()
            .Should().NotContain(a => a.Name != null && (a.Name.Contains("SendGrid") || a.Name.Contains("Twilio") || a.Name.Contains("Amazon")),
                "Preference resolver must remain provider-neutral.");
    }

    [Fact]
    public void NotificationTemplateContext_ContainsNoCredentials()
    {
        Type contextType = typeof(NotificationTemplateContext);
        PropertyInfo[] properties = contextType.GetProperties();

        string[] forbiddenCredentials = new[] { "Password", "PasswordHash", "Secret", "AccessToken", "RefreshToken", "SecurityStamp" };

        foreach (PropertyInfo prop in properties)
        {
            foreach (string cred in forbiddenCredentials)
            {
                prop.Name.Contains(cred, StringComparison.OrdinalIgnoreCase)
                    .Should().BeFalse($"NotificationTemplateContext property '{prop.Name}' must not expose credentials.");
            }
        }
    }

    [Fact]
    public void NotificationPreferenceContracts_ContainNoUserSuppliedRecipientIdentity()
    {
        Type commandType = typeof(LastHour.Modules.Notifications.Application.Commands.UpdateNotificationPreferenceCommand);
        PropertyInfo[] properties = commandType.GetProperties();

        foreach (PropertyInfo prop in properties)
        {
            prop.Name.Equals("RecipientEmail", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
            prop.Name.Equals("RecipientPhone", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        }
    }

    [Fact]
    public void NotificationPreferences_AreUserScoped()
    {
        Type prefRepoType = typeof(NotificationPreferenceRepository);
        MethodInfo[] methods = prefRepoType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (MethodInfo method in methods)
        {
            if (method.Name.StartsWith("Get", StringComparison.Ordinal))
            {
                ParameterInfo[] parameters = method.GetParameters();
                parameters.Should().Contain(p => p.Name != null && p.Name.Equals("userId", StringComparison.OrdinalIgnoreCase),
                    $"Query method '{method.Name}' must be user-scoped.");
            }
        }
    }

    [Fact]
    public void Templates_AreProviderNeutral()
    {
        Type templateType = typeof(LastHour.Modules.Notifications.Domain.Notifications.NotificationTemplate);
        PropertyInfo[] properties = templateType.GetProperties();

        foreach (PropertyInfo prop in properties)
        {
            prop.Name.Contains("Provider", StringComparison.OrdinalIgnoreCase)
                .Should().BeFalse($"NotificationTemplate property '{prop.Name}' must not be provider-specific.");
        }
    }

    [Fact]
    public void ProviderImplementations_RemainInfrastructureOnly()
    {
        TestResult result = Types.InAssembly(NotificationsDomainAssembly)
            .That()
            .ResideInNamespace("LastHour.Modules.Notifications.Domain")
            .Should()
            .NotResideInNamespace("LastHour.Modules.Notifications.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void NotificationApplication_DoesNotReferenceProviderAdapters()
    {
        TestResult result = Types.InAssembly(NotificationsApplicationAssembly)
            .That()
            .ResideInNamespace("LastHour.Modules.Notifications.Application")
            .Should()
            .NotHaveDependencyOnAny("LastHour.Modules.Notifications.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void OperationsApplication_DoesNotReferenceApi()
    {
        TestResult result = Types.InAssembly(NotificationsApplicationAssembly)
            .That()
            .ResideInNamespace("LastHour.Modules.Notifications.Application")
            .Should()
            .NotHaveDependencyOnAny("LastHour.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void OperationsApplication_DoesNotDependOnEfCoreDbContextDirectly()
    {
        TestResult result = Types.InAssembly(NotificationsApplicationAssembly)
            .That()
            .ResideInNamespace("LastHour.Modules.Notifications.Application")
            .Should()
            .NotHaveDependencyOnAny("Microsoft.EntityFrameworkCore.DbContext")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void OperationsQueryDtos_DoNotExposeRecipientPii()
    {
        Type dtoType = typeof(LastHour.Modules.Notifications.Application.Models.NotificationDeliverySummaryDto);
        PropertyInfo[] properties = dtoType.GetProperties();

        string[] piiNames = new[] { "Recipient", "Email", "Phone", "Body", "Template" };
        foreach (PropertyInfo prop in properties)
        {
            foreach (string pii in piiNames)
            {
                prop.Name.Equals(pii, StringComparison.OrdinalIgnoreCase)
                    .Should().BeFalse($"NotificationDeliverySummaryDto property '{prop.Name}' must not contain PII.");
            }
        }
    }

    [Fact]
    public void OperationsAttemptDtos_DoNotExposeRecipientPii()
    {
        Type dtoType = typeof(LastHour.Modules.Notifications.Application.Models.NotificationDeliveryAttemptDto);
        PropertyInfo[] properties = dtoType.GetProperties();

        string[] piiNames = new[] { "Recipient", "Email", "Phone", "Body", "Template" };
        foreach (PropertyInfo prop in properties)
        {
            foreach (string pii in piiNames)
            {
                prop.Name.Equals(pii, StringComparison.OrdinalIgnoreCase)
                    .Should().BeFalse($"NotificationDeliveryAttemptDto property '{prop.Name}' must not contain PII.");
            }
        }
    }

    [Fact]
    public void OperationsAuditDtos_DoNotExposeRecipientPii()
    {
        Type dtoType = typeof(LastHour.Modules.Notifications.Application.Models.NotificationDeliveryAuditDto);
        PropertyInfo[] properties = dtoType.GetProperties();

        string[] piiNames = new[] { "Recipient", "Email", "Phone", "Body", "Template" };
        foreach (PropertyInfo prop in properties)
        {
            foreach (string pii in piiNames)
            {
                prop.Name.Equals(pii, StringComparison.OrdinalIgnoreCase)
                    .Should().BeFalse($"NotificationDeliveryAuditDto property '{prop.Name}' must not contain PII.");
            }
        }
    }

    [Fact]
    public void OperationsCommands_DoNotContainProviderCredentials()
    {
        Type commandType = typeof(LastHour.Modules.Notifications.Application.Commands.RequeueNotificationDeliveryCommand);
        PropertyInfo[] properties = commandType.GetProperties();

        string[] credNames = new[] { "Password", "Secret", "Token", "ApiKey", "ConnectionString" };
        foreach (PropertyInfo prop in properties)
        {
            foreach (string cred in credNames)
            {
                prop.Name.Contains(cred, StringComparison.OrdinalIgnoreCase)
                    .Should().BeFalse($"Command property '{prop.Name}' must not contain credentials.");
            }
        }
    }

    [Fact]
    public void AuditRepositories_RemainAppendOnly()
    {
        Type auditRepoType = typeof(LastHour.Modules.Notifications.Application.Persistence.NotificationDeliveryAuditRepository);
        MethodInfo[] methods = auditRepoType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (MethodInfo method in methods)
        {
            method.Name.StartsWith("Update", StringComparison.Ordinal).Should().BeFalse();
            if (method.Name.StartsWith("Delete", StringComparison.Ordinal))
            {
                method.Name.Should().Be("DeleteOlderThanAsync", "Audit repositories support bulk retention cleanup via DeleteOlderThanAsync only.");
            }
        }
    }

    [Fact]
    public void OperationsHandlers_DoNotReferenceConcreteProviderAdapters()
    {
        Type handlerType = typeof(LastHour.Modules.Notifications.Application.Commands.RequeueNotificationDeliveryCommandHandler);
        FieldInfo[] fields = handlerType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (FieldInfo field in fields)
        {
            field.FieldType.Name.Contains("SendGrid", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
            field.FieldType.Name.Contains("Twilio", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        }
    }

    [Fact]
    public void AdminIdentity_IsTreatedAsActorContextNotRecipientIdentity()
    {
        Type commandType = typeof(LastHour.Modules.Notifications.Application.Commands.RequeueNotificationDeliveryCommand);
        PropertyInfo[] properties = commandType.GetProperties();

        properties.Should().Contain(p => p.Name == "AdminUserId");
        properties.Should().NotContain(p => p.Name == "RecipientId");
    }

    [Fact]
    public void ApplicationOperations_DoNotDependOnHttpTypes()
    {
        NotificationsApplicationAssembly.GetReferencedAssemblies()
            .Should().NotContain(a => a.Name != null && a.Name.Contains("AspNetCore.Http"),
                "Notifications Application layer must not depend on ASP.NET Core HTTP types.");
    }

    [Fact]
    public void AdminRequestDtos_CannotContainAdminUserId()
    {
        Type singleRequestType = typeof(LastHour.Api.Endpoints.Notifications.Requests.RequeueDeliveryRequest);
        Type batchRequestType = typeof(LastHour.Api.Endpoints.Notifications.Requests.BatchRequeueDeliveryRequest);

        singleRequestType.GetProperties().Should().NotContain(p => p.Name.Contains("AdminUserId", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("ActorId", StringComparison.OrdinalIgnoreCase));
        batchRequestType.GetProperties().Should().NotContain(p => p.Name.Contains("AdminUserId", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("ActorId", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void NotificationAdminEndpoints_RequireAuthorization()
    {
        Type endpointsType = typeof(LastHour.Api.Endpoints.Notifications.NotificationAdminEndpoints);
        endpointsType.Should().NotBeNull();
    }

    [Fact]
    public void AdminApi_DoesNotReferenceProviderSdks()
    {
        Type endpointsType = typeof(LastHour.Api.Endpoints.Notifications.NotificationAdminEndpoints);
        endpointsType.Assembly.GetReferencedAssemblies()
            .Should().NotContain(a => a.Name != null && (a.Name.Contains("SendGrid") || a.Name.Contains("Twilio") || a.Name.Contains("Amazon")),
                "Admin API must remain provider-neutral.");
    }

    [Fact]
    public void RetentionApplication_DoesNotReferenceApi()
    {
        Type serviceType = typeof(LastHour.Modules.Notifications.Application.Retention.NotificationOperationsRetentionService);
        serviceType.Assembly.GetReferencedAssemblies()
            .Should().NotContain(a => a.Name != null && a.Name.Equals("LastHour.Api", StringComparison.Ordinal));
    }

    [Fact]
    public void RetentionApplication_DoesNotDependOnEfCoreDbContextDirectly()
    {
        Type serviceType = typeof(LastHour.Modules.Notifications.Application.Retention.NotificationOperationsRetentionService);
        FieldInfo[] fields = serviceType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
        fields.Should().NotContain(f => f.FieldType.Name.Contains("DbContext", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RetentionService_DoesNotDeleteNotificationDeliveryAggregates()
    {
        Type repoType = typeof(LastHour.Modules.Notifications.Application.Persistence.INotificationDeliveryRepository);
        MethodInfo[] methods = repoType.GetMethods();

        foreach (MethodInfo method in methods)
        {
            method.Name.StartsWith("Delete", StringComparison.Ordinal).Should().BeFalse("NotificationDelivery repository must not expose bulk delete methods.");
        }
    }

    [Fact]
    public void RetentionContracts_ContainNoRecipientPii()
    {
        Type resultType = typeof(LastHour.Modules.Notifications.Application.Retention.RetentionCleanupResult);
        PropertyInfo[] properties = resultType.GetProperties();

        string[] piiNames = new[] { "Recipient", "Email", "Phone", "Body", "Template" };
        foreach (PropertyInfo prop in properties)
        {
            foreach (string pii in piiNames)
            {
                prop.Name.Equals(pii, StringComparison.OrdinalIgnoreCase)
                    .Should().BeFalse($"RetentionCleanupResult property '{prop.Name}' must not contain PII.");
            }
        }
    }

    [Fact]
    public void RetentionLogsAndMetrics_ContainNoRecipientPii()
    {
        Type metricsType = typeof(LastHour.Modules.Notifications.Infrastructure.Retention.NotificationOperationsRetentionMetrics);
        MethodInfo[] methods = metricsType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (MethodInfo method in methods)
        {
            foreach (ParameterInfo param in method.GetParameters())
            {
                param.Name.Should().NotContain("Email");
                param.Name.Should().NotContain("Phone");
                param.Name.Should().NotContain("Recipient");
            }
        }
    }

    [Fact]
    public void HttpRequestDtos_ContainNoProviderCredentials()
    {
        Type requestType = typeof(LastHour.Api.Endpoints.Notifications.Requests.RequeueDeliveryRequest);
        PropertyInfo[] properties = requestType.GetProperties();

        string[] credNames = new[] { "Password", "Secret", "Token", "ApiKey", "ConnectionString" };
        foreach (PropertyInfo prop in properties)
        {
            foreach (string cred in credNames)
            {
                prop.Name.Contains(cred, StringComparison.OrdinalIgnoreCase)
                    .Should().BeFalse($"Request DTO property '{prop.Name}' must not contain credentials.");
            }
        }
    }

    [Fact]
    public void AdminIdentity_IsDerivedFromClaims()
    {
        Type requestType = typeof(LastHour.Api.Endpoints.Notifications.Requests.RequeueDeliveryRequest);
        PropertyInfo[] properties = requestType.GetProperties();

        properties.Should().NotContain(p => p.Name == "AdminUserId");
    }
}
