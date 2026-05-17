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

    private static Task ArchitectureAgentMap_CoversArchitectureDocsTestFamily()
    {
        var repoRoot = GetRepoRoot();
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var missing = EnumerateArchitectureDocsTestFiles(repoRoot)
            .Where(file => !AgentMapContainsExactCodeSpan(agentMapText, file))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "AGENT_MAP.md is missing ArchitectureDocs test-family owner entries: " +
                string.Join(", ", missing));
        }

        return Task.CompletedTask;
    }

    private static Task ArchitectureAgentMap_ToolsCommonOwnershipEntriesAreUnique()
    {
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var ownerBulletRegex = new Regex(
            @"^\s*-\s+`(?<path>tools/Common/[^`]+\.cs)`\s+(?:also\s+owns|owns|is)\b",
            RegexOptions.CultureInvariant);
        var firstLineByPath = new Dictionary<string, int>(StringComparer.Ordinal);
        var duplicates = new List<string>();
        var lines = agentMapText.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var match = ownerBulletRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var path = match.Groups["path"].Value;
            var lineNumber = i + 1;
            if (firstLineByPath.TryGetValue(path, out var firstLineNumber))
            {
                duplicates.Add($"{path} first={firstLineNumber} duplicate={lineNumber}");
                continue;
            }

            firstLineByPath[path] = lineNumber;
        }

        if (duplicates.Count > 0)
        {
            throw new InvalidOperationException(
                "AGENT_MAP.md has duplicate tools/Common ownership bullets: " +
                string.Join(" | ", duplicates));
        }

        return Task.CompletedTask;
    }

    private static Task TestProject_DoesNotKeepEmptyPartialMarkerShells()
    {
        var repoRoot = GetRepoRoot();
        var testRoot = Path.Combine(repoRoot, "tests", "Sussudio.Tests");
        var emptyMarkerShells = Directory.EnumerateFiles(testRoot, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(path =>
            {
                var normalized = File.ReadAllText(path).Replace("\r\n", "\n").Trim();
                return normalized == "static partial class Program\n{\n}";
            })
            .Select(path => Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (emptyMarkerShells.Length > 0)
        {
            throw new InvalidOperationException(
                "Empty test partial marker shells add navigation cost without ownership: " +
                string.Join(", ", emptyMarkerShells));
        }

        return Task.CompletedTask;
    }
}
