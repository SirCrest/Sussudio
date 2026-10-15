using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Sussudio.Services.Performance;

// Wire DTOs for the perf report (docs/architecture/performance-telemetry.md section 3.1). Property names are the
// JSON field names: the automation response serializer keeps PascalCase. Wire names are appended, never renamed.

public sealed record PerfReportMetric(
    string Name,
    string Unit,
    long Count,
    double RatePerSec,
    double Mean,
    double P50,
    double P90,
    double P99,
    double P999,
    double Max,
    bool MaxApprox);

public sealed record PerfReportCounter(string Name, long Delta, double RatePerSec);

public sealed record PerfReportThread(
    string Role,
    int ThreadId,
    double CpuPercent,
    ulong Cycles,
    string Priority,
    bool Mmcss,
    bool Alive);

public sealed record PerfReportProcess(
    double CpuPercentOfOneCore,
    long PrivateBytes,
    long WorkingSetBytes,
    long IoReadBytes,
    long IoWriteBytes,
    int Handles,
    int Threads);

public sealed record PerfReportGc(int Gen0, int Gen1, int Gen2, double PauseMs, long AllocatedBytes);

public sealed record PerfReportGpuEngine(string Type, double MeanPercent, double P95Percent);

/// <summary>
/// GPU section. <see cref="Engines"/> are this process's engine utilization while the PDH sensor ran.
/// <see cref="DedicatedBytes"/> is this process's dedicated GPU memory from the same sensor, null while that sensor is
/// not running or has no PDH instance for the process. <see cref="Adapters"/> is read on demand when the report is
/// built (instantaneous gauges); <see cref="AdaptersError"/> says why it is empty when the read failed.
/// </summary>
public sealed record PerfReportGpu(
    IReadOnlyList<PerfReportGpuEngine> Engines,
    long? DedicatedBytes,
    IReadOnlyList<PerfReportGpuAdapter> Adapters,
    string? AdaptersError);

/// <summary>
/// DWM compositor view. <see cref="FramesComposedPerSec"/> counts composition passes, not frames shown: a preview that
/// reaches the display by independent flip, direct flip or an overlay plane is scanned out without DWM composing it,
/// so on a high-refresh display a low value is normal. <see cref="CompositionPath"/> (<c>unknown</c>, <c>idle</c>,
/// <c>bypass</c>, <c>composed</c> or <c>mixed</c>) and <see cref="Interpretation"/> read it against the swap chain's
/// present rate. <see cref="RefreshCountPerSec"/> is the DWM refresh counter's rate; it advances with composition
/// activity and is NOT a hardware vblank rate (an idle desktop measured 0.2/s on a 120 Hz display): use
/// <see cref="PerfReportDisplay.VblankPerSec"/> for the display's real refresh cadence.
/// </summary>
public sealed record PerfReportDwm(
    double? RefreshHz,
    double? FramesComposedPerSec,
    double? RefreshCountPerSec,
    string CompositionPath,
    string Interpretation);

/// <summary>
/// The preview swap chain as the display sees it, from its DXGI frame statistics (already sampled by the renderer).
/// Rates are over the QPC interval between the two samples that bound the report interval. <see cref="Available"/>
/// is false, with <see cref="Note"/> saying why, when there was no renderer, no baseline sample, a re-created swap
/// chain or no new sample (a lifetime report has no display baseline: use a window).
/// <see cref="PresentsPerVblank"/> is presents per display refresh (0.5 for 60 fps on a 120 Hz display).
/// </summary>
public sealed record PerfReportDisplay(
    bool Available,
    string Note,
    double? SampleSeconds,
    double? PresentsPerSec,
    double? VblankPerSec,
    double? PresentsPerVblank);

public sealed record PerfReportCapture(string IngressClock, double? IngressClockMinOffsetMs);

/// <summary>
/// Whether demand-driven sensors are wanted now (<see cref="Active"/>) and why (<see cref="Reason"/>: <c>knob:off</c>,
/// <c>knob:on</c>, <c>window:&lt;names&gt;</c>, <c>trace</c> or <c>idle</c>), the value of the sensors knob
/// (<see cref="Mode"/>) and one line per sensor and for GPU timing (<see cref="Details"/>).
/// </summary>
public sealed record PerfReportSensors(bool Active, string Reason, string Mode, IReadOnlyList<string> Details);

public sealed record PerfReportTrace(bool Running, long Events, int Capacity);

public sealed record PerfReportKnob(string Name, string? Value, string? Launch, string Apply);

