using System;

namespace Sussudio.Services.Performance;

/// <summary>
/// Overflow-safe conversion between Stopwatch (QPC) ticks and 100 ns units, the unit of Media Foundation sample
/// times. <c>ticks * 10_000_000</c> overflows a <see cref="long"/> after 25.6 hours of uptime at 10 MHz, so the
/// fast path splits into whole seconds plus a remainder and the rare extreme falls back to 128-bit arithmetic.
/// Results truncate toward zero and saturate instead of wrapping; a non-positive frequency yields zero.
/// </summary>
public static class QpcMath
{
    public const long HundredNsPerSecond = 10_000_000;

    // Largest frequency / whole-second count for which the 64-bit split cannot overflow. QueryPerformanceFrequency
    // is 10 MHz on current Windows, so the fast path covers every realistic input.
    private const long MaxSplitFrequency = 900_000_000_000;
    private const long MaxSplitWholeSeconds = 900_000_000_000;

    public static long TicksToHundredNs(long ticks, long frequency)
    {
        if (frequency <= 0)
        {
            return 0;
        }

        var whole = ticks / frequency;
        if (frequency <= MaxSplitFrequency && whole <= MaxSplitWholeSeconds && whole >= -MaxSplitWholeSeconds)
        {
            var remainder = ticks - whole * frequency;
            return whole * HundredNsPerSecond + remainder * HundredNsPerSecond / frequency;
        }

        return Saturate((Int128)ticks * HundredNsPerSecond / frequency);
    }

    public static long HundredNsToTicks(long hundredNs, long frequency)
    {
        if (frequency <= 0)
        {
            return 0;
        }

        var whole = hundredNs / HundredNsPerSecond;
        if (frequency <= MaxSplitFrequency && Math.Abs(whole) <= long.MaxValue / (frequency + 1) - 1)
        {
            var remainder = hundredNs - whole * HundredNsPerSecond;
            return whole * frequency + remainder * frequency / HundredNsPerSecond;
        }

        return Saturate((Int128)hundredNs * frequency / HundredNsPerSecond);
    }

    private static long Saturate(Int128 value)
    {
        if (value > long.MaxValue)
        {
            return long.MaxValue;
        }

        return value < long.MinValue ? long.MinValue : (long)value;
    }
}

/// <summary>
/// Decides whether Media Foundation sample times share the QPC clock domain (spec 2.6). Fed the offset (QPC at
/// arrival minus MF sample time, in 100 ns) of each usable sample on the capture read thread, a single writer.
/// If every one of the first <see cref="ClassificationSampleCount"/> offsets lies in [0, 2 s) the clock is QPC and
/// ingress lag is absolute; the first offset outside that range classifies the clock as relative at once. A
/// classification is final until <see cref="Reset"/>. Nothing in the pipeline consumes the result.
/// </summary>
public sealed class IngressClockClassifier
{
    public const int ClassificationSampleCount = 120;
    public const long MaxQpcOffset100Ns = 2 * QpcMath.HundredNsPerSecond;

    private int _observed;

    public PerfIngressClockKind Kind { get; private set; }

    /// <summary>Smallest offset observed while classifying, in 100 ns; <see cref="long.MinValue"/> before any sample.</summary>
    public long MinOffset100Ns { get; private set; } = long.MinValue;

    public bool IsQpcDomain => Kind == PerfIngressClockKind.Qpc;

    /// <summary>Feeds one offset. Returns true only on the call that completes the classification.</summary>
    public bool Observe(long offset100Ns)
    {
        if (Kind != PerfIngressClockKind.Unknown)
        {
            return false;
        }

        if (_observed == 0 || offset100Ns < MinOffset100Ns)
        {
            MinOffset100Ns = offset100Ns;
        }

        _observed++;
        if (offset100Ns < 0 || offset100Ns >= MaxQpcOffset100Ns)
        {
            Kind = PerfIngressClockKind.Relative;
            return true;
        }

        if (_observed >= ClassificationSampleCount)
        {
            Kind = PerfIngressClockKind.Qpc;
            return true;
        }

        return false;
    }

    /// <summary>Returns to unclassified so a restarted capture is judged afresh.</summary>
    public void Reset()
    {
        _observed = 0;
        Kind = PerfIngressClockKind.Unknown;
        MinOffset100Ns = long.MinValue;
    }
}
