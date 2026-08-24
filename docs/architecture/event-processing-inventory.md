# Domain Event Inventory & Event-to-Consumer Architecture Map

## 1. Executive Summary

This document provides the authoritative inventory of all Domain Events, Producers, Transactional Outbox integration paths, Consumers, and Cross-Module Boundaries across the **LastHour Backend** solution as of **Sprint 10.15-D**.

The event-processing pipeline has been fully hardened, observed, and standardized across Sprints 10.10 through 10.15:
- At-least-once outbox persistence via `DomainEventsInterceptor` and `outbox_messages`
- Atomic claiming, exponential backoff, retry scheduling, and poison message handling via `OutboxProcessor`
- Strongly typed sequential dispatch via `EventDispatcher` and `NotificationMessage<T>`
- Standardized consumer idempotency via `IConsumerIdempotencyService` and `consumer_inbox_messages`
- Business-effect uniqueness constraints on downstream read models and notification stores
- Full OpenTelemetry metric (`LastHour.Outbox`, `LastHour.ConsumerIdempotency`) and tracing (`Outbox.Dispatch`, `Consumer.Handle`) observability
- Health checks (`OutboxHealthCheck`, `ConsumerInboxHealthCheck`) and operational diagnostics (`IEventProcessingDiagnostics`)
- Production consumers:
  1. `Notifications.MerchantCreated` (`MerchantCreatedNotificationHandler`)
  2. `Notifications.MerchantStatusChanged` (`MerchantStatusChangedNotificationHandler`)
  3. `Notifications.MerchantProfileUpdated` (`MerchantProfileUpdatedNotificationHandler`)
  4. `Notifications.UserRegistered` (`UserRegisteredNotificationHandler`)
  5. `Notifications.UserPasswordChanged` (`UserPasswordChangedNotificationHandler`)
  6. `Notifications.UserLockedOut` (`UserLockedOutNotificationHandler`)
- Producer-only event foundations: **0** (All 6 production domain events are fully covered by production consumers).

**Coverage Status**:
- **Merchant Domain Events**: 100% (3/3) have active, production-grade consumers with dual-layer idempotency.
- **Identity Domain Events**: 100% (3/3) have active, production-grade consumers with dual-layer idempotency.
- **Total Solution Coverage**: **100% (6/6 events fully covered)**.

---

## 2. Event Processing Pipeline Architecture

```
[ Domain Entity / AggregateRoot (Merchant, User) ]
       │
       │ Raises Domain Event (inherits BaseDomainEvent)
       ▼
[ ChangeTracker.Entries() ]
       │
       │ Intercepted by DomainEventsInterceptor
       ▼
[ outbox_messages Table (PostgreSQL) ]
       │
       │ Atomic background batch claiming & lease acquisition
       ▼
[ OutboxProcessor (BackgroundService) ]
       │
       │ Sequential Dispatch
       ▼
[ EventDispatcher ]
       │
       │ NotificationMessageFactory.Create(domainEvent)
       ▼
[ MediatR Pipeline: NotificationMessage<TEvent> ]
       │
       ├──► [ Consumer 1: Notifications.MerchantCreated ] (IConsumerIdempotencyService)
       │         ├── Layer 1: consumer_inbox_messages check/lease
       │         ├── Business effect: Welcome Notification
       │         └── Layer 2: Business-effect DB uniqueness (IX_notifications_event_id_type)
       │
       ├──► [ Consumer 2: Notifications.MerchantStatusChanged ] (IConsumerIdempotencyService)
       │         ├── Layer 1: consumer_inbox_messages check/lease
       │         ├── Business effect: Status Transition Notification
       │         └── Layer 2: Business-effect DB uniqueness (IX_notifications_event_id_type)
       │
       ├──► [ Consumer 3: Notifications.MerchantProfileUpdated ] (IConsumerIdempotencyService)
       │         ├── Layer 1: consumer_inbox_messages check/lease
       │         ├── Business effect: Profile Updated Notification
       │         └── Layer 2: Business-effect DB uniqueness (IX_notifications_event_id_type)
       │
       ├──► [ Consumer 4: Notifications.UserRegistered ] (IConsumerIdempotencyService)
       │         ├── Layer 1: consumer_inbox_messages check/lease
       │         ├── Business effect: Welcome Registration Notification
       │         └── Layer 2: Business-effect DB uniqueness (IX_notifications_event_id_type)
       │
       ├──► [ Consumer 5: Notifications.UserPasswordChanged ] (IConsumerIdempotencyService)
       │         ├── Layer 1: consumer_inbox_messages check/lease
       │         ├── Business effect: Password Changed Security Notification
       │         └── Layer 2: Business-effect DB uniqueness (IX_notifications_event_id_type)
       │
       └──► [ Consumer 6: Notifications.UserLockedOut ] (IConsumerIdempotencyService)
                 ├── Layer 1: consumer_inbox_messages check/lease
                 ├── Business effect: Account Lockout Security Notification
                 └── Layer 2: Business-effect DB uniqueness (IX_notifications_event_id_type)
```

