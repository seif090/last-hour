# Operations

How the LastHour backend is built, deployed, and operated.

## Container image

`docker/Dockerfile` is a two-stage build:

1. `mcr.microsoft.com/dotnet/sdk:9.0` restores, builds, and publishes the API.
2. `mcr.microsoft.com/dotnet/aspnet:9.0` runs it as the non-root `app` user on
   `http://+:8080` with `DOTNET_EnableDiagnostics=0`.

`curl` is installed in the runtime image for the compose health check. The
`/app/logs` directory is pre-created and owned by `app` so the Serilog file
sink can roll logs.

Build from the repository root:

```bash
docker build -f docker/Dockerfile -t lasthour-api .
```

## Local stack

`docker/docker-compose.yml` runs the API plus its dependencies:

- `postgres:16-alpine` with a `pg_isready` health check
- `redis:7-alpine` (append-only persistence)
- `seq` (structured log aggregation, requires `SEQ_PASSWORD_HASH`)
- `pgadmin` (PostgreSQL administration)

Copy `docker/.env.example` to `docker/.env` and adjust the credentials before
starting. The API only reaches `postgres`, `redis`, and `seq` over the internal
`lasthour` bridge network; the host ports of `postgres`, `redis`, `seq`, and
`pgadmin` are bound to `127.0.0.1` only and are therefore not reachable from
other machines. Only the API port (`API_PORT`, default `8080`) is exposed to
the network.

```bash
docker compose --env-file docker/.env --project-directory . -f docker/docker-compose.yml up -d
```

## Health probes

| Endpoint | Meaning |
|----------|---------|
| `GET /health` | All registered checks |
| `GET /health/live` | Liveness (process up) |
| `GET /health/ready` | Readiness (dependencies reachable) |
| `GET /` | Readiness probe, JSON |

The container health check polls `http://localhost:8080/health` every 30s.
Orchestrators should use `/health/live` for liveness and `/health/ready` for
readiness so a downed dependency degrades readiness without restarting the
process.

## Logs and audit trail

- Operational logs: `logs/last-hour-.log`, rolling daily, compact JSON, 14
  files retained, console mirror in development.
- Audit trail: `logs/last-hour-audit-.log`, separate file for security events
  (rejected access attempts and server errors), rolling daily, 30 files
  retained. Audit events never contain request bodies, query strings, or
  headers.
- Seq aggregates structured logs when configured with
  `Serilog__WriteTo__2__Name=Seq` and the Seq endpoint/API key.

## Deployment checklist

1. Override `AllowedHosts` with the deployment's canonical hostnames.
2. Provide real connection strings via the `Secrets` provider
   (`LASTHOUR_SECRET_*` environment variables), never in committed config.
3. Set `HealthChecks:Redis:ConnectionString` when Redis is present.
4. Configure rate-limit policies under `RateLimiting:Policies` for the
   traffic profile (defaults: global fixed window 120/min, per-IP sliding
   window 30/min).
5. Run the NuGet vulnerability audit in CI (`dotnet list package --vulnerable`)
   and the Docker image build validation job before releasing.
6. Confirm the reverse proxy forwards `X-Forwarded-*` headers and that the
   ForwardedHeaders middleware settings match the proxy layout.
