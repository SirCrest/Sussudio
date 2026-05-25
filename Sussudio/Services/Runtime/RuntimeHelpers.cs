using System;
using System.Globalization;
using System.Runtime.InteropServices;
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

// Best-effort MMCSS registration wrapper for timing-sensitive worker threads.
// Failure is logged but not fatal so the app still runs on systems without AVRT.
internal sealed class MmcssThreadRegistration : IDisposable
{
    private IntPtr _handle;

    private MmcssThreadRegistration(IntPtr handle)
    {
        _handle = handle;
    }

    public static MmcssThreadRegistration? TryRegister(string taskName, int priority, Action<string>? log = null)
    {
        if (string.IsNullOrWhiteSpace(taskName))
        {
            return null;
        }

        try
        {
            var handle = AvSetMmThreadCharacteristics(taskName, out _);
            if (handle == IntPtr.Zero)
            {
                log?.Invoke($"MMCSS registration failed task={taskName} lastError={Marshal.GetLastWin32Error()}");
                return null;
            }

            var clampedPriority = (AvrtPriority)Math.Clamp(priority, -2, 2);
            if (!AvSetMmThreadPriority(handle, clampedPriority))
            {
                log?.Invoke($"MMCSS priority set failed task={taskName} priority={priority} lastError={Marshal.GetLastWin32Error()}");
            }

            log?.Invoke($"MMCSS registered task={taskName} priority={(int)clampedPriority}");
            return new MmcssThreadRegistration(handle);
        }
        catch (DllNotFoundException)
        {
            log?.Invoke("MMCSS registration unavailable: avrt.dll not found.");
            return null;
        }
        catch (EntryPointNotFoundException)
        {
            log?.Invoke("MMCSS registration unavailable: AVRT entry point not found.");
            return null;
        }
        catch (Exception ex)
        {
            log?.Invoke($"MMCSS registration failed type={ex.GetType().Name} msg={ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        var handle = _handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        _handle = IntPtr.Zero;
        AvRevertMmThreadCharacteristics(handle);
    }

    private enum AvrtPriority
    {
        VeryLow = -2,
        Low = -1,
        Normal = 0,
        High = 1,
        Critical = 2
    }

    [DllImport("avrt.dll", EntryPoint = "AvSetMmThreadCharacteristicsW", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr AvSetMmThreadCharacteristics(string taskName, out int taskIndex);

    [DllImport("avrt.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AvSetMmThreadPriority(IntPtr avrtHandle, AvrtPriority priority);

    [DllImport("avrt.dll", ExactSpelling = true, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AvRevertMmThreadCharacteristics(IntPtr avrtHandle);
}
