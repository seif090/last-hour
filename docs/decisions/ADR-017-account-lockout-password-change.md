# ADR-017 — Failed-Attempt Lockout and Password-Change Session Revocation

- **Status:** Accepted
- **Decided by:** Sprint 06 — Account Security & Session Control
- **Related:** ADR-014 (Argon2id password hashing), ADR-016 (refresh-token rotation)

## Context

Login (Sprint 04) had no throttle, so credentials could be guessed without
limit, and password changes could not invalidate outstanding sessions. Sprint
06 must protect the credential with a lockout and make a password change take
effect everywhere at once, without weakening the enumeration-safe login design.

## Decision

- Failed authentication attempts (wrong password on an existing account)
  increment `FailedAccessCount` on the `User` aggregate. Reaching the
  configured threshold (`AccountSecurity:MaxFailedAttempts`, default 5) sets
  `LockoutUntilUtc` (default 15 minutes); the account is denied even with the
  correct password until the window elapses. Lockout is **time-boxed and
  separate from `AccountStatus`** — it never permanently disables an account
  and is never surfaced as a status.
- Success resets the counter and lifts the lockout; an expired lockout is
  treated as finished (the next failure starts fresh); a password change also
  resets the throttle.
- The counter update is applied through a **tracked load**
  (`ITrackedRepository`), preserving the optimistic-concurrency original
  `Version` for the update predicate, and is **committed explicitly** because
  the login command itself fails (the pipeline only commits successes). A
  bounded retry (3) reloads on concurrency conflicts so simultaneous failures
  cannot lose increments or bypass the threshold.
- The public login response stays a single uniform `401` for every failure
  class; lockout state exists only in persistence, and locked accounts still
  pay a dummy Argon2id verification for uniform timing.
- A password change verifies the current password, rejects an identical
  password (verified through the hasher, never by plaintext comparison),
  validates against the policy, re-hashes with Argon2id, records
  `LastPasswordChangedAtUtc`, resets the throttle, and **revokes every active
  refresh token** (`PasswordChanged` reason). Already-issued access tokens
  remain valid until expiry (stateless JWTs, ADR-015).
- Logout-all revokes every active refresh token of the authenticated user only
  (identity from token claims, `AllSessionsRevoked` reason); session
  inspection exposes only safe metadata.
- Security events remain structured logs (event ids 2001–2010); no
  `SecurityEvents` table, because the Serilog/audit infrastructure already
  provides retention-managed, searchable event storage.

## Consequences

- Credential guessing is throttled deterministically, even under concurrency,
  without any response that reveals lockout state.
- Password changes invalidate every refresh credential immediately; the cost
  is that the changing client must re-authenticate (the safest behavior the
  model supports).
- The `ITrackedRepository` contract joins the shared repository family,
  giving any aggregate a concurrency-safe mutation path (its original-values
  semantics are what make the throttle updates correct).
- Lockout is configuration-driven and validated at startup; no security values
  are scattered through application code.
- Access tokens are never instantly revocable (stateless JWTs) — a documented
  limitation anchored on the future `jti`-based revocation.
