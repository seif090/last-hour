using System.Xml.Linq;
using FluentAssertions;

namespace LastHour.BuildingBlocks.Architecture.Tests.Architecture;

/// <summary>
/// Enforces the project-to-project reference graph declared in the csproj files.
/// The declared ProjectReference list is the ground truth for the architecture
/// boundaries. Assembly-level references cannot be used for this purpose: the
/// compiler elides references to assemblies that contribute no used types, so
/// the empty marker projects (Architecture, Contracts and all modules today)
/// would be invisible to a reflection-based check.
/// </summary>
public class ProjectReferenceGraphTests
{
    private static readonly string RepoRoot = LocateRepoRoot();

    private static readonly string[] AllSrcProjectNames =
    [
        "LastHour.Api",
        "LastHour.BuildingBlocks.Application",
        "LastHour.BuildingBlocks.Architecture",
        "LastHour.BuildingBlocks.Contracts",
        "LastHour.BuildingBlocks.Infrastructure",
        "LastHour.BuildingBlocks.SharedKernel",
        "LastHour.Modules.Administration",
        "LastHour.Modules.Analytics",
        "LastHour.Modules.Catalog",
        "LastHour.Modules.Identity",
        "LastHour.Modules.Inventory",
        "LastHour.Modules.Merchant",
        "LastHour.Modules.Notifications",
        "LastHour.Modules.Offers",
        "LastHour.Modules.Ordering",
        "LastHour.Modules.Payments",
        "LastHour.Modules.Platform",
    ];

    public static IEnumerable<object[]> DeclaredReferenceGraph()
    {
        foreach (string projectName in AllSrcProjectNames)
        {
            yield return new object[] { projectName, LoadDeclaredReferences(projectName) };
        }
    }

    [Theory]
    [MemberData(nameof(DeclaredReferenceGraph))]
    public void Project_DeclaresOnlyReferencesInsideItsLayerBoundary(string projectName, string[] references)
    {
        references.Should().BeSubsetOf(AllowedReferencesFor(projectName));
    }

    [Fact]
    public void Api_AsCompositionRoot_ReferencesEveryBuildingBlockAndModule()
    {
        string[] expected = AllSrcProjectNames.Where(name => name != "LastHour.Api").ToArray();

        string[] references = LoadDeclaredReferences("LastHour.Api");

        references.Should().BeEquivalentTo(expected);
    }

    private static string[] LoadDeclaredReferences(string projectName)
    {
        string csprojPath = Directory
            .GetFiles(RepoRoot, $"{projectName}.csproj", SearchOption.AllDirectories)
            .Single();

        XDocument document = XDocument.Load(csprojPath);

        return document.Root!
            .Elements("ItemGroup")
            .Elements("ProjectReference")
            .Attributes("Include")
            .Select(include => Path.GetFileNameWithoutExtension(include.Value))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static string LocateRepoRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "LastHour.Backend.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException(
                "Could not locate the repository root (LastHour.Backend.sln).");
    }

    private static string[] AllowedReferencesFor(string projectName) => projectName switch
    {
        "LastHour.BuildingBlocks.Architecture" => [],
        "LastHour.BuildingBlocks.SharedKernel" => ["LastHour.BuildingBlocks.Architecture"],
        "LastHour.BuildingBlocks.Application" =>
            ["LastHour.BuildingBlocks.Architecture", "LastHour.BuildingBlocks.SharedKernel"],
        "LastHour.BuildingBlocks.Contracts" =>
        [
            "LastHour.BuildingBlocks.Application",
            "LastHour.BuildingBlocks.Architecture",
            "LastHour.BuildingBlocks.SharedKernel",
        ],
        "LastHour.BuildingBlocks.Infrastructure" =>
        [
            "LastHour.BuildingBlocks.Application",
            "LastHour.BuildingBlocks.Architecture",
            "LastHour.BuildingBlocks.SharedKernel",
        ],
        "LastHour.Api" => AllSrcProjectNames.Where(name => name != "LastHour.Api").ToArray(),
        _ when projectName.StartsWith("LastHour.Modules.", StringComparison.Ordinal) =>
        [
            "LastHour.BuildingBlocks.Application",
            "LastHour.BuildingBlocks.Architecture",
            "LastHour.BuildingBlocks.Contracts",
            "LastHour.BuildingBlocks.SharedKernel",
        ],
        _ => throw new ArgumentOutOfRangeException(
            nameof(projectName), projectName, "Unknown src project; keep AllSrcProjectNames in sync."),
    };
}
