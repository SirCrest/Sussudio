using System.Globalization;
using System.Text.Json;

namespace Sussudio.Tools;

internal static class AutomationResponseState
{
    internal static bool TryRead(
        JsonElement response,
        out bool success,
        out string? status,
        out int? retryAfterMs)
    {
        success = false;
        status = null;
        retryAfterMs = null;

        if (response.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (response.TryGetProperty("Success", out var successProperty) &&
            successProperty.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            success = successProperty.GetBoolean();
        }

        if (response.TryGetProperty("Status", out var statusProperty) &&
            statusProperty.ValueKind == JsonValueKind.String)
        {
            status = statusProperty.GetString();
        }

        if (response.TryGetProperty("RetryAfterMs", out var retryAfterProperty))
        {
            if (retryAfterProperty.ValueKind == JsonValueKind.Number &&
                retryAfterProperty.TryGetInt32(out var numeric))
            {
                retryAfterMs = numeric;
            }
            else if (retryAfterProperty.ValueKind == JsonValueKind.String &&
                     int.TryParse(
                         retryAfterProperty.GetString(),
                         NumberStyles.Integer,
                         CultureInfo.InvariantCulture,
                         out var parsed))
            {
                retryAfterMs = parsed;
            }
        }

        return true;
    }
}
