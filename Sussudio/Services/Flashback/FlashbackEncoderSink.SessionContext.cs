using System;
using Sussudio.Models;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private static FlashbackSessionContext CreateSessionContext(RecordingContext context)
    {
        var (frameRateNumerator, frameRateDenominator) = ResolveFrameRateParts(context.FrameRateArg);
        return new FlashbackSessionContext
        {
            Width = checked((int)context.EffectiveWidth),
            Height = checked((int)context.EffectiveHeight),
            FrameRate = context.EffectiveFrameRate,
            FrameRateNumerator = frameRateNumerator,
            FrameRateDenominator = frameRateDenominator,
            BitRate = context.Settings.GetTargetBitrate(),
            IsP010 = context.HdrPipelineActive,
            CodecName = MapCodecName(context.Settings.Format),
            NvencPreset = context.Settings.NvencPreset.ToString(),
            SplitEncodeMode = SplitEncodeModeParser.ToWireString(context.Settings.SplitEncodeMode),
            HdrEnabled = context.HdrPipelineActive,
            IsFullRangeInput = context.IsFullRangeInput,
            HdrMasterDisplayMetadata = context.Settings.HdrMasterDisplayMetadata,
            HdrMaxCll = context.Settings.HdrMaxCll,
            HdrMaxFall = context.Settings.HdrMaxFall,
            D3D11DevicePtr = context.D3D11DevicePtr,
            D3D11DeviceContextPtr = context.D3D11DeviceContextPtr,
            AudioEnabled = !string.IsNullOrWhiteSpace(context.AudioDeviceName),
            MicrophoneEnabled = !string.IsNullOrWhiteSpace(context.MicrophoneDeviceName)
        };
    }

    private static (int? Numerator, int? Denominator) ResolveFrameRateParts(string frameRateArg)
    {
        if (string.IsNullOrWhiteSpace(frameRateArg) || !frameRateArg.Contains('/', StringComparison.Ordinal))
        {
            return (null, null);
        }

        var parts = frameRateArg.Split('/', 2, StringSplitOptions.TrimEntries);
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

    private static string MapCodecName(RecordingFormat format)
        => MediaFormat.MapNvencCodecName(format);
}