---

## 3. Complete Domain Event Inventory

| Event Class | Namespace | Assembly / Project | Aggregate Root | Properties |
|---|---|---|---|---|
| `MerchantCreatedDomainEvent` | `LastHour.BuildingBlocks.Contracts.Events` | `LastHour.BuildingBlocks.Contracts` | `Merchant` | `Guid EventId`<br>`DateTime OccurredOn`<br>`Guid MerchantId`<br>`string Name`<br>`string Address`<br>`string Phone` |
| `MerchantStatusChangedDomainEvent` | `LastHour.BuildingBlocks.Contracts.Events` | `LastHour.BuildingBlocks.Contracts` | `Merchant` | `Guid EventId`<br>`DateTime OccurredOn`<br>`Guid MerchantId`<br>`string PreviousStatus`<br>`string CurrentStatus` |
| `MerchantProfileUpdatedDomainEvent` | `LastHour.BuildingBlocks.Contracts.Events` | `LastHour.BuildingBlocks.Contracts` | `Merchant` | `Guid EventId`<br>`DateTime OccurredOn`<br>`Guid MerchantId`<br>`string Name` |
| `UserRegisteredDomainEvent` | `LastHour.BuildingBlocks.Contracts.Events` | `LastHour.BuildingBlocks.Contracts` | `User` | `Guid EventId`<br>`DateTime OccurredOn`<br>`Guid UserId`<br>`string Email` |
| `UserPasswordChangedDomainEvent` | `LastHour.BuildingBlocks.Contracts.Events` | `LastHour.BuildingBlocks.Contracts` | `User` | `Guid EventId`<br>`DateTime OccurredOn`<br>`Guid UserId` |
| `UserLockedOutDomainEvent` | `LastHour.BuildingBlocks.Contracts.Events` | `LastHour.BuildingBlocks.Contracts` | `User` | `Guid EventId`<br>`DateTime OccurredOn`<br>`Guid UserId`<br>`DateTime LockoutUntilUtc` |

---

## 4. Event Producer Map

### 1. `MerchantCreatedDomainEvent`
- **Producer Aggregate**: `Merchant` (`LastHour.Modules.Merchant.Domain.Merchants.Merchant`)
- **Domain Method**: `Merchant.Register(Guid ownerId, MerchantName name, Address address, PhoneNumber phone)`
- **Application Command**: `RegisterMerchantCommandHandler` (`RegisterMerchantCommand`)
- **Module**: `Merchant`

### 2. `MerchantStatusChangedDomainEvent`
- **Producer Aggregate**: `Merchant` (`LastHour.Modules.Merchant.Domain.Merchants.Merchant`)
- **Domain Methods**:
  - `Merchant.Verify()` $\rightarrow$ `VerifyMerchantCommandHandler`
  - `Merchant.Suspend()` $\rightarrow$ `SuspendMerchantCommandHandler`
  - `Merchant.Activate()` $\rightarrow$ `ActivateMerchantCommandHandler`
  - `Merchant.Close()` $\rightarrow$ `CloseMerchantCommandHandler`