public sealed record PerfReportMarker(string TimestampUtc, string Text);

public sealed record PerfReport(
    string? Window,
    string Since,
    string StartedUtc,
    string GeneratedUtc,
    double DurationMs,
    IReadOnlyList<PerfReportMetric> Metrics,
    IReadOnlyList<PerfReportCounter> Counters,
    IReadOnlyList<PerfReportThread> Threads,
    PerfReportProcess Process,
    PerfReportGc Gc,
    PerfReportGpu Gpu,
    PerfReportDwm Dwm,
    PerfReportDisplay Display,
    PerfReportCapture Capture,
    PerfReportSensors Sensors,
    PerfReportTrace Trace,
    IReadOnlyList<PerfReportKnob> Knobs,
    IReadOnlyList<PerfReportMarker> Markers);

/// <summary>
/// Builds the perf report from the live registry: process lifetime, or the delta of a measurement window. Time metrics
/// recorded in microseconds are reported in milliseconds and percentages recorded in hundredths of a percent are
/// reported in percent; every other metric keeps its own unit. A lifetime report and a window report have one shape
/// so tools have one parser.
/// </summary>
public static class PerfReportBuilder
{
    private static readonly IReadOnlyDictionary<string, string> GpuEngineMetrics = new Dictionary<string, string>
    {
        ["gpu.engine.3d"] = "3D",
        ["gpu.engine.video_encode"] = "VideoEncode",
        ["gpu.engine.video_decode"] = "VideoDecode",
        ["gpu.engine.copy"] = "Copy",
        ["gpu.engine.other"] = "Other",
        ["gpu.nvml.gpu_util"] = "NvmlGpu",
        ["gpu.nvml.encoder_util"] = "NvmlEncoder",
        ["gpu.nvml.decoder_util"] = "NvmlDecoder",
    };

    /// <summary>The report since process start. Never throws.</summary>
    public static PerfReport BuildLifetime(string? filter, bool includeIdle)
    {
        var baseline = ProcessSensors.ProcessStartBaseline;
        var registry = PerfMetrics.Registry.Snapshot();
        var current = ProcessSensors.Capture();
        var elapsedTicks = Math.Max(0, current.Timestamp - baseline.Timestamp);
        return Build(
            window: null,
            since: "process-start",
            startedUtc: baseline.CapturedUtc,
            registry,
            elapsedTicks,
            baseline,
            current,
            filter,
            includeIdle);
    }

    /// <summary>The report for a window reading (delta since <c>begin</c>).</summary>
    public static PerfReport BuildWindow(PerfWindowReading<PerfSystemSample> reading, string? filter, bool includeIdle)
    {
        return Build(
            reading.Name,
            "window",
            reading.StartedUtc,
            reading.Delta,
            reading.ElapsedTicks,
            reading.Baseline,
            reading.Current,
            filter,
            includeIdle);
    }

