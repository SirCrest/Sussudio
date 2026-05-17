using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackExports
{
    internal static async Task ToggleAudioEnabledDuringFlashbackExportAsync(
        Task<JsonElement> exportTask,
        JsonElement baselineSnapshot,
        List<string> actions,
        List<string> warnings,
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync,
        CancellationToken cancellationToken)
    {
        var baselineAudioEnabled = GetBool(baselineSnapshot, "IsAudioEnabled");
        var toggledAudioEnabled = !baselineAudioEnabled;
        var exportRequestOutstandingBeforeToggle = false;

        try
        {
            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            exportRequestOutstandingBeforeToggle = !exportTask.IsCompleted;
            if (exportRequestOutstandingBeforeToggle)
            {
                actions.Add("flashback range export audio switch confirmed export command outstanding before audio toggle");
            }
            else
            {
                warnings.Add("flashback range export audio switch: export completed before audio toggle");
            }

            var toggleResponse = await sendCommandAsync(
                    "SetAudioEnabled",
                    new Dictionary<string, object?> { ["enabled"] = toggledAudioEnabled },
                    10_000)
                .ConfigureAwait(false);
            if (IsSuccess(toggleResponse))
            {
                actions.Add($"flashback range export audio switch toggled audio enabled to {toggledAudioEnabled}");
            }
            else
            {
                warnings.Add(
                    "flashback range export audio switch: audio toggle failed - " +
                    Get(toggleResponse, "Message", "unknown error"));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            warnings.Add($"flashback range export audio switch: audio toggle threw {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            try
            {
                var restoreResponse = await sendCommandAsync(
                        "SetAudioEnabled",
                        new Dictionary<string, object?> { ["enabled"] = baselineAudioEnabled },
                        10_000)
                    .ConfigureAwait(false);
                if (IsSuccess(restoreResponse))
                {
                    actions.Add($"flashback range export audio switch restored audio enabled to {baselineAudioEnabled}");
                }
                else
                {
                    warnings.Add(
                        "flashback range export audio switch: audio restore failed - " +
                        Get(restoreResponse, "Message", "unknown error"));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                warnings.Add($"flashback range export audio switch: audio restore threw {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
