using System;

namespace Sussudio.Services.Runtime;

// Common "how old is this telemetry sample" computation. Several diagnostics
// surfaces (snapshot builders, view-model age refresh, automation hub) need the
// same clamped, floor-rounded seconds-since-timestamp value, plus a short-circuit
// for already-reported ages from upstream telemetry sources.
internal static class TelemetryAgeHelper
{
    public static int? ComputeAgeSeconds(DateTimeOffset? timestampUtc, DateTimeOffset nowUtc)
    {
        if (!timestampUtc.HasValue)
        {
            return null;
        }

        var age = nowUtc - timestampUtc.Value;
        return age < TimeSpan.Zero ? 0 : (int)Math.Floor(age.TotalSeconds);
    }

    public static int? ComputeAgeSeconds(int? reportedAgeSeconds, DateTimeOffset? timestampUtc, DateTimeOffset nowUtc)
    {
        if (reportedAgeSeconds.HasValue)
        {
            return Math.Max(0, reportedAgeSeconds.Value);
        }

        return ComputeAgeSeconds(timestampUtc, nowUtc);
    }
}