    private static PerfReport Build(
        string? window,
        string since,
        DateTime startedUtc,
        PerfRegistrySnapshot registry,
        long elapsedTicks,
        PerfSystemSample baseline,
        PerfSystemSample current,
        string? filter,
        bool includeIdle)
    {
        var durationMs = TicksToMs(elapsedTicks);
        var seconds = durationMs / 1000d;
        var prefix = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim();

        var metrics = new List<PerfReportMetric>();
        var counters = new List<PerfReportCounter>();
        var gpuEngines = new List<PerfReportGpuEngine>();
        foreach (var sample in registry.Metrics)
        {
            if (sample.Histogram is { } histogram)
            {
                var scale = UnitScale(sample.Info.Unit);
                if (GpuEngineMetrics.TryGetValue(sample.Info.Name, out var engineType) && !sample.IsIdle)
                {
                    gpuEngines.Add(new PerfReportGpuEngine(
                        engineType,
                        Round(histogram.Mean * scale),
                        Round(histogram.Percentile(95) * scale)));
                }

                if (!Wanted(sample, prefix, includeIdle))
                {
                    continue;
                }

                metrics.Add(new PerfReportMetric(
                    sample.Info.Name,
                    UnitName(sample.Info.Unit),
                    histogram.Count,
                    Round(Rate(histogram.Count, seconds)),
                    Round(histogram.Mean * scale),
                    Round(histogram.Percentile(50) * scale),
                    Round(histogram.Percentile(90) * scale),
                    Round(histogram.Percentile(99) * scale),
                    Round(histogram.Percentile(99.9) * scale),
                    Round(histogram.Max * scale),
                    histogram.MaxIsApproximate));
            }
            else if (Wanted(sample, prefix, includeIdle))
            {
                counters.Add(new PerfReportCounter(sample.Info.Name, sample.Value, Round(Rate(sample.Value, seconds))));
            }
        }

        var cpuSeconds = Math.Max(0, current.ProcessCpuTicks - baseline.ProcessCpuTicks) / (double)TimeSpan.TicksPerSecond;
        var process = new PerfReportProcess(
            Round(seconds > 0 ? cpuSeconds / seconds * 100d : 0),
            current.PrivateBytes,
            current.WorkingSetBytes,
            Math.Max(0, current.IoReadBytes - baseline.IoReadBytes),
            Math.Max(0, current.IoWriteBytes - baseline.IoWriteBytes),
            current.Handles,
            current.ThreadCount);

        var gc = new PerfReportGc(
            Math.Max(0, current.Gen0Collections - baseline.Gen0Collections),
            Math.Max(0, current.Gen1Collections - baseline.Gen1Collections),
            Math.Max(0, current.Gen2Collections - baseline.Gen2Collections),
            Round(Math.Max(0, current.GcPauseMs - baseline.GcPauseMs)),
            Math.Max(0, current.GcAllocatedBytes - baseline.GcAllocatedBytes));

        double? composedPerSec = null;
        if (current.DwmFramesComposed is { } composedNow && baseline.DwmFramesComposed is { } composedThen && seconds > 0)
        {
            composedPerSec = Round(Math.Max(0, composedNow - composedThen) / seconds);
        }

        double? refreshCountPerSec = null;
        if (current.DwmRefreshCount is { } refreshNow && baseline.DwmRefreshCount is { } refreshThen && seconds > 0)
        {
            refreshCountPerSec = Round(Math.Max(0, refreshNow - refreshThen) / seconds);
        }

        var display = BuildDisplay(baseline.Display, current.Display);
        var composition = DwmCompositionInterpreter.Classify(composedPerSec, display.PresentsPerSec, current.DwmRefreshHz);

        var ingressKind = PerfIngressClock.Kind;
        var trace = FrameTrace.Recorder.GetStatus();
        var windows = PerfWindows.Set.List();
        var sensors = TelemetrySensors.Describe(windows.Select(window => window.Name).ToList(), trace.Running);
        var memory = GpuMemorySensor.Read();

        var markers = PerfMarkers.Log.Snapshot()
            .Where(marker => window is null || marker.TimestampUtc >= startedUtc)
            .Select(marker => new PerfReportMarker(marker.TimestampUtc.ToString("O", CultureInfo.InvariantCulture), marker.Text))
            .ToList();

        var knobs = TuningKnobs.Overrides()
            .Select(state => new PerfReportKnob(
                state.Definition.Name,
                state.Value,
                state.Launch,
                TuningKnobApplyNames.ToWire(state.Definition.Apply)))
            .ToList();

        return new PerfReport(
            window,
            since,
            startedUtc.ToString("O", CultureInfo.InvariantCulture),
            DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            Round(durationMs),
            metrics,
            counters,
            BuildThreads(baseline, current),
            process,
            gc,
            new PerfReportGpu(gpuEngines, TelemetrySensors.GpuEngineStatus?.DedicatedBytes, memory.Adapters, memory.Error),
            new PerfReportDwm(
                current.DwmRefreshHz is { } hz ? Round(hz) : null,
                composedPerSec,
                refreshCountPerSec,
                composition.Path,
                composition.Interpretation),
            display,
            new PerfReportCapture(PerfIngressClock.Name(ingressKind), PerfIngressClock.MinOffsetMs is { } offset ? Round(offset) : null),
            new PerfReportSensors(sensors.Active, sensors.Reason, sensors.Mode, sensors.Details),
            new PerfReportTrace(trace.Running, trace.Retained, trace.Capacity),
            knobs,
            markers);
    }

