using System;
using System.Threading;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

// Best-effort resource release helpers shared by capture lifecycle, Flashback
// export/disposal, and recording finalization paths.
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

    private void DisposeCoordinationLocksBestEffort()
    {
        DisposeSemaphoreBestEffort(_sessionTransitionLock, "session_transition");
        DisposeSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_backend_lease");
        DisposeSemaphoreBestEffort(_flashbackExportOperationLock, "flashback_export_operation");
    }

    private static void ReleaseSemaphoreBestEffort(SemaphoreSlim semaphore, string operation)
    {
        try
        {
            semaphore.Release();
        }
        catch (Exception ex)
        {
            Logger.Log($"CAPTURE_SERVICE_SEMAPHORE_RELEASE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private static void DisposeSemaphoreBestEffort(SemaphoreSlim semaphore, string operation)
    {
        try
        {
            semaphore.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"CAPTURE_SERVICE_SEMAPHORE_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private static void ResumeFlashbackEvictionBestEffort(FlashbackBufferManager? bufferManager, string operation)
    {
        if (bufferManager == null)
        {
            return;
        }

        try
        {
            bufferManager.ResumeEviction();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EVICTION_RESUME_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }
}
