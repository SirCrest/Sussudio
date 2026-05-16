using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Sussudio.Services.Runtime;

namespace Sussudio.Controllers;

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
    public required Func<string> BuildTimeoutDiagnosticPayload { get; init; }
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
                _context.SetStatusText(PreviewStartupFailureTextFormatter.FormatFailureStopStatusText(reason));
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
        var timeoutReason = PreviewStartupFailureTextFormatter.FormatTimeoutReason(
            VisualTimeoutMs,
            _context.GetMissingSignals());
        _context.MarkStartupFailed(timeoutReason);
        Logger.Log(
            $"PREVIEW_START_TIMEOUT attempt={_context.GetAttemptLabel()} " +
            $"elapsedMs={elapsedMs:0} {_context.BuildTimeoutDiagnosticPayload()}");
        _context.LogPlaybackSnapshot("timeout");

        _context.StopStartupOverlay();
        _context.SetStatusText(PreviewStartupFailureTextFormatter.FormatTimeoutStatusText(_context.GetMissingSignals()));
        ScheduleFailureStop(timeoutReason);
        return Task.CompletedTask;
    }
}
