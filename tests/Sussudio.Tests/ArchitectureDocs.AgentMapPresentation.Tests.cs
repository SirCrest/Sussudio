using System.Threading.Tasks;

static partial class Program
{
    private static Task ArchitectureAgentMap_CoversUiPresentationOwnershipFiles()
    {
        var repoRoot = GetRepoRoot();
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var missing = EnumerateUiPresentationOwnershipFiles(repoRoot)
            .Where(file => !AgentMapContainsRequiredUiPresentationCodeSpan(agentMapText, file))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "AGENT_MAP.md is missing UI/presentation ownership file entries: " +
                string.Join(", ", missing));
        }

        return Task.CompletedTask;
    }
}
