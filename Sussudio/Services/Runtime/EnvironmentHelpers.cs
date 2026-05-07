using System;
using System.Globalization;

namespace Sussudio.Services.Runtime;

// Typed environment-variable parsing helpers for experimental performance and
// diagnostics knobs.
internal static class EnvironmentHelpers
{
    public static int GetIntFromEnv(string variableName, int defaultValue, int minValue, int maxValue)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return Math.Clamp(parsedValue, minValue, maxValue);
        }

        return defaultValue;
    }

    public static double GetDoubleFromEnv(string variableName, double defaultValue, double minValue, double maxValue)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        if (double.TryParse(rawValue, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed))
        {
            return Math.Clamp(parsed, minValue, maxValue);
        }

        return defaultValue;
    }

    public static bool TryGetBoolFromEnv(string variableName, out bool value)
    {
        value = false;
        var raw = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (bool.TryParse(raw, out var boolValue))
        {
            value = boolValue;
            return true;
        }

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
        {
            value = intValue != 0;
            return true;
        }

        return false;
    }
}
