using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Disposal-triggered cleanup and final disposed-state ownership for the capture
// service. Normal capture transitions stay in CaptureService.Coordination.cs.
public partial class CaptureService
{
    private async Task CleanupForDisposalAsync()
    {
        await _sessionTransitionLock.WaitAsync(CancellationToken.None).ConfigureAwait(false);
        try
        {
            _sessionState = CaptureSessionState.CleaningUp;
            await CleanupCoreAsync(CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            ReleaseSemaphoreBestEffort(_sessionTransitionLock, "dispose_cleanup");
        }
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;
        try
        {
            Task.Run(CleanupForDisposalAsync).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Logger.Log($"CaptureService.Dispose cleanup warning: {ex.Message}");
        }

        DisposeCoordinationLocksBestEffort();
        _sessionState = CaptureSessionState.Disposed;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0) return;
        try
        {
            await CleanupForDisposalAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.Log($"CaptureService.DisposeAsync cleanup warning: {ex.Message}");
        }

        DisposeCoordinationLocksBestEffort();
        _sessionState = CaptureSessionState.Disposed;
    }

    private void DisposeCoordinationLocksBestEffort()
    {
        DisposeSemaphoreBestEffort(_sessionTransitionLock, "session_transition");
        DisposeSemaphoreBestEffort(_flashbackBackendLeaseLock, "flashback_backend_lease");
        DisposeSemaphoreBestEffort(_flashbackExportOperationLock, "flashback_export_operation");
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
}
