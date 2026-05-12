using System;

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
