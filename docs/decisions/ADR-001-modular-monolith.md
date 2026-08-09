# ADR-001: Modular monolith over microservices

**Status:** Accepted

**Context:** The LastHour platform spans several business domains (Identity,
Catalog, Ordering, Payments, ...). A microservices split would have created
operational complexity — distributed transactions, network failures, per-service
deployment — before the product has even one business feature. A single
monolithic project would have coupled everything together.

**Decision:** Build a modular monolith: one deployment unit, one database, and
code organized into module projects behind enforced dependency boundaries.
Each module is a separate project that may only reference the allowed layers,
so a future extraction of any module into a service is mechanical.

**Consequences:** Team coordination cost is lower than microservices; all
modules share one lifecycle. The enforcement of module boundaries is a
deliberate, automated concern: architecture tests fail the build when a module
crosses a boundary, so the monolith cannot silently erode into a big ball of
mud.
