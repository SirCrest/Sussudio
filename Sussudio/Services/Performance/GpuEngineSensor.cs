using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

namespace Sussudio.Services.Performance;

/// <summary>One PDH instance of a counter: its full instance name (for example <c>pid_4242_luid_0x0_0x1_phys_0_eng_0_engtype_3D</c>) and value.</summary>
public readonly record struct GpuCounterItem(string Instance, double Value);

public enum GpuCounterReadResult
{
    /// <summary>The sample is valid and is recorded, including genuine zeros.</summary>
    Valid,

    /// <summary>
    /// No usable value this tick: the first sample of a rate counter, or PDH has no instance for the process (it has
    /// not touched the GPU yet, or just released its last device). Nothing is recorded, so a missing sample never
    /// reads as 0 %. Try again next tick.
    /// </summary>
    Pending,

    /// <summary>The counters cannot be read at all; <c>detail</c> says why.</summary>
    Unavailable,
}

/// <summary>
/// Source of GPU utilization counters, one process at a time. The production implementation reads PDH
/// (<c>NativePdh</c>); the seam exists so the sensor's failure handling can be exercised without a GPU. An
/// implementation is used from one thread only (the sensor thread opens, reads and disposes it).
/// </summary>
public interface IGpuCounterReader : IDisposable
{
    /// <summary>Prepares the counters for <paramref name="processId"/>. Returns null on success, or a one-line reason.</summary>
    string? Open(int processId);

    /// <summary>
    /// Takes one sample. <paramref name="engines"/> receives <c>\GPU Engine(*)\Utilization Percentage</c> instances
    /// (percent) and <paramref name="memory"/> receives <c>\GPU Process Memory(*)\Dedicated Usage</c> instances
    /// (bytes). Both lists are cleared first. May return instances of other processes; the caller filters.
    /// </summary>
    GpuCounterReadResult Read(List<GpuCounterItem> engines, List<GpuCounterItem> memory, out string? detail);
}

public enum GpuEngineKind
{
    Engine3D,
    VideoEncode,
    VideoDecode,
    Copy,
    Other,
}

/// <summary>Per-engine-type utilization of one process in percent, plus the dedicated VRAM it holds.</summary>
public readonly record struct GpuEngineSample(
    double Engine3D,
    double VideoEncode,
    double VideoDecode,
    double Copy,
    double Other,
    long DedicatedBytes,
    int Instances);

/// <summary>
/// Turns raw PDH instances into one process's per-engine-type totals. Instance names look like
/// <c>pid_&lt;pid&gt;_luid_0x..._0x..._phys_&lt;n&gt;_eng_&lt;k&gt;_engtype_&lt;Type&gt;</c>. Only instances of exactly the requested pid are
/// counted: the wildcard <c>pid_1086*</c> also matches <c>pid_10860_...</c>, so the prefix must end at the underscore.
/// A type's value is the sum over the process's instances of that type; it can exceed 100 when the process uses
/// several physical engines or adapters of one type, and is documented as such in the report. Malformed names
/// (no engine type) count as <see cref="GpuEngineKind.Other"/>; non-finite and negative values are ignored.
/// </summary>
public static class GpuEngineAggregator
{
    private const string EngineTypeMarker = "_engtype_";

    public static GpuEngineSample Aggregate(
        int processId,
        IReadOnlyList<GpuCounterItem> engines,
        IReadOnlyList<GpuCounterItem> memory)
    {
        var prefix = "pid_" + processId.ToString(CultureInfo.InvariantCulture) + "_";
        double d3 = 0, encode = 0, decode = 0, copy = 0, other = 0;
        var instances = 0;
        for (var i = 0; i < engines.Count; i++)
        {
            var item = engines[i];
            if (!item.Instance.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !IsUsable(item.Value))
            {
                continue;
            }

            instances++;
            switch (Classify(item.Instance))
            {
                case GpuEngineKind.Engine3D: d3 += item.Value; break;
                case GpuEngineKind.VideoEncode: encode += item.Value; break;
                case GpuEngineKind.VideoDecode: decode += item.Value; break;
                case GpuEngineKind.Copy: copy += item.Value; break;
                default: other += item.Value; break;
            }
        }

        double dedicated = 0;
        for (var i = 0; i < memory.Count; i++)
        {
            var item = memory[i];
            if (item.Instance.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && IsUsable(item.Value))
            {
                dedicated += item.Value;
            }
        }

        var dedicatedBytes = dedicated >= long.MaxValue ? long.MaxValue : (long)dedicated;
        return new GpuEngineSample(d3, encode, decode, copy, other, dedicatedBytes, instances);
    }

