using System.Text.RegularExpressions;
using System.Threading.Tasks;

static partial class Program
{
    private static Task ArchitectureDocs_ReadRepoFileLiteralPathsResolve()
    {
        var repoRoot = GetRepoRoot();
        var testRoot = Path.Combine(repoRoot, "tests", "Sussudio.Tests");
        var pathPattern = new Regex(
            @"(?:RuntimeContractSource\.)?ReadRepoFile\(\s*""(?<path>[^""]+)""",
            RegexOptions.Compiled);
        var failures = new List<string>();

        foreach (var file in Directory.GetFiles(testRoot, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            foreach (Match match in pathPattern.Matches(text))
            {
                var repoRelativePath = match.Groups["path"].Value;
                var candidate = Path.Combine(
                    repoRoot,
                    repoRelativePath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(candidate) && !Directory.Exists(candidate))
                {
                    var relativeTestPath = Path.GetRelativePath(repoRoot, file).Replace('\\', '/');
                    failures.Add($"{relativeTestPath}: {repoRelativePath}");
                }
            }
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "Literal ReadRepoFile paths must resolve to live repo files or directories:\n" +
                string.Join("\n", failures.OrderBy(path => path, StringComparer.Ordinal)));
        }

        return Task.CompletedTask;
    }

    private static Task ArchitectureCleanupPlan_FileReferencesResolve()
    {
        var repoRoot = GetRepoRoot();
        var cleanupPlanPath = Path.Combine(repoRoot, "docs", "architecture", "cleanup-plan.md");
        var cleanupPlanText = File.ReadAllText(cleanupPlanPath);
        var files = Directory.EnumerateFiles(repoRoot, "*", SearchOption.AllDirectories)
            .Where(file => !HasIgnoredPathSegment(repoRoot, file))
            .Select(file => NormalizeRepoRelativePath(repoRoot, file))
            .ToArray();
        var directories = Directory.EnumerateDirectories(repoRoot, "*", SearchOption.AllDirectories)
            .Where(directory => !HasIgnoredPathSegment(repoRoot, directory))
            .Select(directory => NormalizeRepoRelativePath(repoRoot, directory))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var failures = new List<string>();
        foreach (var token in EnumerateCleanupPlanPathTokens(cleanupPlanText).Distinct(StringComparer.Ordinal))
        {
            if (ResolvesCleanupPlanToken(token, files, directories))
            {
                continue;
            }

            failures.Add(token);
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "cleanup-plan.md references missing repo files or folders: " + string.Join(", ", failures));
        }

        return Task.CompletedTask;
    }

    private static Task ArchitectureCleanupPlan_CoversArchitectureDocsTestFamily()
    {
        var repoRoot = GetRepoRoot();
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");
        var missing = EnumerateArchitectureDocsTestFiles(repoRoot)
            .Where(file => !CleanupPlanContainsExactCodeSpan(cleanupPlanText, file))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "cleanup-plan.md is missing ArchitectureDocs test-family owner entries: " +
                string.Join(", ", missing));
        }

        return Task.CompletedTask;
    }

    private static Task ArchitectureCleanupPlan_DefinesSmallFileHygiene()
    {
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(cleanupPlanText, "Small-file hygiene applies to every slice below");
        AssertContains(cleanupPlanText, "do not create or keep sub-100-line files");
        AssertContains(cleanupPlanText, "owning a stable contract, hot-path lifetime, XAML adapter surface, shared tool");
        AssertContains(cleanupPlanText, "fold it back into that owner and update the source-shape tests");

        return Task.CompletedTask;
    }

    private static Task TestMigrationPlan_FileReferencesResolveAndNamesValidationCommands()
    {
        var repoRoot = GetRepoRoot();
        var migrationPath = Path.Combine(repoRoot, "tests", "Sussudio.Tests", "MIGRATION.md");
        var migrationText = File.ReadAllText(migrationPath);
        var files = Directory.EnumerateFiles(repoRoot, "*", SearchOption.AllDirectories)
            .Where(file => !HasIgnoredPathSegment(repoRoot, file))
            .Select(file => NormalizeRepoRelativePath(repoRoot, file))
            .ToArray();
        var directories = Directory.EnumerateDirectories(repoRoot, "*", SearchOption.AllDirectories)
            .Where(directory => !HasIgnoredPathSegment(repoRoot, directory))
            .Select(directory => NormalizeRepoRelativePath(repoRoot, directory))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var failures = new List<string>();

        foreach (var token in EnumerateMigrationPlanPathTokens(migrationText).Distinct(StringComparer.Ordinal))
        {
            if (ResolvesMigrationPlanToken(token, files, directories))
            {
                continue;
            }

            failures.Add(token);
        }

        if (!migrationText.Contains("dotnet test tests/Sussudio.Tests/Sussudio.Tests.csproj --no-restore", StringComparison.Ordinal))
        {
            failures.Add("missing dotnet test validation command");
        }

        if (!migrationText.Contains("dotnet exec tests\\Sussudio.Tests\\bin\\Debug\\net8.0\\Sussudio.Tests.dll", StringComparison.Ordinal))
        {
            failures.Add("missing dotnet exec legacy harness validation command");
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "MIGRATION.md references or validation commands are stale: " +
                string.Join(", ", failures));
        }

        return Task.CompletedTask;
    }

    private static Task TestMigrationPlan_CoversXUnitInventory()
    {
        var repoRoot = GetRepoRoot();
        var migrationText = ReadRepoFile("tests/Sussudio.Tests/MIGRATION.md");
        var missing = EnumerateXUnitTestFiles(repoRoot)
            .Where(file => !MarkdownContainsExactCodeSpan(migrationText, file))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "MIGRATION.md is missing xUnit test inventory entries: " +
                string.Join(", ", missing));
        }

        return Task.CompletedTask;
    }
}