- **Module**: `Merchant`

### 3. `MerchantProfileUpdatedDomainEvent`
- **Producer Aggregate**: `Merchant` (`LastHour.Modules.Merchant.Domain.Merchants.Merchant`)
- **Domain Method**: `Merchant.UpdateProfile(MerchantName name, Address address, PhoneNumber phone)`
- **Application Command**: `UpdateMerchantProfileCommandHandler` (`UpdateMerchantProfileCommand`)
- **Module**: `Merchant`

### 4. `UserRegisteredDomainEvent`
- **Producer Aggregate**: `User` (`LastHour.Modules.Identity.Domain.Users.User`)
- **Domain Method**: `User.Create(Email email, PasswordCredential? passwordCredential)`
- **Application Command**: `RegisterUserCommandHandler` (`RegisterUserCommand`)
- **Module**: `Identity`

### 5. `UserPasswordChangedDomainEvent`
- **Producer Aggregate**: `User` (`LastHour.Modules.Identity.Domain.Users.User`)
- **Domain Method**: `User.MarkPasswordChanged(DateTime utcNow)`
- **Application Command**: `ChangePasswordCommandHandler` (`ChangePasswordCommand`)
- **Module**: `Identity`

### 6. `UserLockedOutDomainEvent`
- **Producer Aggregate**: `User` (`LastHour.Modules.Identity.Domain.Users.User`)
- **Domain Method**: `User.RecordFailedLogin(DateTime utcNow, int maxFailedAttempts, TimeSpan lockoutDuration)`
- **Application Command**: `LoginCommandHandler` (`LoginCommand`)
- **Module**: `Identity`

---

## 5. Transactional Outbox Participation & Status

| Domain Event | Outbox Interception | JSON Serialization | Dispatch Mechanism | Classification |
|---|---|---|---|---|
| `MerchantCreatedDomainEvent` | Captured | AssemblyQualifiedName + Polymorphic JSON | `EventDispatcher` $\rightarrow$ `NotificationMessage<MerchantCreatedDomainEvent>` | **Class A: Fully Integrated** |
| `MerchantStatusChangedDomainEvent` | Captured | AssemblyQualifiedName + Polymorphic JSON | `EventDispatcher` $\rightarrow$ `NotificationMessage<MerchantStatusChangedDomainEvent>` | **Class A: Fully Integrated** |
| `MerchantProfileUpdatedDomainEvent` | Captured | AssemblyQualifiedName + Polymorphic JSON | `EventDispatcher` $\rightarrow$ `NotificationMessage<MerchantProfileUpdatedDomainEvent>` | **Class A: Fully Integrated** |
| `UserRegisteredDomainEvent` | Captured | AssemblyQualifiedName + Polymorphic JSON | `EventDispatcher` $\rightarrow$ `NotificationMessage<UserRegisteredDomainEvent>` | **Class A: Fully Integrated** |
| `UserPasswordChangedDomainEvent` | Captured | AssemblyQualifiedName + Polymorphic JSON | `EventDispatcher` $\rightarrow$ `NotificationMessage<UserPasswordChangedDomainEvent>` | **Class A: Fully Integrated** |
| `UserLockedOutDomainEvent` | Captured | AssemblyQualifiedName + Polymorphic JSON | `EventDispatcher` $\rightarrow$ `NotificationMessage<UserLockedOutDomainEvent>` | **Class A: Fully Integrated** |

---

## 6. Consumer Inventory

### Consumer 1: `Notifications.MerchantCreated`
- **Handler Class**: `MerchantCreatedNotificationHandler`
- **Module**: `Notifications` (`LastHour.Modules.Notifications.Application.Consumers`)
- **Event Consumed**: `MerchantCreatedDomainEvent` (`NotificationMessage<MerchantCreatedDomainEvent>`)
- **Canonical ConsumerName**: `"Notifications.MerchantCreated"`
- **IConsumerIdempotencyService**: Injected and invoked (`_idempotencyService.ExecuteAsync`)
- **Business Effect**: Creates a merchant welcome `Notification` record in `notifications` table
- **Dual-Layer Idempotency**:
  - Layer 1: Consumer Inbox (`consumer_inbox_messages`, unique key: `(EventId, Consumer)`)
  - Layer 2: Business effect unique constraint (`notifications`, `UNIQUE(EventId, Type) WHERE EventId IS NOT NULL`)
