# Infrastructure layer

This document describes the `LastHour.BuildingBlocks.Infrastructure` project and
how it is wired into the API composition root.

## Scope

The Infrastructure layer provides cross-cutting, persistence-backed services on
top of `SharedKernel` and the application abstractions. Modules depend on
`Contracts` and `Application`; Infrastructure depends on both. The API host
registers it once via `AddLastHourApi` → `AddLastHourInfrastructure`.

## Persistence

### DbContext

`LastHourDbContext` (`Persistence/LastHourDbContext.cs`) owns all module tables.
Because entities live in module assemblies, the context receives the assembly
list that the composition root passes to `AddLastHourInfrastructure` and applies
the matching `IEntityTypeConfiguration` classes via `ApplyConfigurationsFromAssembly`.

Model conventions (`Persistence/Conventions/`): tables use snake_case, property
names map to `snake_case` columns, `DateTime` values are stored `timestamptz`
(in UTC), and `decimal` properties are constrained to 19,4.

Interceptors (`Persistence/Interceptors/`):

- `AuditInterceptor` — fills `CreatedOnUtc`/`ModifiedOnUtc` audit fields from the
  current time.
- `SoftDeleteInterceptor` — converts `DeleteAsync` into a soft-delete marker.

### Options & EF Core 9 wiring

EF Core 9's action-less `AddDbContext<TContext>()` does not consult
`IConfigureOptions<DbContextOptionsBuilder<TContext>>`. Options are built by
iterating `IDbContextOptionsConfiguration<TContext>`
(`Microsoft.EntityFrameworkCore.Infrastructure`). `LastHourDbContextOptionsSetup`
implements that interface and delegates to a private `ConfigureCore` method.

The registration is **scoped**, not singleton, because the configuration
consumes scoped interceptors. Hosting's `ValidateScopes`/`ValidateOnBuild`
rejects a singleton options configuration that pulls in scoped dependencies.

### Unit of work & repositories

- `IUnitOfWork` (`Persistence/UnitOfWork/`) wraps `SaveChangesAsync` for
  transaction-scoped writes.
- `Persistence/Repositories/` provides base repository helpers for the modules.
- `IStronglyTypedId`/value-conversion helpers (`StronglyTypedIds/`) convert
  strongly typed IDs to `uuid` columns.

### Outbox

`Persistence/Outbox/` implements a transactional outbox so domain events are
persisted in the same transaction as the aggregate changes and dispatched
asynchronously by the `OutboxProcessor` hosted service.

- Configured via the `Outbox` options section.
- `OutboxProcessor.StartAsync` checks `Outbox:Enabled` first; the dispatcher
  publishes through MediatR.

### Migrations

`Persistence/Migrations/` holds the EF Core migrations for the infrastructure
schema: the baseline migration `20260811023325_InitialInfrastructure` creates
the `outbox_messages` and `seeding_history` tables. `dotnet-ef` 9.0.18 is
pinned as a local tool (`.config/dotnet-tools.json`). Migrations are applied by
`DatabaseInitializer` in Development only; production applies them through the
release process. See `docs/database.md`.

### Seeding

`Persistence/Seeding/` runs idempotently at startup:

- `IDatabaseSeeder` — module seeders implement this interface.
- `SeederExecutor` — scoped executor. Runs a seeder only if its type name is not
  already recorded in the `seeding_history` table, then records it after success.
- `SeedHistory` — entity with a unique index on `seeder_type`.
- `DatabaseInitializer` — hosted service that gates on
  `DatabaseInitializer:Enabled`, the Development environment, and a relational
  provider before applying `MigrateAsync` and delegating to the `SeederExecutor`.

The `DatabaseInitializer:Enabled` switch exists because `WebApplicationFactory`
runs in the Development environment, where migrations would otherwise run against
an unreachable database during tests.

### Health checks

`HealthChecks/` registers checks via the `HealthChecks` options section. Each check carries
tags: liveness checks are tagged `live`, readiness checks `ready`. The endpoints
(`/health/live`, `/health/ready`) select checks by tag, so the API process can report itself
alive even while its dependencies are down.

| Check | Tags | Failure condition |
|-------|------|-------------------|
| `self` | `self`, `live` | never (process answered the probe) |
| `postgres` | `database`, `ready` | `LastHourDbContext` cannot connect |
| `disk` | `disk`, `ready` | Host drive not ready or free space below `MinimumFreeMegabytes` |
| `memory` | `memory`, `ready` | Working set above `MaximumUsedBytes` |
| `redis` | `redis`, `ready` | Redis `PING` fails within `TimeoutSeconds` (registered only when a connection string is configured) |

The Redis multiplexer is created with `AbortOnConnectFail=false` so a down Redis
instance degrades the health result instead of throwing during the check
registration factory (which would surface as HTTP 500 rather than 503).

Endpoints: `GET /health` (all checks), `/health/live` (liveness),
`/health/ready` (readiness), `GET /` (readiness probe, JSON).

## Dependency injection

`DependencyInjection/InfrastructureServiceCollectionExtensions.cs` exposes:

- `AddLastHourInfrastructure(services, configuration, moduleAssemblies)` — the
  aggregate entry point (persistence, outbox, seeding, health checks, time,
  validation, logging, transactions, performance).
- `RegisterOptions` — binds and validates the `Postgres`, `Outbox`,
  `DatabaseInitializer`, `HealthChecks` options sections (`ValidateOnStart`);
  `PerformanceBehaviorOptions` (`Cqrs:Performance`) is bound and validated the
  same way from `AddPerformanceBehaviorOptions`.
- `RegisterServices` — registers the health checks; conditionally registers the
  Redis multiplexer/probe/check.
- `AddCqrs` — MediatR with the application pipelines (validation, performance,
  logging, exception behavior, transaction behavior) registered as behaviors.

## Test strategy

- Infrastructure unit tests (`tests/BuildingBlocks/.../Infrastructure.Tests`)
  cover options validation, seeders, outbox, health checks, and the DI
  registrations. The DI `BuildProvider` helper uses
  `ValidateScopes = true` to catch lifetime mistakes like the scoped-into-singleton
  bug above.
- API integration tests (`tests/Api/...`) boot the real host via
  `LastHourApiFactory` (`WebApplicationFactory<Program>`) with an in-memory
  configuration that disables `DatabaseInitializer`, the outbox processor, and
  the Redis health check, and points PostgreSQL at an unreachable port so
  `/health` deterministically returns `503 Service Unavailable`.
