using System;
using ElgatoCapture.Models;

namespace ElgatoCapture.Services;

internal static class HdrOutputPolicy
{
    public static bool IsEnabled(CaptureSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var hdrRequested = settings.HdrEnabled && settings.HdrOutputMode == HdrOutputMode.Hdr10Pq;
        if (!hdrRequested)
        {
            return false;
        }

        if (TryReadEnvironmentBool("ELGATOCAPTURE_HDR_OUTPUT_FORCE_OFF", out var forceOff) && forceOff)
        {
            throw new InvalidOperationException(
                "HDR output was requested but ELGATOCAPTURE_HDR_OUTPUT_FORCE_OFF disables the HDR pipeline.");
        }

        if (TryReadEnvironmentBool("ELGATOCAPTURE_HDR_OUTPUT_ENABLED", out var legacyEnabled))
        {
            if (!legacyEnabled)
            {
                throw new InvalidOperationException(
                    "HDR output was requested but ELGATOCAPTURE_HDR_OUTPUT_ENABLED is set to false.");
            }
        }

        return true;
    }

    private static bool TryReadEnvironmentBool(string variableName, out bool parsedValue)
    {
        parsedValue = false;
        var raw = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (bool.TryParse(raw, out var boolValue))
        {
            parsedValue = boolValue;
            return true;
        }

        if (int.TryParse(raw, out var intValue))
        {
            parsedValue = intValue != 0;
            return true;
        }

        return false;
    }
}
