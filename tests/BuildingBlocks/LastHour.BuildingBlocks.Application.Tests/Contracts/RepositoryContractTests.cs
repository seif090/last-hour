using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.SharedKernel.Domain;

namespace LastHour.BuildingBlocks.Application.Tests.Contracts;

public class RepositoryContractTests
{
    [Fact]
    public void IRepository_RequiresAggregateRootConstraint()
    {
        Type aggregateParameter = typeof(IRepository<>).GetGenericArguments()[0];

        Assert.Contains(typeof(IAggregateRoot), aggregateParameter.GetGenericParameterConstraints());
    }

    [Fact]
    public void IReadRepository_RequiresEntityOfTConstraint()
    {
        Type entityParameter = typeof(IReadRepository<,>).GetGenericArguments()[0];

        Assert.Contains(
            entityParameter.GetGenericParameterConstraints(),
            constraint => constraint.GetGenericTypeDefinition() == typeof(IEntity<>));
    }

    [Fact]
    public void IReadRepository_RequiresComparableIdConstraint()
    {
        Type idParameter = typeof(IReadRepository<,>).GetGenericArguments()[1];

        Assert.Contains(
            idParameter.GetGenericParameterConstraints(),
            constraint => constraint.GetGenericTypeDefinition() == typeof(IEquatable<>));
    }
}
