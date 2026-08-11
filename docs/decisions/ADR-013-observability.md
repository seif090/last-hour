# ADR-013: Observability — Serilog, OpenTelemetry, single correlation id

**Status:** Accepted

**Context:** The API must be diagnosable in production (failures, slow
requests, database issues) without an external telemetry server being a hard
dependency, and without logging secrets. Multiple competing identifiers
(correlation ids, trace ids) would make joining logs impossible.

**Decision:**

- **Logging**: Serilog is the single pipeline, configured from the `Serilog`
  section (sinks, minimum levels, enrichments) so operators change behavior
  without redeploying. All application log statements use
  `LoggerMessage.Define` (stable `EventId`s, cached delegates). Console + file
  sinks in containers (writable `/app/logs`), Seq via configuration only.
- **Correlation**: one id per request — `X-Correlation-ID` accepted/generated,
  reusing the OpenTelemetry trace id when an activity exists. It flows through
  `HttpContext.Items`, the Serilog log context, ProblemDetails responses, and
  the OTel `correlation.id` tag.
- **Tracing/metrics**: OpenTelemetry instruments ASP.NET Core, HttpClient,
  EF Core and Npgsql. Exporters (console/OTLP) are configuration-driven and
  default **off**, so the API runs with zero external dependencies and
  degrades gracefully; `OpenTelemetry` section documents the surface.
- **Sensitive data**: request logging never captures bodies, query strings,
  or headers; exception logging happens internally with sanitized client
  responses.

**Consequences:** Logs, traces, and audit events join on one id. Operators opt
into exporters (Seq, OTLP) via configuration; nothing requires Grafana/
Prometheus/Jaeger at runtime. The `Microsoft` framework namespace is kept at
`Warning` in production to avoid SQL/HTTP noise, raised to `Information` in
Development only.
