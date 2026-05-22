using System.Threading.Tasks;

static partial class Program
{
    internal static Task DiagnosticSessionCleanupPolicy_OwnsRestoreWarnings()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var cleanupActionsText = ReadDiagnosticSessionCleanupActionsSource();
        var cleanupActionsRootText = ReadRepoFile("tools/Common/DiagnosticSessionCleanupActions.cs")
            .Replace("\r\n", "\n");
        var cleanupRecordingText = ReadRepoFile("tools/Common/DiagnosticSessionCleanupActions.Recording.cs")
            .Replace("\r\n", "\n");
        var cleanupFlashbackPlaybackText = ReadRepoFile("tools/Common/DiagnosticSessionCleanupActions.FlashbackPlayback.cs")
            .Replace("\r\n", "\n");
        var cleanupPreviewText = ReadRepoFile("tools/Common/DiagnosticSessionCleanupActions.Preview.cs")
            .Replace("\r\n", "\n");
        var cleanupFlashbackStateText = ReadRepoFile("tools/Common/DiagnosticSessionCleanupActions.FlashbackState.cs")
            .Replace("\r\n", "\n");
        var cleanupText = ReadRepoFile("tools/Common/DiagnosticSessionCleanupPolicy.cs")
            .Replace("\r\n", "\n");

        AssertContains(cleanupActionsText, "internal static partial class DiagnosticSessionCleanupActions");
        AssertContains(cleanupActionsText, "internal static async Task<DiagnosticSessionCleanupResult> RunAsync(");
        AssertContains(cleanupActionsText, "internal readonly record struct DiagnosticSessionCleanupResult(bool StoppedRecordingForVerification)");
        AssertContains(cleanupActionsRootText, "StopRecordingForCleanupAsync(");
        AssertContains(cleanupRecordingText, "private static async Task<bool> StopRecordingForCleanupAsync(");
        AssertContains(cleanupRecordingText, "setStage(\"cleanup-stop-recording\")");
        AssertContains(cleanupRecordingText, "recordTerminalException(ex, \"cleanup-stop-recording\")");
        AssertContains(cleanupFlashbackPlaybackText, "private static async Task RestoreLiveFlashbackPlaybackAsync(");
        AssertContains(cleanupFlashbackPlaybackText, "setStage(\"cleanup-go-live\")");
        AssertContains(cleanupPreviewText, "private static async Task StopPreviewIfStartedAsync(");
        AssertContains(cleanupPreviewText, "setStage(\"cleanup-stop-preview\")");
        AssertContains(cleanupFlashbackStateText, "private static async Task RestoreFlashbackEnabledStateAsync(");
        AssertContains(cleanupFlashbackStateText, "setStage(\"cleanup-restore-flashback-off\")");
        AssertContains(cleanupFlashbackStateText, "setStage(\"cleanup-restore-flashback-on\")");
        AssertContains(cleanupActionsText, "using Sussudio.Models;");
        AssertContains(cleanupActionsText, "DiagnosticSessionCommandChannel commandChannel,");
        AssertContains(cleanupActionsText, "commandChannel.SendWithTokenAsync(");
        AssertContains(cleanupActionsText, "AutomationCommandKind.SetRecordingEnabled,");
        AssertContains(cleanupActionsText, "AutomationCommandKind.FlashbackAction,");
        AssertContains(cleanupActionsText, "AutomationCommandKind.SetPreviewEnabled,");
        AssertContains(cleanupActionsText, "AutomationCommandKind.SetFlashbackEnabled,");
        AssertContains(cleanupActionsText, "AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.SetFlashbackEnabled)");
        AssertContains(cleanupText, "internal static class DiagnosticSessionCleanupPolicy");
        AssertContains(cleanupText, "internal static void ValidateCleanupLifecycleRestored(");
        AssertContains(cleanupText, "cleanup: preview remained active after restore");
        AssertContains(cleanupText, "cleanup: Flashback remained active after restore");
        AssertContains(cleanupText, "cleanup: playback did not return live state={state}");
        AssertContains(runnerText, "DiagnosticSessionCleanupActions.RunAsync(");
        AssertContains(runnerText, "runContext.CommandChannel,");
        AssertContains(runnerText, "stoppedRecordingForVerification = cleanupResult.StoppedRecordingForVerification;");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionCleanupPolicy;");
        AssertDoesNotContain(runnerText, "setStage(\"cleanup-stop-recording\")");
        AssertDoesNotContain(runnerText, "setStage(\"cleanup-go-live\")");
        AssertDoesNotContain(runnerText, "setStage(\"cleanup-stop-preview\")");
        AssertDoesNotContain(runnerText, "setStage(\"cleanup-restore-flashback-off\")");
        AssertDoesNotContain(runnerText, "private static void ValidateCleanupLifecycleRestored(");
        AssertDoesNotContain(cleanupActionsText, "sendWithTokenAsync(\"SetRecordingEnabled\"");
        AssertDoesNotContain(cleanupActionsText, "sendWithTokenAsync(\"FlashbackAction\"");
        AssertDoesNotContain(cleanupActionsText, "sendWithTokenAsync(\"SetPreviewEnabled\"");
        AssertDoesNotContain(cleanupActionsText, "sendWithTokenAsync(\"SetFlashbackEnabled\"");
        AssertDoesNotContain(cleanupActionsText, "GetDefaultResponseTimeout(\"SetFlashbackEnabled\")");
        AssertDoesNotContain(cleanupActionsRootText, "setStage(\"cleanup-stop-recording\")");
        AssertDoesNotContain(cleanupActionsRootText, "recordTerminalException(ex, \"cleanup-stop-recording\")");
        AssertDoesNotContain(cleanupRecordingText, "setStage(\"cleanup-go-live\")");
        AssertDoesNotContain(cleanupRecordingText, "setStage(\"cleanup-stop-preview\")");
        AssertDoesNotContain(cleanupRecordingText, "setStage(\"cleanup-restore-flashback-off\")");
        AssertDoesNotContain(cleanupRecordingText, "setStage(\"cleanup-restore-flashback-on\")");
        AssertDoesNotContain(cleanupFlashbackPlaybackText, "SetPreviewEnabled");
        AssertDoesNotContain(cleanupFlashbackPlaybackText, "SetFlashbackEnabled");
        AssertDoesNotContain(cleanupPreviewText, "FlashbackAction");
        AssertDoesNotContain(cleanupPreviewText, "SetFlashbackEnabled");
        AssertDoesNotContain(cleanupFlashbackStateText, "FlashbackAction");
        AssertDoesNotContain(cleanupFlashbackStateText, "SetPreviewEnabled");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionRecordingChecks_OwnPostRunRecordingVerification()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var recordingChecksText = ReadRepoFile("tools/Common/DiagnosticSessionRecordingChecks.cs")
            .Replace("\r\n", "\n");
        var recordingVerificationText = ReadRepoFile("tools/Common/DiagnosticSessionRecordingVerification.cs")
            .Replace("\r\n", "\n");

