using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Sussudio.Services.Runtime;

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
    public bool ShouldRefreshMissingSignalsForSnapshot => IsWaitingForFirstVisual || IsFailed;
    public bool ShouldBeginAttempt => string.IsNullOrWhiteSpace(AttemptId) || IsFailed || IsIdle;
    public string AttemptLabel => AttemptId ?? "none";

    public bool IsSignalWindowActive(bool isPreviewing)
        => isPreviewing &&
           !FirstVisualConfirmed &&
           State is PreviewStartupState.StartingSession or PreviewStartupState.RendererAttaching or PreviewStartupState.WaitingForFirstVisual;

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

internal sealed class PreviewStartupWatchdogControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required Func<bool> IsWaitingForFirstVisual { get; init; }
    public required Func<bool> IsSignalWindowActive { get; init; }
    public required Func<bool> IsWindowClosing { get; init; }
    public required Func<bool> IsPreviewStopRequestedByUser { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<double> GetElapsedMilliseconds { get; init; }
    public required Func<string> GetAttemptLabel { get; init; }
    public required Func<string> BuildMissingSignals { get; init; }
    public required Func<string?> GetMissingSignals { get; init; }
    public required Action<string?> SetMissingSignals { get; init; }
    public required Action<string> MarkStartupFailed { get; init; }
    public required Func<PreviewStartupTimeoutDiagnosticSnapshot> GetTimeoutDiagnosticSnapshot { get; init; }
    public required Action<string> LogPlaybackSnapshot { get; init; }
    public required Action StopStartupOverlay { get; init; }
    public required Action<string> SetStatusText { get; init; }
    public required Func<string, Task> StopPreviewForFailureAsync { get; init; }
    public required Func<Func<Task>, string, Task> RunUiEventHandlerAsync { get; init; }
}

internal sealed class PreviewStartupWatchdogController
{
    private const int PreviewStartupDefaultVisualTimeoutMs = 10000;
    private const int PreviewStartupMinVisualTimeoutMs = 1000;
    private const int PreviewStartupMaxVisualTimeoutMs = 15000;

    // Lazy<int> instead of static readonly so per-test env overrides work:
    // tests that flip SUSSUDIO_PREVIEW_START_TIMEOUT_MS before constructing
    // MainWindow get the override on the first read instead of a value
    // baked in at type-init time.
    private readonly Lazy<int> _visualTimeoutMs = new(static () =>
        EnvironmentHelpers.GetIntFromEnv(
            "SUSSUDIO_PREVIEW_START_TIMEOUT_MS",
            PreviewStartupDefaultVisualTimeoutMs,
            PreviewStartupMinVisualTimeoutMs,
            PreviewStartupMaxVisualTimeoutMs));

    private readonly PreviewStartupWatchdogControllerContext _context;
    private DispatcherQueueTimer? _watchdogTimer;
    private DispatcherQueueTimer? _telemetryTimer;
    private int _failureStopScheduled;

    public PreviewStartupWatchdogController(PreviewStartupWatchdogControllerContext context)
    {
        _context = context;
    }

    public int VisualTimeoutMs => _visualTimeoutMs.Value;

    public void Start()
    {
        Stop();
        if (!_context.IsWaitingForFirstVisual())
        {
            return;
        }

        _watchdogTimer ??= _context.DispatcherQueue.CreateTimer();
        _watchdogTimer.Interval = TimeSpan.FromMilliseconds(VisualTimeoutMs);
        _watchdogTimer.IsRepeating = false;
        _watchdogTimer.Tick -= WatchdogTimer_Tick;
        _watchdogTimer.Tick += WatchdogTimer_Tick;
        _watchdogTimer.Start();
        StartTelemetry();
        Logger.Log(
            $"PREVIEW_START_WATCHDOG_STARTED attempt={_context.GetAttemptLabel()} " +
            $"timeoutMs={VisualTimeoutMs}");
    }

    public void Stop()
    {
        _watchdogTimer?.Stop();
        StopTelemetry();
    }

