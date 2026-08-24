# ADR-028: Multi-Channel External Notification Delivery Architecture

## Status
Accepted

## Date
2026-08-20

## Context
Following the completion of Notification aggregate read-state and query lifecycle features (ADR-027), the **LastHour Backend** requires an external notification delivery architecture to support dispatching notifications across multiple external channels (In-App, Email, SMS, and future Push) without coupling domain and application business logic to third-party providers.

Critical requirements:
1. Multi-channel delivery capability (`InApp`, `Email`, `Sms`, `Push`).
2. Strict decoupled architecture: domain event consumers persist notifications and delivery intents; background processors handle external channel dispatch asynchronously.
3. Provider-neutral abstractions: third-party provider SDKs (SendGrid, Twilio, AWS SNS, SMTP, Firebase) are encapsulated strictly inside Infrastructure adapters.
4. Channel delivery state machine with leasing, bounded exponential retry backoff, and poison message containment.
5. Strict recipient privacy protection (zero PII in metric tags, structured log parameters, or exception tracebacks).
6. Deterministic, network-independent local development and test execution.

## Decisions

### 1. Delivery Architecture Pipeline & Separation of Concerns
Notification delivery follows a strict, asynchronous 8-stage pipeline:
`Domain Event` $\rightarrow$ `Transactional Outbox` $\rightarrow$ `Domain Event Consumer` $\rightarrow$ `Notification Aggregate persisted` $\rightarrow$ `Notification Delivery intents persisted` $\rightarrow$ `NotificationDeliveryProcessor` $\rightarrow$ `Delivery Channel Resolution` $\rightarrow$ `External Provider Adapter`.

- The `Notification` aggregate represents the user-visible notification in-app record.
- The `NotificationDelivery` aggregate root represents channel-specific delivery lifecycle state.
- Notifications and NotificationDeliveries are created atomically in the same database unit of work inside consumer event handlers.

### 2. Channel Resolution Policy (`INotificationDeliveryPolicyResolver`)
- `INotificationDeliveryPolicyResolver` determines which channels are required for a notification.
- Security-critical events (`UserPasswordChanged`, `UserLockedOut`) resolve both `InApp` and `Email` channels.
- Standard merchant events (`MerchantCreated`, `MerchantStatusChanged`, `MerchantProfileUpdated`) and user registration (`UserRegistered`) resolve `InApp` delivery.

### 3. Channel Delivery State Machine (`NotificationDelivery`)
- States: `Pending` $\rightarrow$ `Processing` $\rightarrow$ `Delivered` / `RetryScheduled` $\rightarrow$ `FailedPermanent`.
- Concurrency Protection: Worker leasing via `ClaimToken` and `LockExpiresAtUtc` prevents duplicate node dispatch.
- Indexing: Composite unique index on `(NotificationId, Channel)` prevents duplicate delivery intents for the same notification channel.
- Indexing for Polling: Composite index on `(Status, NextAttemptAtUtc)` for efficient background polling.
- Indexing for Leasing: Composite index on `(Status, LockExpiresAtUtc)` for stale lease reclamation.

### 4. Provider-Neutral Channel Adapters (`INotificationChannelSender`)
- Application contract: `INotificationChannelSender` with `DeliveryChannel Channel` and `Task<Result> SendAsync(...)`.
- Infrastructure adapters:
  - `InAppNotificationChannelSender`: Deterministic completion without external network calls.
  - `EmailNotificationChannelSender`: Provider-neutral Email channel adapter with PII-safe logging.
  - `SmsNotificationChannelSender`: Provider-neutral SMS channel adapter with PII-safe logging.
- `INotificationChannelFactory` resolves appropriate channel senders dynamically.

### 5. Background Processor (`NotificationDeliveryProcessor`)
- Polling background service executing leased worker loops.
- Bounded exponential backoff delay calculation: `min(BaseRetryDelay * 2^(retryCount), MaximumRetryDelay)`.
- Graceful cancellation: `OperationCanceledException` re-thrown during host shutdown without counting as failure or mutating delivery state.
- Failure classification: `NotificationDeliveryFailureClassifier` classifies errors into low-cardinality failure types (`Database`, `Concurrency`, `Validation`, `Transient`, `Cancellation`, `Unknown`).

### 6. Recipient Data Privacy & Observability
- Privacy Invariant: Zero PII (email addresses, phone numbers, recipient IDs, tokens, event IDs) included in metric tags, log event parameters, or exception messages.
- OpenTelemetry Metrics (`LastHour.NotificationDelivery`):
  - Counters: `processed`, `delivered`, `failed`, `retried`, `abandoned`, `reclaimed`, `claim_conflict`.
  - Histogram: `processing_duration` (ms).
  - Low-cardinality tags: `channel`, `failure_type`.

### 7. Distributed Failure Window and Exactly-Once Limitations
- **Distributed Systems Reality**: Guaranteed cross-database and external API physical exactly-once delivery is mathematically impossible without two-phase commit protocols across external provider APIs (SendGrid, Twilio, AWS SNS, etc.).
- **Failure Window Analysis**:
  - In the event of a worker crash or database failure immediately following external provider message acceptance, local state cannot transition to `Delivered` in the database.
  - Subsequent lease reclamation will re-poll the pending delivery and trigger a retry attempt.
- **Mitigation & Guarantees**:
  - **At-Least-Once Internal Processing**: Delivery attempts poll eligible items reliably.
  - **Stable Provider Idempotency Key**: Every delivery attempt passes the stable `DeliveryId` (`delivery.Id`) as the provider's `Idempotency-Key` header, allowing supporting external providers to deduplicate requests.
  - **Optimistic Concurrency Protection**: `ClaimToken` configured as a concurrency token (`.IsConcurrencyToken()`) in EF Core prevents stale workers from overwriting reclaimed or completed deliveries.
  - **Monotonic Retry State & Bounded Backoff**: `RetryCount` strictly increments up to `MaxRetryCount` before transitioning to terminal `FailedPermanent`.
  - **Safe Host Cancellation**: `OperationCanceledException` re-thrown during shutdown leaves delivery state intact without recording false failures.


## Consequences

### Positive
- Domain and Application layers have zero knowledge of third-party email or SMS SDKs.
- System is fully operational and testable offline without external network dependencies or API keys.
- Delivery failures never roll back notification creation or domain event processing.
- Security notifications automatically support multi-channel dispatch.
- Zero PII leakage in observability infrastructure.

### Neutral
- Requires executing EF Core migration `20260820070000_AddNotificationDeliveries.cs`.
- Adds `notification_deliveries` database table and index overhead.