    /// <summary>Engine type of an instance name; unknown or missing types are <see cref="GpuEngineKind.Other"/>.</summary>
    public static GpuEngineKind Classify(string instance)
    {
        var marker = instance.LastIndexOf(EngineTypeMarker, StringComparison.OrdinalIgnoreCase);
        if (marker < 0)
        {
            return GpuEngineKind.Other;
        }

        var type = instance.AsSpan(marker + EngineTypeMarker.Length);
        if (type.Equals("3D", StringComparison.OrdinalIgnoreCase))
        {
            return GpuEngineKind.Engine3D;
        }

        if (type.Equals("VideoEncode", StringComparison.OrdinalIgnoreCase))
        {
            return GpuEngineKind.VideoEncode;
        }

        if (type.Equals("VideoDecode", StringComparison.OrdinalIgnoreCase))
        {
            return GpuEngineKind.VideoDecode;
        }

        if (type.Equals("Copy", StringComparison.OrdinalIgnoreCase))
        {
            return GpuEngineKind.Copy;
        }

        return GpuEngineKind.Other;
    }

    private static bool IsUsable(double value) => double.IsFinite(value) && value >= 0;
}

public enum GpuEngineSensorState
{
    Stopped,

    /// <summary>The sensor thread is running but the counters are not open yet, or opening failed and will be retried.</summary>
    Opening,

    Running,
}

/// <summary>What the sensor is doing, for the report's <c>Sensors.Details</c>.</summary>
public sealed record GpuEngineSensorStatus(
    GpuEngineSensorState State,
    string? LastError,
    long Samples,
    long ConsecutivePending,
    long OpenFailures,
    long? DedicatedBytes);

/// <summary>
/// Samples this process's GPU engine utilization at 1 Hz while demand lasts and records it into the
/// <c>gpu.engine.*</c> histograms (hundredths of a percent). One dedicated thread owns the whole counter lifetime
/// (open, read, close), so no handle is ever used from two threads and Stop only has to signal and join.
/// An unavailable PDH is a state, never an exception: open failures are retried with a bounded backoff (the counters
/// of a process appear only once it first uses the GPU, and PDH can be briefly unavailable), and the last error is
/// kept for the report. A tick with no usable data (the first sample of a rate counter, or no PDH instance for the
/// process) records nothing, so a missing sample can never read as 0 %; the report states the pending count.
/// Contract: docs/architecture/performance-telemetry.md section 2.8.
/// </summary>
public sealed class GpuEngineSensor : IDemandSensor
{
    public const int DefaultPeriodMs = 1000;

    /// <summary>Longest wait between open attempts while the counters are unavailable.</summary>
    public const int MaxOpenRetryMs = 15000;

    private const int StopJoinTimeoutMs = 3000;

    // 10000 % is far beyond any real sum; it only bounds a corrupt counter value inside the histogram range.
    private const double MaxCentiPercent = 1_000_000;

    private readonly Func<IGpuCounterReader> _readerFactory;
    private readonly int _processId;
    private readonly int _periodMs;
    private readonly object _gate = new();
    private readonly List<GpuCounterItem> _engines = new();
    private readonly List<GpuCounterItem> _memory = new();

    private Thread? _thread;
    private ManualResetEventSlim? _stop;

    private int _state;
    private string? _lastError;
    private long _samples;
    private long _consecutivePending;
    private long _openFailures;
    private long _dedicatedBytes = -1;

    public GpuEngineSensor(Func<IGpuCounterReader> readerFactory, int processId, int periodMs = DefaultPeriodMs)
    {
        _readerFactory = readerFactory;
        _processId = processId;
        _periodMs = Math.Max(1, periodMs);
    }

    public string Name => "gpu_engines";

    /// <summary>
    /// One report line. A sensor that is open but sampling nothing says so (no PDH instance for this process yet), and
    /// an unavailable one carries the last PDH error: PDH trouble is a visible state, not an exception.
    /// </summary>
    public string Describe()
    {
        var status = Status;
        switch (status.State)
        {
            case GpuEngineSensorState.Stopped:
                return "gpu_engines: stopped";
            case GpuEngineSensorState.Opening:
                return $"gpu_engines: unavailable, retrying (open_failures={status.OpenFailures}, last_error={status.LastError ?? "none"})";
            default:
                var note = status.ConsecutivePending >= 3
                    ? $" no PDH instance for this process for {status.ConsecutivePending} ticks"
                    : string.Empty;
                var error = status.LastError is null ? string.Empty : $" last_error={status.LastError}";
                return $"gpu_engines: running samples={status.Samples}{note}{error}";
        }
    }

    public GpuEngineSensorStatus Status
    {
        get
        {
            var dedicated = Volatile.Read(ref _dedicatedBytes);
            return new GpuEngineSensorStatus(
                (GpuEngineSensorState)Volatile.Read(ref _state),
                Volatile.Read(ref _lastError),
                Volatile.Read(ref _samples),
                Volatile.Read(ref _consecutivePending),
                Volatile.Read(ref _openFailures),
                dedicated >= 0 ? dedicated : null);
        }
    }

    /// <summary>Starts the sensor thread. Returns false when it is already running.</summary>
    public bool Start()
    {
        lock (_gate)
        {
            if (_thread is not null)
            {
                return false;
            }

            var stop = new ManualResetEventSlim(false);
            var thread = new Thread(() => Loop(stop))
            {
                IsBackground = true,
                Name = "Sussudio.TelemetryGpuEngines",
            };
            _stop = stop;
            _thread = thread;
            Volatile.Write(ref _state, (int)GpuEngineSensorState.Opening);
            thread.Start();
            return true;
        }
    }

