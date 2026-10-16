using System;
using System.Collections.Generic;
using System.Threading;

namespace Sussudio.Services.Performance;

/// <summary>
/// Value of a three-way telemetry knob. <see cref="Auto"/> follows demand (an open measurement window or a running
/// frame trace); <see cref="On"/> and <see cref="Off"/> override demand in either direction.
/// </summary>
public enum TelemetrySensorMode
{
    Auto,
    On,
    Off,
}

/// <summary>
/// Parsing and wire names of the <c>auto|on|off</c> knobs. The knob catalog stores the canonical lower-case choice;
/// the environment variable may also be set by hand, so parsing is case-insensitive, accepts the boolean spellings
/// and never fails: anything unrecognised means <see cref="TelemetrySensorMode.Auto"/>, the documented default.
/// </summary>
public static class TelemetrySensorModes
{
    /// <summary>Gates every demand-driven sensor (UI responsiveness probe, PDH GPU engines, the NVML poll's sensor demand).</summary>
    public const string SensorsKnob = "SUSSUDIO_TELEMETRY_SENSORS";

    /// <summary>Gates the D3D11 timestamp queries around the preview render passes.</summary>
    public const string GpuTimingKnob = "SUSSUDIO_PREVIEW_GPU_TIMING";

    public static TelemetrySensorMode Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return TelemetrySensorMode.Auto;
        }

        var trimmed = text.Trim();
        if (trimmed.Equals("on", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("1", StringComparison.Ordinal)
            || trimmed.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return TelemetrySensorMode.On;
        }

        if (trimmed.Equals("off", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("0", StringComparison.Ordinal)
            || trimmed.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return TelemetrySensorMode.Off;
        }

        return TelemetrySensorMode.Auto;
    }

    public static string ToWire(TelemetrySensorMode mode) => mode switch
    {
        TelemetrySensorMode.On => "on",
        TelemetrySensorMode.Off => "off",
        _ => "auto",
    };
}

/// <summary>Whether demand-driven sensors should run, and the reason the perf report states.</summary>
public readonly record struct SensorDemandDecision(bool Active, string Reason);

/// <summary>
/// The demand rule, kept pure so every combination can be checked without a clock, a window set or a trace.
/// <c>off</c> wins over everything, <c>on</c> wins over demand, and <c>auto</c> is active exactly while a measurement
/// window is open or a frame trace is running. Reasons: <c>knob:off</c>, <c>knob:on</c>, <c>window:&lt;names&gt;</c>,
/// <c>trace</c> and <c>idle</c> (a window is named in preference to the trace when both exist).
/// </summary>
public static class SensorDemandPolicy
{
    /// <summary>Allocation-free form for polling paths.</summary>
    public static bool IsActive(TelemetrySensorMode mode, int openWindowCount, bool traceRunning) => mode switch
    {
        TelemetrySensorMode.On => true,
        TelemetrySensorMode.Off => false,
        _ => openWindowCount > 0 || traceRunning,
    };

    /// <summary>
    /// Whether GPU timestamp queries should run. The GPU timing knob wins when it is <c>on</c> or <c>off</c>; in
    /// <c>auto</c> it follows the sensor demand, including the sensors knob (so sensors <c>off</c> also quiets auto GPU timing).
    /// </summary>
    public static bool IsGpuTimingActive(
        TelemetrySensorMode gpuTimingMode,
        TelemetrySensorMode sensorsMode,
        int openWindowCount,
        bool traceRunning) => gpuTimingMode switch
    {
        TelemetrySensorMode.On => true,
        TelemetrySensorMode.Off => false,
        _ => IsActive(sensorsMode, openWindowCount, traceRunning),
    };

    /// <summary>Report form, with the reason. Allocates; control plane only.</summary>
    public static SensorDemandDecision Decide(TelemetrySensorMode mode, IReadOnlyList<string> openWindowNames, bool traceRunning)
    {
        switch (mode)
        {
            case TelemetrySensorMode.Off:
                return new SensorDemandDecision(false, "knob:off");
            case TelemetrySensorMode.On:
                return new SensorDemandDecision(true, "knob:on");
        }

        if (openWindowNames.Count > 0)
        {
            return new SensorDemandDecision(true, "window:" + string.Join(",", openWindowNames));
        }

        return traceRunning
            ? new SensorDemandDecision(true, "trace")
            : new SensorDemandDecision(false, "idle");
    }
}

/// <summary>
/// A boolean that is re-evaluated at most once per interval, for hot paths that must know whether a sensor is wanted
/// without paying for the answer on every frame. Between evaluations a read is one timestamp compare and one volatile
/// read: no allocation, no lock. The evaluation callback runs on whichever caller finds the interval elapsed; two
/// racing callers may both evaluate, which is harmless because the callback is a pure read of process state.
/// </summary>
public sealed class CachedDemand
{
    private readonly Func<bool> _evaluate;
    private readonly long _intervalTicks;
    private long _nextEvaluationTimestamp;
    private int _active;

