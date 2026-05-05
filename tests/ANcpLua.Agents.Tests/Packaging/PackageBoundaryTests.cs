using System.Xml.Linq;
using StringComparisonExtensions = ANcpLua.Roslyn.Utilities.StringComparisonExtensions;

namespace ANcpLua.Agents.Tests.Packaging;

public sealed class PackageBoundaryTests
{
    private static readonly string s_repoRoot = LocateRepoRoot();

    private static readonly HashSet<string> s_expectedPackageIds =
    [
        "ANcpLua.Agents",
        "ANcpLua.Agents.Workflows",
        "ANcpLua.Agents.Testing",
        "ANcpLua.Agents.Testing.Workflows",
        "ANcpLua.Agents.Hosting.Azure",
        "ANcpLua.Agents.Hosting.Foundry",
        "ANcpLua.Agents.Hosting.Anthropic",
        "ANcpLua.Agents.Hosting.DevUI",
        "ANcpLua.Agents.Foundry",
        "ANcpLua.Agents.Hosting.OpenAI",
    ];

    private static readonly HashSet<string> s_stablePackageIds =
    [
        "ANcpLua.Agents",
        "ANcpLua.Agents.Workflows",
        "ANcpLua.Agents.Testing",
        "ANcpLua.Agents.Testing.Workflows",
    ];

    private static readonly Dictionary<string, string[]> s_expectedDirectMafReferences = new(StringComparer.Ordinal)
    {
        ["ANcpLua.Agents"] = ["Microsoft.Agents.AI"],
        ["ANcpLua.Agents.Workflows"] = ["Microsoft.Agents.AI", "Microsoft.Agents.AI.Workflows"],
        ["ANcpLua.Agents.Testing"] = ["Microsoft.Agents.AI"],
        ["ANcpLua.Agents.Testing.Workflows"] = ["Microsoft.Agents.AI", "Microsoft.Agents.AI.Workflows"],
        ["ANcpLua.Agents.Hosting.Azure"] = ["Microsoft.Agents.AI.Hosting.AzureFunctions"],
        ["ANcpLua.Agents.Hosting.Foundry"] = ["Microsoft.Agents.AI.Foundry.Hosting"],
        ["ANcpLua.Agents.Hosting.Anthropic"] = ["Microsoft.Agents.AI.Anthropic"],
        ["ANcpLua.Agents.Hosting.DevUI"] = ["Microsoft.Agents.AI.DevUI"],
        ["ANcpLua.Agents.Foundry"] =
        [
            "Microsoft.Agents.AI.Foundry",
            "Microsoft.Agents.AI.Workflows.Declarative.Foundry",
        ],
        ["ANcpLua.Agents.Hosting.OpenAI"] =
        [
            "Microsoft.Agents.AI",
            "Microsoft.Agents.AI.Hosting.OpenAI",
        ],
    };

    [Fact]
    public void SourcePackages_MatchExpectedConsumerToolkitLayout()
    {
        var packageIds = LoadSourceProjects()
            .Select(static project => project.PackageId)
            .Order(StringComparer.Ordinal)
            .ToArray();

        packageIds.Should().BeEquivalentTo(s_expectedPackageIds);
    }

    [Fact]
    public void SourcePackages_UsePackageIdAsRootNamespace()
    {
        foreach (var project in LoadSourceProjects())
        {
            project.RootNamespace.Should().Be(
                project.PackageId,
                $"{project.RelativePath} must keep namespace and package identity aligned");
        }
    }

    [Fact]
    public void StablePackages_DoNotReferencePrereleaseMafPackages()
    {
        var centralVersions = LoadCentralPackageVersions();

        foreach (var project in LoadSourceProjects().Where(project => s_stablePackageIds.Contains(project.PackageId)))
        {
            var prereleaseMafReferences = project.PackageReferences
                .Where(static package => StringComparisonExtensions.StartsWithOrdinal(package, "Microsoft.Agents."))
                .Select(package => (Package: package, Version: centralVersions.GetValueOrDefault(package, "")))
                .Where(static reference => StringComparisonExtensions.ContainsOrdinal(reference.Version, "-"));

            prereleaseMafReferences.Should().BeEmpty(
                $"{project.PackageId} is stable and must not pull preview, rc, or alpha MAF dependencies");
        }
    }

    [Fact]
    public void SourcePackages_ReferenceOnlyTheirExpectedDirectMafPackages()
    {
        foreach (var project in LoadSourceProjects())
        {
            var mafReferences = project.PackageReferences
                .Where(static package => StringComparisonExtensions.StartsWithOrdinal(package, "Microsoft.Agents."))
                .Order(StringComparer.Ordinal)
                .ToArray();

            mafReferences.Should().Equal(
                s_expectedDirectMafReferences[project.PackageId].Order(StringComparer.Ordinal),
                $"{project.PackageId} must keep its direct MAF dependency boundary explicit");
        }
    }

