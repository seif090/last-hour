# ADR-019 — Authorization & Policy Enforcement

- **Status:** Accepted
- **Decided by:** Sprint 08 — Authorization & Policy Enforcement
- **Related:** ADR-018 (roles & permissions model), ADR-015 (JWT access tokens),
  ADR-017 (account security), ADR-011 (API contracts and ProblemDetails)

## Context

Sprint 07 established the role/permission model, the permission catalog, per-request
permission resolution (`GetPermissionsForUserAsync`, a single database projection) and
application use cases — but deliberately exposed no privileged management endpoints
because no enforcement layer existed. Sprint 08 must turn that foundation into actual
authorization: protect endpoints by permission, separate authentication from
authorization, and return consistent forbidden responses.

## Decision

- **Permission-based enforcement over role-based attributes.** Application capabilities
  are expressed as catalog permissions (`Roles.Manage`, `Roles.Read`, …) resolved from
  the live database, never as role names in `[Authorize(Roles = "...")]`. Roles remain
  the grouping mechanism; permissions are the decision surface.
- **One focused requirement and handler.** `PermissionRequirement` (an immutable record
  carrying one catalog code) is decided by `PermissionAuthorizationHandler`, which
  resolves the caller's user id from the validated `sub` claim and checks the live
  effective permission set. No authorization logic lives in controllers or attributes.
- **Dynamic policy provider with a strict convention.** `Permission.<code>` policy
  names map on demand to a `PermissionRequirement` via `PermissionPolicyProvider`;
  unknown codes fail closed, and non-convention policies fall through to the framework
  default. This keeps the policy surface in lockstep with the catalog without
  per-permission registration, and avoids a general-purpose dynamic-policy engine.
- **401 and 403 are never conflated.** Unauthenticated/invalid-token requests keep the
  framework challenge (401 + `WWW-Authenticate`); authenticated callers who fail a
  policy receive a consistent RFC 7807 403 ProblemDetails payload via a custom
  `IAuthorizationMiddlewareResultHandler` that preserves the default challenge path.
- **A deliberately small protected surface.** The four role-management endpoints
  (`POST /roles`, `GET|PUT|DELETE /users/{userId}/roles[/{roleId}]`) complete the
  Sprint 07 deferral now that they can be protected. Self-service account endpoints stay
  authentication-only; registration/login/refresh/logout/health stay anonymous.
- **No caching.** Permission resolution is a single indexed query and the consistency
  model wants role changes to apply immediately; caching would add a staleness window
  for no benefit at this scale. No Redis, no in-memory cache.
- **No permissions in the JWT.** The access token stays subject-only (ADR-015); every
  decision reads live state, so assignment changes affect already-issued tokens
  immediately — proven by an end-to-end forbid → allow → forbid test with one token.
- **The HTTP-aware `ICurrentUser`.** `HttpContextCurrentUser` replaces the
  unauthenticated infrastructure default inside the API host so application-layer code
  sees the real caller; claim-to-user-id parsing stays centralized in
  `ClaimsPrincipalExtensions.GetUserId()`.

## Consequences

- Privileged operations are now safe to expose: the `Roles.*` endpoints are reachable
  only by callers holding the corresponding permission, and every failure mode is a
  safe, consistent HTTP response.
- Role changes propagate instantly by design — there is deliberately no "the change
  takes effect on the next token" window. The price is one indexed query per protected
  request, which is the accepted cost of freshness at this scale.
- The `ITrackedRepository` contract gained `GetByIdIncludingAsync` (with a default
  fallback) so aggregates that mutate owned collections load them eagerly; this fixed a
  latent defect where `RemoveRoleFromUser` could never see existing assignments through
  EF Core and always failed with a conflict.
- Authorization state remains split across modules (the catalog lives in the Identity
  module); future modules contribute their own permission definitions to the catalog.
