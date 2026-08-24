using System.Reflection;
using System.Text.RegularExpressions;
using FluentAssertions;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Events;
using LastHour.BuildingBlocks.SharedKernel.Domain.Events;
using MediatR;
using NetArchTest.Rules;

namespace LastHour.BuildingBlocks.Architecture.Tests.Architecture;

/// <summary>
/// Architecture tests enforcing the production standardization rules for domain-event consumers.
/// Every consumer in the modular monolith must adhere to the canonical structure:
/// INotificationHandler&lt;NotificationMessage&lt;TDomainEvent&gt;&gt;, declare a stable unique
/// ConsumerName formatted as &lt;ModuleName&gt;.&lt;OperationName&gt;, inject IConsumerIdempotencyService,
/// and reside strictly in its module's Application.Consumers namespace without illegal cross-module
/// or infrastructure dependencies.
/// </summary>
public class ConsumerArchitectureTests
{
    private static readonly Assembly[] SourceAssemblies = BuildSourceAssemblies();
    private static readonly Regex ConsumerNameRegex = new(@"^[A-Z][a-zA-Z0-9]+\.[A-Z][a-zA-Z0-9]+$", RegexOptions.Compiled);

    [Fact]
    public void AllConsumers_AreLocatedInApplicationConsumers()
    {
        List<Type> consumers = GetAllConsumerTypes();
        consumers.Should().NotBeEmpty("There should be at least one registered consumer in the solution.");

        foreach (Type consumer in consumers)
        {
            consumer.Namespace.Should().NotBeNull();
            consumer.Namespace!.Should().MatchRegex(
                @"^LastHour\.Modules\.[a-zA-Z0-9]+\.Application\.Consumers$",
                $"{consumer.FullName} must reside in a Module's Application.Consumers namespace.");
        }
    }

