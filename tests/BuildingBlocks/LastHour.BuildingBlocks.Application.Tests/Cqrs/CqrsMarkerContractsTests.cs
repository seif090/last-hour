using LastHour.BuildingBlocks.Application.Cqrs;
using LastHour.BuildingBlocks.SharedKernel.Results;
using MediatR;

namespace LastHour.BuildingBlocks.Application.Tests.Cqrs;

public class CqrsMarkerContractsTests
{
    [Fact]
    public void ICommand_DerivesFromMediatRRequest_OfResult()
    {
        Assert.True(typeof(IRequest<Result>).IsAssignableFrom(typeof(ICommand)));
    }

    [Fact]
    public void IQuery_DerivesFromMediatRRequest_OfResultOfT()
    {
        Assert.True(typeof(IRequest<Result<string>>).IsAssignableFrom(typeof(IQuery<string>)));
    }

    [Fact]
    public void ICommandHandler_DerivesFromMediatRRequestHandler_OfResult()
    {
        Assert.True(
            typeof(IRequestHandler<TestCommand, Result>).IsAssignableFrom(
                typeof(ICommandHandler<TestCommand>)));
    }

    [Fact]
    public void IQueryHandler_DerivesFromMediatRRequestHandler_OfResultOfT()
    {
        Assert.True(
            typeof(IRequestHandler<TestQuery, Result<string>>).IsAssignableFrom(
                typeof(IQueryHandler<TestQuery, string>)));
    }

    [Fact]
    public void ICommandHandler_Constraint_RequiresCommandDerivedType()
    {
        Type commandParameter = typeof(ICommandHandler<>).GetGenericArguments()[0];

        Assert.Contains(
            commandParameter.GetGenericParameterConstraints(),
            constraint => constraint == typeof(ICommand));
    }

    [Fact]
    public void IQueryHandler_Constraint_RequiresQueryDerivedType()
    {
        Type queryParameter = typeof(IQueryHandler<,>).GetGenericArguments()[0];

        Assert.Contains(
            queryParameter.GetGenericParameterConstraints(),
            constraint => constraint.GetGenericTypeDefinition() == typeof(IQuery<>));
    }

    private sealed class TestCommand : ICommand
    {
    }

    private sealed class TestQuery : IQuery<string>
    {
    }
}
