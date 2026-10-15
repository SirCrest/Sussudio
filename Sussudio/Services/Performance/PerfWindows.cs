namespace Sussudio.Services.Performance;

/// <summary>
/// The process-wide measurement windows over <see cref="PerfMetrics.Registry"/>. A window's baseline carries a
/// <see cref="PerfSystemSample"/> (process CPU, I/O, GC, DWM and per-thread CPU) taken with
/// <see cref="ProcessSensors.Capture"/>, so one <c>begin</c> / <c>end</c> pair covers histograms, counters and
/// system totals together.
/// </summary>
public static class PerfWindows
{
    public static PerfWindowSet<PerfSystemSample> Set { get; } = new(PerfMetrics.Registry, ProcessSensors.Capture);
}