    [Fact]
    public void AllConsumers_UseNotificationMessageEnvelope()
    {
        List<Type> consumers = GetAllConsumerTypes();
        consumers.Should().NotBeEmpty();

        foreach (Type consumer in consumers)
        {
            Type[] interfaces = consumer.GetInterfaces();
            bool implementsNotificationMessageEnvelope = interfaces.Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(INotificationHandler<>) &&
                i.GetGenericArguments()[0].IsGenericType &&
                i.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(NotificationMessage<>));

            implementsNotificationMessageEnvelope.Should().BeTrue(
                $"{consumer.FullName} must implement INotificationHandler<NotificationMessage<T>>.");
        }
    }

    [Fact]
    public void AllConsumers_HaveStableConsumerNames()
    {
        List<Type> consumers = GetAllConsumerTypes();
        consumers.Should().NotBeEmpty();

        foreach (Type consumer in consumers)
        {
            FieldInfo? field = consumer.GetField("ConsumerName", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            field.Should().NotBeNull($"{consumer.FullName} must declare a public const string ConsumerName.");
            field!.FieldType.Should().Be<string>();

            string? value = field.GetValue(null) as string;
            value.Should().NotBeNullOrWhiteSpace($"{consumer.FullName}.ConsumerName cannot be null or whitespace.");
        }
    }

    [Fact]
    public void AllConsumers_FollowConsumerNameConvention()
    {
        List<Type> consumers = GetAllConsumerTypes();
        consumers.Should().NotBeEmpty();

        foreach (Type consumer in consumers)
        {
            string consumerName = GetConsumerName(consumer);
            ConsumerNameRegex.IsMatch(consumerName).Should().BeTrue(
                $"{consumer.FullName}.ConsumerName '{consumerName}' must match '<ModuleName>.<OperationName>' format.");
        }
    }

    [Fact]
    public void AllConsumers_HaveUniqueConsumerNames()
    {
        List<Type> consumers = GetAllConsumerTypes();
        consumers.Should().NotBeEmpty();

        var duplicates = consumers
            .GroupBy(GetConsumerName)
            .Where(g => g.Count() > 1)
            .Select(g => new { ConsumerName = g.Key, Handlers = g.Select(h => h.FullName).ToList() })
            .ToList();

        duplicates.Should().BeEmpty(
            $"Duplicate ConsumerName declarations detected: {string.Join(", ", duplicates.Select(d => $"{d.ConsumerName} on [{string.Join(", ", d.Handlers)}]"))}");
    }

    [Fact]
    public void AllConsumers_UseConsumerIdempotencyService()
    {
        List<Type> consumers = GetAllConsumerTypes();
        consumers.Should().NotBeEmpty();

        foreach (Type consumer in consumers)
        {
            ConstructorInfo[] constructors = consumer.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            bool hasIdempotencyService = constructors.Any(c =>
                c.GetParameters().Any(p => p.ParameterType == typeof(IConsumerIdempotencyService)));

            hasIdempotencyService.Should().BeTrue(
                $"{consumer.FullName} must inject IConsumerIdempotencyService in its public constructor.");
        }
    }

    [Fact]
    public void Consumers_DoNotReferenceInfrastructure()
    {
        TestResult result = Types
            .InAssemblies(SourceAssemblies)
            .That()
            .ResideInNamespaceEndingWith(".Application.Consumers")
            .Should()
            .NotHaveDependencyOnAny("LastHour.BuildingBlocks.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(DescribeFailures(result));
    }

    [Fact]
    public void Consumers_DoNotReferenceOtherModuleDomains()
    {
        List<Type> consumers = GetAllConsumerTypes();
        consumers.Should().NotBeEmpty();

        foreach (Type consumer in consumers)
        {
            // Extract module name from namespace: LastHour.Modules.<ModuleName>.Application.Consumers
            string[] parts = consumer.Namespace!.Split('.');
            string ownModuleName = parts[2]; // "Notifications", etc.

            // Other module domain namespaces
            string[] otherModuleDomainPrefixes = SourceAssemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.Namespace is not null && t.Namespace.StartsWith("LastHour.Modules.", StringComparison.Ordinal) && t.Namespace.Contains(".Domain"))
                .Select(t => t.Namespace!)
                .Where(ns => !ns.StartsWith($"LastHour.Modules.{ownModuleName}.", StringComparison.Ordinal))
                .Distinct()
                .ToArray();

            if (otherModuleDomainPrefixes.Length > 0)
            {
                TestResult result = Types
                    .InAssembly(consumer.Assembly)
                    .That()
                    .HaveName(consumer.Name)
                    .Should()
                    .NotHaveDependencyOnAny(otherModuleDomainPrefixes)
                    .GetResult();

                result.IsSuccessful.Should().BeTrue(
                    $"{consumer.FullName} must not reference other modules' Domain layer. Failures: {DescribeFailures(result)}");
            }
        }
    }

    [Fact]
    public void Consumers_DoNotAccessInboxDirectly()
    {
        TestResult result = Types
            .InAssemblies(SourceAssemblies)
            .That()
            .ResideInNamespaceEndingWith(".Application.Consumers")
            .Should()
            .NotHaveDependencyOnAny("LastHour.BuildingBlocks.Infrastructure.Persistence.Idempotency.ConsumerInboxMessage")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(DescribeFailures(result));
    }

    [Fact]
    public void Consumers_DoNotAccessOutboxDirectly()
    {
        TestResult result = Types
            .InAssemblies(SourceAssemblies)
            .That()
            .ResideInNamespaceEndingWith(".Application.Consumers")
            .Should()
            .NotHaveDependencyOnAny(
                "LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.OutboxMessage",
                "LastHour.BuildingBlocks.Infrastructure.Persistence.Outbox.OutboxProcessor")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(DescribeFailures(result));
    }

    [Fact]
    public void DomainEvents_ConsumedByConsumers_InheritBaseDomainEvent()
    {
        List<Type> consumers = GetAllConsumerTypes();
        consumers.Should().NotBeEmpty();

        foreach (Type consumer in consumers)
        {
            Type handlerInterface = consumer.GetInterfaces().First(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>));

            Type notificationMessageType = handlerInterface.GetGenericArguments()[0];
            Type eventType = notificationMessageType.GetGenericArguments()[0];

            eventType.IsSubclassOf(typeof(BaseDomainEvent)).Should().BeTrue(
                $"{eventType.FullName} consumed by {consumer.FullName} must inherit from BaseDomainEvent.");

            eventType.Namespace.Should().Be(
                "LastHour.BuildingBlocks.Contracts.Events",
                $"{eventType.FullName} must reside in LastHour.BuildingBlocks.Contracts.Events.");
        }
    }

    [Fact]
    public void DuplicateConsumerNameDetection_FailsWhenDuplicateConsumerIsAnalyzed()
    {
        var simulatedConsumers = new[]
        {
            new { TypeName = "HandlerA", Name = "Notifications.MerchantCreated" },
            new { TypeName = "HandlerB", Name = "Notifications.MerchantCreated" },
        };

        var duplicates = simulatedConsumers
            .GroupBy(c => c.Name)
            .Where(g => g.Count() > 1)
            .Select(g => new { ConsumerName = g.Key, Handlers = g.Select(h => h.TypeName).ToList() })
            .ToList();

        duplicates.Should().ContainSingle();
        duplicates.Single().ConsumerName.Should().Be("Notifications.MerchantCreated");
        duplicates.Single().Handlers.Should().HaveCount(2);
    }

    [Fact]
    public void DomainEvents_DoNotContainSensitiveCredentialProperties()
    {
        Type[] eventTypes = SourceAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(BaseDomainEvent)))
            .ToArray();

        eventTypes.Should().NotBeEmpty("There should be domain events defined in BuildingBlocks.Contracts.");

        string[] forbiddenExactNames =
        [
            "Password",
            "PasswordCredential",
            "PasswordHash",
            "RefreshToken",
            "AccessToken",
            "Jwt",
            "Secret",
            "Credential",
            "Credentials",
            "AuthToken",
            "SessionToken",
        ];

        foreach (Type eventType in eventTypes)
        {
            PropertyInfo[] properties = eventType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                foreach (string forbidden in forbiddenExactNames)
                {
                    property.Name.Equals(forbidden, StringComparison.OrdinalIgnoreCase).Should().BeFalse(
                        $"Domain event '{eventType.FullName}' must not expose sensitive credential property '{property.Name}'.");
                }

                property.PropertyType.Name.Contains("Password", StringComparison.OrdinalIgnoreCase).Should().BeFalse(
                    $"Domain event '{eventType.FullName}' property '{property.Name}' type '{property.PropertyType.Name}' must not be a password type.");
                property.PropertyType.Name.Contains("Credential", StringComparison.OrdinalIgnoreCase).Should().BeFalse(
                    $"Domain event '{eventType.FullName}' property '{property.Name}' type '{property.PropertyType.Name}' must not be a credential type.");
            }
        }
    }

    [Fact]
    public void DomainEventContracts_DoNotReferenceModuleDomainLayers()
    {
        TestResult result = Types
            .InAssemblies(SourceAssemblies)
            .That()
            .ResideInNamespace("LastHour.BuildingBlocks.Contracts.Events")
            .Should()
            .NotHaveDependencyOnAny(
                "LastHour.Modules.Identity.Domain",
                "LastHour.Modules.Merchant.Domain",
                "LastHour.Modules.Catalog.Domain",
                "LastHour.Modules.Inventory.Domain",
                "LastHour.Modules.Ordering.Domain",
                "LastHour.Modules.Payments.Domain",
                "LastHour.Modules.Notifications.Domain",
                "LastHour.Modules.Platform.Domain",
                "LastHour.Modules.Administration.Domain",
                "LastHour.Modules.Analytics.Domain",
                "LastHour.Modules.Offers.Domain")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(DescribeFailures(result));
    }

    [Fact]
    public void AllConsumers_AreInAssembliesReferencedByApi()
    {
        Assembly apiAssembly = typeof(Program).Assembly;
        var referencedAssemblyNames = apiAssembly.GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToHashSet(StringComparer.Ordinal);

        List<Type> consumers = GetAllConsumerTypes();
        foreach (Type consumer in consumers)
        {
            string assemblyName = consumer.Assembly.GetName().Name!;
            referencedAssemblyNames.Should().Contain(
                assemblyName,
                $"The API project must reference the assembly '{assemblyName}' containing {consumer.FullName} to enable DI registration.");
        }
    }

    private static List<Type> GetAllConsumerTypes()
    {
        return SourceAssemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        t.Namespace is not null &&
                        t.Namespace.Contains(".Application.Consumers") &&
                        t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
            .ToList();
    }

    private static string GetConsumerName(Type consumer)
    {
        FieldInfo? field = consumer.GetField("ConsumerName", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        if (field is not null && field.GetValue(null) is string value)
        {
            return value;
        }

        throw new InvalidOperationException($"Consumer {consumer.FullName} does not declare a public const string ConsumerName.");
    }

    [Fact]
    public void AllNotificationChannelSenders_ResideInInfrastructureDelivery()
    {
        Assembly notificationsAssembly = typeof(LastHour.Modules.Notifications.Domain.Notifications.Notification).Assembly;
        Type senderInterface = typeof(LastHour.Modules.Notifications.Application.Delivery.INotificationChannelSender);

        List<Type> senders = notificationsAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && senderInterface.IsAssignableFrom(t))
            .ToList();

        senders.Should().NotBeEmpty();

        foreach (Type sender in senders)
        {
            sender.Namespace.Should().Be("LastHour.Modules.Notifications.Infrastructure.Delivery");
        }
    }

    private static Assembly[] BuildSourceAssemblies()
    {
        Assembly api = typeof(Program).Assembly;
        var loadedAssemblies = new HashSet<Assembly> { api };
        var queue = new Queue<Assembly>();
        queue.Enqueue(api);

        while (queue.Count > 0)
        {
            Assembly current = queue.Dequeue();
            foreach (AssemblyName refName in current.GetReferencedAssemblies())
            {
                if (refName.Name is not null && refName.Name.StartsWith("LastHour.", StringComparison.Ordinal))
                {
                    try
                    {
                        Assembly loaded = Assembly.Load(refName);
                        if (loadedAssemblies.Add(loaded))
                        {
                            queue.Enqueue(loaded);
                        }
                    }
                    catch
                    {
                        // Ignore load failures
                    }
                }
            }
        }

        return loadedAssemblies.ToArray();
    }

    private static string DescribeFailures(TestResult result)
        => result.FailingTypes is null
            ? string.Empty
            : string.Join(Environment.NewLine, result.FailingTypes.Select(type => type.FullName));
}
