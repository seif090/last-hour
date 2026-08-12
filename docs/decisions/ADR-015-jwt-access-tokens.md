# ADR-015 — JWT access tokens for authentication

Status: Accepted

## Context

Sprint 04 must add credential login and authenticated access to the LastHour
API on top of the Argon2id credential foundation (ADR-014). Requirements: a
short-lived access token, minimal claims, strict validation, a signing key that
is never committed, enumeration-safe failures, and no refresh tokens or token
revocation in this sprint.

## Decision

- **Token type:** short-lived JWT access tokens signed with HMAC-SHA256 (HS256).
  Default lifetime 15 minutes (configurable via `Jwt:AccessTokenLifetime`,
  capped at 24 h and validated at startup).
- **Library:** `Microsoft.IdentityModel.JsonWebTokens` 8.19.2 for issuance
  (Infrastructure) and the framework's `Microsoft.AspNetCore.Authentication.JwtBearer`
  9.0.18 for validation (API). Both resolve the IdentityModel 8.19.x line, so
  issuance and validation never drift.
- **Claims:** `sub` (user id), `jti` (unique token id for future revocation /
  audit correlation), `iat`/`nbf`/`exp` and the configured `iss`/`aud`. No email,
  roles, permissions, security state or personal data in the token.
- **Signing key:** resolved exclusively from the secrets surface
  (`LASTHOUR_SECRET_JWT_SIGNINGKEY` in production, user-secrets in development);
  appsettings declares an empty key and startup validation fails fast rather
  than falling back to an ephemeral or unsigned key.
- **Validation:** the JWT bearer handler validates issuer, audience, lifetime,
  and signature, with a deliberately small clock skew (default 1 minute,
  capped at 15). Inbound claims are left unmapped (`MapInboundClaims = false`)
  so endpoints read the raw `sub` claim.
- **Enumeration safety:** every authentication failure — unknown account, wrong
  password, pending/suspended/disabled account — surfaces the identical 401
  problem details payload, and unknown-account attempts verify a dummy
  credential so response timing does not reveal account existence.
- **Login is read-only:** modeled as an `IQuery`; failed-attempt counters and
  lockout remain deferred (per ADR-014) until a sprint that can exercise their
  concurrency semantics.

## Consequences

- The API is protected by framework-standard JWT bearer authentication with no
  custom token-parsing middleware; adding `[Authorize]`/`RequireAuthorization`
  anywhere now gates on a valid access token.
- A missing signing key makes the host fail at startup — an intentional
  fail-safe that requires deployment tooling to inject the secret.
- Tokens are stateless and cannot be individually revoked (no server-side
  session); refresh tokens, rotation, revocation and logout are future sprints.
- The `jti` claim gives future sprints a stable identifier to build revocation
  against without re-issuing tokens.
