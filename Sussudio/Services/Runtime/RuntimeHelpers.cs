using System;
using System.Globalization;
using System.Threading;

namespace Sussudio.Services.Runtime;

// Lock-free max-update helpers. Several diagnostic counters across capture,
// recording, and flashback need to track high-water marks under contention; the
// CAS-loop pattern was open-coded in four files before this consolidation.
internal static class AtomicMax
{
    public static void Update(ref int target, int candidate)
    {
        while (true)
        {
            var current = Volatile.Read(ref target);
            if (candidate <= current)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref target, candidate, current) == current)
            {
                return;
            }
        }
    }

    public static void Update(ref long target, long candidate)
    {
        while (true)
        {
            var current = Interlocked.Read(ref target);
            if (candidate <= current)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref target, candidate, current) == current)
            {
                return;
            }
        }
    }
}

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

// Append-and-snapshot helpers for the simple ring buffers used by the cadence
// trackers and jitter trackers. Callers own their own validation and locking;
// these methods only handle the ring index/count bookkeeping and the bounded
// copy-out, both of which were duplicated across four classes before.
internal static class RingBufferHelpers
{
    public static void Add(double[] window, ref int count, ref int index, double value)
    {
        window[index] = value;
        index = (index + 1) % window.Length;
        if (count < window.Length)
        {
            count++;
        }
    }

    public static void Add(int[] window, ref int count, ref int index, int value)
    {
        window[index] = value;
        index = (index + 1) % window.Length;
        if (count < window.Length)
        {
            count++;
        }
    }

    // maxCount: optional cap on samples returned; defaults to "all available".
    public static double[] Copy(double[] window, int count, int index, int? maxCount = null)
    {
        var take = maxCount.HasValue
            ? Math.Min(Math.Max(0, maxCount.Value), count)
            : count;
        if (take <= 0)
        {
            return Array.Empty<double>();
        }

        var result = new double[take];
        var start = (index - take + window.Length) % window.Length;
        for (var i = 0; i < take; i++)
        {
            result[i] = window[(start + i) % window.Length];
        }

        return result;
    }

    public static int[] Copy(int[] window, int count, int index, int? maxCount = null)
    {
        var take = maxCount.HasValue
            ? Math.Min(Math.Max(0, maxCount.Value), count)
            : count;
        if (take <= 0)
        {
            return Array.Empty<int>();
        }

        var result = new int[take];
        var start = (index - take + window.Length) % window.Length;
        for (var i = 0; i < take; i++)
        {
            result[i] = window[(start + i) % window.Length];
        }

        return result;
    }
}

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
