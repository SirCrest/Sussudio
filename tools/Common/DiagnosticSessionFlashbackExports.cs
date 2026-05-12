using System.Globalization;
using System.Text.Json;
using static Sussudio.Tools.AutomationSnapshotFormatter;

namespace Sussudio.Tools;

internal static class DiagnosticSessionFlashbackExports
{
    internal static int? TryParseFlashbackExportSegmentCount(string message)
    {
        const string marker = " from ";
        var markerIndex = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return null;
        }

        var digitsStart = markerIndex + marker.Length;
        while (digitsStart < message.Length && char.IsWhiteSpace(message[digitsStart]))
        {
            digitsStart++;
        }

        var digitsEnd = digitsStart;
        while (digitsEnd < message.Length && char.IsDigit(message[digitsEnd]))
        {
            digitsEnd++;
        }

        if (digitsEnd == digitsStart)
        {
            return null;
        }

        var suffix = message[digitsEnd..];
        if (!suffix.Contains("segment", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return int.TryParse(
            message.AsSpan(digitsStart, digitsEnd - digitsStart),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var value)
            ? value
            : null;
    }

    internal static Dictionary<string, object?> CreateFlashbackExportVerifyPayload(string filePath) =>
        new()
        {
            ["filePath"] = filePath,
            ["strict"] = true,
            ["verificationProfile"] = "flashback-export"
        };

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

    internal static async Task CleanupFlashbackSelectionAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "clear-in-out-points" }, null)
            .ConfigureAwait(false);
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "go-live" }, null)
            .ConfigureAwait(false);
    }
}
