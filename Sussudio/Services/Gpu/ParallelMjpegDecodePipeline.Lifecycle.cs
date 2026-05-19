using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Gpu;

internal sealed partial class ParallelMjpegDecodePipeline
{
    private Thread? _emitThread;
    private readonly AutoResetEvent _emitSignal = new(false);

    public void Dispose()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        BeginStop();
        if (!TryWaitForShutdown(TimeSpan.FromSeconds(5), out var failureReason))
        {
            Logger.Log(
                $"PARALLEL_MJPEG_PIPELINE_DISPOSE_TIMEOUT reason='{failureReason ?? "unknown"}' " +
                $"decoded={_totalFramesDecoded} emitted={_totalFramesEmitted} dropped={_totalFramesDropped} skips={_reorderSkips}");
            return;
        }

        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        CleanupResources();
    }

    public bool TryStop(TimeSpan timeout, out string? failureReason)
    {
        failureReason = null;

        if (Volatile.Read(ref _threadsStopped) != 0)
        {
            return true;
        }

        BeginStop();
        return TryWaitForShutdown(timeout, out failureReason);
    }

    private void BeginStop()
    {
        if (Interlocked.Exchange(ref _stopRequested, 1) != 0)
        {
            SignalEmitter("stop_already_requested");
            return;
        }

        _stopped = true;
        _workQueue.Writer.TryComplete();

        lock (_reorderLock)
        {
            Monitor.PulseAll(_reorderLock);
        }

        SignalEmitter("stop_requested");
    }

    private void StartEmitter()
    {
        _emitThread = new Thread(EmitLoop)
        {
            IsBackground = true,
            Name = "MjpegEmitter"
        };
        _emitThread.Start();
    }

    private void SignalEmitter(string operation)
    {
        try
        {
            _emitSignal.Set();
        }
        catch (ObjectDisposedException)
        {
            Logger.Log($"MJPEG_PIPELINE_EMIT_SIGNAL_SKIPPED op={operation} reason=disposed");
        }
    }

    private bool TryWaitForShutdown(TimeSpan timeout, out string? failureReason)
    {
        failureReason = null;

        if (Volatile.Read(ref _threadsStopped) != 0)
        {
            return true;
        }

        var deadline = Stopwatch.GetTimestamp() + (long)(timeout.TotalSeconds * Stopwatch.Frequency);
        for (var i = 0; i < _workers.Length; i++)
        {
            var worker = _workers[i];
            if (worker == null || !worker.IsAlive)
            {
                continue;
            }

            if (ReferenceEquals(Thread.CurrentThread, worker))
            {
                failureReason = $"worker_self_join index={i}";
                return false;
            }

            var remaining = GetRemainingTimeout(deadline);
            if (remaining <= TimeSpan.Zero || !worker.Join(remaining))
            {
                failureReason = $"worker_timeout index={i}";
                return false;
            }
        }

        SignalEmitter("wait_for_shutdown");
        if (_emitThread is { IsAlive: true } emitThread)
        {
            if (ReferenceEquals(Thread.CurrentThread, emitThread))
            {
                failureReason = "emitter_self_join";
                return false;
            }

            var remaining = GetRemainingTimeout(deadline);
            if (remaining <= TimeSpan.Zero || !emitThread.Join(remaining))
            {
                failureReason = "emitter_timeout";
                return false;
            }
        }

        Interlocked.Exchange(ref _threadsStopped, 1);
        return true;
    }

    private void SignalFatalError(Exception ex)
    {
        BeginStop();

        if (Interlocked.Exchange(ref _fatalErrorSignaled, 1) != 0)
        {
            return;
        }

        if (_fatalErrorCallback == null)
        {
            return;
        }

        ThreadPool.UnsafeQueueUserWorkItem(
            static state =>
            {
                var (callback, exception) = ((Action<Exception>, Exception))state!;
                try
                {
                    callback(exception);
                }
                catch (Exception callbackEx)
                {
                    Logger.Log($"MJPEG_FATAL_CALLBACK_FAIL type={callbackEx.GetType().Name} msg={callbackEx.Message}");
                }
            },
            (_fatalErrorCallback, ex));
    }

    private static TimeSpan GetRemainingTimeout(long deadlineTimestamp)
    {
        var remainingTicks = deadlineTimestamp - Stopwatch.GetTimestamp();
        if (remainingTicks <= 0)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromSeconds(remainingTicks / (double)Stopwatch.Frequency);
    }
}
