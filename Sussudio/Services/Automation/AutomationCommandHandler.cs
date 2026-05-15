using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Tools;

namespace Sussudio.Services.Automation;

// Holds a single trivial-handler delegate and the payload property name needed to
// extract the typed argument. Factory methods (Bool, String, Double) capture the
// delegate and property name; InvokeAsync reads and validates the property then
// calls the delegate. AcknowledgeMessage returns a stable ack string for the
// response, using the command name because trivial handlers do not retain the
// formatted per-command strings from the old case bodies.
internal sealed record AutomationCommandHandler(
    Func<IAutomationViewModel, JsonElement, CancellationToken, Task> Invoke,
    Func<AutomationCommandKind, JsonElement, string> AcknowledgeMessage,
    string PayloadFieldName,
    AutomationPayloadFieldType PayloadFieldType)
{
    public Task InvokeAsync(IAutomationViewModel viewModel, JsonElement payload, CancellationToken cancellationToken)
        => Invoke(viewModel, payload, cancellationToken);

    public static AutomationCommandHandler Bool(
        Func<IAutomationViewModel, bool, CancellationToken, Task> action,
        string propertyName)
        => new(
            (vm, payload, ct) =>
            {
                var value = GetBoolRequired(payload, propertyName);
                return action(vm, value, ct);
            },
            (command, _) => $"{command} acknowledged.",
            propertyName,
            AutomationPayloadFieldType.Boolean);

    public static AutomationCommandHandler String(
        Func<IAutomationViewModel, string, CancellationToken, Task> action,
        string propertyName)
        => new(
            (vm, payload, ct) =>
            {
                var value = GetStringRequired(payload, propertyName);
                return action(vm, value, ct);
            },
            (command, _) => $"{command} acknowledged.",
            propertyName,
            AutomationPayloadFieldType.String);

    public static AutomationCommandHandler Double(
        Func<IAutomationViewModel, double, CancellationToken, Task> action,
        string propertyName)
        => new(
            (vm, payload, ct) =>
            {
                var value = GetDoubleRequired(payload, propertyName);
                return action(vm, value, ct);
            },
            (command, _) => $"{command} acknowledged.",
            propertyName,
            AutomationPayloadFieldType.Number);

    private static bool GetBoolRequired(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty(propertyName, out var property))
        {
            throw new InvalidOperationException($"Missing required boolean property '{propertyName}'.");
        }

        var result = property.ValueKind switch
        {
            JsonValueKind.True => (bool?)true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(property.GetString(), out var parsed) => parsed,
            JsonValueKind.Number when property.TryGetInt32(out var number) => number != 0,
            _ => null
        };

        return result ?? throw new InvalidOperationException($"Missing required boolean property '{propertyName}'.");
    }

    private static string GetStringRequired(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty(propertyName, out var property))
        {
            throw new InvalidOperationException($"Missing required string property '{propertyName}'.");
        }

        var value = property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.ValueKind != JsonValueKind.Null ? property.ToString() : null;

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Missing required string property '{propertyName}'.");
        }

        return value;
    }

    private static double GetDoubleRequired(JsonElement payload, string propertyName)
    {
        if (payload.ValueKind != JsonValueKind.Object || !payload.TryGetProperty(propertyName, out var property))
        {
            throw new InvalidOperationException($"Missing required numeric property '{propertyName}'.");
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var numeric))
        {
            return numeric;
        }

        if (property.ValueKind == JsonValueKind.String &&
            double.TryParse(property.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Missing required numeric property '{propertyName}'.");
    }
}
