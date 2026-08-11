# ADR-012: Security by default with configuration-driven hardening

**Status:** Accepted

**Context:** The API exposes infrastructure endpoints before any Identity
module exists. The foundation must not ship insecure defaults that later
sprints would inherit, while staying runnable in local development without a
proxy or TLS certificate.

**Decision:** Security is layered and configuration-driven, with startup
validation (`ValidateOnStart` + `IValidateOptions` on every options surface)
so misconfiguration fails fast instead of misbehaving silently:

- **Secrets** never live in files: the `Secrets` mechanism overlays
  `LASTHOUR_SECRET_*` environment variables onto configuration (appsettings
  holds local-dev placeholders only; `appsettings.Development.json` is
  gitignored).
- **Headers/HSTS**: `SecurityHeadersMiddleware` emits
  `X-Content-Type-Options`, CSP, `Referrer-Policy`, `Permissions-Policy`, and
  HSTS (`max-age` in seconds, `includeSubDomains` configurable, preload off by
  default) over HTTPS only.
- **CORS**: explicit `AllowedOrigins` list; the validator rejects
  `AllowAnyOrigin` with credentials in non-Development environments.
- **Rate limiting**: named policies (global fixed-window, per-IP sliding for
  system endpoints) validated at startup, 429 + `Retry-After` via ProblemDetails.
- **Exceptions**: sanitized ProblemDetails everywhere; diagnostics only in
  Development.
- **Forwarded headers**: `KnownProxies`/`KnownNetworks` config-driven;
  loopback trusted by default.
- **Swagger/OpenAPI**: Development-only; the JWT bearer scheme in the OpenAPI
  document is inert scaffolding for the Identity sprint.
- **Request limits**: configurable body size, multipart, header and keep-alive
  timeouts.

**Consequences:** Production operators must supply environment variables
(secrets, CORS origins, forwarded-header networks, HSTS policy) — documented
in `docs/CONFIGURATION.md` and validated at startup. The API stays runnable in
Development without TLS. No claim of absolute security: HTTPS termination,
certificates, and network policies remain deployment responsibilities.
