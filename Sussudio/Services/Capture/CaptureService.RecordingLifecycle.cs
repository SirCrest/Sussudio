using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Contracts;
using Sussudio.Services.Flashback;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

public partial class CaptureService
{
    // Recording finalization state is intentionally retained after stop so the
    // UI, automation, and verifier can explain what happened to the last file
    // even after capture resources have been torn down.
    private string? _lastOutputPath;
    private string _lastFinalizeStatus = "None";
    private DateTimeOffset? _lastFinalizeUtc;
    private IReadOnlyList<string> _lastPreservedArtifacts = Array.Empty<string>();

    public Task StartRecordingAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Recording, async transitionToken =>
        {
            EnsureInitialized();
            if (_isRecording)
            {
                return;
            }

            if (_currentDevice == null)
            {
                throw new InvalidOperationException("No selected video device is available for recording.");
            }

            transitionToken.ThrowIfCancellationRequested();
            _currentSettings = settings;
            _micMonitorEnabled = settings.MicrophoneEnabled;
            _micMonitorDeviceId = settings.MicrophoneDeviceId;
            _micMonitorDeviceName = settings.MicrophoneDeviceName;

            var rollback = new RecordingStartRollbackState();
            _previewAudioGraph.ResetCaptureFault();
            _recordingBackend.ThrowIfPendingLibAvDrainBlocksReentry();
            try
            {
                await DisposeUnusableFlashbackRecordingBackendAsync(transitionToken).ConfigureAwait(false);

                if (_flashbackEnabled && _flashbackBackend.Sink != null)
                {
                    await StartFlashbackRecordingAsync(settings, transitionToken, rollback).ConfigureAwait(false);
                    return;
                }

                await StartLibAvRecordingAsync(settings, transitionToken, rollback).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await RollbackRecordingStartAsync(rollback, ex).ConfigureAwait(false);
                throw;
            }
        }, cancellationToken);

    // Public path used by normal recording-stop (UI Stop button, automation StopRecording).
    public Task StopRecordingAsync(CancellationToken cancellationToken = default)
        => StopRecordingAsync(emergency: false, cancellationToken);

    // Internal overload used by CaptureSessionCoordinator.StopRecordingForEmergencyAsync.
    // Threads `emergency` through StopAndDisposeRecordingBackendAsync to LibAvRecordingSink
    // so the sink applies EmergencyStopTimeoutMs (5s) instead of StopTimeoutMs (30s) - fits
    // inside App.TryEmergencyStopRecording's 8s wrapper (fix #12).
    internal Task StopRecordingAsync(bool emergency, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Ready, async transitionToken =>
        {
            if (!_isRecording && _recordingBackend.Sink == null && _recordingBackend.LibAvSink == null)
            {
                return;
            }

            var result = await StopAndDisposeRecordingBackendAsync("Stopped", emergency, transitionToken).ConfigureAwait(false);
            // Preview continues running on the active source-reader/WASAPI sessions - no resume needed.
            StatusChanged?.Invoke(this, result.StatusMessage);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(result.StatusMessage);
            }
        }, cancellationToken);

    private static async Task<StorageFolder> OpenRecordingOutputFolderAsync(CaptureSettings settings)
    {
        try
        {
            return await StorageFolder.GetFolderFromPathAsync(settings.OutputPath);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Output folder is unavailable: {settings.OutputPath}", ex);
        }
    }

    private async Task<RecordingContext> CreateLibAvRecordingContextAsync(
        CaptureSettings settings,
        StorageFolder outputFolder,
        UnifiedVideoCapture unifiedVideoCapture,
        string? audioDeviceName,
        double recordingFrameRate,
        string videoInputPixelFormat,
        bool isMjpegMode)
    {
        var d3dManager = unifiedVideoCapture.D3DManager;
        var recordingWidth = (uint)Math.Max(1, unifiedVideoCapture.Width);
        var recordingHeight = (uint)Math.Max(1, unifiedVideoCapture.Height);
        return await _artifactManager.CreateContextAsync(
            outputFolder,
            new RecordingContextRequest
            {
                Settings = settings,
                UsePostMuxAudio = false,
                AudioDeviceName = audioDeviceName,
                MicrophoneDeviceName = settings.MicrophoneEnabled ? settings.MicrophoneDeviceName : null,
                EffectiveFrameRate = recordingFrameRate,
                FrameRateArg = ResolveFrameRateArg(settings, recordingFrameRate),
                EffectiveWidth = recordingWidth,
                EffectiveHeight = recordingHeight,
                VideoInputPixelFormat = videoInputPixelFormat,
                IsFullRangeInput = isMjpegMode,
                GpuHandles = new GpuPipelineHandles(
                    isMjpegMode ? IntPtr.Zero : (d3dManager?.Device.NativePointer ?? IntPtr.Zero),
                    isMjpegMode ? IntPtr.Zero : (d3dManager?.ImmediateContext.NativePointer ?? IntPtr.Zero),
                    IntPtr.Zero,
                    IntPtr.Zero)
            }).ConfigureAwait(false);
    }

    private async Task<RecordingContext> CreateFlashbackRecordingContextAsync(
        CaptureSettings settings,
        StorageFolder outputFolder,
        double effectiveFrameRate)
        => await _artifactManager.CreateContextAsync(
            outputFolder,
            new RecordingContextRequest
            {
                Settings = settings,
                UsePostMuxAudio = false,
                AudioDeviceName = settings.AudioEnabled
                    ? (settings.UseCustomAudioInput ? settings.AudioDeviceName : (_audioDeviceName ?? _currentDevice?.AudioDeviceName))
                    : null,
                MicrophoneDeviceName = settings.MicrophoneEnabled ? settings.MicrophoneDeviceName : null,
                EffectiveFrameRate = effectiveFrameRate,
                FrameRateArg = ResolveFrameRateArg(settings, effectiveFrameRate),
                EffectiveWidth = _actualWidth ?? settings.Width,
                EffectiveHeight = _actualHeight ?? settings.Height,
                VideoInputPixelFormat = _videoPipeline.Capture?.IsP010 == true ? "p010le" : "nv12",
                IsFullRangeInput = _videoPipeline.Capture?.IsSoftwareMjpegPipelineActive == true,
                GpuHandles = GpuPipelineHandles.None
            }).ConfigureAwait(false);

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

    private void PublishRecordingStartedOutcome(string finalOutputPath)
    {
        _lastOutputPath = finalOutputPath;
        _lastFinalizeStatus = "Recording";
        _lastFinalizeUtc = null;
        _lastPreservedArtifacts = Array.Empty<string>();
    }

    private void PublishRecordingFinalizedOutcome(FinalizeResult result, bool updateOutputPath)
    {
        if (updateOutputPath)
        {
            _lastOutputPath = result.OutputPath;
        }

        _lastFinalizeStatus = result.StatusMessage;
        _lastFinalizeUtc = DateTimeOffset.UtcNow;
        _lastPreservedArtifacts = result.PreservedArtifacts;
    }

    // Recording finalization router: choose the active recording backend and delegate
    // backend-specific stop/dispose work to the focused finalization partials.
    private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync(string fallbackStatusMessage, bool emergency, CancellationToken cancellationToken)
    {
        if (IsFlashbackRecordingBackendActive())
        {
            return await StopAndDisposeFlashbackRecordingBackendAsync(cancellationToken).ConfigureAwait(false);
        }

        return await StopAndDisposeLibAvRecordingBackendAsync(fallbackStatusMessage, emergency, cancellationToken).ConfigureAwait(false);
    }

    private async Task RollbackRecordingStartAsync(RecordingStartRollbackState rollback, Exception ex)
    {
        Logger.Log($"CAPTURE_RECORDING_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
        RecordLastRecordingFailure(ex);

        if (rollback.FlashbackRecordingStartedSink != null)
        {
            try
            {
                rollback.FlashbackRecordingStartedSink.CancelRecordingStartRollback("start_recording_failed");
            }
            catch (Exception rollbackEx)
            {
                Logger.Log($"FLASHBACK_RECORDING_START_ROLLBACK_WARN type={rollbackEx.GetType().Name} error='{rollbackEx.Message}'");
            }

            _videoPipeline.Capture?.EndFlashbackRecordingAccounting();
            if (_recordingBackend.IsFlashbackBackend(rollback.FlashbackRecordingStartedSink))
            {
                _recordingBackend.ClearActiveBackend();
            }
        }

        Volatile.Write(ref _flashbackRecordingStartInProgress, 0);
        if (rollback.FlashbackRecordingBackendLeaseHeld)
        {
            rollback.FlashbackRecordingBackendLeaseHeld = false;
            ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_recording_start_fail");
        }

        if (rollback.SinkAttachedForAudioOnly && _previewAudioGraph.ProgramCapture != null)
        {
            _previewAudioGraph.ProgramCapture.DetachRecordingSink();
        }

        await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

        if (rollback.OwnedUnifiedVideoCapture != null)
        {
            DetachUnifiedVideoCapture(rollback.OwnedUnifiedVideoCapture);
        }

        try
        {
            await _artifactManager.RollbackAsync(rollback.RecordingContext).ConfigureAwait(false);
        }
        catch (Exception rollbackEx)
        {
            Logger.Log($"Recording start rollback cleanup failed: {rollbackEx.Message}");
        }

        try
        {
            await DisposeTransientRecordingBackendAsync(
                rollback.RecordingSink,
                rollback.OwnedWasapiAudioCapture,
                rollback.OwnedUnifiedVideoCapture).ConfigureAwait(false);
        }
        catch (Exception disposeEx)
        {
            Logger.Log($"Transient recording backend cleanup failed during start rollback: {disposeEx.Message}");
        }

        if (rollback.OwnedWasapiAudioCapture != null && ReferenceEquals(_previewAudioGraph.ProgramCapture, rollback.OwnedWasapiAudioCapture))
        {
            _previewAudioGraph.DetachCapture(
                rollback.OwnedWasapiAudioCapture,
                OnWasapiAudioLevelUpdated,
                OnWasapiCaptureFailed,
                _flashbackBackend.PlaybackController);
            _previewAudioGraph.ProgramCapture = null;
        }

        if (rollback.OwnedUnifiedVideoCapture != null && ReferenceEquals(_videoPipeline.Capture, rollback.OwnedUnifiedVideoCapture))
        {
            CacheMjpegTimingMetrics(rollback.OwnedUnifiedVideoCapture);
            _lastMfSourceReaderFramesDelivered = rollback.OwnedUnifiedVideoCapture.VideoFramesArrived;
            _lastMfSourceReaderFramesDropped = rollback.OwnedUnifiedVideoCapture.VideoFramesDropped;
            _lastMfSourceReaderNegotiatedFormat = rollback.OwnedUnifiedVideoCapture.NegotiatedFormat;
            _videoPipeline.ClearCapture();
        }

        _recordingBackend.ClearContextAndSettings();
        _recordingIntegrityCounterBaseline = null;
        _recordingIntegrityAudioBaseline = null;
        _isRecording = false;
        _recordingStopwatch.Reset();
        _mfConvertersDisabled = false;
    }

    private async Task DisposeTransientRecordingBackendAsync(
        IRecordingSink? sink,
        WasapiAudioCapture? wasapiCapture,
        UnifiedVideoCapture? unifiedVideoCapture)
    {
        if (unifiedVideoCapture != null)
        {
            try
            {
                await unifiedVideoCapture.StopRecordingAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient unified video recording stop failed during rollback: {ex.Message}");
            }
        }

        if (sink != null)
        {
            try
            {
                await sink.StopAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient recording sink stop failed during rollback: {ex.Message}");
            }

            try
            {
                await sink.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient recording sink dispose failed during rollback: {ex.Message}");
            }
        }

        if (unifiedVideoCapture != null)
        {
            if (sink is LibAvRecordingSink libAvSink)
            {
                var libAvDrainTask = libAvSink.EncodingCompletionTask;
                if (!libAvDrainTask.IsCompleted)
                {
                    _recordingBackend.PendingLibAvDrainTask = _videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(
                        libAvDrainTask,
                        unifiedVideoCapture,
                        reason: "recording_start_rollback");
                    unifiedVideoCapture = null;
                }
            }

            try
            {
                if (unifiedVideoCapture != null)
                {
                    await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient unified video stop failed during rollback: {ex.Message}");
            }

            try
            {
                if (unifiedVideoCapture != null)
                {
                    await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient unified video dispose failed during rollback: {ex.Message}");
            }
        }

        if (wasapiCapture != null)
        {
            try
            {
                await wasapiCapture.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Transient WASAPI capture dispose failed during rollback: {ex.Message}");
            }
        }
    }

    private sealed class RecordingStartRollbackState
    {
        public LibAvRecordingSink? LibAvSink { get; set; }

        public IRecordingSink? RecordingSink { get; set; }

        public WasapiAudioCapture? OwnedWasapiAudioCapture { get; set; }

        public UnifiedVideoCapture? OwnedUnifiedVideoCapture { get; set; }

        public RecordingContext? RecordingContext { get; set; }

        public UnifiedVideoCapture? RecordingVideoCapture { get; set; }

        public FlashbackEncoderSink? FlashbackRecordingStartedSink { get; set; }

        public bool FlashbackRecordingBackendLeaseHeld { get; set; }

        public bool SinkAttachedForAudioOnly { get; set; }
    }
}