        AssertContains(recordingChecksText, "internal static class DiagnosticSessionRecordingChecks");
        AssertContains(recordingChecksText, "internal static async Task<DiagnosticSessionRecordingCheckResult> RunAsync(");
        AssertContains(recordingChecksText, "internal readonly record struct DiagnosticSessionRecordingCheckResult(JsonElement? Verification)");
        AssertContains(recordingChecksText, "setStage(\"settings-deferred-restore\")");
        AssertContains(recordingChecksText, "VerifyAndRestoreFlashbackRecordingSettingsAfterStopAsync(");
        AssertContains(recordingChecksText, "DiagnosticSessionRecordingVerification.RunAsync(");
        AssertContains(recordingChecksText, "verification = await DiagnosticSessionRecordingVerification.RunAsync(");
        AssertContains(recordingChecksText, "setStage(\"recording-validation\")");
        AssertContains(recordingChecksText, "ValidateFlashbackRecordingSession(initialSnapshot, samples, warnings)");
        AssertContains(recordingVerificationText, "internal static class DiagnosticSessionRecordingVerification");
        AssertContains(recordingVerificationText, "internal static async Task<JsonElement?> RunAsync(");
        AssertContains(recordingVerificationText, "DiagnosticSessionScenarioCatalog.TryGetFlashbackExportVerificationPath(");
        AssertContains(recordingVerificationText, "setStage(\"recording-verification\")");
        AssertContains(recordingVerificationText, "var verificationCommand = \"VerifyLastRecording\";");
        AssertContains(recordingVerificationText, "verificationCommand = \"VerifyFile\";");
        AssertContains(recordingVerificationText, "[\"strict\"] = true");
        AssertContains(recordingVerificationText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertContains(recordingVerificationText, "sendAsync(verificationCommand, verificationPayload, 60_000)");
        AssertContains(recordingVerificationText, "return verificationElement.Clone();");
        AssertContains(recordingVerificationText, "recording verification skipped: scenario does not produce a recording or export artifact");
        AssertContains(recordingVerificationText, "recordTerminalException(ex, \"recording-verification\")");
        AssertContains(runnerText, "DiagnosticSessionRecordingChecks.RunAsync(");
        AssertDoesNotContain(runnerText, "SetStage(\"settings-deferred-restore\")");
        AssertDoesNotContain(recordingChecksText, "var verificationCommand = \"VerifyLastRecording\"");
        AssertDoesNotContain(runnerText, "DiagnosticSessionScenarioCatalog.TryGetFlashbackExportVerificationPath(");
        AssertDoesNotContain(recordingChecksText, "[\"verificationProfile\"] = \"flashback-export\"");
        AssertDoesNotContain(runnerText, "ValidateFlashbackRecordingSession(initialSnapshot, samples, warnings)");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionPostRunSnapshots_OwnTimelineAndFinalSnapshot()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var postRunText = ReadRepoFile("tools/Common/DiagnosticSessionPostRunSnapshots.cs")
            .Replace("\r\n", "\n");

        AssertContains(postRunText, "internal static class DiagnosticSessionPostRunSnapshots");
        AssertContains(postRunText, "internal static async Task<DiagnosticSessionPostRunSnapshotResult> CaptureAsync(");
        AssertContains(postRunText, "internal readonly record struct DiagnosticSessionPostRunSnapshotResult(");
        AssertContains(postRunText, "JsonElement HealthSnapshot,");
        AssertContains(postRunText, "setStage(\"timeline\")");
        AssertContains(postRunText, "\"GetPerformanceTimeline\"");
        AssertContains(postRunText, "new Dictionary<string, object?> { [\"maxEntries\"] = 240 }");
        AssertContains(postRunText, "recordTerminalException(ex, \"timeline\")");
        AssertContains(postRunText, "setStage(\"final-snapshot\")");
        AssertContains(postRunText, "sendAsync(\"GetSnapshot\", null, null)");
        AssertContains(postRunText, "TryGetSnapshot(finalSnapshotResponse, out var finalSnapshot)");
        AssertContains(postRunText, "recordTerminalException(ex, \"final-snapshot\")");
        AssertContains(runnerText, "DiagnosticSessionPostRunSnapshots.CaptureAsync(");
        AssertContains(runnerText, "postRunSnapshots.HealthSnapshot");
        AssertContains(runnerText, "postRunSnapshots.Timeline");
        AssertDoesNotContain(runnerText, "SetStage(\"timeline\")");
        AssertDoesNotContain(runnerText, "\"GetPerformanceTimeline\"");
        AssertDoesNotContain(runnerText, "RecordTerminalException(ex, \"final-snapshot\")");

        return Task.CompletedTask;
    }

