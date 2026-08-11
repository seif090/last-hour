# Identity

The Identity module (`src/Modules/Identity/LastHour.Modules.Identity`) owns the user
account model of the LastHour platform. This document describes the foundation
established in Sprint 01. Authentication behavior — hashing, sign-in, tokens,
verification, lockout — is **not implemented** and is called out as such below.

## User aggregate

`User` (`Domain/Users/User.cs`) is the identity aggregate root. It is deliberately
small and guarantees only identity-state invariants:

- a valid, normalized email address (`Email` value object),
- a controlled account-status lifecycle (`AccountStatus`),
- an immutable identifier (`UserId`),
- a password credential that exists only as an opaque hash (`PasswordCredential`),
- an optimistic-concurrency token (`Version`).

`User` implements `IAuditableEntity` (audit stamps via the shared interceptor) and
inherits domain-event support from the SharedKernel entity base. It does **not**
implement `ISoftDelete`: account lifecycle is expressed through status
(`Disabled` is the permanent terminal state), and hard deletion is not a use case.
Future authentication behavior is added in later sprints, not to this aggregate.

### Domain behavior

| Operation | Rule |
|-----------|------|
| `Create(email, passwordCredential?)` | New account starts **Pending**; id is generated. |
| `Activate()` | `Pending → Active`, `Suspended → Active` (reinstatement). |
| `Suspend()` | `Active → Suspended`. |
| `Disable()` | `Pending/Active/Suspended → Disabled` (terminal). |
| `ChangeEmail(newEmail)` | Rejected for `Disabled` accounts and for the same normalized address. |
| `SetPasswordCredential(credential)` | Stores the opaque hash; hashing happens outside the aggregate. |

Every mutation increments `Version` (optimistic concurrency, see below). Invalid
status transitions throw `BusinessRuleValidationException` (the repository's
established business-rule error); invalid email input throws
`InvalidEmailException` (a domain invariant violation).

## User ID

`UserId` wraps a `Guid` (RFC 4122) and derives from the SharedKernel
`StronglyTypedId<Guid, UserId>` — the same strategy as every other entity in the
platform (ADR-005). It is immutable, persists as a PostgreSQL `uuid` column
through the shared value converters, and is a distinct type from other ids so a
user id can never be passed where another entity id is expected. No integer ids
are introduced.

## Email strategy

- **Value object** `Email` (`Domain/Users/Email.cs`) owns validation and
  normalization; there is exactly one rule and it cannot be bypassed.
- **Validation**: non-empty, at most 254 characters (RFC 5321), syntactically
  valid (no whitespace; parsed as an RFC 5322 address).
- **Original value** is preserved for display (trimmed, case kept).
- **Normalized value** is `Trim().ToLowerInvariant()` — the single deterministic
  rule, exposed as `Email.NormalizedValue` and `Email.Normalize(...)`.
  `User@Example.com` and `user@example.com` are the same account.
- **Uniqueness** is enforced by a database unique index on the stored normalized
  column (`IX_users_normalized_email`), not by application-level existence
  checks: concurrent double-registration is rejected by PostgreSQL.

## Username

**NOT REQUIRED.** No business requirement defines usernames; the email address is
the login identifier. No username column or uniqueness logic exists. If a later
sprint introduces usernames (for example public display names), it will be an
additive migration with its own normalization decision.

## Account status

`AccountStatus`: `Pending` → `Active` ↔ `Suspended` → `Disabled` (terminal).

- `Pending`: created, not yet usable (e.g. address not verified).
- `Active`: can authenticate.
- `Suspended`: temporarily restricted; reversible to `Active`.
- `Disabled`: permanently deactivated.

Temporary, time-boxed lockout (failed sign-in throttling) is **not** part of the
status model — it is authentication behavior for a future sprint, kept separate
from permanent account state by design.

## Password abstraction

`PasswordCredential` (`Domain/Users/PasswordCredential.cs`) is a value object that
wraps the final hash representation only. The aggregate never sees plaintext
passwords or confirmation values.

