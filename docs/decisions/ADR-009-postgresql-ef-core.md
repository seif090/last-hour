# ADR-009: PostgreSQL with EF Core as the persistence strategy

**Status:** Accepted

**Context:** The modular monolith (ADR-001) needs one database for all modules
and a persistence strategy that survives scale-out. The provider must support
strongly typed IDs (ADR-005), optimistic concurrency later, JSON storage, and
deterministic migrations. The repository had no database code before the
foundation sprints, so the provider and mapping conventions were chosen
explicitly rather than inherited.

**Decision:** PostgreSQL via `Npgsql.EntityFrameworkCore.PostgreSQL` (EF Core
9) is the single relational provider. `LastHourDbContext` lives in
`Infrastructure/Persistence`; module entity mappings arrive as
`IEntityTypeConfiguration<T>` classes discovered by
`ApplyConfigurationsFromAssembly` per module assembly, so adding a module's
entities never rewrites the DbContext. Cross-cutting mapping is centralized in
`DbContextModelConventions` (snake_case names, `timestamp with time zone`,
`jsonb` for outbox content, decimal precision, strongly typed ID value
converters). Connection configuration flows through `PostgresOptions`
(validated at startup) and `LastHourDbContextOptionsSetup` (pooling, retry
policy, command timeout). Migrations are applied by `DatabaseInitializer`
**in the Development environment only**; production applies migrations as a
controlled release step, never automatically on startup.

**Consequences:** One provider means migrations target PostgreSQL DDL only —
acceptable because PostgreSQL is the deployment target. Npgsql-specific
features (e.g. `xmin` concurrency, `jsonb`) remain available when a future
aggregate needs them. Developers need a local PostgreSQL (or the compose
stack) to run migrations; unit and integration tests use in-memory/SQLite
providers and never require a database.
