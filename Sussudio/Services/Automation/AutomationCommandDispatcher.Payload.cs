using System;
using System.Globalization;
using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    private static string RequireString(JsonElement payload, string propertyName)
    {
        var value = GetString(payload, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required string property '{propertyName}'.");
        }

        return value;
    }

    private static bool RequireBool(JsonElement payload, string propertyName)
    {
        var value = GetBool(payload, propertyName);
        if (!value.HasValue)
        {
            throw new InvalidOperationException($"Missing required boolean property '{propertyName}'.");
        }

        return value.Value;
    }

    private static double RequireDouble(JsonElement payload, string propertyName)
    {
        var value = GetDouble(payload, propertyName);
        if (!value.HasValue)
        {
            throw new InvalidOperationException($"Missing required numeric property '{propertyName}'.");
        }

        return value.Value;
    }

    private static string? GetString(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!payload.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        return property.ValueKind != JsonValueKind.Null
            ? property.ToString()
            : null;
    }

    private static bool? GetBool(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(property.GetString(), out var parsed) => parsed,
            JsonValueKind.Number when property.TryGetInt32(out var number) => number != 0,
            _ => null
        };
    }

    private static double? GetDouble(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var numeric))
        {
            return double.IsFinite(numeric) ? numeric : null;
        }

        if (property.ValueKind == JsonValueKind.String &&
            double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return double.IsFinite(parsed) ? parsed : null;
        }

        return null;
    }

    private static int? GetInt(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var numeric))
        {
            return numeric;
        }

        if (property.ValueKind == JsonValueKind.String &&
            int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static bool RequiresReadyDevices(AutomationCommandKind command)
        => AutomationCommandCatalog.Get(command).RequiresReadyDevices;

    private static string ValidatePathPayload(
        AutomationCommandKind command,
        string payloadKey,
        string path)
        => AutomationCommandCatalog.ValidatePath(command, payloadKey, path);

    private static AutomationWindowAction ParseWindowAction(JsonElement payload)
    {
        var raw = GetString(payload, "action");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return AutomationWindowAction.Restore;
        }

        if (Enum.TryParse<AutomationWindowAction>(raw, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Invalid window action: '{raw}'.");
    }

    private static AutomationFlashbackAction ParseFlashbackAction(JsonElement payload)
    {
        var raw = RequireString(payload, "action");
        var normalized = raw.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Trim();

        if (Enum.TryParse<AutomationFlashbackAction>(normalized, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException(
            $"Invalid flashback action: '{raw}'. Expected play, pause, go-live, seek, begin-scrub, update-scrub, end-scrub, set-in-point, set-out-point, or clear-in-out-points.");
    }

    private static AutomationWaitCondition ParseWaitCondition(JsonElement payload)
    {
        var raw = GetString(payload, "condition");
        if (string.IsNullOrWhiteSpace(raw))
        {
            return AutomationWaitCondition.PreviewFramesActive;
        }

        if (Enum.TryParse<AutomationWaitCondition>(raw, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Invalid wait condition: '{raw}'.");
    }
}
