# ADR-010: Single transaction system — UnitOfWork with pipeline wrapping

**Status:** Accepted

**Context:** Commands mutate aggregates and publish domain events through the
outbox (ADR-003). If different transaction mechanisms competed (ambient
transactions, manual connection transactions, per-handler commits), failures
could leave the aggregate write and the outbox row inconsistent, and the code
base would grow parallel rollback logic.

**Decision:** One transaction system. `IUnitOfWork` → `EfUnitOfWork` wraps
`SaveChangesAsync` and exposes explicit `BeginTransactionAsync` /
`CommitTransactionAsync` / `RollbackTransactionAsync` with correct disposal
and cancellation. The `TransactionBehavior` pipeline wraps every command
handler in a transaction (commit on success, rollback on failure), so the
aggregate changes and their outbox messages commit atomically. Read-only
queries are never wrapped. Transactions are scoped per request and never
nested.

**Consequences:** Handlers do not manage transactions themselves; they rely on
the pipeline. The outbox integrity guarantee (ADR-003) holds because the outbox
row is written in the same `SaveChangesAsync` as the aggregate. The design is
single-instance today (ADR-006); if scale-out ever requires distributed
commit, the transaction boundary remains unchanged while the outbox processor
gains row leasing.
