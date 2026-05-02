using System;
using ElgatoCapture.Models;
using ElgatoCapture.Services.Runtime;

namespace ElgatoCapture.Services.Capture;

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

        if (EnvironmentHelpers.TryGetBoolFromEnv("ELGATOCAPTURE_HDR_OUTPUT_FORCE_OFF", out var forceOff) && forceOff)
        {
            Logger.Log("HDR output requested but ELGATOCAPTURE_HDR_OUTPUT_FORCE_OFF disables the HDR pipeline.");
            return false;
        }

        return true;
    }
}