- **Status**: Production-Ready / Fully Verified

### Consumer 2: `Notifications.MerchantStatusChanged`
- **Handler Class**: `MerchantStatusChangedNotificationHandler`
- **Module**: `Notifications` (`LastHour.Modules.Notifications.Application.Consumers`)
- **Event Consumed**: `MerchantStatusChangedDomainEvent` (`NotificationMessage<MerchantStatusChangedDomainEvent>`)
- **Canonical ConsumerName**: `"Notifications.MerchantStatusChanged"`
- **IConsumerIdempotencyService**: Injected and invoked (`_idempotencyService.ExecuteAsync`)
- **Business Effect**: Creates a status change `Notification` record in `notifications` table (Activated, Suspended, Reactivated, Closed)
- **Dual-Layer Idempotency**:
  - Layer 1: Consumer Inbox (`consumer_inbox_messages`, unique key: `(EventId, Consumer)`)
  - Layer 2: Business effect unique constraint (`notifications`, `UNIQUE(EventId, Type) WHERE EventId IS NOT NULL`)
- **Status**: Production-Ready / Fully Verified

### Consumer 3: `Notifications.MerchantProfileUpdated`
- **Handler Class**: `MerchantProfileUpdatedNotificationHandler`
- **Module**: `Notifications` (`LastHour.Modules.Notifications.Application.Consumers`)
- **Event Consumed**: `MerchantProfileUpdatedDomainEvent` (`NotificationMessage<MerchantProfileUpdatedDomainEvent>`)
- **Canonical ConsumerName**: `"Notifications.MerchantProfileUpdated"`
- **IConsumerIdempotencyService**: Injected and invoked (`_idempotencyService.ExecuteAsync`)
- **Business Effect**: Creates a profile update `Notification` record in `notifications` table ("Merchant Profile Updated")
- **Dual-Layer Idempotency**:
  - Layer 1: Consumer Inbox (`consumer_inbox_messages`, unique key: `(EventId, Consumer)`)
  - Layer 2: Business effect unique constraint (`notifications`, `UNIQUE(EventId, Type) WHERE EventId IS NOT NULL`)
- **Status**: Production-Ready / Fully Verified

### Consumer 4: `Notifications.UserRegistered`
- **Handler Class**: `UserRegisteredNotificationHandler`
- **Module**: `Notifications` (`LastHour.Modules.Notifications.Application.Consumers`)
- **Event Consumed**: `UserRegisteredDomainEvent` (`NotificationMessage<UserRegisteredDomainEvent>`)
- **Canonical ConsumerName**: `"Notifications.UserRegistered"`
- **IConsumerIdempotencyService**: Injected and invoked (`_idempotencyService.ExecuteAsync`)
- **Business Effect**: Creates a registration welcome `Notification` record in `notifications` table ("Welcome to LastHour")
- **Dual-Layer Idempotency**:
  - Layer 1: Consumer Inbox (`consumer_inbox_messages`, unique key: `(EventId, Consumer)`)
  - Layer 2: Business effect unique constraint (`notifications`, `UNIQUE(EventId, Type) WHERE EventId IS NOT NULL`)
- **Status**: Production-Ready / Fully Verified

### Consumer 5: `Notifications.UserPasswordChanged`
- **Handler Class**: `UserPasswordChangedNotificationHandler`
- **Module**: `Notifications` (`LastHour.Modules.Notifications.Application.Consumers`)
- **Event Consumed**: `UserPasswordChangedDomainEvent` (`NotificationMessage<UserPasswordChangedDomainEvent>`)
- **Canonical ConsumerName**: `"Notifications.UserPasswordChanged"`
- **IConsumerIdempotencyService**: Injected and invoked (`_idempotencyService.ExecuteAsync`)
- **Business Effect**: Creates a security `Notification` record in `notifications` table ("Password Changed")
- **Dual-Layer Idempotency**:
  - Layer 1: Consumer Inbox (`consumer_inbox_messages`, unique key: `(EventId, Consumer)`)
  - Layer 2: Business effect unique constraint (`notifications`, `UNIQUE(EventId, Type) WHERE EventId IS NOT NULL`)
