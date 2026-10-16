using System.Threading;

namespace Sussudio.Services.Performance;

/// <summary>Whether MF sample times share the QPC domain, as classified by the capture read loop.</summary>
public enum PerfIngressClockKind
{
    /// <summary>Not classified yet (no capture running, or fewer than the classification sample count).</summary>
    Unknown = 0,

    /// <summary>Every observed offset lay in [0, 2 s): <c>capture.ingress_lag</c> is absolute.</summary>
    Qpc = 1,

    /// <summary>Offsets fell outside that range: ingress lag is not recorded.</summary>
    Relative = 2,
}

/// <summary>
/// Latest ingress-clock classification, published by the capture read loop and reported under
/// <c>Capture.IngressClock</c> in the perf report. Nothing in the pipeline reads it.
/// </summary>
public static class PerfIngressClock
{
    private static int _kind;
    private static long _minOffsetTicks100Ns = long.MinValue;

    public static PerfIngressClockKind Kind => (PerfIngressClockKind)Volatile.Read(ref _kind);

    /// <summary>Smallest observed (QPC minus sample time) offset in milliseconds, or null before classification.</summary>
    public static double? MinOffsetMs
    {
        get
        {
            var ticks = Volatile.Read(ref _minOffsetTicks100Ns);
            return ticks == long.MinValue ? null : ticks / 10_000d;
        }
    }

    /// <summary>Publishes a classification with the minimum offset observed, in 100 ns units.</summary>
    public static void Publish(PerfIngressClockKind kind, long minOffsetTicks100Ns)
    {
        Volatile.Write(ref _minOffsetTicks100Ns, minOffsetTicks100Ns);
        Volatile.Write(ref _kind, (int)kind);
    }

    public static string Name(PerfIngressClockKind kind) => kind switch
    {
        PerfIngressClockKind.Qpc => "qpc",
        PerfIngressClockKind.Relative => "relative",
        _ => "unknown",
    };
}
