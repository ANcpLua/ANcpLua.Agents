using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ANcpLua.Agents.Tests.Packaging;

/// <summary>
/// Keeps the publish workflow's pack list honest against the projects that actually exist.
/// </summary>
/// <remarks>
/// The pack step is a hand-maintained list of one <c>dotnet pack</c> per project rather than a
/// solution-wide pack, which is deliberate — a new project should not reach nuget.org just because
/// someone added it to the solution. The failure mode that buys is the inverse: a project that is
/// packable, in the solution, and expected by <see cref="PackageBoundaryTests"/>, but that CI never
/// packs, so it silently never publishes. That happened to
/// <c>ANcpLua.Agents.Evaluation</c> on its first release. These tests make the two lists agree.
/// </remarks>
public sealed partial class PublishWorkflowTests
{
    private static readonly string s_repoRoot = LocateRepoRoot();

    [Fact]
    public void PublishWorkflow_PacksEveryPackableSourceProject()
    {
        var packable = PackableProjectPaths();
        var packed = PackedProjectPaths();

        packed.Should().BeEquivalentTo(
            packable,
            "every packable src project must appear in the publish workflow's pack step, or it builds and " +
            "tests forever without ever reaching nuget.org");
    }

    [Fact]
    public void PublishWorkflow_PackStepHasNoDuplicates()
    {
        var packed = PackedProjectPaths();

        packed.Should().OnlyHaveUniqueItems();
    }

    /// <summary>Every <c>src/**.csproj</c> that declares a PackageId and is not opted out of packing.</summary>
    private static string[] PackableProjectPaths() =>
        [.. Directory.EnumerateFiles(Path.Combine(s_repoRoot, "src"), "*.csproj", SearchOption.AllDirectories)
            .Where(static path =>
            {
                var project = XDocument.Load(path).Root!;
                bool hasPackageId = project.Descendants("PackageId").Any();
                bool optedOut = string.Equals(
                    project.Descendants("IsPackable").FirstOrDefault()?.Value,
                    "false",
                    StringComparison.OrdinalIgnoreCase);
                return hasPackageId && !optedOut;
            })
            .Select(static path => Path.GetRelativePath(s_repoRoot, path).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)];

    /// <summary>Every project path named by a <c>dotnet pack</c> line in the publish workflow.</summary>
    private static string[] PackedProjectPaths()
    {
        string workflow = File.ReadAllText(Path.Combine(s_repoRoot, ".github", "workflows", "nuget-publish.yml"));

        return [.. PackCommandRegex().Matches(workflow)
            .Select(static match => match.Groups["path"].Value)
            .Order(StringComparer.Ordinal)];
    }

    [GeneratedRegex(@"dotnet pack\s+(?<path>src/[^\s]+\.csproj)")]
    private static partial Regex PackCommandRegex();

    private static string LocateRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ANcpLua.Agents.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate ANcpLua.Agents repository root.");
    }
}
