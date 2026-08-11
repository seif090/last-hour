# Testing

How the LastHour backend is tested, how to run the suite, and the conventions new
tests must follow.

## Running the suite

From the repository root:

```bash
dotnet restore LastHour.Backend.sln
dotnet build LastHour.Backend.sln -c Release
dotnet test LastHour.Backend.sln -c Release
dotnet format LastHour.Backend.sln --verify-no-changes --no-restore
```

The build treats warnings as errors (`TreatWarningsAsErrors=true`), runs the
latest-recommended analyzers, and enforces code style
(`EnforceCodeStyleInBuild=true`). `dotnet format --verify-no-changes` must pass;
the CI pipeline fails otherwise.

## Coverage

Collect coverage per test run with the built-in collector:

```bash
dotnet test LastHour.Backend.sln -c Release --collect:"XPlat Code Coverage"
```

Coverage baselines (instrumented line coverage, union across test projects,
measured with the XPlat collector):

| Assembly | Baseline |
|----------|----------|
| `LastHour.BuildingBlocks.SharedKernel` | ~90% |
| `LastHour.Api` | ~90% |
| `LastHour.BuildingBlocks.Infrastructure` | ~84% |

These are informational baselines, not enforced thresholds. The uncovered
Infrastructure paths are deliberate exceptions: the EF migrations
(`src/.../Infrastructure/Migrations/`), the design-time factory, and other
infrastructure that requires an external database (`DesignTimeDbContextFactory`,
EF-tooling glue) are not exercised by unit tests.

## Project layout

One test project per production project, mirrored under `tests/`:

- `tests/BuildingBlocks/LastHour.BuildingBlocks.SharedKernel.Tests`
- `tests/BuildingBlocks/LastHour.BuildingBlocks.Application.Tests`
- `tests/BuildingBlocks/LastHour.BuildingBlocks.Infrastructure.Tests`
- `tests/BuildingBlocks/LastHour.BuildingBlocks.Architecture.Tests`
- `tests/Api/LastHour.Api.Tests`
- `tests/Modules/*/LastHour.Modules.*.Tests` (empty until modules gain behavior)

Empty module test projects exist so every module gets coverage infrastructure
when its first behavior lands. Do not delete them; do not invent tests for
interfaces-only or empty projects (`Contracts` has no production code and is
covered by design).

## Parallelization

xUnit runs test collections in parallel by default. The API tests share one
`LastHourApiFactory` per collection, so the endpoint tests live in the
`ApiEndpoints` collection and execute serially; everything else (unit,
persistence, architecture) runs in parallel across collections. In-memory
SQLite/EF contexts are created per test, so parallel runs never share database
state. Do not introduce global mutable test state; if a static sink is
unavoidable (for example `TestSink` for outbox handler assertions), make it
concurrency-safe and reset it at the start of each test.

## Database reset

Persistence tests never touch a real database. The in-memory provider gives
fresh per-test state automatically; relational-transaction tests use a
SQLite in-memory connection created per test via
`TestPersistence.CreateSqliteContext` (opened before the test, disposed after),
so each test starts from an empty schema. No reset mechanism (drop/recreate,
Respawn, container-per-suite) is needed because no test shares a persistent
store — keep it that way.

## Conventions

- xUnit is the framework. `tests/Directory.Build.props` adds FluentAssertions
  to every test project (use it or the built-in `Assert` — stay consistent
  within a file).
- Every test csproj declares `<Using Include="Xunit" />`, so `using Xunit;`
  must **not** appear in test files (it is flagged as redundant by IDE0005,
  which fails the build).
- Namespace mirrors the folder under the test project
  (`...Tests.Domain`, `...Tests.Persistence`, ...).
- Behavior is asserted through public contracts; never through
  `InternalsVisibleTo`. Internal classes that are not reachable from a public
  entry point are exercised indirectly (for example
  `ResultFailureFactory` via the pipeline behaviors) or documented as
  covered-by-design.
- EF tests use the InMemory provider. Tests that require a relational
  transaction use a SQLite in-memory database via
  `TestPersistence.CreateSqliteContext` (for example the
  `EfUnitOfWork` begin/commit/rollback happy paths).
- API tests boot the real host with `LastHourApiFactory`
  (`WebApplicationFactory<Program>`) and an in-memory configuration that
  disables `DatabaseInitializer`, the outbox processor, and the Redis health
  check, so `/health` deterministically reports `503` until PostgreSQL is
  configured to an unreachable port.

## Architecture tests

`tests/BuildingBlocks/LastHour.BuildingBlocks.Architecture.Tests` enforces the
dependency rules with NetArchTest (layer boundaries) and by parsing the
`.csproj` graph (project references). The API test project references the
composition root so these rules load every `src` assembly. When adding a new
project, update both the layer rules and the expected project-reference list.