- **Status**: Production-Ready / Fully Verified

### Consumer 6: `Notifications.UserLockedOut`
- **Handler Class**: `UserLockedOutNotificationHandler`
- **Module**: `Notifications` (`LastHour.Modules.Notifications.Application.Consumers`)
- **Event Consumed**: `UserLockedOutDomainEvent` (`NotificationMessage<UserLockedOutDomainEvent>`)
- **Canonical ConsumerName**: `"Notifications.UserLockedOut"`
- **IConsumerIdempotencyService**: Injected and invoked (`_idempotencyService.ExecuteAsync`)
- **Business Effect**: Creates an account lockout security `Notification` record in `notifications` table ("Account Temporarily Locked")
- **Dual-Layer Idempotency**:
  - Layer 1: Consumer Inbox (`consumer_inbox_messages`, unique key: `(EventId, Consumer)`)
  - Layer 2: Business effect unique constraint (`notifications`, `UNIQUE(EventId, Type) WHERE EventId IS NOT NULL`)
- **Status**: Production-Ready / Fully Verified

---

## 7. Event-to-Consumer Matrix

| Event | Module Producer | Aggregate | Outbox Status | Existing Consumers | ConsumerName | Business Effect | Coverage Status |
|---|---|---|---|---|---|---|---|
| `MerchantCreatedDomainEvent` | Merchant | Merchant | Fully Integrated | `MerchantCreatedNotificationHandler` | `Notifications.MerchantCreated` | Welcome Notification | **Fully Covered** |
| `MerchantStatusChangedDomainEvent` | Merchant | Merchant | Fully Integrated | `MerchantStatusChangedNotificationHandler` | `Notifications.MerchantStatusChanged` | Status Transition Notification | **Fully Covered** |
| `MerchantProfileUpdatedDomainEvent` | Merchant | Merchant | Fully Integrated | `MerchantProfileUpdatedNotificationHandler` | `Notifications.MerchantProfileUpdated` | Profile Update Notification | **Fully Covered** |
| `UserRegisteredDomainEvent` | Identity | User | Fully Integrated | `UserRegisteredNotificationHandler` | `Notifications.UserRegistered` | Welcome Registration Notification | **Fully Covered** |
| `UserPasswordChangedDomainEvent` | Identity | User | Fully Integrated | `UserPasswordChangedNotificationHandler` | `Notifications.UserPasswordChanged` | Password Changed Notification | **Fully Covered** |
| `UserLockedOutDomainEvent` | Identity | User | Fully Integrated | `UserLockedOutNotificationHandler` | `Notifications.UserLockedOut` | Account Lockout Security Alert | **Fully Covered** |

---

## 8. Cross-Module Boundary Standards

1. **Shared Contract Placement**:
   - Cross-module domain events MUST be defined in `LastHour.BuildingBlocks.Contracts.Events`.
   - They MUST contain only primitive types, string values, and strongly typed IDs.
   - They MUST NEVER expose internal domain entity instances, module-private value objects, or sensitive credentials (passwords, hashes, tokens).

2. **Consumer Placement & Isolation**:
   - Consumers MUST be located in `<Module>.Application.Consumers`.
   - Consumers MUST implement `INotificationHandler<NotificationMessage<TEvent>>`.
   - Consumers MUST define `public const string ConsumerName = "<Module>.<Operation>"`.
   - Consumers MUST inject and use `IConsumerIdempotencyService`.
   - Consumers MUST persist changes only through their own module's `IUnitOfWork` and repositories.
   - Consumers MUST NOT reference another module's `Domain`, `Infrastructure`, or `Persistence` projects directly.

---

## 9. Event Processing Reliability Model

