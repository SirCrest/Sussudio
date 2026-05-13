using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

// Deferred cleanup paths detach live producers first, then wait for encoders or
// exports to drain before disposing native resources and temporary artifacts.
public partial class CaptureService
{
    private void ReleaseFlashbackBackendLeaseIfHeld(ref bool backendLeaseHeld)
    {
        if (!backendLeaseHeld)
        {
            return;
        }

        backendLeaseHeld = false;
        ReleaseSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_backend_lease");
    }

    private void ReleaseFlashbackExportOperationLockIfHeld(ref bool exportOperationLockHeld)
    {
        if (!exportOperationLockHeld)
        {
            return;
        }

        exportOperationLockHeld = false;
        ReleaseSemaphoreBestEffort(_flashbackExportOperationLock, "flashback_export_operation");
    }

    private void ScheduleDeferredFlashbackBackendCleanup(
        Task sinkCompletionTask,
        FlashbackBufferManager? bufferManager,
        FlashbackExporter? flashbackExporter,
        string reason,
        bool purgeSegments,
        int attempt = 0)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await sinkCompletionTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_BACKEND_DEFERRED_WAIT_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                var cleanupCompleted = await CleanupFlashbackBackendArtifactsAfterExportAsync(
                        bufferManager,
                        flashbackExporter,
                        reason,
                        purgeSegments,
                        "deferred")
                    .ConfigureAwait(false);

