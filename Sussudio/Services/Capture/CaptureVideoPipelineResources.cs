using System;
using System.Threading.Tasks;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;

namespace Sussudio.Services.Capture;

internal sealed class CaptureVideoPipelineResources
{
    public UnifiedVideoCapture? Capture { get; set; }
    public IPreviewFrameSink? PreviewFrameSink { get; set; }
    public UnifiedVideoCapture.MjpegPipelineTimingMetrics LastMjpegPipelineTimingMetrics { get; private set; }
    public ParallelMjpegDecodePipeline.PipelineTimingMetrics? LastFullMjpegPipelineTimingMetrics { get; private set; }

    public int NegotiatedVideoWidth => Capture?.Width ?? 0;
    public int NegotiatedVideoHeight => Capture?.Height ?? 0;
    public double NegotiatedVideoFps => Capture?.Fps ?? 0;

    public void InstallCapture(UnifiedVideoCapture capture)
    {
        Capture = capture;
    }

    public UnifiedVideoCapture? TakeCapture()
    {
        var capture = Capture;
        Capture = null;
        return capture;
    }

    public void ClearCapture()
    {
        Capture = null;
    }

    public void SetPreviewFrameSink(IPreviewFrameSink? sink)
    {
        PreviewFrameSink = sink;
        Capture?.SetPreviewSink(sink);
    }

    public void CacheMjpegTimingMetrics(UnifiedVideoCapture? capture)
    {
        if (capture == null)
        {
            return;
        }

        var timingSnapshot = capture.GetMjpegPipelineTimingSnapshot();
        LastMjpegPipelineTimingMetrics = timingSnapshot.Summary;
        LastFullMjpegPipelineTimingMetrics = timingSnapshot.Details;
    }

    public void ResetCachedMjpegTimingMetrics()
    {
        LastMjpegPipelineTimingMetrics = default;
        LastFullMjpegPipelineTimingMetrics = null;
    }

    public ParallelMjpegDecodePipeline.PipelineTimingMetrics? GetMjpegPipelineTimingDetails()
    {
        return Capture?.GetFullMjpegPipelineTimingMetrics() ?? LastFullMjpegPipelineTimingMetrics;
    }

    public CaptureMjpegTimingSnapshot GetMjpegTimingSnapshot(UnifiedVideoCapture? capture)
    {
        var timingSnapshot = capture?.GetMjpegPipelineTimingSnapshot();
        return new CaptureMjpegTimingSnapshot(
            timingSnapshot?.Summary ?? LastMjpegPipelineTimingMetrics,
            timingSnapshot?.Details ?? LastFullMjpegPipelineTimingMetrics);
    }

    public Task ScheduleDeferredUnifiedVideoCaptureCleanup(
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

    internal readonly record struct CaptureMjpegTimingSnapshot(
        UnifiedVideoCapture.MjpegPipelineTimingMetrics Summary,
        ParallelMjpegDecodePipeline.PipelineTimingMetrics? Details);
}
