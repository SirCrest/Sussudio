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
        => token.StartsWith("Sussudio/", StringComparison.Ordinal) ||
           token.StartsWith("tests/", StringComparison.Ordinal) ||
           token.StartsWith("tools/", StringComparison.Ordinal) ||
           token.StartsWith("docs/", StringComparison.Ordinal);

    private static bool ResolvesCleanupPlanToken(
        string token,
        IReadOnlyCollection<string> files,
        IReadOnlySet<string> directories)
    {
        if (token.Contains('*', StringComparison.Ordinal))
        {
            return AgentMapWildcardMatches(token, files);
        }

        var normalized = token.TrimEnd('/');
        return files.Contains(normalized, StringComparer.OrdinalIgnoreCase) ||
            directories.Contains(normalized);
    }
}
