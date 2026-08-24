# Authorization & Policy Enforcement

Sprint 08 turns the Roles & Permissions foundation (Sprint 07) into real enforcement.
This document describes the authorization architecture, how a request is decided, the
permission-protected endpoints, and the security and consistency model.

See [roles-permissions.md](roles-permissions.md) for the model (roles, permissions,
catalog, seeding) and [ADR-018](decisions/ADR-018-roles-permissions-model.md) /
[ADR-019](decisions/ADR-019-authorization-enforcement.md) for the decisions.

## Architecture

An authenticated request flows through the pipeline as follows:

```
Authenticated Request
        ↓
JWT Authentication (Sprint 04)        → 401 on missing/invalid token
        ↓
Authenticated UserId (from claims)
        ↓
Permission Resolution (live, single query)
        ↓
PermissionRequirement
        ↓
PermissionAuthorizationHandler
        ↓
Allow → endpoint executes        Deny → 403 ProblemDetails
```

The authorization system never trusts permissions supplied by the client. Permissions
are resolved server-side from the database for the caller identified by the validated
access-token `sub` claim — never from request bodies, query parameters, or token claims.

## Permission requirement

`PermissionRequirement` (`LastHour.Api.Security.Authorization`) is an immutable record
that carries exactly one thing: the required permission code from the centralized
[permission catalog](roles-permissions.md#permission-model). It contains no database
access and no decision logic — it is pure metadata describing what the caller must hold.

## Authorization handler

`PermissionAuthorizationHandler` (`AuthorizationHandler<PermissionRequirement>`) decides
the requirement:

1. If the principal is not authenticated, the requirement fails (the framework then
   issues the 401 challenge).
2. The user id is resolved from the validated `sub` claim via the shared
   `ClaimsPrincipalExtensions.GetUserId()` helper — the same single source the
   authenticated endpoints use. A missing or malformed subject fails safely.
3. The caller's effective permissions are resolved with
   `IRoleRepository.GetPermissionsForUserAsync(userId)`, the single-projection query the
   identity module built for exactly this path (no N+1).
4. If the required code is in the set, the requirement succeeds.

The handler is registered **scoped** because it depends on the scoped role repository
(and through it the request's unit of work). It is resolved from DI per request like any
other handler.

## Effective permission resolution

Effective permissions follow the live graph:

```
User → UserRoles → Roles → RolePermissions → Permissions
```

- Multiple roles are supported: the permission set is the union across every role the
  user holds.
- Overlapping grants are harmless: the repository returns a deduplicated set, so a
  permission granted through several roles satisfies the requirement exactly once.
- Nothing is embedded in the JWT. Access tokens carry only `sub`, `jti` and the
  standard timestamps (see [ADR-015](decisions/ADR-015-jwt-access-tokens.md)); the
  decision is always made against the current database state.

## Policy strategy

A dynamic `PermissionPolicyProvider` maps the `Permission.<code>` policy-name convention
onto a `PermissionRequirement`:

- `[RequirePermission("Users.Read")]` / `.RequirePermission(...)` produce the policy
  name `Permission.Users.Read`.
- The provider builds the policy on demand, so every catalog code is automatically a
  policy — zero per-permission registration, and the policy surface cannot drift from
  the catalog.
- An unknown code still resolves to a requirement that simply never matches a user's
  permission set, so a mistyped attribute **fails closed**.
- Policy names outside the convention fall through to the framework's default provider.

This is the explicit-policy strategy (option A) with a thin convention layer: with a
small, catalog-driven permission set a full dynamic policy system would be
over-engineering, while hard-coding each permission into `AddPolicy` would duplicate the
catalog. The provider keeps the mapping in one place and scales without edits.

The attribute itself is pure metadata — it only maps a catalog code to a policy name.
No authorization logic lives in controllers or attributes.

## Endpoint protection

### Permission-protected (new in Sprint 08)

| Endpoint | Permission | Use case |
|----------|------------|----------|
| `POST /api/v1/roles` | `Roles.Manage` | `CreateRoleCommand` |
| `GET /api/v1/users/{userId}/roles` | `Roles.Read` | `GetUserRolesQuery` |
| `PUT /api/v1/users/{userId}/roles/{roleId}` | `Roles.Manage` | `AssignRoleToUserCommand` |
| `DELETE /api/v1/users/{userId}/roles/{roleId}` | `Roles.Manage` | `RemoveRoleFromUserCommand` |

These are the first endpoints protected by permission enforcement and complete the
Sprint 07 decision to keep role-management use cases application-only until they could
be safely protected. The surface stays deliberately small — it maps exactly the use
cases the identity module exposes (no user CRUD, no permission-grant surface).

### Authenticated-only (Sprint 04/06, unchanged)

`GET /api/v1/auth/me`, `POST /api/v1/auth/change-password`,
`POST /api/v1/auth/logout-all`, `GET /api/v1/auth/sessions` — any authenticated user
may manage their own account, so they require authentication but no permission.

### Anonymous (unchanged)

`POST /api/v1/auth/register`, `POST /api/v1/auth/login`, `POST /api/v1/auth/refresh`,
`POST /api/v1/auth/logout`, the health endpoints and the system status endpoint remain
public. The public surface is never granted a permission implicitly — anonymity is the
explicit default for those routes, and permission-protected routes always require
authentication first.

## Authentication vs authorization (401 vs 403)

- **401 Unauthorized** — the request is not authenticated or the token is invalid
  (missing, malformed, expired, wrong signature, wrong issuer/audience). Produced by
  the JWT bearer handler with its standard `WWW-Authenticate` challenge; behavior is
  unchanged from Sprint 04.
- **403 Forbidden** — the request is authenticated but the caller lacks the required
  permission. Produced by the authorization middleware.

The two are never conflated: authentication failures are challenges, permission
failures are forbids. No permission internals (the required code, the caller's
permission graph, role names) are revealed in either response.

## Forbidden response (ProblemDetails)

`AuthorizationProblemDetailsResultHandler` (a custom
`IAuthorizationMiddlewareResultHandler`) shapes the middleware outcome:

- success → pass through;
- challenge (unauthenticated) → delegated to the framework's default handler, so the
  401 plus `WWW-Authenticate` header behaves exactly as before;
- forbid (authenticated, insufficient permission) → an RFC 7807 payload in the same
  shape as every other API error:

```json
{
  "type": "https://lasthour.dev/errors/forbidden",
  "title": "Forbidden",
  "status": 403,
  "detail": "You do not have permission to perform this action.",
  "code": "AuthorizationFailed",
  "correlationId": "..."
}
```

The correlation id is attached when the correlation middleware has stored one, matching
the exception handler's convention.

## Current user context

- The HTTP-aware `HttpContextCurrentUser` implements the shared `ICurrentUser`
  contract inside the API host, replacing the unauthenticated infrastructure default
  (`DefaultCurrentUser`) for HTTP requests. Application-layer code therefore sees the
  real caller's `UserId`/`IsAuthenticated` during a request; background workers keep
  the unauthenticated default.
- The access token carries no name or role claims, so `Name` and `Roles`/`IsInRole`
  report empty values — authorization deliberately goes through the live permission
  resolution, not through `ICurrentUser.Roles`.
- Endpoints resolve the user id through the centralized
  `ClaimsPrincipalExtensions.GetUserId()`, and the handler resolves it the same way, so
  claim-to-user-id parsing lives in exactly one place.

## User state handling

Authorization is a live, database-backed check on the caller's permission set; it does
not re-validate account status on every request. The documented model (Sprint 06) is
that an issued access token remains valid until its expiry. Therefore a disabled or
suspended account with a still-valid token can continue to call authenticated endpoints
and, while it retains role assignments, permission-protected endpoints until the token
expires. Account-status gating of in-flight tokens is deferred — it would require a
per-request account-state check and a token-revocation strategy, both out of scope for
this sprint. No account states were invented; the existing `AccountStatus` model is
unchanged.

## Performance

- Permission resolution is a single database projection
  (`GetPermissionsForUserAsync`) — the dedicated authorization query, never N+1.
- Role-assignment reads use the read repository (no tracking); mutating flows load the
  tracked aggregate with its owned collections eagerly
  (`GetByIdIncludingAsync`), which is also what makes `RemoveRole` correct.
- No caching was introduced (see below); each check is one indexed query.

## Caching decision

**No caching.** Permissions are few, the resolution is one indexed query, and the
consistency model deliberately wants role changes to take effect immediately. Caching
would introduce a staleness window and an invalidation problem for zero measurable
benefit at this scale. Redis was not added; no in-memory cache is used. If a measurable
hot path appears later, a short-lived, per-request or per-user cache with an explicit
invalidation point (role/permission mutation) is the documented path — not a
distributed cache.

## Consistency model

- Permissions are resolved **live** on every request. Assigning or removing a role
  changes what an *already-issued* access token may do immediately — no new token, no
  waiting for expiry. The API tests prove this end to end with a single token across
  forbid → allow → forbid transitions.
- Concurrent role/permission mutations are safe at the database: the composite keys of
  `user_roles` and `role_permissions` prevent duplicates, and the aggregates' tracked
  loads carry the optimistic-concurrency token, so conflicting writes surface as
  conflicts rather than silently overwriting.
- A request already in flight when a role is removed finishes against the state it
  read; the next request sees the new state. No distributed synchronization is needed
  or used.

## Security considerations

- **No client-controlled authorization**: permission codes, role ids and the caller's
  identity all come from validated claims/routes/server state, never from request
  bodies.
- **No forged role/permission claims help**: the token carries none, so there is
  nothing to forge — and a validly signed token naming a user with no roles is simply
  forbidden.
- **No permissions in the JWT**: tokens stay small and stale-free.
- **Fail closed**: unknown permissions, missing/malformed subjects, and role-less users
  all deny.
- **No leakage**: 401/403 responses expose no permission graph, role structure, hashes
  or internal details; the only identifier is the correlation id.
- **Authorization logging**: mutating role operations log safe structured events
  (event ids 3001–3005, actor/target ids, no tokens/secrets). Normal 403 traffic is not
  logged per-request — only the security-relevant role mutations, avoiding log noise.

## Swagger / OpenAPI

The Bearer scheme is configured once (Sprint 04) and every protected endpoint declares
its 401/403 problem responses explicitly. The role-management endpoints are documented
under the `Roles` tag with the required permission noted in their summaries; no tokens,
secrets or privileged ids appear in the documentation.

## Not implemented yet

- Resource-based authorization (per-object checks) — the requirement/handler model
  extends naturally to resource requirements in a future sprint.
- Dynamic/ABAC policies beyond the `Permission.<code>` convention.
- Admin management UI or frontend authorization.
- Account-status gating of in-flight access tokens.
- MFA, OAuth, password reset.

## Testing strategy

- **Unit tests** (`LastHour.Api.Tests/Security/Authorization`): the requirement, the
  attribute's policy mapping, the policy provider (known/unknown/foreign policies), the
  handler (authenticated with/without permission, unauthenticated, missing/malformed
  subject, overlapping grants, unknown permission), and the result handler
  (success/challenge/forbid shapes, correlation id).
- **API tests** (`LastHour.Api.Tests/Endpoints/AuthorizationEndpointsTests`): the full
  status-code matrix (anonymous → 401, wrong/expired token → 401, no permission → 403
  with ProblemDetails, with permission → success), the live-permission propagation
  scenario, and the security checks (no permission/role claims in tokens; validly
  signed token for a non-existent user is forbidden, never allowed).
- The EF include regression (removing a role requires the aggregate's owned collection
  to be loaded) is covered by the remove-role endpoint test; the fix lives in
  `GetByIdIncludingAsync` on the tracked repository.
