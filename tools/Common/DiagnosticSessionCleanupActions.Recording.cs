using Sussudio.Models;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionCleanupActions
{
    private static async Task<bool> StopRecordingForCleanupAsync(
        DiagnosticSessionOptions options,
        bool startedRecording,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, CancellationToken, Task> tryWaitWithTokenAsync,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        var shouldStopRecordingForVerification = startedRecording && options.VerifyRecording;
        if (!startedRecording || (!shouldStopRecordingForVerification && options.LeaveRunning))
        {
            return false;
        }

        try
        {
            setStage("cleanup-stop-recording");
            const int recordingCleanupTimeoutMs = 300_000;
            using var cleanupCts = CreateCleanupCts(TimeSpan.FromMilliseconds(recordingCleanupTimeoutMs));
            var stopResponse = await commandChannel.SendWithTokenAsync(
                    AutomationCommandKind.SetRecordingEnabled,
                    new Dictionary<string, object?> { ["enabled"] = false },
                    recordingCleanupTimeoutMs,
                    false,
                    cleanupCts.Token)
                .ConfigureAwait(false);
            actions.Add(shouldStopRecordingForVerification && options.LeaveRunning
                ? "recording stopped for verification"
                : "recording stopped");
            var stoppedRecordingForVerification = shouldStopRecordingForVerification &&
                                                  IsSuccess(stopResponse);
            if (IsSuccess(stopResponse))
            {
                await tryWaitWithTokenAsync("RecordingStopped", recordingCleanupTimeoutMs, cleanupCts.Token)
                    .ConfigureAwait(false);
            }

            return stoppedRecordingForVerification;
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "cleanup-stop-recording");
            return false;
        }
    }
}
