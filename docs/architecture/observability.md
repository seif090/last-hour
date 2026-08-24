# Production Event Observability & Operational Reliability

This document outlines the observability, metrics, structured logging, tracing, health checks, and operational diagnostics contract for the **LastHour Backend** event processing pipeline.

---

## 1. Observability Architecture

The event processing pipeline exposes full observability across every stage of the lifecycle:

```
[Domain Event]
      ↓
[Transactional Outbox] → Metrics: last_hour.outbox.*, Tracing: "Outbox.Dispatch"
      ↓
[OutboxProcessor] → Heartbeats, Retries, Exponential Backoff, Dead-Letter (Abandoned)
      ↓
[EventDispatcher] → Envelope into NotificationMessage<T>
      ↓
[Consumer Handler] → Metrics: last_hour.consumer.*, Tracing: "Consumer.Handle"
      ↓
[IConsumerIdempotencyService] → Lease Acquisition, Lease Reclaim, Duplicate Suppression
      ↓
[Health Checks & Diagnostics] → OutboxHealthCheck, ConsumerInboxHealthCheck, EventProcessingDiagnostics
```

---

## 2. Metrics Contract

All metrics adhere to low-cardinality, production-safe OpenTelemetry standards.

### Outbox Metrics (`Meter: LastHour.Outbox`)
| Metric Name | Instrument | Unit | Dimensions | Description |
|---|---|---|---|---|
| `last_hour.outbox.messages.processed` | Counter | `{message}` | `event_type` | Number of outbox messages successfully dispatched. |
| `last_hour.outbox.messages.failed` | Counter | `{attempt}` | `event_type`, `failure_type` | Number of failed processing attempts. |
| `last_hour.outbox.messages.retried` | Counter | `{attempt}` | `event_type`, `failure_type` | Number of messages scheduled for subsequent retry. |
| `last_hour.outbox.messages.abandoned` | Counter | `{message}` | `event_type`, `failure_type` | Number of poison messages abandoned after exhausting retries. |
| `last_hour.outbox.messages.reclaimed` | Counter | `{message}` | `event_type` | Number of expired processing leases reclaimed. |
| `last_hour.outbox.messages.requeued` | Counter | `{message}` | `event_type` | Number of abandoned messages manually re-queued. |
| `last_hour.outbox.claims.conflicts` | Counter | `{conflict}` | (none) | Number of atomic claim race conflicts. |
| `last_hour.outbox.processing.duration` | Histogram | `ms` | `event_type` | Processing latency from claim to commit. |
| `last_hour.outbox.delivery.latency` | Histogram | `ms` | `event_type` | End-to-end duration from event occurrence to commit. |

### Consumer Metrics (`Meter: LastHour.ConsumerIdempotency`)
| Metric Name | Instrument | Unit | Dimensions | Description |
|---|---|---|---|---|
| `last_hour.consumer.processed` | Counter | `{event}` | `consumer` | Number of events successfully processed and committed by consumer. |
| `last_hour.consumer.duplicate` | Counter | `{event}` | `consumer` | Number of duplicate event deliveries suppressed. |
| `last_hour.consumer.failed` | Counter | `{attempt}` | `consumer`, `failure_type` | Number of failed consumer execution attempts. |
| `last_hour.consumer.lease_acquired` | Counter | `{lease}` | `consumer` | Number of initial consumer processing leases acquired. |
| `last_hour.consumer.lease_reclaimed` | Counter | `{lease}` | `consumer` | Number of expired consumer processing leases reclaimed. |
| `last_hour.consumer.processing_duration` | Histogram | `ms` | `consumer` | Execution latency of consumer handler delegate. |

---

## 3. Failure Classification

Failures are categorized into a bounded, low-cardinality set of categories using `FailureClassifier`:
- **`Cancellation`**: Graceful shutdown or explicit task cancellation.
- **`Database`**: Relational database errors (connection, execution, constraints).
- **`Concurrency`**: Optimistic concurrency token mismatches (`DbUpdateConcurrencyException`).
- **`Validation`**: Input format or domain validation errors (`ArgumentException`, `ValidationException`).
- **`Infrastructure`**: Network, HTTP, or socket communication errors.
- **`Transient`**: Timeouts or transient deadlocks.
- **`Unknown`**: Unclassified exceptions.

---

## 4. Structured Logging Standards

All logging uses high-performance compile-time `LoggerMessage.Define`:
- **Allowed Structured Fields**: `{EventId}`, `{Consumer}`, `{EventType}`, `{OutboxMessageId}`, `{ElapsedMilliseconds}`, `{RetryCount}`, `{NextAttemptAtUtc}`, `{FailureType}`, `{PreviousLockExpiresAtUtc}`.
- **Prohibited in Logs**: Sensitive credentials, passwords, JWT tokens, connection strings, unmasked PII.

---

## 5. Health Checks & Diagnostics

### Health Checks
- **`outbox`** (`OutboxHealthCheck`): Evaluates processor liveness (`processor_is_running`, `last_heartbeat_utc`), ready pending backlog count, oldest message age, and poison message accumulation.
- **`consumer_inbox`** (`ConsumerInboxHealthCheck`): Inspects stalled expired leases (`CompletedOnUtc == null && LockExpiresAtUtc < now`) to detect dead consumers or stalled executions.

### Operational Diagnostics
Inject `IEventProcessingDiagnostics` to obtain point-in-time aggregate operational snapshots:
- `GetOutboxSnapshotAsync()`: Returns pending, ready, retrying, active processing, and abandoned counts along with processor heartbeat status.
- `GetConsumerInboxSnapshotAsync()`: Returns total inbox, completed, active lease, and expired lease counts.
