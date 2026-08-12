# ADR-014 — Password hashing with Argon2id

Status: Accepted

## Context

The Identity module (Sprint 01) persisted an opaque `PasswordCredential` hash
but deferred the hashing scheme. Sprint 02 must choose a password-specific,
memory-hard algorithm and a mature implementation. Requirements: no custom
cryptography, no MD5/SHA-family password hashing, self-describing stored hashes
so parameters can evolve, and safe behavior for malformed stored data.

## Decision

- **Algorithm:** Argon2id (version 1.3), the memory-hard password hash
  recommended by OWASP and RFC 9106.
- **Library:** Isopoh.Cryptography.Argon2 2.0.0 — mature, maintained, pure
  managed code (no native dependencies), PHC-string output.
- **Hash format:** PHC string embedding algorithm, version, parameters, salt and
  derived key (`$argon2id$v=19$m=…,t=…,p=…$salt$hash`); no separate salt column.
- **Parameters:** m=65536 KiB, t=3, p=4, 32-byte key, 16-byte CSPRNG salt
  (configurable via the `PasswordHashing` section, validated at startup).
- **Abstraction:** `IPasswordHasher` (BuildingBlocks.Application) implemented by
  `Argon2PasswordHasher` (BuildingBlocks.Infrastructure); verification returns
  `PasswordVerificationResult` with `SuccessRehashNeeded` for stale hashes, and
  malformed stored hashes are treated as failed verifications, never thrown.
- **Policy:** length-only (minimum 12, maximum 128), no composition rules, no
  password normalization.
- **Deferred at the time:** failed-attempt counters, lockout, and security
  stamps were NOT part of this sprint. Lockout is now implemented (Sprint 06,
  docs/account-security.md, ADR-017); security stamps remain unnecessary because
  session revocation is explicit. The aggregate's optimistic `Version` token
  remains the credential-versioning primitive today.

## Consequences

- Users get a strong, auditable default hashing scheme; parameters can be
  raised per environment and existing hashes upgrade transparently after a
  successful verification (rehash strategy).
- Isopoh adds a small dependency to the infrastructure layer only; the domain
  and application layers depend solely on the `IPasswordHasher` contract.
- Managed Argon2id costs roughly 0.4 s per operation at the defaults — an
  intentional server-side budget that doubles as brute-force mitigation.
- Lockout/throttling is implemented (Sprint 06, ADR-017) and the Argon2id
  cost now also throttles guessing even when an account is locked (dummy
  verification).
