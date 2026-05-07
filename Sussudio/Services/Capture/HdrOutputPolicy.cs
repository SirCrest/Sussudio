using System;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

// Single policy gate for enabling HDR output. Environment overrides live here
// so capture setup and UI readiness checks stay consistent.
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

        if (EnvironmentHelpers.TryGetBoolFromEnv("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", out var forceOff) && forceOff)
        {
            Logger.Log("HDR output requested but SUSSUDIO_HDR_OUTPUT_FORCE_OFF disables the HDR pipeline.");
            return false;
        }

        return true;
    }
}
