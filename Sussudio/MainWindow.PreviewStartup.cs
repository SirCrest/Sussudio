using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using WinRT.Interop;
using Sussudio.Services.Audio;
using Sussudio.Services.Automation;
using Sussudio.Services.Capture;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio;

// Preview startup visual state machine. It delays the "preview is visible"
// transition until meaningful media/render signals arrive, avoiding black-frame
// flashes during source-reader and renderer warm-up. Signal collection lives in
// MainWindow.PreviewStartupSignals.cs.
public sealed partial class MainWindow
{
    private enum PreviewStartupState
    {
        Idle,
        StartingSession,
        RendererAttaching,
        WaitingForFirstVisual,
        Rendering,
        Failed
    }

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
    private PreviewStartupState _previewStartupState = PreviewStartupState.Idle;
    private string? _previewStartupAttemptId;
    private DateTimeOffset? _previewStartupRequestedUtc;
    private DateTimeOffset? _previewRendererAttachedUtc;
    private DateTimeOffset? _previewFirstVisualUtc;
    private string? _previewLastFailureReason;
    private string? _previewStartupMissingSignals;
    private int _previewRecoveryAttemptCount;
    private bool _previewFirstVisualConfirmed;
    private int _previewStartupFailureStopScheduled;
    private bool _previewStopRequestedByUser;
    private bool _isPreviewReinitAnimating;

    private int PreviewStartupVisualTimeoutMs => _previewStartupVisualTimeoutMs.Value;

    private static bool IsPreviewStartupFailedState(PreviewStartupState state)
        => state == PreviewStartupState.Failed;

    private static bool IsPreviewStartupTerminalState(PreviewStartupState state)
        => state is PreviewStartupState.Idle or PreviewStartupState.Rendering or PreviewStartupState.Failed;

