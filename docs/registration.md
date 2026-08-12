# Registration

The registration foundation (Sprint 03) turns the Sprint 01/02 identity and credential
foundations into a usable, safe sign-up flow: a CQRS command, its validator, the use-case
handler, the concrete user repository, and the `POST /api/v1/auth/register` endpoint.

## Flow

```
HTTP POST /api/v1/auth/register
  → RegistrationEndpoints (thin orchestration: bind, dispatch, map)
    → CQRS pipeline (MediatR)
      → ValidationBehavior      (FluentValidation: required fields, email length)
      → RegisterUserCommandHandler
          1. Email.From(...)            — domain normalization + format validation
          2. ValidatePassword(...)      — password policy (Sprint 02, single source)
          3. ExistsByEmailAsync(...)    — duplicate pre-check (UX layer)
          4. User.Create(email, hash)   — aggregate, starts Pending
          5. _users.Add(user)
      → TransactionBehavior             — commits atomically; unique-violation → 409
    → 201 Created with { userId, status } or RFC 7807 problem details
```

The API never hashes passwords, never touches EF entities, and contains no business rules.
The handler never sees a repository implementation — it depends on the module's
`IUserRepository` contract, which is satisfied by a composition adapter over the shared
repository contracts.

## Command and DTOs

- `RegisterUserCommand` (module Application) — only `Email` and `Password`. No identifier,
  hash, status or timestamps are accepted; all are server/domain-owned.
- `RegisterUserRequest` / `RegisterUserResponse` (API endpoint module) — the request carries
  only client-controlled fields; the response carries only `userId` and `status`. Passwords,
  hashes and security state never appear in any response.

## Validation

- **FluentValidation** (`RegisterUserCommandValidator`): email required + ≤ 254 characters,
  password required. Runs in the shared `ValidationBehavior`, returning 400 with the per-field
  codes in the problem details `errors` dictionary.
- **Domain/policy validation** (handler): `Email.From` enforces format and length through the
  domain's single rule; `IPasswordCredentialService.ValidatePassword` enforces the Sprint 02
  policy. Both are 400s with stable codes (`InvalidEmail`, `Identity.PasswordTooShort`, …).
  Nothing is duplicated between the layers.

## Email normalization

Registration never normalizes independently: `Email.From(...)` and `Email.Normalize(...)` are
the only rules, exactly as Sprint 01 established. `User@Example.com` and `user@example.com`
are the same account.

## Password handling

The plaintext password is validated, passed directly to `IPasswordCredentialService.HashPassword`
(Argon2id, Sprint 02), and only the resulting hash is stored via `PasswordCredential`. The
plaintext is never logged, returned or persisted.

## Duplicate protection — two layers

1. **Application pre-check**: `IUserRepository.ExistsByEmailAsync` runs before creation and
   returns `Identity.EmailAlreadyRegistered` (409) for the common case.
2. **Database unique index** on `NormalizedEmail` (`IX_users_normalized_email`): the
   correctness backstop under concurrency. When two registrations race and the loser reaches
   the insert, `TransactionBehavior` detects the unique-constraint violation during the commit
   and returns a safe 409 (`ResourceConflict`) — no raw PostgreSQL/SQLite exception, no
   constraint name, no SQL, no stack trace ever reaches the client.

## Concurrency and transaction strategy

- Registration is a single atomic insert; the existing `TransactionBehavior` commits it — no
  new transaction abstraction was introduced.
- `ICommand<TValue>` (new in the Application CQRS contracts) lets value-returning commands
  (`RegisterUserCommand → Result<UserId>`) flow through the same transaction behavior as
  value-less commands.
- The concurrent-registration race test starts four registrations with the same normalized
  email: exactly one succeeds; every loser fails as a 409 conflict; the database holds exactly
  one user.

## Initial account status

Accounts start `Pending` (Sprint 01 semantics): usable only after identity is established
(e.g. verification). No email-verification workflow exists yet — the status model already
supports the future `Pending → Active` transition.

## HTTP contract

- `POST /api/v1/auth/register` (versioned minimal endpoint, tag `Auth`, rate-limited by the
  global policy).
- `201 Created` with `{ "userId": "<guid>", "status": "Pending" }`. No Location header —
  there is no user GET resource, and none was invented for it.
- `400 Bad Request` — validation failures (missing fields, invalid email, weak password).
- `400 Bad Request` — malformed JSON bodies (handled via `BadHttpRequestException`, now
  answered with its own status instead of a 500).
- `409 Conflict` — duplicate normalized email.
- `500` — unexpected server errors, generic in production (existing exception handler).

All failures are RFC 7807 problem details with the `code` extension and correlation id;
stack traces and connection strings are never exposed.

## Logging

Registration events are logged with `LoggerMessage` templates and never include the password,
the hash, the Authorization header or the raw email: success logs the user id and status;
duplicate attempts log only the category. Trace/correlation ids are attached by the existing
logging pipeline.

## Repository wiring

`UserRepository` (module Application) implements `IUserRepository` by composing the shared
`IRepository<User>` and `IReadRepository<User, UserId>`. To support the email lookups,
`IReadRepository<TEntity, TId>` gained predicate-based `AnyAsync` and `FirstOrDefaultAsync`
implemented by `EfReadRepository` as no-tracking queries. This completes the Sprint 01/02
deferral: the login sprint's `GetByEmailAsync` lookup now has a working implementation.

## Security considerations

- Two-layer duplicate protection; the database wins under concurrency.
- Enumeration: registration responses reveal only whether *your own* email is taken (409);
  no other account data (id, status, hash) is ever exposed.
- Malformed stored credentials and constraint violations are never surfaced as internal
  details.
- No CAPTCHA, no anti-bot services (future concern, rate limiting exists from Sprint 0.4).

## NOT IMPLEMENTED YET (future sprints)

- Login and JWT access tokens: **implemented** (Sprint 04, docs/login-jwt.md).
- Refresh tokens, logout, token rotation: **implemented** (Sprint 05, docs/refresh-tokens.md).
- Email verification workflow and email sending.
- Password reset.
- MFA / OAuth / external providers.
- Roles, permissions, authorization policies.
- CAPTCHA or registration-specific rate-limit policies.
