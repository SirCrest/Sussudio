using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackExports
{
    internal static async Task CleanupFlashbackSelectionAsync(
        Func<string, Dictionary<string, object?>?, int?, Task<JsonElement>> sendCommandAsync)
    {
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "clear-in-out-points" }, null)
            .ConfigureAwait(false);
        await sendCommandAsync("FlashbackAction", new Dictionary<string, object?> { ["action"] = "go-live" }, null)
            .ConfigureAwait(false);
    }
}
