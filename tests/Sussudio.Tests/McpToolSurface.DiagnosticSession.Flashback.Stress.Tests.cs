using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionFlashbackStressScenario_OwnsStressFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var stressText = ReadDiagnosticSessionFlashbackStressScenarioSource();
        var stressRootText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackStressScenario.Stress.cs")
            .Replace("\r\n", "\n");
        var warmPlaybackText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackStressScenario.WarmPlayback.cs")
            .Replace("\r\n", "\n");
        var warmPlaybackAudioText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackStressScenario.WarmPlaybackAudio.cs")
            .Replace("\r\n", "\n");
        var commandDrainText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackStressScenario.CommandDrain.cs")
            .Replace("\r\n", "\n");
        var scrubText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackStressScenario.Scrub.cs")
            .Replace("\r\n", "\n");
        var scrubDrainText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackStressScenario.ScrubDrain.cs")
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
        AssertContains(warmPlaybackText, "CaptureFlashbackStressWarmPlaybackAudioBaseline(");
        AssertContains(warmPlaybackText, "CaptureFlashbackStressWarmPlaybackAudioDeltas(");
        AssertContains(warmPlaybackText, "ClassifyFlashbackStressAudioMasterFallbackWarning(");
        AssertDoesNotContain(warmPlaybackText, "FlashbackPlaybackAudioMasterUnavailableFallbacks");
        AssertContains(warmPlaybackAudioText, "private readonly record struct FlashbackStressWarmPlaybackAudioBaseline(");
        AssertContains(warmPlaybackAudioText, "private readonly record struct FlashbackStressWarmPlaybackAudioDeltas(");
        AssertContains(warmPlaybackAudioText, "private static FlashbackStressWarmPlaybackAudioBaseline CaptureFlashbackStressWarmPlaybackAudioBaseline(");
        AssertContains(warmPlaybackAudioText, "private static FlashbackStressWarmPlaybackAudioDeltas CaptureFlashbackStressWarmPlaybackAudioDeltas(");
        AssertContains(warmPlaybackAudioText, "FlashbackPlaybackAudioMasterUnavailableFallbacks");
        AssertContains(warmPlaybackAudioText, "FlashbackPlaybackAudioMasterLastFallbackReason");
        AssertDoesNotContain(warmPlaybackAudioText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertDoesNotContain(warmPlaybackText, "\"flashback stress: playback command queue did not drain within 10s \"");
        AssertContains(commandDrainText, "private static async Task ValidateFlashbackStressCommandDrainAsync(");
        AssertContains(commandDrainText, "BuildPlaybackCommandHealth(lastSnapshot, baselineSnapshot)");
        AssertContains(commandDrainText, "\"flashback stress: playback command queue did not drain within 10s \"");
        AssertDoesNotContain(commandDrainText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(stressText, "internal static async Task RunFlashbackScrubStressAsync(");
        AssertContains(scrubText, "WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(scrubText, "new Dictionary<string, object?> { [\"action\"] = \"begin-scrub\", [\"positionMs\"] = 500 }");
        AssertContains(scrubText, "new Dictionary<string, object?> { [\"action\"] = \"update-scrub\", [\"positionMs\"] = positions[i] }");
        AssertContains(scrubText, "new Dictionary<string, object?> { [\"action\"] = \"end-scrub\", [\"positionMs\"] = positions[^1] }");
        AssertContains(scrubText, "ValidateFlashbackScrubStressDrainAsync(");
        AssertDoesNotContain(scrubText, "\"flashback scrub stress: playback did not settle live with an empty queue within 10s \"");
        AssertContains(scrubDrainText, "private static async Task ValidateFlashbackScrubStressDrainAsync(");
        AssertContains(scrubDrainText, "\"flashback scrub stress: playback did not settle live with an empty queue within 10s \"");
        AssertContains(scrubDrainText, "BuildPlaybackCommandHealth(lastSnapshot, baselineSnapshot)");
        AssertContains(scrubDrainText, "FlashbackScrubStressMaxPlaybackPendingCommands");
        AssertContains(stressText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(stressText, "internal static string? ClassifyFlashbackStressAudioMasterFallbackWarning(");
        AssertContains(stressText, "\"flashback stress: audio-master harmful fallbacks increased during warmed playback \"");
        AssertContains(stressText, "internal static void RegisterSelectedFlashbackStressScenarioTasks(");
        AssertContains(stressText, "1,\n                \"flashback-stress-task\",");
        AssertContains(stressText, "3,\n                \"flashback-scrub-stress-task\",");
        AssertContains(stressText, "RunFlashbackStressAsync(");
        AssertContains(stressText, "RunFlashbackScrubStressAsync(");
        AssertContains(stressText, "sendRawWithConnectRetryAsync");
        AssertContains(stressText, "actions.Add(\"flashback stress started\")");
        AssertContains(stressText, "actions.Add(\"flashback scrub stress started\")");
        AssertContains(startupText, "DiagnosticSessionFlashbackStressScenario.RegisterSelectedFlashbackStressScenarioTasks(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackStressScenario;");
        AssertDoesNotContain(startupText, "RunFlashbackStressAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackScrubStressAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackStressAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackScrubStressAsync(");
        AssertDoesNotContain(runnerText, "private static string? ClassifyFlashbackStressAudioMasterFallbackWarning(");
        AssertDoesNotContain(runnerText, "private const int FlashbackStressMaxPlaybackPendingCommands = 4;");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackStressScenario_ClassifiesAudioMasterFallbacks()
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
