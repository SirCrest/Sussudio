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
