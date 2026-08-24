# ADR-030: Notification Operations, Administrative Requeue, and Data Retention Policy

## Status
Accepted

## Context
Sprint 10.20 introduced operational capabilities for the Notification Delivery subsystem, enabling enterprise visibility, administrative delivery recovery, execution history tracking, audit logging, and data retention maintenance. Operational management requires strong administrative separation, claim-derived security, zero PII leakage, and strict concurrency safety.

## Decision

1. **Administrative Control Separation**:
   - Operational management of notification deliveries is separated into distinct administrative CQRS query and command interfaces (`GetNotificationDeliveriesQuery`, `GetNotificationDeliveryDetailsQuery`, `GetNotificationDeliveryAttemptsQuery`, `GetNotificationDeliveryAuditQuery`, `RequeueNotificationDeliveryCommand`, `RequeueNotificationDeliveriesCommand`).

2. **Claim-Derived Administrative Identity (Zero Client AdminUserId Rule)**:
   - Administrative commands and HTTP API contracts MUST NEVER accept `AdminUserId` or `ActorId` from client request bodies, route parameters, headers, or query parameters.
   - Administrative identity is derived exclusively from the authenticated user's claims context via `ClaimsPrincipal.GetUserId()`.

3. **Requeue Semantics & State Constraints**:
   - Administrative requeue is supported only for non-terminal failed or stuck deliveries (`FailedPermanent`, `RetryScheduled`, or expired processing worker leases).
   - Requeue attempts on `Delivered` or actively leased processing deliveries are deterministically rejected with `RequeueRejected`.
   - Requeuing resets `RetryCount` to 0, clears `LastError`, clears claim tokens and lock expiration timestamps, and transitions status to `Pending`.

4. **Attempt History & Audit Logging (Append-Only)**:
   - Execution attempt history (`notification_delivery_attempts`) and operational audit entries (`notification_delivery_audit_entries`) are strictly append-only. Repositories do not expose update or delete methods for individual entries.

5. **Data Retention Policy & Bounded Cleanup**:
   - Retention policy is configured via `NotificationOperationsRetentionOptions` with safe bounds (`AttemptRetentionDays`: 1–3650, `AuditRetentionDays`: 1–3650, `CleanupBatchSize`: 1–1000).
   - `NotificationOperationsRetentionService` executes bounded batch deletion of expired attempt history and audit records.
   - **Non-Goal & Invariant**: Retention cleanup targets ONLY `notification_delivery_attempts` and `notification_delivery_audit_entries`. It NEVER deletes `NotificationDelivery` aggregate records, `Notification` domain aggregates, or active operational records.

6. **Zero-PII Policy**:
   - Operational query projections, attempt logs, audit entries, metrics, and background worker log templates MUST NOT expose recipient names, email addresses, phone numbers, notification bodies, rendered templates, or provider credentials.

7. **Batch Requeue Partial Success & Concurrency Protection**:
   - Batch administrative requeues support partial success (1 to 100 items). Invalid state, missing deliveries, or `ClaimToken` optimistic concurrency conflicts on individual items are recorded as itemized rejections without aborting remaining valid items in the batch.
   - `DbUpdateConcurrencyException` triggers per-item entry detachment in EF Core ChangeTracker to prevent context corruption across batch iterations.

## Consequences

- The Notification Delivery subsystem is fully operationally manageable with enterprise auditability, safety, and background retention maintenance.
- Zero PII leakage and zero client-supplied admin identity prevent unauthorized privilege escalation and privacy violations.
- System throughput and database health are protected against unbounded table growth through bounded background retention cleanup.