**Password hashing is NOT implemented in this sprint.** A future authentication
sprint introduces the hashing scheme (Argon2id/bcrypt/PBKDF2 choice is an
upcoming decision) and constructs `PasswordCredential.FromHash(...)`. The column
(`PasswordHash`, varchar(512)) has headroom for any modern scheme and is never
exposed through API responses or logs.

## Security state

No `SecurityStamp`, lockout counters, or token state exist yet — those belong to
the authentication/authorization sprints and will be additive migrations. The
only security-adjacent state today is the credential hash and the concurrency
token.

## Persistence

- **Mapping**: `Persistence/UserConfiguration.cs` in the module
  (`IEntityTypeConfiguration<User>`); the shared `LastHourDbContext` discovers it
  via `ApplyConfigurationsFromAssembly` (the API composition root registers the
  module assembly).
- **Table**: `users`; columns follow the repository convention (PascalCase
  columns, snake_case table).
- **Indexes**: unique index on `NormalizedEmail` (the only index — no
  speculative indexes on other columns).
- **Concurrency**: `Version bigint` is an EF concurrency token. The aggregate
  increments it on every mutation; EF includes the original value in update
  predicates, so lost updates raise `DbUpdateConcurrencyException`. (An
  app-managed token was chosen over `xmin`/rowversion because it is provider
  agnostic and testable on SQLite.)
- **Audit**: `CreatedAt/CreatedBy/UpdatedAt/UpdatedBy` are stamped by the shared
  `AuditInterceptor`. `CreatedBy`/`UpdatedBy` remain null until authentication
  exists — no fake actor id is written.

## Repository contract

`IUserRepository : IRepository<User>` (`Domain/Users/IUserRepository.cs`) adds the
identity-specific lookups: `GetByEmailAsync(Email)` and `ExistsByEmailAsync(Email)`.
The contract takes `Email` so queries can only run against the normalized value.
No implementation exists yet — the first command handler sprint provides it
(against `IReadRepository`/the generic repository pattern).

## Migration

`20260811152933_AddIdentityUsers` creates the `users` table, its primary key, and
the unique normalized-email index. `Up()` is create-only; `Down()` drops the
table. Module migrations are generated from the API host (which references every
module) so the design-time factory discovers module entity configurations:

```bash
dotnet dotnet-ef migrations add <Name> \
  --project src/BuildingBlocks/LastHour.BuildingBlocks.Infrastructure/LastHour.BuildingBlocks.Infrastructure.csproj \
  --startup-project src/Api/LastHour.Api/LastHour.Api.csproj
```

## Not implemented (future sprints)

- Registration and sign-in (no endpoints exist; the aggregate stays internal).
- Password hashing/verification.
- JWT access tokens and refresh tokens.
- Email verification workflow.
- Password reset.
- MFA / OAuth / external providers.
- Roles, permissions, authorization policies.
- Account lockout and security stamps.
- Admin user-management endpoints.

## Security considerations

- No plaintext passwords, no fake hashes, no tokens, no secrets in the module.
- The password hash is persisted but never exposed through DTOs or logs; the
  request-logging policy (Sprint 0.7) never captures sensitive values.
- Email addresses are personally identifying data: they are not logged by the
  request/audit logging pipeline, and error responses never echo full addresses
  beyond standard validation messages.
- Privacy of the `users` table is a deployment concern: restrict database
  access to the application role (least privilege).

## Future authentication integration points

- `IUserRepository` gives handlers the normalized-email lookup for sign-in.
- `AccountStatus` gates authentication: only `Active` accounts authenticate.
- `PasswordCredential` accepts the future hashing scheme's output unchanged.
- `Version` protects concurrent updates (e.g. simultaneous profile and password
  changes) once write flows exist.
- `IAuditableEntity` records who created/modified accounts once `ICurrentUser`
  is populated by authentication.
