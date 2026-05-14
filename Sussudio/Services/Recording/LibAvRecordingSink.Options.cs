using System;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Recording;

public sealed partial class LibAvRecordingSink
{
    private static string MapCodecName(RecordingFormat format)
        => MediaFormat.MapNvencCodecName(format);

    private static (int? Numerator, int? Denominator) ResolveFrameRateParts(RecordingContext context)
    {
        if (string.IsNullOrWhiteSpace(context.FrameRateArg) || !context.FrameRateArg.Contains('/', StringComparison.Ordinal))
        {
            return (null, null);
        }

        var parts = context.FrameRateArg.Split('/', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var numerator) ||
            !int.TryParse(parts[1], out var denominator) ||
            numerator <= 0 ||
            denominator <= 0)
        {
            return (null, null);
        }

        return (numerator, denominator);
    }

    private LibAvEncoderOptions CreateOptions(RecordingContext context)
    {
        var (frameRateNumerator, frameRateDenominator) = ResolveFrameRateParts(context);
        return new LibAvEncoderOptions
        {
            OutputPath = context.VideoOutputPath,
            CodecName = MapCodecName(context.Settings.Format),
            Width = checked((int)context.EffectiveWidth),
            Height = checked((int)context.EffectiveHeight),
            FrameRate = context.EffectiveFrameRate,
            FrameRateNumerator = frameRateNumerator,
            FrameRateDenominator = frameRateDenominator,
            BitRate = context.Settings.GetTargetBitrate(),
            IsP010 = context.HdrPipelineActive,
            NvencPreset = context.Settings.NvencPreset.ToString(),
            SplitEncodeMode = SplitEncodeModeParser.ToWireString(context.Settings.SplitEncodeMode),
            HdrEnabled = context.HdrPipelineActive,
            IsFullRangeInput = context.IsFullRangeInput,
            HdrMasterDisplayMetadata = context.Settings.HdrMasterDisplayMetadata,
            HdrMaxCll = context.Settings.HdrMaxCll,
            HdrMaxFall = context.Settings.HdrMaxFall,
            D3D11DevicePtr = context.D3D11DevicePtr,
            D3D11DeviceContextPtr = context.D3D11DeviceContextPtr,
            CudaHwDeviceCtxPtr = context.CudaHwDeviceCtxPtr,
            CudaHwFramesCtxPtr = context.CudaHwFramesCtxPtr,
            AudioEnabled = !string.IsNullOrWhiteSpace(context.AudioDeviceName),
            AudioSampleRate = 48_000,
            AudioChannels = 2,
            AudioBitRate = 320_000,
            MicrophoneEnabled = _microphoneEnabled,
            MicrophoneSampleRate = 48_000,
            MicrophoneChannels = 2,
            MicrophoneBitRate = 320_000
        };
    }
}
