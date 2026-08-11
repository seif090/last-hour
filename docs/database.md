# Database

How the LastHour backend persists data: provider, EF Core strategy, schema
management, transactions, and testing. This document describes behavior that
exists today; nothing here invents future entities or business tables.

## Provider & packages

- **Provider:** PostgreSQL, via `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4
  (matches .NET 9 / EF Core 9.0.18).
- **EF Core:** 9.0.18 (`Microsoft.EntityFrameworkCore`,
  `.Design` for tooling, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`).
- **Test-only providers:** `Microsoft.EntityFrameworkCore.InMemory` and
  `Microsoft.EntityFrameworkCore.Sqlite` (used by unit tests only; never at runtime).
- **No other database providers** are referenced. Do not add one without an
  architecture decision.

## DbContext

`LastHourDbContext` lives in `src/BuildingBlocks/LastHour.BuildingBlocks.Infrastructure/Persistence/LastHourDbContext.cs`.

- Registered **scoped** (`AddDbContext<LastHourDbContext>` with scoped options
  configuration) because it consumes scoped interceptors and the audit/soft-delete
  ambient context.
- Provider wiring happens in `LastHourDbContextOptionsSetup`
  (`IDbContextOptionsConfiguration<LastHourDbContext>`): Npgsql data source,
  transient-failure retry policy, command timeout, diagnostic flags, and the
  three interceptors.
- The context owns the **outbox** and **seeding history** tables and applies
  cross-cutting conventions to every mapped entity:
  - snake_case tables and columns, `timestamptz` for `DateTime`, `decimal(19,4)`;
  - automatic value converters for strongly typed IDs;
  - global query filters for `ISoftDelete` entities.
- Modules contribute entity configurations by registering their assemblies with
  `AddLastHourInfrastructure(services, configuration, moduleAssemblies)`; the
  context applies them via `ApplyConfigurationsFromAssembly`.

## Schema management (migrations)

- **Strategy:** EF Core migrations, generated in the Infrastructure project
  (`src/BuildingBlocks/.../Infrastructure/Migrations/`), applied by
  `DatabaseInitializer` on startup **in the Development environment only**.
