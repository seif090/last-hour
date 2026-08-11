# LastHour Backend — Architecture Audit & Enterprise Foundation Hardening

> Sprint 00.1, hardening pass. Validates the Clean Architecture skeleton and
> raises the repository to enterprise standards. No business logic was added;
> no working code was rewritten; no breaking changes were introduced.

## 1. Scope and method

The audit covered every project, configuration file, and the CI workflow:

- Solution manifest (`LastHour.Backend.sln`, 35 projects).
- All 17 `src` csprojs + 18 `tests` csprojs.
- Root artifacts: `Directory.Build.props`, `Directory.Packages.props`,
  `NuGet.config`, `.editorconfig`, `global.json`, `.gitignore`, `stylecop.json`.
- `src/Api/LastHour.Api` host (`Program.cs`, appsettings, launch settings).
- `.github/workflows/build.yml`.

Verification toolchain: `dotnet restore`, `dotnet build`, `dotnet test`,
`dotnet format --verify-no-changes`, and `docker compose config`. Local SDK:
9.0.316 (per `global.json`: 9.0.311, `rollForward: latestFeature`).

## 2. Baseline: already compliant

- **Dependency rule / no cycles.** Building blocks depend downward only:
  Architecture (nothing) → SharedKernel → Application → Infrastructure and
  Contracts; each Module references Application + Architecture + SharedKernel
  + Contracts; the API references all blocks + all modules (composition root).
- **Central Package Management** was already enabled with zero inline
  `Version=` attributes anywhere.
