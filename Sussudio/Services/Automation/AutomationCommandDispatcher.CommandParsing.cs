using System;
using System.Text.Json;
using Sussudio.Models;
using Sussudio.Tools;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
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
