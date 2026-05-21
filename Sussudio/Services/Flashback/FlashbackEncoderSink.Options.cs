using System;
using Sussudio.Models;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    /// <summary>
    /// MPEG-TS supports H.264 and HEVC natively. AV1 in MPEG-TS requires newer ffmpeg builds
    /// (libavformat 61.7+) and is not widely supported. Use fMP4 for AV1.
    /// </summary>
    private static bool SupportsTransportStream(string codecName) =>
        codecName.Contains("264", StringComparison.OrdinalIgnoreCase) ||
        codecName.Contains("hevc", StringComparison.OrdinalIgnoreCase) ||
        codecName.Contains("265", StringComparison.OrdinalIgnoreCase);

    internal static string GetSegmentExtension(string codecName) =>
        SupportsTransportStream(codecName) ? ".ts" : ".mp4";

    private static LibAvEncoderOptions CreateOptions(FlashbackSessionContext context, string outputPath)
    {
        var (frameRateNumerator, frameRateDenominator) = ResolveSessionFrameRateParts(
            context.FrameRateNumerator,
            context.FrameRateDenominator);

        return new LibAvEncoderOptions
        {
            OutputPath = outputPath,
            ContainerFormat = SupportsTransportStream(context.CodecName) ? "mpegts" : "mp4",
            FragmentedMp4 = !SupportsTransportStream(context.CodecName),
            CodecName = context.CodecName,
            Width = context.Width,
            Height = context.Height,
            FrameRate = context.FrameRate,
            FrameRateNumerator = frameRateNumerator,
            FrameRateDenominator = frameRateDenominator,
            BitRate = context.BitRate,
            IsP010 = context.IsP010,
            NvencPreset = context.NvencPreset,
            SplitEncodeMode = context.SplitEncodeMode,
            // 1-second GOP for fast interactive seeking. The default (2x frame rate)
            // means up to 2 seconds of decode-forward on every pause/scrub.
            // 1x frame rate halves worst-case seek latency with minimal bitrate impact.
            GopSize = (int)Math.Max(1, Math.Round(context.FrameRate)),
            HdrEnabled = context.HdrEnabled,
            IsFullRangeInput = context.IsFullRangeInput,
            HdrMasterDisplayMetadata = context.HdrMasterDisplayMetadata,
            HdrMaxCll = context.HdrMaxCll,
            HdrMaxFall = context.HdrMaxFall,
            D3D11DevicePtr = context.D3D11DevicePtr,
            D3D11DeviceContextPtr = context.D3D11DeviceContextPtr,
            AudioEnabled = context.AudioEnabled,
            AudioSampleRate = 48_000,
            AudioChannels = 2,
            AudioBitRate = 320_000,
            MicrophoneEnabled = context.MicrophoneEnabled,
            MicrophoneSampleRate = 48_000,
            MicrophoneChannels = 2,
            MicrophoneBitRate = 320_000
        };
    }

    private static (int? Numerator, int? Denominator) ResolveSessionFrameRateParts(int? numerator, int? denominator)
    {
        if (!numerator.HasValue || !denominator.HasValue || numerator <= 0 || denominator <= 0)
        {
            return (null, null);
        }

        var fps = (double)numerator.Value / denominator.Value;
        if (!double.IsFinite(fps) || fps <= 0 || fps > MaxSessionFrameRate)
        {
            return (null, null);
        }

        return (numerator, denominator);
    }
}
