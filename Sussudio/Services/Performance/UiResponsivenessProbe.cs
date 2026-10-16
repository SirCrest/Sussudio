using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Performance;

/// <summary>Counts of a <see cref="UiResponsivenessProbe"/> since it was created.</summary>
public readonly record struct UiProbeStats(
    bool Running,
    long Enqueued,
    long Completed,
    long SkippedWhileBusy,
    long EnqueueFailures);

/// <summary>
/// Measures how long the UI dispatcher takes to start a no-op work item: the number a human perceives as "the UI is
/// frozen". While running, a dedicated thread wakes every <see cref="DefaultPeriodMs"/> and enqueues one no-op through
/// the injected <c>enqueue</c> delegate (the app passes <c>DispatcherQueue.TryEnqueue</c>); when the dispatcher runs
/// it, the enqueue-to-run wait lands in <c>ui.dispatcher_latency</c>, waits above 50 ms increment
/// <c>ui.stall_50ms</c>, and waits above one 60 Hz frame (16 ms) also record a <c>UiDispatcherLatency</c> trace span.
///
/// The probe never blocks the UI thread and never queues work behind itself: at most one probe is outstanding, and a
/// tick that finds the previous probe still waiting skips (a dispatcher stalled for seconds yields one long sample
/// when it recovers, not a backlog). <c>enqueue</c> must be non-blocking; a delegate that returns false (the queue is
/// shutting down) or throws is counted and retried next tick, so the probe cannot take the app down. <see cref="Stop"/>
/// is bounded because the loop thread only ever waits on its stop event. Start and stop are idempotent and
/// restartable; the sensor supervisor owns when to call them (while a window is open or a trace runs).
/// Contract: docs/architecture/performance-telemetry.md section 2.8.
/// </summary>
public sealed class UiResponsivenessProbe : IDemandSensor
{
    public const int DefaultPeriodMs = 100;

    /// <summary>A wait above this many milliseconds increments <c>ui.stall_50ms</c>.</summary>
    public const long StallThresholdMs = 50;

    /// <summary>A wait above this many milliseconds (one 60 Hz frame) is also written to the frame trace.</summary>
    public const long TraceThresholdMs = 16;

    private const int StopJoinTimeoutMs = 2000;

    private readonly Func<Action, bool> _enqueue;
    private readonly Func<long> _timestamp;
    private readonly int _periodMs;
    private readonly long _stallTicks;
    private readonly long _traceTicks;
    private readonly Action _onDispatcher;
    private readonly object _gate = new();

    private Thread? _thread;
    private ManualResetEventSlim? _stop;

    // 0 = no probe in flight, 1 = one is enqueued and has not run. Only one probe exists, so _enqueuedAt needs no queue.
    private int _outstanding;
    private long _enqueuedAt;
    private int _uiThreadNamed;
    private volatile bool _running;

    private long _enqueued;
    private long _completed;
    private long _skippedWhileBusy;
    private long _enqueueFailures;

    public UiResponsivenessProbe(Func<Action, bool> enqueue, Func<long>? timestamp = null, int periodMs = DefaultPeriodMs)
    {
        _enqueue = enqueue;
        _timestamp = timestamp ?? Stopwatch.GetTimestamp;
        _periodMs = Math.Max(1, periodMs);
        _stallTicks = StallThresholdMs * Stopwatch.Frequency / 1000;
        _traceTicks = TraceThresholdMs * Stopwatch.Frequency / 1000;
        _onDispatcher = OnDispatcher;
    }

    public string Name => "ui_probe";

    public bool IsRunning => _running;

    public string Describe()
    {
        var stats = Stats;
        return stats.Running
            ? $"ui_probe: running enqueued={stats.Enqueued} completed={stats.Completed} skipped_busy={stats.SkippedWhileBusy} enqueue_failures={stats.EnqueueFailures}"
            : "ui_probe: stopped";
    }

    public UiProbeStats Stats => new(
        _running,
        Volatile.Read(ref _enqueued),
        Volatile.Read(ref _completed),
        Volatile.Read(ref _skippedWhileBusy),
        Volatile.Read(ref _enqueueFailures));

    /// <summary>Starts the probe thread. Returns false when it is already running.</summary>
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
                Name = "Sussudio.TelemetryUiProbe",
            };
            _stop = stop;
            _thread = thread;
            _running = true;
            thread.Start();
            return true;
        }
    }

    /// <summary>
    /// Stops the probe thread and waits for it (bounded). Harmless when not running. A probe already enqueued still
    /// runs when the dispatcher gets to it and records its wait; nothing new is enqueued.
    /// </summary>
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
            _running = false;
        }

        if (thread is null || stop is null)
        {
            return;
        }

        stop.Set();
        // The loop only waits on the stop event and calls a non-blocking enqueue, so this returns at once; the timeout
        // only guards a misbehaving delegate. The event is released by the loop thread, never here, so a timed-out
        // join cannot dispose an event the thread still waits on.
        thread.Join(StopJoinTimeoutMs);
    }

    /// <summary>One probe attempt, the body of the loop. Internal so tests can drive it without a thread or a sleep.</summary>
    internal void Tick()
    {
        if (Interlocked.CompareExchange(ref _outstanding, 1, 0) != 0)
        {
            Interlocked.Increment(ref _skippedWhileBusy);
            return;
        }

        _enqueuedAt = _timestamp();
        bool queued;
        try
        {
            queued = _enqueue(_onDispatcher);
        }
        catch (Exception)
        {
            queued = false;
        }

        if (queued)
        {
            Interlocked.Increment(ref _enqueued);
            return;
        }

        // Nothing is in flight: release the slot so the next tick tries again.
        Interlocked.Increment(ref _enqueueFailures);
        Volatile.Write(ref _outstanding, 0);
    }

    // Runs on the UI thread. Allocation-free after the first call.
    private void OnDispatcher()
    {
        var startedAt = _enqueuedAt;
        var waitTicks = _timestamp() - startedAt;
        try
        {
            if (waitTicks < 0)
            {
                waitTicks = 0;
            }

            PerfMetrics.UiDispatcherLatency.RecordTicks(waitTicks);
            Interlocked.Increment(ref _completed);
            if (waitTicks > _stallTicks)
            {
                PerfMetrics.UiStall50Ms.Increment();
            }

            if (waitTicks > _traceTicks && FrameTrace.IsEnabled)
            {
                if (Interlocked.Exchange(ref _uiThreadNamed, 1) == 0)
                {
                    FrameTrace.NameCurrentThread("ui");
                }

                // The span is enqueue to run, drawn on the UI thread's track; no frame owns it (id -1).
                FrameTrace.Record(FrameTraceEventKind.UiDispatcherLatency, startedAt, waitTicks, -1);
            }
        }
        finally
        {
            Volatile.Write(ref _outstanding, 0);
        }
    }

    private void Loop(ManualResetEventSlim stop)
    {
        using var hotThread = HotThreads.Register("telemetry.ui_probe");
        try
        {
            while (!stop.Wait(_periodMs))
            {
                Tick();
            }
        }
        finally
        {
            stop.Dispose();
        }
    }
}
