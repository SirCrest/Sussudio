using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

// Flashback recording support: backend ownership checks, audio attachment,
// frame-encoded fan-out, and capability validation.
public partial class CaptureService
{
    private bool IsFlashbackRecordingBackendActive()
        => _flashbackBackend.Sink != null &&
           _recordingBackend.IsFlashbackBackend(_flashbackBackend.Sink);

    private bool IsFlashbackRecordingBackendOwnedByRecording()
        => Volatile.Read(ref _flashbackRecordingStartInProgress) != 0 ||
           Volatile.Read(ref _flashbackRecordingFinalizeInProgress) != 0 ||
           (_isRecording && IsFlashbackRecordingBackendActive());

    private void AttachFlashbackAudioIfSupported(WasapiAudioCapture? capture, string reason)
    {
        var flashbackSink = _flashbackBackend.Sink;
        if (capture == null || flashbackSink == null)
            return;

        if (!flashbackSink.AudioEnabled)
        {
            Logger.Log($"FLASHBACK_AUDIO_ATTACH_SKIPPED reason='{reason}' sink_audio_enabled=false");
            return;
        }

        capture.AttachFlashbackSink(flashbackSink);
        Logger.Log($"FLASHBACK_AUDIO_ATTACH_OK reason='{reason}'");
    }

    private async Task EnsureFlashbackAudioInputsAsync(
        CaptureSettings settings,
        CancellationToken cancellationToken,
        string reason)
    {
        var audioDeviceId = settings.AudioEnabled
            ? (settings.UseCustomAudioInput ? settings.AudioDeviceId : (_audioDeviceId ?? _currentDevice?.AudioDeviceId))
            : null;

        if (settings.AudioEnabled && _previewAudioGraph.ProgramCapture == null)
        {
            if (!string.IsNullOrWhiteSpace(audioDeviceId))
            {
                WasapiAudioCapture? wasapiCapture = new();
                try
                {
                    await wasapiCapture.InitializeAsync(audioDeviceId, cancellationToken).ConfigureAwait(false);
                    wasapiCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
                    wasapiCapture.CaptureFailed += OnWasapiCaptureFailed;
                    wasapiCapture.Start();
                    _previewAudioGraph.ProgramCapture = wasapiCapture;
                    wasapiCapture = null;
                    ResetAvSyncDriftBaseline();
                    _previewAudioGraph.ResetCaptureFault();
                    Logger.Log($"FLASHBACK_AUDIO_CAPTURE_RESTORED reason='{reason}' device='{audioDeviceId}'");
                }
                finally
                {
                    if (wasapiCapture != null)
                    {
                        wasapiCapture.AudioLevelUpdated -= OnWasapiAudioLevelUpdated;
                        wasapiCapture.CaptureFailed -= OnWasapiCaptureFailed;
                        try { await wasapiCapture.DisposeAsync().ConfigureAwait(false); }
                        catch (Exception disposeEx) { Logger.Log($"FLASHBACK_AUDIO_CAPTURE_RESTORE_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
                    }
                }
            }
            else
            {
                Logger.Log($"FLASHBACK_AUDIO_CAPTURE_UNAVAILABLE reason='{reason}'");
            }
        }

        AttachFlashbackAudioIfSupported(_previewAudioGraph.ProgramCapture, reason);

        if (_micMonitorEnabled && _previewAudioGraph.MicrophoneCapture == null && !string.IsNullOrWhiteSpace(_micMonitorDeviceId))
        {
            WasapiAudioCapture? micCapture = new();
            try
            {
                await micCapture.InitializeAsync(_micMonitorDeviceId, cancellationToken).ConfigureAwait(false);
                micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
                micCapture.CaptureFailed += OnWasapiCaptureFailed;
                micCapture.Start();
                _previewAudioGraph.MicrophoneCapture = micCapture;
                micCapture = null;
                Logger.Log("MIC_MONITOR_START device='" + (_micMonitorDeviceName ?? "?") + "'");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception micEx)
            {
                Logger.Log("Mic monitor start failed (non-fatal): " + micEx.Message);
            }
            finally
            {
                if (micCapture != null)
                {
                    micCapture.AudioLevelUpdated -= OnMicrophoneAudioLevelUpdated;
                    micCapture.CaptureFailed -= OnWasapiCaptureFailed;
                    try { await micCapture.DisposeAsync().ConfigureAwait(false); }
                    catch (Exception disposeEx) { Logger.Log($"MIC_MONITOR_RESTORE_DISPOSE_WARN type={disposeEx.GetType().Name} msg={disposeEx.Message}"); }
                }
            }
        }

        if (_previewAudioGraph.MicrophoneCapture != null && _flashbackBackend.Sink is { MicrophoneEnabled: true } fbSink)
        {
            _previewAudioGraph.MicrophoneCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));
            Logger.Log($"FLASHBACK_MIC_ATTACH_OK reason='{reason}'");
        }
    }

    private void OnFlashbackFrameEncoded(object? sender, long frameCount)
    {
        if (!IsFlashbackRecordingBackendActive())
            return;

        FrameCaptured?.Invoke(this, unchecked((ulong)Math.Max(0L, frameCount)));
    }

