using System;
using System.Collections.Generic;
using System.Threading;

namespace Sussudio.Services.Performance;

/// <summary>
/// What the perf report's <c>Sensors</c> section states: whether demand-driven sensors are wanted right now and why,
/// the value of the sensors knob, and one line per sensor and per GPU-timing state.
/// </summary>
public sealed record TelemetrySensorsStatus(bool Active, string Reason, string Mode, IReadOnlyList<string> Details);

/// <summary>
/// Process-wide owner of the demand-driven telemetry sensors: the UI responsiveness probe and the PDH GPU engine
/// sampler. <see cref="Configure"/> (called once from the main window) builds them and wires the three demand inputs
/// (open measurement windows, a running frame trace, the knob store) to a <see cref="TelemetrySensorSupervisor"/>
/// through their <c>Changed</c> events, so an idle app holds no sensor thread and no timer. The GPU timestamp queries
/// of the preview renderer follow the same demand through <see cref="IsGpuTimingWanted"/> but are owned by the
/// renderer (they need its device); the renderer reports their state back with <see cref="SetGpuTimingState"/> so the
/// report can say why <c>preview.gpu_render</c> is or is not recording. VRAM is read on demand at report time by
/// <see cref="GpuMemorySensor"/> and has no thread. Contract: docs/architecture/performance-telemetry.md section 2.8.
/// </summary>
public static class TelemetrySensors
{
    private static readonly object Gate = new();
    private static readonly Action DemandChangedHandler = OnDemandInputChanged;

    // Knob values cached as ints so IsGpuTimingWanted (polled by the render thread) never reads the environment.
    // They are refreshed from the environment at start-up and by every demand event; a knob change raises one.
    private static int _sensorsMode = ReadMode(TelemetrySensorModes.SensorsKnob);
    private static int _gpuTimingMode = ReadMode(TelemetrySensorModes.GpuTimingKnob);
    private static string _gpuTimingState = "not created";

    private static TelemetrySensorSupervisor? _supervisor;
    private static GpuEngineSensor? _gpuEngines;
    private static bool _subscribed;

    /// <summary>
    /// Builds the sensors and starts watching demand. <paramref name="uiEnqueue"/> is the main window's non-blocking
    /// dispatcher enqueue (<c>DispatcherQueue.TryEnqueue</c>); null skips the UI probe (a host without a UI thread).
    /// A second call tears the first configuration down and replaces it.
    /// </summary>
    public static void Configure(Func<Action, bool>? uiEnqueue)
    {
        Shutdown();

        var sensors = new List<IDemandSensor>(2);
        if (uiEnqueue is not null)
        {
            sensors.Add(new UiResponsivenessProbe(uiEnqueue));
        }

        var gpuEngines = new GpuEngineSensor(() => new NativePdhGpuCounterReader(), Environment.ProcessId);
        sensors.Add(gpuEngines);

        var supervisor = new TelemetrySensorSupervisor(
            () => TelemetrySensorModes.Parse(Environment.GetEnvironmentVariable(TelemetrySensorModes.SensorsKnob)),
            () => PerfWindows.Set.OpenCount,
            () => FrameTrace.IsEnabled,
            sensors);

        lock (Gate)
        {
            _gpuEngines = gpuEngines;
            _supervisor = supervisor;
            if (!_subscribed)
            {
                PerfWindows.Set.Changed += DemandChangedHandler;
                FrameTrace.Recorder.Changed += DemandChangedHandler;
                TuningKnobs.Changed += DemandChangedHandler;
                _subscribed = true;
            }
        }

        OnDemandInputChanged();
    }

