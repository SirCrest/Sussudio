using System.Threading.Tasks;

static partial class Program
{
    private static Task ArchitectureAgentMap_CoversAutomationConsumerChecklist()
    {
        var readmeText = ReadRepoFile("README.md")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var consumers = ExtractReadmeAutomationConsumers(readmeText).ToArray();
        var missing = new List<string>();

        AssertEqual(9, consumers.Length, "README automation consumer checklist count");

        foreach (var consumer in consumers)
        {
            if (AutomationConsumerIsCoveredByAgentMap(consumer, agentMapText))
            {
                continue;
            }

            missing.Add(consumer);
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "AGENT_MAP.md is missing automation consumer ownership for: " +
                string.Join(", ", missing));
        }

        return Task.CompletedTask;
    }
}
