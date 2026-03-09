using System;

namespace ElgatoCapture.Services;

internal static class EnvironmentHelpers
{
    public static int GetIntFromEnv(string variableName, int defaultValue, int minValue, int maxValue)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        if (int.TryParse(rawValue, out var parsedValue))
        {
            return Math.Clamp(parsedValue, minValue, maxValue);
        }

        return defaultValue;
    }

    public static double GetDoubleFromEnv(string variableName, double defaultValue, double minValue, double maxValue)
    {
        var rawValue = Environment.GetEnvironmentVariable(variableName);
        if (double.TryParse(rawValue, out var parsed))
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

        if (int.TryParse(raw, out var intValue))
        {
            value = intValue != 0;
            return true;
        }

        return false;
    }
}
