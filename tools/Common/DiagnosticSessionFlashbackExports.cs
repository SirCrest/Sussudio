using System.Globalization;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackExports
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

    internal static async Task CleanupFlashbackSelectionAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "clear-in-out-points" }, null)
            .ConfigureAwait(false);
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "go-live" }, null)
            .ConfigureAwait(false);
    }
}
