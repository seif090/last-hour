# ADR-003: Transactional outbox for domain events

**Status:** Accepted

**Context:** Aggregates raise domain events that must eventually cause side
effects (notifications, downstream projections). Publishing events immediately
inside the request risks losing them when the transaction rolls back, and
publishing after commit risks inconsistency between the database write and the
published event.

**Decision:** `DomainEventsInterceptor` captures events raised on tracked
aggregates and persists them as `OutboxMessage` rows in the same database
transaction as the aggregate changes. A hosted `OutboxProcessor` drains the
table, rehydrates each payload, and publishes through MediatR in-process.
Failed messages are retried until `MaxRetryCount`, then abandoned with the last
error preserved. Delivery is at-least-once.

**Consequences:** Strong durability: an event exists if and only if the change
that raised it exists. Handlers must be idempotent because at-least-once can
redeliver. The processor assumes a single instance; scale-out would require
row leasing (see ADR-006).
