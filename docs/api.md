# API

How the LastHour HTTP API is hosted, routed, and shaped. This document
describes behavior that exists today; it does not describe future Identity or
business endpoints.

## Hosting model

- ASP.NET Core (minimal hosting model) with **minimal endpoint modules** —
  there are no feature controllers yet. MVC is registered
  (`services.AddControllers()`) so controllers can be added later without
  changing the composition root.
- `src/Api/LastHour.Api` is the composition root: `Program.cs` wires the
  infrastructure layer (PostgreSQL / EF Core, unit of work, repositories,
  outbox, seeding, health checks), the CQRS pipeline, and the HTTP surface
  through `AddLastHourApi(...)`. Endpoints orchestrate requests through
  application abstractions and never touch infrastructure types.
- Endpoint modules live under `src/Api/LastHour.Api/Endpoints/<Feature>/` as
  static classes exposing `IEndpointRouteBuilder` extension methods
  (`MapSystemEndpoints`, `MapProblemDetailsEndpoints`), composed in
  `EndpointExtensions.MapLastHourEndpoints`.

## Middleware order

`Program.cs` registers the pipeline in this order (top = outermost):

1. Correlation ID middleware
2. Exception handler (converts unhandled exceptions to ProblemDetails)
3. Swagger / Swagger UI (Development only)
4. Forwarded headers
5. HTTPS redirection
6. Security headers (including HSTS and `X-Content-Type-Options`)
7. CORS
8. Response compression
9. Request logging (structured, one event per request)
10. Audit logging
11. Routing
12. Rate limiting
13. Output caching
14. Endpoints

## Base path & versioning

- Routes are versioned through the URL segment: `/api/v1/...` (Asp.Versioning,
  URL-segment strategy). `ApiVersions` declares V1 (served today) and V2/V3
  (reserved); unsupported versions return `404`.
- The service root is unversioned: `GET /` returns `{ "service": "LastHour.Api", "status": "ready" }`.

## Response contract

- Success responses are plain DTOs serialized as JSON — there is no
  `ApiResponse<T>` envelope.
- Failed results flow through `ResultEndpointFilter` (`.MapResultToProblemDetails()`),
  which converts `LastHour.BuildingBlocks.SharedKernel.Results.Result` failures
  into RFC 7807 ProblemDetails responses. Endpoints return `Result` and never
  construct error responses by hand.
- Error responses follow RFC 7807 (`application/problem+json`) with the
  project's extensions: `code` (stable machine-readable error code) and
  `correlationId`.

## ProblemDetails & exception handling

- `ProblemDetailsExceptionHandler` (`IExceptionHandler`) is the single
  unhandled-exception path, registered via `UseExceptionHandler()`.
- **Production** returns a generic 500 response — no stack traces, SQL,
  connection strings, paths, or internal exception text.
- **Development** may include a redacted message, but secrets are still
  redacted (connection strings, passwords, etc.).
- Every ProblemDetails response carries the request correlation id.
- Known error categories are mapped to status codes by
  `ErrorToProblemDetailsMapper` (validation → 400, not-found → 404, conflict →
  409, unauthorized → 401, forbidden → 403, failure → 500, etc.).
- Demonstration routes exist under `GET /api/v1/system/problems/{kind}` with
  kinds `validation`, `not-found`, `conflict`, `unauthorized`, `forbidden`,
  and `unhandled` — they document the error contract end to end.

## Validation

- Request validation uses the application-layer `Result`/`Error.Validation`
  pattern (via the CQRS pipeline and `ResultEndpointFilter`); there is no
  separate DataAnnotations/FluentValidation stack on the HTTP surface.
- Validation failures become ProblemDetails responses with per-field error
  details; they do not leak implementation details.

## Correlation & logging

- Every response carries `X-Correlation-ID`. The middleware accepts an
  incoming header of the same name or generates one; the value is written into
  request-scoped state so logs and ProblemDetails share it.
- Request logging emits one structured event per request: method, path,
  status, duration, remote IP, user agent, correlation id, request id
  (`TraceIdentifier`), and authenticated user. **Bodies, query strings,
  headers, tokens, and secrets are never logged.**

## Health endpoints

- `GET /health` — aggregate health (all checks; `503` when any fails).
- `GET /health/live` — liveness only (always `200` while the process runs).
- `GET /health/ready` — readiness only (database, disk, memory, and — when
  configured — Redis checks; `503` when any fails).
- Health responses return minimal status text and never expose connection
  strings, hostnames, or stack details. Health endpoints bypass rate limiting.

## Swagger / OpenAPI

- Swagger UI and the Swagger JSON documents are **Development-only**
  (guarded by `app.Environment.IsDevelopment()`); they are not served in
  production.
- One document per supported API version at `/swagger/v{version}/swagger.json`,
  generated from XML doc comments on every `LastHour.*` assembly, with
  operation filters for version defaults, the correlation header, and
  ProblemDetails responses.

## CORS

- CORS is configuration-driven (`Cors:AllowedOrigins`); production must not
  use `AllowAnyOrigin` (the options validator rejects permissive wildcard
  configurations). See `docs/SECURITY.md`.

## Rate limiting

- Policies are declared in configuration (`RateLimiting:Policies`) with
  algorithm, partition key, and limits; validated at startup
  (`ValidateOnStart`). The `Global` policy is applied to every request when
  `RateLimiting:GlobalPolicyName` is set; endpoints can opt into a named policy
  with `RequireRateLimiting(...)` (e.g., the `SystemStatus` policy on
  `/api/v1/system/status`).
- Rejected requests receive `429` with an RFC 6585 `application/problem+json`
  body and a `Retry-After` header when the limiter provides one.

## HTTP status code conventions

Established by the error mapper and tests:

| Outcome | Status |
| --- | --- |
| Success (GET/status) | 200 OK |
| Validation failure | 400 Bad Request |
| Unauthenticated | 401 Unauthorized |
| Forbidden | 403 Forbidden |
| Not found | 404 Not Found |
| Conflict | 409 Conflict |
| Rate limited | 429 Too Many Requests |
| Unhandled exception | 500 Internal Server Error |

`201`/`202`/`204` are reserved for future write endpoints; they are not used
today because no business endpoints exist yet.

## Testing

The API is exercised with `WebApplicationFactory<Program>`
(`tests/Api/LastHour.Api.Tests/Support/LastHourApiFactory.cs`). Run:

```bash
dotnet test tests/Api/LastHour.Api.Tests/LastHour.Api.Tests.csproj
```

Coverage includes: system endpoints and health, versioning, ProblemDetails
demonstration routes, the exception handler's safe-production contract,
correlation ID behavior, security headers, CORS, rate limiting, audit
middleware, and output caching.
