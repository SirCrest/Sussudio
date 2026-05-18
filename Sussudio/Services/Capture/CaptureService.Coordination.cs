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
            EnterTransitionState(transitionState);
            await action(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            ResolveSessionSteadyState();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            ResolveSessionSteadyState();
            throw;
        }
        catch (Exception ex)
        {
            EnterFaultedState();
            ErrorOccurred?.Invoke(this, ex);
            throw;
        }
        finally
        {
            ReleaseSemaphoreBestEffort(_sessionTransitionLock, "session_transition");
        }
    }

    private void EnterTransitionState(CaptureSessionState transitionState)
    {
        CaptureSessionTransitionPolicy.ThrowIfDisallowed(_sessionState, transitionState);
        Interlocked.Increment(ref _sessionGeneration);
        _sessionState = transitionState;
    }

    private void ResolveSessionSteadyState()
        => _sessionState = ResolveSteadyState();

    private CaptureSessionState ResolveSteadyState()
    {
        return CaptureSessionTransitionPolicy.ResolveSteadyState(
            _isDisposed != 0,
            _isRecording,
            _isVideoPreviewActive,
            _isAudioPreviewActive,
            _isInitialized);
    }

    private void EnterCleanupState()
        => _sessionState = CaptureSessionState.CleaningUp;

    private void EnterFaultedState()
        => _sessionState = CaptureSessionState.Faulted;

    private void EnterDisposedState()
        => _sessionState = CaptureSessionState.Disposed;

    private void ResetSessionStateAfterCleanup()
        => _sessionState = _isDisposed != 0 ? CaptureSessionState.Disposed : CaptureSessionState.Uninitialized;

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
