# ADR-007: Warnings-as-errors and analyzers as a quality gate

**Status:** Accepted

**Context:** A hardening sprint cannot trust discipline alone. Compiler
warnings, analyzer diagnostics, code-style violations, and formatting drift
have all previously surfaced as latent defects.

**Decision:** `Directory.Build.props` sets `TreatWarningsAsErrors=true`,
`EnableNETAnalyzers=true`, `AnalysisLevel=latest-recommended`,
`AnalysisMode=Recommended`, `EnforceCodeStyleInBuild=true`, and
`GenerateDocumentationFile=true` for every project. Exclusions are explicit
and minimal (`CS1591` missing XML docs, `CA1014` CLS compliance). CI runs
`dotnet format --verify-no-changes` and fails on any deviation.

**Consequences:** The build rejects new warnings, analyzer violations, and
style drift automatically — there is no separate review gate for them. The
cost is occasional friction (nullability annotations, style choices) and a
deliberate, documented `NoWarn` list that must never grow silently.
