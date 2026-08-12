# Authentication Foundation

The authentication foundation (Sprint 02) provides secure credential management:
password hashing and verification with Argon2id, a configurable password policy,
the application-level credential service, and the authentication-result
abstraction. Login and JWT access tokens are implemented (Sprint 04 — see
docs/login-jwt.md) and registration is implemented (Sprint 03 — see
docs/registration.md); this sprint built the machinery those flows consume.

## Layering

```
API (composition root)
 └─ Identity module Application: PasswordPolicy, PasswordCredentialService,
    AuthenticationResult            (LastHour.Modules.Identity/Application)
      └─ BuildingBlocks Application: IPasswordHasher, PasswordVerificationResult
           └─ BuildingBlocks Infrastructure: Argon2PasswordHasher,
              PasswordHashingOptions        (…/Infrastructure/Security)
                └─ Isopoh.Cryptography.Argon2 (Argon2id)
```

- The **domain** (`User`, `PasswordCredential`, `Email`, `AccountStatus`) has no
  dependency on hashing, EF Core, HTTP or ASP.NET Core Identity.
- The **application layer** depends only on the `IPasswordHasher` contract — never
  on the concrete provider.
- The **infrastructure layer** owns the hashing implementation and its options.

## Password hashing

- **Algorithm:** Argon2id (version 1.3), via the mature, maintained
  [Isopoh.Cryptography.Argon2](https://github.com/mheyman/Isopoh.Cryptography.Argon2)
  (v2.0.0) — a managed implementation, no native dependencies.
- **Hash format:** PHC string, self-describing:
  `$argon2id$v=19$m=65536,t=3,p=4$<salt>$<hash>`. The salt and derived key are
  embedded, so verification needs no separate stored metadata and parameters can
  be raised over time.
- **Salt:** 16 random bytes per hash from `RandomNumberGenerator` (CSPRNG). Two
  hashes of the same password are always different.
- **No custom crypto:** nothing is implemented by hand; MD5/SHA1/SHA2 and plain
  encryption are never used for passwords.

### Parameters (server-side defaults)

| Parameter | Value | Meaning |
|-----------|-------|---------|
| `TimeCost` (`t`) | 3 | passes over memory |
| `MemoryCost` (`m`) | 65536 KiB = 64 MiB | memory budget |
| `Lanes` (`p`) | 4 | parallel lanes |
| `Threads` | 4 | worker threads |
| `HashLength` | 32 bytes | derived key length |
| `SaltLength` | 16 bytes | random salt |

Configured in the `PasswordHashing` appsettings section, validated at startup
(`IValidateOptions` + `ValidateOnStart`; minimums: t ≥ 1, m ≥ 8192 KiB, lanes ≥
1, salt ≥ 16 bytes, hash 16–64 bytes). Raising parameters later is safe: the
rehash strategy (below) upgrades stored hashes transparently.

Measured cost on a typical developer machine: ≈ 0.4 s per hash+verify at the
defaults — the intended server-side budget. Tunable per environment.

## Password verification

`IPasswordHasher.Verify(password, hash)` returns `PasswordVerificationResult`:

- `Failed` — password does not match, **or** the stored hash is malformed or
  unsupported. Malformed hashes are swallowed (never thrown, never logged with
  internals) and treated exactly like a wrong password.
- `Success` — matched, current parameters.
- `SuccessRehashNeeded` — matched, but the stored hash uses weaker parameters or
  a different Argon2 flavor (e.g. an old argon2i hash). The caller may re-hash
  and store the upgrade after successful authentication.

Verification runs inside the hashing library, which performs constant-time
comparisons; wrong-password attempts do not throw.

## Password policy

`PasswordPolicy` (appsettings section `PasswordPolicy`), validated at startup:

- `MinimumLength`: 12 (OWASP recommendation).
- `MaximumLength`: 128.

Deliberately **no composition rules** (uppercase/lowercase/digit/symbol) — modern
guidance rejects them in favor of a strong minimum length. Passwords are never
trimmed, lower-cased or otherwise normalized; they are verified exactly as
entered (spaces are valid characters). A null/empty/whitespace-only input is
rejected as `Identity.PasswordRequired`. Length is measured in UTF-16 code units
(the same units the hasher receives).

Policy violations return `Result` errors with stable codes
(`Identity.PasswordTooShort`, `Identity.PasswordTooLong`,
`Identity.PasswordRequired`) — no exceptions for ordinary invalid input, and no
password value ever appears in an error message.

## Credential service

`IPasswordCredentialService` (`LastHour.Modules.Identity/Application`):

- `HashPassword(password)` → `PasswordCredential` (opaque hash only).
- `VerifyPassword(password, credential)` → `PasswordVerificationResult`.
- `ValidatePassword(password)` → `Result` against the policy.
- `ChangePassword(user, currentPassword, newPassword)` → `Result`: verifies the
  current password (`Identity.InvalidCurrentPassword`), validates the new one,
  hashes it, and calls `user.SetPasswordCredential(...)` (bumping `Version`).
  The caller owns persistence via the unit of work; the service is deliberately
  free of repository and HTTP dependencies.

No password value ever leaves these methods — only hashes. Nothing here
generates tokens, sends mail or touches roles.

## Authentication result

`AuthenticationResult` (module Application) models an authentication attempt:

- `Success()`
- `Failure(AuthenticationFailureReason)` where the reason is one of
  `InvalidCredentials`, `AccountPending`, `AccountSuspended`, `AccountDisabled`.

The application may distinguish these internally; the API layer decides
what is safe to expose. **Account enumeration protection:** the login flow
(Sprint 04) responds identically for "unknown email" and "wrong password"
(`InvalidCredentials`) and never reveals account state to unauthenticated
callers — every failure surfaces the same public 401 (docs/login-jwt.md).
`AccountLocked` is an internal reason only — the public payload stays identical.

## Account status semantics (authentication view)

Only `Active` accounts authenticate. `Pending` accounts (identity not yet
established) fail with `AccountPending`; `Suspended` and `Disabled` fail with
their reasons. There is no `Locked` status: temporary lockout is time-boxed
authentication behavior tracked separately on the aggregate
(`FailedAccessCount`/`LockoutUntilUtc`, Sprint 06) and kept separate from
permanent account state by design.

## Failed-attempt tracking, lockout, security stamp

**Implemented (Sprint 06, docs/account-security.md, ADR-017).** Failed
authentication attempts increment `FailedAccessCount`; reaching the configured
threshold sets a time-boxed `LockoutUntilUtc`; success, lockout expiry or a
password change resets the throttle. Counter updates run through the shared
`ITrackedRepository` (tracked load) and commit explicitly with a bounded
concurrency retry, so simultaneous failures cannot lose increments or bypass
the threshold.

A **security stamp is still not needed**: password changes and logout-all
revoke the actual refresh-token sessions explicitly, which is a stronger
mechanism than a stamp-based invalidation. The optimistic `Version` token
remains the aggregate's credential-versioning primitive.

## User lookup

`IUserRepository` (Sprint 01) exposes `GetByEmailAsync(Email)` and
`ExistsByEmailAsync(Email)` — the normalized-email lookups sign-in needs.
Implemented by `UserRepository` (module Application, Sprint 03), composed purely
from the generic `IRepository<User>` / `IReadRepository<User, UserId>`
implementations; the module needs no Infrastructure reference.

## Rehash strategy

After a successful verification returning `SuccessRehashNeeded`, a future login
flow re-hashes the entered password and stores the result (guarded by the same
`Version` concurrency token). This upgrades credentials whenever the configured
parameters are raised. Nothing in this sprint mutates hashes automatically.

## Security logging

The login flow (Sprint 04) logs authentication events through
`LoginQueryHandler`: success (user id) and rejection (result category only) via
`LoggerMessage` templates. The policy: event type, user id when safely
available, timestamp, trace id, result category — **never** passwords, hashes,
tokens, Authorization headers, cookies or raw emails. The existing
request-logging pipeline (Sprint 0.7) already redacts sensitive headers and
bodies.

## Configuration summary

| Section | Purpose | Validated |
|---------|---------|-----------|
| `PasswordHashing` | Argon2id parameters | `PasswordHashingOptionsValidator` |
| `PasswordPolicy` | min/max length | `PasswordPolicyValidator` |

Both fail fast at startup. No secrets are stored in either section; hashing
parameters are never exposed through API responses.

## Dependency injection

- `IPasswordHasher` → `Argon2PasswordHasher` (**singleton** — stateless and
  thread-safe per the library's contract).
- `IPasswordCredentialService` → `PasswordCredentialService` (**scoped**).
- Options with `ValidateOnStart`, registered by the infrastructure and module DI
  extensions; the API composition root calls `AddLastHourIdentity(configuration)`.

## NOT IMPLEMENTED YET (future sprints)

- Login endpoint and JWT access tokens: **implemented** (Sprint 04, docs/login-jwt.md).
- Registration: **implemented** (Sprint 03, docs/registration.md).
- Refresh tokens, token rotation, logout, revocation: **implemented** (Sprint 05, docs/refresh-tokens.md).
- Email verification.
- Password reset.
- MFA / OAuth / external providers.
- Security-stamp-based token invalidation (session revocation is explicit, Sprint 06).
- Roles, permissions, authorization policies.
- Brute-force-specific rate-limit policies (the global and per-IP limits from
  Sprint 0.4 already apply; endpoint-specific policies are future work).

## Login integration (realized in Sprint 04)

- `IUserRepository.GetByEmailAsync` → normalized-email sign-in lookup (`LoginQueryHandler`).
- `IPasswordCredentialService.VerifyPassword` → password check; a future login
  flow maps `SuccessRehashNeeded` to a transparent hash upgrade (not yet done).
- `AccountStatus` → the authentication gate (only `Active`).
- `AuthenticationResult` → the internal outcome; every failure maps to one safe 401.
- `IPasswordCredentialService.ChangePassword` → the password-change flow (no endpoint yet).
- `Version` → optimistic concurrency and a future security-stamp source.
- Rate limiting + `AuthenticationResult` → enumeration-safe, throttled login.
