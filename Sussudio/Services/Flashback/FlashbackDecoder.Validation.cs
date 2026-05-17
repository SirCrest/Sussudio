using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackDecoder
{
    private static int CalculateFrameBufferSize(int width, int height, bool isHdr)
    {
        ValidateVideoDimensions(width, height);
        var pixels = (long)width * height;
        var bytes = isHdr ? pixels * 3 : pixels + pixels / 2;
        if (bytes <= 0 || bytes > MaxDecodedVideoFrameBytes || bytes > int.MaxValue)
        {
            throw CreateException($"Invalid decoded video frame size: {bytes} bytes for {width}x{height} hdr={isHdr}.");
        }

        return (int)bytes;
    }

    private static void ValidateVideoDimensions(int width, int height)
    {
        if (width <= 0 ||
            height <= 0 ||
            width > MaxDecodedVideoDimension ||
            height > MaxDecodedVideoDimension ||
            (width & 1) != 0 ||
            (height & 1) != 0)
        {
            throw CreateException($"Invalid video dimensions: {width}x{height}.");
        }
    }

    private static bool TryValidateSoftwareVideoFrame(
        AVFrame* frame,
        AVPixelFormat format,
        int width,
        int height,
        bool isHdr,
        out string failure)
    {
        failure = string.Empty;
        if (frame == null)
        {
            failure = "frame_null";
            return false;
        }

        if (frame->width > 0 && frame->width != width)
        {
            failure = $"width_mismatch frame={frame->width} expected={width}";
            return false;
        }

        if (frame->height > 0 && frame->height != height)
        {
            failure = $"height_mismatch frame={frame->height} expected={height}";
            return false;
        }

        var targetFormat = isHdr ? AVPixelFormat.AV_PIX_FMT_P010LE : AVPixelFormat.AV_PIX_FMT_NV12;
        if (format == targetFormat)
        {
            var lumaBytes = isHdr ? width * 2 : width;
            var chromaBytes = isHdr ? width * 2 : width;
            return TryValidatePlane(frame, 0, lumaBytes, out failure) &&
                   TryValidatePlane(frame, 1, chromaBytes, out failure);
        }

        if (!isHdr && format == AVPixelFormat.AV_PIX_FMT_YUV420P)
        {
            return TryValidatePlane(frame, 0, width, out failure) &&
                   TryValidatePlane(frame, 1, width / 2, out failure) &&
                   TryValidatePlane(frame, 2, width / 2, out failure);
        }

        if (isHdr && format == AVPixelFormat.AV_PIX_FMT_YUV420P10LE)
        {
            return TryValidatePlane(frame, 0, width * 2, out failure) &&
                   TryValidatePlane(frame, 1, width, out failure) &&
                   TryValidatePlane(frame, 2, width, out failure);
        }

        failure = $"unsupported_format:{format}";
        return false;
    }

    private static bool TryValidatePlane(AVFrame* frame, int planeIndex, int minLineSize, out string failure)
    {
        var plane = (uint)planeIndex;
        if (frame->data[plane] == null)
        {
            failure = $"plane_{planeIndex}_null";
            return false;
        }

        if (frame->linesize[plane] < minLineSize)
        {
            failure = $"plane_{planeIndex}_linesize:{frame->linesize[plane]}<{minLineSize}";
            return false;
        }

        failure = string.Empty;
        return true;
    }

    private static bool TryValidateD3D11VideoFrame(AVFrame* frame, int width, int height, out string failure)
    {
        failure = string.Empty;
        if (frame == null)
        {
            failure = "frame_null";
            return false;
        }

        if (frame->width > 0 && frame->width != width)
        {
            failure = $"width_mismatch frame={frame->width} expected={width}";
            return false;
        }

        if (frame->height > 0 && frame->height != height)
        {
            failure = $"height_mismatch frame={frame->height} expected={height}";
            return false;
        }

        if (frame->data[0] == null)
        {
            failure = "texture_null";
            return false;
        }

        var subresource = (long)frame->data[1];
        if (subresource < 0 || subresource > int.MaxValue)
        {
            failure = $"subresource_out_of_range:{subresource}";
            return false;
        }

        return true;
    }

    private static bool TryGetInputStreamCount(AVFormatContext* formatCtx, out int streamCount, out string failureMessage)
    {
        streamCount = 0;
        if (formatCtx == null)
        {
            failureMessage = "input context was not available.";
            return false;
        }

        var nativeStreamCount = formatCtx->nb_streams;
        if (nativeStreamCount == 0)
        {
            failureMessage = "input had no streams.";
            return false;
        }

        if (nativeStreamCount > MaxSupportedInputStreams)
        {
            failureMessage = $"input stream count {nativeStreamCount} exceeds supported maximum {MaxSupportedInputStreams}.";
            return false;
        }

        streamCount = (int)nativeStreamCount;
        failureMessage = string.Empty;
        return true;
    }

    private static bool IsValidStreamIndex(int streamIndex, int streamCount)
        => streamIndex >= 0 && streamIndex < streamCount;
}