- **`Directory.Build.props`** already set `net9.0`, `Nullable=enable`,
  `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, `Deterministic`,
  `EnableNETAnalyzers`, `AnalysisLevel=latest-recommended`,
  `EnforceCodeStyleInBuild`.
- **Naming** follows `LastHour.{Layer}[.{Module}]` everywhere.

## 3. Issues found and fixes

| # | Issue | Fix |
|---|-------|-----|
| 1 | `.sln` solution folders shared one GUID (Merchant/Catalog/Inventory/… all nested under Identity); `tests/Api` folder misplaced | Full rewrite with unique folder GUIDs; project GUIDs preserved; `dotnet sln list` confirms 35 projects |
| 2 | 16 `Class1.cs` template placeholders in `src` | Deleted |
| 3 | `NuGet.config` had no `packageSourceMapping` (supply-chain risk) | Added single-source mapping to `nuget.org` |
| 4 | `.editorconfig` minimal (40 lines): no StyleCop, no naming conventions, no enforced style rules | Rewritten: StyleCop wiring, naming conventions, 6 IDE rules promoted to `warning` (enforced at build) |
| 5 | No static-analysis rule set beyond the SDK analyzers | `StyleCop.Analyzers` 1.1.118 added centrally (src only) with `stylecop.json` |
| 6 | `GenerateDocumentationFile=false` prevented the IDE0005 gate | Set to `true` (CS1591 stays suppressed) |
| 7 | New `src/` and `tests/` `Directory.Build.props` shadowed the root file (MSBuild imports only the *nearest* one) | Both explicitly `Import` the root props |
| 8 | CPM table covered only OpenAPI + test packages | Pinned and extended: Serilog 10.0.x, Seq sink 9.1.0, StyleCop 1.1.118; test stack bumped to latest stable and verified |
| 9 | `Program.cs` had no logging, no health checks | Serilog bootstrap + request logging; `/health`, `/health/live`, `/health/ready` |
| 10 | appsettings had no Serilog or connection strings | Serilog section (Console + rolling File + optional Seq via env override); Development connection-string placeholders |
| 11 | `docker/` was a `.gitkeep` stub | Multi-stage non-root Dockerfile + compose stack + `.env.example` + `.dockerignore` |
| 12 | CI workflow minimal (restore/build/test only) | Hardened: `permissions`, `CI: true`, `global.json` pin, style-gate step, test-results + API-publish artifacts |
| 13 | `docs/` was a `.gitkeep` stub | This audit document |

## 4. Architecture decisions (this pass)

1. **StyleCop is applied to `src` only.** Test projects inherit the SDK
   analyzers and code-style rules but deliberately skip StyleCop — xUnit code
   is written to a different (test-focused) idiom.
2. **No XML file headers.** `file_header_template = unset` and
   `SA1633` disabled; `stylecop.json` disables all documentation rules
   (`documentExposedElements/InternalElements/PrivateElements/PrivateFields`
   and `documentInterfaces` all `false`). XML docs can be introduced later
   without changing these settings.
3. **`GenerateDocumentationFile=true` + `NoWarn CS1591`.** Enables the
   IDE0005 (unused using) build gate without requiring doc comments.
4. **Build-enforced style gates** (`warning` → error via
   `TreatWarningsAsErrors`): IDE0005 (unused usings), IDE0011 (braces),
   IDE0036 (modifier order), IDE0130 (namespace matches folder), IDE0161
   (file-scoped namespace), IDE0240 (redundant nullable directives). All other
   style options stay at `suggestion`.
5. **Serilog: standard sinks only.** Console + rolling File are configured in
   appsettings; the Seq sink is *opt-in* via compose environment overrides
   (`Serilog__WriteTo__2__*`), so local runs need no Seq instance. No custom
   sinks.
6. **Health-check surface.** `/health` (all checks), `/health/live`
   (liveness — no checks), `/health/ready` (readiness — all checks). The check
   set is intentionally empty until modules register infrastructure checks.
7. **Test packages moved to latest stable** (Test.Sdk 18.8.1, xunit 2.9.3,
   runner 3.1.5, coverlet 10.0.1) and verified green under warnings-as-errors.
8. **`Program` partial is declared as an empty class body**, not
   `public partial class Program;`, keeping `WebApplicationFactory<Program>`
   support while satisfying StyleCop (the `;` form is an "empty statement"
   and crashes SA1106/SA1500/SA1502/SA1508).
9. **Docker:** single `Dockerfile` (SDK→publish, then `aspnet:9.0` runtime
   running as the non-root `app` user, `curl` installed for health checks,
   writable `/app/logs` for the File sink). Compose provides the full local
   stack: API + PostgreSQL 16 + Redis 7 + Seq + PgAdmin, named volumes, custom
   bridge network, `restart: unless-stopped`, and health checks where
   applicable. Secrets are placeholders in `.env.example` only.

## 5. Verification results

| Gate | Result |
|------|--------|
| `dotnet restore LastHour.Backend.sln` | 35 projects restored |
| `dotnet build ... -c Release` | 0 warnings, 0 errors |
| `dotnet test ...` | exit code 0 (empty suites; host + runner functional) |
| `dotnet format --verify-no-changes` | exit code 0 (fully conformant) |
| `docker compose config` | valid (with `.env.example`) |

## 6. Tech debt / deferred work

> Resolved in Sprint 0.2: `packages.lock.json` is now committed per project
> (`RestorePackagesWithLockFile` in `Directory.Build.props`), CI restores with
> `--locked-mode`, NuGet caching is keyed on the lock files, GitHub Actions are
> pinned to commit SHAs, and Dependabot watches NuGet + Actions updates.
> Docker image build is validated by the CI `docker-build` job.

- **Docker image build** not executed in the original pass (Docker daemon was
  not running); the Dockerfile follows standard, conservative patterns and is
  now validated in CI on every push/PR.
- **Seq credentials** (`SEQ_PASSWORD_HASH`, `SEQ_API_KEY`) are operator
  supplied; the compose health check assumes the Seq `/health` endpoint.
- **Health checks are not yet meaningful** — no DB/Redis adapters exist, so
  `/health/ready` has an empty check set. Modules will register checks as
  infrastructure lands.
- **SDK 10.0.x** is installed locally but `global.json` deliberately stays on
  9.0.x until the upgrade is planned.
- **ArchUnit-style tests** (enforcing the reference graph) are the first
  candidate for the next sprint.

## 7. Next sprint (Sprint 00.2)

- Shared kernel primitives (`Result`, `Error`, `ValueObject`, `Entity`,
  `AggregateRoot`, `IDomainEvent`).
- Application abstractions (`ICommand`, `IQuery`, handlers, integration
  events) and Contracts envelopes.
- Architecture enforcement tests (reference-graph + naming).
- First real infrastructure wiring (EF Core / Dapper, Redis) which will
  populate health checks and connection strings.
