using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Sussudio.Controllers;
using Sussudio.Services.Runtime;

namespace Sussudio;

// Preview startup watchdog and timeout recovery. Core startup state remains in
// MainWindow.PreviewStartup.cs; readiness-signal collection remains in
// MainWindow.PreviewStartupSignals.cs.
public sealed partial class MainWindow
{
    private const int PreviewStartupDefaultVisualTimeoutMs = 10000;
    private const int PreviewStartupMinVisualTimeoutMs = 1000;
    private const int PreviewStartupMaxVisualTimeoutMs = 15000;
    // Lazy<int> instead of static readonly so per-test env overrides work:
    // tests that flip SUSSUDIO_PREVIEW_START_TIMEOUT_MS before constructing
    // MainWindow get the override on the first read instead of a value
    // baked in at type-init time.
    private readonly Lazy<int> _previewStartupVisualTimeoutMs = new(static () =>
        EnvironmentHelpers.GetIntFromEnv(
            "SUSSUDIO_PREVIEW_START_TIMEOUT_MS",
            PreviewStartupDefaultVisualTimeoutMs,
            PreviewStartupMinVisualTimeoutMs,
            PreviewStartupMaxVisualTimeoutMs));

    private DispatcherQueueTimer? _previewStartupWatchdogTimer;
    private DispatcherQueueTimer? _previewStartupTelemetryTimer;
    private int _previewStartupFailureStopScheduled;

    private int PreviewStartupVisualTimeoutMs => _previewStartupVisualTimeoutMs.Value;

    private void StartPreviewStartupWatchdog()
    {
        StopPreviewStartupWatchdog();
        if (_previewStartupState != PreviewStartupState.WaitingForFirstVisual)
        {
            return;
        }

        _previewStartupWatchdogTimer ??= _dispatcherQueue.CreateTimer();
        _previewStartupWatchdogTimer.Interval = TimeSpan.FromMilliseconds(PreviewStartupVisualTimeoutMs);
        _previewStartupWatchdogTimer.IsRepeating = false;
        _previewStartupWatchdogTimer.Tick -= PreviewStartupWatchdogTimer_Tick;
        _previewStartupWatchdogTimer.Tick += PreviewStartupWatchdogTimer_Tick;
        _previewStartupWatchdogTimer.Start();
        StartPreviewStartupTelemetry();
        Logger.Log(
            $"PREVIEW_START_WATCHDOG_STARTED attempt={_previewStartupAttemptId ?? "none"} " +
            $"timeoutMs={PreviewStartupVisualTimeoutMs}");
    }

    private void StopPreviewStartupWatchdog()
    {
        _previewStartupWatchdogTimer?.Stop();
        StopPreviewStartupTelemetry();
    }

    private void StartPreviewStartupTelemetry()
    {
        _previewStartupTelemetryTimer ??= _dispatcherQueue.CreateTimer();
        _previewStartupTelemetryTimer.Interval = TimeSpan.FromSeconds(1);
        _previewStartupTelemetryTimer.IsRepeating = true;
        _previewStartupTelemetryTimer.Tick -= PreviewStartupTelemetryTimer_Tick;
        _previewStartupTelemetryTimer.Tick += PreviewStartupTelemetryTimer_Tick;
        _previewStartupTelemetryTimer.Start();
    }

    private void StopPreviewStartupTelemetry()
    {
        _previewStartupTelemetryTimer?.Stop();
    }

    private void PreviewStartupTelemetryTimer_Tick(object? sender, object e)
    {
        if (!IsPreviewStartupSignalWindowActive())
        {
            return;
        }

        LogPreviewStartupPlaybackSnapshot("watchdog-tick");
    }

    private void SchedulePreviewStartupFailureStop(string reason)
    {
        if (_isWindowClosing)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _previewStartupFailureStopScheduled, 1, 0) != 0)
        {
            return;
        }

        _ = RunUiEventHandlerAsync(async () =>
        {
            try
            {
                if (!ViewModel.IsPreviewing)
                {
                    return;
                }

                Logger.Log($"PREVIEW_START_FAILURE_STOP begin reason={reason} attempt={_previewStartupAttemptId ?? "none"}");
                // Preview startup failed; pipeline state is unclean, so force full teardown.
                await ViewModel.StopPreviewAsync(userInitiated: true, teardownPipeline: true);
                ViewModel.StatusText = PreviewStartupFailureTextFormatter.FormatFailureStopStatusText(reason);
                Logger.Log($"PREVIEW_START_FAILURE_STOP completed reason={reason} attempt={_previewStartupAttemptId ?? "none"}");
            }
            finally
            {
                Interlocked.Exchange(ref _previewStartupFailureStopScheduled, 0);
            }
        }, "PreviewStartupFailureStop");
    }

    private async void PreviewStartupWatchdogTimer_Tick(object? sender, object e)
    {
        StopPreviewStartupWatchdog();
        await HandlePreviewStartupTimeoutAsync();
    }

    private Task HandlePreviewStartupTimeoutAsync()
    {
        if (_isWindowClosing || _previewStopRequestedByUser)
        {
            Logger.Log("PREVIEW_START_TIMEOUT_IGNORED reason=user-or-shutdown-stop-requested");
            return Task.CompletedTask;
        }

        if (!ViewModel.IsPreviewing || _previewStartupState != PreviewStartupState.WaitingForFirstVisual)
        {
            return Task.CompletedTask;
        }

        var elapsedMs = _previewStartupRequestedUtc.HasValue
            ? (DateTimeOffset.UtcNow - _previewStartupRequestedUtc.Value).TotalMilliseconds
            : 0;
        _previewStartupMissingSignals = BuildPreviewStartupMissingSignals();
        var timeoutReason = PreviewStartupFailureTextFormatter.FormatTimeoutReason(
            PreviewStartupVisualTimeoutMs,
            _previewStartupMissingSignals);
        SetPreviewStartupState(PreviewStartupState.Failed, timeoutReason);
        Logger.Log(
            $"PREVIEW_START_TIMEOUT attempt={_previewStartupAttemptId ?? "none"} " +
            $"elapsedMs={elapsedMs:0} placeholder={NoDevicePlaceholder.Visibility} " +
            $"gpuVisible={PreviewSwapChainPanel.Visibility} cpuVisible={PreviewImage.Visibility} " +
            $"strategy={_previewStartupStrategy} required={PreviewStartupSignalFormatter.FormatSignalList(_previewStartupRequiredSignals)} " +
            $"received={PreviewStartupSignalFormatter.FormatSignalList(_previewStartupReceivedSignals)} " +
            $"missing={_previewStartupMissingSignals ?? "-"}");
        LogPreviewStartupPlaybackSnapshot("timeout");

        StopPreviewStartupOverlay();
        ViewModel.StatusText = PreviewStartupFailureTextFormatter.FormatTimeoutStatusText(_previewStartupMissingSignals);
        SchedulePreviewStartupFailureStop(timeoutReason);
        return Task.CompletedTask;
    }
}
