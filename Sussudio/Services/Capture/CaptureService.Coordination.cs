using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

// Transition serialization, disposal, and best-effort lock cleanup for the
// capture service. Feature partials call these helpers instead of owning their
// own session-state mutation policy.
public partial class CaptureService
{
    private async Task RunTransitionAsync(
        CaptureSessionState transitionState,
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _sessionTransitionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            CaptureSessionTransitionPolicy.ThrowIfDisallowed(_sessionState, transitionState);
            Interlocked.Increment(ref _sessionGeneration);
            _sessionState = transitionState;
            await action(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            _sessionState = ResolveSteadyState();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _sessionState = ResolveSteadyState();
            throw;
        }
        catch (Exception ex)
        {
            _sessionState = CaptureSessionState.Faulted;
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
        finally
        {
            ReleaseSemaphoreBestEffort(_sessionTransitionLock, "session_transition");
        }
    }

    private CaptureSessionState ResolveSteadyState()
    {
        return CaptureSessionTransitionPolicy.ResolveSteadyState(
            _isDisposed != 0,
            _isRecording,
            _isVideoPreviewActive,
            _isAudioPreviewActive,
            _isInitialized);
    }

    private void EnsureInitialized()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Capture not initialized");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed != 0)
        {
            throw new ObjectDisposedException(nameof(CaptureService));
        }
    }

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
