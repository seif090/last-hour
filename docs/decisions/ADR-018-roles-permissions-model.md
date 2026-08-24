# ADR-018 — Roles & Permissions Model

- **Status:** Accepted
- **Decided by:** Sprint 07 — Roles & Permissions Foundation
- **Related:** ADR-014 (Argon2id password hashing), ADR-015 (JWT access tokens),
  ADR-016 (refresh-token rotation), ADR-017 (lockout and password change)

## Context

The authentication chain (Sprints 01–06) establishes identity, credentials, sessions and
account security but has no authorization concept: there is no way to express that a user
may manage users, and no foundation for future policy checks. Sprint 07 must establish the
role/permission model without exposing any privileged management surface that cannot yet be
protected, and without embedding authorization state into the access token.

## Decision

- Model the classic graph — `User — UserRole — Role — RolePermission — Permission` — with
  two aggregates (`Role`, `Permission`) and two association entities with composite primary
  keys. Duplicate assignments and grants are impossible at the database; the aggregates own
  the assignment/grant invariants.
- Role names are normalized with the identity module's single rule
  (`RoleName.Normalize` = trim + invariant lowercase, same as email normalization) and are
  unique by normalized value. Permission codes follow the `Resource.Action` convention and
  are declared only in the centralized `PermissionCatalog` — never from client input.
- **No default role at registration.** The business requirements do not define one, and
  public registration must never assign privileges. Registration stays role-less and its
  command shape (email + password only) is guarded by a test.
- **Bootstrap without accounts.** Seeding creates the catalog permissions, one
  `Administrator` role holding the full catalog, and the grants — no user, no credential.
  The bootstrap is idempotent by construction and commits atomically; the `SeederExecutor`
  runs it once per database in Development.
- **Management use cases exist but are not endpoints.** `CreateRole`,
  `AssignRoleToUser`, `RemoveRoleFromUser`, `GetUserRoles`, `GetRolePermissions`,
  `GetUserPermissions` are application-ready, MediatR-discovered operations. Without an
  authorization-enforcement layer (Sprint 08), exposing them as HTTP endpoints would leave
  privileged operations unprotected.
- **JWT stays minimal (subject only).** Effective permissions are resolved per request from
  the live database via a single-query resolution (`Permission → RolePermissions → Role →
  UserRoles → User`), so role changes apply immediately, tokens never carry stale
  authorization state, and access tokens remain small. This follows the "minimal claims"
  decision of ADR-015.
- Referential actions: deleting an assigned role or a granted permission is restricted;
  deleting a user cascades to assignments. Authorization state can never be orphaned, and no
  PostgreSQL multiple-cascade-path is introduced.
- The `Permission` suffix is the reserved CA1711 suffix (associated with
  `System.Security.IPermission`); because "permission" is the domain term throughout the
  authorization model, `.editorconfig` allows that single suffix
  (`dotnet_code_quality.ca1711.allowed_suffixes = Permission`) with an explanatory comment.

## Consequences

- The full authorization graph exists in the database with uniqueness, referential
  integrity and single-query resolution — the foundation for Sprint 08 policy enforcement.
- There is no live way to assign roles yet (no endpoint), so the seeded `Administrator` role
  is inert until a protected provisioning flow exists; this is deliberate.
- Access tokens do not reflect role changes mid-flight (stateless JWTs); the next
  resolution reads the new state. Real-time access-token revocation is out of scope.
- Future sprints can add roles/permissions for the other modules without schema changes, and
  can assign a default role transactionally at registration if a business rule requires it.
