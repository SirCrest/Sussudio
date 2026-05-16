using System.Threading;
using System.Threading.Channels;
using Sussudio.Models;

namespace Sussudio.Services.Recording;

public sealed partial class LibAvRecordingSink
{
    private void InitializeVideoSessionQueues()
    {
        if (_encoder.UseCudaHardwareFrames)
        {
            _cudaQueue = Channel.CreateBounded<CudaFramePacket>(new BoundedChannelOptions(CudaQueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true
            });
            _cudaEncodingEnabled = true;
            Logger.Log("LIBAV_SINK_CUDA_QUEUE_INIT capacity=" + CudaQueueCapacity);
        }
        else if (_encoder.UseHardwareFrames)
        {
            _gpuQueue = Channel.CreateBounded<GpuFramePacket>(new BoundedChannelOptions(GpuQueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true
            });
            _gpuEncodingEnabled = true;
            Logger.Log("LIBAV_SINK_GPU_QUEUE_INIT capacity=" + GpuQueueCapacity);
        }

        _videoQueue = Channel.CreateBounded<VideoFramePacket>(new BoundedChannelOptions(VideoQueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    private void ResetVideoSessionState(RecordingContext context)
    {
        _width = checked((int)context.EffectiveWidth);
        _height = checked((int)context.EffectiveHeight);
        ResetVideoSessionMetrics();
    }

    private void ResetVideoSessionMetrics()
    {
        Interlocked.Exchange(ref _droppedVideoFrames, 0);
        Interlocked.Exchange(ref _encodedVideoFrames, 0);
        Interlocked.Exchange(ref _videoFramesEnqueued, 0);
        Interlocked.Exchange(ref _videoFramesSubmittedToEncoder, 0);
        Interlocked.Exchange(ref _videoDropsQueueSaturated, 0);
        Interlocked.Exchange(ref _videoDropsBacklogEviction, 0);
        Interlocked.Exchange(ref _gpuFramesEnqueued, 0);
        Interlocked.Exchange(ref _gpuFramesDropped, 0);
        Interlocked.Exchange(ref _cudaFramesEnqueued, 0);
        Interlocked.Exchange(ref _cudaFramesDropped, 0);
        Interlocked.Exchange(ref _videoQueueMaxDepth, 0);
        Interlocked.Exchange(ref _gpuQueueMaxDepth, 0);
        Interlocked.Exchange(ref _cudaQueueMaxDepth, 0);
        Interlocked.Exchange(ref _videoQueueDepth, 0);
        Interlocked.Exchange(ref _gpuQueueDepth, 0);
        Interlocked.Exchange(ref _cudaQueueDepth, 0);
        Interlocked.Exchange(ref _lastVideoEnqueueTick, 0);
        Interlocked.Exchange(ref _lastVideoWriteTick, 0);
        ResetVideoDiagnostics();
    }

    private void ResetVideoDiagnostics() => _videoLatencyTracker.ResetAll();
}
