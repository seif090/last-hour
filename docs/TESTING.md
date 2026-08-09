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

Coverage baselines (instrumented line coverage, union across test projects):

| Assembly | Baseline |
|----------|----------|
| `LastHour.BuildingBlocks.SharedKernel` | ~91% |
| `LastHour.BuildingBlocks.Api` | ~92% |
| `LastHour.BuildingBlocks.Infrastructure` | ~94% |

Uncovered paths are tracked as deliberate exceptions rather than enforced
thresholds; see the sprint report for the known uncovered items
(`DesignTimeDbContextFactory`, EF-tooling glue, and infrastructure that requires
an external database).

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
