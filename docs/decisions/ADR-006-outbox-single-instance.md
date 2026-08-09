# ADR-006: Single-instance outbox with at-least-once delivery

**Status:** Accepted

**Context:** The outbox processor must not dispatch the same message twice
concurrently, and the hardening sprint deliberately ships one API instance
behind a reverse proxy. Later scale-out would run multiple processor instances
against the same outbox table.

**Decision:** For now the `OutboxProcessor` runs in-process as a singleton
hosted service with a strictly sequential loop, so a single instance never
overlaps cycles. The event-delivery contract is at-least-once. No row-leasing
or claim-tracking mechanism is implemented.

**Consequences:** Correct and simple today. When the platform scales to
multiple instances, this ADR must be revisited: row leases (e.g. an
`attempts`/`locked_until` scheme) are required to prevent duplicate dispatch
and double processing of the same batch. Event handlers should already be
idempotent under the at-least-once contract.
