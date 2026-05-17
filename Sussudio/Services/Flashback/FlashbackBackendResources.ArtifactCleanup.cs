using System;
using System.Threading.Tasks;

namespace Sussudio.Services.Flashback;

internal readonly record struct FlashbackBackendArtifactCleanupRequest(
    FlashbackBufferManager? BufferManager,
    FlashbackExporter? FlashbackExporter,
    string Reason,
    bool PurgeSegments);

internal sealed partial class FlashbackBackendResources
{
    public void ScheduleDeferredArtifactCleanup(
        Task sinkCompletionTask,
        FlashbackBackendArtifactCleanupRequest request,
        Func<Task<bool>> acquireExportOperationLockAsync,
        Action<string> releaseExportOperationLock,
        int attempt = 0)
    {
        ArgumentNullException.ThrowIfNull(sinkCompletionTask);
        ArgumentNullException.ThrowIfNull(acquireExportOperationLockAsync);
        ArgumentNullException.ThrowIfNull(releaseExportOperationLock);

        _ = Task.Run(async () =>
        {
            try
            {
                await sinkCompletionTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_BACKEND_DEFERRED_WAIT_WARN reason='{request.Reason}' type={ex.GetType().Name} msg={ex.Message}");
            }
            finally
            {
                var cleanupCompleted = await CleanupArtifactsAfterExportAsync(
                        request,
                        "deferred",
                        acquireExportOperationLockAsync,
                        releaseExportOperationLock)
                    .ConfigureAwait(false);

                if (cleanupCompleted)
                {
                    Logger.Log($"FLASHBACK_BACKEND_DEFERRED_CLEANUP_OK reason='{request.Reason}' attempt={attempt}");
                }
                else if (attempt < 3)
                {
                    var nextAttempt = attempt + 1;
                    Logger.Log($"FLASHBACK_BACKEND_DEFERRED_CLEANUP_RETRY reason='{request.Reason}' attempt={attempt} next_attempt={nextAttempt}");
                    ScheduleDeferredArtifactCleanup(
                        Task.Delay(TimeSpan.FromSeconds(5)),
                        request,
                        acquireExportOperationLockAsync,
                        releaseExportOperationLock,
                        nextAttempt);
                }
                else
                {
                    Logger.Log($"FLASHBACK_BACKEND_DEFERRED_CLEANUP_GIVE_UP reason='{request.Reason}' attempt={attempt} preserve_segments=true");
                }
            }
        });
    }

    public async Task<bool> CleanupArtifactsAfterExportAsync(
        FlashbackBackendArtifactCleanupRequest request,
        string mode,
        Func<Task<bool>> acquireExportOperationLockAsync,
        Action<string> releaseExportOperationLock,
        bool exportOperationLockAlreadyHeld = false)
    {
        ArgumentNullException.ThrowIfNull(acquireExportOperationLockAsync);
        ArgumentNullException.ThrowIfNull(releaseExportOperationLock);

        if (request.BufferManager == null && request.FlashbackExporter == null)
        {
            return true;
        }

        var lockAcquired = exportOperationLockAlreadyHeld;
        var releaseLockOnExit = false;
        try
        {
            if (!exportOperationLockAlreadyHeld)
            {
                Logger.Log($"FLASHBACK_BACKEND_CLEANUP_AWAITING_EXPORT_LOCK mode={mode} reason='{request.Reason}'");
                var lockSw = System.Diagnostics.Stopwatch.StartNew();
                lockAcquired = await acquireExportOperationLockAsync().ConfigureAwait(false);
                lockSw.Stop();

                if (!lockAcquired)
                {
                    Logger.Log($"FLASHBACK_BACKEND_CLEANUP_EXPORT_LOCK_TIMEOUT mode={mode} reason='{request.Reason}' preserve_segments=true");
                    return false;
                }

                releaseLockOnExit = true;
                Logger.Log($"FLASHBACK_BACKEND_CLEANUP_LOCK_ACQUIRED mode={mode} elapsed_ms={lockSw.ElapsedMilliseconds} reason='{request.Reason}'");
            }
            else
            {
                Logger.Log($"FLASHBACK_BACKEND_CLEANUP_LOCK_REUSED mode={mode} reason='{request.Reason}'");
            }

            if (request.FlashbackExporter != null)
            {
                try { request.FlashbackExporter.Dispose(); }
                catch (Exception ex) { Logger.Log($"FLASHBACK_EXPORTER_CLEANUP_DISPOSE_WARN mode={mode} reason='{request.Reason}' type={ex.GetType().Name} msg={ex.Message}"); }
            }

            if (request.BufferManager != null)
            {
                if (request.PurgeSegments)
                {
                    try { request.BufferManager.PurgeAllSegments(); }
                    catch (Exception ex) { Logger.Log($"FLASHBACK_BUFFER_CLEANUP_PURGE_WARN mode={mode} reason='{request.Reason}' type={ex.GetType().Name} msg={ex.Message}"); }
                }

                try { request.BufferManager.Dispose(); }
                catch (Exception ex) { Logger.Log($"FLASHBACK_BUFFER_CLEANUP_DISPOSE_WARN mode={mode} reason='{request.Reason}' type={ex.GetType().Name} msg={ex.Message}"); }
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_BACKEND_CLEANUP_WARN mode={mode} reason='{request.Reason}' type={ex.GetType().Name} msg={ex.Message}");
            return false;
        }
        finally
        {
            if (lockAcquired && releaseLockOnExit)
            {
                releaseExportOperationLock(mode);
            }
        }
    }
}
