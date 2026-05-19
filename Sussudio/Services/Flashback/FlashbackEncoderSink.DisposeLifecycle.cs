using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    // REVIEWED 2026-04-07: IDisposable fallback only; all callers use DisposeAsync.
    // CaptureService.DisposeFlashbackPreviewBackendAsync awaits DisposeAsync directly.
    public void Dispose()
    {
        if (_disposed) return;
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (Interlocked.Exchange(ref _recordingActive, 0) != 0)
        {
            ResumeEvictionBestEffort(_bufferManager, "dispose");
        }

        lock (_sync)
        {
            _started = false;
        }

        CompleteWriter(_videoQueue);
        CompleteWriter(_audioQueue);
        CompleteWriter(_microphoneQueue);
        CompleteWriter(_gpuQueue);
        CancelEncodingCts("dispose");

        if (_encodingTask == null)
        {
            FinalizeDisposeCore();
            return;
        }

        var completedTask = await Task.WhenAny(_encodingTask, Task.Delay(DisposeTimeoutMs)).ConfigureAwait(false);
        if (ReferenceEquals(completedTask, _encodingTask))
        {
            ObserveEncodingTaskCompletion(_encodingTask);
            FinalizeDisposeCore();
            return;
        }

        Logger.Log($"FLASHBACK_SINK_DISPOSE_DEFERRED timeout_ms={DisposeTimeoutMs}");
        ScheduleDeferredDisposeCleanup(_encodingTask);
    }

    private void ScheduleDeferredDisposeCleanup(Task encodingTask)
    {
        if (Interlocked.CompareExchange(ref _deferredDisposeScheduled, 1, 0) != 0)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await encodingTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _encodingFailure ??= ex;
            }
            finally
            {
                FinalizeDisposeCore();
                Logger.Log("FLASHBACK_SINK_DISPOSE_DEFERRED_COMPLETE");
            }
        });
    }

    private void ObserveEncodingTaskCompletion(Task encodingTask)
    {
        try
        {
            encodingTask.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _encodingFailure ??= ex;
        }
    }

    private void FinalizeDisposeCore()
    {
        if (Interlocked.CompareExchange(ref _disposeFinalized, 1, 0) != 0)
        {
            return;
        }

        ReturnAllRemainingQueuedBuffers();

        DisposeCtsBestEffort(_cts, "finalize_dispose");
        _cts = null;
        _videoQueue = null;
        _audioQueue = null;
        _microphoneQueue = null;
        _gpuQueue = null;
        _gpuEncodingEnabled = false;
        _audioEnabled = false;
        _microphoneEnabled = false;
        _sessionContext = null;
        _width = 0;
        _height = 0;
        _tsFilePath = null;
        _recordingOutputPath = string.Empty;
        _segmentStartPts = TimeSpan.Zero;
        _segmentDuration = TimeSpan.Zero;
        _ptsBaseOffset = TimeSpan.Zero;
        Interlocked.Exchange(ref _segmentStartBytes, 0);
        _encodingTask = null;
        DisposeWorkAvailableBestEffort("finalize_dispose");
        CompletePendingForceRotateWithEmptyResult();
        DisposeEncoderBestEffort("finalize_dispose");

        if (_ownsBufferManager)
        {
            try
            {
                _bufferManager.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_SINK_BUFFER_DISPOSE_WARN type={ex.GetType().Name} msg={ex.Message}");
            }
        }
    }

    private void CancelEncodingCts(string operation)
    {
        try
        {
            _cts?.Cancel();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_CANCEL_WARN op={operation} type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void DisposeCtsBestEffort(CancellationTokenSource? cts, string operation)
    {
        if (cts == null) return;

        try
        {
            cts.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_CTS_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void DisposeWorkAvailableBestEffort(string operation)
    {
        try
        {
            _workAvailable.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_WORK_SIGNAL_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg={ex.Message}");
        }
    }

    private void DisposeEncoderBestEffort(string operation)
    {
        try
        {
            _encoder.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_SINK_ENCODER_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg={ex.Message}");
        }
    }
}
