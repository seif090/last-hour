# ADR-026: Event Processing Reliability Model, Poison Event Protection & Architectural Hardening

## Status
Accepted (Sprint 10.16)

## Context
Across Sprints 10.10 through 10.15, the **LastHour Backend** solution completed the foundation for asynchronous event-driven communication via Transactional Outbox, In-Process MediatR dispatching, and Consumer Inbox idempotency.

With six production domain events active across the Merchant and Identity modules:
1. `MerchantCreatedDomainEvent`
2. `MerchantStatusChangedDomainEvent`
3. `MerchantProfileUpdatedDomainEvent`
4. `UserRegisteredDomainEvent`
5. `UserPasswordChangedDomainEvent`
6. `UserLockedOutDomainEvent`

It is essential to formally codify and harden the reliability guarantees, failure classification, poison message containment, lease lifecycle, ordering policies, observability boundaries, and architectural governance rules.

## Decision

1. **Outbox Reliability & Lifecycle Model**:
   - **Persistence**: Outbox messages are atomically persisted in `outbox_messages` as part of aggregate command transactions via `DomainEventsInterceptor`.
   - **State Progression**: `Pending` (`ProcessedOnUtc == null && NextAttemptAtUtc <= now && LockExpiresAtUtc < now`) $\rightarrow$ `Claimed` (`ClaimToken != null, LockExpiresAtUtc = now + LeaseDuration`) $\rightarrow$ `Dispatched` $\rightarrow$ `Processed` (`ProcessedOnUtc = now, LockExpiresAtUtc = null, NextAttemptAtUtc = null, Error = null`).
   - **At-Least-Once Delivery**: Messages are processed at-least-once. Consumer idempotency guarantees exact-once business outcomes.

2. **Poison Event Containment & Backoff**:
   - **Exponential Backoff**: When dispatch fails, retry is scheduled with exponential backoff: `min(BaseRetryDelay * 2^(RetryCount), MaximumRetryDelay)`.
   - **Poison Abandonment**: Upon reaching `MaxRetryCount`, the message is marked abandoned (`ProcessedOnUtc = failedOnUtc, NextAttemptAtUtc = null, Error = lastExceptionMessage`). This permanently stops hot-looping in background processors.
   - **Manual Requeue**: Abandoned messages can be inspected and safely requeued (`Requeue(DateTime currentUtc)` $\rightarrow$ resets retry count and schedules immediate eligibility) via administrative APIs.

3. **Consumer Inbox Lease & Dual-Layer Idempotency**:
   - **Layer 1 (Consumer Boundary)**: `consumer_inbox_messages` table stores `(EventId, Consumer)` with optimistic concurrency tokens on `LockExpiresAtUtc`.
   - **Lease Claim & Reclaim**: Workers claim a lease for `LeaseDuration`. If a worker crashes before completion, subsequent cycles reclaim the expired lease safely.
   - **Layer 2 (Business Effect Protection)**: Downstream entity stores enforce domain-level unique constraints (e.g. `IX_notifications_event_id_type` on `notifications`). Application pre-checks (`ExistsByEventIdAndTypeAsync`) avoid redundant writes, while `NotificationDbExceptionDetector.IsNotificationUniqueConstraintViolation` absorbs concurrent insertion races idempotently without bubbling exceptions.
   - **Non-Unique Failures**: Any unrelated database or infrastructure exception immediately bubbles up to prompt outbox retry.

4. **Event Ordering Policy**:
   - **Outbox Polling Order**: Outbox processor queries order by `NextAttemptAtUtc` ascending, then `OccurredOnUtc` ascending.
   - **Sequential In-Process Dispatch**: Each batch is dispatched sequentially within its execution scope.
   - **Causal Ordering**: Business operations across independent aggregates (e.g. `UserRegistered`, `UserPasswordChanged`, `UserLockedOut`) are commutative at the consumer inbox level. Consumers process each event independently through idempotent handlers. Global distributed total ordering is deliberately rejected to avoid distributed locking bottlenecks.

5. **Failure Classification & Structured Observability**:
   - **Deterministic Classification**: `FailureClassifier` maps exceptions into bounded low-cardinality strings (`Cancellation`, `Database`, `Concurrency`, `Validation`, `Infrastructure`, `Transient`, `Unknown`).
   - **Observability Metrics**: `LastHour.Outbox` and `LastHour.ConsumerIdempotency` meters emit counters and histograms tagged only by low-cardinality keys (`consumer`, `failure_type`).
   - **Privacy Boundary**: Metric tags, log events, and trace attributes strictly prohibit sensitive data, user emails, passwords, hashes, tokens, or arbitrary payload blobs.

6. **Automated Architectural Governance**:
   - Automated reflection and NetArchTest architecture tests enforce:
     1. All domain events inherit `BaseDomainEvent` and reside in `LastHour.BuildingBlocks.Contracts.Events`.
     2. Contracts contain zero sensitive credential properties (`Password`, `PasswordHash`, `RefreshToken`, `AccessToken`, `Jwt`, `Secret`, `Credential`).
     3. Contracts have zero references to any `LastHour.Modules.*.Domain` namespaces.
     4. Consumers reside in `<Module>.Application.Consumers` and implement `INotificationHandler<NotificationMessage<TEvent>>`.
     5. Consumers declare a unique `ConsumerName` matching `<ModuleName>.<OperationName>`.
     6. Consumers never reference `Infrastructure`, `DbContext`, or another module's domain directly.

## Consequences

### Positive
- Production resilience: zero event loss during worker crashes, network failures, or concurrent redeliveries.
- Zero infinite retry loops: poison messages are safely contained with bounded retries and exponential backoff.
- Zero security leaks: contract reflection tests prevent accidental exposure of credentials or PII in domain events and logs.
- Strict architectural compliance: automated regression gates ensure modular monolith boundaries remain inviolable.

### Negative / Trade-offs
- Abandoned poison messages require operator investigation and manual requeuing via administration endpoints if recovery is desired.