1. **At-Least-Once Delivery**:
   - Outbox messages are stored atomically with domain state modifications via `DomainEventsInterceptor`.
   - `OutboxProcessor` drains pending messages in batches, claiming leases for a configurable `LeaseDuration`.
   - If a background worker crashes mid-execution, uncompleted messages have their expired leases reclaimed on subsequent cycles.

2. **Poison Message Containment**:
   - Exponential backoff with `min(BaseRetryDelay * 2^RetryCount, MaximumRetryDelay)` avoids hot retry loops.
   - Messages exceeding `MaxRetryCount` are transitioned to an abandoned state with their final error metadata recorded.
   - Abandoned messages are safely requeued via `Requeue(DateTime currentUtc)` for administrative recovery.

3. **Dual-Layer Consumer Idempotency**:
   - **Layer 1**: `IConsumerIdempotencyService` enforces consumer-level idempotency using `consumer_inbox_messages` table `(EventId, ConsumerName)`.
   - **Layer 2**: Target module databases enforce business uniqueness constraints (`IX_notifications_event_id_type`).
   - Uniqueness conflicts resulting from concurrent delivery races are classified as idempotent successes via `NotificationDbExceptionDetector`, while unexpected database faults propagate for retry.

4. **Observability & Privacy**:
   - Low-cardinality metrics (`last_hour.outbox.*`, `last_hour.consumer.*`) and OpenTelemetry activity traces provide full operational visibility.
   - Sensitive credential material and PII are strictly excluded from metric dimensions, logs, and domain event payloads.

---

## 10. Notification Lifecycle Subsystem

1. **Internal Notification States**:
   - Notifications created by domain event consumers are initialized in the `Unread` state (`IsRead = false, ReadAtUtc = null`).
   - Transition to `Read` occurs via `Notification.MarkAsRead(DateTime utcNow)` which synchronizes `IsRead = true` and `ReadAtUtc = utcNow`.
   - Repeated calls to `MarkAsRead` are strictly idempotent and preserve the initial read timestamp.

2. **Query & Management APIs**:
   - `GET /api/v1/notifications`: Paginated feed scoped strictly to the authenticated user (`RecipientId == AuthenticatedUserId`), with deterministic ordering (`CreatedAtUtc DESC`, `Id DESC`) and optional read-status or event-type filters.
   - `PUT /api/v1/notifications/{id}/read`: Mark individual notification as read with anti-probing 404 security.
   - `POST /api/v1/notifications/read-all`: Transactional bulk transition of all unread notifications for the authenticated user.

---

## 11. External Multi-Channel Notification Delivery Architecture

1. **Pipeline & Separation of Concerns**:
   - `Domain Event` $\rightarrow$ `Transactional Outbox` $\rightarrow$ `Domain Event Consumer` $\rightarrow$ `Notification Aggregate persisted` $\rightarrow$ `Notification Delivery Intents persisted` $\rightarrow$ `NotificationDeliveryProcessor` $\rightarrow$ `Delivery Channel Resolution` $\rightarrow$ `Delivery Attempt` $\rightarrow$ `External Provider Adapter`.
   - External delivery is completely decoupled from domain event outbox processing and domain business logic.

2. **Channel Resolution & State Machine**:
   - `INotificationDeliveryPolicyResolver` determines channels (`InApp`, `Email`, `Sms`, `Push`). Security events (`UserPasswordChanged`, `UserLockedOut`) resolve both `InApp` and `Email`.
   - `NotificationDelivery` aggregate root manages delivery lifecycle (`Pending`, `Processing`, `Delivered`, `RetryScheduled`, `FailedPermanent`).
   - Lease locks (`ClaimToken`, `LockExpiresAtUtc`), composite unique constraint `(NotificationId, Channel)` on `notification_deliveries`, and composite index on `(Status, NextAttemptAtUtc)` guarantee worker safety and fast polling.

3. **Observability & Recipient Privacy**:
   - OpenTelemetry metrics (`LastHour.NotificationDelivery`): `processed`, `delivered`, `failed`, `retried`, `abandoned`, `reclaimed`, `claim_conflict`, `processing_duration`.
   - Strictly zero PII in metric tags or log parameters.

