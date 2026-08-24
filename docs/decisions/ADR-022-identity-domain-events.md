# ADR-022: Identity Domain Events Foundation & Security Boundaries

## Status
Accepted (Sprint 10.15-A)

## Context
The **LastHour** platform uses a modular monolith architecture with a production-grade Transactional Outbox pattern for decoupled, reliable cross-module event communication.

Prior to Sprint 10.15-A, only the `Merchant` module raised domain events (`MerchantCreatedDomainEvent`, `MerchantStatusChangedDomainEvent`, `MerchantProfileUpdatedDomainEvent`), all of which are consumed by the `Notifications` module.

The `Identity` module contains critical domain lifecycle and security state transitions:
1. User registration (`User.Create`)
2. Password credential replacement (`User.MarkPasswordChanged`)
3. Account security lockout (`User.RecordFailedLogin` transitioning into locked status)

Downstream modules (such as `Notifications`, `Analytics`, `Administration/Audit`, and `Security/SIEM`) require notification of these transitions without introducing tight compile-time or runtime coupling to `Identity.Domain` or internal identity repositories.

Crucially, events emanating from the `Identity` boundary present unique security risks: sensitive credentials, cleartext passwords, password hashes, and session secrets must never leak into event contracts, outbox payloads, logs, traces, or downstream consumer stores.

## Decision

1. **Emit Domain Events on Successful State Transitions**:
   - The `User` aggregate root is responsible for raising domain events when and only when aggregate state transitions succeed.
   - `User.Create` raises `UserRegisteredDomainEvent`.
   - `User.MarkPasswordChanged` raises `UserPasswordChangedDomainEvent`.
   - `User.RecordFailedLogin` raises `UserLockedOutDomainEvent` **only on the transition** when `FailedAccessCount` reaches the configured `maxFailedAttempts` threshold and the account transitions into a locked-out state. It does not raise events on every individual failed attempt, nor does it re-raise on subsequent attempts while already locked out.

2. **Strict Credential & Secret Exclusion**:
   - Domain event contracts MUST NOT contain passwords, cleartext secrets, password hashes (Argon2id hashes), refresh tokens, access tokens, or security answers.
   - Event contracts contain only contract-safe identifiers and primitive types:
     - `UserRegisteredDomainEvent(Guid UserId, string Email)`
     - `UserPasswordChangedDomainEvent(Guid UserId)`
     - `UserLockedOutDomainEvent(Guid UserId, DateTime LockoutUntilUtc)`

3. **Contract Placement in `LastHour.BuildingBlocks.Contracts.Events`**:
   - All three domain events inherit `BaseDomainEvent` and reside in `LastHour.BuildingBlocks.Contracts.Events`.
   - The events have zero dependencies on `Identity.Domain` value objects (`UserId`, `Email`, `PasswordCredential`, `AccountStatus`).

4. **Transactional Outbox Integration**:
   - Events raised by `User` are automatically captured by `DomainEventsInterceptor.SavingChangesAsync`, serialized into polymorphic JSON via `OutboxJson`, and persisted atomically in the `outbox_messages` table within the same database transaction.
   - Dispatched sequentially by `OutboxProcessor` and `EventDispatcher` wrapped in `NotificationMessage<T>`.

5. **Producer-Only Foundation in Sprint 10.15-A**:
   - In Sprint 10.15-A, these three Identity events are established as durable producer-only foundation events.
   - Consumers (`Notifications.UserWelcome`, `Notifications.UserPasswordChanged`, `Notifications.UserLockedOut`) will be introduced in subsequent focused sprints following the standardized consumer pattern (ADR-020).

## Consequences

### Positive
- Cross-module decoupling: Other modules can react to identity lifecycle events via the Transactional Outbox without referencing `Identity.Domain`.
- Data security: Zero sensitive credentials or hashes leak into outbox payloads, event queues, or logs.
- High semantic precision: Events strictly represent committed domain state transitions.
- Reliability: Atomic outbox persistence ensures at-least-once delivery guarantee across the platform.

### Negative / Trade-offs
- Outbox message volume increases with each user registration, password change, and lockout transition (mitigated by existing single-instance outbox leasing and cleanup mechanisms).
