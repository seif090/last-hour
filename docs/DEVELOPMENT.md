# Development

How to work on the LastHour backend.

## Prerequisites

- .NET SDK matching `global.json` (9.0.311, `rollForward: latestFeature`).
- (Optional) Docker with the compose stack for PostgreSQL/Redis/Seq — see
  `docs/OPERATIONS.md`. Unit and integration tests run without a database.

## Daily loop

```bash
dotnet restore LastHour.Backend.sln
dotnet build LastHour.Backend.sln -c Release
dotnet test LastHour.Backend.sln -c Release
dotnet format LastHour.Backend.sln --verify-no-changes --no-restore
```

Warnings are errors. The build fails on any analyzer/code-style violation, and
`dotnet format --verify-no-changes` must pass. The same commands run in CI on
every push/PR.

Run a single project when iterating:

```bash
dotnet test tests/BuildingBlocks/LastHour.BuildingBlocks.SharedKernel.Tests -c Release
```

## Layout

- `src/Api/LastHour.Api` — composition root: `Program`, endpoints, middleware,
  security, observability, OpenAPI.
- `src/BuildingBlocks/` — `Architecture` (rules), `SharedKernel` (domain
  primitives + results), `Application` (CQRS/contract abstractions),
  `Infrastructure` (persistence, outbox, health, time, pipelines), `Contracts`
  (empty until business contracts exist).
- `src/Modules/*` — module projects (Identity, Catalog, Ordering, ...).
  All are empty until their business behavior is implemented.
- `tests/` mirrors `src/` one-to-one.

## Adding behavior

1. Implement in the layer that owns the concern. Dependency direction is
   enforced by tests: `SharedKernel` has no references; `Application` may
   depend on `Architecture` + `SharedKernel`; `Infrastructure` and `Contracts`
   on those three; modules on `Application` + `Architecture` + `SharedKernel`
   + `Contracts`; only the API references everything.
2. Follow the existing patterns: `Result`/`Result<T>` for expected failures,
   MediatR `ICommand`/`IQuery` + handlers with a `Result` response, strongly
   typed IDs, domain events via the outbox.
3. Add tests in the mirrored test project. See `docs/TESTING.md` for
   conventions — and do **not** add `using Xunit;` (global using already
   present).
4. If you add or move a project, update the architecture tests
   (`tests/.../Architecture/`) — both the NetArchTest layer rules and the
   `.csproj` reference-graph list.

## Adding a module

- Create `src/Modules/{Name}/LastHour.Modules.{Name}` referencing
  `Application` + `Architecture` + `SharedKernel` + `Contracts`.
- Add the matching empty test project under `tests/Modules/{Name}/`.
- Register the module assembly with the API composition root
  (`AddLastHourApi`) and add the project to `LastHour.Backend.sln`.
- Contribute entity configurations through `ApplyConfigurationsFromAssembly`
  (the DbContext already applies module assemblies automatically); the module
  must not depend on Infrastructure directly.

## Configuration while developing

`appsettings.Development.json` is untracked and machine-local (e.g. pointing
PostgreSQL at the compose stack). Never commit it. Secret values come from
`LASTHOUR_SECRET_*` environment variables — see `docs/CONFIGURATION.md`.
