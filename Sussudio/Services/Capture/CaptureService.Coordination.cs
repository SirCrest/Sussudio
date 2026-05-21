using System;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Steady-state sampling and guard helpers for capture transitions. Serialized
// transition execution lives in CaptureService.TransitionExecution.cs.
public partial class CaptureService
{
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