    internal static Task DiagnosticSessionMetrics_OwnsSessionMetricProjection()
    {
        var runnerText = ReadDiagnosticSessionRunnerSource();
        var builderText = ReadDiagnosticSessionResultBuilderSource();
        var metricsText = ReadDiagnosticSessionMetricsSource();

        AssertContains(metricsText, "internal static class DiagnosticSessionMetrics");
        AssertContains(metricsText, "internal sealed class SourceCadenceSessionMetrics");
        AssertContains(metricsText, "internal sealed class PreviewCadenceSessionMetrics");
        AssertContains(metricsText, "internal sealed class VisualCadenceSessionMetrics");
        AssertContains(metricsText, "internal sealed class PreviewD3DMetrics");
        AssertContains(metricsText, "internal readonly record struct PlaybackCommandHealth(");
        AssertContains(metricsText, "internal static SourceCadenceSessionMetrics BuildSourceCadenceSessionMetrics(");
        AssertContains(metricsText, "internal static PreviewCadenceSessionMetrics BuildPreviewCadenceSessionMetrics(");
        AssertContains(metricsText, "internal static VisualCadenceSessionMetrics BuildVisualCadenceSessionMetrics(");
        AssertContains(metricsText, "private static void ObserveSourceCadenceSnapshot(");
        AssertContains(metricsText, "private static void ObservePreviewCadenceSnapshot(");
        AssertContains(metricsText, "internal static bool IsVisualCadenceSessionHealthy(");
        AssertContains(metricsText, "private static void ObserveVisualCadenceSnapshot(");
        AssertContains(metricsText, "internal static PreviewD3DMetrics BuildPreviewD3DMetrics(");
        AssertContains(metricsText, "CountArrayItems(sample.Snapshot, \"PreviewD3DRecentSlowFrames\")");
        AssertContains(metricsText, "private static void ObservePreviewD3DCpuTiming(PreviewD3DMetrics metrics, JsonElement snapshot)");
        AssertContains(metricsText, "private static void ApplySlowFrame(PreviewD3DMetrics metrics, JsonElement slowFrame)");
        AssertContains(metricsText, "private static bool TryGetLatestSlowFrame(JsonElement snapshot, out JsonElement slowFrame)");
        AssertContains(metricsText, "internal static PlaybackCommandHealth BuildPlaybackCommandHealth(");
        AssertContains(metricsText, "internal static long GetResetAwareCounterDelta(");
        AssertContains(metricsText, "internal static bool IsVisualCadenceSessionHealthy(");
        AssertContains(builderText, "using static Sussudio.Tools.DiagnosticSessionMetrics;");
        AssertDoesNotContain(runnerText, "private sealed class SourceCadenceSessionMetrics");
        AssertDoesNotContain(runnerText, "private sealed class PreviewD3DMetrics");
        AssertDoesNotContain(runnerText, "private static PlaybackCommandHealth BuildPlaybackCommandHealth(");
        AssertDoesNotContain(runnerText, "private static long GetCounterDelta(");

        return Task.CompletedTask;
    }
}
