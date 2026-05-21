using System.Text.Json;
using Sussudio.Models;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionCleanupActions
{
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
}
