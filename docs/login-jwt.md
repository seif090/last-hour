# Login & JWT Authentication

Sprint 04 adds credential login and JWT access-token authentication on top of
the Identity (Sprint 01), Authentication (Sprint 02) and Registration
(Sprint 03) foundations. This document describes the login flow, the token
design, configuration, and the security properties of the implementation.

## Login flow

```
POST /api/v1/auth/login { email, password }
        │
        ▼
LoginQuery (IQuery, module Application)          ── no state is mutated
        │  1. Email.From(...)                     ── domain normalization, single rule
        │  2. IUserRepository.GetByEmailAsync     ── lookup by normalized email
        │  3. password verification (Argon2id)    ── Sprint 02 credential service
        │  4. account status gate                 ── only Active authenticates
        │  5. IAccessTokenGenerator.Generate      ── JWT issuance (Infrastructure)
        ▼
Result<LoginResult>  { accessToken, expiresAtUtc }
        │
        ▼
200 OK { accessToken, tokenType: "Bearer", expiresIn }
```

The API layer never queries the database, verifies passwords or generates
tokens — it binds the request, dispatches the use case and maps the result onto
the HTTP surface.

### Endpoints

| Endpoint | Purpose | Auth |
|----------|---------|------|
| `POST /api/v1/auth/register` | Account creation (Sprint 03) | anonymous |
| `POST /api/v1/auth/login` | Credential login, returns an access token | anonymous |
| `POST /api/v1/auth/refresh` | Rotates a refresh token into a new pair (Sprint 05) | anonymous (refresh token) |
| `POST /api/v1/auth/logout` | Revokes the presented session (Sprint 05) | anonymous (refresh token) |
| `GET /api/v1/auth/me` | Echoes the authenticated user id from the token | Bearer required |

`POST /api/v1/auth/login` now also returns a `refreshToken`; see
[refresh-tokens.md](refresh-tokens.md) for the full Sprint 05 lifecycle.

`/me` exists solely to verify authenticated access — it exposes the
authenticated subject's identifier and nothing else. It is an infrastructure
verification endpoint, not a profile feature.

## JWT design

- **Algorithm:** HS256 (HMAC-SHA256) with a symmetric key.
- **Lifetime:** 15 minutes by default (`Jwt:AccessTokenLifetime`), deliberately
  short. Configurable; startup validation rejects lifetimes beyond 24 hours.
- **Claims (minimal by design):**

| Claim | Value | Purpose |
|-------|-------|---------|
| `sub` | user id (GUID) | the stable subject identifier; what `/me` and future `[Authorize]` code read |
| `jti` | random GUID | unique token id — future revocation / audit correlation |
| `iat` | issue time | standard timestamp |
| `nbf` | not-before time | standard timestamp |
| `exp` | expiration | enforced by validation |
| `iss` / `aud` | configured issuer / audience | enforced by validation |

  Deliberately **not** in the token: email (personally identifying data),
  password material, hashes, security state, roles and permissions. The
  roles/permissions foundation (Sprint 07, docs/roles-permissions.md) resolves the
  **live** effective permission set per request instead, so role changes take effect
  immediately and tokens never carry stale authorization state.

## Configuration

```json
"Jwt": {
  "Issuer": "https://lasthour.dev",
  "Audience": "lasthour-api",
  "SigningKey": "",
  "AccessTokenLifetime": "00:15:00",
  "ClockSkew": "00:01:00"
}
```

- `Jwt:SigningKey` is **never** committed. It is resolved from the secrets
  surface (`Secrets:Names` includes `Jwt:SigningKey`):
  - production / Docker: the `LASTHOUR_SECRET_JWT_SIGNINGKEY` environment
    variable (the existing `LASTHOUR_SECRET_` prefix maps `:` to `_`);
  - local development: `dotnet user-secrets set "Jwt:SigningKey" "<value>"` in
    `src/Api/LastHour.Api`.
- Startup validation fails fast when the key is missing or shorter than
  32 characters, when the lifetime is non-positive or over 24 h, or when the
  clock skew is negative or over 15 minutes. There is **no** silent fallback to
  a generated or unsigned key.

### Local development setup

```bash
cd src/Api/LastHour.Api
dotnet user-secrets set "Jwt:SigningKey" "dev-only-key-<32+ random chars>"
dotnet run
```

Without a key the host refuses to start — that is the intended fail-safe.

## Validation settings (JWT bearer)

`AddLastHourAuthentication` (API composition root) registers the framework's
JWT bearer handler with strict `TokenValidationParameters`:

- `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`,
  `ValidateIssuerSigningKey` — all enabled;
- `ClockSkew` = 1 minute by default (deliberately small; capped at 15);
- `MapInboundClaims = false` — endpoints read the raw `sub` claim
  (`JwtRegisteredClaimNames.Sub`), never the legacy `ClaimTypes` names;
- no lifetime or signature validation is ever disabled.

Issuance and validation are configured from the same validated `JwtOptions`, so
the two cannot drift apart. Middleware order in `Program.cs`:
`UseRouting → UseAuthentication → UseAuthorization → rate limiting → endpoints`.

## Security properties

- **Enumeration safety:** every login failure — unknown account, wrong
  password, pending, suspended or disabled account — returns the identical
  `401` problem details payload (`code: "AuthenticationFailed"`). The internal
  reason is only logged.
- **Timing equalization:** when no account exists (or no credential is stored),
  the submitted password is verified against a dummy Argon2id credential so the
  request costs the same as a genuine verification; response timing does not
  reveal account existence.
- **No secrets in transit or at rest beyond the hash:** passwords are never
  logged, returned or persisted; hashes and tokens are never logged; security
  state is never exposed through API responses.
- **Rate limiting:** the existing global fixed-window policy covers the login
  endpoint. An authentication-specific policy is future work.
- **Malformed stored hashes** are treated as failed verifications, never
  thrown, so a corrupted credential cannot crash authentication.

## Testing strategy

- **Unit (module):** login use case — success, wrong password, unknown user
  (with the timing-equalization assertion), unavailable accounts, malformed
  email, uniform failure payload across all causes.
- **Unit (infrastructure):** token generation (minimal claims, expiry matching
  the configured lifetime, unique `jti`, no sensitive claims) and `JwtOptions`
  startup validation.
- **API:** the login endpoint over the full HTTP → pipeline → database → token
  path (valid, wrong password, unknown user, unavailable accounts, malformed
  input) and the `/me` endpoint (valid token, missing / malformed / expired /
  wrong-key / wrong-issuer / wrong-audience / unsigned tokens all rejected
  with `401`).
- Tests seed users directly through the domain aggregate (registration
  creates `Pending` accounts, which cannot authenticate yet), so the full
  login path is exercised without an activation endpoint.

## Account status semantics

Only `Active` accounts authenticate. `Pending` (created, not yet verified),
`Suspended` and `Disabled` accounts receive the same generic `401` as invalid
credentials — the password is still verified first, so the response cannot be
used to probe account state. Registration always creates `Pending` accounts;
activating them (verification) is a future sprint, so **no account can log in
through the public API yet** until an activation path exists.

## NOT IMPLEMENTED YET

- Refresh tokens / rotation / logout: **implemented** (Sprint 05, docs/refresh-tokens.md).
- Token revocation of already-issued access tokens (the `jti` claim is the future anchor for it)
- Roles / permissions / authorization policies beyond "authenticated"
- OAuth / Google / Facebook login
- MFA
- Password reset
- Email verification / account activation workflow
- Failed-attempt counters and lockout: **implemented** (Sprint 06, docs/account-security.md, ADR-017)
