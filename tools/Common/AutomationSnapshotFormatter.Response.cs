using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    internal static bool IsSuccess(JsonElement response)
    {
        return response.ValueKind == JsonValueKind.Object &&
               response.TryGetProperty("Success", out var success) &&
               success.ValueKind == JsonValueKind.True;
    }
}
