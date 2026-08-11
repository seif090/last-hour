# ADR-011: Minimal endpoints, URL versioning, Result → ProblemDetails

**Status:** Accepted

**Context:** The API host needs a consistent contract for every future module:
stable routing, a single error shape, and no competing response formats. The
application layer returns `Result`/`Result<T>` for expected failures
(ADR-002); the HTTP layer must translate those without inventing a parallel
error system.

**Decision:** ASP.NET Core minimal hosting with endpoint modules under
`Endpoints/<Feature>/` as static `IEndpointRouteBuilder` extensions. Routes
are versioned by URL segment (`/api/v1/...`) via Asp.Versioning; unversioned
system endpoints (health, service root) sit outside `/api`. Success responses
are plain DTOs. Expected failures surface as RFC 7807 ProblemDetails: the
`ResultEndpointFilter` (`.MapResultToProblemDetails()`) converts `Result`
failures, and `ErrorToProblemDetailsMapper` maps error categories to status
codes (validation 400, not-found 404, conflict 409, unauthorized 401,
forbidden 403) with `code` and `correlationId` extensions. Unexpected
exceptions go through the single `ProblemDetailsExceptionHandler`
(`IExceptionHandler`): sanitized in all environments, diagnostics only in
Development, never stack traces, SQL, or credentials.

**Consequences:** One error contract end to end (demonstrated by the
`/api/v1/system/problems/{kind}` routes). Endpoint modules give modules a
place to register routes without touching `Program.cs`. The `Result` filter
must be attached per endpoint group; forgetting it would return raw
`Result` JSON instead of ProblemDetails, which the endpoint conventions and
architecture tests guard against.