---

## 12. Notification Preferences, Templates & Policy Resolution Subsystem

1. **User Notification Preferences (`NotificationPreference`)**:
   - Stores user-level opt-in / opt-out preferences per `(UserId, NotificationType, Channel)`.
   - Security Invariant: `InApp` channel for security events (`UserPasswordChanged`, `UserLockedOut`) is mandatory and cannot be disabled by user opt-out. Email channel for security events defaults to enabled but respects explicit user opt-out (`IsEnabled = false`).
   - Defaults: Security events $\rightarrow$ `[InApp, Email]`; Standard events $\rightarrow$ `[InApp]`. SMS and Push default to disabled.

2. **Notification Templates (`NotificationTemplate`)**:
   - Stores versioned templates per `(TemplateKey, Channel, Version)` with active flag (`IsActive`).
   - Unique index `IX_notification_templates_key_channel_version` and index `IX_notification_templates_type_channel_active`.
   - Provider-neutral template rendering (`NotificationTemplateRenderer`) with safe placeholder substitution (`{{VarName}}`). Zero reflection, zero expression execution.

3. **Quiet Hours (`NotificationQuietHours`)**:
   - User quiet-hours window (`StartLocalTime`, `EndLocalTime`, `TimeZoneId`).
   - InApp delivery is immediate; Email/SMS/Push channels are deferred outside quiet hours.

4. **API Security & User Scoping**:
   - Endpoints (`/api/v1/notification-preferences`) strictly derive `UserId` from authenticated JWT claims (`ClaimsPrincipal.GetUserId()`). Request route/body cannot override `UserId`.

---

## 13. Notification Operations, Administrative Requeue & Data Retention

1. **Operational CQRS Architecture**:
   - `Notification Delivery` $\rightarrow$ `Delivery Attempts` $\rightarrow$ `Operational Audit` $\rightarrow$ `CQRS Operations` $\rightarrow$ `Administrative API` $\rightarrow$ `Authorization`.
   - Read queries (`GetNotificationDeliveriesQuery`, `GetNotificationDeliveryDetailsQuery`, `GetNotificationDeliveryAttemptsQuery`, `GetNotificationDeliveryAuditQuery`) project low-cardinality zero-PII operational DTOs.

2. **Administrative Requeue Workflows**:
   - Single (`RequeueNotificationDeliveryCommand`) and Batch (`RequeueNotificationDeliveriesCommand`) commands.
   - Enforces terminal status (`Delivered`) and active lease safety.
   - Supports batch partial success (1 to 100 items): invalid state or concurrency conflict on individual items results in itemized rejections without aborting remaining valid items.
   - `DbUpdateConcurrencyException` triggers EF Core entry detachment on the conflicted entity to prevent context poisoning across batch items.

3. **Security & Claim-Derived Identity (Zero Client AdminUserId Rule)**:
   - Administrative API endpoints (`/api/v1/admin/notification-deliveries/*`) enforce `Notifications.Read` and `Notifications.Manage` permissions.
   - Administrative actor identity (`AdminUserId`) is extracted EXCLUSIVELY from authenticated claims via `ClaimsPrincipal.GetUserId()`. Request bodies and routes NEVER accept `AdminUserId`.

4. **Data Retention Policy & Bounded Cleanup**:
   - `Historical Operational Data` $\rightarrow$ `Retention Policy` $\rightarrow$ `Bounded Cleanup`.
   - Options: `NotificationOperationsRetentionOptions` (`AttemptRetentionDays`, `AuditRetentionDays`, `CleanupBatchSize`, `ExecutionIntervalMinutes`).
   - `NotificationOperationsRetentionService` executes bounded batch deletion of expired attempt history (`notification_delivery_attempts`) and audit log entries (`notification_delivery_audit_entries`).
   - **Retention Invariant**: Retention targets ONLY historical execution attempt and audit entries. It NEVER deletes `NotificationDelivery` aggregates, `Notification` domain aggregates, or active operational records.

