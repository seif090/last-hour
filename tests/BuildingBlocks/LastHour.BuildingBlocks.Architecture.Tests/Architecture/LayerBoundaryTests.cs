using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;

namespace LastHour.BuildingBlocks.Architecture.Tests.Architecture;

/// <summary>
/// Namespace-level boundary rules enforced with NetArchTest. These complement the
/// csproj reference-graph tests: even where a project reference is allowed, code
/// must not reach across architectural layers or pull framework concerns into the
/// domain. Modules are empty placeholders today, so the module rules guard the
/// future modular monolith rather than today's code.
/// </summary>
public class LayerBoundaryTests
{
    private static readonly Assembly[] SourceAssemblies = BuildSourceAssemblies();

    [Fact]
    public void SharedKernel_DoesNotDependOnAnyHigherLayer()
    {
        TestResult result = Types
            .InAssemblies(SourceAssemblies)
            .That()
            .ResideInNamespace("LastHour.BuildingBlocks.SharedKernel")
            .Should()
            .NotHaveDependencyOnAny(
                "LastHour.BuildingBlocks.Application",
                "LastHour.BuildingBlocks.Contracts",
                "LastHour.BuildingBlocks.Infrastructure",
                "LastHour.Api",
                "LastHour.Modules")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(DescribeFailures(result));
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureContractsOrHigherLayers()
    {
        TestResult result = Types
            .InAssemblies(SourceAssemblies)
            .That()
            .ResideInNamespace("LastHour.BuildingBlocks.Application")
            .Should()
            .NotHaveDependencyOnAny(
                "LastHour.BuildingBlocks.Contracts",
                "LastHour.BuildingBlocks.Infrastructure",
                "LastHour.Api",
                "LastHour.Modules")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(DescribeFailures(result));
    }

    [Fact]
    public void Contracts_DoesNotDependOnInfrastructureOrHigherLayers()
    {
        TestResult result = Types
            .InAssemblies(SourceAssemblies)
            .That()
            .ResideInNamespace("LastHour.BuildingBlocks.Contracts")
            .Should()
            .NotHaveDependencyOnAny(
                "LastHour.BuildingBlocks.Infrastructure",
                "LastHour.Api",
                "LastHour.Modules")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(DescribeFailures(result));
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApiOrModules()
    {
        TestResult result = Types
            .InAssemblies(SourceAssemblies)
            .That()
            .ResideInNamespace("LastHour.BuildingBlocks.Infrastructure")
            .Should()
            .NotHaveDependencyOnAny("LastHour.Api", "LastHour.Modules")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(DescribeFailures(result));
    }

    [Fact]
    public void DomainLayer_DoesNotUsePersistenceWebOrLoggingFrameworks()
    {
        string[] forbiddenFrameworks =
        [
            "Microsoft.EntityFrameworkCore",
            "Npgsql",
            "StackExchange.Redis",
            "Microsoft.AspNetCore",
            "Serilog",
        ];

        TestResult result = Types
            .InAssemblies(SourceAssemblies)
            .That()
            .ResideInNamespace("LastHour.BuildingBlocks.SharedKernel")
            .Or()
            .ResideInNamespace("LastHour.BuildingBlocks.Application")
            .Or()
            .ResideInNamespace("LastHour.BuildingBlocks.Contracts")
            .Should()
            .NotHaveDependencyOnAny(forbiddenFrameworks)
            .GetResult();

        result.IsSuccessful.Should().BeTrue(DescribeFailures(result));
    }

    [Fact]
    public void Modules_DoNotDependOnInfrastructureOrTheApi()
    {
        TestResult result = Types
            .InAssemblies(SourceAssemblies)
            .That()
            .ResideInNamespace("LastHour.Modules")
            .Should()
            .NotHaveDependencyOnAny(
                "LastHour.BuildingBlocks.Infrastructure",
                "LastHour.Api")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(DescribeFailures(result));
    }

    [Fact]
    public void Api_IsThinCompositionRoot_AndUsesNoPersistenceFrameworkDirectly()
    {
        TestResult result = Types
            .InAssemblies(SourceAssemblies)
            .That()
            .ResideInNamespace("LastHour.Api")
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Npgsql",
                "StackExchange.Redis")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(DescribeFailures(result));
    }

    private static Assembly[] BuildSourceAssemblies()
    {
        Assembly api = typeof(Program).Assembly;

        return [api, .. api.GetReferencedAssemblies()
            .Where(reference => reference.Name!.StartsWith("LastHour.", StringComparison.Ordinal))
            .Select(reference => Assembly.Load(reference.Name!))];
    }

    private static string DescribeFailures(TestResult result)
        => string.Join(Environment.NewLine, result.FailingTypes.Select(type => type.FullName));
}
