using System.Text.RegularExpressions;
using System.Threading.Tasks;

static partial class Program
{
    private static Task ArchitectureAgentMap_TestOwnerPathsUseCodeSpansAndResolve()
    {
        var repoRoot = GetRepoRoot();
        var agentMapPath = Path.Combine(repoRoot, "docs", "architecture", "AGENT_MAP.md");
        var agentMapText = File.ReadAllText(agentMapPath);
        var failures = new List<string>();

        foreach (var line in agentMapText.Split('\n'))
        {
            var normalizedLine = line.TrimEnd('\r');
            if (normalizedLine.Contains("ests/Sussudio.Tests/", StringComparison.Ordinal) &&
                !MarkdownCodeSpanRegex.IsMatch(normalizedLine))
            {
                failures.Add(normalizedLine.Trim());
                continue;
            }

            if (Regex.IsMatch(
                normalizedLine,
                @"^\s*-\s+tests/Sussudio\.Tests/[^`]+ owns\b",
                RegexOptions.CultureInvariant))
            {
                failures.Add(normalizedLine.Trim());
            }
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "AGENT_MAP.md test-owner paths must be complete and wrapped in code spans: " +
                string.Join(" | ", failures));
        }

        return ArchitectureAgentMap_FileReferencesResolve();
    }
}
