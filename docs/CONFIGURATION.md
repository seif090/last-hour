# Configuration

Every configuration surface of the LastHour API. All values come from
`appsettings.json`, environment-specific overrides, and environment variables
(through the standard .NET configuration provider chain and the `Secrets`
source described below).

## Options sections

Options are bound from configuration and validated with
`ValidateOnStart`; a misconfigured value fails the process at startup instead
of surfacing as a runtime error. This includes `RateLimiting`, whose policies
must be uniquely named with algorithm-appropriate limits, a rejection status
in the 400-599 range, and a header name when partitioning by header.

| Section | Purpose | Key settings |
|---------|---------|--------------|
| `ConnectionStrings:Postgres` | PostgreSQL connection string | host, port, database, credentials |
| `ConnectionStrings:Redis` | Redis connection string for the health check and rate limiter backplane | optional; no value disables the Redis check |
| `Outbox` | Transactional outbox processor | `Enabled`, `ProcessingInterval`, `BatchSize`, `MaxRetryCount` |
| `DatabaseInitializer` | Startup migration + seeding | `Enabled` (gates on Development environment + relational provider) |
| `HealthChecks` | Health probe behavior | `TimeoutSeconds`, `Redis`, `Disk` (`Path`, `MinimumFreeMegabytes`), `Memory` (`MaximumUsedBytes`) |
| `Cqrs:Performance` | Slow-request threshold | `SlowRequestThreshold` (e.g. `"00:00:01"`) |
| `ResponseCompression` | HTTP compression | `Enabled`, `EnableForHttps`, `CompressionLevel`, `MimeTypes` |
| `Cors` | Cross-origin policy | `Enabled`, `AllowAnyOrigin`, `AllowedOrigins`, `AllowCredentials` |
| `Secrets` | Environment-variable secret resolution | `Enabled`, `EnvironmentVariablePrefix`, `Names` |
| `RequestLimits` | Kestrel request hardening | `Enabled`, `MaxRequestBodySize`, `RequestHeadersTimeout`, `KeepAliveTimeout`, `MultipartBodyLengthLimit`, `MinRequestBodyDataRateBytesPerSecond`, `MinRequestBodyDataRateGracePeriod` |
| `AuditLogging` | Security audit trail | `Enabled`, `FilePath`, `RetainedFileCount` |
| `OutputCache` | Response caching | `Enabled`, `UseCaseSensitivePaths`, `Profiles` |
| `RateLimiting` | API rate limiting | `Enabled`, `RejectionStatusCode`, `GlobalPolicyName`, `Policies` |
| `OpenTelemetry` | Traces and metrics | `Enabled`, `TracingEnabled`, `MetricsEnabled`, `ConsoleExporterEnabled` (local debugging), `UseOtlpExporter` + `OtlpEndpoint` (collector), `ServiceName`, `ServiceVersion` |
| `Serilog` | Logging pipeline | `MinimumLevel`, `Enrich`, `WriteTo` (console, rolling file, optional Seq) |

## Secrets

The `Secrets` provider substitutes environment variables for named configuration
keys at startup. With `EnvironmentVariablePrefix=LASTHOUR_SECRET_` and
`Names=["ConnectionStrings:Postgres"]`, the value of `LASTHOUR_SECRET_ConnectionStrings__Postgres`
replaces the connection string. The provider never writes secrets to logs; the
problem-details exception handler redacts connection-string credentials.

## Environment variables

Configuration uses the standard `__` (double underscore) separator for nested
keys, so `Serilog__WriteTo__2__Name=Seq` sets the third Serilog sink. The
container image (`docker/Dockerfile`) expects `ASPNETCORE_URLS=http://+:8080`
and the docker-compose stack injects connection strings and the Seq endpoint
from `docker/.env`.

## Host hardening

- `AllowedHosts` is `*` in the base configuration. Production deployments must
  override it with their canonical hostnames (host-header validation); this is
  a deployment responsibility because the value is deployment-specific.
- `ASP.NET Core` development certificates and the `https` launch profile are
  localhost-only; the container runtime serves HTTP on port 8080 behind the
  reverse proxy.
