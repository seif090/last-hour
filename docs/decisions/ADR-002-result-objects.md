# ADR-002: Result objects instead of exceptions for expected failures

**Status:** Accepted

**Context:** Command/query handlers face expected failures (validation errors,
not-found, conflict, forbidden) as a matter of routine. Using exceptions for
these makes the failure path slow, obscures the failure type in signatures, and
forces the API to catch-and-map everywhere.

**Decision:** Handlers return `Result` or `Result<T>` (`SharedKernel.Results`)
carrying an `Error` (typed: `ValidationError`, `NotFoundError`,
`ConflictError`, `UnauthorizedError`, `ForbiddenError`). The
`ResultEndpointFilter` converts failed results into RFC 7807 problem details.
Exceptions remain reserved for genuine programming errors and are handled once
by `UnhandledExceptionBehavior` and the problem-details exception handler.

**Consequences:** Failure semantics are explicit in signatures and testable
without throwing. The one place that knows about HTTP mapping is the API layer.
The cost is discipline: handlers must return results, and infrastructure must
not swallow exception-based failures meant for the pipeline.