    /// <summary>Stops the thread and waits for it (bounded). Harmless when not running.</summary>
    public void Stop()
    {
        Thread? thread;
        ManualResetEventSlim? stop;
        lock (_gate)
        {
            thread = _thread;
            stop = _stop;
            _thread = null;
            _stop = null;
        }

        if (thread is null || stop is null)
        {
            return;
        }

        stop.Set();
        thread.Join(StopJoinTimeoutMs);
    }

    /// <summary>
    /// One read of an open reader, aggregated and recorded. Internal so tests can drive the failure handling without
    /// a thread. Returns false when the reader reported <see cref="GpuCounterReadResult.Unavailable"/>.
    /// </summary>
    internal bool SampleOnce(IGpuCounterReader reader)
    {
        var result = reader.Read(_engines, _memory, out var detail);
        if (result == GpuCounterReadResult.Unavailable)
        {
            Volatile.Write(ref _lastError, detail ?? "GPU counters unavailable");
            return false;
        }

        if (result == GpuCounterReadResult.Pending)
        {
            Interlocked.Increment(ref _consecutivePending);
            return true;
        }

        var sample = GpuEngineAggregator.Aggregate(_processId, _engines, _memory);
        if (sample.Instances == 0)
        {
            // The reader returned instances, but none of them is this process (a longer pid sharing the prefix, for
            // example): the same "no data" as an empty read, never a recorded 0 %.
            Interlocked.Increment(ref _consecutivePending);
            return true;
        }

        Record(PerfMetrics.GpuEngine3d, sample.Engine3D);
        Record(PerfMetrics.GpuEngineVideoEncode, sample.VideoEncode);
        Record(PerfMetrics.GpuEngineVideoDecode, sample.VideoDecode);
        Record(PerfMetrics.GpuEngineCopy, sample.Copy);
        Record(PerfMetrics.GpuEngineOther, sample.Other);
        Volatile.Write(ref _dedicatedBytes, sample.DedicatedBytes);
        Volatile.Write(ref _lastError, null);
        Volatile.Write(ref _consecutivePending, 0);
        Interlocked.Increment(ref _samples);
        return true;
    }

    private static void Record(PerfHistogram histogram, double percent)
    {
        var centi = Math.Round(percent * 100d);
        histogram.Record(centi > MaxCentiPercent ? (long)MaxCentiPercent : (long)centi);
    }

    private void Loop(ManualResetEventSlim stop)
    {
        using var hotThread = HotThreads.Register("telemetry.gpu_engines");
        IGpuCounterReader? reader = null;
        try
        {
            var retryMs = _periodMs;
            while (!stop.IsSet)
            {
                if (reader is null)
                {
                    reader = TryOpen();
                    if (reader is null)
                    {
                        if (stop.Wait(retryMs))
                        {
                            break;
                        }

                        retryMs = Math.Min(MaxOpenRetryMs, retryMs * 2);
                        continue;
                    }

                    retryMs = _periodMs;
                    Volatile.Write(ref _state, (int)GpuEngineSensorState.Running);
                }

                if (stop.Wait(_periodMs))
                {
                    break;
                }

                bool ok;
                try
                {
                    ok = SampleOnce(reader);
                }
                catch (Exception ex)
                {
                    Volatile.Write(ref _lastError, "GPU counter read failed: " + ex.GetType().Name);
                    ok = false;
                }

                if (!ok)
                {
                    // Close and reopen: the counters may have gone away (driver reset, PDH reload).
                    SafeDispose(reader);
                    reader = null;
                    Volatile.Write(ref _dedicatedBytes, -1);
                    Volatile.Write(ref _state, (int)GpuEngineSensorState.Opening);
                }
            }
        }
        finally
        {
            SafeDispose(reader);

            // A dedicated-memory figure from a sensor that is no longer measuring would read as current in the report.
            Volatile.Write(ref _dedicatedBytes, -1);
            Volatile.Write(ref _state, (int)GpuEngineSensorState.Stopped);
            stop.Dispose();
        }
    }

    private IGpuCounterReader? TryOpen()
    {
        IGpuCounterReader? reader = null;
        try
        {
            reader = _readerFactory();
            var error = reader.Open(_processId);
            if (error is null)
            {
                return reader;
            }

            Volatile.Write(ref _lastError, error);
        }
        catch (Exception ex)
        {
            Volatile.Write(ref _lastError, "GPU counter open failed: " + ex.GetType().Name);
        }

        Interlocked.Increment(ref _openFailures);
        SafeDispose(reader);
        return null;
    }

    private static void SafeDispose(IGpuCounterReader? reader)
    {
        try
        {
            reader?.Dispose();
        }
        catch (Exception)
        {
            // Closing a counter that is already gone must not mask the reason the sensor is reopening.
        }
    }
}
