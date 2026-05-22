using System.Text.Json;
using Sussudio.Models;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal readonly record struct DiagnosticSessionCleanupResult(bool StoppedRecordingForVerification);

internal static class DiagnosticSessionCleanupActions
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
        DiagnosticSessionCommandChannel commandChannel,
        Func<string, int, CancellationToken, Task> tryWaitWithTokenAsync,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        var stoppedRecordingForVerification = await StopRecordingForCleanupAsync(
                options,
                startedRecording,
                actions,
                commandChannel,
                tryWaitWithTokenAsync,
                setStage,
                recordTerminalException)
            .ConfigureAwait(false);

        if (!options.LeaveRunning)
        {
            await RestoreLiveFlashbackPlaybackAsync(
                    startedFlashbackPlayback,
                    actions,
                    commandChannel,
                    setStage,
                    recordTerminalException)
                .ConfigureAwait(false);
            await StopPreviewIfStartedAsync(
                    startedPreview,
                    initialSnapshot,
                    actions,
                    commandChannel,
                    setStage,
                    recordTerminalException)
                .ConfigureAwait(false);
            await RestoreFlashbackEnabledStateAsync(
                    enabledFlashback,
                    disabledFlashback,
                    initialSnapshot,
                    actions,
                    commandChannel,
                    setStage,
                    recordTerminalException)
                .ConfigureAwait(false);
        }

        return new DiagnosticSessionCleanupResult(stoppedRecordingForVerification);
    }

    private static CancellationTokenSource CreateCleanupCts(TimeSpan timeout)
        => new(timeout);

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

    private static async Task RestoreLiveFlashbackPlaybackAsync(
        bool startedFlashbackPlayback,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        if (!startedFlashbackPlayback)
        {
            return;
        }

        try
        {
            setStage("cleanup-go-live");
            using var cleanupCts = CreateCleanupCts(TimeSpan.FromSeconds(15));
            await commandChannel.SendWithTokenAsync(
                    AutomationCommandKind.FlashbackAction,
                    new Dictionary<string, object?> { ["action"] = "go-live" },
                    15_000,
                    false,
                    cleanupCts.Token)
                .ConfigureAwait(false);
            actions.Add("flashback playback returned live");
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "cleanup-go-live");
        }
    }

    private static async Task StopPreviewIfStartedAsync(
        bool startedPreview,
        JsonElement initialSnapshot,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        if (!startedPreview || GetBool(initialSnapshot, "IsPreviewing"))
        {
            return;
        }

        try
        {
            setStage("cleanup-stop-preview");
            using var cleanupCts = CreateCleanupCts(TimeSpan.FromSeconds(15));
            await commandChannel.SendWithTokenAsync(
                    AutomationCommandKind.SetPreviewEnabled,
                    new Dictionary<string, object?> { ["enabled"] = false },
                    15_000,
                    false,
                    cleanupCts.Token)
                .ConfigureAwait(false);
            actions.Add("preview stopped");
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "cleanup-stop-preview");
        }
    }

    private static async Task RestoreFlashbackEnabledStateAsync(
        bool enabledFlashback,
        bool disabledFlashback,
        JsonElement initialSnapshot,
        List<string> actions,
        DiagnosticSessionCommandChannel commandChannel,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        if (enabledFlashback && !GetBool(initialSnapshot, "FlashbackActive"))
        {
            try
            {
                setStage("cleanup-restore-flashback-off");
                var cleanupTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.SetFlashbackEnabled);
                using var cleanupCts = CreateCleanupCts(TimeSpan.FromMilliseconds(cleanupTimeoutMs));
                await commandChannel.SendWithTokenAsync(
                        AutomationCommandKind.SetFlashbackEnabled,
                        new Dictionary<string, object?> { ["enabled"] = false },
                        cleanupTimeoutMs,
                        false,
                        cleanupCts.Token)
                    .ConfigureAwait(false);
                actions.Add("flashback restored off");
            }
            catch (Exception ex)
            {
                recordTerminalException(ex, "cleanup-restore-flashback-off");
            }
        }

        if (disabledFlashback && GetBool(initialSnapshot, "FlashbackActive"))
        {
            try
            {
                setStage("cleanup-restore-flashback-on");
                var cleanupTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.SetFlashbackEnabled);
                using var cleanupCts = CreateCleanupCts(TimeSpan.FromMilliseconds(cleanupTimeoutMs));
                await commandChannel.SendWithTokenAsync(
                        AutomationCommandKind.SetFlashbackEnabled,
                        new Dictionary<string, object?> { ["enabled"] = true },
                        cleanupTimeoutMs,
                        false,
                        cleanupCts.Token)
                    .ConfigureAwait(false);
                actions.Add("flashback restored on");
            }
            catch (Exception ex)
            {
                recordTerminalException(ex, "cleanup-restore-flashback-on");
            }
        }
    }
}