    public void ResetFailureStopSchedule()
        => Interlocked.Exchange(ref _failureStopScheduled, 0);

    public void ScheduleFailureStop(string reason)
    {
        if (_context.IsWindowClosing())
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _failureStopScheduled, 1, 0) != 0)
        {
            return;
        }

        _ = _context.RunUiEventHandlerAsync(async () =>
        {
            try
            {
                if (!_context.IsPreviewing())
                {
                    return;
                }

                Logger.Log($"PREVIEW_START_FAILURE_STOP begin reason={reason} attempt={_context.GetAttemptLabel()}");
                // Preview startup failed; pipeline state is unclean, so force full teardown.
                await _context.StopPreviewForFailureAsync(reason).ConfigureAwait(true);
                _context.SetStatusText(FormatFailureStopStatusText(reason));
                Logger.Log($"PREVIEW_START_FAILURE_STOP completed reason={reason} attempt={_context.GetAttemptLabel()}");
            }
            finally
            {
                ResetFailureStopSchedule();
            }
        }, "PreviewStartupFailureStop");
    }

    private void StartTelemetry()
    {
        _telemetryTimer ??= _context.DispatcherQueue.CreateTimer();
        _telemetryTimer.Interval = TimeSpan.FromSeconds(1);
        _telemetryTimer.IsRepeating = true;
        _telemetryTimer.Tick -= TelemetryTimer_Tick;
        _telemetryTimer.Tick += TelemetryTimer_Tick;
        _telemetryTimer.Start();
    }

    private void StopTelemetry()
    {
        _telemetryTimer?.Stop();
    }

    private void TelemetryTimer_Tick(object? sender, object e)
    {
        if (!_context.IsSignalWindowActive())
        {
            return;
        }

        _context.LogPlaybackSnapshot("watchdog-tick");
    }

    private async void WatchdogTimer_Tick(object? sender, object e)
    {
        Stop();
        await HandleTimeoutAsync().ConfigureAwait(true);
    }

    private Task HandleTimeoutAsync()
    {
        if (_context.IsWindowClosing() || _context.IsPreviewStopRequestedByUser())
        {
            Logger.Log("PREVIEW_START_TIMEOUT_IGNORED reason=user-or-shutdown-stop-requested");
            return Task.CompletedTask;
        }

        if (!_context.IsPreviewing() || !_context.IsWaitingForFirstVisual())
        {
            return Task.CompletedTask;
        }

        var elapsedMs = _context.GetElapsedMilliseconds();
        _context.SetMissingSignals(_context.BuildMissingSignals());
        var timeoutReason = FormatTimeoutReason(
            VisualTimeoutMs,
            _context.GetMissingSignals());
        _context.MarkStartupFailed(timeoutReason);
        var timeoutDiagnosticPayload = PreviewStartupSignalFormatter.FormatTimeoutDiagnosticPayload(
            _context.GetTimeoutDiagnosticSnapshot());
        Logger.Log(
            $"PREVIEW_START_TIMEOUT attempt={_context.GetAttemptLabel()} " +
            $"elapsedMs={elapsedMs:0} {timeoutDiagnosticPayload}");
        _context.LogPlaybackSnapshot("timeout");

        _context.StopStartupOverlay();
        _context.SetStatusText(FormatTimeoutStatusText(_context.GetMissingSignals()));
        ScheduleFailureStop(timeoutReason);
        return Task.CompletedTask;
    }

    private static string FormatTimeoutReason(int timeoutMs, string? missingSignals)
        => string.IsNullOrWhiteSpace(missingSignals)
            ? $"no-visual-confirmation-within-{timeoutMs}ms"
            : $"no-visual-confirmation-within-{timeoutMs}ms missing:{missingSignals}";

    private static string FormatTimeoutStatusText(string? missingSignals)
        => string.IsNullOrWhiteSpace(missingSignals)
            ? "Preview failed to attach to UI (session started but no visual confirmation)."
            : $"Preview failed to start (missing readiness signal: {missingSignals}).";

    private static string FormatFailureStopStatusText(string reason)
        => $"Preview startup failed: {reason}";
}
