using System;
using Sussudio.Models;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

// Flashback recording session-context policy: codec selection, GPU handle handoff, and HDR rails.
public partial class CaptureService
{
    private FlashbackSessionContext CreateFlashbackSessionContext(
        UnifiedVideoCapture unifiedVideoCapture,
        CaptureSettings settings)
    {
        var isP010 = unifiedVideoCapture.IsP010;
        var frameRate = unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : settings.FrameRate;
        if (isP010 && settings.Format == RecordingFormat.H264Mp4)
        {
            throw new InvalidOperationException("HDR/P010 recording requires HEVC or AV1; H.264 cannot encode this pipeline.");
        }

        if (settings.Format == RecordingFormat.Av1Mp4 && !_hasAv1Nvenc)
        {
            throw new InvalidOperationException("AV1 recording requires the av1_nvenc encoder, but it is not available.");
        }

        var codecName = settings.Format switch
        {
            RecordingFormat.HevcMp4 => "hevc_nvenc",
            RecordingFormat.Av1Mp4 => "av1_nvenc",
            _ => "h264_nvenc"
        };
        var audioDeviceId = settings.AudioEnabled
            ? (settings.UseCustomAudioInput ? settings.AudioDeviceId : (_audioDeviceId ?? _currentDevice?.AudioDeviceId))
            : null;
        var d3dManager = unifiedVideoCapture.D3DManager;
        // When the software MJPEG decode pipeline is active, frames arrive as CPU NV12
        // buffers (not D3D11 textures). Do not initialize hw_frames for software
        // packets; nvenc would expect D3D11 textures and can crash in the driver.
        var useGpuEncoding = !unifiedVideoCapture.IsSoftwareMjpegPipelineActive;

        var frameRateParts = ResolveFlashbackSessionFrameRateParts(settings, frameRate);
        frameRate = frameRateParts.EffectiveFrameRate;
        var fpsNum = frameRateParts.Numerator;
        var fpsDen = frameRateParts.Denominator;

        var flashbackNvencPreset = settings.NvencPreset;

        // Hard rail: HDR must never silently degrade. If the user requested HDR
        // but UVC negotiation did not land on P010, fail the operation rather than
        // allowing SDR data to be encoded as if it were HDR (or vice versa).
        var hdrRequested = HdrOutputPolicy.IsEnabled(settings);
        if (hdrRequested != isP010)
        {
            Logger.Log(
                $"FLASHBACK_HDR_NEGOTIATION_FAIL requested={hdrRequested} negotiated_p010={isP010} resolved_codec={codecName}");
            throw new InvalidOperationException(
                $"Flashback HDR negotiation mismatch: HDR requested={hdrRequested} but UVC negotiated P010={isP010}. " +
                "Operation aborted to prevent silent HDR degradation.");
        }

        return new FlashbackSessionContext
        {
            Width = Math.Max(1, unifiedVideoCapture.Width),
            Height = Math.Max(1, unifiedVideoCapture.Height),
            FrameRate = frameRate,
            FrameRateNumerator = fpsNum,
            FrameRateDenominator = fpsDen,
            CodecName = codecName,
            NvencPreset = flashbackNvencPreset.ToString(),
            SplitEncodeMode = SplitEncodeModeParser.ToWireString(settings.SplitEncodeMode),
            IsP010 = isP010,
            BitRate = settings.GetTargetBitrate(),
            HdrEnabled = hdrRequested,
            IsFullRangeInput = unifiedVideoCapture.IsHighFrameRateMjpegMode,
            HdrMasterDisplayMetadata = settings.HdrMasterDisplayMetadata,
            HdrMaxCll = settings.HdrMaxCll,
            HdrMaxFall = settings.HdrMaxFall,
            D3D11DevicePtr = useGpuEncoding ? (d3dManager?.Device?.NativePointer ?? IntPtr.Zero) : IntPtr.Zero,
            D3D11DeviceContextPtr = useGpuEncoding ? (d3dManager?.ImmediateContext?.NativePointer ?? IntPtr.Zero) : IntPtr.Zero,
            AudioEnabled = settings.AudioEnabled && !string.IsNullOrWhiteSpace(audioDeviceId),
            MicrophoneEnabled = settings.MicrophoneEnabled && !string.IsNullOrWhiteSpace(settings.MicrophoneDeviceId)
        };
    }
}