    public CachedDemand(Func<bool> evaluate, long intervalTicks)
    {
        _evaluate = evaluate;
        _intervalTicks = Math.Max(1, intervalTicks);
    }

    /// <summary>The cached answer, re-evaluating first when <paramref name="nowTimestamp"/> is past the interval.</summary>
    public bool IsActive(long nowTimestamp)
    {
        if (nowTimestamp >= Volatile.Read(ref _nextEvaluationTimestamp))
        {
            // A throwing callback must not take the render thread down: keep the previous answer and retry next interval.
            try
            {
                Volatile.Write(ref _active, _evaluate() ? 1 : 0);
            }
            catch (Exception)
            {
                // Sensor demand is advisory; the last known answer stands.
            }

            Volatile.Write(ref _nextEvaluationTimestamp, nowTimestamp + _intervalTicks);
        }

        return Volatile.Read(ref _active) != 0;
    }
}

/// <summary>A sensor that runs only while there is demand for it. Start and Stop are idempotent and do not throw for ordinary reasons.</summary>
public interface IDemandSensor
{
    /// <summary>Stable short name used in the report details (for example <c>ui_probe</c>).</summary>
    string Name { get; }

    /// <summary>Starts the sensor thread or timer. Returns false when it is already running.</summary>
    bool Start();

    /// <summary>Stops the sensor and releases its thread or timer. Harmless when stopped.</summary>
    void Stop();

    /// <summary>One line for the report: state and counters. Cheap, thread-safe, never throws.</summary>
    string Describe();
}

/// <summary>
/// Starts the demand-driven sensors when demand appears and stops them when it ends, with no polling of its own:
/// the owners of the three demand inputs (the window set, the frame trace, the knob store) call
/// <see cref="Reevaluate"/> from their <c>Changed</c> events, so an idle app runs no sensor thread and no timer.
/// Reevaluate recomputes demand from current state rather than trusting the event, so concurrent or out-of-order
/// events converge on the right answer, and it only touches the sensors when the answer changed (a window restart
/// or an unrelated knob does not bounce a running sensor). A sensor that throws on start or stop is recorded and
/// does not stop the others. After <see cref="Shutdown"/> nothing starts again.
/// </summary>
public sealed class TelemetrySensorSupervisor
{
    private readonly object _gate = new();
    private readonly Func<TelemetrySensorMode> _readMode;
    private readonly Func<int> _openWindowCount;
    private readonly Func<bool> _traceRunning;
    private readonly IReadOnlyList<IDemandSensor> _sensors;
    private bool _active;
    private bool _shutdown;
    private string? _lastError;

    public TelemetrySensorSupervisor(
        Func<TelemetrySensorMode> readMode,
        Func<int> openWindowCount,
        Func<bool> traceRunning,
        IReadOnlyList<IDemandSensor> sensors)
    {
        _readMode = readMode;
        _openWindowCount = openWindowCount;
        _traceRunning = traceRunning;
        _sensors = sensors;
    }

    /// <summary>True while the sensors were last started (demand was present at the last <see cref="Reevaluate"/>).</summary>
    public bool IsActive
    {
        get
        {
            lock (_gate)
            {
                return _active;
            }
        }
    }

    /// <summary>The last start or stop failure (sensor name and exception type), or null.</summary>
    public string? LastError
    {
        get
        {
            lock (_gate)
            {
                return _lastError;
            }
        }
    }

    /// <summary>Recomputes demand and starts or stops every sensor if it changed. Safe to call from any thread, at any rate.</summary>
    public void Reevaluate()
    {
        lock (_gate)
        {
            if (_shutdown)
            {
                return;
            }

            var wanted = SensorDemandPolicy.IsActive(_readMode(), _openWindowCount(), _traceRunning());
            if (wanted == _active)
            {
                return;
            }

            _active = wanted;
            ApplyLocked(wanted);
        }
    }

    /// <summary>Stops every sensor and prevents any later start. Idempotent.</summary>
    public void Shutdown()
    {
        lock (_gate)
        {
            if (_shutdown)
            {
                return;
            }

            _shutdown = true;
            _active = false;
            ApplyLocked(start: false);
        }
    }

    /// <summary>One line per sensor for the report.</summary>
    public IReadOnlyList<string> Describe()
    {
        var lines = new List<string>(_sensors.Count);
        foreach (var sensor in _sensors)
        {
            try
            {
                lines.Add(sensor.Describe());
            }
            catch (Exception ex)
            {
                lines.Add(sensor.Name + ": describe failed (" + ex.GetType().Name + ")");
            }
        }

        return lines;
    }

    private void ApplyLocked(bool start)
    {
        foreach (var sensor in _sensors)
        {
            try
            {
                if (start)
                {
                    sensor.Start();
                }
                else
                {
                    sensor.Stop();
                }
            }
            catch (Exception ex)
            {
                _lastError = sensor.Name + ": " + (start ? "start" : "stop") + " failed (" + ex.GetType().Name + ")";
            }
        }
    }
}
