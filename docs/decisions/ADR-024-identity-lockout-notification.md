# ADR-024: Identity Account-Lockout Notification Consumer & Security Idempotency

## Status
Accepted (Sprint 10.15-C)

## Context
When a user account exceeds the configured threshold of consecutive failed authentication attempts (`User.RecordFailedLogin`), the user account transitions into a temporary locked-out state (`AccountStatus.Locked` / `LockoutUntilUtc`), raising `UserLockedOutDomainEvent`.

Security best practices require notifying the account owner immediately when an account lockout occurs. This alerts the legitimate owner of potentially hostile brute-force or credential-stuffing attacks targeting their credentials.

This security alert must be generated asynchronously across the modular boundary without:
1. Coupling `Notifications` to `Identity.Domain`.
2. Exposing sensitive diagnostic details (failed password strings, password hashes, attempt counts, IP addresses).
3. Querying `Identity` database synchronously inside the consumer.
4. Violating at-least-once outbox delivery guarantees or causing duplicate lockout notifications during network retries or transient crashes.

## Decision

1. **Implement `Notifications.UserLockedOut` Consumer**:
   - Resides in `LastHour.Modules.Notifications.Application.Consumers.UserLockedOutNotificationHandler`.
   - Implements `INotificationHandler<NotificationMessage<UserLockedOutDomainEvent>>`.
   - Declares `public const string ConsumerName = "Notifications.UserLockedOut"`.

2. **Security Isolation & Minimal Contract**:
   - The event contract `UserLockedOutDomainEvent` contains only `Guid UserId` and `DateTime LockoutUntilUtc`.
   - The consumer creates a `Notification` with:
     - `Title = "Account Temporarily Locked"`
     - `Body = "Your account has been temporarily locked after multiple failed login attempts."`
     - `Type = "UserLockedOut"`
     - `RecipientId = domainEvent.UserId`
     - `Recipient = domainEvent.UserId.ToString("D")`
     - `EventId = domainEvent.EventId`
   - The consumer does NOT query `Identity` to enrich the payload, preserving module isolation and eliminating latency bottlenecks.

3. **Dual-Layer Idempotency**:
   - **Layer 1 (Consumer Inbox Boundary)**: `consumer_inbox_messages` table row `(EventId, "Notifications.UserLockedOut")` managed by `IConsumerIdempotencyService`.
   - **Layer 2 (Business Effect Protection)**:
     - Application pre-check: `_notificationRepository.ExistsByEventIdAndTypeAsync(domainEvent.EventId, "UserLockedOut", ct)`.
     - Database uniqueness constraint: `IX_notifications_event_id_type` on `(EventId, Type)`.
     - Unique constraint violations are gracefully caught via `NotificationDbExceptionDetector.IsNotificationUniqueConstraintViolation` so duplicate race attempts complete cleanly without bubbling errors.

4. **Production Observability & Structured Logging**:
   - Uses compile-time `LoggerMessage.Define` for zero-allocation structured logs (`UserLockedOutNotificationCreated`, `UserLockedOutNotificationAlreadyExists`).
   - Emits standardized metrics via `LastHour.ConsumerIdempotency` meter (`last_hour.consumer.processed`, `last_hour.consumer.duplicate`, `last_hour.consumer.failed`, `last_hour.consumer.processing_duration`).
   - Tags metrics only with low-cardinality dimensions (`consumer`, `failure_type`).

## Consequences

### Positive
- Users receive timely security alerts when account lockouts occur.
- Strict isolation: zero credential data or attack telemetry is exposed to or handled by the `Notifications` module.
- High resilience: dual-layer idempotency prevents duplicate notifications during network retries or worker crashes.
- Clean Architecture: follows established standardized consumer pattern (ADR-020).

### Negative / Trade-offs
- The notification recipient is initially keyed by `UserId.ToString("D")`; a dedicated communication delivery worker in a future sprint will resolve the contact channel asynchronously.
