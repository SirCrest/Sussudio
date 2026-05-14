using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    private static Task DiagnosticSessionRunner_IgnoresTransientFlashbackWarmupWarnings()
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

    private static Task DiagnosticSessionFlashbackCycleScenarios_OwnCycleFlows()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioStartup.cs")
            .Replace("\r\n", "\n");
        var cyclesText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackCycleScenarios.cs")
            .Replace("\r\n", "\n");

        AssertContains(cyclesText, "internal static class DiagnosticSessionFlashbackCycleScenarios");
        AssertContains(cyclesText, "internal static async Task RunFlashbackRestartCycleAsync(");
        AssertContains(cyclesText, "\"RestartFlashback\"");
        AssertContains(cyclesText, "\"flashback-restart-cycle-export.mp4\"");
        AssertContains(cyclesText, "flashback restart cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackEncoderCycleAsync(");
        AssertContains(cyclesText, "var cycledPreset = string.Equals(originalPreset, \"P1\", StringComparison.OrdinalIgnoreCase) ? \"P2\" : \"P1\";");
        AssertContains(cyclesText, "\"flashback-encoder-cycle-export.mp4\"");
        AssertContains(cyclesText, "flashback encoder preset restored to");
        AssertContains(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackCycleScenarios;");
        AssertContains(startupText, "RunFlashbackRestartCycleAsync(");
        AssertContains(startupText, "RunFlashbackEncoderCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRestartCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackEncoderCycleAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackMetrics_OwnsFlashbackSessionMetricProjection()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var metricsText = ReadDiagnosticSessionFlashbackMetricsSource();

        AssertContains(metricsText, "internal static partial class DiagnosticSessionFlashbackMetrics");
        AssertContains(metricsText, "internal sealed class FlashbackRecordingSessionMetrics");
        AssertContains(metricsText, "internal sealed class FlashbackPlaybackSessionMetrics");
        AssertContains(metricsText, "internal sealed class FlashbackPlaybackResultMetrics");
        AssertContains(metricsText, "internal sealed class FlashbackExportSessionMetrics");
        AssertContains(metricsText, "internal static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(");
        AssertContains(metricsText, "internal static FlashbackPlaybackSessionMetrics BuildFlashbackPlaybackSessionMetrics(");
        AssertContains(metricsText, "internal static FlashbackPlaybackResultMetrics BuildFlashbackPlaybackResultMetrics(");
        AssertContains(metricsText, "internal static FlashbackExportSessionMetrics BuildFlashbackExportSessionMetrics(");
        AssertContains(metricsText, "private static bool IsPlaybackSnapshotActive(");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionFlashbackMetrics;");
        AssertContains(builderText, "var playbackResultMetrics = BuildFlashbackPlaybackResultMetrics(playbackSessionMetrics);");
        AssertDoesNotContain(runnerText, "private sealed class FlashbackPlaybackSessionMetrics");
        AssertDoesNotContain(runnerText, "GetString(playbackEndSnapshot,");
        AssertDoesNotContain(runnerText, "private sealed class FlashbackExportSessionMetrics");
        AssertDoesNotContain(runnerText, "private static FlashbackRecordingSessionMetrics BuildFlashbackRecordingMetrics(");
        AssertDoesNotContain(runnerText, "private static bool IsPlaybackSnapshotActive(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackPreviewCycleScenarios_OwnPreviewCycleFlows()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioStartup.cs")
            .Replace("\r\n", "\n");
        var cyclesText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackPreviewCycleScenarios.cs")
            .Replace("\r\n", "\n");

        AssertContains(cyclesText, "internal static class DiagnosticSessionFlashbackPreviewCycleScenarios");
        AssertContains(cyclesText, "internal static async Task RunFlashbackPreviewCycleAsync(");
        AssertContains(cyclesText, "\"flashback-preview-off-export.mp4\"");
        AssertContains(cyclesText, "flashback preview cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackPlaybackPreviewCycleAsync(");
        AssertContains(cyclesText, "flashback playback preview cycle preview stopped during playback");
        AssertContains(cyclesText, "flashback playback preview cycle export verified");
        AssertContains(cyclesText, "internal static async Task RunFlashbackRecordingPreviewCycleAsync(");
        AssertContains(cyclesText, "flashback recording preview cycle preview stopped");
        AssertContains(cyclesText, "internal static bool IsPreviewCycleScenario(");
        AssertContains(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackPreviewCycleScenarios;");
        AssertContains(startupText, "RunFlashbackPreviewCycleAsync(");
        AssertContains(startupText, "RunFlashbackPlaybackPreviewCycleAsync(");
        AssertContains(startupText, "RunFlashbackRecordingPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackPlaybackPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRecordingPreviewCycleAsync(");
        AssertDoesNotContain(runnerText, "private static bool IsPreviewCycleScenario(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackRejectedExports_OwnRejectionFlows()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var rejectedText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRejectedExports.cs")
            .Replace("\r\n", "\n");

        AssertContains(rejectedText, "internal static class DiagnosticSessionFlashbackRejectedExports");
        AssertContains(rejectedText, "internal static async Task RunFlashbackExportRejectedAsync(");
        AssertContains(rejectedText, "\"flashback-rejected-export.mp4\"");
        AssertContains(rejectedText, "BufferInactive");
        AssertContains(rejectedText, "Flashback buffer not active");
        AssertContains(rejectedText, "internal static async Task RunFlashbackRecordingExportRejectedAsync(");
        AssertContains(rejectedText, "\"flashback-recording-rejected-export.mp4\"");
        AssertContains(rejectedText, "UnavailableDuringRecording");
        AssertContains(rejectedText, "recording backend changed after rejected export");
        AssertContains(runnerText, "using static Sussudio.Tools.DiagnosticSessionFlashbackRejectedExports;");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackExportRejectedAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRecordingExportRejectedAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackSegmentPlaybackScenarios_OwnSegmentPlaybackFlow()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioStartup.cs")
            .Replace("\r\n", "\n");
        var segmentPlaybackText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs")
            .Replace("\r\n", "\n");

        AssertContains(segmentPlaybackText, "internal static class DiagnosticSessionFlashbackSegmentPlaybackScenarios");
        AssertContains(segmentPlaybackText, "internal static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertContains(segmentPlaybackText, "flashback segment playback live headroom established");
        AssertContains(segmentPlaybackText, "flashback segment playback started near boundary");
        AssertContains(segmentPlaybackText, "frameCount >= 180");
        AssertContains(segmentPlaybackText, "playback FPS below source-rate target after warm sample");
        AssertContains(segmentPlaybackText, "private static async Task<bool> CreateFlashbackCompletedSegmentViaRecordingAsync(");
        AssertContains(segmentPlaybackText, "recording-assisted rotation started");
        AssertContains(segmentPlaybackText, "private static async Task TryStopRecordingAsync(");
        AssertContains(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackSegmentPlaybackScenarios;");
        AssertContains(startupText, "RunFlashbackSegmentPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackSegmentPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<bool> CreateFlashbackCompletedSegmentViaRecordingAsync(");
        AssertDoesNotContain(runnerText, "private static async Task TryStopRecordingAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackRecordingSettingsScenarios_OwnDeferredSettingsFlow()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioStartup.cs")
            .Replace("\r\n", "\n");
        var recordingChecksText = ReadRepoFile("tools/Common/DiagnosticSessionRecordingChecks.cs")
            .Replace("\r\n", "\n");
        var settingsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackRecordingSettingsScenarios.cs")
            .Replace("\r\n", "\n");

        AssertContains(settingsText, "internal readonly record struct FlashbackRecordingSettingsDeferredPresetState(");
        AssertContains(settingsText, "internal static class DiagnosticSessionFlashbackRecordingSettingsScenarios");
        AssertContains(settingsText, "internal static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(settingsText, "flashback recording settings deferred preset changed to");
        AssertContains(settingsText, "RestartFlashback unexpectedly succeeded during recording");
        AssertContains(settingsText, "SetFlashbackEnabled(false) unexpectedly succeeded during recording");
        AssertContains(settingsText, "internal static async Task VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertContains(settingsText, "flashback recording settings deferred post-stop buffer verified");
        AssertContains(settingsText, "flashback recording settings deferred preset restored to");
        AssertContains(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;");
        AssertContains(startupText, "RunFlashbackRecordingSettingsDeferredAsync(");
        AssertContains(recordingChecksText, "using static Sussudio.Tools.DiagnosticSessionFlashbackRecordingSettingsScenarios;");
        AssertContains(recordingChecksText, "VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertDoesNotContain(runnerText, "private static async Task<FlashbackRecordingSettingsDeferredPresetState> RunFlashbackRecordingSettingsDeferredAsync(");
        AssertDoesNotContain(runnerText, "private static async Task VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertDoesNotContain(runnerText, "private readonly record struct FlashbackRecordingSettingsDeferredPresetState(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackLifecycleScenarios_OwnLifecycleFlow()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioStartup.cs")
            .Replace("\r\n", "\n");
        var lifecycleText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackLifecycleScenarios.cs")
            .Replace("\r\n", "\n");

        AssertContains(lifecycleText, "internal static class DiagnosticSessionFlashbackLifecycleScenarios");
        AssertContains(lifecycleText, "internal static async Task RunFlashbackLifecycleAsync(");
        AssertContains(lifecycleText, "flashback lifecycle pause requested");
        AssertContains(lifecycleText, "flashback lifecycle disabled during playback");
        AssertContains(lifecycleText, "flashback lifecycle: playback worker still alive after disable");
        AssertContains(lifecycleText, "flashback lifecycle: pending commands remained after disable");
        AssertContains(lifecycleText, "flashback lifecycle re-enabled");
        AssertContains(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackLifecycleScenarios;");
        AssertContains(startupText, "RunFlashbackLifecycleAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackLifecycleAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackExportScenarios_OwnExportFlows()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioStartup.cs")
            .Replace("\r\n", "\n");
        var scenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();

        AssertContains(scenariosText, "internal static partial class DiagnosticSessionFlashbackExportScenarios");
        AssertContains(scenariosText, "internal static async Task RunFlashbackExportConcurrentAsync(");
        AssertContains(scenariosText, "\"flashback-concurrent-a.mp4\"");
        AssertContains(scenariosText, "flashback concurrent exports verified");
        AssertContains(scenariosText, "internal static async Task RunFlashbackDisableDuringExportAsync(");
        AssertContains(scenariosText, "\"flashback-disable-during-export.mp4\"");
        AssertContains(scenariosText, "SendCommandWithConnectRetryAsync(");
        AssertContains(scenariosText, "internal static async Task RunFlashbackRotatedExportAsync(");
        AssertContains(scenariosText, "TryParseFlashbackExportSegmentCount(exportMessage)");
        AssertContains(scenariosText, "internal static async Task RunFlashbackExportPlaybackAsync(");
        AssertContains(scenariosText, "flashback export during playback verified");
        AssertContains(scenariosText, "internal static async Task RunFlashbackRangeExportAsync(");
        AssertContains(scenariosText, "[\"useSelectionRange\"] = true");
        AssertContains(startupText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExportScenarios;");
        AssertContains(startupText, "RunFlashbackExportConcurrentAsync(");
        AssertContains(startupText, "RunFlashbackDisableDuringExportAsync(");
        AssertContains(startupText, "RunFlashbackRotatedExportAsync(");
        AssertContains(startupText, "RunFlashbackExportPlaybackAsync(");
        AssertContains(startupText, "RunFlashbackRangeExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackExportConcurrentAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackDisableDuringExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRotatedExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackExportPlaybackAsync(");
        AssertDoesNotContain(runnerText, "private static async Task RunFlashbackRangeExportAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackExports_OwnsExportHelpers()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var exportScenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var stressText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackStressScenario.cs")
            .Replace("\r\n", "\n");
        var exportsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackExports.cs")
            .Replace("\r\n", "\n");

        AssertContains(exportsText, "internal static class DiagnosticSessionFlashbackExports");
        AssertContains(exportsText, "internal static int? TryParseFlashbackExportSegmentCount(");
        AssertContains(exportsText, "const string marker = \" from \";");
        AssertContains(exportsText, "suffix.Contains(\"segment\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(exportsText, "internal static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(string filePath)");
        AssertContains(exportsText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertContains(exportsText, "internal static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertContains(exportsText, "\"SetAudioEnabled\"");
        AssertContains(exportsText, "internal static async Task CleanupFlashbackSelectionAsync(");
        AssertContains(exportsText, "\"clear-in-out-points\"");
        AssertContains(exportsText, "\"go-live\"");
        AssertContains(exportScenariosText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExports;");
        AssertContains(stressText, "using static Sussudio.Tools.DiagnosticSessionFlashbackExports;");
        AssertDoesNotContain(runnerText, "private static int? TryParseFlashbackExportSegmentCount(");
        AssertDoesNotContain(runnerText, "private static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(");
        AssertDoesNotContain(runnerText, "private static async Task ToggleAudioEnabledDuringFlashbackExportAsync(");
        AssertDoesNotContain(runnerText, "private static async Task CleanupFlashbackSelectionAsync(");

        return Task.CompletedTask;
    }

    private static Task DiagnosticSessionFlashbackSegments_OwnsSegmentWaitsAndParsing()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var exportScenariosText = ReadDiagnosticSessionFlashbackExportScenariosSource();
        var segmentPlaybackText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegmentPlaybackScenarios.cs")
            .Replace("\r\n", "\n");
        var segmentsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackSegments.cs")
            .Replace("\r\n", "\n");

        AssertContains(segmentsText, "internal static class DiagnosticSessionFlashbackSegments");
        AssertContains(segmentsText, "internal readonly record struct FlashbackSegmentProbe(");
        AssertContains(segmentsText, "internal readonly record struct FlashbackSegmentPlaybackTarget(");
        AssertContains(segmentsText, "internal static async Task<FlashbackSegmentProbe?> WaitForFlashbackCompletedSegmentAsync(");
        AssertContains(segmentsText, "internal static bool TryGetFlashbackSegments(");
        AssertContains(segmentsText, "internal static async Task<FlashbackSegmentPlaybackTarget?> WaitForFlashbackPlayableCompletedSegmentAsync(");
        AssertContains(segmentsText, "internal static async Task<bool> WaitForFlashbackSegmentPlaybackHeadroomAsync(");
        AssertContains(segmentsText, "\"FlashbackGetSegments\"");
        AssertContains(segmentsText, "data.TryGetProperty(\"Segments\", out var segmentsElement)");
        AssertContains(segmentsText, "const int requiredHeadroomMs = 8_000;");
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

    private static Task DiagnosticSessionFlashbackStressScenario_OwnsStressFlow()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var startupText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioStartup.cs")
            .Replace("\r\n", "\n");
        var stressText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackStressScenario.cs")
            .Replace("\r\n", "\n");

        AssertContains(stressText, "internal static class DiagnosticSessionFlashbackStressScenario");
        AssertContains(stressText, "internal const int FlashbackStressMaxPlaybackPendingCommands = 4;");
        AssertContains(stressText, "internal const int FlashbackStressMaxPlaybackCommandLatencyMs = 750;");
        AssertContains(stressText, "internal const double FlashbackStressPlaybackWarmSeconds = 10.0;");
        AssertContains(stressText, "internal const long FlashbackStressAudioUnavailableFallbackAllowance = 4;");
        AssertContains(stressText, "internal const int FlashbackScrubStressMaxPlaybackPendingCommands = 20;");
        AssertContains(stressText, "internal static async Task RunFlashbackStressAsync(");
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

    private static Task DiagnosticSessionFlashbackWaits_OwnsSnapshotPollingWaits()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var setupText = ReadRepoFile("tools/Common/DiagnosticSessionScenarioSetup.cs")
            .Replace("\r\n", "\n");
        var waitsText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackWaits.cs")
            .Replace("\r\n", "\n");

        AssertContains(waitsText, "internal static class DiagnosticSessionFlashbackWaits");
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

    private static Task DiagnosticSessionFlashbackValidation_OwnsFlashbackWarningPolicy()
    {
        var runnerText = ReadRepoFile("tools/Common/DiagnosticSessionRunner.cs")
            .Replace("\r\n", "\n");
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var validationText = ReadRepoFile("tools/Common/DiagnosticSessionFlashbackValidation.cs")
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
