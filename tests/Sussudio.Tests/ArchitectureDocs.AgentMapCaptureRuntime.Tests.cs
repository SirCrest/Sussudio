using System.Threading.Tasks;

static partial class Program
{
    private static Task ArchitectureAgentMap_CoversCaptureRuntimeOwnershipFiles()
    {
        var repoRoot = GetRepoRoot();
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var missing = EnumerateCaptureRuntimeOwnershipFiles(repoRoot)
            .Where(file => !AgentMapContainsExactCodeSpan(agentMapText, file))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "AGENT_MAP.md is missing CaptureService ownership file entries: " +
                string.Join(", ", missing));
        }

        return Task.CompletedTask;
    }
}
