using System;

namespace Sussudio.Controllers;

internal enum PreviewStartupState
{
    Idle,
    StartingSession,
    RendererAttaching,
    WaitingForFirstVisual,
    Rendering,
    Failed
}

internal sealed class PreviewStartupSessionController
{
    public PreviewStartupState State { get; private set; } = PreviewStartupState.Idle;
    public string? AttemptId { get; private set; }
    public DateTimeOffset? RequestedUtc { get; private set; }
    public DateTimeOffset? RendererAttachedUtc { get; private set; }
    public DateTimeOffset? FirstVisualUtc { get; private set; }
    public string? LastFailureReason { get; private set; }
    public string? MissingSignals { get; private set; }
    public int RecoveryAttemptCount { get; private set; }
    public bool FirstVisualConfirmed { get; private set; }

    public bool IsFailed => IsFailedState(State);
    public bool IsIdle => State == PreviewStartupState.Idle;
    public bool IsWaitingForFirstVisual => State == PreviewStartupState.WaitingForFirstVisual;
    public bool IsTerminal => IsTerminalState(State);
    public bool ShouldBeginAttempt => string.IsNullOrWhiteSpace(AttemptId) || IsFailed || IsIdle;

    public static bool IsFailedState(PreviewStartupState state)
        => state == PreviewStartupState.Failed;

    public static bool IsTerminalState(PreviewStartupState state)
        => state is PreviewStartupState.Idle or PreviewStartupState.Rendering or PreviewStartupState.Failed;

    public bool BeginAttempt(string attemptId, DateTimeOffset requestedUtc)
    {
        RecoveryAttemptCount = 0;
        AttemptId = attemptId;
        RequestedUtc = requestedUtc;
        RendererAttachedUtc = null;
        FirstVisualUtc = null;
        LastFailureReason = null;
        MissingSignals = null;
        FirstVisualConfirmed = false;

        return SetState(PreviewStartupState.StartingSession);
    }

    public bool SetState(PreviewStartupState state, string? reason = null)
    {
        if (!string.IsNullOrWhiteSpace(reason))
        {
            LastFailureReason = reason;
        }

        if (State == state && string.IsNullOrWhiteSpace(reason))
        {
            return false;
        }

        State = state;
        return true;
    }

    public void MarkRendererAttached(DateTimeOffset attachedUtc)
        => RendererAttachedUtc = attachedUtc;

    public bool MarkFirstVisualConfirmed(DateTimeOffset firstVisualUtc)
    {
        if (FirstVisualConfirmed)
        {
            return false;
        }

        FirstVisualConfirmed = true;
        FirstVisualUtc = firstVisualUtc;
        return true;
    }

    public void SetMissingSignals(string? missingSignals)
        => MissingSignals = missingSignals;

    public bool Reset(bool keepRecoveryCount = false)
    {
        var shouldLogIdle = !IsTerminal;

        AttemptId = null;
        RequestedUtc = null;
        RendererAttachedUtc = null;
        FirstVisualUtc = null;
        LastFailureReason = null;
        MissingSignals = null;
        FirstVisualConfirmed = false;

        if (!keepRecoveryCount)
        {
            RecoveryAttemptCount = 0;
        }

        if (shouldLogIdle)
        {
            return SetState(PreviewStartupState.Idle);
        }

        State = PreviewStartupState.Idle;
        return false;
    }

    public double GetElapsedMilliseconds(DateTimeOffset utcNow)
        => RequestedUtc.HasValue
            ? (utcNow - RequestedUtc.Value).TotalMilliseconds
            : 0;
}
