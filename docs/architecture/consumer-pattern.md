# Production Domain Event Consumer Standard

This document defines the mandatory architecture and implementation pattern for consuming domain events in the **LastHour Backend**.

---

## 1. Architectural Overview

Events in the LastHour modular monolith flow through an asynchronous, decoupled, at-least-once outbox pipeline:

```
[Module A Aggregate]
        ↓ Raises
[BaseDomainEvent (in Contracts.Events)]
        ↓ Intercepted by
[DomainEventsInterceptor]
        ↓ Persisted into
[outbox_messages Table] (Same DB transaction as Aggregate)
        ↓ Polled & Leased by
[OutboxProcessor] (Background Hosted Service)
        ↓ Rehydrates & Envelopes
[EventDispatcher]
        ↓ Publishes NotificationMessage<TDomainEvent>
[MediatR Pipeline]
        ↓ Dispatches to
[INotificationHandler<NotificationMessage<TDomainEvent>>] (Consumer in Module B)
        ↓ Executes via
[IConsumerIdempotencyService.ExecuteAsync]
        ↓ Layer 1 Guard
[consumer_inbox_messages Table] UNIQUE(EventId, Consumer)
        ↓ Business Effect Handler Delegate
[Business Persistence (via IUnitOfWork)]
        ↓ Layer 2 Guard
[Domain Table Unique Constraint, e.g., notifications UNIQUE(EventId, Type)]
```

---

## 2. Delivery & Idempotency Model

### Delivery Semantics
Domain events are delivered with **At-Least-Once Delivery** guarantees. Under network failures, application crashes, or database reconnections, the same event may legitimately be delivered more than once.

### The Two-Layer Idempotency Defense
1. **Layer 1: Consumer Execution Idempotency**
   - Guarded by `consumer_inbox_messages` table with `UNIQUE(EventId, Consumer)`.
   - Acquires a time-limited lease (`LockExpiresAtUtc = Now + 2 minutes`).
   - Suppresses redeliveries after successful completion (`CompletedOnUtc != null`).
   - Automatically handles lease expiration and renewal for crashed/timed-out workers using optimistic concurrency tokens.
2. **Layer 2: Business Effect Idempotency**
   - Guards against the crash window where business persistence succeeds but the process terminates before `consumer_inbox_messages` is marked completed.
   - Guarded by domain-level constraints (e.g. `notifications` with `UNIQUE(EventId, Type) WHERE "EventId" IS NOT NULL`).
   - Supported by pre-checks (`ExistsByEventIdAndTypeAsync`) and database unique violation recovery (`NotificationDbExceptionDetector`).

**Formula**:
$$\text{At-Least-Once Delivery} + \text{Consumer Idempotency} + \text{Business Effect Idempotency} = \text{Effectively-Once Persistence}$$

---

## 3. Canonical Consumer Standard Template

Every domain-event consumer MUST adhere to this canonical pattern:

```csharp
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Application.Events;
using LastHour.BuildingBlocks.Contracts.Events;
using LastHour.Modules.<TargetModule>.Application.Persistence;
using LastHour.Modules.<TargetModule>.Domain.<Aggregate>;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LastHour.Modules.<TargetModule>.Application.Consumers;

/// <summary>
/// Event consumer that handles <see cref="<Source>DomainEvent"/> notifications under consumer idempotency controls.
/// </summary>
public sealed class <Operation>NotificationHandler : INotificationHandler<NotificationMessage<<Source>DomainEvent>>
{
    /// <summary>
    /// The canonical, stable, unique consumer identifier used for consumer-scoped idempotency boundaries.
    /// </summary>
    public const string ConsumerName = "<TargetModuleName>.<OperationName>";

    private readonly IConsumerIdempotencyService _idempotencyService;
    private readonly I<Target>Repository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<<Operation>NotificationHandler> _logger;

    public <Operation>NotificationHandler(
        IConsumerIdempotencyService idempotencyService,
        I<Target>Repository repository,
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<<Operation>NotificationHandler> logger)
    {
        _idempotencyService = idempotencyService ?? throw new ArgumentNullException(nameof(idempotencyService));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task Handle(
        NotificationMessage<<Source>DomainEvent> notification,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);
        <Source>DomainEvent domainEvent = notification.Message;

        await _idempotencyService.ExecuteAsync(
            domainEvent,
            ConsumerName,
            async ct =>
            {
                // 1. Optional Layer 2 Pre-Check (for crash recovery efficiency)
                if (await _repository.ExistsAsync(domainEvent.EventId, ct).ConfigureAwait(false))
                {
                    return;
                }

                // 2. Perform business logic & entity creation
                var entity = <Aggregate>.Create(domainEvent, _clock.UtcNow);
                await _repository.AddAsync(entity, ct).ConfigureAwait(false);

                // 3. Persist via Unit of Work
                try
                {
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                }
                catch (DbUpdateException ex) when (<Module>DbExceptionDetector.IsUniqueConstraintViolation(ex))
                {
                    // Layer 2 uniqueness catch: already committed by prior crashed attempt
                }
            },
            cancellationToken).ConfigureAwait(false);
    }
}
```

