using System.Threading.Tasks;

static partial class Program
{
    private static Task ArchitectureAgentMap_CoversToolAutomationPartialFamiliesWithExactPaths()
    {
        var repoRoot = GetRepoRoot();
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var missing = EnumerateToolAutomationPartialFamilyFiles(repoRoot)
            .Where(file => !agentMapText.Contains($"`{file}`", StringComparison.Ordinal))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "AGENT_MAP.md is missing exact tool automation ownership file entries: " +
                string.Join(", ", missing));
        }

        return Task.CompletedTask;
    }
}