                if (cleanupCompleted)
                {
                    Logger.Log($"FLASHBACK_BACKEND_DEFERRED_CLEANUP_OK reason='{reason}' attempt={attempt}");
                }
                else if (attempt < 3)
                {
                    var nextAttempt = attempt + 1;
                    Logger.Log($"FLASHBACK_BACKEND_DEFERRED_CLEANUP_RETRY reason='{reason}' attempt={attempt} next_attempt={nextAttempt}");
                    ScheduleDeferredFlashbackBackendCleanup(
                        Task.Delay(TimeSpan.FromSeconds(5)),
                        bufferManager,
                        flashbackExporter,
                        reason,
                        purgeSegments,
                        nextAttempt);
                }
                else
                {
                    Logger.Log($"FLASHBACK_BACKEND_DEFERRED_CLEANUP_GIVE_UP reason='{reason}' attempt={attempt} preserve_segments=true");
                }
            }
        });
    }

    private async Task<bool> CleanupFlashbackBackendArtifactsAfterExportAsync(
        FlashbackBufferManager? bufferManager,
        FlashbackExporter? flashbackExporter,
        string reason,
        bool purgeSegments,
        string mode,
        bool exportOperationLockAlreadyHeld = false)
    {
        if (bufferManager == null && flashbackExporter == null)
        {
            return true;
        }

        var lockAcquired = exportOperationLockAlreadyHeld;
        var releaseLockOnExit = false;
        try
        {
            if (!exportOperationLockAlreadyHeld)
            {
                Logger.Log($"FLASHBACK_BACKEND_CLEANUP_AWAITING_EXPORT_LOCK mode={mode} reason='{reason}'");
                var lockSw = System.Diagnostics.Stopwatch.StartNew();
                lockAcquired = await _flashbackExportOperationLock
                    .WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None)
                    .ConfigureAwait(false);
                lockSw.Stop();

                if (!lockAcquired)
                {
                    Logger.Log($"FLASHBACK_BACKEND_CLEANUP_EXPORT_LOCK_TIMEOUT mode={mode} reason='{reason}' preserve_segments=true");
                    return false;
                }

                releaseLockOnExit = true;
                Logger.Log($"FLASHBACK_BACKEND_CLEANUP_LOCK_ACQUIRED mode={mode} elapsed_ms={lockSw.ElapsedMilliseconds} reason='{reason}'");
            }
            else
            {
                Logger.Log($"FLASHBACK_BACKEND_CLEANUP_LOCK_REUSED mode={mode} reason='{reason}'");
            }

            if (flashbackExporter != null)
            {
                try { flashbackExporter.Dispose(); }
                catch (Exception ex) { Logger.Log($"FLASHBACK_EXPORTER_CLEANUP_DISPOSE_WARN mode={mode} reason='{reason}' type={ex.GetType().Name} msg={ex.Message}"); }
            }

            if (bufferManager != null)
            {
                if (purgeSegments)
                {
                    try { bufferManager.PurgeAllSegments(); }
                    catch (Exception ex) { Logger.Log($"FLASHBACK_BUFFER_CLEANUP_PURGE_WARN mode={mode} reason='{reason}' type={ex.GetType().Name} msg={ex.Message}"); }
                }

                try { bufferManager.Dispose(); }
                catch (Exception ex) { Logger.Log($"FLASHBACK_BUFFER_CLEANUP_DISPOSE_WARN mode={mode} reason='{reason}' type={ex.GetType().Name} msg={ex.Message}"); }
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_BACKEND_CLEANUP_WARN mode={mode} reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
            return false;
        }
        finally
        {
            if (lockAcquired && releaseLockOnExit)
            {
                ReleaseSemaphoreBestEffort(_flashbackExportOperationLock, $"flashback_backend_cleanup_{mode}");
            }
        }
    }

    private Task ScheduleDeferredUnifiedVideoCaptureCleanup(
        Task sinkCompletionTask,
        UnifiedVideoCapture unifiedVideoCapture,
        string reason)
    {
        try
        {
            unifiedVideoCapture.SetPreviewSink(null);
        }
        catch (Exception ex)
        {
            Logger.Log($"UNIFIED_VIDEO_DEFERRED_PREVIEW_DETACH_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
        }

        return Task.Run(async () =>
        {
            Exception? cleanupFailure = null;
            try
            {
                await sinkCompletionTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"UNIFIED_VIDEO_DEFERRED_WAIT_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                try
                {
                    await unifiedVideoCapture.StopAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    cleanupFailure ??= ex;
                    Logger.Log($"UNIFIED_VIDEO_DEFERRED_STOP_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
                }

                try
                {
                    await unifiedVideoCapture.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    cleanupFailure ??= ex;
                    Logger.Log($"UNIFIED_VIDEO_DEFERRED_DISPOSE_WARN reason='{reason}' type={ex.GetType().Name} msg={ex.Message}");
                }

                Logger.Log($"UNIFIED_VIDEO_DEFERRED_CLEANUP_END reason='{reason}'");

                if (cleanupFailure != null)
                {
                    throw new InvalidOperationException(
                        $"Deferred unified video cleanup failed for reason '{reason}'.",
                        cleanupFailure);
                }
            }
        });
    }

    private void ClearPendingLibAvDrainTaskIfCompletedSuccessfully()
    {
        if (_pendingLibAvDrainTask?.IsCompletedSuccessfully == true)
        {
            _pendingLibAvDrainTask = null;
        }
    }

    private void ThrowIfPendingLibAvDrainTaskBlocksReentry()
    {
        var pendingLibAvDrainTask = _pendingLibAvDrainTask;
        if (pendingLibAvDrainTask == null)
        {
            return;
        }

        if (pendingLibAvDrainTask.IsCompletedSuccessfully)
        {
            _pendingLibAvDrainTask = null;
            return;
        }

        if (pendingLibAvDrainTask.IsFaulted)
        {
            throw new InvalidOperationException(
                "Previous recording backend failed to finalize cleanly. Check the logs and retry.",
                pendingLibAvDrainTask.Exception?.GetBaseException());
        }

        if (pendingLibAvDrainTask.IsCanceled)
        {
            throw new InvalidOperationException("Previous recording backend cleanup was canceled. Check the logs and retry.");
        }

        throw new InvalidOperationException("Previous recording backend is still finalizing. Please wait a moment and try again.");
    }
}
