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

    private static Task ArchitectureAgentMap_MapsFlashbackPreviewStartupToResourceOwner()
    {
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var previewBackendEntry = ExtractTextBetween(
            agentMapText,
            "- `CaptureService.FlashbackPreviewBackend.cs` owns Flashback preview backend",
            "- `CaptureService.FlashbackPreviewBackendDisposal.cs` owns Flashback preview");

        AssertContains(previewBackendEntry, "transition coordination");
        AssertContains(previewBackendEntry, "AV1 encoder support probing");
        AssertContains(previewBackendEntry, "video/audio readiness");
        AssertContains(previewBackendEntry, "resource-owner request construction");
        AssertContains(previewBackendEntry, "deferred cleanup handoff");
        AssertContains(previewBackendEntry, "Startup construction, install, playback initialization, producer attachment");
        AssertContains(previewBackendEntry, "`FlashbackBackendResources.cs`");
        AssertDoesNotContain(previewBackendEntry, "startup: buffer manager, encoder sink, exporter, playback controller, and\n  producer attachment");

        return Task.CompletedTask;
    }
}
