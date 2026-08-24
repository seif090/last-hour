# ADR-025: Identity User-Registered Welcome Notification Consumer & Privacy Boundaries

## Status
Accepted (Sprint 10.15-D)

## Context
When a new user registers on the platform (`User.Create`), the account is provisioned in `AccountStatus.Pending` and emits `UserRegisteredDomainEvent`.

Customer onboarding and security practices require generating an immediate welcome and registration confirmation notification.

This notification must be generated asynchronously across the modular boundary without:
1. Coupling `Notifications` to `Identity.Domain` or `Identity` repositories.
2. Exposing sensitive credential material (passwords, Argon2id hashes, security tokens).
3. Leaking personal data (e.g. embedding email addresses unnecessarily into notification bodies or log streams).
4. Violating at-least-once outbox delivery guarantees or producing duplicate welcome notifications.

## Decision

1. **Implement `Notifications.UserRegistered` Consumer**:
   - Resides in `LastHour.Modules.Notifications.Application.Consumers.UserRegisteredNotificationHandler`.
   - Implements `INotificationHandler<NotificationMessage<UserRegisteredDomainEvent>>`.
   - Declares `public const string ConsumerName = "Notifications.UserRegistered"`.

2. **Deterministic Business Effect & Privacy Boundaries**:
   - The event contract `UserRegisteredDomainEvent` contains `Guid UserId` and `string Email`.
   - The consumer creates a `Notification` with:
     - `Title = "Welcome to LastHour"`
     - `Body = "Your account has been created successfully. Welcome to LastHour."`
     - `Type = "UserRegistered"`
     - `RecipientId = domainEvent.UserId`
     - `Recipient = domainEvent.UserId.ToString("D")`
     - `EventId = domainEvent.EventId`
     - `CreatedAtUtc = _clock.UtcNow`
   - The user's email address is intentionally not placed in the notification body or log messages to prevent unnecessary PII proliferation.

3. **Dual-Layer Idempotency**:
   - **Layer 1 (Consumer Inbox Boundary)**: `consumer_inbox_messages` table row `(EventId, "Notifications.UserRegistered")` managed by `IConsumerIdempotencyService`.
   - **Layer 2 (Business Effect Protection)**:
     - Application pre-check: `_notificationRepository.ExistsByEventIdAndTypeAsync(domainEvent.EventId, "UserRegistered", ct)`.
     - Database uniqueness constraint: `IX_notifications_event_id_type` on `(EventId, Type)`.
     - Unique constraint violations are gracefully caught via `NotificationDbExceptionDetector.IsNotificationUniqueConstraintViolation` so duplicate race attempts complete cleanly without bubbling errors.

4. **Production Observability & Structured Logging**:
   - Uses compile-time `LoggerMessage.Define` for zero-allocation structured logs (`UserRegisteredNotificationCreated`, `UserRegisteredNotificationAlreadyExists`).
   - Emits standardized metrics via `LastHour.ConsumerIdempotency` meter (`last_hour.consumer.processed`, `last_hour.consumer.duplicate`, `last_hour.consumer.failed`, `last_hour.consumer.processing_duration`).
   - Tags metrics only with low-cardinality dimensions (`consumer`, `failure_type`).

## Consequences

### Positive
- Every registered user receives a welcome notification reliably under at-least-once outbox semantics.
- Strict isolation: zero credential data is handled by or stored in the `Notifications` module.
- High resilience: dual-layer idempotency prevents duplicate notifications during network retries or worker crashes.
- 100% of defined domain events across Merchant and Identity modules are now actively consumed in production (0 producer-only events).
- Clean Architecture: follows established standardized consumer pattern (ADR-020).

### Negative / Trade-offs
- The notification recipient is initially keyed by `UserId.ToString("D")`; when a dedicated delivery worker (email/SMS dispatcher) is introduced in a future sprint, it will resolve the destination contact handle asynchronously.
