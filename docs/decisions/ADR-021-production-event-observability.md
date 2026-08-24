# ADR-021: Production event observability, failure classification & operational diagnostics

**Status:** Accepted

**Context:** The asynchronous event processing pipeline (Outbox $\rightarrow$ Dispatcher $\rightarrow$ Consumer) requires continuous operational visibility to detect backlogs, lease contention, duplicate surges, poison message abandonment, and processing latency without incurring high metric cardinality or performance penalties.

**Decision:**
1. Standardize metric contracts on OpenTelemetry Meters `LastHour.Outbox` and `LastHour.ConsumerIdempotency`.
2. Enforce strict low-cardinality dimensions: `event_type`, `consumer`, and `failure_type`. Never include high-cardinality values (`EventId`, user IDs, exception strings) in metric tags.
3. Introduce deterministic, O(1) exception classification via `FailureClassifier` (`Cancellation`, `Database`, `Concurrency`, `Validation`, `Infrastructure`, `Transient`, `Unknown`).
4. Instrument tracing using `ActivitySource` on `"Outbox.Dispatch"` and `"Consumer.Handle"`.
5. Provide dedicated health checks (`OutboxHealthCheck`, `ConsumerInboxHealthCheck`) and an aggregated diagnostics query interface (`IEventProcessingDiagnostics`).

**Consequences:**
- Full end-to-end visibility into outbox and consumer lifecycles.
- Fast alerting on stalled leases, poison message accumulation, and elevated latency.
- Zero high-cardinality memory leaks or metric exporter overload.
- Graceful cancellation remains cleanly excluded from failure telemetry.
