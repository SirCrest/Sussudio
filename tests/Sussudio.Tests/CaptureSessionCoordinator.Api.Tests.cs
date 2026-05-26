using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task CaptureSessionCoordinator_HasExpectedPublicMethods()
    {
        var coordinatorType = RequireType("Sussudio.Services.Capture.CaptureSessionCoordinator");

        // Core lifecycle methods
        var expectedMethods = new[]
        {
            "InitializeAsync",
            "StartVideoPreviewAsync",
            "StopVideoPreviewAsync",
            "StopVideoPreviewWithTeardownAsync",
            "StartRecordingAsync",
            "StopRecordingAsync",
            "CleanupAsync",
            "StartAudioPreviewAsync",
            "StopAudioPreviewAsync",
            "StopAudioPreviewWithTeardownAsync",
            "UpdateAudioMonitoringAsync",
            "UpdateAudioInputAsync",
            "UpdateMicrophoneMonitorAsync",
            "RestartFlashbackAsync",
            "UpdateRecordingFormatAsync",
            "CycleFlashbackEncoderSettingsAsync",
            "SetFlashbackEnabledAsync",
            "UpdateFlashbackSettingsAsync"
        };

        foreach (var methodName in expectedMethods)
        {
            var method = Array.Find(
                coordinatorType.GetMethods(BindingFlags.Public | BindingFlags.Instance),
                method => method.Name == methodName);
            AssertNotNull(method, $"CaptureSessionCoordinator.{methodName}");
        }

        // Snapshot property
        var snapshotProp = coordinatorType.GetProperty("Snapshot", BindingFlags.Public | BindingFlags.Instance);
        AssertNotNull(snapshotProp, "CaptureSessionCoordinator.Snapshot");

        // Implements IDisposable and IAsyncDisposable
        AssertEqual(true, typeof(IDisposable).IsAssignableFrom(coordinatorType),
            "Implements IDisposable");
        AssertEqual(true, typeof(IAsyncDisposable).IsAssignableFrom(coordinatorType),
            "Implements IAsyncDisposable");

        return Task.CompletedTask;
    }

    // ── CaptureSessionCoordinator: CaptureCommand shape ──

    internal static Task CaptureSessionCoordinator_CaptureCommandKind_HasExpectedValues()
    {
        var commandKindType = RequireType("Sussudio.Services.Capture.CaptureCommandKind");

        // Core command kinds should exist
        var expectedValues = new[]
        {
            "Initialize", "StartVideoPreview", "StopVideoPreview",
            "StartRecording", "StopRecording", "Cleanup",
            "StartAudioPreview", "StopAudioPreview",
            "UpdateAudioMonitoring", "UpdateAudioInput",
            "UpdateMicrophoneMonitor",
            "SetFlashbackEnabled", "UpdateFlashbackSettings",
            "RestartFlashback", "UpdateFlashbackRecordingFormat",
            "CycleFlashbackEncoderSettings"
        };

        foreach (var value in expectedValues)
        {
            var parsed = Enum.Parse(commandKindType, value);
            AssertNotNull(parsed, $"CaptureCommandKind.{value}");
        }

        return Task.CompletedTask;
    }

    // ── CaptureSessionCoordinator: CaptureSessionSnapshot ──

    internal static Task CaptureSessionCoordinator_CaptureSessionSnapshot_HasFullContract()
    {
        var snapshotType = RequireType("Sussudio.Services.Capture.CaptureSessionSnapshot");

        var expectedProps = new[]
        {
            "LastTransitionUtc", "LastCommand", "LastCorrelationId",
            "LastError", "CommandsEnqueued", "CommandsCompleted",
            "CommandsFailed", "CommandsCanceled", "CommandsCoalesced", "PendingCommands",
            "MaxPendingCommands", "OldestPendingCommandAgeMs",
            "LastCommandQueueLatencyMs", "MaxCommandQueueLatencyMs", "LastOutcome", "SessionState",
            "IsRecording", "IsInitialized", "IsVideoPreviewActive", "IsAudioPreviewActive"
        };

        foreach (var prop in expectedProps)
        {
            var propInfo = snapshotType.GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            AssertNotNull(propInfo, $"CaptureSessionSnapshot.{prop}");
        }

        // Default state should be clean
        var snapshot = Activator.CreateInstance(snapshotType)!;
        AssertEqual(false, GetBoolProperty(snapshot, "IsRecording"), "Default IsRecording");
        AssertEqual(false, GetBoolProperty(snapshot, "IsInitialized"), "Default IsInitialized");
        AssertEqual(0, Convert.ToInt32(GetPropertyValue(snapshot, "PendingCommands")), "Default PendingCommands");
        AssertEqual(0, Convert.ToInt32(GetPropertyValue(snapshot, "MaxPendingCommands")), "Default MaxPendingCommands");
        AssertEqual(0L, Convert.ToInt64(GetPropertyValue(snapshot, "OldestPendingCommandAgeMs")), "Default OldestPendingCommandAgeMs");
        AssertEqual(0L, Convert.ToInt64(GetPropertyValue(snapshot, "MaxCommandQueueLatencyMs")), "Default MaxCommandQueueLatencyMs");
        AssertEqual(0L, Convert.ToInt64(GetPropertyValue(snapshot, "CommandsCoalesced")), "Default CommandsCoalesced");
        AssertEqual("None", GetStringProperty(snapshot, "LastOutcome"), "Default LastOutcome");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionSnapshot_DefaultState()
    {
        var snapshotType = RequireType("Sussudio.Services.Capture.CaptureSessionSnapshot");
        var snapshot = RuntimeHelpers.GetUninitializedObject(snapshotType);

        AssertEqual(false, GetBoolProperty(snapshot, "IsRecording"), "IsRecording default");
        AssertEqual(false, GetBoolProperty(snapshot, "IsInitialized"), "IsInitialized default");
        AssertEqual(false, GetBoolProperty(snapshot, "IsVideoPreviewActive"), "IsVideoPreviewActive default");
        AssertEqual(false, GetBoolProperty(snapshot, "IsAudioPreviewActive"), "IsAudioPreviewActive default");
        AssertEqual(0, (int)GetPropertyValue(snapshot, "PendingCommands")!, "PendingCommands default");
        AssertEqual(0L, GetLongProperty(snapshot, "CommandsCoalesced"), "CommandsCoalesced default");
        AssertEqual("None", GetStringProperty(snapshot, "LastOutcome"), "LastOutcome default");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_ModelsLiveInFocusedFile()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var modelText = rootText;

        AssertContains(modelText, "public enum CaptureCommandKind");
        AssertContains(modelText, "public enum CaptureCommandOutcome");
        AssertContains(modelText, "public readonly record struct CaptureCommand(");
        AssertContains(modelText, "public sealed class CaptureSessionSnapshot");
        AssertContains(modelText, "internal readonly record struct FlashbackPlaybackSnapshot(");
        AssertContains(modelText, "internal readonly record struct FlashbackBufferStatus(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionCoordinator.Models.cs")),
            "coordinator model surface folded into the coordinator root");

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionCoordinator_FlashbackFacadeLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.cs")
            .Replace("\r\n", "\n");
        var flashbackText = ReadRepoFile("Sussudio/Services/Capture/CaptureSessionCoordinator.Flashback.cs")
            .Replace("\r\n", "\n");
        var flashbackStatusText = flashbackText;
        var flashbackExportText = flashbackText;
        var flashbackGuardsText = flashbackText;

        AssertContains(flashbackText, "public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)");
        AssertContains(flashbackText, "public Task UpdateRecordingFormatAsync(RecordingFormat format, CancellationToken cancellationToken = default)");
        AssertContains(flashbackText, "public Task CycleFlashbackEncoderSettingsAsync(");
        AssertContains(flashbackText, "public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)");
        AssertContains(flashbackStatusText, "internal FlashbackBufferStatus GetFlashbackBufferStatus()");
        AssertContains(flashbackStatusText, "internal FlashbackPlaybackSnapshot GetFlashbackPlaybackSnapshot()");
        AssertContains(flashbackExportText, "internal Task<FinalizeResult> ExportFlashbackRangeAsync(");
        AssertContains(flashbackExportText, "internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()");
        AssertContains(flashbackGuardsText, "private bool TryGetActiveFlashback(");
        AssertDoesNotContain(rootText, "public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(rootText, "public Task CycleFlashbackEncoderSettingsAsync(");
        AssertDoesNotContain(rootText, "internal FlashbackPlaybackSnapshot GetFlashbackPlaybackSnapshot()");
        AssertDoesNotContain(rootText, "private bool TryGetActiveFlashback(");

        return Task.CompletedTask;
    }
}