    // CPU of each registered thread over the part of the report interval it was running: a thread that started
    // mid-window, or that exited before the window ended, is divided by its own running time. Retired threads that
    // did no work in the window are left out.
    private static List<PerfReportThread> BuildThreads(PerfSystemSample baseline, PerfSystemSample current)
    {
        var before = baseline.ThreadSamples.ToDictionary(sample => sample.Serial);
        var threads = new List<PerfReportThread>();
        foreach (var thread in current.ThreadSamples)
        {
            before.TryGetValue(thread.Serial, out var earlier);
            var cpuTicks = Math.Max(0, thread.CpuTicks - (earlier?.CpuTicks ?? 0));
            var cycles = thread.Cycles >= (earlier?.Cycles ?? 0) ? thread.Cycles - (earlier?.Cycles ?? 0) : 0;
            if (!thread.Alive && cpuTicks == 0 && cycles == 0)
            {
                continue;
            }

            var intervalStart = Math.Max(baseline.Timestamp, thread.StartTimestamp);
            var intervalEnd = Math.Min(current.Timestamp, thread.EndTimestamp ?? current.Timestamp);
            var intervalSeconds = Math.Max(0, intervalEnd - intervalStart) / (double)Stopwatch.Frequency;
            var cpuSeconds = cpuTicks / (double)TimeSpan.TicksPerSecond;
            threads.Add(new PerfReportThread(
                thread.Role,
                thread.ThreadId,
                Round(intervalSeconds > 0 ? cpuSeconds / intervalSeconds * 100d : 0),
                cycles,
                thread.Priority,
                thread.Mmcss,
                thread.Alive));
        }

        return threads
            .OrderByDescending(thread => thread.CpuPercent)
            .ThenBy(thread => thread.Role, StringComparer.Ordinal)
            .ToList();
    }

    // Rates come from the swap chain's DXGI frame statistics. Both counters of one sample were read together, so the
    // QPC time of the vertical sync in each sample (not the report clock, which may be far from the last sample if
    // the preview stopped) is the interval both rates are divided by.
    private static PerfReportDisplay BuildDisplay(PerfDisplayCounters? baseline, PerfDisplayCounters? current)
    {
        if (current is null)
        {
            return DisplayUnavailable("No preview renderer is sampling DXGI frame statistics.");
        }

        if (baseline is null)
        {
            return DisplayUnavailable(
                "The interval began before the preview had a DXGI frame statistics sample. A lifetime report has no display baseline: begin a measurement window with the preview running.");
        }

        if (baseline.Generation != current.Generation)
        {
            return DisplayUnavailable("The preview swap chain or device was re-created during the interval, so the DXGI counters restarted.");
        }

        var qpcTicks = current.SyncQpcTime - baseline.SyncQpcTime;
        if (qpcTicks <= 0)
        {
            return DisplayUnavailable("No new DXGI frame statistics sample arrived during the interval (the preview was not presenting).");
        }

        var presents = current.PresentCount - baseline.PresentCount;
        var vblanks = current.SyncRefreshCount - baseline.SyncRefreshCount;
        if (presents < 0 || vblanks < 0)
        {
            return DisplayUnavailable("The DXGI counters went backwards during the interval.");
        }

        var sampleSeconds = qpcTicks / (double)Stopwatch.Frequency;
        var presentsPerSec = presents / sampleSeconds;
        var vblankPerSec = vblanks / sampleSeconds;
        return new PerfReportDisplay(
            true,
            "Rates over the QPC interval between the first and last DXGI frame statistics sample of the report interval.",
            Round(sampleSeconds),
            Round(presentsPerSec),
            Round(vblankPerSec),
            vblankPerSec > 0 ? Round(presentsPerSec / vblankPerSec) : null);
    }

    private static PerfReportDisplay DisplayUnavailable(string note) =>
        new(false, note, null, null, null, null);

    private static bool Wanted(PerfMetricSample sample, string? prefix, bool includeIdle)
    {
        if (!includeIdle && sample.IsIdle)
        {
            return false;
        }

        return prefix is null || sample.Info.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static string UnitName(PerfUnit unit) => unit switch
    {
        PerfUnit.Microseconds or PerfUnit.Milliseconds => "ms",
        PerfUnit.Bytes => "bytes",
        PerfUnit.Percent or PerfUnit.CentiPercent => "percent",
        _ => "count",
    };

    // Multiplier from a histogram's recorded integer values to the reported unit.
    private static double UnitScale(PerfUnit unit) => unit switch
    {
        PerfUnit.Microseconds => 0.001,
        PerfUnit.CentiPercent => 0.01,
        _ => 1.0,
    };

    private static double Rate(long count, double seconds) => seconds > 0 ? count / seconds : 0;

    private static double TicksToMs(long ticks) => ticks * 1000d / Stopwatch.Frequency;

    private static double Round(double value) => double.IsFinite(value) ? Math.Round(value, 4) : 0;
}
