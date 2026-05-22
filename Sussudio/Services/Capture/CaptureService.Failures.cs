using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Capture;

// Fatal capture, recording, and Flashback backend failure handling callbacks.
// This file owns last-failure telemetry and fatal capture cleanup launch.
public partial class CaptureService
{
    private readonly object _recordingFailureTelemetryLock = new();
    private bool _lastRecordingEncodingFailed;
    private string? _lastRecordingEncodingFailureType;
    private string? _lastRecordingEncodingFailureMessage;
    private bool _lastFlashbackEncodingFailed;
    private string? _lastFlashbackEncodingFailureType;
    private string? _lastFlashbackEncodingFailureMessage;

    private void OnUnifiedVideoCaptureFatalError(object? sender, Exception ex)
    {
        Logger.Log($"UNIFIED_VIDEO_CAPTURE_FATAL type={ex.GetType().Name} msg={ex.Message}");
        if (_isRecording)
        {
            RecordLastRecordingFailure(ex);
        }

        if (_flashbackBackend.Sink != null)
        {
            RecordLastFlashbackFailure(ex);
        }

        BeginFatalCaptureCleanup(ex);
    }

    private void OnRecordingBackendFatalError(Exception ex)
    {
        Logger.Log($"RECORDING_BACKEND_FATAL type={ex.GetType().Name} msg={ex.Message}");
        if (_isRecording)
        {
            RecordLastRecordingFailure(ex);
        }

        BeginFatalCaptureCleanup(ex);
    }

    private void OnFlashbackBackendFatalError(Exception ex)
    {
        Logger.Log($"FLASHBACK_BACKEND_FATAL type={ex.GetType().Name} msg={ex.Message}");
        var flashbackIsRecordingBackend = IsFlashbackRecordingBackendOwnedByRecording();
        if (flashbackIsRecordingBackend)
        {
            RecordLastRecordingFailure(ex);
        }

        if (_flashbackBackend.Sink != null)
        {
            RecordLastFlashbackFailure(ex);
        }

        if (flashbackIsRecordingBackend)
        {
            BeginFatalCaptureCleanup(ex);
            return;
        }

        BeginFlashbackBackendCleanup(ex);
    }

    private void BeginFatalCaptureCleanup(Exception ex)
    {
        if (Interlocked.Exchange(ref _fatalCleanupInProgress, 1) != 0)
        {
            return;
        }

        var generationAtFault = CurrentSessionGeneration;

        _ = Task.Run(async () =>
        {
            try
            {
                await _sessionTransitionLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
                try
                {
                    if (CurrentSessionGeneration != generationAtFault)
                    {
                        Logger.Log("FATAL_CLEANUP_SKIP_STALE reason='session_generation_changed_before_cleanup'");
                        return;
                    }

                    EnterCleanupState();

                    // Stop the preview renderer before disposing the shared D3D11
                    // device. Same race as the reinit crash: the renderer may be
                    // calling VideoProcessorBlt/Present on the shared device when
                    // cleanup disposes it.
                    try { PreCleanupRequested?.Invoke(); }
                    catch (Exception preEx) { Logger.Log($"PreCleanupRequested handler warning: {preEx.Message}"); }

                    await CleanupCoreAsync(CancellationToken.None).ConfigureAwait(false);
                    EnterFaultedState();
                    StatusChanged?.Invoke(this, $"Video capture error: {ex.Message}");
                    ErrorOccurred?.Invoke(this, ex);
                }
                finally
                {
                    ReleaseSemaphoreBestEffort(_sessionTransitionLock, "fatal_capture_cleanup");
                }
            }
            catch (Exception cleanupEx)
            {
                Logger.Log($"Fatal capture cleanup warning: {cleanupEx.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _fatalCleanupInProgress, 0);
            }
        });
    }

    private void RecordLastRecordingFailure(Exception ex)
    {
        lock (_recordingFailureTelemetryLock)
        {
            _lastRecordingEncodingFailed = true;
            _lastRecordingEncodingFailureType = ex.GetType().Name;
            _lastRecordingEncodingFailureMessage = ex.Message;
        }
    }

    private void RecordLastFlashbackFailure(Exception ex)
    {
        lock (_recordingFailureTelemetryLock)
        {
            _lastFlashbackEncodingFailed = true;
            _lastFlashbackEncodingFailureType = ex.GetType().Name;
            _lastFlashbackEncodingFailureMessage = ex.Message;
        }
    }

    private void ClearLastRecordingFailure()
    {
        lock (_recordingFailureTelemetryLock)
        {
            _lastRecordingEncodingFailed = false;
            _lastRecordingEncodingFailureType = null;
            _lastRecordingEncodingFailureMessage = null;
        }
    }

    private void ClearLastFlashbackFailure()
    {
        lock (_recordingFailureTelemetryLock)
        {
            _lastFlashbackEncodingFailed = false;
            _lastFlashbackEncodingFailureType = null;
            _lastFlashbackEncodingFailureMessage = null;
        }
    }

    private (
        bool RecordingFailed,
        string? RecordingFailureType,
        string? RecordingFailureMessage,
        bool FlashbackFailed,
        string? FlashbackFailureType,
        string? FlashbackFailureMessage) GetLastFailureTelemetry()
    {
        lock (_recordingFailureTelemetryLock)
        {
            return (
                _lastRecordingEncodingFailed,
                _lastRecordingEncodingFailureType,
                _lastRecordingEncodingFailureMessage,
                _lastFlashbackEncodingFailed,
                _lastFlashbackEncodingFailureType,
                _lastFlashbackEncodingFailureMessage);
        }
    }
}
