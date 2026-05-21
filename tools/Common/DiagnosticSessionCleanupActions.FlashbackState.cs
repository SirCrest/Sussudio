using System.Text.Json;
using Sussudio.Models;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionCleanupActions
{
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
