# LastHour — Backend

Enterprise .NET 9 backend for the LastHour platform.

This repository is the production solution skeleton. Sprint 00.1 established
the project layout, building blocks, modules, API host, test projects, and —
in the hardening pass — the enterprise quality gates (CPM, StyleCop,
warnings-as-errors, Serilog, health checks, Docker, CI). No business logic is
implemented yet: foundation only.

## Solution layout

```
LastHour.Backend.sln
src/
  Api/                        # Web API host (LastHour.Api) — composition root
  BuildingBlocks/             # Cross-cutting concerns
    Architecture
    Application
    Contracts
    Infrastructure
    SharedKernel
  Modules/                    # Domain modules
    Administration
    Analytics
    Catalog
    Identity
    Inventory
    Merchant
    Notifications
    Offers
    Ordering
    Payments
    Platform
tests/
  Api/                        # Web API integration tests
  BuildingBlocks/             # Cross-cutting tests
  Modules/                    # Module tests (mirror of src/Modules)
docker/                       # Dockerfile + docker-compose + .env.example
deploy/                       # Deployment manifests (added in later sprints)
scripts/                      # Local utility scripts (added in later sprints)
.github/                      # CI workflows
.ai/                          # AI engineering context (do not edit by hand)
docs/                         # Long-form design documentation
```

## Build & verify

```powershell
dotnet restore
dotnet build --configuration Release
dotnet test  --configuration Release
dotnet format --verify-no-changes
```

The solution targets `net9.0` (pinned via `global.json`), treats warnings as
errors, enables nullable reference types, runs the latest-recommended .NET
analyzers plus StyleCop (src projects), enforces code style at build, and
centralizes package versions (`Directory.Packages.props`). Restore is
deterministic: every project commits a `packages.lock.json` and CI restores
with `--locked-mode`. See `docs/ARCHITECTURE_AUDIT.md` for the full audit.

## API surface

| Endpoint | Purpose |
|----------|---------|
| `GET /` | Readiness probe (JSON) |
| `GET /health` | All health checks |
| `GET /health/live` | Liveness (process is up) |
| `GET /health/ready` | Readiness (dependencies reachable) |
| `GET /swagger/v1/swagger.json` | OpenAPI document (Development only) |

See `docs/api.md` for the full API conventions: versioning, response
contract, ProblemDetails, rate limiting, correlation IDs and health.

Logging is Serilog (structured) with Console + rolling File sinks; a Seq sink
can be enabled via environment overrides in `docker/docker-compose.yml`.

## Container image

Build the production image from the repository root (non-root `app` user,
port 8080):

```powershell
docker build -f docker\Dockerfile -t lasthour-api .
```

The image restore runs in `--locked-mode`; a stale `packages.lock.json` fails
the build. CI validates the image on every push/PR.

## Local stack (Docker)

The compose file brings up the API plus PostgreSQL, Redis, Seq, and PgAdmin.

```powershell
Copy-Item docker\.env.example docker\.env   # then edit placeholders
docker compose -f docker\docker-compose.yml --env-file docker\.env up -d --build
```

- API: `http://localhost:8080`
- Seq: `http://localhost:5341`
- PgAdmin: `http://localhost:5050` (email/password from `.env`)
- PostgreSQL: `localhost:5432`, Redis: `localhost:6379`

Named volumes persist PostgreSQL, Redis, Seq, and PgAdmin data. Containers
run with `restart: unless-stopped` on a dedicated bridge network with health
checks.

## Reference graph

Dependencies flow downward only. There are no cycles.

```
Architecture
   ^
SharedKernel
   ^
Application     --->  Contracts   <-- each module
   ^                ^
Infrastructure     Modules/*
   ^
   Api  ---->  all building blocks + all modules
```

## Sprint status

- **Sprint 00.1 — Foundation:** complete. Enterprise quality gates (CPM,
  StyleCop, warnings-as-errors, nullable), Serilog, API versioning, output
  caching, rate limiting, correlation IDs, problem details, Docker, CI.
- **Sprint 00.5 — Infrastructure Foundation:** complete. `LastHourDbContext`
  with conventions/interceptors, EF Core 9 options wiring, unit of work,
  repositories, transactional outbox, idempotent seeding, health checks
  (postgres/disk/memory/redis), and the `AddLastHourInfrastructure` composition
  root. See `docs/INFRASTRUCTURE.md` and `.ai/context/current-state.md` for the
  live status.
- **Sprint 00.6 — Security, Observability & Runtime Infrastructure:** complete.
  Serilog enrichment + JSON file sink, correlation ids, per-request structured
  logging, OpenTelemetry (OTLP/console), tag-based liveness/readiness health
  checks, security headers (CSP/HSTS), forwarded headers, config-driven CORS,
  environment-backed secrets, Kestrel request limits, a dedicated audit trail,
  startup validation for every options section, and a performance review. See
  `docs/RUNTIME_HARDENING.md`.

No business modules or authentication are wired yet.
