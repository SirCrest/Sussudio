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

/// <summary>GPU section. <see cref="DedicatedBytes"/> is null until a sensor measures it.</summary>
public sealed record PerfReportGpu(
    IReadOnlyList<PerfReportGpuEngine> Engines,
    long? DedicatedBytes,
    IReadOnlyList<string> Adapters);

public sealed record PerfReportDwm(double? RefreshHz, double? FramesComposedPerSec);

public sealed record PerfReportCapture(string IngressClock, double? IngressClockMinOffsetMs);

public sealed record PerfReportSensors(bool Active, string Reason);

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
    PerfReportCapture Capture,
    PerfReportSensors Sensors,
    PerfReportTrace Trace,
    IReadOnlyList<PerfReportKnob> Knobs,
    IReadOnlyList<PerfReportMarker> Markers);

/// <summary>
/// Builds the perf report from the live registry: process lifetime, or the delta of a measurement window. Time metrics
/// recorded in microseconds are reported in milliseconds; every other metric keeps its own unit. A lifetime report and
/// a window report have one shape so tools have one parser.
/// </summary>
public static class PerfReportBuilder
{
    private static readonly IReadOnlyDictionary<string, string> GpuEngineMetrics = new Dictionary<string, string>
    {
        ["gpu.engine.3d"] = "3D",
        ["gpu.engine.video_encode"] = "VideoEncode",
        ["gpu.engine.video_decode"] = "VideoDecode",
        ["gpu.engine.copy"] = "Copy",
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
                if (GpuEngineMetrics.TryGetValue(sample.Info.Name, out var engineType) && !sample.IsIdle)
                {
                    gpuEngines.Add(new PerfReportGpuEngine(engineType, Round(histogram.Mean), Round(histogram.Percentile(95))));
                }

                if (!Wanted(sample, prefix, includeIdle))
                {
                    continue;
                }

                var scale = sample.Info.Unit == PerfUnit.Microseconds ? 0.001 : 1.0;
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

        var ingressKind = PerfIngressClock.Kind;
        var trace = FrameTrace.Recorder.GetStatus();
        var windows = PerfWindows.Set.List();
        var (sensorsActive, sensorsReason) = SensorDemand(windows, trace.Running);

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
            new PerfReportGpu(gpuEngines, DedicatedBytes: null, Array.Empty<string>()),
            new PerfReportDwm(current.DwmRefreshHz is { } hz ? Round(hz) : null, composedPerSec),
            new PerfReportCapture(PerfIngressClock.Name(ingressKind), PerfIngressClock.MinOffsetMs is { } offset ? Round(offset) : null),
            new PerfReportSensors(sensorsActive, sensorsReason),
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

    // Sensors that wake threads run on demand: while a window is open or a trace runs. (The always-available process,
    // GC and DWM readings in this report cost nothing to leave idle.)
    private static (bool Active, string Reason) SensorDemand(IReadOnlyList<PerfWindowInfo> windows, bool traceRunning)
    {
        if (windows.Count > 0)
        {
            return (true, "window:" + string.Join(",", windows.Select(window => window.Name)));
        }

        return traceRunning ? (true, "trace") : (false, "idle");
    }

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
        PerfUnit.Percent => "percent",
        _ => "count",
    };

    private static double Rate(long count, double seconds) => seconds > 0 ? count / seconds : 0;

    private static double TicksToMs(long ticks) => ticks * 1000d / Stopwatch.Frequency;

    private static double Round(double value) => double.IsFinite(value) ? Math.Round(value, 4) : 0;
}
