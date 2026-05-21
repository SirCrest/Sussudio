using System;
using System.Threading;
using System.Threading.Channels;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private string? GetVideoEnqueueRejectReason(bool isGpu)
    {
        if (_disposed)
        {
            return "disposed";
        }

        if (!_started)
        {
            return "not_started";
        }

        if (_cts?.IsCancellationRequested == true)
        {
            return "cancelled";
        }

        if (Volatile.Read(ref _forceRotateDraining))
        {
            return "force_rotate_draining";
        }

        var failure = Volatile.Read(ref _encodingFailure);
        return failure != null
            ? $"encoding_failed:{failure.GetType().Name}"
            : null;
    }

    private string? GetVideoInputRejectReason(Channel<VideoFramePacket>? queue, int expectedSize, bool dataIsEmpty)
    {
        var lifecycleReason = GetVideoEnqueueRejectReason(isGpu: false);
        if (lifecycleReason != null)
        {
            return lifecycleReason;
        }

        if (queue == null)
        {
            return "queue_null";
        }

        if (expectedSize <= 0)
        {
            return "invalid_expected_size";
        }

        return dataIsEmpty ? "data_empty" : null;
    }

    private string? GetGpuInputRejectReason(Channel<GpuFramePacket>? queue, IntPtr texture)
    {
        var lifecycleReason = GetVideoEnqueueRejectReason(isGpu: true);
        if (lifecycleReason != null)
        {
            return lifecycleReason;
        }

        if (queue == null)
        {
            return "queue_null";
        }

        return texture == IntPtr.Zero ? "null_texture" : null;
    }
}
