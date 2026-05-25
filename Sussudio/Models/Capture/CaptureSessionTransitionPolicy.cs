using System;
using System.Threading;

namespace Sussudio.Models;

/// <summary>
/// Pure lifecycle rules for capture session state transitions. Resource
/// acquisition and release still belong to CaptureService; this type only
/// defines which high-level states may be entered.
/// </summary>
public static class CaptureSessionTransitionPolicy
{
    public static bool CanEnterTransition(
        CaptureSessionState currentState,
        CaptureSessionState transitionState)
    {
        if (currentState == CaptureSessionState.Disposed)
        {
            return false;
        }

        if (currentState == transitionState)
        {
            return true;
        }

        if (currentState == CaptureSessionState.CleaningUp)
        {
            return transitionState == CaptureSessionState.CleaningUp;
        }

        return transitionState switch
        {
            CaptureSessionState.Initializing => true,
            CaptureSessionState.Ready => true,
            CaptureSessionState.Previewing => true,
            CaptureSessionState.Recording => currentState is CaptureSessionState.Ready or CaptureSessionState.Previewing or CaptureSessionState.Recording,
            CaptureSessionState.CleaningUp => true,
            CaptureSessionState.Uninitialized => false,
            CaptureSessionState.Faulted => false,
            CaptureSessionState.Disposed => false,
            _ => false
        };
    }

    public static void ThrowIfDisallowed(
        CaptureSessionState currentState,
        CaptureSessionState transitionState)
    {
        if (CanEnterTransition(currentState, transitionState))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Capture session transition is not allowed: {currentState} -> {transitionState}.");
    }

    public static CaptureSessionState ResolveSteadyState(
        bool isDisposed,
        bool isRecording,
        bool isVideoPreviewActive,
        bool isAudioPreviewActive,
        bool isInitialized)
    {
        if (isDisposed) return CaptureSessionState.Disposed;
        if (isRecording) return CaptureSessionState.Recording;
        if (isVideoPreviewActive || isAudioPreviewActive) return CaptureSessionState.Previewing;
        return isInitialized ? CaptureSessionState.Ready : CaptureSessionState.Uninitialized;
    }
}

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
