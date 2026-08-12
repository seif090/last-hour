# Refresh Tokens & Session Foundation

Sprint 05 adds refresh-token rotation and session management on top of the
Identity (Sprint 01), Authentication (Sprint 02), Registration (Sprint 03) and
Login + JWT (Sprint 04) foundations. This document describes the token
architecture, storage strategy, rotation semantics, session behavior and the
security properties of the implementation.

## Architecture

```
LOGIN (POST /api/v1/auth/login)
  │  credentials verified → JWT access token + refresh token issued
  ▼
ACCESS TOKEN EXPIRES (15 min)
  │
  ▼
REFRESH (POST /api/v1/auth/refresh { refreshToken })
  │  1. hash submitted token → look up record by hash
  │  2. active? not revoked? not expired? account still Active?
  │  3. revoke old token (reason: Rotation, link to successor)
  │  4. insert successor (same family, new secret, new hash)
  │  5. atomic commit — at most one active token per family (DB unique index)
  ▼
new access token + new refresh token  (old token is dead)
```

Every refresh rotates the pair: the old refresh token is revoked and can never
be used again, and the family keeps a single active token at any instant.

## Endpoints

| Endpoint | Purpose | Auth |
|----------|---------|------|
| `POST /api/v1/auth/login` | Credential login; now issues access + refresh token | anonymous |
| `POST /api/v1/auth/refresh` | Atomic rotation of a refresh token into a new pair | anonymous (the refresh token is the credential) |
| `POST /api/v1/auth/logout` | Revokes the presented refresh token's session (idempotent) | anonymous (the refresh token is the credential) |
| `GET /api/v1/auth/me` | Echoes the authenticated user id (Sprint 04) | Bearer required |

All three auth endpoints return the same safe `TokenResponse` shape:

```json
{ "accessToken": "...", "refreshToken": "...", "tokenType": "Bearer", "expiresIn": 900 }
```

Logout returns `204 No Content`. Every failure — invalid, revoked, expired,
reused or account-unavailable token — returns the identical `401` problem
details payload (`code: "InvalidRefreshToken"`), so the endpoints cannot be
used to probe token state. A structurally invalid request (missing/oversized
token) is a `400`; a lost concurrent rotation race is a `409`.

## Token security model

- **Generation:** raw secrets are 256 bits from `RandomNumberGenerator`
  (CSPRNG), base64url-encoded without padding — 43 characters. No sequential
  ids, timestamps or user data are ever part of the secret.
- **Storage:** the database stores only `SHA-256(rawToken)` (lowercase hex,
  64 chars). The raw token exists only in the client's hands and cannot be
  recovered from the database. Hashing is centralized in
  `RefreshTokenService` (`IRefreshTokenService` in the module Application).
- **Why SHA-256 and not Argon2id:** refresh tokens are random high-entropy
  secrets (256 bits), not human-chosen passwords, so a fast deterministic hash
  is the right tool — it provides lookup by hash and safe comparison without
  the cost of a memory-hard KDF. Passwords keep Argon2id (ADR-014); the two
  credential types are deliberately hashed differently.
- **Lookup:** the submitted token is hashed and the record is found by the
  unique `TokenHash` index — the raw token is never used in a query and never
  stored.

## Session model

- A **family** is one login session: the id of its first token. Every rotated
  successor keeps the family id.
- **Multi-session:** every login creates a fresh family, so a user can have
  several independent sessions (desktop, mobile, browser) at once. Logging out
  of one session revokes only that family.
- **Logout semantics:** logout revokes the presented refresh credential only.
  Because JWT access tokens are stateless, an already-issued access token
  remains valid until it expires (≤ 15 minutes). "Logout all" and
  password-change invalidation are future work; the family model is designed so
  a future operation can revoke every family of a user in one query.
- **Reuse detection:** presenting an already-rotated token is treated as
  possible credential compromise — the entire family is revoked
  (`ReuseDetected`) and a security event is logged. Even a freshly rotated
  successor stops working after reuse is detected. The family revocation is the
  complete security response, so it is committed even though the command itself
  fails.

## Rotation concurrency

Two simultaneous refreshes with the same token cannot both succeed:

- the rotation (revoke old + insert successor) is one atomic `SaveChanges`
  under the pipeline's unit of work;
- a **partial unique index** — `UNIQUE (FamilyId) WHERE RevokedAtUtc IS NULL` —
  guarantees at most one active token per family at the database level. The
  losing request's successor insert violates it and fails safely (`409`
  `ResourceConflict` via `TransactionBehavior`), leaving the family with
  exactly one active replacement chain.

