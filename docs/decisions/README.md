# Architecture Decision Records

Significant architecture decisions for the LastHour backend, recorded
lightweight (Status / Context / Decision / Consequences). Add new ADRs when a
decision affects more than one module or the foundation itself; keep each ADR
short and linked from the code it describes where practical.

| ADR | Decision |
|-----|----------|
| [ADR-001](ADR-001-modular-monolith.md) | Modular monolith over microservices |
| [ADR-002](ADR-002-result-objects.md) | Result objects instead of exceptions for expected failures |
| [ADR-003](ADR-003-outbox.md) | Transactional outbox for domain events |
| [ADR-004](ADR-004-cqrs-mediatr.md) | CQRS via MediatR with pipeline behaviors |
| [ADR-005](ADR-005-strongly-typed-ids.md) | Strongly typed identifiers |
| [ADR-006](ADR-006-outbox-single-instance.md) | Single-instance outbox with at-least-once delivery |
| [ADR-007](ADR-007-warnings-as-errors.md) | Warnings-as-errors and analyzers as a quality gate |
| [ADR-008](ADR-008-architecture-tests.md) | Architecture boundaries enforced by tests |
| [ADR-009](ADR-009-postgresql-ef-core.md) | PostgreSQL with EF Core as the persistence strategy |
| [ADR-010](ADR-010-transactions-unit-of-work.md) | Single transaction system — UnitOfWork with pipeline wrapping |
| [ADR-011](ADR-011-api-contracts-errors.md) | Minimal endpoints, URL versioning, Result → ProblemDetails |
| [ADR-012](ADR-012-security-by-default.md) | Security by default with configuration-driven hardening |
| [ADR-013](ADR-013-observability.md) | Observability — Serilog, OpenTelemetry, single correlation id |
| [ADR-014](ADR-014-password-hashing-argon2id.md) | Argon2id password hashing with self-describing PHC hashes |
| [ADR-015](ADR-015-jwt-access-tokens.md) | JWT access tokens for authentication |
| [ADR-016](ADR-016-refresh-token-rotation.md) | Refresh-token rotation with family-based reuse detection |
| [ADR-017](ADR-017-account-lockout-password-change.md) | Failed-attempt lockout and password-change session revocation |
| [ADR-018](ADR-018-roles-permissions-model.md) | Roles & permissions model with catalog-driven permissions and per-request resolution |
| [ADR-019](ADR-019-authorization-enforcement.md) | Permission-based authorization enforcement with live resolution and consistent 403 responses |
| [ADR-020](ADR-020-consumer-idempotency-standard.md) | Standardized domain event consumer pattern & dual-layer idempotency |
| [ADR-021](ADR-021-production-event-observability.md) | Production event observability, failure classification & operational diagnostics |
| [ADR-022](ADR-022-identity-domain-events.md) | Identity domain events foundation & security boundaries |
| [ADR-023](ADR-023-identity-password-changed-notification.md) | Identity password-changed notification consumer & security idempotency |
| [ADR-024](ADR-024-identity-lockout-notification.md) | Identity account-lockout notification consumer & security idempotency |
| [ADR-025](ADR-025-identity-user-registered-notification.md) | Identity user-registered welcome notification consumer & privacy boundaries |
| [ADR-026](ADR-026-event-processing-hardening.md) | Event processing reliability model, poison event protection & architectural hardening |
| [ADR-027](ADR-027-notification-lifecycle-and-read-state.md) | Notification lifecycle, read-state invariants, and query operations |
| [ADR-028](ADR-028-notification-delivery-architecture.md) | Multi-channel external notification delivery architecture |
| [ADR-029](ADR-029-notification-preferences-and-templates.md) | Notification preferences, templates, and policy resolution subsystem |
| [ADR-030](ADR-030-notification-operations-and-retention.md) | Notification operations, administrative requeue, and data retention policy |
