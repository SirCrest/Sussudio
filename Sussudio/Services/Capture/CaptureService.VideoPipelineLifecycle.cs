using System;
using System.Threading.Tasks;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private IPreviewFrameSink? _previewFrameSink;
    private UnifiedVideoCapture.MjpegPipelineTimingMetrics _lastMjpegPipelineTimingMetrics;
    private ParallelMjpegDecodePipeline.PipelineTimingMetrics? _lastFullMjpegPipelineTimingMetrics;

    public int GetNegotiatedVideoWidth() => _unifiedVideoCapture?.Width ?? 0;
    public int GetNegotiatedVideoHeight() => _unifiedVideoCapture?.Height ?? 0;
    public double GetNegotiatedVideoFps() => _unifiedVideoCapture?.Fps ?? 0;

    internal void SetPreviewFrameSink(IPreviewFrameSink? sink)
    {
        var controller = _flashbackPlaybackController;
        if (sink == null && controller is { IsDisposed: false, IsInitialized: true })
        {
            controller.PrepareForPreviewDetach();
        }

        _previewFrameSink = sink;
        _unifiedVideoCapture?.SetPreviewSink(sink);
        TryApplySharedPreviewDevice(_unifiedVideoCapture, sink);
        // Late-initialize playback controller if it was created before the renderer
        if (controller is { IsDisposed: false, IsInitialized: false } && sink != null && _unifiedVideoCapture != null)
        {
            controller.Initialize(sink, _unifiedVideoCapture, _wasapiAudioPlayback, _wasapiAudioCapture);
            Logger.Log("FLASHBACK_PLAYBACK_LATE_INIT via SetPreviewFrameSink");
        }
        else if (controller is { IsDisposed: false, IsInitialized: true })
        {
            controller.UpdatePreviewComponents(sink, _unifiedVideoCapture);
        }
    }

    private void CacheMjpegTimingMetrics(UnifiedVideoCapture? unifiedVideoCapture)
    {
        if (unifiedVideoCapture == null)
        {
            return;
        }

        var timingSnapshot = unifiedVideoCapture.GetMjpegPipelineTimingSnapshot();
        _lastMjpegPipelineTimingMetrics = timingSnapshot.Summary;
        _lastFullMjpegPipelineTimingMetrics = timingSnapshot.Details;
    }

    private void ResetCachedMjpegTimingMetrics()
    {
        _lastMjpegPipelineTimingMetrics = default;
        _lastFullMjpegPipelineTimingMetrics = null;
    }

    internal ParallelMjpegDecodePipeline.PipelineTimingMetrics? GetMjpegPipelineTimingDetails()
    {
        return _unifiedVideoCapture?.GetFullMjpegPipelineTimingMetrics() ?? _lastFullMjpegPipelineTimingMetrics;
    }

    private void AttachUnifiedVideoCapture(UnifiedVideoCapture unifiedVideoCapture)
    {
        unifiedVideoCapture.FatalErrorOccurred += OnUnifiedVideoCaptureFatalError;
        unifiedVideoCapture.SetPixelFormatDetectedCallback(fmt => RecordObservedPixelFormat(fmt));
    }

    private void DetachUnifiedVideoCapture(UnifiedVideoCapture? unifiedVideoCapture)
    {
        if (unifiedVideoCapture == null)
        {
            return;
        }

        unifiedVideoCapture.FatalErrorOccurred -= OnUnifiedVideoCaptureFatalError;
        unifiedVideoCapture.SetPixelFormatDetectedCallback(null);
    }

    private Task ScheduleDeferredUnifiedVideoCaptureCleanup(
        Task sinkCompletionTask,
        UnifiedVideoCapture unifiedVideoCapture,
        string reason)
    {
        try
        {
            unifiedVideoCapture.SetPreviewSink(null);
        }
        catch (Exception ex)
        {
            Logger.Log($"UNIFIED_VIDEO_DEFERRED_PREVIEW_DETACH_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
        }

        return Task.Run(async () =>
        {
            Exception? cleanupFailure = null;
            try
            {
                await sinkCompletionTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"UNIFIED_VIDEO_DEFERRED_WAIT_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                try
                {
                    await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    cleanupFailure ??= ex;
                    Logger.Log($"UNIFIED_VIDEO_DEFERRED_STOP_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
                }

                try
                {
                    await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    cleanupFailure ??= ex;
                    Logger.Log($"UNIFIED_VIDEO_DEFERRED_DISPOSE_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
                }

                Logger.Log($"UNIFIED_VIDEO_DEFERRED_CLEANUP_END reason='{reason}'");

                if (cleanupFailure != null)
                {
                    throw new InvalidOperationException(
                        $"Deferred unified video cleanup failed for reason '{reason}'.",
                        cleanupFailure);
                }
            }
        });
    }

    private void ClearPendingLibAvDrainTaskIfCompletedSuccessfully()
    {
        if (_pendingLibAvDrainTask?.IsCompletedSuccessfully == true)
        {
            _pendingLibAvDrainTask = null;
        }
    }

    private void ThrowIfPendingLibAvDrainTaskBlocksReentry()
    {
        var pendingLibAvDrainTask = _pendingLibAvDrainTask;
        if (pendingLibAvDrainTask == null)
        {
            return;
        }

        if (pendingLibAvDrainTask.IsCompletedSuccessfully)
        {
            _pendingLibAvDrainTask = null;
            return;
        }

        if (pendingLibAvDrainTask.IsFaulted)
        {
            throw new InvalidOperationException(
                "Previous recording backend failed to finalize cleanly. Check the logs and retry.",
                pendingLibAvDrainTask.Exception?.GetBaseException());
        }

        if (pendingLibAvDrainTask.IsCanceled)
        {
            throw new InvalidOperationException("Previous recording backend cleanup was canceled. Check the logs and retry.");
        }

        throw new InvalidOperationException("Previous recording backend is still finalizing. Please wait a moment and try again.");
    }

    private void TryApplySharedPreviewDevice(UnifiedVideoCapture? capture, IPreviewFrameSink? sink)
    {
        if (capture == null || sink is not D3D11PreviewRenderer renderer)
        {
            return;
        }

        renderer.FullRangeInput = capture.IsHighFrameRateMjpegMode;
        var d3dManager = capture.D3DManager;
        if (d3dManager == null)
        {
            return;
        }

        if (!d3dManager.TryCreateDeviceReference(out var sharedDevice, out var reason) || sharedDevice == null)
        {
            Logger.Log($"UNIFIED_VIDEO_SHARED_DEVICE_APPLY_SKIP reason={reason}");
            return;
        }

        try
        {
            renderer.SetSharedDevice(sharedDevice);
        }
        catch (Exception ex)
        {
            Logger.Log($"UNIFIED_VIDEO_SHARED_DEVICE_APPLY_WARN type={ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
        }
        finally
        {
            sharedDevice.Dispose();
        }
    }
}
