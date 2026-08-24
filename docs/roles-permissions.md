# Roles & Permissions Foundation

- **Sprint:** 07 — Roles & Permissions Foundation
- **Related docs:** [identity](identity.md), [authentication](authentication.md),
  [login-jwt](login-jwt.md), [registration](registration.md),
  [ADR-018 — Roles & Permissions Model](decisions/ADR-018-roles-permissions-model.md)

## Authorization model

The authorization model is the classic roles-and-permissions graph, owned entirely by the
Identity module:

```
User ──< UserRole >── Role ──< RolePermission >── Permission
```

- A **role** is a named, normalized collection of permissions.
- A **permission** is a single stable application capability.
- A **user** holds zero or more roles; a role grants zero or more permissions.
- The **effective permissions** of a user are the union of the permissions of every role the
  user holds.

The model is deliberately minimal: no hierarchy, no permission negation, no resource scoping.
Those are future policy concerns (Sprint 08 territory) and the model supports them without
schema changes.

## Role model

`Role` (Domain/Roles) is an aggregate root with:

- a stable `RoleId` (strongly typed `Guid`);
- a `RoleName` value object with `Value` (display, case preserved) and `NormalizedValue`
  (the single identity used for uniqueness and lookups);
- an optional description;
- audit stamps and the read-only association collections (`UserRoles`, `RolePermissions`).

**Normalization.** Role names use the identity module's single normalization rule — the same
one used for emails: whitespace trimmed and lower-cased with the invariant culture
(`RoleName.Normalize`). `Administrator` and `administrator` are the same role. No other
normalization exists and none should be added.

**Uniqueness.** The database has a unique index on the normalized name
(`IX_roles_normalized_name`), so duplicate roles are impossible even under concurrency. The
`CreateRole` use case pre-checks the normalized name for a clean conflict response, and the
index is the correctness backstop.

## Permission model

`Permission` (Domain/Roles) is an aggregate root with:

- a stable `PermissionId`;
- a `Code` — the deterministic identity of the capability, unique in the database
  (`IX_permissions_code`);
- an optional description.

**Naming convention.** Codes follow the `Resource.Action` convention:

- `Users.Read` — view user accounts
- `Users.Manage` — create, update and disable user accounts
- `Roles.Read` — view roles and their permissions
- `Roles.Manage` — create roles and manage role/permission assignments

The convention is enforced structurally (one or more dot-separated, letter-led segments) by
`PermissionDefinition.HasValidCode`.

**Catalog.** All permissions are declared in `PermissionCatalog` (Application/Permissions) —
the single source of truth. The static constructor fails fast on duplicate codes or
malformed codes, so a broken catalog never reaches production. Permissions are **never**
created from client input; they are seeded from the catalog and referenced by future policy
checks. The catalog is deliberately focused on existing capabilities and grows with the
product.

## Relationships

- `UserRole` — association entity with composite primary key `(UserId, RoleId)`. A user can
  hold many roles and a role can belong to many users; the composite key makes a duplicate
  assignment impossible at the database. Assignment invariants live on the `User` aggregate
  (`AssignRole`/`RemoveRole`/`HasRole`).
- `RolePermissionGrant` — association entity with composite primary key `(RoleId,
  PermissionId)`. A role can hold many permissions and a permission can belong to many roles;
  the composite key makes duplicate grants impossible at the database. Grant invariants live
  on the `Role` aggregate (`AssignPermission`/`RevokePermission`/`HasPermission`).

**Referential actions.** Deleting a role that is assigned to users or holds permissions is
rejected (restrict); deleting a granted permission is rejected (restrict). Deleting a user
cascades to their assignments. Authorization state can never be orphaned silently, and no
multiple-cascade-path is created (important for PostgreSQL).

## Application use cases

The use cases are MediatR-discovered from the Identity module assembly. They are exposed as
HTTP endpoints only where the authorization-enforcement layer (Sprint 08, see
[authorization.md](authorization.md)) protects them with the corresponding catalog
permission — `CreateRole`, `AssignRoleToUser` and `RemoveRoleFromUser` behind
`Roles.Manage`, and `GetUserRoles` behind `Roles.Read`. Assigning a role is a
privilege-granting operation and is never reachable through an unprotected flow.

