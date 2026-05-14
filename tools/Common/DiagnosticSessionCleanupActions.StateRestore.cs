using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionCleanupActions
{
    private static async Task RestoreLiveFlashbackPlaybackAsync(
        bool startedFlashbackPlayback,
        List<string> actions,
        Func<string, Dictionary<string, object?>?, int?, bool, CancellationToken, Task<JsonElement>> sendWithTokenAsync,
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
            await sendWithTokenAsync(
                    "FlashbackAction",
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
        Func<string, Dictionary<string, object?>?, int?, bool, CancellationToken, Task<JsonElement>> sendWithTokenAsync,
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
            await sendWithTokenAsync(
                    "SetPreviewEnabled",
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
        Func<string, Dictionary<string, object?>?, int?, bool, CancellationToken, Task<JsonElement>> sendWithTokenAsync,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException)
    {
        if (enabledFlashback && !GetBool(initialSnapshot, "FlashbackActive"))
        {
            try
            {
                setStage("cleanup-restore-flashback-off");
                var cleanupTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout("SetFlashbackEnabled");
                using var cleanupCts = CreateCleanupCts(TimeSpan.FromMilliseconds(cleanupTimeoutMs));
                await sendWithTokenAsync(
                        "SetFlashbackEnabled",
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
                var cleanupTimeoutMs = AutomationPipeProtocol.GetDefaultResponseTimeout("SetFlashbackEnabled");
                using var cleanupCts = CreateCleanupCts(TimeSpan.FromMilliseconds(cleanupTimeoutMs));
                await sendWithTokenAsync(
                        "SetFlashbackEnabled",
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
