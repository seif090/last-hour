# ADR-020: Standardized domain event consumer pattern & dual-layer idempotency

**Status:** Accepted

**Context:** Domain events from the transactional outbox are delivered with at-least-once semantics. Consumers must be safe against duplicate deliveries, lease expirations, stale worker races, and process crashes between business persistence and outbox/inbox completion. Without a strict architectural standard, modules risk introducing custom, incompatible, or fragile duplicate-detection mechanisms.

**Decision:**
1. Every domain event consumer must implement `INotificationHandler<NotificationMessage<TDomainEvent>>` and reside in `<Module>.Application.Consumers`.
2. Every consumer must define a unique, deterministic `public const string ConsumerName = "<ModuleName>.<OperationName>"`.
3. Consumers must execute all business side-effects through `IConsumerIdempotencyService.ExecuteAsync(domainEvent, ConsumerName, delegate, ct)`.
4. The system enforces a two-layer defense:
   - **Layer 1 (Consumer Inbox)**: `UNIQUE(EventId, Consumer)` with time-limited lease (`LockExpiresAtUtc`) and optimistic concurrency tokens.
   - **Layer 2 (Business Effect)**: Domain-level idempotency (e.g. `UNIQUE(EventId, Type)` on `notifications`) with pre-checks and database unique constraint exception handling.
5. All rules are statically and reflectively enforced via `ConsumerArchitectureTests`.

**Consequences:**
- Guaranteed effectively-once business effect persistence across the entire modular monolith.
- Zero ad-hoc locking, zero static dictionaries, zero infrastructure leaks into Application modules.
- Consumers automatically gain OpenTelemetry metrics (`processed`, `duplicate`, `failed`) and cancellation safety.
- Changing a `ConsumerName` is treated as a breaking behavioral change because it alters the persistent idempotency key.
