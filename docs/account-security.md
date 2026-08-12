# Account Security & Session Control

Sprint 06 adds account lockout, password change and session control on top of
the Identity (Sprint 01), Authentication (Sprint 02), Registration (Sprint 03),
Login + JWT (Sprint 04) and Refresh Tokens (Sprint 05) foundations. This
document describes the lockout policy, the password-change flow, session
revocation semantics and the security properties of the implementation.

## Failed-login tracking & lockout

Every failed authentication attempt (wrong password) against an existing
account increments the account's `FailedAccessCount`. When the count reaches
the configured threshold the account enters a **temporary lockout**:
`LockoutUntilUtc` is set to now plus the lockout duration, and login is denied
even with the correct password until the window elapses.

- **Configuration** (`AccountSecurity` section, validated at startup):

```json
"AccountSecurity": {
  "MaxFailedAttempts": 5,
  "LockoutDuration": "00:15:00"
}
```

- **Reset semantics:** a successful authentication resets the counter and
  lifts the lockout. A lockout that has already expired is treated as finished
  — the next failure starts counting fresh instead of extending the old
  window. A password change also resets the throttle.
- **What counts:** only credential failures (wrong password) on existing
  accounts. Unknown accounts have nothing to track; pending/suspended/disabled
  accounts reject *after* a correct verification and are not counted, because
  their unavailability is a lifecycle matter, not a guessing attack. A locked
  account rejects further attempts without incrementing further.
- **Enumeration safety is preserved:** every failure — wrong password, unknown
  account, pending, suspended, disabled or locked — returns the identical
  `401` payload (`code: "AuthenticationFailed"`). The lockout exists only in
  persisted security state, never in the response. Unknown and locked accounts
  still pay a dummy Argon2id verification so response timing stays uniform.
- **Concurrency:** counter updates run through a *tracked* load (the shared
  `ITrackedRepository`), so the optimistic-concurrency token (`Version`)
  guards the write. A failed-login counter update commits explicitly even
  though the login command itself fails (the pipeline only commits successful
  commands), and a bounded retry (3 attempts) re-reads the latest state after
  a concurrency conflict — so simultaneous failed logins cannot lose
  increments or bypass the threshold. Covered by an API test firing five
  concurrent failures: all five land, the account locks, and the failure
  responses stay uniform 401s.

Lockout is deliberately **time-boxed and separate from `AccountStatus`**: it
never permanently disables an account, and it is never surfaced to clients as
a status.

## Password change

```
POST /api/v1/auth/change-password      [Bearer required]
{ "currentPassword": "...", "newPassword": "..." }
→ 204 No Content
```

The authenticated user's identity comes from the validated access-token
`sub` claim — never from the request body. The flow:

1. verify the current password (wrong → `400 Identity.InvalidCurrentPassword`);
2. reject a change to the identical password (verified through the hasher,
   never by plaintext comparison → `400 Identity.PasswordUnchanged`);
3. validate the new password against the Sprint 02 policy (length-based;
   `400 Identity.PasswordTooShort` / `PasswordTooLong`);
4. hash the new password (Argon2id, Sprint 02) and replace the credential;
5. record `LastPasswordChangedAtUtc` and reset the authentication throttle;
6. **revoke every active session** of the account (`PasswordChanged` reason).

Because JWT access tokens are stateless, already-issued access tokens remain
valid until they expire (≤ 15 minutes); the client must re-authenticate to get
new tokens. This is the safest behavior the model supports: after a password
change, no refresh credential anywhere in the system can mint new tokens, and
the change takes effect everywhere at once.

## Session control

| Endpoint | Purpose | Auth |
|----------|---------|------|
| `POST /api/v1/auth/logout` | Ends the presented session (Sprint 05) | refresh token |
| `POST /api/v1/auth/logout-all` | Ends every session of the caller (`AllSessionsRevoked` reason) | Bearer required |
| `GET /api/v1/auth/sessions` | Lists the caller's sessions with safe metadata | Bearer required |

- **Logout-all** revokes every active refresh token of the authenticated user
  only — the identity comes from the token claims, so no other account can
  ever be affected (covered by a test with two users). It is idempotent.
- **Session inspection** returns `sessionId`, `createdAtUtc`, `expiresAtUtc`
  and `active` for every session of the caller — never hashes, family ids,
  revocation reasons or any other internal state. There is no device metadata
  to show because the session model deliberately stores none. The current
  session cannot be marked as such yet: the access token carries no session
  identifier, and adding one is deferred to keep the Sprint 04 minimal-claims
  decision intact.
- **JWT limitation:** like logout, logout-all and password change revoke
  *refresh credentials* only. Already-issued access tokens stay valid until
  expiry — there is no server-side access-token revocation (stateless JWTs;
  the `jti` claim is the future anchor).

## Security events

Security events are recorded as **structured logs** (`LoggerMessage` event ids
2001–2010), not persisted in a database table: the repository's Serilog +
audit-logging infrastructure already provides searchable, retention-managed
event storage, and a `SecurityEvents` table would duplicate it. Event ids in
use: login success (2001), login rejection with reason category (2002), refresh
success (2003), refresh rejection (2004), reuse detected (2005), session ended
(2006), account locked (2007), counter-update retries exhausted (2008),
password changed (2009), all sessions ended (2010). Never logged: passwords,
password hashes, raw refresh tokens, access tokens, authorization headers,
signing keys.

## User-ID claim resolution

All authenticated endpoints resolve the user via `ClaimsPrincipalExtensions
.GetUserId()` (API `Security/Authentication`) from the raw `sub` claim — the
single, centralized rule. Request bodies and query parameters never carry
identity.

## Database changes

Migration `20260812014158_AddAccountSecurity` adds three columns to `users`
(additive only, no data migration): `FailedAccessCount` (`integer`, default
0), `LockoutUntilUtc` (`timestamptz?`), `LastPasswordChangedAtUtc`
(`timestamptz?`). The optimistic-concurrency `Version` token is unchanged and
now also guards the throttle updates.

## Testing strategy

- **Domain:** counter increments, threshold → lockout window, expired lockout
  restarts fresh, success resets, password change resets, invalid threshold
  rejected.
- **Module (handler):** wrong password increments and commits despite the
  failed result; threshold reached locks; locked account rejects the correct
  password without counting; success resets; expired lockout allows login;
  password change (success, wrong current, weak new, identical new, unknown
  user, sessions revoked with `PasswordChanged`); logout-all (own sessions
  only, idempotent); sessions query (safe metadata only, own sessions only).
- **Pipeline (real EF):** the full lockout sequence through the CQRS pipeline
  and SQLite — tracked counter updates and the lockout window against real
  persistence.
- **API:** change-password (valid, wrong current, weak new, identical new,
  unauthenticated, session invalidation), logout-all (own-only, idempotent,
  unauthenticated), sessions (safe metadata, own-only, unauthenticated),
  lockout (repeated failures → locked with uniform 401s, concurrent failures
  → every increment lands and the threshold holds).
- **JWT regression:** the full Sprint 04/05 suite is unchanged and green.

## NOT IMPLEMENTED YET

- MFA
- Password reset
- OAuth / external providers
- Roles / permissions / fine-grained authorization
- Security-stamp-based token invalidation (session revocation is explicit;
  a stamp is unnecessary for the current model)
- Marking the current session in the sessions list (needs a `sid` claim)
- Admin session management / cross-user session control
- Suspicious-login detection / device notifications
