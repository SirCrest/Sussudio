using System.Text.RegularExpressions;
using System.Threading.Tasks;

static partial class Program
{
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

    private static IEnumerable<string> EnumerateCleanupPlanPathTokens(string markdown)
    {
        foreach (Match match in MarkdownCodeSpanRegex.Matches(markdown))
        {
            var token = NormalizeProjectInclude(match.Groups[1].Value.Trim());
            if (IsCleanupPlanPathToken(token))
            {
                yield return token;
            }
        }
    }

    private static bool IsCleanupPlanPathToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || string.Equals(token, "/", StringComparison.Ordinal))
        {
            return false;
        }

        if (token.StartsWith("Sussudio/", StringComparison.Ordinal) ||
            token.StartsWith("tests/", StringComparison.Ordinal) ||
            token.StartsWith("tools/", StringComparison.Ordinal) ||
            token.StartsWith("docs/", StringComparison.Ordinal))
        {
            return true;
        }

        return token.StartsWith("MainWindow.", StringComparison.Ordinal) &&
            token.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ResolvesCleanupPlanToken(
        string token,
        IReadOnlyCollection<string> files,
        IReadOnlySet<string> directories)
    {
        if (token.EndsWith("/", StringComparison.Ordinal))
        {
            return directories.Contains(token.TrimEnd('/'));
        }

        if (token.Contains('*', StringComparison.Ordinal))
        {
            return AgentMapWildcardMatches(token, files);
        }

        var normalized = token.TrimEnd('/');
        if (token.Contains('/', StringComparison.Ordinal))
        {
            return files.Contains(normalized, StringComparer.OrdinalIgnoreCase) ||
                directories.Contains(normalized);
        }

        return files.Any(file => string.Equals(Path.GetFileName(file), token, StringComparison.OrdinalIgnoreCase));
    }

    private static bool CleanupPlanContainsExactCodeSpan(string cleanupPlanText, string relativePath)
    {
        var normalizedPath = NormalizeProjectInclude(relativePath);
        var fileName = GetRepoFileName(normalizedPath);

        return cleanupPlanText.Contains($"`{normalizedPath}`", StringComparison.Ordinal) ||
            cleanupPlanText.Contains($"`{fileName}`", StringComparison.Ordinal);
    }
}