    private void SetPreviewStartupState(PreviewStartupState state, string? reason = null)
    {
        if (!string.IsNullOrWhiteSpace(reason))
        {
            _previewLastFailureReason = reason;
        }

        if (_previewStartupState == state && string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        _previewStartupState = state;
        Logger.Log(
            $"PREVIEW_START_STATE state={state} attempt={_previewStartupAttemptId ?? "none"} " +
            $"recovery={_previewRecoveryAttemptCount} reason={reason ?? "-"}");
    }
    private void BeginPreviewStartupAttempt()
    {
        _previewRecoveryAttemptCount = 0;
        _previewStartupAttemptId = Guid.NewGuid().ToString("N");
        _previewStartupRequestedUtc = DateTimeOffset.UtcNow;
        _previewRendererAttachedUtc = null;
        _previewFirstVisualUtc = null;
        _previewLastFailureReason = null;
        _previewStartupMissingSignals = null;
        _previewFirstVisualConfirmed = false;
        ResetPreviewSignalState();
        Interlocked.Exchange(ref _previewStartupFailureStopScheduled, 0);

        SetPreviewStartupState(PreviewStartupState.StartingSession);
        Logger.Log(
            $"PREVIEW_START_REQUESTED attempt={_previewStartupAttemptId} " +
            $"device={ViewModel.SelectedDevice?.Name ?? "none"}");
    }
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
    private void StartPreviewStartupOverlay()
    {
        var ring = (ProgressRing)PreviewLoadingOverlay.Children[0];
        ring.IsActive = true;
        FadeInElement(PreviewLoadingOverlay);
    }
    private void StopPreviewStartupOverlay()
    {
        if (PreviewLoadingOverlay.Visibility == Visibility.Collapsed)
        {
            return;
        }

        var ring = (ProgressRing)PreviewLoadingOverlay.Children[0];
        ring.IsActive = false;
        if (_isPreviewReinitAnimating)
        {
            PreviewLoadingOverlay.Visibility = Visibility.Collapsed;
            PreviewLoadingOverlay.Opacity = 1.0;
            return;
        }

        FadeOutElement(PreviewLoadingOverlay);
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
                // Preview startup failed — pipeline state is unclean; force full teardown.
                await ViewModel.StopPreviewAsync(userInitiated: true, teardownPipeline: true);
                ViewModel.StatusText = $"Preview startup failed: {reason}";
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
        var timeoutReason = string.IsNullOrWhiteSpace(_previewStartupMissingSignals)
            ? $"no-visual-confirmation-within-{PreviewStartupVisualTimeoutMs}ms"
            : $"no-visual-confirmation-within-{PreviewStartupVisualTimeoutMs}ms missing:{_previewStartupMissingSignals}";
        SetPreviewStartupState(PreviewStartupState.Failed, timeoutReason);
        Logger.Log(
            $"PREVIEW_START_TIMEOUT attempt={_previewStartupAttemptId ?? "none"} " +
            $"elapsedMs={elapsedMs:0} placeholder={NoDevicePlaceholder.Visibility} " +
            $"gpuVisible={PreviewSwapChainPanel.Visibility} cpuVisible={PreviewImage.Visibility} " +
            $"strategy={_previewStartupStrategy} required={BuildPreviewStartupSignalList(_previewStartupRequiredSignals)} " +
            $"received={BuildPreviewStartupSignalList(_previewStartupReceivedSignals)} " +
            $"missing={_previewStartupMissingSignals ?? "-"}");
        LogPreviewStartupPlaybackSnapshot("timeout");

        StopPreviewStartupOverlay();
        ViewModel.StatusText = string.IsNullOrWhiteSpace(_previewStartupMissingSignals)
            ? "Preview failed to attach to UI (session started but no visual confirmation)."
            : $"Preview failed to start (missing readiness signal: {_previewStartupMissingSignals}).";
        SchedulePreviewStartupFailureStop(timeoutReason);
        return Task.CompletedTask;
    }
    private void ConfirmPreviewFirstVisual(string source)
    {
        if (_previewFirstVisualConfirmed || !ViewModel.IsPreviewing)
        {
            return;
        }

        if (_previewStopRequestedByUser)
        {
            Logger.Log(
                $"PREVIEW_FIRST_VISUAL_IGNORED attempt={_previewStartupAttemptId ?? "none"} " +
                $"source={source} reason=stop-requested");
            return;
        }

        _previewFirstVisualConfirmed = true;
        _previewStartupReceivedSignals |= PreviewStartupSignalFlags.FirstVisual;
        _previewFirstVisualUtc = DateTimeOffset.UtcNow;
        SetPreviewStartupState(PreviewStartupState.Rendering);
        StopPreviewStartupWatchdog();
        StopPreviewStartupOverlay();
        // Wait for a few rendered frames before fading in — the first frame
        // from the source reader may be black or stale while the signal settles.
        SchedulePreviewFadeIn();
        if (_isPreviewReinitAnimating)
        {
            Logger.Log($"PREVIEW_REINIT_ANIMATE_IN attempt={_previewStartupAttemptId ?? "none"}");
            _isPreviewReinitAnimating = false;
            Logger.Log($"D3D11_RENDERER_REINIT_FLAG flag=false caller={nameof(ConfirmPreviewFirstVisual)}");
        }
        _previewStartupMissingSignals = string.Empty;
        var elapsedMs = _previewStartupRequestedUtc.HasValue
            ? (DateTimeOffset.UtcNow - _previewStartupRequestedUtc.Value).TotalMilliseconds
            : 0;
        Logger.Log(
            $"PREVIEW_FIRST_VISUAL_CONFIRMED attempt={_previewStartupAttemptId ?? "none"} " +
            $"source={source} elapsedMs={elapsedMs:0} recovery={_previewRecoveryAttemptCount}");
    }
    private void ResetPreviewStartupTracking(bool keepRecoveryCount = false, bool preserveReinitAnimation = false)
    {
        StopPreviewStartupWatchdog();
        StopPreviewStartupOverlay();
        StopPreviewFadeInTimer();
        if (!preserveReinitAnimation)
        {
            _isPreviewReinitAnimating = false;
            Logger.Log($"D3D11_RENDERER_REINIT_FLAG flag=false caller={nameof(ResetPreviewStartupTracking)}");
        }
        _previewStartupAttemptId = null;
        _previewStartupRequestedUtc = null;
        _previewRendererAttachedUtc = null;
        _previewFirstVisualUtc = null;
        _previewLastFailureReason = null;
        _previewStartupMissingSignals = null;
        _previewFirstVisualConfirmed = false;
        ResetPreviewSignalState();
        Interlocked.Exchange(ref _previewStartupFailureStopScheduled, 0);

        if (!keepRecoveryCount)
        {
            _previewRecoveryAttemptCount = 0;
        }

        if (!IsPreviewStartupTerminalState(_previewStartupState))
        {
            SetPreviewStartupState(PreviewStartupState.Idle);
        }
        else
        {
            _previewStartupState = PreviewStartupState.Idle;
        }
    }
}
