using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionFlashbackValidation_OwnsFlashbackWarningPolicy()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var validationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSupport.cs")
            .Replace("\r\n", "\n");

        AssertContains(validationText, "internal static class DiagnosticSessionFlashbackValidation");
        AssertContains(validationText, "internal static void ValidateFlashbackRecordingSession(");
        AssertContains(validationText, "\"flashback recording: no Flashback video frames submitted to encoder\"");
        AssertContains(validationText, "internal static void ValidateFlashbackPlaybackSession(");
        AssertContains(validationText, "\"flashback playback: no playback frames were observed\"");
        AssertContains(validationText, "\"flashback playback: absolute A/V drift exceeded budget");
        AssertContains(validationText, "internal static void ValidateFlashbackPreviewScheduler(");
        AssertContains(validationText, "\"flashback preview: present/display pressure \"");
        AssertContains(validationText, "latestSlowReason={FormatOptional(previewD3DMetrics.LatestSlowFrameReason)}");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionFlashbackValidation;");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackRecordingSession(");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackPlaybackSession(");
        AssertDoesNotContain(runnerText, "private static void ValidateFlashbackPreviewScheduler(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRunner_IgnoresTransientFlashbackWarmupWarnings()
    {
        var assembly = LoadToolAssembly(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var healthPolicyType = assembly.GetType("Sussudio.Tools.DiagnosticSessionHealthPolicy")
            ?? throw new InvalidOperationException("DiagnosticSessionHealthPolicy type was not found.");
        var sampleType = assembly.GetType("Sussudio.Tools.DiagnosticSessionSample")
            ?? throw new InvalidOperationException("DiagnosticSessionSample type was not found.");
        var buildObservation = healthPolicyType.GetMethod(
                "BuildSessionDiagnosticHealthObservation",
                BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("BuildSessionDiagnosticHealthObservation was not found.");

        var samples = CreateDiagnosticSessionSampleList(
            sampleType,
            (1_000, CreateDiagnosticSnapshot("Warning", "flashback_playback", "startup 1% low")),
            (12_000, CreateDiagnosticSnapshot("Healthy", "none", "warmed")));
        var finalSnapshot = CreateDiagnosticSnapshot("Healthy", "none", "final");
        var transientWarningObservation = buildObservation.Invoke(
                null,
                new object?[] { samples, finalSnapshot, true })
            ?? throw new InvalidOperationException("Transient warning observation was null.");
        AssertEqual("Healthy", GetPropertyValue(transientWarningObservation, "HealthStatus") as string, "flashback warmup health status");
        AssertEqual("none", GetPropertyValue(transientWarningObservation, "LikelyStage") as string, "flashback warmup likely stage");

        var criticalSamples = CreateDiagnosticSessionSampleList(
            sampleType,
            (1_000, CreateDiagnosticSnapshot("Critical", "flashback_playback", "startup crash")),
            (12_000, CreateDiagnosticSnapshot("Healthy", "none", "warmed")));
        var criticalObservation = buildObservation.Invoke(
                null,
                new object?[] { criticalSamples, finalSnapshot, true })
            ?? throw new InvalidOperationException("Critical observation was null.");
        AssertEqual("Critical", GetPropertyValue(criticalObservation, "HealthStatus") as string, "flashback critical health status");
        AssertEqual("flashback_playback", GetPropertyValue(criticalObservation, "LikelyStage") as string, "flashback critical likely stage");

        return Task.CompletedTask;

        static object CreateDiagnosticSessionSampleList(Type sampleType, params (long OffsetMs, JsonElement Snapshot)[] values)
        {
            var listType = typeof(List<>).MakeGenericType(sampleType);
            var list = (System.Collections.IList)(Activator.CreateInstance(listType)
                ?? throw new InvalidOperationException("DiagnosticSessionSample list could not be created."));
            foreach (var value in values)
            {
                var sample = Activator.CreateInstance(sampleType)
                    ?? throw new InvalidOperationException("DiagnosticSessionSample instance could not be created.");
                sampleType.GetProperty("OffsetMs")!.SetValue(sample, value.OffsetMs);
                sampleType.GetProperty("TimestampUtc")!.SetValue(sample, DateTimeOffset.UtcNow);
                sampleType.GetProperty("Snapshot")!.SetValue(sample, value.Snapshot);
                list.Add(sample);
            }

            return list;
        }

        static JsonElement CreateDiagnosticSnapshot(string health, string stage, string evidence)
        {
            using var document = JsonDocument.Parse($$"""
                {
                  "DiagnosticHealthStatus": "{{health}}",
                  "DiagnosticLikelyStage": "{{stage}}",
                  "DiagnosticEvidence": "{{evidence}}"
                }
                """);
            return document.RootElement.Clone();
        }
    }

    internal static Task DiagnosticSessionFlashbackWaits_OwnsSnapshotPollingWaits()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var setupText = ReadDiagnosticSessionScenarioSetupSource();
        var waitsText = ReadDiagnosticSessionFlashbackWaitsSource();

        AssertContains(waitsText, "internal static class DiagnosticSessionFlashbackWaits");
        AssertDoesNotContain(waitsText, "internal static partial class DiagnosticSessionFlashbackWaits");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackBoundaryCrossAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackStateAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(waitsText, "internal static async Task<bool> WaitForFlashbackPlaybackPositionAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackActiveAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForPreviewActiveAsync(");
        AssertContains(waitsText, "internal static async Task<JsonElement?> WaitForFlashbackRecordingReadyAsync(");
        AssertContains(waitsText, "internal static async Task<bool> WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(waitsText, "FlashbackPlaybackPendingCommands");
        AssertContains(waitsText, "FlashbackPlaybackFrameCount");
        AssertContains(waitsText, "RecordingBackend");
        AssertContains(waitsText, "RecordingFileGrowing");
        AssertContains(waitsText, "FlashbackBufferedDurationMs");
        AssertContains(waitsText, "requiredEncodedFrames");
        AssertContains(waitsText, "string expectedState");
        AssertContains(waitsText, "positionMs >= boundaryMs + 1_500");
        AssertContains(waitsText, "FlashbackPlaybackTargetFps");
        AssertContains(waitsText, "SelectedExactFrameRate");
        AssertContains(waitsText, "Math.Abs(position - targetPositionMs) <= 1_500");
        AssertContains(setupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackWaits;");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackPlaybackBoundaryCrossAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackPlaybackStateAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> WaitForFlashbackPlaybackPositionAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackActiveAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForPreviewActiveAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<JsonElement?> WaitForFlashbackRecordingReadyAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> WaitForFlashbackStressBufferReadyAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackStressScenario_OwnsStressFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var stressText = ReadDiagnosticSessionFlashbackStressScenarioSource();

        AssertContains(stressText, "internal static class DiagnosticSessionFlashbackStressScenario");
        AssertDoesNotContain(stressText, "internal static partial class DiagnosticSessionFlashbackStressScenario");
        AssertContains(stressText, "internal const int FlashbackStressMaxPlaybackPendingCommands = 4;");
        AssertContains(stressText, "internal const int FlashbackStressMaxPlaybackCommandLatencyMs = 750;");
        AssertContains(stressText, "internal const double FlashbackStressPlaybackWarmSeconds = 10.0;");
        AssertContains(stressText, "internal const long FlashbackStressAudioUnavailableFallbackAllowance = 4;");
        AssertContains(stressText, "internal const int FlashbackScrubStressMaxPlaybackPendingCommands = 20;");
        AssertContains(stressText, "internal static async Task RunFlashbackStressAsync(");
        AssertContains(stressText, "ValidateFlashbackStressWarmPlaybackAsync(");
        AssertContains(stressText, "private static async Task VerifyFlashbackStressExportAsync(");
        AssertContains(stressText, "\"flashback-stress-export.mp4\"");
        AssertContains(stressText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(stressText, "flashback stress export verified");
        AssertContains(stressText, "private static async Task ValidateFlashbackStressWarmPlaybackAsync(");
        AssertContains(stressText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(stressText, "\"flashback playback warmed frames=");
        AssertContains(stressText, "private readonly record struct FlashbackStressWarmPlaybackAudioBaseline(");
        AssertContains(stressText, "private readonly record struct FlashbackStressWarmPlaybackAudioDeltas(");
        AssertContains(stressText, "private static FlashbackStressWarmPlaybackAudioBaseline CaptureFlashbackStressWarmPlaybackAudioBaseline(");
        AssertContains(stressText, "private static FlashbackStressWarmPlaybackAudioDeltas CaptureFlashbackStressWarmPlaybackAudioDeltas(");
        AssertContains(stressText, "FlashbackPlaybackAudioMasterUnavailableFallbacks");
        AssertContains(stressText, "FlashbackPlaybackAudioMasterLastFallbackReason");
        AssertContains(stressText, "private static async Task ValidateFlashbackStressCommandDrainAsync(");
        AssertContains(stressText, "BuildPlaybackCommandHealth(lastSnapshot, baselineSnapshot)");
        AssertContains(stressText, "\"flashback stress: playback command queue did not drain within 10s \"");
        AssertContains(stressText, "private readonly record struct FlashbackStressPlaybackDrainResult(");
        AssertContains(stressText, "private static async Task<FlashbackStressPlaybackDrainResult> WaitForFlashbackStressPlaybackCommandDrainAsync(");
        AssertContains(stressText, "GetInt(lastSnapshot, \"FlashbackPlaybackPendingCommands\") == 0");
        AssertContains(stressText, "GetString(lastSnapshot, \"FlashbackPlaybackState\")");
        AssertContains(stressText, "internal static async Task RunFlashbackScrubStressAsync(");
        AssertContains(stressText, "WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(stressText, "new Dictionary<string, object?> { [\"action\"] = \"begin-scrub\", [\"positionMs\"] = 500 }");
        AssertContains(stressText, "private static async Task<int> RunFlashbackScrubStressUpdateBurstAsync(");
        AssertContains(stressText, "new Dictionary<string, object?> { [\"action\"] = \"update-scrub\", [\"positionMs\"] = positions[i] }");
        AssertContains(stressText, "return positions[^1];");
        AssertContains(stressText, "flashback scrub stress: {failedUpdates} update-scrub command(s) failed");
        AssertContains(stressText, "new Dictionary<string, object?> { [\"action\"] = \"end-scrub\", [\"positionMs\"] = finalScrubPositionMs }");
        AssertContains(stressText, "private static async Task ValidateFlashbackScrubStressDrainAsync(");
        AssertContains(stressText, "\"flashback scrub stress: playback did not settle live with an empty queue within 10s \"");
        AssertContains(stressText, "FlashbackScrubStressMaxPlaybackPendingCommands");
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

    internal static Task DiagnosticSessionFlashbackExportScenarios_OwnExportFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var scenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var rootText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.cs")
            .Replace("\r\n", "\n");
        var disableDuringExportText = rootText;
        var playbackText = rootText;
        var rangeText = rootText;
        var scenariosTextWithoutSpaces = scenariosText.Replace(" ", string.Empty);

        AssertContains(scenariosText, "internal static class DiagnosticSessionFlashbackExportScenarios");
        AssertContains(scenariosText, "internal static async Task RunFlashbackExportConcurrentAsync(");
        AssertContains(scenariosText, "\"flashback-concurrent-a.mp4\"");
        AssertContains(scenariosText, "flashback concurrent exports verified");
        AssertContains(scenariosText, "internal static async Task RunFlashbackDisableDuringExportAsync(");
        AssertContains(scenariosText, "\"flashback-disable-during-export.mp4\"");
        AssertContains(scenariosText, "SendCommandWithConnectRetryAsync(");
        AssertContains(disableDuringExportText, "ValidateFlashbackDisableDuringExportFileAsync(");
        AssertContains(disableDuringExportText, "ValidateFlashbackDisabledAfterExportAsync(");
        AssertContains(disableDuringExportText, "ValidateFlashbackReenabledAfterDisableDuringExportAsync(");
        AssertContains(disableDuringExportText, "private static async Task ValidateFlashbackDisableDuringExportFileAsync(");
        AssertContains(disableDuringExportText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(disableDuringExportText, "private static async Task ValidateFlashbackDisabledAfterExportAsync(");
        AssertContains(disableDuringExportText, "flashback disable during export: pending playback commands remained after disable");
        AssertContains(disableDuringExportText, "private static async Task ValidateFlashbackReenabledAfterDisableDuringExportAsync(");
        AssertContains(scenariosText, "internal static async Task RunFlashbackRotatedExportAsync(");
        AssertContains(scenariosText, "TryParseFlashbackExportSegmentCount(exportMessage)");
        AssertContains(scenariosText, "internal static async Task RunFlashbackExportPlaybackAsync(");
        AssertContains(scenariosText, "flashback export during playback verified");
        AssertContains(playbackText, "CaptureFlashbackExportPlaybackFrameCountBeforeExportAsync(");
        AssertContains(playbackText, "ValidateFlashbackExportPlaybackAfterExportAsync(");
        AssertContains(playbackText, "ValidateFlashbackExportPlaybackFinalStateAsync(");
        AssertContains(playbackText, "private static async Task<long> CaptureFlashbackExportPlaybackFrameCountBeforeExportAsync(");
        AssertContains(playbackText, "flashback export playback: expected Playing before export");
        AssertContains(playbackText, "private static async Task ValidateFlashbackExportPlaybackAfterExportAsync(");
        AssertContains(playbackText, "flashback export playback: playback frame count did not advance during export");
        AssertContains(playbackText, "private static async Task ValidateFlashbackExportPlaybackFinalStateAsync(");
        AssertContains(playbackText, "BuildPlaybackCommandHealth(finalSnapshot, baselineSnapshot)");
        AssertContains(playbackText, "flashback export playback: pending commands remained after go-live");
        AssertContains(scenariosText, "internal static async Task RunFlashbackRangeExportAsync(");
        AssertContains(rangeText, "private static async Task<FlashbackSelectionRange?> PrepareFlashbackSelectionRangeAsync(");
        AssertContains(rangeText, "private readonly record struct FlashbackSelectionRange(");
        AssertContains(rangeText, "WaitForFlashbackStressBufferReadyAsync(");
        AssertContains(rangeText, "private static async Task MarkFlashbackSelectionPointAsync(");
        AssertContains(rangeText, "WaitForFlashbackPlaybackPositionAsync(");
        AssertContains(scenariosText, "\"clear-in-out-points\"");
        AssertContains(scenariosText, "\"set-in-point\"");
        AssertContains(scenariosText, "\"set-out-point\"");
        AssertContains(scenariosText, "[\"useSelectionRange\"] = true");
        AssertContains(scenariosText, "private static void ValidateFlashbackRangeExportResult(");
        AssertContains(scenariosText, "private static async Task ValidateFlashbackRangeExportCleanupAsync(");
        AssertContains(rootText, "internal static void RegisterSelectedFlashbackExportScenarioTasks(");
        AssertContains(rootText, "RegisterFlashbackExportPlaybackTask(");
        AssertContains(rootText, "RegisterFlashbackRangeExportTasks(");
        AssertContains(rootText, "RegisterFlashbackExportCoordinationTasks(");
        AssertContains(rootText, "backgroundTasks.AddScenario(");
        AssertContains(rootText, "private static void RegisterFlashbackExportPlaybackTask(");
        AssertContains(rootText, "6,\n            \"flashback-export-playback-task\",");
        AssertContains(rootText, "flashback export playback started");
        AssertContains(rootText, "private static void RegisterFlashbackRangeExportTasks(");
        AssertContains(rootText, "8,\n                \"flashback-range-export-task\",");
        AssertContains(rootText, "9,\n                \"flashback-range-export-audio-switch-task\",");
        AssertContains(rootText, "flashback range export audio switch started");
        AssertContains(rootText, "private static void RegisterFlashbackExportCoordinationTasks(");
        AssertContains(rootText, "10,\n                \"flashback-export-concurrent-task\",");
        AssertContains(rootText, "11,\n                \"flashback-disable-during-export-task\",");
        AssertContains(rootText, "12,\n                \"flashback-rotated-export-task\",");
        AssertContains(rootText, "flashback rotated export started");
        AssertContains(scenariosTextWithoutSpaces, "6,\n\"flashback-export-playback-task\",");
        AssertContains(scenariosTextWithoutSpaces, "8,\n\"flashback-range-export-task\",");
        AssertContains(scenariosTextWithoutSpaces, "9,\n\"flashback-range-export-audio-switch-task\",");
        AssertContains(scenariosTextWithoutSpaces, "10,\n\"flashback-export-concurrent-task\",");
        AssertContains(scenariosTextWithoutSpaces, "11,\n\"flashback-disable-during-export-task\",");
        AssertContains(scenariosTextWithoutSpaces, "12,\n\"flashback-rotated-export-task\",");
        AssertContains(scenariosText, "\"flashback-range-export-task\"");
        AssertContains(scenariosText, "actions.Add(\"flashback concurrent export started\")");
        AssertContains(startupText, "DiagnosticSessionFlashbackExportScenarios.RegisterSelectedFlashbackExportScenarioTasks(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExportScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackExportConcurrentAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackDisableDuringExportAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackRotatedExportAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackExportPlaybackAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackRangeExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackExportConcurrentAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackDisableDuringExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRotatedExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackExportPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRangeExportAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackExports_OwnsExportHelpers()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var exportScenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var stressText = ReadDiagnosticSessionFlashbackStressScenarioSource();
        var exportHelpersText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSupport.cs")
            .Replace("\r\n", "\n");

        AssertContains(exportHelpersText, "internal static class DiagnosticSessionFlashbackExports");
        AssertDoesNotContain(exportHelpersText, "internal static partial class DiagnosticSessionFlashbackExports");
        AssertContains(exportHelpersText, "internal static int? TryParseFlashbackExportSegmentCount(");
        AssertContains(exportHelpersText, "const string marker = \" from \";");
        AssertContains(exportHelpersText, "suffix.Contains(\"segment\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(exportHelpersText, "internal static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(string filePath)");
        AssertContains(exportHelpersText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertContains(exportHelpersText, "internal static async Task CleanupFlashbackSelectionAsync(");
        AssertContains(exportHelpersText, "\"clear-in-out-points\"");
        AssertContains(exportHelpersText, "\"go-live\"");
        AssertContains(exportHelpersText, "internal static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertContains(exportHelpersText, "\"SetAudioEnabled\"");
        AssertContains(exportScenariosText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExports;");
        AssertContains(stressText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExports;");
        AssertDoesNotContain(runnerText, "private static int? TryParseFlashbackExportSegmentCount(");
        AssertDoesNotContain(runnerText, "private static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(");
        AssertDoesNotContain(runnerText, "private static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task CleanupFlashbackSelectionAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackSegments_OwnsSegmentWaitsAndParsing()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var exportScenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var segmentPlaybackText = ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource();
        var segmentsText = ReadDiagnosticSessionFlashbackSegmentsSource();

        AssertContains(segmentsText, "internal static class DiagnosticSessionFlashbackSegments");
        AssertDoesNotContain(segmentsText, "internal static partial class DiagnosticSessionFlashbackSegments");
        AssertContains(segmentsText, "internal readonly record struct FlashbackSegmentProbe(");
        AssertContains(segmentsText, "internal readonly record struct FlashbackSegmentPlaybackTarget(");
        AssertContains(segmentsText, "internal static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(");
        AssertContains(segmentsText, "\"FlashbackGetSegments\"");
        AssertContains(segmentsText, "internal static bool TryGetFlashbackSegments(");
        AssertContains(segmentsText, "internal static async Task<FlashbackSegmentPlaybackTarget?> WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertContains(segmentsText, "const int requiredHeadroomMs = 8_000;");
        AssertContains(segmentsText, "internal static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(");
        AssertContains(segmentsText, "data.TryGetProperty(\"Segments\", out var segmentsElement)");
        AssertContains(segmentsText, "sendCommandAsync(\"FlashbackGetSegments\", null, null)");
        AssertContains(segmentsText, "sendCommandAsync(\"GetSnapshot\", null, null)");
        AssertContains(exportScenariosText, "using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;");
        AssertContains(segmentPlaybackText, "using static Sussudio.Tools.DiagnosticSessionFlashbackSegments;");
        AssertDoesNotContain(runnerText, "private readonly record struct FlashbackSegmentProbe(");
        AssertDoesNotContain(runnerText, "private readonly record struct FlashbackSegmentPlaybackTarget(");
        AssertDoesNotContain(runnerText, "private static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(");
        AssertDoesNotContain(runnerText, "private static bool TryGetFlashbackSegments(");
        AssertDoesNotContain(runnerText, "private static async Task<FlashbackSegmentPlaybackTarget?> WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackCycleScenarios_OwnCycleFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var cyclesText = ReadDiagnosticSessionFlashbackCycleScenariosSource();

        AssertContains(cyclesText, "internal static class DiagnosticSessionFlashbackCycleScenarios");
        AssertDoesNotContain(cyclesText, "internal static partial class DiagnosticSessionFlashbackCycleScenarios");
        AssertContains(cyclesText, "internal static async Task RunFlashbackRestartCycleAsync(");
        AssertContains(cyclesText, "\"RestartFlashback\"");
        AssertContains(cyclesText, "private static async Task<bool> ValidateFlashbackRestartCycleActiveStateAsync(");
        AssertContains(cyclesText, "FlashbackPlaybackThreadAlive");
        AssertContains(cyclesText, "pending playback commands remained after restart");
        AssertContains(cyclesText, "private static async Task VerifyFlashbackRestartCycleExportAsync(");
        AssertContains(cyclesText, "\"flashback-restart-cycle-export.mp4\"");
        AssertContains(cyclesText, "flashback restart cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackEncoderCycleAsync(");
        AssertContains(cyclesText, "var cycledPreset = string.Equals(originalPreset, \"P1\", StringComparison.OrdinalIgnoreCase) ? \"P2\" : \"P1\";");
        AssertContains(cyclesText, "ValidateFlashbackEncoderCycleSnapshot(afterSnapshot, originalFilePath, warnings);");
        AssertContains(cyclesText, "private static void ValidateFlashbackEncoderCycleSnapshot(");
        AssertContains(cyclesText, "post-cycle encoder did not reach readiness frame count");
        AssertContains(cyclesText, "playback state not clean after preset cycle");
        AssertContains(cyclesText, "private static async Task VerifyFlashbackEncoderCycleExportAsync(");
        AssertContains(cyclesText, "\"flashback-encoder-cycle-export.mp4\"");
        AssertContains(cyclesText, "flashback encoder cycle export verified");
        AssertContains(cyclesText, "private static async Task RestoreFlashbackEncoderCyclePresetAsync(");
        AssertContains(cyclesText, "flashback encoder preset restored to");
        AssertContains(cyclesText, "Flashback buffer did not become ready after preset restore");
        AssertContains(cyclesText, "internal static void RegisterSelectedFlashbackCycleScenarioTasks(");
        AssertContains(cyclesText, "4,\n                \"flashback-restart-cycle-task\",");
        AssertContains(cyclesText, "5,\n                \"flashback-encoder-cycle-task\",");
        AssertContains(startupText, "DiagnosticSessionFlashbackCycleScenarios.RegisterSelectedFlashbackCycleScenarioTasks(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackCycleScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackRestartCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackEncoderCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRestartCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackEncoderCycleAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackPreviewCycleScenarios_OwnPreviewCycleFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var cyclesText = ReadDiagnosticSessionFlashbackPreviewCycleScenariosSource();
        var flashbackCycleText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.cs")
            .Replace("\r\n", "\n");
        var playbackCycleText = flashbackCycleText;
        var recordingCycleText = flashbackCycleText;

        AssertContains(cyclesText, "internal static class DiagnosticSessionFlashbackPreviewCycleScenarios");
        AssertDoesNotContain(cyclesText, "internal static partial class DiagnosticSessionFlashbackPreviewCycleScenarios");
        AssertContains(cyclesText, "internal static async Task RunFlashbackPreviewCycleAsync(");
        AssertContains(flashbackCycleText, "flashback preview cycle preview stopped");
        AssertContains(flashbackCycleText, "CaptureFlashbackPreviewCycleEncodedFramesBeforeStopAsync(");
        AssertContains(flashbackCycleText, "ValidateFlashbackPreviewCycleStoppedAsync(");
        AssertContains(flashbackCycleText, "ValidateFlashbackPreviewCycleRestartedAsync(");
        AssertContains(flashbackCycleText, "private static async Task<long> CaptureFlashbackPreviewCycleEncodedFramesBeforeStopAsync(");
        AssertContains(flashbackCycleText, "private static async Task<bool> ValidateFlashbackPreviewCycleStoppedAsync(");
        AssertContains(flashbackCycleText, "flashback preview cycle: Flashback frames did not advance while preview was off");
        AssertContains(flashbackCycleText, "private static async Task ValidateFlashbackPreviewCycleRestartedAsync(");
        AssertContains(flashbackCycleText, "VideoFramesFlowing");
        AssertContains(flashbackCycleText, "VerifyFlashbackPreviewCycleExportAsync(");
        AssertContains(flashbackCycleText, "private static async Task VerifyFlashbackPreviewCycleExportAsync(");
        AssertContains(flashbackCycleText, "\"flashback-preview-off-export.mp4\"");
        AssertContains(flashbackCycleText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(flashbackCycleText, "flashback preview cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackPlaybackPreviewCycleAsync(");
        AssertContains(playbackCycleText, "flashback playback preview cycle preview stopped during playback");
        AssertContains(playbackCycleText, "CapturePlaybackPreviewCycleFrameCountBeforeStopAsync(");
        AssertContains(playbackCycleText, "ValidatePlaybackPreviewCycleStoppedAsync(");
        AssertContains(playbackCycleText, "ValidatePlaybackPreviewCycleRestartedAsync(");
        AssertContains(playbackCycleText, "private static async Task<long> CapturePlaybackPreviewCycleFrameCountBeforeStopAsync(");
        AssertContains(playbackCycleText, "WaitForFlashbackPlaybackWarmSampleAsync(");
        AssertContains(playbackCycleText, "private static async Task<bool> ValidatePlaybackPreviewCycleStoppedAsync(");
        AssertContains(playbackCycleText, "flashback playback preview cycle: playback did not return live after preview stop");
        AssertContains(playbackCycleText, "private static async Task ValidatePlaybackPreviewCycleRestartedAsync(");
        AssertContains(playbackCycleText, "VideoFramesFlowing");
        AssertContains(playbackCycleText, "VerifyFlashbackPlaybackPreviewCycleExportAsync(");
        AssertContains(playbackCycleText, "private static async Task VerifyFlashbackPlaybackPreviewCycleExportAsync(");
        AssertContains(playbackCycleText, "\"flashback-playback-preview-cycle.mp4\"");
        AssertContains(playbackCycleText, "CreateFlashbackExportVerifyPayload(exportPath)");
        AssertContains(playbackCycleText, "flashback playback preview cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackRecordingPreviewCycleAsync(");
        AssertContains(cyclesText, "flashback recording preview cycle preview stopped");
        AssertContains(recordingCycleText, "CaptureRecordingPreviewCycleCountersBeforeStopAsync(");
        AssertContains(recordingCycleText, "ValidateRecordingPreviewCycleStoppedAsync(");
        AssertContains(recordingCycleText, "ValidateRecordingPreviewCycleRestartedAsync(");
        AssertContains(recordingCycleText, "private readonly record struct RecordingPreviewCycleCounters(");
        AssertContains(recordingCycleText, "private static async Task<RecordingPreviewCycleCounters?> CaptureRecordingPreviewCycleCountersBeforeStopAsync(");
        AssertContains(recordingCycleText, "WaitForFlashbackRecordingReadyAsync(");
        AssertContains(recordingCycleText, "WaitForPreviewActiveAsync(");
        AssertContains(recordingCycleText, "private static async Task<bool> ValidateRecordingPreviewCycleStoppedAsync(");
        AssertContains(recordingCycleText, "flashback recording preview cycle: recording counters did not advance while preview was off");
        AssertContains(recordingCycleText, "private static async Task ValidateRecordingPreviewCycleRestartedAsync(");
        AssertContains(recordingCycleText, "VideoFramesFlowing");
        AssertContains(recordingCycleText, "flashback recording preview cycle: preview frames did not resume");
        AssertDoesNotContain(cyclesText, "internal static bool IsPreviewCycleScenario(");
        AssertContains(cyclesText, "internal static void RegisterSelectedFlashbackPreviewCycleScenarioTasks(");
        AssertContains(cyclesText, "13,\n                \"flashback-preview-cycle-task\",");
        AssertContains(cyclesText, "14,\n                \"flashback-playback-preview-cycle-task\",");
        AssertContains(cyclesText, "15,\n                \"flashback-recording-preview-cycle-task\",");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackPreviewCycleScenarios.Playback.cs")),
            "Flashback playback preview-cycle scenario stays with the preview-cycle scenario family");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackPreviewCycleScenarios.Recording.cs")),
            "Flashback recording preview-cycle scenario stays with the preview-cycle scenario family");
        AssertContains(startupText, "DiagnosticSessionFlashbackPreviewCycleScenarios.RegisterSelectedFlashbackPreviewCycleScenarioTasks(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackPreviewCycleScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackPreviewCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackPlaybackPreviewCycleAsync(");
        AssertDoesNotContain(startupText, "RunFlashbackRecordingPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackPlaybackPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRecordingPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static bool IsPreviewCycleScenario(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackRejectedExports_OwnRejectionFlows()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var rejectedExportsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExportScenarios.cs")
            .Replace("\r\n", "\n");

        AssertContains(rejectedExportsText, "internal static class DiagnosticSessionFlashbackExportScenarios");
        AssertContains(rejectedExportsText, "internal static async Task RunSelectedRejectedExportScenariosAsync(");
        AssertContains(rejectedExportsText, "private static async Task RunFlashbackExportRejectedAsync(");
        AssertContains(rejectedExportsText, "\"flashback-rejected-export.mp4\"");
        AssertContains(rejectedExportsText, "BufferInactive");
        AssertContains(rejectedExportsText, "Flashback buffer not active");
        AssertContains(rejectedExportsText, "private static async Task RunFlashbackRecordingExportRejectedAsync(");
        AssertContains(rejectedExportsText, "\"flashback-recording-rejected-export.mp4\"");
        AssertContains(rejectedExportsText, "UnavailableDuringRecording");
        AssertContains(rejectedExportsText, "recording backend changed after rejected export");
        var dispatchText = ExtractMemberCode(rejectedExportsText, "RunSelectedRejectedExportScenariosAsync");
        AssertContains(dispatchText, "scenarioPlan.RunFlashbackExportRejected");
        AssertContains(dispatchText, "scenarioPlan.RunFlashbackRecordingExportRejected");
        AssertOccursBefore(dispatchText, "RunFlashbackExportRejectedAsync(", "RunFlashbackRecordingExportRejectedAsync(");
        AssertContains(runnerText, "DiagnosticSessionFlashbackExportScenarios.RunSelectedRejectedExportScenariosAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackRejectedExports.cs")),
            "Flashback rejected-export scenarios stay folded into the export scenario owner");
        AssertDoesNotContain(runnerText, "DiagnosticSessionFlashbackRejectedExports.");
        AssertDoesNotContain(runnerText, "RunFlashbackExportRejectedAsync(");
        AssertDoesNotContain(runnerText, "RunFlashbackRecordingExportRejectedAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackExportRejectedAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRecordingExportRejectedAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackSegmentPlaybackScenarios_OwnSegmentPlaybackFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var segmentPlaybackText = ReadDiagnosticSessionFlashbackSegmentPlaybackScenariosSource();

        AssertContains(segmentPlaybackText, "internal static class DiagnosticSessionFlashbackSegmentPlaybackScenarios");
        AssertDoesNotContain(segmentPlaybackText, "internal static partial class DiagnosticSessionFlashbackSegmentPlaybackScenarios");
        AssertContains(segmentPlaybackText, "internal static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertContains(segmentPlaybackText, "flashback segment playback live headroom established");
        AssertContains(segmentPlaybackText, "flashback segment playback started near boundary");
        AssertContains(segmentPlaybackText, "private static async Task<FlashbackSegmentPlaybackTarget?> AcquireFlashbackSegmentPlaybackTargetAsync(");
        AssertContains(segmentPlaybackText, "WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertContains(segmentPlaybackText, "no playable completed segment became available after recording-assisted rotation");
        AssertContains(segmentPlaybackText, "private static void ValidateFlashbackSegmentPlaybackSnapshot(");
        AssertContains(segmentPlaybackText, "frameCount >= 180");
        AssertContains(segmentPlaybackText, "playback FPS below source-rate target after warm sample");
        AssertContains(segmentPlaybackText, "flashback segment playback: command queue unhealthy");
        AssertContains(segmentPlaybackText, "private static async Task ReturnFlashbackSegmentPlaybackLiveAsync(");
        AssertContains(segmentPlaybackText, "\"go-live\"");
        AssertContains(segmentPlaybackText, "flashback segment playback go-live requested");
        AssertContains(segmentPlaybackText, "flashback segment playback: playback ended in state");
        AssertContains(segmentPlaybackText, "private static async Task<bool> CreateFlashbackCompletedSegmentViaRecordingAsync(");
        AssertContains(segmentPlaybackText, "recording-assisted rotation started");
        AssertContains(segmentPlaybackText, "private static async Task TryStopRecordingAsync(");
        AssertContains(segmentPlaybackText, "internal static void RegisterSelectedFlashbackSegmentPlaybackScenarioTask(");
        AssertContains(segmentPlaybackText, "scenarioPlan.RunFlashbackSegmentPlayback");
        AssertContains(segmentPlaybackText, "7,\n            \"flashback-segment-playback-task\",");
        AssertContains(segmentPlaybackText, "actions.Add(\"flashback segment playback started\")");
        AssertContains(startupText, "DiagnosticSessionFlashbackSegmentPlaybackScenarios.RegisterSelectedFlashbackSegmentPlaybackScenarioTask(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackSegmentPlaybackScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackSegmentPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> CreateFlashbackCompletedSegmentViaRecordingAsync(");
        AssertDoesNotContain(runnerText, "private static async Task TryStopRecordingAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackRecordingSettingsScenarios_OwnDeferredSettingsFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var recordingChecksText = ReadDiagnosticSessionCleanupActionsSource()
            .Replace("\r\n", "\n");
        var recordingSettingsText = ReadDiagnosticSessionFlashbackRecordingSettingsScenariosSource();

        AssertContains(recordingSettingsText, "internal readonly record struct FlashbackRecordingSettingsDeferredPresetState(");
        AssertContains(recordingSettingsText, "internal static class DiagnosticSessionFlashbackRecordingSettingsScenarios");
        AssertDoesNotContain(recordingSettingsText, "internal static partial class DiagnosticSessionFlashbackRecordingSettingsScenarios");
        AssertContains(recordingSettingsText, "internal static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(recordingSettingsText, "flashback recording settings deferred preset changed to");
        AssertContains(recordingSettingsText, "VerifyFlashbackRestartRejectedDuringRecordingAsync(");
        AssertContains(recordingSettingsText, "VerifyFlashbackDisableRejectedDuringRecordingAsync(");
        AssertContains(recordingSettingsText, "VerifyFlashbackRecordingSettingsDeferredStillRecordingAsync(");
        AssertContains(recordingSettingsText, "private static async Task VerifyFlashbackRecordingSettingsCommandRejectedDuringRecordingAsync(");
        AssertContains(recordingSettingsText, "RestartFlashback unexpectedly succeeded during recording");
        AssertContains(recordingSettingsText, "SetFlashbackEnabled(false) unexpectedly succeeded during recording");
        AssertContains(recordingSettingsText, "Flashback recording backend did not remain active after mutations");
        AssertContains(recordingSettingsText, "recording counters did not advance after mutation attempts");
        AssertContains(recordingSettingsText, "internal static async Task VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertContains(recordingSettingsText, "flashback recording settings deferred post-stop buffer verified");
        AssertContains(recordingSettingsText, "private static async Task RestoreFlashbackRecordingSettingsOriginalPresetAsync(");
        AssertContains(recordingSettingsText, "\"SetPreset\"");
        AssertContains(recordingSettingsText, "flashback recording settings deferred preset restored to");
        AssertContains(recordingSettingsText, "selected preset was not restored");
        AssertContains(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;");
        AssertContains(startupText, "RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(recordingChecksText, "using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;");
        AssertContains(recordingChecksText, "VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertDoesNotContain(runnerText, "private static async Task VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertDoesNotContain(runnerText, "private readonly record struct FlashbackRecordingSettingsDeferredPresetState(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackLifecycleScenarios_OwnLifecycleFlow()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var startupText = ReadDiagnosticSessionScenarioStartupSource();
        var lifecycleText = ReadDiagnosticSessionFlashbackLifecycleScenariosSource();

        AssertContains(lifecycleText, "internal static class DiagnosticSessionFlashbackLifecycleScenarios");
        AssertContains(lifecycleText, "internal static void RegisterSelectedFlashbackLifecycleScenarioTask(");
        AssertContains(lifecycleText, "scenarioPlan.RunFlashbackLifecycle");
        AssertContains(lifecycleText, "backgroundTasks.AddScenario(");
        AssertContains(lifecycleText, "2,\n            \"flashback-lifecycle-task\",");
        AssertContains(lifecycleText, "actions.Add(\"flashback lifecycle started\")");
        AssertContains(lifecycleText, "internal static async Task RunFlashbackLifecycleAsync(");
        AssertContains(lifecycleText, "flashback lifecycle pause requested");
        AssertContains(lifecycleText, "flashback lifecycle disabled during playback");
        AssertContains(lifecycleText, "ValidateFlashbackLifecycleDisabledAsync(");
        AssertContains(lifecycleText, "flashback lifecycle re-enabled");
        AssertContains(lifecycleText, "ValidateFlashbackLifecycleReenabledAsync(");
        AssertContains(lifecycleText, "private static async Task ValidateFlashbackLifecycleDisabledAsync(");
        AssertContains(lifecycleText, "flashback lifecycle: playback worker still alive after disable");
        AssertContains(lifecycleText, "flashback lifecycle: pending commands remained after disable");
        AssertContains(lifecycleText, "private static async Task ValidateFlashbackLifecycleReenabledAsync(");
        AssertContains(startupText, "DiagnosticSessionFlashbackLifecycleScenarios.RegisterSelectedFlashbackLifecycleScenarioTask(");
        AssertDoesNotContain(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackLifecycleScenarios;");
        AssertDoesNotContain(startupText, "RunFlashbackLifecycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackLifecycleAsync(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackMetrics_OwnsFlashbackSessionMetricProjection()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var metricsText = ReadDiagnosticSessionFlashbackMetricsSource();
        var recordingText = metricsText;
        var playbackSessionText = metricsText;
        var playbackObservationText = metricsText;
        var playbackResultText = metricsText;
        var exportText = metricsText;

        AssertContains(metricsText, "internal static class DiagnosticSessionFlashbackMetrics");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.Recording.cs")), "Flashback recording metrics stay folded into the consolidated metrics owner");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.Export.cs")), "Flashback export metrics stay folded into the consolidated metrics owner");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.PlaybackObservation.cs")), "Flashback playback observation metrics stay folded into the consolidated metrics owner");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.RecordingExport.cs")), "Flashback recording/export metrics stay folded into the consolidated metrics owner");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.PlaybackSession.cs")), "Flashback playback session metrics stay folded into the consolidated metrics owner");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "tools", "Common", "DiagnosticSessionFlashbackMetrics.PlaybackResult.cs")), "Flashback playback result metrics stay folded into the consolidated metrics owner");
        AssertContains(recordingText, "internal sealed class FlashbackRecordingSessionMetrics");
        AssertContains(playbackSessionText, "internal sealed class FlashbackPlaybackSessionMetrics");
        AssertContains(playbackResultText, "internal sealed class FlashbackPlaybackResultMetrics");
        AssertContains(exportText, "internal sealed class FlashbackExportSessionMetrics");
        AssertContains(playbackSessionText, "public JsonElement BaselineSnapshot { get; init; }");
        AssertContains(playbackSessionText, "public int MaxCommandQueueLatencyMsObserved { get; set; }");
        AssertContains(playbackSessionText, "public double MaxSlowFramePercentObserved { get; set; }");
        AssertContains(playbackSessionText, "public long MinOnePercentLowAudioMasterFallbacks { get; set; }");
        AssertContains(playbackSessionText, "public string MaxDecodePhaseObserved { get; set; } = string.Empty;");
        AssertContains(playbackSessionText, "public double MaxAbsAvDriftMsObserved { get; set; }");
        AssertContains(playbackSessionText, "public long SubmitFailuresDelta { get; set; }");
        AssertContains(playbackResultText, "public JsonElement EndSnapshot { get; init; }");
        AssertContains(playbackResultText, "public int PendingCommandsAtEnd { get; init; }");
        AssertContains(playbackResultText, "public double OnePercentLowFpsAtEnd { get; init; }");
        AssertContains(playbackResultText, "public string MaxDecodePhaseAtEnd { get; init; } = string.Empty;");
        AssertContains(playbackResultText, "public long AudioMasterFallbacksAtEnd { get; init; }");
        AssertContains(playbackResultText, "public long SeekForwardDecodeCapHitsDelta { get; init; }");
        AssertContains(exportText, "public long ForceRotateFallbacksAtEnd { get; set; }");
        AssertContains(metricsText, "internal static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(");
        AssertContains(playbackSessionText, "internal static FlashbackPlaybackSessionMetrics BuildFlashbackPlaybackSessionMetrics(");
        AssertContains(playbackObservationText, "private static void ObservePlaybackSnapshot(");
        AssertContains(playbackObservationText, "var relevance = BuildPlaybackSnapshotRelevance(");
        AssertContains(playbackObservationText, "private readonly record struct FlashbackPlaybackSnapshotRelevance(");
        AssertContains(playbackObservationText, "private static FlashbackPlaybackSnapshotRelevance BuildPlaybackSnapshotRelevance(");
        AssertContains(playbackObservationText, "private static bool IsPlaybackSnapshotActive(");
        AssertContains(playbackObservationText, "GetInt(snapshot, \"FlashbackPlaybackPendingCommands\") > 0");
        AssertContains(playbackObservationText, "ObservePlaybackOnePercentLow(");
        AssertContains(playbackObservationText, "ObservePlaybackFrameAndDecodeMetrics(metrics, snapshot);");
        AssertContains(playbackObservationText, "ObservePlaybackAudioMasterMetrics(metrics, snapshot);");
        AssertContains(playbackObservationText, "private static void ObservePlaybackOnePercentLow(");
        AssertContains(playbackObservationText, "metrics.OnePercentLowSampleWindowObserved = true;");
        AssertContains(playbackObservationText, "private static void ObservePlaybackFrameAndDecodeMetrics(");
        AssertContains(playbackObservationText, "metrics.MaxDecodePhaseObserved = GetString(snapshot, \"FlashbackPlaybackMaxDecodePhase\") ?? string.Empty;");
        AssertContains(playbackObservationText, "private static void ObservePlaybackAudioMasterMetrics(");
        AssertContains(playbackObservationText, "GetResetAwareCounterDelta(snapshot, metrics.BaselineSnapshot, \"FlashbackPlaybackAudioMasterFallbacks\")");
        AssertContains(playbackResultText, "internal static FlashbackPlaybackResultMetrics BuildFlashbackPlaybackResultMetrics(");
        AssertContains(playbackResultText, "var commands = BuildFlashbackPlaybackResultCommandMetrics(observed, endSnapshot, metrics);");
        AssertContains(playbackResultText, "PendingCommandsAtEnd = commands.PendingCommandsAtEnd,");
        AssertContains(playbackResultText, "private static long GetObservedLong(bool observed, JsonElement snapshot, string propertyName)");
        AssertContains(playbackResultText, "private static double GetObservedDouble(bool observed, JsonElement snapshot, string propertyName)");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultCommandMetrics BuildFlashbackPlaybackResultCommandMetrics(");
        AssertContains(playbackResultText, "PendingCommandsAtEnd: observed ? GetInt(endSnapshot, \"FlashbackPlaybackPendingCommands\") : 0");
        AssertContains(playbackResultText, "LastCommandFailureAtEnd: observed ? GetString(endSnapshot, \"FlashbackPlaybackLastCommandFailure\") ?? string.Empty : string.Empty");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultCadenceMetrics BuildFlashbackPlaybackResultCadenceMetrics(");
        AssertContains(playbackResultText, "DroppedFramesAtEnd: GetObservedLong(observed, endSnapshot, \"FlashbackPlaybackDroppedFrames\")");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultDecodeMetrics BuildFlashbackPlaybackResultDecodeMetrics(");
        AssertContains(playbackResultText, "MaxDecodePhaseAtEnd: observed ? GetString(endSnapshot, \"FlashbackPlaybackMaxDecodePhase\") ?? string.Empty : string.Empty");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultAudioMasterMetrics BuildFlashbackPlaybackResultAudioMasterMetrics(");
        AssertContains(playbackResultText, "AudioMasterFallbacksAtEnd: GetObservedLong(observed, endSnapshot, \"FlashbackPlaybackAudioMasterFallbacks\")");
        AssertContains(playbackResultText, "private static FlashbackPlaybackResultStageMetrics BuildFlashbackPlaybackResultStageMetrics(");
        AssertContains(playbackResultText, "GetCounterDelta(endSnapshot, metrics.BaselineSnapshot, \"FlashbackPlaybackSeekForwardDecodeCapHits\")");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultCommandMetrics(");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultCadenceMetrics(");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultDecodeMetrics(");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultAudioMasterMetrics(");
        AssertContains(playbackResultText, "private readonly record struct FlashbackPlaybackResultStageMetrics(");
        AssertContains(metricsText, "internal static FlashbackExportSessionMetrics BuildFlashbackExportSessionMetrics(");
        AssertContains(metricsText, "metrics.ForceRotateFallbacksAtEnd = GetNullableLong(lastSnapshot, \"FlashbackExportForceRotateFallbacks\") ?? 0;");
        AssertContains(metricsText, "metrics.ForceRotateFallbacksDelta = GetCounterDelta(");
        AssertContains(metricsText, "metrics.LastForceRotateFallbackSegmentsAtEnd =");
        AssertContains(exportText, "private static void ObserveExportSnapshot(");
        AssertContains(exportText, "var relevantToSession =");
        AssertContains(exportText, "metrics.MaxThroughputBytesPerSecObserved = Math.Max(");
        AssertContains(playbackSessionText, "private static void ObservePlaybackOnePercentLow(");
        AssertContains(playbackSessionText, "private static void ObservePlaybackFrameAndDecodeMetrics(");
        AssertContains(playbackSessionText, "private static void ObservePlaybackAudioMasterMetrics(");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionFlashbackMetrics;");
        AssertContains(builderText, "var playbackResultMetrics = BuildFlashbackPlaybackResultMetrics(playbackSessionMetrics);");
        AssertDoesNotContain(runnerText, "private sealed class FlashbackPlaybackSessionMetrics");
        AssertDoesNotContain(runnerText, "GetString(playbackEndSnapshot,");
        AssertDoesNotContain(runnerText, "private sealed class FlashbackExportSessionMetrics");
        AssertDoesNotContain(runnerText, "private static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(");
        AssertDoesNotContain(runnerText, "private static bool IsPlaybackSnapshotActive(");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionFlashbackMetrics_ExportForceRotateCountersIgnoreRelevanceGate()
    {
        var assembly = LoadToolAssemblyIsolated(Path.Combine("tools", "ssctl", "bin", "Debug", "net8.0", "ssctl.dll"));
        var metricsType = assembly.GetType("Sussudio.Tools.DiagnosticSessionFlashbackMetrics")
            ?? throw new InvalidOperationException("Sussudio.Tools.DiagnosticSessionFlashbackMetrics was not found.");
        var sampleType = assembly.GetType("Sussudio.Tools.DiagnosticSessionSample")
            ?? throw new InvalidOperationException("Sussudio.Tools.DiagnosticSessionSample was not found.");
        var buildMetrics = metricsType.GetMethod(
            "BuildFlashbackExportSessionMetrics",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildFlashbackExportSessionMetrics was not found.");

        using var initialDocument = JsonDocument.Parse(
            """
            {
              "FlashbackExportId": 0,
              "FlashbackExportActive": false,
              "FlashbackExportStatus": "NotStarted",
              "FlashbackExportForceRotateFallbacks": 1,
              "FlashbackExportLastForceRotateFallbackSegments": 0
            }
            """);
        using var lastDocument = JsonDocument.Parse(
            """
            {
              "FlashbackExportId": 0,
              "FlashbackExportActive": false,
              "FlashbackExportStatus": "NotStarted",
              "FlashbackExportForceRotateFallbacks": 3,
              "FlashbackExportLastForceRotateFallbackSegments": 2
            }
            """);

        var samples = Array.CreateInstance(sampleType, 0);
        var metrics = buildMetrics.Invoke(
            null,
            new object?[] { initialDocument.RootElement, samples, lastDocument.RootElement })
            ?? throw new InvalidOperationException("BuildFlashbackExportSessionMetrics returned null.");

        AssertEqual(false, (bool)GetPropertyValue(metrics, "Observed")!, "Non-relevant export remains unobserved");
        AssertEqual(3L, Convert.ToInt64(GetPropertyValue(metrics, "ForceRotateFallbacksAtEnd")), "ForceRotateFallbacksAtEnd");
        AssertEqual(2L, Convert.ToInt64(GetPropertyValue(metrics, "ForceRotateFallbacksDelta")), "ForceRotateFallbacksDelta");
        AssertEqual(2, Convert.ToInt32(GetPropertyValue(metrics, "LastForceRotateFallbackSegmentsAtEnd")), "LastForceRotateFallbackSegmentsAtEnd");

        return Task.CompletedTask;
    }
}
