# ADR-005: Strongly typed identifiers

**Status:** Accepted

**Context:** Module entities keyed by raw `Guid`/`int` invite type confusion:
passing a catalog id where an order id is expected compiles, and a bug surfaces
only at runtime.

**Decision:** Identifiers derive from `StronglyTypedId<TValue>` (`GuidId`,
`IntId`) and implement `IStronglyTypedId`. EF value converters
(`StronglyTypedIdValueConverter`) map them to `uuid`/`integer` columns, and
JSON converters serialize them as their primitive value. `IRepository<>` and
`IReadRepository<,>` constrain their identity types to these patterns.

**Consequences:** Id types are distinct at compile time and still persist,
query, and serialize naturally. The infrastructure cost (converters) is paid
once in the Infrastructure layer; modules consume ids as value types.