---

## 4. Consumer Identity (`ConsumerName`) Rules

1. **Format**: `<ModuleName>.<OperationName>` (PascalCase separated by a single dot).
   - Example: `Notifications.MerchantCreated`
   - Example: `Analytics.MerchantCreated`
   - Example: `Audit.UserRegistered`
2. **Immutability & Stability**:
   - `ConsumerName` is part of the persistent composite primary key `(EventId, Consumer)` in `consumer_inbox_messages`.
   - Renaming a `ConsumerName` will cause all historical events to be re-processed if replayed. Treat changes to `ConsumerName` as a database schema change.
3. **No Dynamic / Environment Values**:
   - MUST NOT contain GUIDs, machine names, process IDs, assembly versions, or environment names.
4. **Uniqueness**:
   - No two consumer classes may declare the same `ConsumerName`. Enforced via automated architecture tests.

---

## 5. Architectural Boundaries & Dependencies

### Allowed References
- Consumers reside in: `LastHour.Modules.<Module>.Application.Consumers`.
- Event payloads reside in: `LastHour.BuildingBlocks.Contracts.Events` and inherit from `BaseDomainEvent`.
- Consumers access abstractions via: `LastHour.BuildingBlocks.Application.Contracts` (`IConsumerIdempotencyService`, `IUnitOfWork`, `IClock`).
- Repositories accessed via: `LastHour.Modules.<Module>.Application.Persistence`.

### Prohibited Patterns
- **DO NOT** reference another module's internal Domain or Persistence layers. Cross-module communication occurs exclusively via shared Contracts and domain events.
- **DO NOT** reference `LastHour.BuildingBlocks.Infrastructure` directly from `Application` or module assemblies.
- **DO NOT** query or modify `consumer_inbox_messages` or `outbox_messages` tables directly.
- **DO NOT** implement custom in-memory locking, `static` dictionaries, or `Thread.Sleep` retry loops.
- **DO NOT** bypass `IConsumerIdempotencyService`.

---

## 6. Observability & Telemetry

Consumers automatically emit OpenTelemetry metrics via `IConsumerIdempotencyService`:
- `last_hour.consumer.processed` (Counter tagged by `consumer`): Incremented on successful completion.
- `last_hour.consumer.duplicate` (Counter tagged by `consumer`): Incremented when duplicate delivery is suppressed.
- `last_hour.consumer.failed` (Counter tagged by `consumer`): Incremented when unhandled handler failure occurs.

Logging in consumers must use high-performance `LoggerMessage.Define` and include `EventId` and `ConsumerName` where applicable.

---

## 7. Concrete Reference Example: MerchantCreated Consumer

See the production implementation:
- Consumer: [`MerchantCreatedNotificationHandler.cs`](file:///c:/Users/seaif/Desktop/last%20hour/src/Modules/Notifications/LastHour.Modules.Notifications/Application/Consumers/MerchantCreatedNotificationHandler.cs)
- Contract: [`MerchantCreatedDomainEvent.cs`](file:///c:/Users/seaif/Desktop/last%20hour/src/BuildingBlocks/LastHour.BuildingBlocks.Contracts/Events/MerchantCreatedDomainEvent.cs)
- Entity: [`Notification.cs`](file:///c:/Users/seaif/Desktop/last%20hour/src/Modules/Notifications/LastHour.Modules.Notifications/Domain/Notifications/Notification.cs)
- Unique Index: [`NotificationConfiguration.cs`](file:///c:/Users/seaif/Desktop/last%20hour/src/Modules/Notifications/LastHour.Modules.Notifications/Persistence/NotificationConfiguration.cs)
- Exception Detector: [`NotificationDbExceptionDetector.cs`](file:///c:/Users/seaif/Desktop/last%20hour/src/Modules/Notifications/LastHour.Modules.Notifications/Persistence/NotificationDbExceptionDetector.cs)
