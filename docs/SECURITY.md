# Security

The security posture of the LastHour backend. Production deployments should
treat the defaults in `appsettings.json` as development values and override
them per the checklist at the end.

## Defense-in-depth layers

| Layer | Mechanism |
|-------|-----------|
| Authentication boundary | Not yet implemented (Epic 01). The `ICurrentUser` abstraction isolates the host from authentication; the API currently runs with an anonymous/default principal in non-request contexts. |
| Authorization | Out of scope until identity lands; the audit middleware already records rejected access attempts. |
| Rate limiting | Fixed/sliding window, token bucket, concurrency, or no-limit per policy; default global policy is a 120/min fixed window with `PartitionBy=Global` and no queuing (bursts are rejected with 429, not buffered). |
| Request limits | Kestrel-level limits: max request body 10 MiB, multipart 128 MiB, header timeout 30s, keep-alive 2m10s, minimum data-rate enforcement to slow-loris. |
| CORS | Off by configuration risk: `AllowAnyOrigin=false`, `AllowCredentials=false`, explicit `AllowedOrigins` required. |
| Security headers | `SecurityHeadersMiddleware` applies the configured headers on every response. |
| Correlation ID | Every request carries/echoes `X-Correlation-ID`; it appears in logs and problem-details so incidents trace across layers. |
| Audit trail | Rejected access (401/403/429) and server errors (5xx) are written to a dedicated audit log, separate from operational logs. |
| Error contract | Failures return RFC 7807 problem details; unhandled exceptions are caught by the exception handler and their connection-string/secret references are redacted before logging. |

## Secrets handling

- Connection strings and other secrets are injected at startup from
  `LASTHOUR_SECRET_*` environment variables (see `docs/CONFIGURATION.md`), so
  credentials are never committed.
- The audit logger and request logger never write request bodies, query
  strings, headers, passwords, tokens, or secrets.
- The problem-details exception handler redacts `Password=`-style
  connection-string segments from any logged exception message.
- `docker/.env.example` is explicitly placeholders-only; never commit real
  secrets.

## Dependency security

The CI pipeline runs `dotnet list LastHour.Backend.sln package --vulnerable`
after restore and fails the build when any known-vulnerable package is
reported. Keep the CPM versions in `Directory.Packages.props` current as part
of the regular maintenance cycle.

## Container security

- Non-root `app` user; no `root`-owned writable paths needed at runtime.
- `DOTNET_EnableDiagnostics=0` (no diagnostics IPC exposed).
- Only port 8080 is exposed; `postgres`, `redis`, `seq`, and `pgadmin` host
  ports are bound to `127.0.0.1` for local development.
- The CI `docker-build` job builds the image on every push/PR, so a broken or
  supply-chain-changed base image fails the pipeline.

## Host-header and DNS notes

- `AllowedHosts: "*"` in the base config accepts any host header. Production
  must set it to the canonical hostnames — host-header validation rejects
  spoofed `Host` headers used in cache-poisoning and link-injection attacks.
- The ForwardedHeaders middleware is configured and validated by options;
  place the API behind a reverse proxy that sets `X-Forwarded-Proto`,
  `X-Forwarded-Host`, and `X-Forwarded-For` so HTTPS detection and client IPs
  are correct and the forwarded headers themselves are stripped at the edge.

## Identity (future)

Identity, users, and access-control enforcement are explicitly out of scope
for the hardening sprint. The seams are ready: `ICurrentUser`/`ICurrentTenant`
abstractions, the audit trail recording access outcomes, and the
`IsInRole`-style checks on the default principal that currently deny
everything. Implement authentication before exposing any protected endpoint.
