using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
    private readonly object _recordingOutcomeLock = new();
    private string _lastFinalizeStatus = "None";
    private DateTimeOffset? _lastFinalizeUtc;
    private IReadOnlyList<string> _lastPreservedArtifacts = Array.Empty<string>();
    private RecordingLifecyclePhase _recordingLifecyclePhase = RecordingLifecyclePhase.Idle;
    private RecordingFinalizeOutcome _lastFinalizeOutcome = RecordingFinalizeOutcome.None;
    private string _lastFinalizeFailureCode = string.Empty;
    private bool _lastFinalizationVerificationCompleted;
    private bool _recordingFinalizationCleanupPending;
    private long _lastFinalizationElapsedMs;
    private string? _lastRecordingRecoveryPath;
    private IReadOnlyList<string> _lastRequestedRecordingTracks = Array.Empty<string>();
    private IReadOnlyList<string> _lastObservedRecordingTracks = Array.Empty<string>();
    private string _recordingFinalizationProgressStage = "Idle";
    private DateTimeOffset? _lastRecordingFinalizationProgressUtc;
    private Task? _pendingFlashbackFinalizeCleanupTask;
    private int _recordingAudioStartInProgress;
    private int _recordingFaultAttributionActive;
    private long _recordingMicrophoneSamplesBaseline;
    private long _recordingMicrophoneDropsBaseline;
    private long _recordingMicrophoneDiscontinuitiesBaseline;
    private long _recordingMicrophoneDiscontinuitiesFinal;
    private string? _activeRecordingRecoveryJournalPath;

    internal RecordingFailureRecoveryState? RestoreLastRecordingFailure(string? outputDirectory)
    {
        var recovery = RecordingFinalizationRecoveryArtifacts.TryLoadLatest(outputDirectory);
        if (recovery == null)
        {
            return null;
        }

        lock (_recordingOutcomeLock)
        {
            _lastOutputPath = recovery.OutputPath;
            _lastFinalizeStatus = recovery.Reason;
            _lastFinalizeUtc = recovery.RecordedUtc;
            _lastPreservedArtifacts = recovery.PreservedArtifacts;
            _recordingLifecyclePhase = RecordingLifecyclePhase.Idle;
            _lastFinalizeOutcome = RecordingFinalizeOutcome.Failed;
            _lastFinalizeFailureCode = RecordingFailureCodes.RecoveryRestored;
            _lastFinalizationVerificationCompleted = false;
            _recordingFinalizationCleanupPending = false;
            _lastFinalizationElapsedMs = 0;
            _lastRecordingRecoveryPath = recovery.PreservedArtifacts.FirstOrDefault(path =>
                !RecordingFinalizationRecoveryArtifacts.IsUnresolvedMarkerPath(path))
                ?? recovery.MarkerPath;
            _lastRequestedRecordingTracks = Array.Empty<string>();
            _lastObservedRecordingTracks = Array.Empty<string>();
            _recordingFinalizationProgressStage = "Failed";
            _lastRecordingFinalizationProgressUtc = recovery.RecordedUtc;
        }

        lock (_recordingFailureTelemetryLock)
        {
            _lastRecordingEncodingFailed = true;
            _lastRecordingEncodingFailureType = "RecoveredFinalizationFailure";
            _lastRecordingEncodingFailureMessage = recovery.Reason;
        }

        Logger.Log(
            "RECORDING_FAILURE_RECOVERY_RESTORED " +
            $"marker='{recovery.MarkerPath}' output='{recovery.OutputPath}'");
        return recovery;
    }

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
            _recordingBackend.ThrowIfPendingLibAvDrainBlocksReentry();
            ThrowIfPendingFlashbackFinalizeBlocksReentry();
            ValidateRequestedMicrophone(settings);

            _currentSettings = settings;
            _micMonitorEnabled = settings.MicrophoneEnabled;
            _micMonitorDeviceId = settings.MicrophoneDeviceId;
            _micMonitorDeviceName = settings.MicrophoneDeviceName;

            var rollback = new RecordingStartRollbackState();
            ClearLastRecordingFailure();
            _previewAudioGraph.ResetCaptureFault();
            Volatile.Write(ref _recordingAudioStartInProgress, 1);
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
            finally
            {
                Volatile.Write(ref _recordingAudioStartInProgress, 0);
            }
        }, cancellationToken);

    // Public path used by normal recording-stop (UI Stop button, automation StopRecording).
    public Task StopRecordingAsync(CancellationToken cancellationToken = default)
        => StopRecordingAsync(emergency: false, cancellationToken);

    // Pass the coordinator's emergency flag through backend cleanup so LibAv uses
    // its shorter emergency finalization wait. The app separately limits how long
    // it waits for this entire transition before fatal shutdown.
    internal Task StopRecordingAsync(bool emergency, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CaptureSessionState.Ready, async transitionToken =>
        {
            if (!_isRecording && _recordingBackend.Sink == null && _recordingBackend.LibAvSink == null)
            {
                return;
            }

            transitionToken.ThrowIfCancellationRequested();
            PublishRecordingFinalizingOutcome();
            StatusChanged?.Invoke(this, "Finalizing recording...");
            FinalizeResult result;
            try
            {
                result = await StopAndDisposeRecordingBackendAsync(
                    "Recording saved",
                    emergency,
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                MarkRecordingFinalizationUnresolved(
                    $"Recording failed during finalization: {ex.Message}");
                throw;
            }
            // Preview continues running on the active source-reader/WASAPI sessions - no resume needed.
            StatusChanged?.Invoke(this, result.StatusMessage);
            if (!result.Succeeded)
            {
                var recoveryDetail = string.IsNullOrWhiteSpace(result.RecoveryPath)
                    ? string.Empty
                    : $" Recovery details: {result.RecoveryPath}";
                throw new InvalidOperationException(result.StatusMessage + recoveryDetail);
            }
        }, cancellationToken);

    private static void ValidateRequestedMicrophone(CaptureSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (settings.MicrophoneEnabled && string.IsNullOrWhiteSpace(settings.MicrophoneDeviceId))
        {
            throw new InvalidOperationException(
                "Recording microphone is enabled but no microphone device is selected.");
        }
    }

    private void ThrowIfPendingFlashbackFinalizeBlocksReentry()
    {
        var pending = _pendingFlashbackFinalizeCleanupTask;
        if (pending == null)
        {
            return;
        }

        if (!pending.IsCompleted)
        {
            throw new InvalidOperationException(
                "A prior Flashback recording is still completing bounded cleanup. Wait before starting another recording.");
        }

        _pendingFlashbackFinalizeCleanupTask = null;
    }

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
                GpuHandles = GpuPipelineHandles.None,
                ReserveFinalOutputFile = false
            }).ConfigureAwait(false);

    private async Task StartLibAvRecordingAsync(
        CaptureSettings settings,
        CancellationToken transitionToken,
        RecordingStartRollbackState rollback)
    {
        var libAvSink = new LibAvRecordingSink();
        rollback.LibAvSink = libAvSink;
        libAvSink.OnEncodingFailed = OnRecordingBackendFatalError;
        libAvSink.OnFinalizationProgress = OnRecordingFinalizationProgress;
        libAvSink.FrameEncoded += (s, count) => FrameCaptured?.Invoke(this, unchecked((ulong)Math.Max(0L, count)));
        rollback.RecordingSink = libAvSink;

        var outputFolder = await OpenRecordingOutputFolderAsync(settings).ConfigureAwait(false);

        transitionToken.ThrowIfCancellationRequested();

        var effectiveWidth = _actualWidth ?? settings.Width;
        var effectiveHeight = _actualHeight ?? settings.Height;
        var effectiveFrameRate = _actualFrameRate ?? settings.FrameRate;
        await RefreshSourceTelemetryAsync(transitionToken).ConfigureAwait(false);
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
        SetActualCaptureFrameRate(settings, unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps : effectiveFrameRate);
        _actualPixelFormat = unifiedVideoCapture.NativeInputFormat ?? (unifiedVideoCapture.IsP010 ? "P010" : "NV12");

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

        _recordingMicrophoneSamplesBaseline = activeLibAvSink.MicrophoneSamplesReceived;
        _recordingMicrophoneDropsBaseline =
            activeLibAvSink.MicrophoneDropsQueueSaturated +
            activeLibAvSink.MicrophoneDropsBacklogEviction;
        _recordingMicrophoneDiscontinuitiesBaseline =
            _previewAudioGraph.MicrophoneCapture?.AudioDataDiscontinuityCount ?? 0;
        _recordingMicrophoneDiscontinuitiesFinal = _recordingMicrophoneDiscontinuitiesBaseline;

        IGpuVideoFrameTryEncoder? gpuEncoder =
            (!isMjpegMode && activeLibAvSink.GpuEncodingEnabled)
                ? activeLibAvSink
                : null;

        _recordingIntegrityCounterBaseline = CaptureRecordingIntegrityCounters(activeLibAvSink);
        _recordingIntegrityAudioBaseline = CaptureRecordingAudioCounters(
            _previewAudioGraph.ProgramCapture,
            activeLibAvSink,
            settings);
        activeLibAvSink.MarkRecordingBoundaryStarted();
        await unifiedVideoCapture.StartRecordingAsync(rollback.RecordingSink, activeLibAvSink, gpuEncoder).ConfigureAwait(false);
        if (gpuEncoder != null)
        {
            Logger.Log("GPU_RECORDING_ACTIVE gpu_encoder=active");
        }

        if (rollback.OwnedUnifiedVideoCapture != null)
        {
            rollback.OwnedUnifiedVideoCapture.Start();
        }

        ThrowIfRecordingStartupFailed();
        ValidateRequestedRecordingAudioInputs(settings);
        PrepareActiveRecordingRecoveryJournal(rollback.RecordingContext);
        _recordingBackend.InstallLibAv(
            rollback.LibAvSink,
            rollback.RecordingSink,
            rollback.RecordingContext,
            settings);
        _isRecording = true;
        _activeVideoInputPixelFormat = videoInputPixelFormat;
        Interlocked.Exchange(ref _videoFramesDropped, 0);
        PublishRecordingStartedOutcome(rollback.RecordingContext);
        _recordingStopwatch.Restart();
        EnsureCaptureTelemetrySampling();
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
        if (settings.AudioEnabled &&
            _previewAudioGraph.ProgramCapture is { } staleProgramCapture &&
            (!staleProgramCapture.IsReadyForRecording ||
             !string.Equals(staleProgramCapture.AudioDeviceId, audioDeviceId, StringComparison.OrdinalIgnoreCase)))
        {
            _previewAudioGraph.ProgramCapture = null;
            _previewAudioGraph.DetachCapture(
                staleProgramCapture,
                OnWasapiAudioLevelUpdated,
                OnWasapiCaptureFailed,
                _flashbackBackend.PlaybackController);
            await staleProgramCapture.DisposeAsync().ConfigureAwait(false);
            Logger.Log("RECORDING_AUDIO_CAPTURE_REPLACED reason=terminal_preview_worker");
        }

        if (_previewAudioGraph.ProgramCapture == null && settings.AudioEnabled)
        {
            var resolvedAudioDeviceId = audioDeviceId
                ?? throw new InvalidOperationException("Recording requires an audio capture device.");
            rollback.OwnedWasapiAudioCapture = new WasapiAudioCapture();
            await rollback.OwnedWasapiAudioCapture.InitializeAsync(resolvedAudioDeviceId, transitionToken).ConfigureAwait(false);
            rollback.OwnedWasapiAudioCapture.AudioLevelUpdated += OnWasapiAudioLevelUpdated;
            rollback.OwnedWasapiAudioCapture.CaptureFailed += OnWasapiCaptureFailed;
            _previewAudioGraph.ProgramCapture = rollback.OwnedWasapiAudioCapture;
            await rollback.OwnedWasapiAudioCapture.StartAndWaitForRecordingReadyAsync(transitionToken).ConfigureAwait(false);
        }

        if (_previewAudioGraph.ProgramCapture == null && settings.AudioEnabled)
        {
            throw new InvalidOperationException(
                "Recording requested device audio, but no live audio capture is available.");
        }

        // Dispose preview-time mic monitor; recording creates its own capture wired to the active sink.
        await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);

        if (settings.MicrophoneEnabled)
        {
            var microphoneDeviceId = settings.MicrophoneDeviceId
                ?? throw new InvalidOperationException(
                    "Recording microphone is enabled but no microphone device is selected.");
            var micSink = activeLibAvSink; // capture stable reference - LibAv sink is nulled on success path
            var micCapture = new WasapiAudioCapture();
            await micCapture.InitializeAsync(microphoneDeviceId, transitionToken).ConfigureAwait(false);
            micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;
            micCapture.CaptureFailed += OnWasapiCaptureFailed;
            _previewAudioGraph.MicrophoneCapture = micCapture;
            await micCapture.StartAndWaitForRecordingReadyAsync(transitionToken).ConfigureAwait(false);
            Logger.Log("MICROPHONE_CAPTURE_START device='" + settings.MicrophoneDeviceName + "'");
        }

        // All requested workers are ready before any recording writer is attached.
        // This gives program audio, microphone, and video one tight recording epoch
        // instead of allowing audio to accumulate during microphone initialization.
        if (settings.AudioEnabled)
        {
            _previewAudioGraph.ProgramCapture!.AttachRecordingSink(recordingSink);
            rollback.SinkAttachedForAudioOnly = true;
            if (_isAudioPreviewActive)
            {
                await _previewAudioGraph.StartPlaybackAsync(
                    transitionToken,
                    _flashbackBackend.PlaybackController).ConfigureAwait(false);
            }
        }

        if (settings.MicrophoneEnabled)
        {
            var micSink = activeLibAvSink;
            _previewAudioGraph.MicrophoneCapture!.SetAudioWriter(
                samples => micSink.WriteMicrophoneAudioAsync(samples));
        }
    }

    private void ValidateRequestedRecordingAudioInputs(CaptureSettings settings)
    {
        var expectedProgramAudioDeviceId = settings.UseCustomAudioInput
            ? settings.AudioDeviceId
            : (_audioDeviceId ?? _currentDevice?.AudioDeviceId);
        if (settings.AudioEnabled &&
            (_previewAudioGraph.ProgramCapture?.IsReadyForRecording != true ||
             !string.Equals(
                 _previewAudioGraph.ProgramCapture.AudioDeviceId,
                 expectedProgramAudioDeviceId,
                 StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "Recording requested device audio, but its capture worker is not running.");
        }

        if (settings.MicrophoneEnabled &&
            (_previewAudioGraph.MicrophoneCapture?.IsReadyForRecording != true ||
             !string.Equals(
                 _previewAudioGraph.MicrophoneCapture.AudioDeviceId,
                 settings.MicrophoneDeviceId,
                 StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "Recording requested a microphone, but its capture worker is not running.");
        }

        var startupFault = _previewAudioGraph.ConsumeCaptureFault();
        if (startupFault.Faulted && IsRequestedAudioFault(settings, startupFault.Source))
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(startupFault.Message)
                    ? "A requested audio capture failed during recording startup."
                    : $"A requested audio capture failed during recording startup: {startupFault.Message}");
        }
    }

    private static bool IsRequestedAudioFault(CaptureSettings settings, string? source)
        => string.Equals(source, "microphone", StringComparison.Ordinal)
            ? settings.MicrophoneEnabled
            : string.Equals(source, "program", StringComparison.Ordinal)
                ? settings.AudioEnabled
                : settings.AudioEnabled || settings.MicrophoneEnabled;

    private void ThrowIfRecordingStartupFailed()
    {
        var failure = GetLastFailureTelemetry();
        if (!failure.RecordingFailed)
        {
            return;
        }

        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(failure.RecordingFailureMessage)
                ? "The recording pipeline failed during startup."
                : $"The recording pipeline failed during startup: {failure.RecordingFailureMessage}");
    }

    private void PrepareActiveRecordingRecoveryJournal(
        RecordingContext recordingContext,
        string? artifactDirectory = null)
    {
        _activeRecordingRecoveryJournalPath = RecordingFinalizationRecoveryArtifacts.BeginActive(
            recordingContext.FinalOutputPath,
            recordingContext.VideoOutputPath,
            string.IsNullOrWhiteSpace(artifactDirectory)
                ? Array.Empty<string>()
                : new[] { artifactDirectory });
    }

    private void PublishRecordingStartedOutcome(RecordingContext recordingContext)
    {
        Volatile.Write(ref _recordingFaultAttributionActive, 1);
        lock (_recordingOutcomeLock)
        {
            _recordingLifecyclePhase = RecordingLifecyclePhase.Recording;
            _lastFinalizeOutcome = RecordingFinalizeOutcome.None;
            _lastOutputPath = recordingContext.FinalOutputPath;
            _lastFinalizeStatus = "Recording";
            _lastFinalizeUtc = null;
            _lastPreservedArtifacts = Array.Empty<string>();
            _lastFinalizeFailureCode = string.Empty;
            _lastFinalizationVerificationCompleted = false;
            _recordingFinalizationCleanupPending = false;
            _lastFinalizationElapsedMs = 0;
            _lastRecordingRecoveryPath = null;
            _lastRequestedRecordingTracks = RecordingTracks.BuildRequestedTracks(recordingContext);
            _lastObservedRecordingTracks = Array.Empty<string>();
            _recordingFinalizationProgressStage = "Recording";
            _lastRecordingFinalizationProgressUtc = DateTimeOffset.UtcNow;
        }
    }

    private void PublishRecordingFinalizingOutcome()
    {
        lock (_recordingOutcomeLock)
        {
            _recordingLifecyclePhase = RecordingLifecyclePhase.Finalizing;
            _lastFinalizeOutcome = RecordingFinalizeOutcome.None;
            _lastFinalizeStatus = "Finalizing recording";
            _lastFinalizeUtc = null;
            _lastFinalizeFailureCode = string.Empty;
            _lastFinalizationVerificationCompleted = false;
            _recordingFinalizationCleanupPending = false;
            _lastFinalizationElapsedMs = 0;
            _recordingFinalizationProgressStage = "Draining";
            _lastRecordingFinalizationProgressUtc = DateTimeOffset.UtcNow;
        }
    }

    private void OnRecordingFinalizationProgress(RecordingFinalizationProgress progress)
    {
        lock (_recordingOutcomeLock)
        {
            _recordingFinalizationProgressStage = progress.Stage;
            _lastFinalizationElapsedMs = progress.ElapsedMs;
            _lastRecordingFinalizationProgressUtc = DateTimeOffset.UtcNow;
        }
        if (progress.NoProgressWarning)
        {
            StatusChanged?.Invoke(
                this,
                $"Still finalizing recording ({progress.Stage.ToLowerInvariant()})...");
        }

    }

    private void PublishRecordingFinalizedOutcome(FinalizeResult result, bool updateOutputPath)
    {
        lock (_recordingOutcomeLock)
        {
            if (updateOutputPath)
            {
                _lastOutputPath = result.OutputPath;
            }

            _lastFinalizeStatus = result.StatusMessage;
            _lastFinalizeUtc = DateTimeOffset.UtcNow;
            _lastPreservedArtifacts = result.PreservedArtifacts;
            _lastFinalizeFailureCode = result.FailureCode;
            _lastFinalizationVerificationCompleted = result.VerificationCompleted;
            _recordingFinalizationCleanupPending = result.CleanupPending;
            _lastFinalizationElapsedMs = result.FinalizationElapsedMs;
            _lastRecordingRecoveryPath = result.RecoveryPath;
            _lastRequestedRecordingTracks = result.RequestedTracks;
            _lastObservedRecordingTracks = result.ObservedTracks;
            _recordingFinalizationProgressStage = result.Succeeded ? "Verified" : "Failed";
            _lastRecordingFinalizationProgressUtc = DateTimeOffset.UtcNow;
            _lastFinalizeOutcome = result.Outcome;
            _recordingLifecyclePhase = RecordingLifecyclePhase.Idle;
        }

        if (result.CleanupPending &&
            (_recordingBackend.PendingLibAvDrainTask?.IsCompleted == true ||
             _pendingFlashbackFinalizeCleanupTask?.IsCompleted == true))
        {
            MarkRecordingFinalizationCleanupCompleted("already_complete_at_publish");
        }

        if (result.Succeeded || HasDurableUnresolvedRecoveryMarker(result))
        {
            var activeJournalPath = Interlocked.Exchange(ref _activeRecordingRecoveryJournalPath, null);
            RecordingFinalizationRecoveryArtifacts.RetireActive(activeJournalPath);
        }
    }

    private static bool HasDurableUnresolvedRecoveryMarker(FinalizeResult result)
    {
        if (IsExistingUnresolvedRecoveryMarker(result.RecoveryPath))
        {
            return true;
        }

        return result.PreservedArtifacts.Any(IsExistingUnresolvedRecoveryMarker);
    }

    private static bool IsExistingUnresolvedRecoveryMarker(string? path)
        => RecordingFinalizationRecoveryArtifacts.IsUnresolvedMarkerPath(path) && File.Exists(path);

    private void SetPendingLibAvCleanupTask(Task cleanupTask, string backend)
    {
        _recordingBackend.PendingLibAvDrainTask = cleanupTask;
        _ = TrackRecordingFinalizationCleanupAsync(cleanupTask, backend);
    }

    private async Task TrackRecordingFinalizationCleanupAsync(Task cleanupTask, string backend)
    {
        try
        {
            await cleanupTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log(
                "RECORDING_FINALIZATION_CLEANUP_FAILED " +
                $"backend='{backend}' type={ex.GetType().Name} error='{ex.Message}'");
        }
        finally
        {
            // A later ownership step may compose this task with capture cleanup.
            // Only the currently authoritative task may publish cleanup completion.
            if (ReferenceEquals(_recordingBackend.PendingLibAvDrainTask, cleanupTask))
            {
                MarkRecordingFinalizationCleanupCompleted(backend);
            }
        }
    }

    private void MarkRecordingFinalizationCleanupCompleted(string backend)
    {
        lock (_recordingOutcomeLock)
        {
            _recordingFinalizationCleanupPending = false;
            _recordingFinalizationProgressStage = "CleanupComplete";
            _lastRecordingFinalizationProgressUtc = DateTimeOffset.UtcNow;
        }
        Logger.Log($"RECORDING_FINALIZATION_CLEANUP_COMPLETE backend='{backend}'");
    }

    private RecordingOutcomeSnapshot CaptureRecordingOutcomeSnapshot()
    {
        lock (_recordingOutcomeLock)
        {
            return new RecordingOutcomeSnapshot(
                _lastOutputPath,
                _lastFinalizeStatus,
                _lastFinalizeUtc,
                _lastPreservedArtifacts,
                _recordingLifecyclePhase,
                _lastFinalizeOutcome,
                _lastFinalizeFailureCode,
                _lastFinalizationVerificationCompleted,
                _recordingFinalizationCleanupPending,
                _lastFinalizationElapsedMs,
                _lastRecordingRecoveryPath,
                _lastRequestedRecordingTracks,
                _lastObservedRecordingTracks,
                _recordingFinalizationProgressStage,
                _lastRecordingFinalizationProgressUtc);
        }
    }

    private sealed record RecordingOutcomeSnapshot(
        string? OutputPath,
        string FinalizeStatus,
        DateTimeOffset? FinalizeUtc,
        IReadOnlyList<string> PreservedArtifacts,
        RecordingLifecyclePhase LifecyclePhase,
        RecordingFinalizeOutcome FinalizeOutcome,
        string FailureCode,
        bool VerificationCompleted,
        bool CleanupPending,
        long FinalizationElapsedMs,
        string? RecoveryPath,
        IReadOnlyList<string> RequestedTracks,
        IReadOnlyList<string> ObservedTracks,
        string ProgressStage,
        DateTimeOffset? LastProgressUtc);

    internal void MarkRecordingFinalizationUnresolved(string statusMessage)
    {
        var recordingOutcome = CaptureRecordingOutcomeSnapshot();
        if (recordingOutcome.FinalizeUtc.HasValue &&
            !string.Equals(recordingOutcome.FinalizeStatus, "Recording", StringComparison.Ordinal) &&
            !string.Equals(recordingOutcome.FinalizeStatus, "None", StringComparison.Ordinal))
        {
            Logger.Log(
                "RECORDING_FINALIZE_UNRESOLVED_SKIPPED " +
                $"reason=existing_finalization_status status='{recordingOutcome.FinalizeStatus}'");
            return;
        }

        var fallbackOutputPath = _recordingBackend.Context?.FinalOutputPath ?? recordingOutcome.OutputPath ?? string.Empty;
        var flashbackArtifacts = PreserveUnresolvedFlashbackRecordingArtifacts();
        var preservedArtifacts = RecordingFinalizationRecoveryArtifacts.PreserveUnresolvedWithArtifacts(
            _recordingBackend.Context,
            fallbackOutputPath,
            statusMessage,
            flashbackArtifacts);
        var unresolvedResult = EnsureRecordingFailureRecovery(
            FinalizeResult.Failure(
                fallbackOutputPath,
                statusMessage,
                preservedArtifacts,
                RecordingFailureCodes.FinalizationUnresolved),
            _recordingBackend.Context);
        PublishRecordingFinalizedOutcome(unresolvedResult, updateOutputPath: false);
    }

    private IReadOnlyList<string> PreserveUnresolvedFlashbackRecordingArtifacts()
    {
        // Forced exit can race the narrow handoff between detaching the recording
        // backend and publishing finalize-in-progress. Preserve any live Flashback
        // session conservatively rather than depending on that transient ownership flag.
        if (_flashbackBackend.BufferManager == null)
        {
            return Array.Empty<string>();
        }

        try
        {
            _flashbackBackend.PreserveRecoverySegments("recording_finalization_unresolved");
            return GetFlashbackSegments()
                .Select(segment => segment.Path)
                .ToArray();
        }
        catch (Exception ex)
        {
            Logger.Log(
                "RECORDING_FINALIZE_UNRESOLVED_FLASHBACK_PRESERVE_WARN " +
                $"type={ex.GetType().Name} error='{ex.Message}'");
            return Array.Empty<string>();
        }
    }

    // Recording finalization router: choose the active recording backend and delegate
    // backend-specific stop/dispose work to the focused owners.
    private async Task<FinalizeResult> StopAndDisposeRecordingBackendAsync(string fallbackStatusMessage, bool emergency, CancellationToken cancellationToken)
    {
        if (IsFlashbackRecordingBackendActive())
        {
            return await StopAndDisposeFlashbackRecordingBackendAsync(emergency, cancellationToken).ConfigureAwait(false);
        }

        return await StopAndDisposeLibAvRecordingBackendAsync(fallbackStatusMessage, emergency, cancellationToken).ConfigureAwait(false);
    }

    private async Task<FinalizeResult> StopAndDisposeLibAvRecordingBackendAsync(string fallbackStatusMessage, bool emergency, CancellationToken cancellationToken)
    {
        var detachedBackend = _recordingBackend.DetachLibAvBackend();
        var sink = detachedBackend.Sink;
        var libAvSink = detachedBackend.LibAvSink;
        var recordingContext = detachedBackend.Context;
        var fallbackOutputPath = recordingContext?.FinalOutputPath ?? (_lastOutputPath ?? string.Empty);

        var result = FinalizeResult.Success(fallbackOutputPath, fallbackStatusMessage);
        OperationCanceledException? cancellationException = null;

        Volatile.Write(ref _recordingFaultAttributionActive, 0);
        libAvSink?.MarkRecordingBoundaryStopped();
        var videoBoundary = await StopUnifiedVideoRecordingForLibAvFinalizeAsync(
            result,
            fallbackOutputPath,
            cancellationToken).ConfigureAwait(false);
        result = videoBoundary.Result;
        cancellationException = videoBoundary.CancellationException;

        // Detach the microphone writer synchronously. During emergency shutdown,
        // await its worker alongside encoder finalization to reduce total wait time.
        var microphoneStopTask = DetachLibAvRecordingAudioBeforeSinkStopAsync();
        if (!emergency)
        {
            await microphoneStopTask.ConfigureAwait(false);
        }

        var sinkStop = await StopAndDisposeLibAvSinkForFinalizeAsync(
            sink,
            libAvSink,
            result,
            fallbackOutputPath,
            emergency,
            cancellationException,
            cancellationToken).ConfigureAwait(false);
        result = sinkStop.Result;
        cancellationException = sinkStop.CancellationException;

        if (emergency)
        {
            await microphoneStopTask.ConfigureAwait(false);
        }

        var libAvFinalAudioCounters = libAvSink != null
            ? GetRecordingAudioCountersSinceBaseline(
                CaptureRecordingAudioCounters(_previewAudioGraph.ProgramCapture, libAvSink, _recordingBackend.SettingsSnapshot))
            : RecordingAudioIntegrityCounterSnapshot.Disabled;

        var idlePreviewDisposal = await DisposeIdleLibAvPreviewResourcesAfterRecordingAsync(
            result,
            fallbackOutputPath,
            cancellationException,
            emergency).ConfigureAwait(false);
        result = idlePreviewDisposal.Result;
        cancellationException = idlePreviewDisposal.CancellationException;

        result = FoldRecordingAudioFaultIntoFinalizeResult(
            result,
            cancellationException,
            recordingContext?.Settings);
        result = FoldRequestedProgramAudioIntegrityIntoFinalizeResult(
            result,
            libAvFinalAudioCounters);
        if (libAvSink != null)
        {
            result = FoldRequestedMicrophoneIntegrityIntoFinalizeResult(
                result,
                recordingContext?.MicrophoneEnabled == true,
                libAvSink.MicrophoneSamplesReceived,
                libAvSink.MicrophoneDropsQueueSaturated + libAvSink.MicrophoneDropsBacklogEviction,
                Math.Max(
                    0,
                    _recordingMicrophoneDiscontinuitiesFinal -
                    _recordingMicrophoneDiscontinuitiesBaseline));
        }
        result = FoldRecordedRuntimeFailureIntoFinalizeResult(result);
        result = VerifyFinalizedOutputBeforeSaved(result, recordingContext);
        result = EnsureRecordingFailureRecovery(result, recordingContext);

        PublishLibAvRecordingIntegrity(
            libAvSink,
            result,
            videoBoundary,
            libAvFinalAudioCounters);

        await CompleteLibAvRecordingFinalizeStateAsync().ConfigureAwait(false);

        cancellationException = await RestoreLibAvPreviewFeaturesAfterRecordingAsync(
            cancellationException,
            cancellationToken).ConfigureAwait(false);

        PublishRecordingFinalizedOutcome(result, updateOutputPath: true);

        if (cancellationException != null)
        {
            throw cancellationException;
        }

        return result;
    }

    private static FinalizeResult EnsureRecordingFailureRecovery(
        FinalizeResult result,
        RecordingContext? recordingContext)
    {
        if (result.Succeeded)
        {
            return result;
        }

        var recoveryArtifacts = RecordingFinalizationRecoveryArtifacts.PreserveUnresolvedWithArtifacts(
            recordingContext,
            result.OutputPath,
            result.StatusMessage,
            result.PreservedArtifacts);
        var combinedArtifacts = new List<string>();
        AddUniqueArtifacts(combinedArtifacts, result.PreservedArtifacts);
        AddUniqueArtifacts(combinedArtifacts, recoveryArtifacts);

        var recoveryPath = RecordingFinalizationRecoveryArtifacts.ResolveRecoveryPath(combinedArtifacts, result.RecoveryPath);
        var recovered = result.AsFailureWithArtifacts(combinedArtifacts, recoveryPath);

        // AsFailureWithArtifacts leaves track evidence untouched; preserve the
        // original fallback that fills in requested tracks from the recording
        // context when the result hadn't already recorded any.
        return recovered.RequestedTracks.Count > 0 || recordingContext == null
            ? recovered
            : recovered.WithTrackEvidence(
                RecordingTracks.BuildRequestedTracks(recordingContext),
                recovered.ObservedTracks);
    }

    private static FinalizeResult VerifyFinalizedOutputBeforeSaved(
        FinalizeResult result,
        RecordingContext? recordingContext,
        TimeSpan? expectedRecordingDuration = null)
    {
        if (!result.Succeeded || result.VerificationCompleted)
        {
            return result;
        }

        if (recordingContext == null)
        {
            return FinalizeResult.Failure(
                result.OutputPath,
                "Recording failed (finalization context was unavailable for verification)",
                result.PreservedArtifacts,
                RecordingFailureCodes.VerificationContextMissing,
                result.CleanupPending,
                result.RecoveryPath,
                verificationCompleted: false,
                finalizationElapsedMs: result.FinalizationElapsedMs)
                .WithTrackEvidence(Array.Empty<string>(), Array.Empty<string>());
        }

        var verification = new InProcessRecordingStructureVerifier().Verify(
            recordingContext,
            expectedRecordingDuration);
        if (!verification.Succeeded)
        {
            return FinalizeResult.Failure(
                result.OutputPath,
                $"Recording failed (structural verification failed: {verification.Detail})",
                result.PreservedArtifacts,
                verification.FailureCode,
                result.CleanupPending,
                result.RecoveryPath,
                verificationCompleted: false,
                finalizationElapsedMs: result.FinalizationElapsedMs)
                .WithTrackEvidence(verification.RequestedTracks, verification.ObservedTracks);
        }

        return FinalizeResult.Success(
            result.OutputPath,
            result.StatusMessage,
            verificationCompleted: true,
            finalizationElapsedMs: result.FinalizationElapsedMs)
            .WithTrackEvidence(verification.RequestedTracks, verification.ObservedTracks);
    }

    private static void AddUniqueArtifacts(
        List<string> target,
        IEnumerable<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) &&
                !target.Contains(candidate, StringComparer.OrdinalIgnoreCase))
            {
                target.Add(candidate);
            }
        }
    }

    private static FinalizeResult MergeFinalizeTrackEvidence(
        FinalizeResult result,
        FinalizeResult supplementary)
    {
        var requestedTracks = new List<string>();
        AddUniqueArtifacts(requestedTracks, result.RequestedTracks);
        AddUniqueArtifacts(requestedTracks, supplementary.RequestedTracks);

        var observedTracks = new List<string>();
        AddUniqueArtifacts(observedTracks, result.ObservedTracks);
        AddUniqueArtifacts(observedTracks, supplementary.ObservedTracks);

        return result.WithTrackEvidence(requestedTracks, observedTracks);
    }

    private FinalizeResult FoldRecordingAudioFaultIntoFinalizeResult(
        FinalizeResult result,
        OperationCanceledException? cancellationException,
        CaptureSettings? requestedSettings)
    {
        var wasapiAudioCaptureFault = _previewAudioGraph.ConsumeCaptureFault();
        if (!wasapiAudioCaptureFault.Faulted ||
            requestedSettings == null ||
            !IsRequestedAudioFault(requestedSettings, wasapiAudioCaptureFault.Source) ||
            cancellationException != null ||
            !result.Succeeded)
        {
            return result;
        }

        var statusMessage = string.IsNullOrWhiteSpace(wasapiAudioCaptureFault.Message)
            ? "Recording failed (WASAPI audio capture faulted)."
            : $"Recording failed (WASAPI audio capture faulted: {wasapiAudioCaptureFault.Message})";
        Logger.Log($"RECORDING_AUDIO_FAULT status='{statusMessage}'");
        return result.AsFailure(statusMessage, RecordingFailureCodes.AudioCaptureFailed);
    }

    private FinalizeResult FoldRequestedMicrophoneIntegrityIntoFinalizeResult(
        FinalizeResult result,
        bool microphoneRequested,
        long microphoneSamplesReceived,
        long microphoneDrops,
        long microphoneDiscontinuities)
    {
        if (!result.Succeeded || !microphoneRequested)
        {
            return result;
        }

        var recordedSamples = Math.Max(0, microphoneSamplesReceived - _recordingMicrophoneSamplesBaseline);
        var droppedPackets = Math.Max(0, microphoneDrops - _recordingMicrophoneDropsBaseline);
        if (recordedSamples > 0 && droppedPackets == 0 && microphoneDiscontinuities == 0)
        {
            return result;
        }

        var reason = recordedSamples == 0
            ? "Recording failed because the requested microphone produced no samples during the recording interval."
            : droppedPackets > 0
                ? $"Recording failed because the requested microphone dropped {droppedPackets} packet(s)."
                : $"Recording failed because the requested microphone reported {microphoneDiscontinuities} data discontinuity event(s).";
        Logger.Log(
            $"RECORDING_MICROPHONE_INTEGRITY_FAIL samples={recordedSamples} " +
            $"drops={droppedPackets} discontinuities={microphoneDiscontinuities}");
        return result.AsFailure(reason, RecordingFailureCodes.MicrophoneIntegrityFailed);
    }

    private FinalizeResult FoldRequestedProgramAudioIntegrityIntoFinalizeResult(
        FinalizeResult result,
        RecordingAudioIntegrityCounterSnapshot audioCounters)
    {
        if (!result.Succeeded || !audioCounters.AudioEnabled)
        {
            return result;
        }

        var noRecordedAudio = audioCounters.AudioSamplesEncoded <= 0 ||
                              audioCounters.AudioFramesWrittenToSink <= 0;
        var integrityEvents = SumNonNegative(
            audioCounters.AudioDropEvents,
            audioCounters.AudioDiscontinuities);
        if (!noRecordedAudio && integrityEvents == 0)
        {
            return result;
        }

        var reason = noRecordedAudio
            ? "Recording failed because the requested device-audio stream produced no samples during the recording interval."
            : $"Recording failed because the requested device-audio stream reported {integrityEvents} loss or discontinuity event(s).";
        Logger.Log(
            "RECORDING_PROGRAM_AUDIO_INTEGRITY_FAIL " +
            $"samples={audioCounters.AudioSamplesEncoded} " +
            $"drops={audioCounters.AudioDropEvents} " +
            $"discontinuities={audioCounters.AudioDiscontinuities} " +
            $"timestamp_errors={audioCounters.AudioTimestampErrors} " +
            $"callback_gaps={audioCounters.AudioCallbackGaps}");
        return result.AsFailure(reason, RecordingFailureCodes.ProgramAudioIntegrityFailed);
    }

    private FinalizeResult FoldRecordedRuntimeFailureIntoFinalizeResult(FinalizeResult result)
    {
        if (!result.Succeeded)
        {
            return result;
        }

        var failure = GetLastFailureTelemetry();
        if (!failure.RecordingFailed)
        {
            return result;
        }

        var failureType = string.IsNullOrWhiteSpace(failure.RecordingFailureType)
            ? "runtime"
            : failure.RecordingFailureType;
        var failureMessage = string.IsNullOrWhiteSpace(failure.RecordingFailureMessage)
            ? "The recording pipeline reported a fatal runtime failure."
            : failure.RecordingFailureMessage;
        Logger.Log(
            "RECORDING_RUNTIME_FAILURE_FOLDED " +
            $"type='{failureType}' message='{failureMessage}'");
        return result.AsFailure($"Recording failed ({failureType}: {failureMessage})", RecordingFailureCodes.RuntimeFailed);
    }

    private void PublishLibAvRecordingIntegrity(
        LibAvRecordingSink? libAvSink,
        FinalizeResult result,
        LibAvVideoBoundaryStopResult videoBoundary,
        RecordingAudioIntegrityCounterSnapshot libAvFinalAudioCounters)
    {
        if (libAvSink == null)
        {
            return;
        }

        CaptureEncoderRuntimeTelemetry(libAvSink);
        _lastRecordingIntegrity = BuildRecordingIntegritySummary(
            backend: "LibAv",
            recordingActive: false,
            finalizeSucceeded: result.Succeeded,
            finalizeStatus: result.StatusMessage,
            completedUtc: DateTimeOffset.UtcNow,
            sourceFrames: videoBoundary.RecordingFramesDeliveredToBoundary,
            acceptedFrames: videoBoundary.RecordingFramesAcceptedByBoundary,
            counters: GetRecordingIntegrityCountersSinceBaseline(CaptureRecordingIntegrityCounters(libAvSink)),
            audioCounters: libAvFinalAudioCounters,
            recordingBoundaryRejectedFrames: videoBoundary.RecordingFramesRejectedByBoundary,
            recordingQueueRejectedFrames: videoBoundary.RecordingQueueRejectedByBoundary);
        _recordingIntegrityCounterBaseline = null;
        _recordingIntegrityAudioBaseline = null;
        LogRecordingIntegritySummary(_lastRecordingIntegrity);
    }

    // Recording integrity compares counters captured at start/stop, not just final
    // file metadata. This catches capture/sink discontinuities that a syntactically
    // valid MP4 would otherwise hide.
    private const double RecordingIntegrityAvSyncDriftWarningMs = 500.0;
    private const long RecordingIntegrityAudioBoundaryToleranceFrames = 960;

    private sealed record RecordingIntegrityCounterSnapshot(
        string Backend,
        long SubmittedFrames,
        long EncodedFrames,
        long PacketsWritten,
        long EncoderDroppedFrames,
        long QueueDroppedFrames,
        long SequenceGaps,
        int QueueMaxDepth,
        long QueueOldestFrameAgeMs,
        long BackpressureWaitMs,
        long BackpressureEvents,
        long BackpressureMaxWaitMs,
        bool EncodingFailed,
        string? EncodingFailureType,
        string? EncodingFailureMessage);

    private sealed record RecordingAudioIntegrityCounterSnapshot(
        bool AudioEnabled,
        bool AudioCaptureActive,
        long AudioFramesArrived,
        long AudioFramesWrittenToSink,
        long AudioSamplesEncoded,
        long AudioDropEvents,
        long AudioDiscontinuities,
        long AudioTimestampErrors,
        long AudioCallbackGaps,
        double? AvSyncDriftMs,
        double? AvSyncDriftRateMsPerSec,
        double? EncoderAvSyncDriftMs,
        long? EncoderAvSyncCorrectionSamples)
    {
        public static RecordingAudioIntegrityCounterSnapshot Disabled { get; } = new(
            AudioEnabled: false,
            AudioCaptureActive: false,
            AudioFramesArrived: 0,
            AudioFramesWrittenToSink: 0,
            AudioSamplesEncoded: 0,
            AudioDropEvents: 0,
            AudioDiscontinuities: 0,
            AudioTimestampErrors: 0,
            AudioCallbackGaps: 0,
            AvSyncDriftMs: null,
            AvSyncDriftRateMsPerSec: null,
            EncoderAvSyncDriftMs: null,
            EncoderAvSyncCorrectionSamples: null);
    }

    private readonly record struct RecordingIntegritySummaryVideoFields
    {
        public long SourceFrames { get; init; }
        public long AcceptedFrames { get; init; }
        public long PipelineDroppedFrames { get; init; }
        public long RecordingBoundaryRejectedFrames { get; init; }
        public long RecordingQueueRejectedFrames { get; init; }
        public long QueueDroppedFrames { get; init; }
        public long SubmittedFrames { get; init; }
        public long EncodedFrames { get; init; }
        public long PacketsWritten { get; init; }
        public long EncoderDroppedFrames { get; init; }
        public long SequenceGaps { get; init; }
        public int QueueMaxDepth { get; init; }
        public long QueueOldestFrameAgeMs { get; init; }
        public long BackpressureWaitMs { get; init; }
        public long BackpressureEvents { get; init; }
        public long BackpressureMaxWaitMs { get; init; }
        public bool EncodingFailed { get; init; }
        public string? EncodingFailureType { get; init; }
        public string? EncodingFailureMessage { get; init; }
    }

    private readonly record struct RecordingIntegritySummaryAudioFields
    {
        public bool AudioEnabled { get; init; }
        public bool AudioCaptureActive { get; init; }
        public long AudioFramesArrived { get; init; }
        public long AudioFramesWrittenToSink { get; init; }
        public long AudioSamplesEncoded { get; init; }
        public long AudioDropEvents { get; init; }
        public long AudioDiscontinuities { get; init; }
        public long AudioTimestampErrors { get; init; }
        public long AudioCallbackGaps { get; init; }
        public double? AvSyncDriftMs { get; init; }
        public double? AvSyncDriftRateMsPerSec { get; init; }
        public double? EncoderAvSyncDriftMs { get; init; }
        public long? EncoderAvSyncCorrectionSamples { get; init; }
    }

    private readonly record struct RecordingIntegritySummaryEvaluation(
        string Status,
        string AudioStatus,
        string Reason);

    private RecordingIntegritySummary ResolveRecordingIntegritySummary(
        UnifiedVideoCapture? unifiedVideoCapture,
        LibAvRecordingSink? sink,
        FlashbackEncoderSink? fbSink)
    {
        if (!_isRecording)
        {
            return _lastRecordingIntegrity;
        }

        if (IsFlashbackRecordingBackendOwnedByRecording() && fbSink != null)
        {
            var counters = CaptureFlashbackRecordingIntegrityCountersSinceBaseline(fbSink, unifiedVideoCapture);
            var audioCounters = GetRecordingAudioCountersSinceBaseline(
                CaptureRecordingAudioCounters(_previewAudioGraph.ProgramCapture, fbSink, _recordingBackend.SettingsSnapshot));
            return BuildRecordingIntegritySummary(
                backend: "Flashback",
                recordingActive: true,
                finalizeSucceeded: true,
                finalizeStatus: "Recording",
                completedUtc: null,
                sourceFrames: unifiedVideoCapture?.RecordingFramesDelivered ?? 0,
                acceptedFrames: unifiedVideoCapture?.VideoFramesWrittenToSink ?? 0,
                counters: counters,
                audioCounters: audioCounters,
                recordingBoundaryRejectedFrames: unifiedVideoCapture?.RecordingFramesRejected ?? 0,
                recordingQueueRejectedFrames: unifiedVideoCapture?.RecordingQueueRejectedFrames ?? 0);
        }

        if (sink != null)
        {
            var counters = GetRecordingIntegrityCountersSinceBaseline(CaptureRecordingIntegrityCounters(sink));
            var audioCounters = GetRecordingAudioCountersSinceBaseline(
                CaptureRecordingAudioCounters(_previewAudioGraph.ProgramCapture, sink, _recordingBackend.SettingsSnapshot));
            return BuildRecordingIntegritySummary(
                backend: "LibAv",
                recordingActive: true,
                finalizeSucceeded: true,
                finalizeStatus: "Recording",
                completedUtc: null,
                sourceFrames: unifiedVideoCapture?.RecordingFramesDelivered ?? 0,
                acceptedFrames: unifiedVideoCapture?.VideoFramesWrittenToSink ?? 0,
                counters: counters,
                audioCounters: audioCounters,
                recordingBoundaryRejectedFrames: unifiedVideoCapture?.RecordingFramesRejected ?? 0,
                recordingQueueRejectedFrames: unifiedVideoCapture?.RecordingQueueRejectedFrames ?? 0);
        }

        return new RecordingIntegritySummary
        {
            Status = "Active",
            Backend = ResolveRecordingBackendName(),
            Reason = "Recording active; recording boundary is still attaching."
        };
    }

    private RecordingIntegrityCounterSnapshot GetRecordingIntegrityCountersSinceBaseline(RecordingIntegrityCounterSnapshot current)
    {
        var baseline = _recordingIntegrityCounterBaseline;
        if (baseline == null ||
            !string.Equals(baseline.Backend, current.Backend, StringComparison.Ordinal))
        {
            return current;
        }

        return current with
        {
            SubmittedFrames = DeltaCounter(current.SubmittedFrames, baseline.SubmittedFrames),
            EncodedFrames = DeltaCounter(current.EncodedFrames, baseline.EncodedFrames),
            PacketsWritten = DeltaCounter(current.PacketsWritten, baseline.PacketsWritten),
            EncoderDroppedFrames = DeltaCounter(current.EncoderDroppedFrames, baseline.EncoderDroppedFrames),
            QueueDroppedFrames = DeltaCounter(current.QueueDroppedFrames, baseline.QueueDroppedFrames),
            SequenceGaps = DeltaCounter(current.SequenceGaps, baseline.SequenceGaps),
            BackpressureWaitMs = DeltaCounter(current.BackpressureWaitMs, baseline.BackpressureWaitMs),
            BackpressureEvents = DeltaCounter(current.BackpressureEvents, baseline.BackpressureEvents)
        };
    }

    private static RecordingIntegrityCounterSnapshot CaptureRecordingIntegrityCounters(LibAvRecordingSink sink)
        => new(
            Backend: "LibAv",
            SubmittedFrames: sink.VideoFramesSubmittedToEncoder,
            EncodedFrames: sink.EncodedVideoFrames,
            PacketsWritten: sink.VideoEncoderPacketsWritten,
            EncoderDroppedFrames: sink.VideoEncoderDroppedFrames,
            QueueDroppedFrames: SumNonNegative(
                sink.VideoDropsQueueSaturated,
                sink.GpuFramesDropped,
                sink.CudaFramesDropped),
            SequenceGaps: sink.VideoSequenceGaps,
            QueueMaxDepth: Math.Max(sink.VideoQueueMaxDepth, Math.Max(sink.GpuQueueMaxDepth, sink.CudaQueueMaxDepth)),
            QueueOldestFrameAgeMs: sink.VideoQueueOldestFrameAgeMs,
            BackpressureWaitMs: sink.VideoBackpressureWaitMs,
            BackpressureEvents: sink.VideoBackpressureEvents,
            BackpressureMaxWaitMs: sink.MaxVideoBackpressureWaitMs,
            EncodingFailed: sink.EncodingFailed,
            EncodingFailureType: sink.EncodingFailureType,
            EncodingFailureMessage: sink.EncodingFailureMessage);

    private static RecordingIntegrityCounterSnapshot CaptureRecordingIntegrityCounters(FlashbackEncoderSink sink)
        => new(
            Backend: "Flashback",
            SubmittedFrames: sink.VideoFramesSubmittedToEncoder,
            EncodedFrames: sink.EncodedVideoFrames,
            PacketsWritten: sink.VideoEncoderPacketsWritten,
            EncoderDroppedFrames: sink.VideoEncoderDroppedFrames,
            QueueDroppedFrames: SumNonNegative(
                sink.VideoDropsQueueSaturated,
                sink.GpuFramesDropped),
            SequenceGaps: sink.VideoSequenceGaps,
            QueueMaxDepth: Math.Max(sink.VideoQueueMaxDepth, sink.GpuQueueMaxDepth),
            QueueOldestFrameAgeMs: sink.VideoQueueOldestFrameAgeMs,
            BackpressureWaitMs: sink.VideoBackpressureWaitMs,
            BackpressureEvents: sink.VideoBackpressureEvents,
            BackpressureMaxWaitMs: sink.MaxVideoBackpressureWaitMs,
            EncodingFailed: sink.EncodingFailed,
            EncodingFailureType: sink.EncodingFailureType,
            EncodingFailureMessage: sink.EncodingFailureMessage);

    private RecordingIntegrityCounterSnapshot CaptureFlashbackRecordingIntegrityCountersSinceBaseline(
        FlashbackEncoderSink sink,
        UnifiedVideoCapture? videoCapture)
    {
        var counters = GetRecordingIntegrityCountersSinceBaseline(CaptureRecordingIntegrityCounters(sink));
        return videoCapture == null
            ? counters
            : counters with { SequenceGaps = Math.Max(0, videoCapture.FlashbackRecordingSequenceGaps) };
    }

    private RecordingAudioIntegrityCounterSnapshot GetRecordingAudioCountersSinceBaseline(RecordingAudioIntegrityCounterSnapshot current)
    {
        var baseline = _recordingIntegrityAudioBaseline;
        if (baseline == null)
        {
            return current;
        }

        return current with
        {
            AudioFramesArrived = DeltaCounter(current.AudioFramesArrived, baseline.AudioFramesArrived),
            AudioFramesWrittenToSink = DeltaCounter(current.AudioFramesWrittenToSink, baseline.AudioFramesWrittenToSink),
            AudioSamplesEncoded = DeltaCounter(current.AudioSamplesEncoded, baseline.AudioSamplesEncoded),
            AudioDropEvents = DeltaCounter(current.AudioDropEvents, baseline.AudioDropEvents),
            AudioDiscontinuities = DeltaCounter(current.AudioDiscontinuities, baseline.AudioDiscontinuities),
            AudioTimestampErrors = DeltaCounter(current.AudioTimestampErrors, baseline.AudioTimestampErrors),
            AudioCallbackGaps = DeltaCounter(current.AudioCallbackGaps, baseline.AudioCallbackGaps)
        };
    }

    private RecordingAudioIntegrityCounterSnapshot CaptureRecordingAudioCounters(
        WasapiAudioCapture? capture,
        LibAvRecordingSink sink,
        CaptureSettings? settings)
    {
        double? encoderAvSyncDriftMs = null;
        long? encoderAvSyncCorrectionSamples = null;
        if (sink.TryGetEncoderAvSyncDrift(out var driftMs, out var correctionSamples))
        {
            encoderAvSyncDriftMs = driftMs;
            encoderAvSyncCorrectionSamples = correctionSamples;
        }

        return CreateRecordingAudioCounters(
            capture,
            settings,
            audioFramesArrived: sink.AudioSamplesReceived,
            audioFramesWrittenToSink: sink.AudioSamplesReceived,
            audioSamplesEncoded: sink.AudioSamplesReceived,
            audioDropEvents: SumNonNegative(sink.AudioDropsQueueSaturated, sink.AudioDropsBacklogEviction),
            avSyncDriftMs: null,
            avSyncDriftRateMsPerSec: null,
            encoderAvSyncDriftMs: encoderAvSyncDriftMs,
            encoderAvSyncCorrectionSamples: encoderAvSyncCorrectionSamples);
    }

    private RecordingAudioIntegrityCounterSnapshot CaptureRecordingAudioCounters(
        WasapiAudioCapture? capture,
        FlashbackEncoderSink sink,
        CaptureSettings? settings)
        => CreateRecordingAudioCounters(
            capture,
            settings,
            audioFramesArrived: sink.AudioSamplesReceived,
            audioFramesWrittenToSink: sink.AudioSamplesReceived,
            audioSamplesEncoded: sink.AudioSamplesReceived,
            audioDropEvents: SumNonNegative(sink.AudioDropsQueueSaturated, sink.AudioDropsBacklogEviction),
            avSyncDriftMs: null,
            avSyncDriftRateMsPerSec: null,
            encoderAvSyncDriftMs: null,
            encoderAvSyncCorrectionSamples: null);

    private RecordingAudioIntegrityCounterSnapshot CreateRecordingAudioCounters(
        WasapiAudioCapture? capture,
        CaptureSettings? settings,
        long audioFramesArrived,
        long audioFramesWrittenToSink,
        long audioSamplesEncoded,
        long audioDropEvents,
        double? avSyncDriftMs,
        double? avSyncDriftRateMsPerSec,
        double? encoderAvSyncDriftMs,
        long? encoderAvSyncCorrectionSamples)
    {
        var audioEnabled = settings?.AudioEnabled == true;
        if (!audioEnabled)
        {
            return RecordingAudioIntegrityCounterSnapshot.Disabled;
        }

        return new RecordingAudioIntegrityCounterSnapshot(
            AudioEnabled: true,
            AudioCaptureActive: capture?.IsCapturing == true,
            AudioFramesArrived: audioFramesArrived,
            AudioFramesWrittenToSink: audioFramesWrittenToSink,
            AudioSamplesEncoded: audioSamplesEncoded,
            AudioDropEvents: audioDropEvents,
            AudioDiscontinuities: capture?.AudioDataDiscontinuityCount ?? 0,
            AudioTimestampErrors: capture?.AudioTimestampErrorCount ?? 0,
            AudioCallbackGaps: capture?.CaptureCallbackSevereGapCount ?? 0,
            AvSyncDriftMs: avSyncDriftMs,
            AvSyncDriftRateMsPerSec: avSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs: encoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples: encoderAvSyncCorrectionSamples);
    }

    private static RecordingIntegritySummary BuildRecordingIntegritySummary(
        string backend,
        bool recordingActive,
        bool finalizeSucceeded,
        string finalizeStatus,
        DateTimeOffset? completedUtc,
        long sourceFrames,
        long acceptedFrames,
        RecordingIntegrityCounterSnapshot counters,
        RecordingAudioIntegrityCounterSnapshot? audioCounters = null,
        long recordingBoundaryRejectedFrames = 0,
        long recordingQueueRejectedFrames = 0)
    {
        audioCounters ??= RecordingAudioIntegrityCounterSnapshot.Disabled;
        var videoFields = BuildRecordingIntegritySummaryVideoFields(
            recordingActive,
            sourceFrames,
            acceptedFrames,
            counters,
            recordingBoundaryRejectedFrames,
            recordingQueueRejectedFrames);
        var audioFields = BuildRecordingIntegritySummaryAudioFields(audioCounters);
        var evaluation = EvaluateRecordingIntegritySummary(
            recordingActive,
            finalizeSucceeded,
            finalizeStatus,
            videoFields,
            audioFields);

        return new RecordingIntegritySummary
        {
            Status = evaluation.Status,
            Complete = !recordingActive && string.Equals(evaluation.Status, "Complete", StringComparison.Ordinal),
            Backend = backend,
            CompletedUtc = completedUtc,
            SourceFrames = videoFields.SourceFrames,
            AcceptedFrames = videoFields.AcceptedFrames,
            PipelineDroppedFrames = videoFields.PipelineDroppedFrames,
            QueueDroppedFrames = videoFields.QueueDroppedFrames,
            SubmittedFrames = videoFields.SubmittedFrames,
            EncodedFrames = videoFields.EncodedFrames,
            PacketsWritten = videoFields.PacketsWritten,
            EncoderDroppedFrames = videoFields.EncoderDroppedFrames,
            SequenceGaps = videoFields.SequenceGaps,
            QueueMaxDepth = videoFields.QueueMaxDepth,
            QueueOldestFrameAgeMs = videoFields.QueueOldestFrameAgeMs,
            BackpressureWaitMs = videoFields.BackpressureWaitMs,
            BackpressureEvents = videoFields.BackpressureEvents,
            BackpressureMaxWaitMs = videoFields.BackpressureMaxWaitMs,
            AudioStatus = evaluation.AudioStatus,
            AudioEnabled = audioFields.AudioEnabled,
            AudioCaptureActive = audioFields.AudioCaptureActive,
            AudioFramesArrived = audioFields.AudioFramesArrived,
            AudioFramesWrittenToSink = audioFields.AudioFramesWrittenToSink,
            AudioSamplesEncoded = audioFields.AudioSamplesEncoded,
            AudioDropEvents = audioFields.AudioDropEvents,
            AudioDiscontinuities = audioFields.AudioDiscontinuities,
            AudioTimestampErrors = audioFields.AudioTimestampErrors,
            AudioCallbackGaps = audioFields.AudioCallbackGaps,
            AvSyncDriftMs = audioFields.AvSyncDriftMs,
            AvSyncDriftRateMsPerSec = audioFields.AvSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs = audioFields.EncoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples = audioFields.EncoderAvSyncCorrectionSamples,
            Reason = evaluation.Reason
        };
    }

    private static RecordingIntegritySummaryVideoFields BuildRecordingIntegritySummaryVideoFields(
        bool recordingActive,
        long sourceFrames,
        long acceptedFrames,
        RecordingIntegrityCounterSnapshot counters,
        long recordingBoundaryRejectedFrames,
        long recordingQueueRejectedFrames)
    {
        var normalizedSourceFrames = Math.Max(0, sourceFrames);
        var normalizedAcceptedFrames = Math.Max(0, acceptedFrames);
        var normalizedBoundaryRejectedFrames = Math.Max(0, recordingBoundaryRejectedFrames);
        var normalizedQueueRejectedFrames = Math.Min(
            normalizedBoundaryRejectedFrames,
            Math.Max(0, recordingQueueRejectedFrames));
        var rawPipelineDroppedFrames = Math.Max(0, normalizedSourceFrames - normalizedAcceptedFrames);
        var activePipelineDroppedFrames = recordingActive
            ? Math.Max(normalizedBoundaryRejectedFrames, rawPipelineDroppedFrames - 1)
            : rawPipelineDroppedFrames;

        return new RecordingIntegritySummaryVideoFields
        {
            SourceFrames = normalizedSourceFrames,
            AcceptedFrames = normalizedAcceptedFrames,
            PipelineDroppedFrames = activePipelineDroppedFrames,
            RecordingBoundaryRejectedFrames = normalizedBoundaryRejectedFrames,
            RecordingQueueRejectedFrames = normalizedQueueRejectedFrames,
            QueueDroppedFrames = Math.Max(0, counters.QueueDroppedFrames),
            SubmittedFrames = Math.Max(0, counters.SubmittedFrames),
            EncodedFrames = Math.Max(0, counters.EncodedFrames),
            PacketsWritten = Math.Max(0, counters.PacketsWritten),
            EncoderDroppedFrames = Math.Max(0, counters.EncoderDroppedFrames),
            SequenceGaps = Math.Max(0, counters.SequenceGaps),
            QueueMaxDepth = Math.Max(0, counters.QueueMaxDepth),
            QueueOldestFrameAgeMs = Math.Max(0, counters.QueueOldestFrameAgeMs),
            BackpressureWaitMs = Math.Max(0, counters.BackpressureWaitMs),
            BackpressureEvents = Math.Max(0, counters.BackpressureEvents),
            BackpressureMaxWaitMs = Math.Max(0, counters.BackpressureMaxWaitMs),
            EncodingFailed = counters.EncodingFailed,
            EncodingFailureType = counters.EncodingFailureType,
            EncodingFailureMessage = counters.EncodingFailureMessage
        };
    }

    private static RecordingIntegritySummaryAudioFields BuildRecordingIntegritySummaryAudioFields(
        RecordingAudioIntegrityCounterSnapshot audioCounters)
        => new()
        {
            AudioEnabled = audioCounters.AudioEnabled,
            AudioCaptureActive = audioCounters.AudioCaptureActive,
            AudioFramesArrived = Math.Max(0, audioCounters.AudioFramesArrived),
            AudioFramesWrittenToSink = Math.Max(0, audioCounters.AudioFramesWrittenToSink),
            AudioSamplesEncoded = Math.Max(0, audioCounters.AudioSamplesEncoded),
            AudioDropEvents = Math.Max(0, audioCounters.AudioDropEvents),
            AudioDiscontinuities = Math.Max(0, audioCounters.AudioDiscontinuities),
            AudioTimestampErrors = Math.Max(0, audioCounters.AudioTimestampErrors),
            AudioCallbackGaps = Math.Max(0, audioCounters.AudioCallbackGaps),
            AvSyncDriftMs = audioCounters.AvSyncDriftMs,
            AvSyncDriftRateMsPerSec = audioCounters.AvSyncDriftRateMsPerSec,
            EncoderAvSyncDriftMs = audioCounters.EncoderAvSyncDriftMs,
            EncoderAvSyncCorrectionSamples = audioCounters.EncoderAvSyncCorrectionSamples
        };

    private static RecordingIntegritySummaryEvaluation EvaluateRecordingIntegritySummary(
        bool recordingActive,
        bool finalizeSucceeded,
        string finalizeStatus,
        RecordingIntegritySummaryVideoFields videoFields,
        RecordingIntegritySummaryAudioFields audioFields)
    {
        var reasons = new List<string>();
        if (!recordingActive && !finalizeSucceeded)
        {
            reasons.Add($"finalize='{finalizeStatus}'");
        }

        if (videoFields.EncodingFailed)
        {
            var failure = string.IsNullOrWhiteSpace(videoFields.EncodingFailureMessage)
                ? videoFields.EncodingFailureType ?? "unknown"
                : $"{videoFields.EncodingFailureType ?? "unknown"}: {videoFields.EncodingFailureMessage}";
            reasons.Add($"encoding={failure}");
        }

        if (videoFields.PipelineDroppedFrames > 0)
        {
            reasons.Add($"pipeline_drops={videoFields.PipelineDroppedFrames}");
        }

        if (videoFields.QueueDroppedFrames > 0)
        {
            reasons.Add($"queue_drops={videoFields.QueueDroppedFrames}");
        }

        if (videoFields.RecordingQueueRejectedFrames > 0)
        {
            reasons.Add($"queue_rejections={videoFields.RecordingQueueRejectedFrames}");
        }

        if (videoFields.RecordingBoundaryRejectedFrames > videoFields.RecordingQueueRejectedFrames)
        {
            reasons.Add($"recording_boundary_rejections={videoFields.RecordingBoundaryRejectedFrames}");
        }

        if (videoFields.EncoderDroppedFrames > 0)
        {
            reasons.Add($"encoder_drops={videoFields.EncoderDroppedFrames}");
        }

        if (videoFields.SequenceGaps > 0)
        {
            reasons.Add($"sequence_gaps={videoFields.SequenceGaps}");
        }

        var audioStatus = EvaluateRecordingIntegrityAudioStatus(audioFields, reasons);
        var status = reasons.Count > 0
            ? (videoFields.EncodingFailed ||
               (!recordingActive && !finalizeSucceeded) ||
               string.Equals(audioStatus, "Failed", StringComparison.Ordinal)
                ? "Failed"
                : "Incomplete")
            : recordingActive ? "Active" : "Complete";
        var reason = reasons.Count > 0
            ? string.Join("; ", reasons)
            : recordingActive
                ? "Recording active; all delivered source frames have reached the recording boundary so far."
                : "Every delivered source frame reached the recording boundary.";

        return new RecordingIntegritySummaryEvaluation(status, audioStatus, reason);
    }

    private static string EvaluateRecordingIntegrityAudioStatus(
        RecordingIntegritySummaryAudioFields audioFields,
        List<string> reasons)
    {
        if (!audioFields.AudioEnabled)
        {
            return "Disabled";
        }

        var audioFailed = false;
        var audioIncomplete = false;
        if (!audioFields.AudioCaptureActive)
        {
            audioFailed = true;
            reasons.Add("audio_inactive");
        }

        if (audioFields.AudioFramesArrived <= 0)
        {
            audioFailed = true;
            reasons.Add("audio_no_frames");
        }

        var audioBoundaryDropFrames = audioFields.AudioFramesArrived > audioFields.AudioFramesWrittenToSink
            ? audioFields.AudioFramesArrived - audioFields.AudioFramesWrittenToSink
            : 0;
        if (audioBoundaryDropFrames > RecordingIntegrityAudioBoundaryToleranceFrames)
        {
            audioIncomplete = true;
            reasons.Add($"audio_boundary_drops={audioBoundaryDropFrames}");
        }

        if (audioFields.AudioSamplesEncoded <= 0)
        {
            audioFailed = true;
            reasons.Add("audio_sink_no_samples");
        }

        if (audioFields.AudioDropEvents > 0)
        {
            audioIncomplete = true;
            reasons.Add($"audio_drops={audioFields.AudioDropEvents}");
        }

        if (audioFields.AudioDiscontinuities > 0)
        {
            audioIncomplete = true;
            reasons.Add($"audio_discontinuities={audioFields.AudioDiscontinuities}");
        }

        if (audioFields.AudioTimestampErrors > 0)
        {
            audioIncomplete = true;
            reasons.Add($"audio_timestamp_errors={audioFields.AudioTimestampErrors}");
        }

        if (audioFields.AudioCallbackGaps > 0)
        {
            audioIncomplete = true;
            reasons.Add($"audio_callback_gaps={audioFields.AudioCallbackGaps}");
        }

        if (audioFields.AvSyncDriftMs is { } captureDriftMs &&
            Math.Abs(captureDriftMs) > RecordingIntegrityAvSyncDriftWarningMs)
        {
            audioIncomplete = true;
            reasons.Add($"av_sync_drift_ms={FormatRecordingIntegrityDouble(captureDriftMs)}");
        }

        if (audioFields.EncoderAvSyncDriftMs is { } encoderDriftMs &&
            Math.Abs(encoderDriftMs) > RecordingIntegrityAvSyncDriftWarningMs)
        {
            audioIncomplete = true;
            reasons.Add($"encoder_av_sync_drift_ms={FormatRecordingIntegrityDouble(encoderDriftMs)}");
        }

        return audioFailed ? "Failed" : audioIncomplete ? "Incomplete" : "Clean";
    }

    private static string FormatRecordingIntegrityDouble(double value)
        => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static void LogRecordingIntegritySummary(RecordingIntegritySummary summary)
    {
        Logger.Log(
            "RECORDING_INTEGRITY " +
            $"status={summary.Status} " +
            $"complete={summary.Complete} " +
            $"backend={summary.Backend} " +
            $"source_frames={summary.SourceFrames} " +
            $"accepted_frames={summary.AcceptedFrames} " +
            $"pipeline_drops={summary.PipelineDroppedFrames} " +
            $"queue_drops={summary.QueueDroppedFrames} " +
            $"submitted_frames={summary.SubmittedFrames} " +
            $"encoded_frames={summary.EncodedFrames} " +
            $"packets_written={summary.PacketsWritten} " +
            $"encoder_drops={summary.EncoderDroppedFrames} " +
            $"sequence_gaps={summary.SequenceGaps} " +
            $"queue_max_depth={summary.QueueMaxDepth} " +
            $"queue_oldest_age_ms={summary.QueueOldestFrameAgeMs} " +
            $"backpressure_wait_ms={summary.BackpressureWaitMs} " +
            $"backpressure_events={summary.BackpressureEvents} " +
            $"backpressure_max_wait_ms={summary.BackpressureMaxWaitMs} " +
            $"audio_status={summary.AudioStatus} " +
            $"audio_enabled={summary.AudioEnabled} " +
            $"audio_active={summary.AudioCaptureActive} " +
            $"audio_arrived={summary.AudioFramesArrived} " +
            $"audio_written={summary.AudioFramesWrittenToSink} " +
            $"audio_encoded={summary.AudioSamplesEncoded} " +
            $"audio_drops={summary.AudioDropEvents} " +
            $"audio_discontinuities={summary.AudioDiscontinuities} " +
            $"audio_timestamp_errors={summary.AudioTimestampErrors} " +
            $"audio_callback_gaps={summary.AudioCallbackGaps} " +
            $"av_drift_ms={summary.AvSyncDriftMs?.ToString("0.###", CultureInfo.InvariantCulture) ?? "N/A"} " +
            $"encoder_av_drift_ms={summary.EncoderAvSyncDriftMs?.ToString("0.###", CultureInfo.InvariantCulture) ?? "N/A"} " +
            $"reason='{summary.Reason.Replace("'", "\\'", StringComparison.Ordinal)}'");
    }

    private static long DeltaCounter(long current, long baseline)
        => current >= baseline ? current - baseline : current;

    private static long SumNonNegative(long a, long b)
        => (a > 0 ? a : 0) + (b > 0 ? b : 0);

    private static long SumNonNegative(long a, long b, long c)
        => (a > 0 ? a : 0) + (b > 0 ? b : 0) + (c > 0 ? c : 0);

    private async Task CompleteLibAvRecordingFinalizeStateAsync()
    {
        _recordingStopwatch.Stop();
        _isRecording = false;
        if (!_isVideoPreviewActive) await StopSourceTelemetryPollingAsync().ConfigureAwait(false);
        _recordingBackend.ClearContextAndSettings();
        _mfConvertersDisabled = false;
    }

    private async Task<LibAvVideoBoundaryStopResult> StopUnifiedVideoRecordingForLibAvFinalizeAsync(
        FinalizeResult result,
        string fallbackOutputPath,
        CancellationToken cancellationToken)
    {
        OperationCanceledException? cancellationException = null;
        var unifiedVideoCapture = _videoPipeline.Capture;
        var recordingFramesDeliveredToBoundary = 0L;
        var recordingFramesAcceptedByBoundary = 0L;
        var recordingFramesRejectedByBoundary = 0L;
        var recordingQueueRejectedByBoundary = 0L;
        if (unifiedVideoCapture != null)
        {
            try
            {
                await unifiedVideoCapture.StopRecordingAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancellationException = new OperationCanceledException(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Log($"Unified video recording stop failed: {ex.Message}");
                if (cancellationException == null && result.Succeeded)
                {
                    result = FinalizeResult.Failure(
                        fallbackOutputPath,
                        $"Unified video recording stop failed: {ex.Message}",
                        null,
                        RecordingFailureCodes.UnifiedStopFailed);
                }
            }
            finally
            {
                // Keep SkipCpuReadback=true - preview uses GPU textures, not CPU bytes.
                // Lock2D is never needed while D3D shared device is active.
            }

            _lastMfSourceReaderFramesDelivered = unifiedVideoCapture.VideoFramesArrived;
            _lastMfSourceReaderFramesDropped = unifiedVideoCapture.VideoFramesDropped;
            _lastMfSourceReaderNegotiatedFormat = unifiedVideoCapture.NegotiatedFormat;
            recordingFramesDeliveredToBoundary = unifiedVideoCapture.RecordingFramesDelivered;
            recordingFramesAcceptedByBoundary = unifiedVideoCapture.VideoFramesWrittenToSink;
            recordingFramesRejectedByBoundary = unifiedVideoCapture.RecordingFramesRejected;
            recordingQueueRejectedByBoundary = unifiedVideoCapture.RecordingQueueRejectedFrames;
            Logger.Log(
                "VIDEO_DIAG mf_source_reader " +
                $"frames_delivered={_lastMfSourceReaderFramesDelivered} " +
                $"frames_dropped={_lastMfSourceReaderFramesDropped} " +
                $"negotiated_format='{_lastMfSourceReaderNegotiatedFormat ?? "unknown"}'");
            Logger.Log(
                "VIDEO_DIAG recording_pipeline " +
                $"source_frames_during_recording={recordingFramesDeliveredToBoundary} " +
                $"frames_enqueued_to_encoder={recordingFramesAcceptedByBoundary} " +
                $"frames_rejected_by_boundary={recordingFramesRejectedByBoundary} " +
                $"queue_rejections={recordingQueueRejectedByBoundary} " +
                $"pipeline_drops={recordingFramesDeliveredToBoundary - recordingFramesAcceptedByBoundary}");
        }

        return new LibAvVideoBoundaryStopResult(
            result,
            cancellationException,
            recordingFramesDeliveredToBoundary,
            recordingFramesAcceptedByBoundary,
            recordingFramesRejectedByBoundary,
            recordingQueueRejectedByBoundary);
    }

    private async Task DetachLibAvRecordingAudioBeforeSinkStopAsync()
    {
        if (_previewAudioGraph.ProgramCapture != null)
        {
            try
            {
                _previewAudioGraph.ProgramCapture.DetachRecordingSink();
            }
            catch (Exception ex)
            {
                Logger.Log($"Audio recording sink detach failed: {ex.Message}");
            }
        }

        _recordingMicrophoneDiscontinuitiesFinal =
            _previewAudioGraph.MicrophoneCapture?.AudioDataDiscontinuityCount ??
            _recordingMicrophoneDiscontinuitiesBaseline;
        await DisposeMicrophoneCaptureAsync().ConfigureAwait(false);
    }

    private async Task<LibAvFinalizeStepResult> StopAndDisposeLibAvSinkForFinalizeAsync(
        IRecordingSink? sink,
        LibAvRecordingSink? libAvSink,
        FinalizeResult result,
        string fallbackOutputPath,
        bool emergency,
        OperationCanceledException? cancellationException,
        CancellationToken cancellationToken)
    {
        if (sink == null)
        {
            return new LibAvFinalizeStepResult(result, cancellationException);
        }

        try
        {
            // LibAv accepts the emergency flag to shorten its finalization wait.
            // Other sink implementations use the standard IRecordingSink stop path.
            var sinkResult = libAvSink != null
                ? await libAvSink.StopAsync(emergency, cancellationToken).ConfigureAwait(false)
                : await sink.StopAsync(cancellationToken).ConfigureAwait(false);
            if (result.Succeeded)
            {
                result = sinkResult;
            }
            else
            {
                var priorResult = result;
                var mergedArtifacts = new List<string>();
                AddUniqueArtifacts(mergedArtifacts, priorResult.PreservedArtifacts);
                AddUniqueArtifacts(mergedArtifacts, sinkResult.PreservedArtifacts);
                result = FinalizeResult.Failure(
                    priorResult.OutputPath,
                    priorResult.StatusMessage,
                    mergedArtifacts,
                    priorResult.FailureCode ?? sinkResult.FailureCode,
                    priorResult.CleanupPending || sinkResult.CleanupPending,
                    priorResult.RecoveryPath ?? sinkResult.RecoveryPath,
                    priorResult.VerificationCompleted || sinkResult.VerificationCompleted,
                    Math.Max(priorResult.FinalizationElapsedMs, sinkResult.FinalizationElapsedMs));

                result = MergeFinalizeTrackEvidence(result, priorResult);
                result = MergeFinalizeTrackEvidence(result, sinkResult);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancellationException = new OperationCanceledException(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.Log($"Recording sink stop failed: {ex.Message}");
            if (result.Succeeded)
            {
                result = FinalizeResult.Failure(
                    fallbackOutputPath,
                    $"Recording stop failed: {ex.Message}",
                    null,
                    RecordingFailureCodes.StopFailed);
            }
        }
        finally
        {
            try
            {
                await sink.DisposeAsync().ConfigureAwait(false);
                if (libAvSink != null)
                {
                    var libAvCleanupTask = libAvSink.CleanupCompletionTask;
                    if (!libAvCleanupTask.IsCompleted)
                    {
                        SetPendingLibAvCleanupTask(libAvCleanupTask, "LibAv");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Recording sink dispose failed: {ex.Message}");
                if (cancellationException == null && result.Succeeded)
                {
                    result = result.AsFailure($"Recording dispose failed: {ex.Message}", RecordingFailureCodes.SinkDisposeFailed);
                }
            }
        }

        if (!result.Succeeded &&
            result.CleanupPending &&
            (libAvSink?.CleanupCompletionTask.IsCompleted ?? true))
        {
            result = FinalizeResult.Failure(
                result.OutputPath,
                result.StatusMessage,
                result.PreservedArtifacts,
                result.FailureCode,
                cleanupPending: false,
                recoveryPath: result.RecoveryPath,
                verificationCompleted: result.VerificationCompleted,
                finalizationElapsedMs: result.FinalizationElapsedMs)
                .WithTrackEvidence(result.RequestedTracks, result.ObservedTracks);
        }

        return new LibAvFinalizeStepResult(result, cancellationException);
    }

    private async Task<LibAvFinalizeStepResult> DisposeIdleLibAvPreviewResourcesAfterRecordingAsync(
        FinalizeResult result,
        string fallbackOutputPath,
        OperationCanceledException? cancellationException,
        bool emergency)
    {
        if (_isVideoPreviewActive)
        {
            return new LibAvFinalizeStepResult(result, cancellationException);
        }

        await StopPreviewRendererBeforeCaptureCleanupAsync(CancellationToken.None).ConfigureAwait(false);
        var unifiedVideoCapture = _videoPipeline.TakeCapture();
        if (unifiedVideoCapture != null)
        {
            try
            {
                CacheMjpegTimingMetrics(unifiedVideoCapture);
                DetachUnifiedVideoCapture(unifiedVideoCapture);
                if (_recordingBackend.PendingLibAvDrainTask is { IsCompleted: false } pendingLibAvDrainTask)
                {
                    var captureCleanupTask = _videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(
                        pendingLibAvDrainTask,
                        unifiedVideoCapture,
                        reason: "recording_stop_deferred_drain");
                    SetPendingLibAvCleanupTask(
                        Task.WhenAll(pendingLibAvDrainTask, captureCleanupTask),
                        "LibAv+UnifiedVideoCapture");
                }
                else
                {
                    var captureCleanupTask = StopAndDisposeUnifiedVideoCaptureAsync(unifiedVideoCapture);
                    var absoluteBudgetMs = emergency ? 5_000L : 120_000L;
                    var remainingBudgetMs = Math.Max(
                        0,
                        absoluteBudgetMs - Math.Max(0, result.FinalizationElapsedMs));
                    var cleanupWaitMs = Math.Min(5_000L, remainingBudgetMs);
                    var cleanupCompleted = cleanupWaitMs > 0 &&
                        ReferenceEquals(
                            await Task.WhenAny(
                                captureCleanupTask,
                                Task.Delay((int)cleanupWaitMs)).ConfigureAwait(false),
                            captureCleanupTask);
                    if (cleanupCompleted)
                    {
                        await captureCleanupTask.ConfigureAwait(false);
                    }
                    else
                    {
                        SetPendingLibAvCleanupTask(captureCleanupTask, "LibAv+UnifiedVideoCapture");
                        result = FinalizeResult.Failure(
                            fallbackOutputPath,
                            "Recording failed because the video capture worker did not stop within the finalization deadline; cleanup continues in quarantine.",
                            result.PreservedArtifacts,
                            RecordingFailureCodes.VideoCaptureCleanupTimeout,
                            cleanupPending: true,
                            recoveryPath: result.RecoveryPath,
                            verificationCompleted: result.VerificationCompleted,
                            finalizationElapsedMs: Math.Max(result.FinalizationElapsedMs, absoluteBudgetMs))
                            .WithTrackEvidence(result.RequestedTracks, result.ObservedTracks);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Unified video capture dispose failed: {ex.Message}");
                if (cancellationException == null && result.Succeeded)
                {
                    result = result.AsFailure($"Unified video capture dispose failed: {ex.Message}", RecordingFailureCodes.VideoCaptureDisposeFailed);
                }
            }
        }

        var capture = _previewAudioGraph.ProgramCapture;
        _previewAudioGraph.ProgramCapture = null;
        _previewAudioGraph.DetachCapture(
            capture,
            OnWasapiAudioLevelUpdated,
            OnWasapiCaptureFailed,
            _flashbackBackend.PlaybackController);
        if (capture != null)
        {
            try
            {
                var programAudioCleanupTask = capture.DisposeAsync().AsTask();
                var audioCleanupCompleted = !emergency;
                if (emergency)
                {
                    var remainingEmergencyMs = Math.Max(
                        0,
                        5_000L - Math.Max(0, result.FinalizationElapsedMs));
                    audioCleanupCompleted = remainingEmergencyMs > 0 &&
                        ReferenceEquals(
                            await Task.WhenAny(
                                programAudioCleanupTask,
                                Task.Delay((int)remainingEmergencyMs)).ConfigureAwait(false),
                            programAudioCleanupTask);
                }

                if (audioCleanupCompleted)
                {
                    await programAudioCleanupTask.ConfigureAwait(false);
                }
                else
                {
                    var priorCleanupTask = _recordingBackend.PendingLibAvDrainTask;
                    var combinedCleanupTask = priorCleanupTask is { IsCompleted: false }
                        ? Task.WhenAll(priorCleanupTask, programAudioCleanupTask)
                        : programAudioCleanupTask;
                    SetPendingLibAvCleanupTask(combinedCleanupTask, "LibAv+ProgramAudio");
                    result = FinalizeResult.Failure(
                        fallbackOutputPath,
                        "Recording failed because the program-audio worker did not stop within the emergency deadline; cleanup continues in quarantine.",
                        result.PreservedArtifacts,
                        RecordingFailureCodes.ProgramAudioCleanupTimeout,
                        cleanupPending: true,
                        recoveryPath: result.RecoveryPath,
                        verificationCompleted: result.VerificationCompleted,
                        finalizationElapsedMs: Math.Max(result.FinalizationElapsedMs, 5_000))
                        .WithTrackEvidence(result.RequestedTracks, result.ObservedTracks);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Recording WASAPI capture dispose failed: {ex.Message}");
                if (cancellationException == null && result.Succeeded)
                {
                    result = result.AsFailure($"Recording WASAPI capture dispose failed: {ex.Message}", RecordingFailureCodes.ProgramAudioDisposeFailed);
                }
            }
        }

        return new LibAvFinalizeStepResult(result, cancellationException);
    }

    private static async Task StopAndDisposeUnifiedVideoCaptureAsync(UnifiedVideoCapture capture)
    {
        await capture.StopAsync().ConfigureAwait(false);
        await capture.DisposeAsync().ConfigureAwait(false);
    }

    private async Task<OperationCanceledException?> RestoreLibAvPreviewFeaturesAfterRecordingAsync(
        OperationCanceledException? cancellationException,
        CancellationToken cancellationToken)
    {
        cancellationException = await RestorePendingFlashbackEnableAfterLibAvRecordingAsync(
            cancellationException,
            cancellationToken).ConfigureAwait(false);

        return await RestartStandardMicrophoneMonitorAfterLibAvRecordingAsync(
            cancellationException,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<OperationCanceledException?> RestorePendingFlashbackEnableAfterLibAvRecordingAsync(
        OperationCanceledException? cancellationException,
        CancellationToken cancellationToken)
    {
        if (!_pendingFlashbackEnableAfterRecording)
        {
            return cancellationException;
        }

        _pendingFlashbackEnableAfterRecording = false;
        var unifiedVideoCapture = _videoPipeline.Capture;
        var settings = _currentSettings;
        if (_flashbackEnabled && _isVideoPreviewActive && unifiedVideoCapture != null && settings != null)
        {
            try
            {
                await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancellationException ??= new OperationCanceledException(cancellationToken);
                _flashbackEnabled = false;
                _pendingFlashbackEnableAfterRecording = false;
                if (_flashbackBackend.HasAnyResource)
                {
                    await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true).ConfigureAwait(false);
                }

                Logger.Log("FLASHBACK_ENABLE_AFTER_RECORDING_CANCELLED");
            }
            catch (Exception ex)
            {
                _flashbackEnabled = false;
                _pendingFlashbackEnableAfterRecording = false;
                if (_flashbackBackend.HasAnyResource)
                {
                    await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true).ConfigureAwait(false);
                }

                Logger.Log($"FLASHBACK_ENABLE_AFTER_RECORDING_FAIL type={ex.GetType().Name} error='{ex.Message}'");
            }
        }

        return cancellationException;
    }

    private async Task<OperationCanceledException?> RestartStandardMicrophoneMonitorAfterLibAvRecordingAsync(
        OperationCanceledException? cancellationException,
        CancellationToken cancellationToken)
    {
        try
        {
            await RestartMicrophoneMonitorAfterRecordingAsync(
                new MicrophoneMonitorRestartOptions(
                    OnlyWhenMissing: false,
                    FlashbackAttachReason: "mic_monitor_restart",
                    RestartLogEvent: "MIC_MONITOR_RESTART",
                    DisposeWarningEvent: "MIC_MONITOR_RESTART_DISPOSE_WARN"),
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancellationException ??= new OperationCanceledException(cancellationToken);
        }
        catch (Exception micEx)
        {
            Logger.Log("Mic monitor restart failed (non-fatal): " + micEx.Message);
        }

        return cancellationException;
    }

    private readonly record struct LibAvFinalizeStepResult(
        FinalizeResult Result,
        OperationCanceledException? CancellationException);

    private readonly record struct LibAvVideoBoundaryStopResult(
        FinalizeResult Result,
        OperationCanceledException? CancellationException,
        long RecordingFramesDeliveredToBoundary,
        long RecordingFramesAcceptedByBoundary,
        long RecordingFramesRejectedByBoundary,
        long RecordingQueueRejectedByBoundary);

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
        RecordingFinalizationRecoveryArtifacts.RetireActive(
            Interlocked.Exchange(ref _activeRecordingRecoveryJournalPath, null));
        _recordingIntegrityCounterBaseline = null;
        _recordingIntegrityAudioBaseline = null;
        _isRecording = false;
        Volatile.Write(ref _recordingFaultAttributionActive, 0);
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
                var libAvCleanupTask = libAvSink.CleanupCompletionTask;
                if (!libAvCleanupTask.IsCompleted)
                {
                    var captureCleanupTask = _videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(
                        libAvCleanupTask,
                        unifiedVideoCapture,
                        reason: "recording_start_rollback");
                    SetPendingLibAvCleanupTask(
                        Task.WhenAll(libAvCleanupTask, captureCleanupTask),
                        "LibAvRollback+UnifiedVideoCapture");
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
