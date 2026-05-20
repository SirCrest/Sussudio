using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

internal readonly record struct CaptureSessionSteadyStateInputs(
    bool IsDisposed,
    bool IsRecording,
    bool IsVideoPreviewActive,
    bool IsAudioPreviewActive,
    bool IsInitialized);

internal sealed class CaptureSessionStateMachine
{
    private CaptureSessionState _state = CaptureSessionState.Uninitialized;
    private long _generation;

    public CaptureSessionState State => _state;

    public long Generation => Interlocked.Read(ref _generation);

    public void EnterTransition(CaptureSessionState transitionState)
    {
        CaptureSessionTransitionPolicy.ThrowIfDisallowed(_state, transitionState);
        Interlocked.Increment(ref _generation);
        _state = transitionState;
    }

    public void ResolveSteadyState(CaptureSessionSteadyStateInputs inputs)
        => _state = CaptureSessionTransitionPolicy.ResolveSteadyState(
            inputs.IsDisposed,
            inputs.IsRecording,
            inputs.IsVideoPreviewActive,
            inputs.IsAudioPreviewActive,
            inputs.IsInitialized);

    public void EnterCleanup()
        => _state = CaptureSessionState.CleaningUp;

    public void EnterFaulted()
        => _state = CaptureSessionState.Faulted;

    public void EnterDisposed()
        => _state = CaptureSessionState.Disposed;

    public void ResetAfterCleanup(bool isDisposed)
        => _state = isDisposed ? CaptureSessionState.Disposed : CaptureSessionState.Uninitialized;
}
