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
        => _sessionStateMachine.EnterTransition(transitionState);

    private void ResolveSessionSteadyState()
        => _sessionStateMachine.ResolveSteadyState(BuildSteadyStateInputs());

    private CaptureSessionState CurrentSessionState
        => _sessionStateMachine.State;

    private long CurrentSessionGeneration
        => _sessionStateMachine.Generation;

    private CaptureSessionSteadyStateInputs BuildSteadyStateInputs()
        => new(
            _isDisposed != 0,
            _isRecording,
            _isVideoPreviewActive,
            _isAudioPreviewActive,
            _isInitialized);

    private void EnterCleanupState()
        => _sessionStateMachine.EnterCleanup();

    private void EnterFaultedState()
        => _sessionStateMachine.EnterFaulted();

    private void EnterDisposedState()
        => _sessionStateMachine.EnterDisposed();

    private void ResetSessionStateAfterCleanup()
        => _sessionStateMachine.ResetAfterCleanup(_isDisposed != 0);

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
