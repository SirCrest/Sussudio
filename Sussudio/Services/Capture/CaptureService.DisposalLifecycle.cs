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

}
