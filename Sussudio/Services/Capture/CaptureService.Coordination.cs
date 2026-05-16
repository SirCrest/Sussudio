using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Transition serialization for the capture service. Feature partials call this
// helper instead of owning their own session-state mutation policy.
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

}