| Use case | Result | Notes |
|---|---|---|
| `CreateRoleCommand` | `RoleId` | Duplicate normalized name → conflict |
| `AssignRoleToUserCommand` | success | Both sides must exist; duplicate → conflict; user concurrency token bumped |
| `RemoveRoleFromUserCommand` | success | Role not held → conflict |
| `GetUserRolesQuery` | roles | Unknown user → not found |
| `GetRolePermissionsQuery` | permissions | Unknown role → not found |
| `GetUserPermissionsQuery` | permissions | The authorization resolution foundation (see below) |

`GetUserPermissionsQuery` resolves the live effective permission set of a user in **a single
query** (a navigation chain `Permission → RolePermissions → Role → UserRoles → UserId`
translated by EF), never an N+1 loop — this is the foundation future authorization checks
build on.

## Default role strategy

**No default role is assigned during registration.** The business requirements do not define
one, and assigning any role during public registration would be a privilege-escalation
surface. `RegisterUserCommand` accepts only the email and password (guarded by a test), so a
caller can never supply a role or permission through registration. A future sprint can assign
a default role transactionally inside registration once a business requirement defines it.

## Bootstrap / seed strategy

The database bootstrap seeds:

- every permission in `PermissionCatalog`;
- one **Administrator** role (`BootstrapRoles.Administrator`) holding the full catalog;
- the role-permission grants binding them.

The bootstrap **creates no user accounts and no credentials** — assigning the Administrator
role to a real user is a protected provisioning flow, never part of seeding. Seeding is:

- **idempotent by construction** — upserts keyed on the permission code and the normalized
  role name, grants through the aggregate's set semantics, so a re-run cannot duplicate rows;
- **atomic** — the whole bootstrap commits in one unit-of-work save;
- **run exactly once per database** by the `SeederExecutor` seeding history, in Development
  only (see [database](database.md)).

Implementation: the logic lives in `AuthorizationBootstrapService` (module Application, using
only module abstractions + the unit of work); the composition root wraps it in
`RolesAndPermissionsSeeder : IDatabaseSeeder`, which keeps the module free of infrastructure
references and keeps EF out of the API namespace.

## JWT claims decision

**The JWT carries no roles and no permissions.** Sprint 04 kept the access token minimal
(subject only); this sprint confirms and documents that decision. Permission checks will
resolve the **live** effective permission set per request via `GetUserPermissionsQuery` —
role and permission changes take effect immediately, tokens never go stale, and tokens stay
small. Putting permissions in the JWT was rejected because it embeds stale authorization
state, bloats the token, and makes revocation lag. See [login-jwt](login-jwt.md) and
ADR-018.

## Consistency model

- Role/permission changes apply to the database immediately.
- Already-issued access tokens remain valid until they expire (stateless JWTs) — this sprint
  does **not** implement real-time access-token revocation, and it does not claim
  immediate privilege propagation. Future authorization checks evaluate the live permission
  set, so the *next* token or the next request resolves the new state.
- Session (refresh-token) revocation after privilege changes is future work (Sprint 06
  already revokes sessions on password change and logout-all).

## Privilege escalation protections

- Public registration cannot assign roles or permissions (command shape is guarded by tests).
- No management endpoint exists yet, so there is no public role-assignment surface.
- Role/permission identifiers in commands come only from protected flows; nothing is derived
  from unauthenticated input.
- Duplicate assignments are rejected by the domain and by the database composite keys.
- No hardcoded admin account, default password, or privileged secret exists anywhere.

## Security logging

Role changes are logged safely (structured events, no secrets):

- `CreateRoleCommandHandler` — role created (role id, normalized name);
- `AssignRoleToUserCommandHandler` — role assigned / duplicate rejected (user id, role id);
- `RemoveRoleFromUserCommandHandler` — role removed (user id, role id).

No password, token, hash, or secret is ever logged. Event ids 3001–3005.

## Not implemented yet

- Resource-based (per-object) authorization and dynamic ABAC policies
- Admin management UI or frontend authorization
- Default role assignment at registration
- MFA, OAuth, password reset
- Roles/permissions for the other modules (they have no entities in the shared model yet)

Policy enforcement itself is implemented — see [authorization.md](authorization.md) and
[ADR-019](decisions/ADR-019-authorization-enforcement.md).
