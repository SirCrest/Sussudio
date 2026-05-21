using System.Threading;
using System.Threading.Channels;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private void InitializeStartupQueues(FlashbackSessionContext sessionContext)
    {
        // FullMode = Wait only affects WriteAsync (which we never call).
        // TryWrite returns false immediately when full regardless of FullMode,
        // allowing manual eviction paths to clean up resources before dropping.
        var videoQueueCapacity = ResolveVideoQueueCapacity(sessionContext, _encoder.UseHardwareFrames);
        Volatile.Write(ref _videoQueueCapacity, videoQueueCapacity);
        if (!_encoder.UseHardwareFrames && IsHighResolutionFrame(sessionContext))
        {
            Logger.Log($"FLASHBACK_SINK_WARN_CPU_ENCODING width={sessionContext.Width} height={sessionContext.Height} — GPU encoding unavailable, performance will be severely degraded");
        }

        if (_encoder.UseHardwareFrames)
        {
            _gpuQueue = Channel.CreateBounded<GpuFramePacket>(new BoundedChannelOptions(GpuQueueCapacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true
            });
            _gpuEncodingEnabled = true;
            Logger.Log($"FLASHBACK_SINK_GPU_QUEUE_INIT capacity={GpuQueueCapacity}");
        }

        _videoQueue = Channel.CreateBounded<VideoFramePacket>(new BoundedChannelOptions(videoQueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
        _audioQueue = Channel.CreateBounded<AudioSamplePacket>(new BoundedChannelOptions(AudioQueueCapacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
        if (sessionContext.MicrophoneEnabled)
        {
            _microphoneQueue = Channel.CreateBounded<AudioSamplePacket>(new BoundedChannelOptions(AudioQueueCapacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
        }
    }
}
