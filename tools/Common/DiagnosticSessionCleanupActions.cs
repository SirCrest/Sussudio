using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionCleanupActions
{
    internal static async Task<DiagnosticSessionCleanupResult> RunAsync(
        DiagnosticSessionOptions options,
        JsonElement initialSnapshot,
        bool startedRecording,
        bool startedPreview,
        bool enabledFlashback,
        bool disabledFlashback,
        bool startedFlashbackPlayback,
        List<string> actions,
        Func<string, Dictionary<string, object?>?, int?, bool, CancellationToken, Task<JsonElement>> sendWithTokenAsync,
        Func<string, int, CancellationToken, Task> tryWaitWithTokenAsync,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        var stoppedRecordingForVerification = false;
        var shouldStopRecordingForVerification = startedRecording && options.VerifyRecording;
        if (startedRecording && (shouldStopRecordingForVerification || !options.LeaveRunning))
        {
            try
            {
                setStage("cleanup-stop-recording");
                const int recordingCleanupTimeoutMs = 300_000;
                using var cleanupCts = CreateCleanupCts(TimeSpan.FromMilliseconds(recordingCleanupTimeoutMs));
                var stopResponse = await sendWithTokenAsync(
                        "SetRecordingEnabled",
                        new Dictionary<string, object?> { ["enabled"] = false },
                        recordingCleanupTimeoutMs,
                        false,
                        cleanupCts.Token)
                    .ConfigureAwait(false);
                actions.Add(shouldStopRecordingForVerification && options.LeaveRunning
                    ? "recording stopped for verification"
                    : "recording stopped");
                stoppedRecordingForVerification = shouldStopRecordingForVerification &&
                                                   IsSuccess(stopResponse);
                if (IsSuccess(stopResponse))
                {
                    await tryWaitWithTokenAsync("RecordingStopped", recordingCleanupTimeoutMs, cleanupCts.Token)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                recordTerminalException(ex, "cleanup-stop-recording");
            }
        }

        if (!options.LeaveRunning)
        {
            await RestoreLiveFlashbackPlaybackAsync(
                    startedFlashbackPlayback,
                    actions,
                    sendWithTokenAsync,
                    setStage,
                    recordTerminalException)
                .ConfigureAwait(false);
            await StopPreviewIfStartedAsync(
                    startedPreview,
                    initialSnapshot,
                    actions,
                    sendWithTokenAsync,
                    setStage,
                    recordTerminalException)
                .ConfigureAwait(false);
            await RestoreFlashbackEnabledStateAsync(
                    enabledFlashback,
                    disabledFlashback,
                    initialSnapshot,
                    actions,
                    sendWithTokenAsync,
                    setStage,
                    recordTerminalException)
                .ConfigureAwait(false);
        }

        return new DiagnosticSessionCleanupResult(stoppedRecordingForVerification);
    }

    private static CancellationTokenSource CreateCleanupCts(TimeSpan timeout)
        => new(timeout);
}
