# ADR-023: Identity Password-Changed Notification Consumer & Security Idempotency

## Status
Accepted (Sprint 10.15-B)

## Context
In Sprint 10.15-A, the `Identity` module was equipped with domain event foundations (`UserRegisteredDomainEvent`, `UserPasswordChangedDomainEvent`, and `UserLockedOutDomainEvent`).

When a user modifies their authentication credential (`User.MarkPasswordChanged`), security best practices mandate notifying the account holder immediately about the credential mutation. This alerts the user in cases of unauthorized credential updates or account compromise.

This notification must be generated asynchronously across the modular boundary without:
1. Coupling `Notifications` to `Identity.Domain`.
2. Exposing sensitive credential material (cleartext passwords, Argon2id hashes, refresh tokens, session identifiers).
3. Querying `Identity` database synchronously inside the consumer.
4. Violating at-least-once outbox delivery guarantees or creating duplicate security alerts.

## Decision

1. **Implement `Notifications.UserPasswordChanged` Consumer**:
   - Resides in `LastHour.Modules.Notifications.Application.Consumers.UserPasswordChangedNotificationHandler`.
   - Implements `INotificationHandler<NotificationMessage<UserPasswordChangedDomainEvent>>`.
   - Declares `public const string ConsumerName = "Notifications.UserPasswordChanged"`.

2. **Strict Security Isolation**:
   - The event contract `UserPasswordChangedDomainEvent` contains only `Guid UserId` and base `EventId` / `OccurredOn`.
   - The consumer creates a `Notification` with:
     - `Title = "Password Changed"`
     - `Body = "Your password has been changed successfully."`
     - `Type = "UserPasswordChanged"`
     - `RecipientId = domainEvent.UserId`
     - `Recipient = domainEvent.UserId.ToString("D")`
     - `EventId = domainEvent.EventId`
   - The consumer does NOT query `Identity` to enrich the payload, eliminating cross-module runtime dependencies and latency.

3. **Dual-Layer Idempotency**:
   - **Layer 1 (Consumer Inbox Boundary)**: `consumer_inbox_messages` table row `(EventId, "Notifications.UserPasswordChanged")` managed by `IConsumerIdempotencyService`.
   - **Layer 2 (Business Effect Protection)**:
     - Application pre-check: `_notificationRepository.ExistsByEventIdAndTypeAsync(domainEvent.EventId, "UserPasswordChanged", ct)`.
     - Database uniqueness constraint: `IX_notifications_event_id_type` on `(EventId, Type)`.
     - Unique constraint violations are gracefully caught via `NotificationDbExceptionDetector.IsNotificationUniqueConstraintViolation` so duplicate race attempts complete cleanly without crashing.

4. **Production Observability & Structured Logging**:
   - Uses compile-time `LoggerMessage.Define` for zero-allocation structured logs.
   - Emits standardized metrics via `LastHour.ConsumerIdempotency` meter (`last_hour.consumer.processed`, `last_hour.consumer.duplicate`, `last_hour.consumer.failed`, `last_hour.consumer.processing_duration`).
   - Tags metrics only with low-cardinality dimensions (`consumer`, `failure_type`).

## Consequences

### Positive
- Users receive timely security notifications when passwords change.
- Strict isolation: zero credential data is handled by or stored in the `Notifications` module.
- High resilience: dual-layer idempotency prevents duplicate emails or notifications during network retries or worker crashes.
- Clean Architecture: follows established standardized consumer pattern (ADR-020).

### Negative / Trade-offs
- The notification recipient is initially keyed by `UserId.ToString("D")`; when a dedicated delivery worker (email/SMS dispatcher) is introduced in a future sprint, it will resolve the destination contact handle asynchronously.