- Tooling: `dotnet-ef` 9.0.18 is pinned as a local tool
  (`.config/dotnet-tools.json`), matching the EF Core version. Generate module
  migrations with the **API as startup project** so module entity configurations
  participate in the model (the design-time factory discovers `LastHour.Modules.*`
  assemblies from the startup host's output):
  `dotnet dotnet-ef migrations add <Name> --project src/BuildingBlocks/LastHour.BuildingBlocks.Infrastructure/LastHour.BuildingBlocks.Infrastructure.csproj --startup-project src/Api/LastHour.Api/LastHour.Api.csproj`
- Design-time factory: `DesignTimeDbContextFactory` (design-time only; uses a
  development placeholder connection string that is never used at runtime, and
  loads module assemblies for the model).
- **Baseline migration:** `20260811023325_InitialInfrastructure` creates exactly
  the two infrastructure tables that exist today: `outbox_messages` and
  `seeding_history`.
- **Identity migration:** `20260811152933_AddIdentityUsers` creates the `users`
  table (uuid primary key, unique normalized-email index). Business modules add
  their own migrations as they gain entities; see `docs/identity.md` for the
  Identity schema.
- **Production:** migrations are applied by the release process, not
  automatically on production startup (`DatabaseInitializer` is a no-op outside
  Development). The startup migration path is for local development only.
- Migrations are deterministic; the model snapshot (`LastHourDbContextModelSnapshot.cs`)
  is committed and must stay in sync (EF tooling regenerates it).

## Transactions

- `IUnitOfWork` / `EfUnitOfWork` wrap the DbContext:
  `SaveChangesAsync` commits tracked changes atomically; explicit transactions
  via `BeginTransactionAsync` / `CommitTransactionAsync` / `RollbackTransactionAsync`.
- The CQRS pipeline's `TransactionBehavior` starts a unit-of-work transaction
  around command handlers and commits on success, rolls back on failure.
- Outbox messages are written in the same `SaveChangesAsync` as the aggregate
  changes (same transaction), so a domain event is never persisted without its
  aggregate change.

## Repositories

- `IRepository<TAggregateRoot>` (write) → `EfRepository<TAggregateRoot>`:
  add/update/remove only; tracking in memory, no commits.
- `IReadRepository<TEntity, TId>` (read) → `EfReadRepository<TEntity, TId>`:
  no-tracking queries, respects global query filters.
- Repositories stay minimal and focused; no generic mega-repository was
  introduced. Modules implement against the abstractions, never EF directly.

## Strongly typed IDs

- `GuidId` / `IntId` (SharedKernel) map to `uuid` / `integer` columns via
  value converters (`StronglyTypedIdValueConverter`) discovered by the model
  conventions. The mapping is covered by
  `StronglyTypedIdValueConverterTests` and `Model_StronglyTypedIdProperty_HasAutomaticValueConverter`.

## Audit, soft delete, concurrency

- **Audit:** `IAuditableEntity` (`CreatedAt`, `CreatedBy`, `UpdatedAt`,
  `UpdatedBy`) is stamped by `AuditInterceptor` from `IClock` and `ICurrentUser`.
  Until Identity exists, `ICurrentUser` returns `null` — the actor is not faked.
- **Soft delete:** `ISoftDelete` (`IsDeleted`, `DeletedAt`, `DeletedBy`).
  `SoftDeleteInterceptor` converts `Remove` into a logical delete; the model
  adds a global query filter so deleted rows are hidden from all queries.
- **Concurrency:** no concurrency token is used today — no mutable business
  entity exists yet. When the first aggregate with update-vs-update semantics
  lands, add a version column (or PostgreSQL `xmin`) deliberately; do not bolt
  one on to infrastructure tables that do not need it.

## Connection configuration

- `PostgresOptions` bound from the `Postgres` section, validated at startup
  (`PostgresOptionsValidator`); falls back to `ConnectionStrings:Postgres`
  (standard `ConnectionStrings__Postgres` environment override).
- Secrets are injected via `LASTHOUR_SECRET_*` environment variables
  (`Secrets` provider), never committed. `docker/.env.example` holds
  placeholders only.
- Pooling is configured on a shared `NpgsqlDataSource` (pool size, idle
  lifetime, pruning interval, connection timeout) from the same options.

## Seeding

- `IDatabaseSeeder` + `SeederExecutor` + `SeedHistory` (unique index on
  `seeder_type`): seeders run at most once, recorded after success.
- `DatabaseInitializer` runs migrations + seeders **only** in Development when
  `DatabaseInitializer:Enabled` is true and the provider is relational.
- No business seed data exists or is created in this sprint; Identity seed data
  belongs to the Identity sprint.

## Health checks

- `postgres` readiness check via `AddDbContextCheck<LastHourDbContext>` (tags
  `database`, `ready`): fails when the context cannot connect. Exposes status
  only — never credentials, connection strings, or SQL.

## Local setup (Docker)

`docker/docker-compose.yml` provides PostgreSQL 16:

- named volume for persistence (`postgres-data`);
- credentials from `docker/.env` (`POSTGRES_DB/USER/PASSWORD`), placeholders in
  `.env.example`, never committed;
- `pg_isready` health check;
- internal service name `postgres` on the `lasthour` bridge network; host port
  bound to `127.0.0.1` only.

Start the stack, then run the API locally (Development) so
`DatabaseInitializer` applies migrations and seeds:

```bash
docker compose --env-file docker/.env --project-directory . -f docker/docker-compose.yml up -d postgres
dotnet run --project src/Api/LastHour.Api
```

## Testing

- Infrastructure unit tests use the **InMemory** provider for most behavior and
  **SQLite in-memory** for relational specifics (explicit transactions).
  See `TestPersistence.CreateContext` / `CreateSqliteContext`.
- The API integration host (`LastHourApiFactory`) disables `DatabaseInitializer`,
  the outbox processor, and Redis, and points PostgreSQL at an unreachable port
  so `/health` deterministically reports 503 — no developer-installed database
  is required to run the suite.
- Migration SQL is validated via `dotnet dotnet-ef migrations script`
  (generates the full DDL without a live database).

## Security considerations

- Credentials are never hardcoded; connection strings come from configuration
  overridden by `LASTHOUR_SECRET_*` environment variables.
- `EnableSensitiveDataLogging` / `EnableDetailedErrors` default off and are
  validated so they cannot be enabled accidentally in production configuration
  without intent.
- Database errors surface as generic problem details (no connection strings or
  SQL), per `ProblemDetailsExceptionHandler`.
- Production should connect over TLS with least-privilege credentials; that is
  deployment configuration, not enforced by this codebase.
