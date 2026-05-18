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

internal sealed class PreviewStartupSessionControllerContext
{
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsPreviewStopRequestedByUser { get; init; }
    public required Func<string?> GetSelectedDeviceName { get; init; }
    public required Action ResetSignalState { get; init; }
    public required Action ResetFailureStopSchedule { get; init; }
    public required Action MarkFirstVisualSignalConfirmed { get; init; }
    public required Action StopWatchdog { get; init; }
    public required Action StopOverlay { get; init; }
    public required Action StopFadeInTimer { get; init; }
    public required Action ScheduleFadeIn { get; init; }
    public required Action<string, string> CompleteFirstVisualTransition { get; init; }
    public required Action<bool, string> ClearReinitTransitionForStartupReset { get; init; }
    public required Action<string> Log { get; init; }
    public required Func<string> CreateAttemptId { get; init; }
    public required Func<DateTimeOffset> GetUtcNow { get; init; }
}

internal sealed class PreviewStartupSessionController
{
    private const string ConfirmFirstVisualCallerName = "ConfirmPreviewFirstVisual";
    private const string ResetTrackingCallerName = "ResetPreviewStartupTracking";

    private readonly PreviewStartupSessionControllerContext _context;

    public PreviewStartupSessionController(PreviewStartupSessionControllerContext context)
    {
        _context = context;
    }

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
    public string AttemptLabel => AttemptId ?? "none";

    public static bool IsFailedState(PreviewStartupState state)
        => state == PreviewStartupState.Failed;

    public static bool IsTerminalState(PreviewStartupState state)
        => state is PreviewStartupState.Idle or PreviewStartupState.Rendering or PreviewStartupState.Failed;

    private bool BeginAttemptCore(string attemptId, DateTimeOffset requestedUtc)
    {
        RecoveryAttemptCount = 0;
        AttemptId = attemptId;
        RequestedUtc = requestedUtc;
        RendererAttachedUtc = null;
        FirstVisualUtc = null;
        LastFailureReason = null;
        MissingSignals = null;
        FirstVisualConfirmed = false;

        return SetStateCore(PreviewStartupState.StartingSession);
    }

    public void BeginStartupAttempt()
    {
        var stateChanged = BeginAttemptCore(
            _context.CreateAttemptId(),
            _context.GetUtcNow());
        _context.ResetSignalState();
        _context.ResetFailureStopSchedule();

        if (stateChanged)
        {
            LogStateChange(PreviewStartupState.StartingSession);
        }

        _context.Log(
            $"PREVIEW_START_REQUESTED attempt={AttemptId} " +
            $"device={_context.GetSelectedDeviceName() ?? "none"}");
    }

    private bool SetStateCore(PreviewStartupState state, string? reason = null)
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

    public void SetStartupState(PreviewStartupState state, string? reason = null)
    {
        if (SetStateCore(state, reason))
        {
            LogStateChange(state, reason);
        }
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

    public void ConfirmFirstVisual(string source)
    {
        if (FirstVisualConfirmed || !_context.IsPreviewing())
        {
            return;
        }

        if (_context.IsPreviewStopRequestedByUser())
        {
            _context.Log(
                $"PREVIEW_FIRST_VISUAL_IGNORED attempt={AttemptLabel} " +
                $"source={source} reason=stop-requested");
            return;
        }

        MarkFirstVisualConfirmed(_context.GetUtcNow());
        _context.MarkFirstVisualSignalConfirmed();
        SetStartupState(PreviewStartupState.Rendering);
        _context.StopWatchdog();
        _context.StopOverlay();
        _context.ScheduleFadeIn();
        _context.CompleteFirstVisualTransition(
            AttemptLabel,
            ConfirmFirstVisualCallerName);
        MissingSignals = string.Empty;
        var elapsedMs = GetElapsedMilliseconds(_context.GetUtcNow());
        _context.Log(
            $"PREVIEW_FIRST_VISUAL_CONFIRMED attempt={AttemptLabel} " +
            $"source={source} elapsedMs={elapsedMs:0} recovery={RecoveryAttemptCount}");
    }

    public void SetMissingSignals(string? missingSignals)
        => MissingSignals = missingSignals;

    private bool ResetCore(bool keepRecoveryCount = false)
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
            return SetStateCore(PreviewStartupState.Idle);
        }

        State = PreviewStartupState.Idle;
        return false;
    }

    public void ResetStartupTracking(bool keepRecoveryCount = false, bool preserveReinitAnimation = false)
    {
        _context.StopWatchdog();
        _context.StopOverlay();
        _context.StopFadeInTimer();
        _context.ClearReinitTransitionForStartupReset(
            preserveReinitAnimation,
            ResetTrackingCallerName);
        _context.ResetSignalState();
        _context.ResetFailureStopSchedule();

        if (ResetCore(keepRecoveryCount))
        {
            LogStateChange(PreviewStartupState.Idle);
        }
    }

    public double GetElapsedMilliseconds(DateTimeOffset utcNow)
        => RequestedUtc.HasValue
            ? (utcNow - RequestedUtc.Value).TotalMilliseconds
            : 0;

    private void LogStateChange(PreviewStartupState state, string? reason = null)
    {
        _context.Log(
            $"PREVIEW_START_STATE state={state} attempt={AttemptLabel} " +
            $"recovery={RecoveryAttemptCount} reason={reason ?? "-"}");
    }
}