    [Fact]
    public void NonTestingPackages_DoNotReferenceTestingPackages()
    {
        foreach (var project in LoadSourceProjects().Where(static project => !StringComparisonExtensions.ContainsOrdinal(project.PackageId, ".Testing")))
        {
            var testingReferences = project.ProjectReferences
                .Select(Path.GetFileNameWithoutExtension)
                .Where(static reference => reference is not null && StringComparisonExtensions.ContainsOrdinal(reference, ".Testing"));

            testingReferences.Should().BeEmpty($"{project.PackageId} must not depend on test-only packages");
        }
    }

    [Fact]
    public void PackageReadmes_DeclareMafCompatibility()
    {
        foreach (var project in LoadSourceProjects())
        {
            var readmePath = Path.Combine(Path.GetDirectoryName(project.FullPath)!, "README.md");
            File.Exists(readmePath).Should().BeTrue($"{project.PackageId} must pack a package README");

            var readme = File.ReadAllText(readmePath);
            readme.Should().Contain("Consumer toolkit for Microsoft Agent Framework");
            readme.Should().Contain("Compatible with: Microsoft.Agents.AI 1.3.x");
            readme.Should().Contain("Tested against: Microsoft.Agents.AI 1.3.0");
        }
    }

    private static ProjectInfo[] LoadSourceProjects()
    {
        return Directory.EnumerateFiles(Path.Combine(s_repoRoot, "src"), "*.csproj", SearchOption.AllDirectories)
            .Select(ProjectInfo.Load)
            .Where(static project => StringComparisonExtensions.StartsWithOrdinal(project.PackageId, "ANcpLua.Agents"))
            .OrderBy(static project => project.PackageId, StringComparer.Ordinal)
            .ToArray();
    }

    private static Dictionary<string, string> LoadCentralPackageVersions()
    {
        var versions = new Dictionary<string, string>(StringComparer.Ordinal);
        var properties = LoadVersionProperties();
        var document = XDocument.Load(Path.Combine(s_repoRoot, "Directory.Packages.props"));

        foreach (var packageVersion in document.Descendants("PackageVersion"))
        {
            var include = packageVersion.Attribute("Include")?.Value ?? packageVersion.Attribute("Update")?.Value;
            var version = packageVersion.Attribute("Version")?.Value;

            if (!string.IsNullOrWhiteSpace(include) && !string.IsNullOrWhiteSpace(version))
            {
                versions[include] = ResolveProperty(version, properties);
            }
        }

        return versions;
    }

    private static Dictionary<string, string> LoadVersionProperties()
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        var document = XDocument.Load(Path.Combine(s_repoRoot, "Version.props"));

        foreach (var property in document.Descendants().Where(static element => !element.HasElements))
        {
            properties[property.Name.LocalName] = property.Value;
        }

        return properties;
    }

    private static string ResolveProperty(string value, Dictionary<string, string> properties)
    {
        if (StringComparisonExtensions.StartsWithOrdinal(value, "$(") &&
            value.EndsWith(')') &&
            properties.TryGetValue(value[2..^1], out var resolved))
        {
            return resolved;
        }

        return value;
    }

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

    private sealed record ProjectInfo(
        string FullPath,
        string RelativePath,
        string PackageId,
        string RootNamespace,
        IReadOnlyCollection<string> PackageReferences,
        IReadOnlyCollection<string> ProjectReferences)
    {
        public static ProjectInfo Load(string projectPath)
        {
            var document = XDocument.Load(projectPath);
            var project = document.Root ?? throw new InvalidOperationException($"Missing root element in {projectPath}.");
            var packageId = ValueOf(project, "PackageId");
            var rootNamespace = ValueOf(project, "RootNamespace");

            var packageReferences = project.Descendants("PackageReference")
                .Select(static element => element.Attribute("Include")?.Value)
                .Where(static value => value is not null)
                .Cast<string>()
                .ToArray();

            var projectReferences = project.Descendants("ProjectReference")
                .Select(static element => element.Attribute("Include")?.Value)
                .Where(static value => value is not null)
                .Cast<string>()
                .ToArray();

            return new ProjectInfo(
                projectPath,
                Path.GetRelativePath(s_repoRoot, projectPath),
                packageId,
                rootNamespace,
                packageReferences,
                projectReferences);
        }

        private static string ValueOf(XElement project, string elementName)
        {
            return project.Descendants(elementName).FirstOrDefault()?.Value
                ?? throw new InvalidOperationException($"Missing <{elementName}> in project.");
        }
    }
}
