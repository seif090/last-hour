# ADR-016 — Refresh-Token Rotation with Family-Based Reuse Detection

- **Status:** Accepted
- **Decided by:** Sprint 05 — Refresh Tokens & Session Foundation
- **Related:** ADR-014 (Argon2id password hashing), ADR-015 (JWT access tokens)

## Context

Sprint 04 introduced short-lived JWT access tokens (15 min) with no way to
obtain a new one after expiry. A refresh-token credential is needed, and it is
a high-value secret: it must never be stored in plaintext, must rotate on
every use, and must fail safely when stolen or reused.

## Decision

- Raw refresh tokens are 256-bit CSPRNG secrets, base64url-encoded
  (43 characters). Only their SHA-256 hash is stored, in a `refresh_tokens`
  table with a unique hash index for lookup.
- Refresh tokens use **SHA-256**, not Argon2id: they are random high-entropy
  secrets, not human-chosen passwords, so a fast deterministic hash is
  appropriate and keeps the lookup cheap (password hashing remains Argon2id
  per ADR-014).
- Every successful refresh **rotates**: the old token is revoked (reason
  `Rotation`, linked to the successor) and a new token is inserted in the same
  atomic unit of work.
- Each session is a **token family** (the id of its first token). A partial
  unique index — `UNIQUE (FamilyId) WHERE RevokedAtUtc IS NULL` — guarantees at
  most one active token per family, which is what makes concurrent rotations
  safe: the loser of a simultaneous refresh race violates the index and fails
  with a safe `409`.
- Presenting an already-rotated token is treated as **reuse / possible
  compromise**: the entire family is revoked (`ReuseDetected`) and a security
  event is logged. The family revocation is committed explicitly because it is
  the complete security response even though the command itself fails.
- Logout revokes the presented session's family only. JWT access tokens remain
  valid until expiry (they are stateless by design, ADR-015).
- Multi-session support: each login creates a fresh family; sessions are
  independent.

## Consequences

- The raw token can never be recovered from the database; a database leak
  yields only unusable hashes.
- A stolen refresh token is useless after its first (attacker or legitimate)
  use — and its use is detected, revoking the whole session.
- Concurrency safety is structural (database invariant), not reliant on
  in-memory locks.
- SHA-256 hashing of high-entropy tokens is fast; the cost profile is dominated
  by the Argon2id password verification, as intended.
- Reuse detection revokes the session for the legitimate client too when a
  stolen token was used first — the accepted trade-off for compromise
  containment.
- Token-family revocation and per-user logout-all are enabled by the model but
  not yet exposed as endpoints (future work).
