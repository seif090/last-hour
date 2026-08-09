# ADR-004: CQRS via MediatR with pipeline behaviors

**Status:** Accepted

**Context:** Commands (state-changing) and queries (read-only) have different
cross-cutting needs — validation, logging, performance measurement, transaction
wrapping, exception containment. Wiring these into every handler by hand
repeats logic and invites drift.

**Decision:** Commands and queries derive from MediatR's `IRequest` markers
(`ICommand : IRequest<Result>`, `IQuery<T> : IRequest<Result<T>>`) and are
handled by `ICommandHandler`/`IQueryHandler` implementations discovered by
assembly scan. Cross-cutting concerns are MediatR pipeline behaviors:
`ValidationBehavior`, `RequestLoggingBehavior`, `PerformanceBehavior`,
`TransactionBehavior`, `UnhandledExceptionBehavior`.

**Consequences:** Handlers stay thin and focused; cross-cutting behavior is
consistent and testable in isolation. The marker contracts are locked by
contract tests so a broken inheritance/constraint would fail the build.
MediatR is an explicit framework dependency of the Application layer.
