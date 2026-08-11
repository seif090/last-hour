# Observability

How the LastHour backend is observed: logging, request/audit trails,
telemetry, and health.

## Structured logging (Serilog)

Configured in `Serilog` (`appsettings.json`):

- **Sinks**: console (all environments — required for `docker logs`),
  rolling file `logs/last-hour-.log` (compact JSON, daily roll, 14
  retained), optional Seq when configured through the environment.
- **Enrichment**: `FromLogContext`, machine name, environment, process id,
  thread id.
- **Levels**: `Information` default; `Microsoft`/`Microsoft.AspNetCore`
  dropped to `Warning`; `Microsoft.Hosting.Lifetime` and the health-check
  diagnostics stay at `Information`.

All log statements in `src` use `LoggerMessage.Define` (high-performance,
structured) — never string interpolation. The `EventId` per message gives a
stable machine-readable code (e.g. `SlowRequest`, `UnhandledException`).

## Correlation ids

`CorrelationIdMiddleware` honors an incoming `X-Correlation-ID`, generates one
when absent, echoes it on every response, and stores it in
`HttpContext.Items["CorrelationId"]`. The request logger pushes a `RequestId`
(`TraceIdentifier`) into the Serilog context for the duration of the request,
so every log line emitted while processing a request carries it.

## Request logging

`RequestLoggingMiddleware` records one structured event per request: method,
path, status, execution time, correlation id, plus `RemoteIpAddress`,
`UserAgent`, `UserId`, and `Authenticated` pushed into the log context. It
never logs bodies, query strings, headers, or secrets.

## Audit trail

`AuditMiddleware` + `IAuditLogger`/`SerilogAuditLogger` write security-relevant
outcomes — 401, 403, 429, and 5xx — to a **separate** audit log
(`logs/last-hour-audit-.log`). Events carry a stable `EventType`
(`AccessRejected`/`ServerError`), the action (`METHOD path`), outcome, status
code, correlation id, and client IP. Audit events deliberately carry no
request payload. The audit log is excluded from the redaction and
sample-rate concerns that apply to operational logs.

## Pipeline diagnostics

MediatR pipeline behaviors in Infrastructure emit structured events:

- `RequestLoggingBehavior` — request start/finish with correlation id.
- `PerformanceBehavior` — warning when a request exceeds
  `Cqrs:Performance:SlowRequestThreshold`.
- `UnhandledExceptionBehavior` — error event with the failed result type.
- `ValidationBehavior` — records validation failures with the failing result
  error types.

## Telemetry (OpenTelemetry)

`OpenTelemetryServiceCollectionExtensions` wires tracing, metrics, and logging
from the `OpenTelemetry` configuration (validated by
`OpenTelemetryOptionsValidator`): ASP.NET Core, `HttpClient`, EF Core/Npgsql,
and application instrumentation sources and meters. Exporters and endpoints
are set from configuration so the API can push to any OTLP collector.

## Health

Health probes are described in `docs/OPERATIONS.md`. Each check carries
`live`/`ready` tags so liveness (`process is up`) is independent of readiness
(dependencies reachable). The Redis check only registers when a connection
string is configured, and uses `AbortOnConnectFail=false` so a down Redis
yields a degraded health result rather than an HTTP 500.

## Metrics of interest

- Request latency, status, and correlation id (request logger).
- Slow-request count (performance behavior).
- Outbox processing failures and abandoned messages (outbox processor).
- Health check outcomes for `self`, `postgres`, `disk`, `memory`, `redis`.