    /// <summary>Stops every sensor and stops listening for demand. Idempotent; after it nothing starts until <see cref="Configure"/>.</summary>
    public static void Shutdown()
    {
        TelemetrySensorSupervisor? supervisor;
        lock (Gate)
        {
            supervisor = _supervisor;
            _supervisor = null;
            _gpuEngines = null;
            if (_subscribed)
            {
                PerfWindows.Set.Changed -= DemandChangedHandler;
                FrameTrace.Recorder.Changed -= DemandChangedHandler;
                TuningKnobs.Changed -= DemandChangedHandler;
                _subscribed = false;
            }
        }

        supervisor?.Shutdown();
    }

    /// <summary>
    /// Whether the preview renderer should create GPU timestamp queries now: the GPU timing knob when <c>on</c> or
    /// <c>off</c>, otherwise the sensor demand. Allocation-free and lock-free (the render thread wraps it in a
    /// <see cref="CachedDemand"/> and asks about once a second).
    /// </summary>
    public static bool IsGpuTimingWanted() => SensorDemandPolicy.IsGpuTimingActive(
        (TelemetrySensorMode)Volatile.Read(ref _gpuTimingMode),
        (TelemetrySensorMode)Volatile.Read(ref _sensorsMode),
        PerfWindows.Set.OpenCount,
        FrameTrace.IsEnabled);

    /// <summary>The renderer's one-line GPU timing state for the report (for example <c>active slots=6</c> or <c>disabled: WARP adapter</c>).</summary>
    public static void SetGpuTimingState(string state) => Volatile.Write(ref _gpuTimingState, state);

    /// <summary>The PDH GPU engine sensor's state, or null before <see cref="Configure"/> or after <see cref="Shutdown"/>.</summary>
    public static GpuEngineSensorStatus? GpuEngineStatus
    {
        get
        {
            GpuEngineSensor? sensor;
            lock (Gate)
            {
                sensor = _gpuEngines;
            }

            return sensor?.Status;
        }
    }

    /// <summary>Demand decision and sensor state for the report. Control plane only; allocates.</summary>
    public static TelemetrySensorsStatus Describe(IReadOnlyList<string> openWindowNames, bool traceRunning)
    {
        var mode = TelemetrySensorModes.Parse(Environment.GetEnvironmentVariable(TelemetrySensorModes.SensorsKnob));
        var gpuMode = TelemetrySensorModes.Parse(Environment.GetEnvironmentVariable(TelemetrySensorModes.GpuTimingKnob));
        var decision = SensorDemandPolicy.Decide(mode, openWindowNames, traceRunning);

        TelemetrySensorSupervisor? supervisor;
        lock (Gate)
        {
            supervisor = _supervisor;
        }

        var details = new List<string>();
        if (supervisor is null)
        {
            details.Add("sensors: not configured (no UI host in this process)");
        }
        else
        {
            details.AddRange(supervisor.Describe());
            if (supervisor.LastError is { } error)
            {
                details.Add("supervisor: " + error);
            }
        }

        var gpuTimingWanted = SensorDemandPolicy.IsGpuTimingActive(gpuMode, mode, openWindowNames.Count, traceRunning);
        details.Add(
            $"gpu_timing: mode={TelemetrySensorModes.ToWire(gpuMode)} wanted={(gpuTimingWanted ? "yes" : "no")} state={Volatile.Read(ref _gpuTimingState)}");
        details.Add("vram: on demand at report time");
        return new TelemetrySensorsStatus(decision.Active, decision.Reason, TelemetrySensorModes.ToWire(mode), details);
    }

    private static void OnDemandInputChanged()
    {
        Volatile.Write(ref _sensorsMode, ReadMode(TelemetrySensorModes.SensorsKnob));
        Volatile.Write(ref _gpuTimingMode, ReadMode(TelemetrySensorModes.GpuTimingKnob));

        TelemetrySensorSupervisor? supervisor;
        lock (Gate)
        {
            supervisor = _supervisor;
        }

        supervisor?.Reevaluate();
    }

    private static int ReadMode(string knob) => (int)TelemetrySensorModes.Parse(Environment.GetEnvironmentVariable(knob));
}
