using System.Threading.Tasks;

static partial class Program
{
    private static Task ArchitectureAgentMap_FileReferencesResolve()
    {
        var repoRoot = GetRepoRoot();
        var agentMapPath = Path.Combine(repoRoot, "docs", "architecture", "AGENT_MAP.md");
        var agentMapText = File.ReadAllText(agentMapPath);
        var files = Directory.EnumerateFiles(repoRoot, "*", SearchOption.AllDirectories)
            .Where(file => !HasIgnoredPathSegment(repoRoot, file))
            .Select(file => NormalizeRepoRelativePath(repoRoot, file))
            .ToArray();
        var directories = Directory.EnumerateDirectories(repoRoot, "*", SearchOption.AllDirectories)
            .Where(directory => !HasIgnoredPathSegment(repoRoot, directory))
            .Select(directory => NormalizeRepoRelativePath(repoRoot, directory))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var failures = new List<string>();
        foreach (var token in EnumerateAgentMapPathTokens(agentMapText).Distinct(StringComparer.Ordinal))
        {
            if (ResolvesAgentMapToken(token, files, directories))
            {
                continue;
            }

            failures.Add(token);
        }

        if (failures.Count > 0)
        {
            throw new InvalidOperationException(
                "AGENT_MAP.md references missing files or folders: " + string.Join(", ", failures));
        }

        return Task.CompletedTask;
    }
}
