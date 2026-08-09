# ADR-008: Architecture boundaries enforced by tests

**Status:** Accepted

**Context:** Layer rules (who may reference whom) protect the modular monolith
from coupling. Convention-only enforcement rots: one pragmatic reference
becomes many, and the architecture silently drifts.

**Decision:** `LastHour.BuildingBlocks.Architecture.Tests` enforces two
complementary checks that fail the build:

1. **NetArchTest rules** over the loaded `src` assemblies — layer boundaries
   (SharedKernel references nothing, Application never references
   Infrastructure, Domain code never references EF/Npgsql/Redis, modules never
   reference Infrastructure, the API is the only composition root).
2. **Project-reference graph tests** that parse every `.csproj` and assert the
   exact allowed reference set for each project.

**Consequences:** A boundary crossing is a build error, reviewable and
actionable at the point of change. Adding a project means updating the graph
expectations deliberately. The tests double as living documentation of the
dependency contract.
