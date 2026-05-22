using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.Services.Capture;

// Flashback backend failure cleanup ownership. This path preserves recoverable
// rolling-buffer files on GPU device loss and disposes only the Flashback
// backend; fatal capture state changes route through TransitionExecution helpers.
public partial class CaptureService
{
    private void BeginFlashbackBackendCleanup(Exception ex)
    {
        // Centralised TDR guard: if the triggering exception is a GPU device-loss
        // (DXGI_ERROR_DEVICE_REMOVED / _HUNG / _RESET), preserve flashback segments
        // BEFORE entering the async cleanup task. This mirrors the sibling pattern at
        // buffer_cycle_failed / recording_finalize_failed, which both call
        // PreserveRecoverySegments before invoking BeginFlashbackBackendCleanup.
        // PreserveRecoverySegments is idempotent (sets a bool flag), so the
        // double-call from buffer_cycle_failed is harmless. The flag causes
        // ResolveSegmentPurge (inside DisposeFlashbackPreviewBackendAsync) to
        // short-circuit the purge regardless of the purgeSegments argument, and
        // MarkSessionPreservedForRecovery suppresses Directory.Delete in
        // FlashbackBufferManager.Dispose so segment files survive on disk.
        if (IsGpuDeviceLost(ex))
        {
            _flashbackBackend.PreserveRecoverySegments("device_lost");
            Logger.Log($"FLASHBACK_BACKEND_FATAL_DEVICE_LOST type={ex.GetType().Name} preserving_segments=true");
        }

        if (Volatile.Read(ref _fatalCleanupInProgress) != 0 ||
            Interlocked.Exchange(ref _flashbackCleanupInProgress, 1) != 0)
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
                        Logger.Log("FLASHBACK_FATAL_CLEANUP_SKIP_STALE reason='session_generation_changed_before_cleanup'");
                        return;
                    }

                    var preserveDedicatedRecordingMic = _isRecording && !IsFlashbackRecordingBackendActive();
                    await DisposeFlashbackPreviewBackendAsync(
                        CancellationToken.None,
                        purgeSegments: true,
                        detachMicrophoneWriter: !preserveDedicatedRecordingMic).ConfigureAwait(false);

                    StatusChanged?.Invoke(this, $"Flashback error: {ex.Message}");
                }
                finally
                {
                    ReleaseSemaphoreBestEffort(_sessionTransitionLock, "flashback_backend_cleanup");
                }
            }
            catch (Exception cleanupEx)
            {
                Logger.Log($"Flashback backend cleanup warning: {cleanupEx.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _flashbackCleanupInProgress, 0);
            }
        });
    }

    /// <summary>
    /// Returns true when <paramref name="ex"/> represents a GPU device-removed /
    /// hung / reset condition (TDR). In these cases the flashback rolling buffer
    /// (CPU-resident file data, independent of GPU state) is intact and must NOT
    /// be purged during backend cleanup.
    ///
    /// Covers one level of <see cref="AggregateException"/> unwrap, matching
    /// <c>App.IsRecoverableUnhandled</c>. Deeper unwrap is a separate policy
    /// decision tracked as a deferred follow-up so both classifiers move together.
    ///
    /// DEVICE_HUNG (0x887A0006) is included alongside DEVICE_REMOVED and
    /// DEVICE_RESET because a hung GPU is treated by the driver as a TDR reset:
    /// the OS kills and recreates the device, leaving buffer data intact.
    /// AUDCLNT_E_DEVICE_INVALIDATED and MF_E_* are intentionally excluded -
    /// they are not GPU TDR events and would not flow through this path.
    /// </summary>
    private static bool IsGpuDeviceLost(Exception ex)
    {
        if (ex is AggregateException agg && agg.InnerExceptions.Count == 1 && agg.InnerException is not null)
        {
            ex = agg.InnerException;
        }

        if (ex is COMException com)
        {
            unchecked
            {
                return com.HResult == (int)0x887A0005   // DXGI_ERROR_DEVICE_REMOVED
                    || com.HResult == (int)0x887A0006   // DXGI_ERROR_DEVICE_HUNG
                    || com.HResult == (int)0x887A0007;  // DXGI_ERROR_DEVICE_RESET
            }
        }

        return false;
    }
}