The race is covered by an API test that fires three concurrent refreshes with
the same token and asserts exactly one `200` and a single active token.

## Configuration

```json
"RefreshToken": {
  "Lifetime": "14.00:00:00"
}
```

- `RefreshToken:Lifetime` defaults to 14 days — long enough to outlive many
  short access tokens, short enough to bound the exposure of a stolen secret.
  Startup validation rejects non-positive lifetimes and anything over 90 days.
- No secrets belong in this section. Entropy, hashing scheme and rotation
  rules are fixed security properties and are deliberately not configurable.

## Database schema

Migration `20260812010634_AddRefreshTokens` creates the `refresh_tokens` table:

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `uuid` | primary key (strongly typed `RefreshTokenId`) |
| `UserId` | `uuid` | FK → `users.Id`, cascade delete |
| `TokenHash` | `varchar(64)` | SHA-256 hex; unique index |
| `FamilyId` | `uuid` | session family identifier |
| `CreatedAtUtc` / `ExpiresAtUtc` | `timestamptz` | lifetime enforcement |
| `RevokedAtUtc` | `timestamptz?` | null while active |
| `ReplacedByTokenId` | `uuid?` | rotation link to successor |
| `RevocationReason` | `varchar(20)?` | `Logout` / `Rotation` / `ReuseDetected` / `SecurityEvent` |

Indexes are limited to what the queries need: unique `TokenHash` (lookup),
partial unique `FamilyId WHERE RevokedAtUtc IS NULL` (single-active-family
invariant), and `UserId` (future per-user revocation / cleanup).

Revocation reasons are a closed set: `Rotation`, `Logout`, `ReuseDetected`,
`SecurityEvent`, `PasswordChanged` (Sprint 06) and `AllSessionsRevoked`
(Sprint 06). No other values exist.

## Cleanup & retention

Expired and revoked tokens are retained for auditability — revocation is
security evidence and is never deleted immediately. Automated cleanup (a
scheduled job deleting records past their lifetime plus a retention window) is
**not implemented**; it is future infrastructure work. The schema (revocation
timestamps, unique lookups) is designed so a cleanup job can be added without
changes.

## Security logging

Events logged (none contain secrets):

- login success / login failure (Sprint 04)
- refresh success (`2003`), refresh rejection with reason category (`2004`)
- reuse detection (`2005`, includes the user id)
- session ended (`2006`)

Never logged: raw refresh tokens, access tokens, token hashes, authorization
headers, passwords, signing keys. Prefer event category + user id + trace id.

## Account status semantics

The session outlives a single login, so a refresh re-checks the account: if
the user is gone or no longer `Active`, the presented token is revoked
(`SecurityEvent`) and the request fails with the same `401` as every other
rejection. Only `Active` accounts can refresh.

## Testing strategy

- **Unit (module):** token service (CSPRNG uniqueness, 43-char format, hash
  determinism, no raw token in the hash), login command (issues refresh token,
  persists hash not raw token), refresh command (rotation, revocation,
  expiration, unknown token, account gating, reuse → family revocation), logout
  command (idempotency, unknown token), options validation.
- **Infrastructure:** `TransactionBehavior` translates the unique-violation
  race into a safe `409`.
- **API:** login now returns a refresh token; refresh (valid, missing, unknown,
  revoked, expired, reused, malformed JSON, simultaneous race), logout (valid,
  unknown, already revoked — always `204`), multi-session independence (logout
  of one session leaves the other refreshable), and full Sprint 04 JWT
  regression (valid / expired / wrong-key / wrong-issuer / wrong-audience /
  missing tokens).
- Integration tests run against per-test SQLite databases (the repository's
  established strategy); the concurrency behavior is additionally guaranteed
  structurally by the partial unique index.

## Session control (Sprint 06)

- `POST /api/v1/auth/logout-all` revokes every active session of the
  authenticated user (`AllSessionsRevoked` reason).
- A password change (`POST /api/v1/auth/change-password`) revokes every active
  session (`PasswordChanged` reason).
- `GET /api/v1/auth/sessions` lists the caller's sessions with safe metadata
  only.

See docs/account-security.md for the full Sprint 06 behavior.

## NOT IMPLEMENTED YET

- Token-family revocation UI / admin session management
- Scheduled cleanup of expired/revoked tokens
- OAuth / Google login / MFA / password reset / email verification
- Roles / permissions / fine-grained authorization
- Cross-device notification on new login
