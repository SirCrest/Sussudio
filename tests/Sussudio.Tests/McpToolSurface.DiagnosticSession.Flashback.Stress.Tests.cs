using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionFlashbackStressScenario_OwnsStressFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var stressText = ReadDiagnosticSessionFlashbackStressScenarioSource();
        var stressRootText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackStressScenario.Stress.cs")
            .Replace("\r\n", "\n");
        var warmPlaybackText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackStressScenario.WarmPlayback.cs")
            .Replace("\r\n", "\n");
        var commandDrainText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackStressScenario.CommandDrain.cs")
            .Replace("\r\n", "\n");

        AssertContains(stressText, "internal static partial class DiagnosticSessionFlashbackStressScenario");
        AssertContains(stressText, "internal const int FlashbackStressMaxPlaybackPendingCommands = 4;");
        AssertContains(stressText, "internal const int FlashbackStressMaxPlaybackCommandLatencyMs = 750;");
        AssertContains(stressText, "internal const double FlashbackStressPlaybackWarmSeconds = 10.0;");
        AssertContains(stressText, "internal const long FlashbackStressAudioUnavailableFallbackAllowance = 4;");
        AssertContains(stressText, "internal const int FlashbackScrubStressMaxPlaybackPendingCommands = 20;");
        AssertContains(stressText, "internal static async Task RunFlashbackStressAsync(");
        AssertContains(stressRootText, "ValidateFlashbackStressWarmPlaybackAsync(");
        AssertContains(stressRootText, "ValidateFlashbackStressCommandDrainAsync(");
        AssertDoesNotContain(stressRootText, "ClassifyFlashbackStressAudioMasterFallbackWarning(");
        AssertContains(warmPlaybackText, "private static async Task ValidateFlashbackStressWarmPlaybackAsync(");
        AssertContains(warmPlaybackText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(warmPlaybackText, "\"flashback playback warmed frames=");
        AssertContains(warmPlaybackText, "ClassifyFlashbackStressAudioMasterFallbackWarning(");
        AssertDoesNotContain(warmPlaybackText, "\"flashback stress: playback command queue did not drain within 10s \"");
        AssertContains(commandDrainText, "private static async Task ValidateFlashbackStressCommandDrainAsync(");
        AssertContains(commandDrainText, "BuildPlaybackCommandHealth(lastSnapshot, baselineSnapshot)");
        AssertContains(commandDrainText, "\"flashback stress: playback command queue did not drain within 10s \"");
        AssertDoesNotContain(commandDrainText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(stressText, "internal static async Task RunFlashbackScrubStressAsync(");
        AssertContains(stressText, "WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(stressText, "new Dictionary<string, object?> { [\"action\"] = \"begin-scrub\", [\"positionMs\"] = 500 }");
        AssertContains(stressText, "new Dictionary<string, object?> { [\"action\"] = \"update-scrub\", [\"positionMs\"] = positions[i] }");
        AssertContains(stressText, "new Dictionary<string, object?> { [\"action\"] = \"end-scrub\", [\"positionMs\"] = positions[^1] }");
        AssertContains(stressText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(stressText, "internal static string? ClassifyFlashbackStressAudioMasterFallbackWarning(");
        AssertContains(stressText, "\"flashback stress: audio-master harmful fallbacks increased during warmed playback \"");
        AssertContains(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackStressScenario;");
        AssertContains(startupText, "RunFlashbackStressAsync(");
        AssertContains(startupText, "RunFlashbackScrubStressAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackStressAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackScrubStressAsync(");
        AssertDoesNotContain(runnerText, "private static string? ClassifyFlashbackStressAudioMasterFallbackWarning(");
        AssertDoesNotContain(runnerText, "private const int FlashbackStressMaxPlaybackPendingCommands = 4;");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackStressScenario_ClassifiesAudioMasterFallbacks()
    {
        var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var stressScenarioType = assembly.GetType("Sussudio.Tools.DiagnosticSessionFlashbackStressScenario")
            ?? throw new InvalidOperationException("DiagnosticSessionFlashbackStressScenario type was not found.");
        var classify = stressScenarioType.GetMethod(
                "ClassifyFlashbackStressAudioMasterFallbackWarning",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Audio-master fallback classifier was not found.");

        AssertEqual((string?)null, Invoke(0, 0, 0, 0), "no audio-master fallback warning");
        AssertEqual((string?)null, Invoke(4, 4, 0, 0), "startup unavailable fallback allowance");

        var unavailable = Invoke(5, 5, 0, 0)
            ?? throw new InvalidOperationException("Expected unavailable fallback warning.");
        AssertContains(unavailable, "audio-master unavailable fallbacks exceeded startup allowance");
        AssertContains(unavailable, "unavailableDelta=5");
        AssertContains(unavailable, "allowance=4");
        AssertContains(unavailable, "totalDelta=5");

        var stale = Invoke(2, 0, 1, 0)
            ?? throw new InvalidOperationException("Expected stale fallback warning.");
        AssertContains(stale, "audio-master harmful fallbacks increased during warmed playback");
        AssertContains(stale, "staleDelta=1");
        AssertContains(stale, "driftOutlierDelta=0");

        var driftOutlier = Invoke(2, 0, 0, 1)
            ?? throw new InvalidOperationException("Expected drift-outlier fallback warning.");
        AssertContains(driftOutlier, "audio-master harmful fallbacks increased during warmed playback");
        AssertContains(driftOutlier, "staleDelta=0");
        AssertContains(driftOutlier, "driftOutlierDelta=1");

        var unclassified = Invoke(2, 0, 0, 0)
            ?? throw new InvalidOperationException("Expected unclassified fallback warning.");
        AssertContains(unclassified, "audio-master unclassified fallbacks increased during warmed playback");
        AssertContains(unclassified, "delta=2");

        return Task.CompletedTask;

        string? Invoke(long totalDelta, long unavailableDelta, long staleDelta, long driftOutlierDelta)
            => classify.Invoke(null, new object?[] { totalDelta, unavailableDelta, staleDelta, driftOutlierDelta }) as string;
    }
}
