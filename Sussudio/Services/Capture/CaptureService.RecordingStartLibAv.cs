using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    private async Task StartLibAvRecordingAsync(
        CaptureSettings settings,
        CancellationToken transitionToken,
        RecordingStartRollbackState rollback)
    {
        var libAvSink = new LibAvRecordingSink();
        rollback.LibAvSink = libAvSink;
        libAvSink.OnEncodingFailed = OnRecordingBackendFatalError;
        libAvSink.FrameEncoded += (s, count) => FrameCaptured?.Invoke(this, unchecked((ulong)Math.Max(0L, count)));
        rollback.RecordingSink = libAvSink;

        var outputFolder = await OpenRecordingOutputFolderAsync(settings).ConfigureAwait(false);

        transitionToken.ThrowIfCancellationRequested();

        var effectiveWidth = _actualWidth ?? settings.Width;
        var effectiveHeight = _actualHeight ?? settings.Height;
        var effectiveFrameRate = _actualFrameRate ?? settings.FrameRate;
        await RefreshSourceTelemetryAsync(transitionToken).ConfigureAwait(false);
        TryCorrectFrameRateFromTelemetry();
        var hdrPipelineRequested = HdrOutputPolicy.IsEnabled(settings);
        if (hdrPipelineRequested && _latestSourceTelemetry.IsHdr == false)
        {
            Logger.Log("HDR requested while source telemetry reports SDR; continuing to request P010 (no silent fallback).");
        }

        var videoInputPixelFormat = hdrPipelineRequested ? "p010le" : "nv12";
        var audioDeviceName = settings.AudioEnabled
            ? (settings.UseCustomAudioInput ? settings.AudioDeviceName : (_audioDeviceName ?? _currentDevice?.AudioDeviceName))
            : null;
        var audioDeviceId = settings.AudioEnabled
            ? (settings.UseCustomAudioInput ? settings.AudioDeviceId : (_audioDeviceId ?? _currentDevice?.AudioDeviceId))
            : null;

        var requireP010 = string.Equals(videoInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase);
        var useMjpegHighFrameRateMode = settings.UseMjpegHighFrameRateMode;
        var unifiedVideoCapture = await PrepareLibAvRecordingVideoCaptureAsync(
                settings,
                rollback,
                effectiveWidth,
                effectiveHeight,
                effectiveFrameRate,
                requireP010,
                useMjpegHighFrameRateMode)
            .ConfigureAwait(false);

        var isMjpegMode = unifiedVideoCapture.IsSoftwareMjpegPipelineActive;
        var recordingFrameRate = unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : effectiveFrameRate;
        rollback.RecordingContext = await CreateLibAvRecordingContextAsync(
            settings,
            outputFolder,
            unifiedVideoCapture,
            audioDeviceName,
            recordingFrameRate,
            videoInputPixelFormat,
            isMjpegMode).ConfigureAwait(false);

        transitionToken.ThrowIfCancellationRequested();
        _mfConvertersDisabled = requireP010 || isMjpegMode;
        Logger.Log(
            "HDR_NEGOTIATION " +
            $"requested_hdr={hdrPipelineRequested} " +
            $"requested_subtype={(hdrPipelineRequested ? "P010" : "NV12")} " +
            $"requested_source_subtype={settings.RequestedPixelFormat ?? (hdrPipelineRequested ? "P010" : "NV12")} " +
            $"mjpeg_hfr={useMjpegHighFrameRateMode} " +
            $"negotiated_pixel_format={(unifiedVideoCapture.IsP010 ? "P010" : "NV12")} " +
            $"negotiated_subtype_token={(string.Equals(videoInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase) ? "P010|MFVideoFormat_P010" : "NV12")} " +
            $"hdr_static_metadata_requested={(!string.IsNullOrWhiteSpace(settings.HdrMasterDisplayMetadata) || (settings.HdrMaxCll > 0 && settings.HdrMaxFall > 0))} " +
            $"hdr_master_display_set={(!string.IsNullOrWhiteSpace(settings.HdrMasterDisplayMetadata))} " +
            $"hdr_max_cll={settings.HdrMaxCll} " +
            $"hdr_max_fall={settings.HdrMaxFall} " +
            $"mf_readwrite_disable_converters={(_mfConvertersDisabled ? "true" : "false")} " +
            $"libav_ingest_pix_fmt={(string.Equals(videoInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase) ? "AV_PIX_FMT_P010LE" : "AV_PIX_FMT_NV12")}");

        await rollback.RecordingSink.StartAsync(rollback.RecordingContext, transitionToken).ConfigureAwait(false);
        transitionToken.ThrowIfCancellationRequested();

        _lastMfSourceReaderFramesDelivered = 0;
        _lastMfSourceReaderFramesDropped = 0;
        _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
        _actualWidth = (uint)Math.Max(1, unifiedVideoCapture.Width);
        _actualHeight = (uint)Math.Max(1, unifiedVideoCapture.Height);
        _actualFrameRateNumerator = settings.RequestedFrameRateNumerator;
        _actualFrameRateDenominator = settings.RequestedFrameRateDenominator;
        _actualFrameRate = _actualFrameRateNumerator.HasValue && _actualFrameRateDenominator is > 0
            ? (double)_actualFrameRateNumerator.Value / _actualFrameRateDenominator.Value
            : unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : effectiveFrameRate;
        _actualFrameRateArg = ResolveFrameRateArg(settings, _actualFrameRate ?? effectiveFrameRate);
        _actualPixelFormat = unifiedVideoCapture.NativeInputFormat ?? (unifiedVideoCapture.IsP010 ? "P010" : "NV12");
        TryCorrectFrameRateFromTelemetry();

        var activeRecordingSink = rollback.RecordingSink
            ?? throw new InvalidOperationException("Recording requires an active sink.");
        var activeLibAvSink = rollback.LibAvSink
            ?? throw new InvalidOperationException("Recording requires an active LibAv sink.");
        await StartLibAvRecordingAudioInputsAsync(
            settings,
            transitionToken,
            rollback,
            activeLibAvSink,
            activeRecordingSink,
            audioDeviceId).ConfigureAwait(false);

        IGpuVideoFrameEncoder? gpuEncoder =
            (!isMjpegMode && activeLibAvSink.GpuEncodingEnabled)
                ? activeLibAvSink
                : null;

        _recordingIntegrityCounterBaseline = CaptureRecordingIntegrityCounters(activeLibAvSink);
        _recordingIntegrityAudioBaseline = CaptureRecordingAudioCounters(
            _previewAudioGraph.ProgramCapture,
            activeLibAvSink,
            settings);
        await unifiedVideoCapture.StartRecordingAsync(rollback.RecordingSink, activeLibAvSink, gpuEncoder).ConfigureAwait(false);
        if (gpuEncoder != null)
        {
            Logger.Log("GPU_RECORDING_ACTIVE gpu_encoder=active");
        }

        if (rollback.OwnedUnifiedVideoCapture != null)
        {
            rollback.OwnedUnifiedVideoCapture.Start();
        }

        _recordingBackend.InstallLibAv(
            rollback.LibAvSink,
            rollback.RecordingSink,
            rollback.RecordingContext,
            settings);
        ClearLastRecordingFailure();
        _isRecording = true;
        _activeVideoInputPixelFormat = videoInputPixelFormat;
        Interlocked.Exchange(ref _videoFramesDropped, 0);
        ResetObservedPixelTelemetry();
        RecordObservedPixelFormat(rollback.RecordingContext.HdrPipelineActive ? "P010" : "NV12", incrementAsFrame: false);
        PublishRecordingStartedOutcome(rollback.RecordingContext.FinalOutputPath);
        _lastUsePostMuxAudio = rollback.RecordingContext.UsePostMuxAudio;
        _recordingStopwatch.Restart();
        StatusChanged?.Invoke(this, "Recording");
        rollback.LibAvSink = null;
        rollback.RecordingSink = null;
        rollback.OwnedWasapiAudioCapture = null;
        rollback.OwnedUnifiedVideoCapture = null;
    }

    private async Task<UnifiedVideoCapture> PrepareLibAvRecordingVideoCaptureAsync(
        CaptureSettings settings,
        RecordingStartRollbackState rollback,
        uint effectiveWidth,
        uint effectiveHeight,
        double effectiveFrameRate,
        bool requireP010,
        bool useMjpegHighFrameRateMode)
    {
        var unifiedVideoCapture = _videoPipeline.Capture;
        if (unifiedVideoCapture == null)
        {
            rollback.OwnedUnifiedVideoCapture = new UnifiedVideoCapture();
            AttachUnifiedVideoCapture(rollback.OwnedUnifiedVideoCapture);
            await rollback.OwnedUnifiedVideoCapture.InitializeAsync(
                _currentDevice!.Id,
                (int)effectiveWidth,
                (int)effectiveHeight,
                effectiveFrameRate,
                requireP010,
                settings.RequestedPixelFormat,
                useMjpegHighFrameRateMode,
                settings.MjpegDecoderCount).ConfigureAwait(false);
            rollback.OwnedUnifiedVideoCapture.SetPreviewSink(_isVideoPreviewActive ? _videoPipeline.PreviewFrameSink : null);
            TryApplySharedPreviewDevice(rollback.OwnedUnifiedVideoCapture, _isVideoPreviewActive ? _videoPipeline.PreviewFrameSink : null);
            unifiedVideoCapture = rollback.OwnedUnifiedVideoCapture;
            _videoPipeline.InstallCapture(rollback.OwnedUnifiedVideoCapture);
        }
        else if (unifiedVideoCapture.IsP010 != requireP010)
        {
            throw new InvalidOperationException(
                $"Recording requires {(requireP010 ? "P010" : "NV12")}, but the active source-reader session negotiated {(unifiedVideoCapture.IsP010 ? "P010" : "NV12")}.");
        }
        else if (unifiedVideoCapture.IsHighFrameRateMjpegMode != useMjpegHighFrameRateMode)
        {
            throw new InvalidOperationException(
                $"Recording requested mjpeg_hfr={useMjpegHighFrameRateMode}, but the active preview session is mjpeg_hfr={unifiedVideoCapture.IsHighFrameRateMjpegMode}.");
        }

        rollback.RecordingVideoCapture = unifiedVideoCapture;
        TryApplySharedPreviewDevice(unifiedVideoCapture, _isVideoPreviewActive ? _videoPipeline.PreviewFrameSink : null);
        return unifiedVideoCapture;
    }

    private async Task StartLibAvRecordingAudioInputsAsync(
        CaptureSettings settings,
        CancellationToken transitionToken,
        RecordingStartRollbackState rollback,
        LibAvRecordingSink activeLibAvSink,
        IRecordingSink recordingSink,
        string? audioDeviceId)
    {
        if (_previewAudioGraph.ProgramCapture == null && settings.AudioEnabled)
        {
            var resolvedAudioDeviceId = audioDeviceId
                ?? throw new InvalidOperationException("Recording requires an audio capture device.");
            rollback.OwnedWasapiAudioCapture = new WasapiAudioCapture();
            await rollback.OwnedWasapiAudioCapture.InitializeAsync(resolvedAudioDeviceId, transitionToken).ConfigureAwait(false);
            rollback.OwnedWasapiAudioCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
            rollback.OwnedWasapiAudioCapture.CaptureFailed += OnWasapiCaptureFailed;
            rollback.OwnedWasapiAudioCapture.Start();
            _previewAudioGraph.ProgramCapture = rollback.OwnedWasapiAudioCapture;
        }

        if (_previewAudioGraph.ProgramCapture != null && settings.AudioEnabled)
        {
            _previewAudioGraph.ProgramCapture.AttachRecordingSink(recordingSink);
            rollback.SinkAttachedForAudioOnly = true;
            if (_isAudioPreviewActive)
            {
                await _previewAudioGraph.StartPlaybackAsync(
                    transitionToken,
                    _flashbackBackend.PlaybackController).ConfigureAwait(false);
            }
        }

        // Dispose preview-time mic monitor; recording creates its own capture wired to the active sink.
        await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

        if (settings.MicrophoneEnabled && !string.IsNullOrWhiteSpace(settings.MicrophoneDeviceId))
        {
            var micSink = activeLibAvSink; // capture stable reference - LibAv sink is nulled on success path
            var micCapture = new WasapiAudioCapture();
            await micCapture.InitializeAsync(settings.MicrophoneDeviceId, transitionToken).ConfigureAwait(false);
            micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
            micCapture.CaptureFailed += OnWasapiCaptureFailed;
            micCapture.SetAudioWriter(samples => micSink.WriteMicrophoneAudioAsync(samples));
            micCapture.Start();
            _previewAudioGraph.MicrophoneCapture = micCapture;
            Logger.Log("MICROPHONE_CAPTURE_START device='" + settings.MicrophoneDeviceName + "'");
        }
    }
}
