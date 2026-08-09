# Architecture

The LastHour backend is a **modular monolith**: one deployment unit, one
database, and code organized into modules behind clear dependency boundaries.
The boundaries are enforced by tests, not by convention.

## Layered model

```
┌────────────────────────────────────────────────────────────┐
│ LastHour.Api (composition root)                            │
│  endpoints · middleware · security · observability · OpenAPI│
├────────────────────────────────────────────────────────────┤
│ Modules (Identity, Catalog, Ordering, ...)                  │
│  business behavior; empty until Epic 01                     │
├────────────────────────────────────────────────────────────┤
│ Infrastructure · Contracts                                  │
│  persistence · outbox · health · time · pipelines           │
├────────────────────────────────────────────────────────────┤
│ Application                                                 │
│  CQRS markers, repository/UoW/current-user abstractions     │
├────────────────────────────────────────────────────────────┤
│ SharedKernel · Architecture                                 │
│  Result, domain primitives · dependency rules               │
└────────────────────────────────────────────────────────────┘
```

Dependency direction (enforced in `tests/.../Architecture.Tests`):

- `SharedKernel` — zero project references.
- `Application` — references `Architecture` + `SharedKernel`.
- `Infrastructure`, `Contracts` — reference `Application` + `Architecture`
  + `SharedKernel`.
- Modules — reference `Application` + `Architecture` + `SharedKernel`
  + `Contracts`.
- `Api` — references everything except `SharedKernel`'s lower layers are
  reached transitively; it is the only project allowed to reference
  Infrastructure and modules.

No layer below the API may reference `Microsoft.EntityFrameworkCore`, `Npgsql`,
or `StackExchange.Redis`; the API itself contains no direct persistence code
(verified by grep and architecture tests).

## Key patterns

### Result, not exceptions

Expected failures are values, not control flow. Every command/query handler
returns `Result` or `Result<T>`; the API converts failures to RFC 7807 problem
details through `ResultEndpointFilter`. Exceptions are reserved for
programming errors and are caught by `UnhandledExceptionBehavior` / the
problem-details exception handler.

### CQRS via MediatR

`ICommand`/`IQuery` derive from MediatR's `IRequest` so handlers are
discovered by assembly scan and run through a pipeline of behaviors:
validation, logging, performance, transaction, and unhandled-exception.
Contract tests lock these inheritance/constraint relationships.

### Transactional outbox

Domain events raised on aggregates are captured by `DomainEventsInterceptor`
and persisted as outbox messages in the same transaction as the aggregate
change. `OutboxProcessor` (hosted service) dispatches them in-process through
MediatR with per-message retry and abandonment. Delivery is at-least-once and
assumes a single processor instance.

### Strongly typed identifiers

IDs are value types (`GuidId`, `IntId`) mapped to `uuid`/`integer` columns by
value converters, avoiding primitive-type confusion across modules.

### Repository/unit-of-work

`IRepository<TAggregateRoot>` (write) and `IReadRepository<TEntity, TId>`
(read) abstract persistence; `IUnitOfWork` commits atomically and exposes
explicit transactions. Modules implement against these abstractions, never
against EF directly.

### Options validation

Every configuration section is bound to a typed options class validated with
`ValidateOnStart`, so bad configuration fails fast at startup.

## Runtime concerns

- Correlation ids flow through middleware → logs → problem details → audit.
- Request logging, performance, audit, and exception handling are
  middleware/behavior-based and cross-cutting.
- Liveness vs readiness health checks are separated by tag.
- Rate limiting and request limits are configured per policy (see
  `docs/SECURITY.md`).

## Decisions

Architecture decisions are recorded as ADRs under `docs/decisions/`. When
making a significant structural choice, add an ADR.

## Audit trail

`docs/ARCHITECTURE_AUDIT.md` records the findings and rationale of the
hardening audit that produced this architecture documentation and the
test suites.