    private void ValidateFlashbackRecordingCapabilities(
        FlashbackEncoderSink flashbackSink,
        bool requiresHdmiAudio,
        bool requiresMicrophone)
    {
        if (requiresHdmiAudio && !flashbackSink.AudioEnabled)
            throw new InvalidOperationException(
                "Flashback recording cannot include HDMI audio because the active flashback session was started without audio.");

        if (requiresMicrophone && !flashbackSink.MicrophoneEnabled)
            throw new InvalidOperationException(
                "Flashback recording cannot include microphone audio because the active flashback session was started without microphone support.");
    }

    private static void EnsureFlashbackRecordingTopologyMatches(
        FlashbackEncoderSink flashbackSink,
        bool audioEnabled,
        bool microphoneEnabled)
    {
        if (flashbackSink.AudioEnabled == audioEnabled &&
            flashbackSink.MicrophoneEnabled == microphoneEnabled)
            return;

        throw new InvalidOperationException(
            "Flashback recording settings changed after preview start. " +
            $"Restart preview so flashback can reopen with audio={audioEnabled} microphone={microphoneEnabled} " +
            $"(current audio={flashbackSink.AudioEnabled} microphone={flashbackSink.MicrophoneEnabled}).");
    }

    private static string? ResolveFlashbackExportVerificationFormat(
        CaptureSettings? settings,
        UnifiedVideoCapture? unifiedVideoCapture)
        => settings?.Format.ToString();

    /// <summary>
    /// Flashback recording honors the requested codec and preset directly. This legacy
    /// snapshot field remains for compatibility and should stay null unless a future
    /// explicit, user-visible substitution is introduced.
    /// </summary>
    private static string? ResolveFlashbackCodecDowngradeReason(
        CaptureSettings? settings,
        UnifiedVideoCapture? unifiedVideoCapture)
        => null;

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

    private static (int? Numerator, int? Denominator, double EffectiveFrameRate) ResolveFlashbackSessionFrameRateParts(
        CaptureSettings settings,
        double deliveryFrameRate)
    {
        // Preserve exact rationals only when they describe the actual delivered USB cadence.
        // A source-reported 120000/1001 rate paired with ~120 delivered frames/sec causes A/V
        // drift if we stamp Flashback video against the slower source clock.
        if (!double.IsFinite(deliveryFrameRate) || deliveryFrameRate <= 0)
        {
            return (null, null, deliveryFrameRate);
        }

        if (settings.RequestedFrameRateNumerator is not uint numerator ||
            settings.RequestedFrameRateDenominator is not uint denominator ||
            numerator == 0 ||
            denominator == 0 ||
            numerator > int.MaxValue ||
            denominator > int.MaxValue)
        {
            return InferFlashbackSessionFrameRateParts(deliveryFrameRate);
        }

        var rationalFps = numerator / (double)denominator;
        if (!double.IsFinite(rationalFps) || rationalFps <= 0)
        {
            return (null, null, deliveryFrameRate);
        }

        var deltaFps = Math.Abs(rationalFps - deliveryFrameRate);
        var toleranceFps = Math.Max(0.01, deliveryFrameRate * 0.0001);
        if (deltaFps > toleranceFps)
        {
            Logger.Log(
                $"FLASHBACK_FRAME_RATE_RATIONAL_REJECT requested={numerator}/{denominator} " +
                $"rational={rationalFps:0.######} delivery={deliveryFrameRate:0.######} " +
                $"delta={deltaFps:0.######} tolerance={toleranceFps:0.######}");
            return InferFlashbackSessionFrameRateParts(deliveryFrameRate);
        }

        Logger.Log(
            $"FLASHBACK_FRAME_RATE_RATIONAL_ACCEPT requested={numerator}/{denominator} " +
            $"delivery={deliveryFrameRate:0.######} effective={rationalFps:0.######}");
        return ((int)numerator, (int)denominator, rationalFps);
    }

    private static (int? Numerator, int? Denominator, double EffectiveFrameRate) InferFlashbackSessionFrameRateParts(double deliveryFrameRate)
    {
        foreach (var (numerator, denominator) in CommonFlashbackFrameRateParts)
        {
            var rationalFps = numerator / (double)denominator;
            var deltaFps = Math.Abs(rationalFps - deliveryFrameRate);
            var toleranceFps = Math.Max(0.01, deliveryFrameRate * 0.0001);
            if (deltaFps <= toleranceFps)
            {
                Logger.Log(
                    $"FLASHBACK_FRAME_RATE_RATIONAL_INFER inferred={numerator}/{denominator} " +
                    $"delivery={deliveryFrameRate:0.######} effective={rationalFps:0.######}");
                return (numerator, denominator, rationalFps);
            }
        }

        return (null, null, deliveryFrameRate);
    }

    private static readonly (int Numerator, int Denominator)[] CommonFlashbackFrameRateParts =
    {
        (24, 1),
        (24000, 1001),
        (25, 1),
        (30, 1),
        (30000, 1001),
        (50, 1),
        (60, 1),
        (60000, 1001),
        (100, 1),
        (120, 1),
        (120000, 1001),
        (144, 1),
        (240, 1)
    };
}
