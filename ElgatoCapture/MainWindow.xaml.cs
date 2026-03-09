using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.Services;
using ElgatoCapture.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using WinRT.Interop;

namespace ElgatoCapture;

public sealed partial class MainWindow : Window, IAutomationWindowControl
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
    private static readonly TimeSpan PreviewStartupPlaybackAdvanceThreshold = TimeSpan.FromMilliseconds(33);
    private static readonly int PreviewStartupVisualTimeoutMs = ResolveStartupSetting(
        "ELGATOCAPTURE_PREVIEW_START_TIMEOUT_MS",
        PreviewStartupDefaultVisualTimeoutMs,
        PreviewStartupMinVisualTimeoutMs,
        PreviewStartupMaxVisualTimeoutMs);

    public MainViewModel ViewModel { get; }
    private readonly DispatcherQueue _dispatcherQueue;
    private SoftwareBitmapSource? _previewSource;
    private D3D11PreviewRenderer? _d3dRenderer;
    private SpriteVisual? _videoShadowVisual;
    private SpriteVisual? _controlBarShadowVisual;
    private DispatcherQueueTimer? _statsPollTimer;
    private Storyboard? _statsDockStoryboard;
    private Storyboard? _showStatsDockStoryboard;
    private Storyboard? _hideStatsDockStoryboard;
    private long _previewFramesArrived;
    private long _previewFramesDisplayed;
    private long _previewFramesDropped;
    private long _previewLastResizeLogTick;
    private long _previewLastPresentedTick;
    private int _windowCloseRequested;
    private int _windowCloseCleanupStarted;
    private long _previewMinPresentationIntervalMs;
    private readonly IAutomationDiagnosticsHub _automationDiagnosticsHub;
    private readonly NamedPipeAutomationServer _automationPipeServer;
    private int _automationServicesStarted;
    private int _deviceSelectionSyncQueued;
    private int _audioSelectionSyncQueued;
    private int _resolutionSelectionSyncQueued;
    private int _frameRateSelectionSyncQueued;
    private int _formatSelectionSyncQueued;
    private int _qualitySelectionSyncQueued;
    private int _presetSelectionSyncQueued;
    private int _splitEncodeSelectionSyncQueued;
    private readonly string _windowTitleBase;
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
    private bool _previewStartupExpectGpuDualSignals;
    private bool _previewGpuSignalMediaOpened;
    private bool _previewGpuSignalFirstFrame;
    private bool _previewGpuSignalPlaybackAdvancing;
    private PreviewStartupSignalFlags _previewStartupRequiredSignals = PreviewStartupSignalFlags.None;
    private PreviewStartupSignalFlags _previewStartupReceivedSignals = PreviewStartupSignalFlags.None;
    private PreviewStartupStrategy _previewStartupStrategy = PreviewStartupStrategy.None;
    private TimeSpan _previewStartupLastPlaybackPosition = TimeSpan.Zero;
    private long _previewStartupPositionEventCount;
    private bool _previewStartupPlaybackPositionInitialized;
    private int _previewStartupFailureStopScheduled;
    private long _previewStartupLastPositionDispatchTick;
    private bool _previewStopRequestedByUser;
    private bool _isPreviewReinitAnimating;
    private DispatcherQueueTimer? _previewFadeInTimer;
    private const int PreviewFadeInFrameThreshold = 3;
    private bool _isWindowClosing;
    private bool _toggleLabelsVisible;
    private bool _entranceAnimationPlayed;
    private double _savedPreviewVolume;
    private bool _isVolumeFadingIn;
    private bool _isSettingsShelfAnimating;
    private bool _captureSettingsNarrow;
    private const double ControlBarLabelThreshold = 900.0;
    private const int MinWindowWidth = 900;
    private const int MinWindowHeight = 500;
    private WndProcDelegate? _minSizeWndProc;
    private IntPtr _originalWndProc;
    private IntPtr _hwnd;
    private double _audioPeakHoldLevel;
    private long _audioPeakHoldTimestamp;
    private double _audioRangeMin = 1.0;
    private double _audioRangeMax;
    private long _audioRangeResetTimestamp;
    private double _audioMeterDisplayLevel;
    private double _audioMeterTargetLevel;
    private LinearGradientBrush? _audioMeterColorBrush;
    private LinearGradientBrush? _audioMeterGreyBrush;
    private DispatcherTimer? _audioMeterAnimationTimer;

    private const long AudioPeakHoldDurationMs = 1500;
    private const double AudioPeakHoldDecayPerSecond = 0.8;
    private const long AudioRangeWindowMs = 3000;

    private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)
        => Math.Abs(a - b) < tolerance;

    private static bool IsAutoFrameRateOption(FrameRateOption option)
        => option.Value <= 0 || option.FriendlyValue <= 0;

    private static bool IsPreviewStartupFailedState(PreviewStartupState state)
        => state == PreviewStartupState.Failed;

    private static bool IsPreviewStartupTerminalState(PreviewStartupState state)
        => state is PreviewStartupState.Idle or PreviewStartupState.Rendering or PreviewStartupState.Failed;

    private bool IsPreviewStartupSignalWindowActive()
        => ViewModel.IsPreviewing &&
           !_previewFirstVisualConfirmed &&
           _previewStartupState is PreviewStartupState.StartingSession or PreviewStartupState.RendererAttaching or PreviewStartupState.WaitingForFirstVisual;

    private static int ResolveStartupSetting(string envName, int fallbackValue, int minValue, int maxValue)
    {
        var raw = Environment.GetEnvironmentVariable(envName);
        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return fallbackValue;
        }

        return Math.Clamp(parsed, minValue, maxValue);
    }

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
        _previewStartupExpectGpuDualSignals = false;
        _previewGpuSignalMediaOpened = false;
        _previewGpuSignalFirstFrame = false;
        _previewGpuSignalPlaybackAdvancing = false;
        _previewStartupRequiredSignals = PreviewStartupSignalFlags.None;
        _previewStartupReceivedSignals = PreviewStartupSignalFlags.None;
        _previewStartupStrategy = PreviewStartupStrategy.None;
        _previewStartupLastPlaybackPosition = TimeSpan.Zero;
        _previewStartupPositionEventCount = 0;
        _previewStartupPlaybackPositionInitialized = false;
        Interlocked.Exchange(ref _previewStartupFailureStopScheduled, 0);
        Interlocked.Exchange(ref _previewStartupLastPositionDispatchTick, 0);

        SetPreviewStartupState(PreviewStartupState.StartingSession);
        Logger.Log(
            $"PREVIEW_START_REQUESTED attempt={_previewStartupAttemptId} " +
            $"device={ViewModel.SelectedDevice?.Name ?? "none"}");
    }

    private void ConfigurePreviewStartupSignals(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)
    {
        _previewStartupStrategy = strategy;
        _previewStartupRequiredSignals = requiredSignals;
        _previewStartupReceivedSignals = PreviewStartupSignalFlags.None;
        _previewGpuSignalMediaOpened = false;
        _previewGpuSignalFirstFrame = false;
        _previewGpuSignalPlaybackAdvancing = false;
        _previewStartupLastPlaybackPosition = TimeSpan.Zero;
        _previewStartupPositionEventCount = 0;
        _previewStartupPlaybackPositionInitialized = false;
        _previewStartupMissingSignals = BuildPreviewStartupMissingSignals();
        Interlocked.Exchange(ref _previewStartupLastPositionDispatchTick, 0);

        Logger.Log(
            $"PREVIEW_START_STRATEGY attempt={_previewStartupAttemptId ?? "none"} " +
            $"strategy={_previewStartupStrategy} required={BuildPreviewStartupSignalList(_previewStartupRequiredSignals)}");
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

    private string BuildPreviewStartupMissingSignals()
    {
        if (_previewStartupRequiredSignals == PreviewStartupSignalFlags.None)
        {
            return _previewFirstVisualConfirmed ? string.Empty : "FirstVisual";
        }

        var missing = _previewStartupRequiredSignals & ~_previewStartupReceivedSignals;
        return missing == PreviewStartupSignalFlags.None
            ? string.Empty
            : BuildPreviewStartupSignalList(missing);
    }

    private static string BuildPreviewStartupSignalList(PreviewStartupSignalFlags signals)
    {
        if (signals == PreviewStartupSignalFlags.None)
        {
            return "None";
        }

        var labels = new List<string>(4);
        if (signals.HasFlag(PreviewStartupSignalFlags.MediaOpened))
        {
            labels.Add("MediaOpened");
        }

        if (signals.HasFlag(PreviewStartupSignalFlags.FirstCaptureFrame))
        {
            labels.Add("FirstCaptureFrame");
        }

        if (signals.HasFlag(PreviewStartupSignalFlags.PlaybackAdvancing))
        {
            labels.Add("PlaybackAdvancing");
        }

        if (signals.HasFlag(PreviewStartupSignalFlags.FirstVisual))
        {
            labels.Add("FirstVisual");
        }

        return labels.Count == 0 ? "None" : string.Join("+", labels);
    }

    private void MarkGpuStartupSignal(PreviewStartupSignalFlags signal, string signalName)
    {
        if (!IsPreviewStartupSignalWindowActive() || !_previewStartupExpectGpuDualSignals)
        {
            return;
        }

        if ((_previewStartupReceivedSignals & signal) != 0)
        {
            return;
        }

        _previewStartupReceivedSignals |= signal;
        if (signal == PreviewStartupSignalFlags.MediaOpened)
        {
            _previewGpuSignalMediaOpened = true;
        }
        else if (signal == PreviewStartupSignalFlags.FirstCaptureFrame)
        {
            _previewGpuSignalFirstFrame = true;
        }
        else if (signal == PreviewStartupSignalFlags.PlaybackAdvancing)
        {
            _previewGpuSignalPlaybackAdvancing = true;
        }

        _previewStartupMissingSignals = BuildPreviewStartupMissingSignals();
        Logger.Log($"PREVIEW_START_SIGNAL signal={signalName} attempt={_previewStartupAttemptId ?? "none"}");
        LogPreviewStartupPlaybackSnapshot($"signal:{signalName}");
        TryConfirmPreviewFirstVisualFromGpuSignals();
    }

    private void MarkGpuStartupSignalMediaOpened()
    {
        MarkGpuStartupSignal(PreviewStartupSignalFlags.MediaOpened, "MediaOpened");
    }

    private void MarkGpuStartupSignalFirstFrame()
    {
        if (!IsPreviewStartupSignalWindowActive() || !_previewStartupExpectGpuDualSignals)
        {
            return;
        }

        MarkGpuStartupSignal(PreviewStartupSignalFlags.FirstCaptureFrame, "FirstCaptureFrame");
    }

    private void MarkGpuStartupSignalPlaybackAdvancing(TimeSpan position)
    {
        if (!IsPreviewStartupSignalWindowActive() || !_previewStartupExpectGpuDualSignals)
        {
            Logger.Log(
                $"PREVIEW_START_POSITION_IGNORED attempt={_previewStartupAttemptId ?? "none"} " +
                $"reason=inactive-or-not-gpu positionMs={position.TotalMilliseconds:0.###}");
            return;
        }

        if (!_previewStartupPlaybackPositionInitialized)
        {
            _previewStartupPlaybackPositionInitialized = true;
            _previewStartupLastPlaybackPosition = position;
            Logger.Log(
                $"PREVIEW_START_POSITION_BASELINE attempt={_previewStartupAttemptId ?? "none"} " +
                $"positionMs={position.TotalMilliseconds:0.###} thresholdMs={PreviewStartupPlaybackAdvanceThreshold.TotalMilliseconds:0.###}");
            if (position >= PreviewStartupPlaybackAdvanceThreshold)
            {
                MarkGpuStartupSignal(PreviewStartupSignalFlags.PlaybackAdvancing, "PlaybackAdvancing");
            }

            return;
        }

        var delta = position - _previewStartupLastPlaybackPosition;
        if (position > _previewStartupLastPlaybackPosition)
        {
            _previewStartupLastPlaybackPosition = position;
        }

        Logger.Log(
            $"PREVIEW_START_POSITION_CHECK attempt={_previewStartupAttemptId ?? "none"} " +
            $"positionMs={position.TotalMilliseconds:0.###} deltaMs={delta.TotalMilliseconds:0.###} " +
            $"thresholdMs={PreviewStartupPlaybackAdvanceThreshold.TotalMilliseconds:0.###}");
        if (position >= PreviewStartupPlaybackAdvanceThreshold || delta >= PreviewStartupPlaybackAdvanceThreshold)
        {
            MarkGpuStartupSignal(PreviewStartupSignalFlags.PlaybackAdvancing, "PlaybackAdvancing");
        }
    }

    private void TryConfirmPreviewFirstVisualFromGpuSignals()
    {
        if (!_previewStartupExpectGpuDualSignals)
        {
            return;
        }

        var missing = _previewStartupRequiredSignals & ~_previewStartupReceivedSignals;
        if (missing != PreviewStartupSignalFlags.None)
        {
            Logger.Log(
                $"PREVIEW_START_WAITING attempt={_previewStartupAttemptId ?? "none"} " +
                $"required={BuildPreviewStartupSignalList(_previewStartupRequiredSignals)} " +
                $"received={BuildPreviewStartupSignalList(_previewStartupReceivedSignals)} " +
                $"missing={BuildPreviewStartupSignalList(missing)}");
            return;
        }

        ConfirmPreviewFirstVisual($"GpuStartupSignals({BuildPreviewStartupSignalList(_previewStartupRequiredSignals)})");
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

    private void LogPreviewStartupPlaybackSnapshot(string reason)
    {
        var renderer = _d3dRenderer;
        if (renderer == null)
        {
            Logger.Log(
                $"PREVIEW_START_PLAYBACK_SNAPSHOT attempt={_previewStartupAttemptId ?? "none"} " +
                $"reason={reason} renderer=null");
            return;
        }

        Logger.Log(
            $"PREVIEW_START_PLAYBACK_SNAPSHOT attempt={_previewStartupAttemptId ?? "none"} " +
            $"reason={reason} state={(renderer.IsRendering ? "Rendering" : "Idle")} " +
            $"positionMs=0 " +
            $"gpuVisible={PreviewSwapChainPanel.Visibility} " +
            $"required={BuildPreviewStartupSignalList(_previewStartupRequiredSignals)} " +
            $"received={BuildPreviewStartupSignalList(_previewStartupReceivedSignals)} " +
            $"missing={BuildPreviewStartupMissingSignals()}");
    }

    private void EnsurePreviewPlaybackStarted(string reason, bool recoveryAttempt)
    {
        Logger.Log(
            $"PREVIEW_START_PLAY_SKIPPED attempt={_previewStartupAttemptId ?? "none"} " +
            $"reason={reason} mode=D3D11");
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
                await ViewModel.StopPreviewAsync(userInitiated: true);
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
        }
        _previewStartupMissingSignals = string.Empty;
        var elapsedMs = _previewStartupRequestedUtc.HasValue
            ? (DateTimeOffset.UtcNow - _previewStartupRequestedUtc.Value).TotalMilliseconds
            : 0;
        Logger.Log(
            $"PREVIEW_FIRST_VISUAL_CONFIRMED attempt={_previewStartupAttemptId ?? "none"} " +
            $"source={source} elapsedMs={elapsedMs:0} recovery={_previewRecoveryAttemptCount}");
    }

    private void SchedulePreviewFadeIn()
    {
        StopPreviewFadeInTimer();

        var renderer = _d3dRenderer;
        if (renderer == null)
        {
            // CPU fallback path — no frame counter, just animate in after a short delay
            _previewFadeInTimer = _dispatcherQueue.CreateTimer();
            _previewFadeInTimer.Interval = TimeSpan.FromMilliseconds(50);
            _previewFadeInTimer.IsRepeating = false;
            _previewFadeInTimer.Tick += (_, _) =>
            {
                StopPreviewFadeInTimer();
                _ = AnimatePreviewInAsync();
            };
            _previewFadeInTimer.Start();
            return;
        }

        // Wait until the renderer has rendered enough frames for the signal to stabilize.
        // Poll every ~16ms (one vsync) and check FramesRendered.
        var baselineFrames = renderer.FramesRendered;
        _previewFadeInTimer = _dispatcherQueue.CreateTimer();
        _previewFadeInTimer.Interval = TimeSpan.FromMilliseconds(16);
        _previewFadeInTimer.IsRepeating = true;
        _previewFadeInTimer.Tick += (_, _) =>
        {
            var current = _d3dRenderer;
            if (current == null || current != renderer)
            {
                // Renderer changed or gone — fade in now to avoid being stuck invisible
                StopPreviewFadeInTimer();
                _ = AnimatePreviewInAsync();
                return;
            }

            var rendered = current.FramesRendered - baselineFrames;
            if (rendered >= PreviewFadeInFrameThreshold)
            {
                StopPreviewFadeInTimer();
                Logger.Log($"PREVIEW_FADE_IN_READY framesRendered={rendered} baseline={baselineFrames}");
                _ = AnimatePreviewInAsync();
            }
        };
        _previewFadeInTimer.Start();
    }

    private void StopPreviewFadeInTimer()
    {
        _previewFadeInTimer?.Stop();
        _previewFadeInTimer = null;
    }

    private void ResetPreviewStartupTracking(bool keepRecoveryCount = false, bool preserveReinitAnimation = false)
    {
        StopPreviewStartupWatchdog();
        StopPreviewStartupOverlay();
        StopPreviewFadeInTimer();
        if (!preserveReinitAnimation)
        {
            _isPreviewReinitAnimating = false;
        }
        _previewStartupAttemptId = null;
        _previewStartupRequestedUtc = null;
        _previewRendererAttachedUtc = null;
        _previewFirstVisualUtc = null;
        _previewLastFailureReason = null;
        _previewStartupMissingSignals = null;
        _previewFirstVisualConfirmed = false;
        _previewStartupExpectGpuDualSignals = false;
        _previewGpuSignalMediaOpened = false;
        _previewGpuSignalFirstFrame = false;
        _previewGpuSignalPlaybackAdvancing = false;
        _previewStartupRequiredSignals = PreviewStartupSignalFlags.None;
        _previewStartupReceivedSignals = PreviewStartupSignalFlags.None;
        _previewStartupStrategy = PreviewStartupStrategy.None;
        _previewStartupLastPlaybackPosition = TimeSpan.Zero;
        _previewStartupPositionEventCount = 0;
        _previewStartupPlaybackPositionInitialized = false;
        Interlocked.Exchange(ref _previewStartupFailureStopScheduled, 0);
        Interlocked.Exchange(ref _previewStartupLastPositionDispatchTick, 0);

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

    private void OnD3DRendererFirstFrameRendered()
    {
        Logger.Log($"PREVIEW_D3D_FIRST_FRAME attempt={_previewStartupAttemptId ?? "none"}");
        ConfirmPreviewFirstVisual("D3D11FirstFrame");
    }

    private void OnPreviewSwapChainPanelSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
    {
        // Composition transform only — overlay sizing is driven by the container.
        var scale = PreviewSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;
        _d3dRenderer?.OnPanelSizeChanged(e.NewSize.Width, e.NewSize.Height, scale);
    }

    private void OnPreviewContentGridSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs e)
    {
        UpdateVideoContentOverlays();
    }

    private void UpdateVideoContentOverlays()
    {
        var srcW = (double)(ViewModel.SourceWidth ?? 0);
        var srcH = (double)(ViewModel.SourceHeight ?? 0);
        // Use the container (PreviewContentGrid) size, not the SwapChainPanel,
        // because the panel is now explicitly sized to fitW x fitH.
        var dstW = PreviewContentGrid.ActualWidth;
        var dstH = PreviewContentGrid.ActualHeight;

        if (dstW <= 0 || dstH <= 0)
        {
            RecordingGlowBorder.Margin = new Thickness(0);
            if (_videoShadowVisual != null) _videoShadowVisual.Size = Vector2.Zero;
            return;
        }

        double fitW, fitH;
        if (srcW <= 0 || srcH <= 0)
        {
            // Source dimensions unknown — fill the container (same as old Stretch behavior).
            fitW = dstW;
            fitH = dstH;
        }
        else
        {
            var srcAspect = srcW / srcH;
            var dstAspect = dstW / dstH;

            if (srcAspect > dstAspect)
            {
                fitW = dstW;
                fitH = dstW / srcAspect;
            }
            else
            {
                fitH = dstH;
                fitW = dstH * srcAspect;
            }
        }

        // Resize SwapChainPanel to exactly the video content area (no letterbox).
        PreviewSwapChainPanel.Width = fitW;
        PreviewSwapChainPanel.Height = fitH;

        var marginH = (dstW - fitW) / 2;
        var marginV = (dstH - fitH) / 2;
        var videoMargin = new Thickness(marginH, marginV, marginH, marginV);
        RecordingGlowBorder.Margin = videoMargin;

        // Update shadow visual to match the video content rect.
        // VideoShadowHost is a sibling of PreviewBorder — shadow casts onto app background.
        if (_videoShadowVisual != null)
        {
            const float borderMarginH = 12f; // PreviewBorder Margin left/right
            const float borderMarginV = 6f;  // PreviewBorder Margin top/bottom
            const float hostMargin = 16f;    // PreviewShadowHost Margin
            _videoShadowVisual.Offset = new Vector3(
                borderMarginH + hostMargin + (float)marginH,
                borderMarginV + hostMargin + (float)marginV, 0);
            _videoShadowVisual.Size = new Vector2(Math.Max(0, (float)fitW), Math.Max(0, (float)fitH));
        }
    }

    private void SetupVideoFrameShadow()
    {
        var compositor = ElementCompositionPreview.GetElementVisual(VideoShadowHost).Compositor;

        var shadow = compositor.CreateDropShadow();
        shadow.BlurRadius = 16;
        shadow.Color = Windows.UI.Color.FromArgb(160, 0, 0, 0);
        shadow.Offset = new Vector3(0, 2, 0);
        shadow.Mask = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));

        var spriteVisual = compositor.CreateSpriteVisual();
        spriteVisual.Shadow = shadow;

        spriteVisual.Opacity = 0f; // Start invisible — faded in with preview entrance
        _videoShadowVisual = spriteVisual;
        ElementCompositionPreview.SetElementChildVisual(VideoShadowHost, spriteVisual);
    }

    private void SetupControlBarShadow()
    {
        var compositor = ElementCompositionPreview.GetElementVisual(ControlBarShadowHost).Compositor;

        var shadow = compositor.CreateDropShadow();
        shadow.BlurRadius = 12;
        shadow.Color = Windows.UI.Color.FromArgb(120, 0, 0, 0);
        shadow.Offset = new Vector3(0, 1, 0);
        shadow.Mask = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));

        var spriteVisual = compositor.CreateSpriteVisual();
        spriteVisual.Shadow = shadow;
        spriteVisual.Opacity = 0f; // Start invisible — faded in with control bar entrance

        _controlBarShadowVisual = spriteVisual;
        ElementCompositionPreview.SetElementChildVisual(ControlBarShadowHost, spriteVisual);

        // Track control bar size changes to keep the shadow aligned.
        ControlBarBorder.SizeChanged += (s, e) =>
        {
            if (_controlBarShadowVisual == null) return;
            var margin = ControlBarBorder.Margin;
            _controlBarShadowVisual.Offset = new Vector3((float)margin.Left, (float)margin.Top, 0);
            _controlBarShadowVisual.Size = new Vector2((float)e.NewSize.Width, (float)e.NewSize.Height);
        };
    }

    private void SetGpuPreviewVisibility(Visibility visibility)
    {
        // PreviewLetterboxBackground stays Collapsed — letterbox areas must be
        // transparent so the Composition DropShadow is visible around the video.
        PreviewSwapChainPanel.Visibility = visibility;
    }

    private void EnsureDeviceSelection()
    {
        if (ViewModel.Devices.Count == 0)
        {
            DeviceComboBox.SelectedItem = null;
            return;
        }

        var matchingDevice = ViewModel.SelectedDevice != null
            ? ViewModel.Devices.FirstOrDefault(device =>
                string.Equals(device.Id, ViewModel.SelectedDevice.Id, StringComparison.OrdinalIgnoreCase))
            : null;
        matchingDevice ??= ViewModel.Devices.FirstOrDefault();
        if (matchingDevice == null)
        {
            return;
        }

        if (!ReferenceEquals(ViewModel.SelectedDevice, matchingDevice))
        {
            ViewModel.SelectedDevice = matchingDevice;
        }

        if (!ReferenceEquals(DeviceComboBox.SelectedItem, matchingDevice))
        {
            DeviceComboBox.SelectedItem = matchingDevice;
        }
    }

    private void EnsureAudioInputSelection()
    {
        if (ViewModel.AudioInputDevices.Count == 0)
        {
            AudioInputComboBox.SelectedItem = null;
            return;
        }

        var matchingDevice = ViewModel.SelectedAudioInputDevice != null
            ? ViewModel.AudioInputDevices.FirstOrDefault(device =>
                string.Equals(device.Id, ViewModel.SelectedAudioInputDevice.Id, StringComparison.OrdinalIgnoreCase))
            : null;
        matchingDevice ??= ViewModel.AudioInputDevices.FirstOrDefault();
        if (matchingDevice == null)
        {
            return;
        }

        if (!ReferenceEquals(ViewModel.SelectedAudioInputDevice, matchingDevice))
        {
            ViewModel.SelectedAudioInputDevice = matchingDevice;
        }

        if (!ReferenceEquals(AudioInputComboBox.SelectedItem, matchingDevice))
        {
            AudioInputComboBox.SelectedItem = matchingDevice;
        }
    }

    private void EnsureResolutionSelection()
    {
        if (ViewModel.AvailableResolutions.Count == 0)
        {
            if (ViewModel.SelectedDevice == null || !ViewModel.IsPreviewing)
            {
                ResolutionComboBox.SelectedItem = null;
            }

            return;
        }

        var matchingResolution = ViewModel.AvailableResolutions.FirstOrDefault(option =>
            string.Equals(option.Value, ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase))
            ?? ViewModel.AvailableResolutions.FirstOrDefault(option => option.IsEnabled)
            ?? ViewModel.AvailableResolutions.FirstOrDefault();
        if (matchingResolution == null)
        {
            return;
        }

        if (!string.Equals(matchingResolution.Value, ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase))
        {
            ViewModel.SelectedResolution = matchingResolution.Value;
        }

        if (ResolutionComboBox.SelectedItem is not ResolutionOption selectedResolutionOption ||
            !string.Equals(selectedResolutionOption.Value, matchingResolution.Value, StringComparison.OrdinalIgnoreCase))
        {
            ResolutionComboBox.SelectedItem = matchingResolution;
        }
    }

    private void EnsureFrameRateSelection()
    {
        if (ViewModel.AvailableFrameRates.Count == 0)
        {
            if (ViewModel.SelectedDevice == null || !ViewModel.IsPreviewing)
            {
                FrameRateComboBox.SelectedItem = null;
            }

            return;
        }

        if (ViewModel.IsAutoFrameRateSelected)
        {
            var autoOption = ViewModel.AvailableFrameRates
                .FirstOrDefault(IsAutoFrameRateOption);
            if (autoOption != null)
            {
                if (!ReferenceEquals(FrameRateComboBox.SelectedItem, autoOption))
                {
                    FrameRateComboBox.SelectedItem = autoOption;
                }

                return;
            }
        }

        var matchingRate = ViewModel.AvailableFrameRates
            .FirstOrDefault(option => IsFrameRateMatch(option.Value, ViewModel.SelectedFrameRate))
            ?? ViewModel.AvailableFrameRates.FirstOrDefault(option => option.IsEnabled)
            ?? ViewModel.AvailableFrameRates.FirstOrDefault();
        if (matchingRate == null)
        {
            return;
        }

        if (!IsFrameRateMatch(matchingRate.Value, ViewModel.SelectedFrameRate))
        {
            ViewModel.SelectedFrameRate = matchingRate.Value;
        }

        if (FrameRateComboBox.SelectedItem is not FrameRateOption currentFps ||
            !IsFrameRateMatch(currentFps.Value, matchingRate.Value))
        {
            FrameRateComboBox.SelectedItem = matchingRate;
        }
    }

    private void EnsureFormatSelection()
    {
        if (ViewModel.AvailableRecordingFormats.Count == 0)
        {
            if (ViewModel.SelectedDevice == null || !ViewModel.IsPreviewing)
            {
                FormatComboBox.SelectedItem = null;
            }

            return;
        }

        var matchingFormat = ViewModel.AvailableRecordingFormats
            .FirstOrDefault(format => string.Equals(format, ViewModel.SelectedRecordingFormat, StringComparison.OrdinalIgnoreCase))
            ?? ViewModel.AvailableRecordingFormats.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(matchingFormat))
        {
            return;
        }

        if (!string.Equals(matchingFormat, ViewModel.SelectedRecordingFormat, StringComparison.OrdinalIgnoreCase))
        {
            ViewModel.SelectedRecordingFormat = matchingFormat;
        }

        if (!string.Equals(FormatComboBox.SelectedItem as string, matchingFormat, StringComparison.OrdinalIgnoreCase))
        {
            FormatComboBox.SelectedItem = matchingFormat;
        }
    }

    private void EnsureQualitySelection()
    {
        if (ViewModel.AvailableQualities.Count == 0)
        {
            QualityComboBox.SelectedItem = null;
            return;
        }

        var matchingQuality = ViewModel.AvailableQualities
            .FirstOrDefault(quality => string.Equals(quality, ViewModel.SelectedQuality, StringComparison.OrdinalIgnoreCase))
            ?? ViewModel.AvailableQualities.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(matchingQuality))
        {
            return;
        }

        if (!string.Equals(matchingQuality, ViewModel.SelectedQuality, StringComparison.OrdinalIgnoreCase))
        {
            ViewModel.SelectedQuality = matchingQuality;
        }

        if (!string.Equals(QualityComboBox.SelectedItem as string, matchingQuality, StringComparison.OrdinalIgnoreCase))
        {
            QualityComboBox.SelectedItem = matchingQuality;
        }
    }

    private void EnsurePresetSelection()
    {
        if (ViewModel.AvailablePresets.Count == 0)
        {
            PresetComboBox.SelectedItem = null;
            return;
        }

        var matchingPreset = ViewModel.AvailablePresets
            .FirstOrDefault(preset => string.Equals(preset, ViewModel.SelectedPreset, StringComparison.OrdinalIgnoreCase))
            ?? ViewModel.AvailablePresets.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(matchingPreset))
        {
            return;
        }

        if (!string.Equals(matchingPreset, ViewModel.SelectedPreset, StringComparison.OrdinalIgnoreCase))
        {
            ViewModel.SelectedPreset = matchingPreset;
        }

        if (!string.Equals(PresetComboBox.SelectedItem as string, matchingPreset, StringComparison.OrdinalIgnoreCase))
        {
            PresetComboBox.SelectedItem = matchingPreset;
        }
    }

    private void EnsureSplitEncodeModeSelection()
    {
        if (ViewModel.AvailableSplitEncodeModes.Count == 0)
        {
            SplitEncodeComboBox.SelectedItem = null;
            return;
        }

        var matchingMode = ViewModel.AvailableSplitEncodeModes
            .FirstOrDefault(mode => string.Equals(mode, ViewModel.SelectedSplitEncodeMode, StringComparison.OrdinalIgnoreCase))
            ?? ViewModel.AvailableSplitEncodeModes.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(matchingMode))
        {
            return;
        }

        if (!string.Equals(matchingMode, ViewModel.SelectedSplitEncodeMode, StringComparison.OrdinalIgnoreCase))
        {
            ViewModel.SelectedSplitEncodeMode = matchingMode;
        }

        if (!string.Equals(SplitEncodeComboBox.SelectedItem as string, matchingMode, StringComparison.OrdinalIgnoreCase))
        {
            SplitEncodeComboBox.SelectedItem = matchingMode;
        }
    }

    private void QueueDeviceSelectionSync()
    {
        if (Interlocked.Exchange(ref _deviceSelectionSyncQueued, 1) != 0)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                EnsureDeviceSelection();
            }
            finally
            {
                Interlocked.Exchange(ref _deviceSelectionSyncQueued, 0);
            }
        });
    }

    private void QueueAudioSelectionSync()
    {
        if (Interlocked.Exchange(ref _audioSelectionSyncQueued, 1) != 0)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                EnsureAudioInputSelection();
            }
            finally
            {
                Interlocked.Exchange(ref _audioSelectionSyncQueued, 0);
            }
        });
    }

    private void QueueResolutionSelectionSync()
    {
        if (Interlocked.Exchange(ref _resolutionSelectionSyncQueued, 1) != 0)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                EnsureResolutionSelection();
            }
            finally
            {
                Interlocked.Exchange(ref _resolutionSelectionSyncQueued, 0);
            }
        });
    }

    private void QueueFrameRateSelectionSync()
    {
        if (Interlocked.Exchange(ref _frameRateSelectionSyncQueued, 1) != 0)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                EnsureFrameRateSelection();
            }
            finally
            {
                Interlocked.Exchange(ref _frameRateSelectionSyncQueued, 0);
            }
        });
    }

    private void QueueFormatSelectionSync()
    {
        if (Interlocked.Exchange(ref _formatSelectionSyncQueued, 1) != 0)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                EnsureFormatSelection();
            }
            finally
            {
                Interlocked.Exchange(ref _formatSelectionSyncQueued, 0);
            }
        });
    }

    private void QueueQualitySelectionSync()
    {
        if (Interlocked.Exchange(ref _qualitySelectionSyncQueued, 1) != 0)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                EnsureQualitySelection();
            }
            finally
            {
                Interlocked.Exchange(ref _qualitySelectionSyncQueued, 0);
            }
        });
    }

    private void QueuePresetSelectionSync()
    {
        if (Interlocked.Exchange(ref _presetSelectionSyncQueued, 1) != 0)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                EnsurePresetSelection();
            }
            finally
            {
                Interlocked.Exchange(ref _presetSelectionSyncQueued, 0);
            }
        });
    }

    private void QueueSplitEncodeModeSelectionSync()
    {
        if (Interlocked.Exchange(ref _splitEncodeSelectionSyncQueued, 1) != 0)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                EnsureSplitEncodeModeSelection();
            }
            finally
            {
                Interlocked.Exchange(ref _splitEncodeSelectionSyncQueued, 0);
            }
        });
    }

    private long ResolvePreviewExpectedIntervalMs()
    {
        var sourceFps = ViewModel.SelectedFormat?.FrameRateExact ?? 0;
        if (sourceFps <= 0)
        {
            sourceFps = 60;
        }

        return Math.Max(1L, (long)Math.Round(1000.0 / sourceFps));
    }

    private static bool IsHdrSubtype(string? subtype)
        => MediaFormat.IsHdrPixelFormat(subtype);

    private static string BuildWindowTitleBase()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            return "SimpleCapture";
        }

        var buildTime = File.GetLastWriteTime(exePath);
        if (buildTime == DateTime.MinValue)
        {
            return "SimpleCapture";
        }

        return $"SimpleCapture (build {buildTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)})";
    }

    private void ApplyWindowTitle()
    {
        if (ViewModel.IsRecording)
        {
            Title = $"{_windowTitleBase} - REC {ViewModel.RecordingTime}";
            return;
        }

        Title = _windowTitleBase;
    }

    public MainWindow()
    {
        InitializeComponent();

        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        ViewModel = new MainViewModel();
        _windowTitleBase = BuildWindowTitleBase();
        ApplyWindowTitle();
        var automationToken = Environment.GetEnvironmentVariable("ELGATOCAPTURE_AUTOMATION_TOKEN");
        var automationPipeName = Environment.GetEnvironmentVariable("ELGATOCAPTURE_AUTOMATION_PIPE");
        if (string.IsNullOrWhiteSpace(automationPipeName))
        {
            automationPipeName = "ElgatoCaptureAutomation";
        }

        _automationDiagnosticsHub = new AutomationDiagnosticsHub(
            ViewModel,
            GetPreviewRuntimeSnapshotAsync,
            new RecordingVerifier());
        var automationDispatcher = new AutomationCommandDispatcher(
            ViewModel,
            _automationDiagnosticsHub,
            this,
            automationToken);
        _automationPipeServer = new NamedPipeAutomationServer(automationDispatcher, automationPipeName);
        _previewMinPresentationIntervalMs = ResolvePreviewExpectedIntervalMs();

        // Set window handle for folder picker
        _hwnd = WindowNative.GetWindowHandle(this);
        ViewModel.SetWindowHandle(_hwnd);

        // Cloak the window to prevent white flash before XAML renders
        int cloakTrue = 1;
        DwmSetWindowAttribute(_hwnd, DWMWA_CLOAK, ref cloakTrue, sizeof(int));
        int darkMode = 1;
        DwmSetWindowAttribute(_hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

        // Enforce minimum window size via WM_GETMINMAXINFO
        _minSizeWndProc = MinSizeWndProc;
        _originalWndProc = GetWindowLongPtr(_hwnd, GWLP_WNDPROC);
        SetWindowLongPtr(_hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_minSizeWndProc));

        // Set initial window size and constraints
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
            Microsoft.UI.Win32Interop.GetWindowIdFromWindow(_hwnd));

        // Ensure window is not maximized
        if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
        {
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;

            // Force normal (non-maximized) state
            presenter.Restore();
        }

        // Set window size to accommodate 1920x1080 preview + UI controls
        // Height calculation: 1080px video + ~250px UI controls + ~120px padding/spacing/titlebar
        appWindow.Resize(new Windows.Graphics.SizeInt32(1950, 1450));

        // Set title bar icon
        appWindow.SetIcon("Assets\\AppIcon.ico");

        // Subscribe to ViewModel changes
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        ViewModel.PreviewStartRequested += ViewModel_PreviewStartRequested;
        ViewModel.PreviewStopRequested += ViewModel_PreviewStopRequested;
        ViewModel.PreviewReinitRequested += ViewModel_PreviewReinitRequested;

        // Wire up UI controls to ViewModel
        SetupBindings();
        SetupButtonHoverAnimations();
        SetupControlBarShadow();

        // Entrance animation: hide everything initially
        ControlBarBorder.Opacity = 0;
        ControlBarBorder.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 1.0);
        ControlBarBorder.RenderTransform = new TranslateTransform { Y = 16 };
        StatsRow.Opacity = 0;
        StatsRow.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0);
        StatsRow.RenderTransform = new TranslateTransform { Y = -8 };
        PreviewBorder.Opacity = 0;
        PreviewBorderScale.ScaleX = 0.97;
        PreviewBorderScale.ScaleY = 0.97;

        var entranceButtons = GetEntranceButtons();
        foreach (var button in entranceButtons)
        {
            button.Opacity = 0;
            if (button.RenderTransform is ScaleTransform transform)
            {
                transform.ScaleX = 0.85;
                transform.ScaleY = 0.85;
            }
        }

        // Shadow for control bar depth effect
        var shadow = new Microsoft.UI.Xaml.Media.ThemeShadow();
        shadow.Receivers.Add(SettingsOverlayPanel);
        ControlBarBorder.Shadow = shadow;
        ControlBarBorder.Translation = new System.Numerics.Vector3(0, 0, 32);

        // Record button: floating elevation with shadow
        var recShadow = new Microsoft.UI.Xaml.Media.ThemeShadow();
        RecordButton.Shadow = recShadow;
        RecordButton.Translation = new System.Numerics.Vector3(0, 0, 16);

        // Refresh devices on load - use Loaded event to ensure XAML is fully parsed
        var mainContent = (FrameworkElement)this.Content;
        mainContent.Loaded += MainWindow_Loaded;
        mainContent.SizeChanged += MainWindow_SizeChanged;
        Closed += MainWindow_Closed;

    }

    private void SetupBindings()
    {
        InitializeAudioMeterBrushes();

        // Bind all collections to ComboBoxes
        DeviceComboBox.ItemsSource = ViewModel.Devices;
        AudioInputComboBox.ItemsSource = ViewModel.AudioInputDevices;
        ResolutionComboBox.ItemsSource = ViewModel.AvailableResolutions;
        FrameRateComboBox.ItemsSource = ViewModel.AvailableFrameRates;
        FormatComboBox.ItemsSource = ViewModel.AvailableRecordingFormats;
        QualityComboBox.ItemsSource = ViewModel.AvailableQualities;
        PresetComboBox.ItemsSource = ViewModel.AvailablePresets;
        SplitEncodeComboBox.ItemsSource = ViewModel.AvailableSplitEncodeModes;

        ViewModel.Devices.CollectionChanged += (s, e) =>
        {
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                return;
            }

            QueueDeviceSelectionSync();
        };

        ViewModel.AudioInputDevices.CollectionChanged += (s, e) =>
        {
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                return;
            }

            QueueAudioSelectionSync();
        };

        ViewModel.AvailableResolutions.CollectionChanged += (s, e) =>
        {
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                return;
            }

            QueueResolutionSelectionSync();
        };

        // Subscribe to collection changes to sync SelectedItem after items are added
        ViewModel.AvailableFrameRates.CollectionChanged += (s, e) =>
        {
            // After items are added, sync the selected frame rate
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add ||
                e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset ||
                e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                QueueFrameRateSelectionSync();
            }
        };

        ViewModel.AvailableRecordingFormats.CollectionChanged += (s, e) =>
        {
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                return;
            }

            QueueFormatSelectionSync();
        };

        ViewModel.AvailableQualities.CollectionChanged += (s, e) =>
        {
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                return;
            }

            QueueQualitySelectionSync();
        };

        ViewModel.AvailablePresets.CollectionChanged += (s, e) =>
        {
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                return;
            }

            QueuePresetSelectionSync();
        };

        ViewModel.AvailableSplitEncodeModes.CollectionChanged += (s, e) =>
        {
            if (e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Add &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Reset &&
                e.Action != System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                return;
            }

            QueueSplitEncodeModeSelectionSync();
        };

        // Set initial values
        OutputPathTextBox.Text = ViewModel.OutputPath;
        DiskSpaceTextBlock.Text = ViewModel.DiskSpaceInfo;
        RecordingSizeTextBlock.Text = ViewModel.RecordingSizeInfo;
        RecordingBitrateTextBlock.Text = ViewModel.RecordingBitrateInfo;
        LiveResolutionTextBlock.Text = ViewModel.LiveResolution;
        LiveFrameRateTextBlock.Text = ViewModel.LiveFrameRate;
        LivePixelFormatTextBlock.Text = ViewModel.LivePixelFormat;
        AudioRecordToggle.IsChecked = ViewModel.IsAudioEnabled;
        AudioPreviewToggle.IsChecked = ViewModel.IsAudioPreviewEnabled;
        AudioPreviewToggle.IsEnabled = ViewModel.IsAudioEnabled;
        // Save the user's preferred volume, start at 0 for fade-in
        _savedPreviewVolume = ViewModel.PreviewVolume;
        _isVolumeFadingIn = true;
        ViewModel.VolumeSaveOverride = _savedPreviewVolume;
        ViewModel.SuppressVolumeSave = true;
        ViewModel.PreviewVolume = 0;
        ViewModel.SuppressVolumeSave = false;
        PreviewVolumeSlider.Value = 0;
        PreviewVolumeLabel.Text = "0%";
        PreviewVolumeSlider.ValueChanged += (s, e) =>
        {
            ViewModel.PreviewVolume = e.NewValue / 100.0;
            PreviewVolumeLabel.Text = $"{(int)e.NewValue}%";
        };
        PreviewVolumeSlider.PointerCaptureLost += (s, e) =>
        {
            if (!_isVolumeFadingIn)
            {
                ViewModel.SavePreviewVolume();
            }
        };
        CustomAudioToggle.IsOn = ViewModel.IsCustomAudioInputEnabled;
        CustomAudioToggle.IsEnabled = !ViewModel.IsRecording;
        ShowAllCaptureOptionsToggle.IsOn = ViewModel.ShowAllCaptureOptions;
        var customAudioVisible = ViewModel.IsCustomAudioInputEnabled ? Visibility.Visible : Visibility.Collapsed;
        AudioInputLabel.Visibility = customAudioVisible;
        AudioInputComboBox.Visibility = customAudioVisible;
        AudioInputComboBox.SelectedItem = ViewModel.SelectedAudioInputDevice;
        AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled && !ViewModel.IsRecording;
        FormatComboBox.SelectedItem = ViewModel.SelectedRecordingFormat;
        QualityComboBox.SelectedItem = ViewModel.SelectedQuality;
        PresetComboBox.SelectedItem = ViewModel.SelectedPreset;
        SplitEncodeComboBox.SelectedItem = ViewModel.SelectedSplitEncodeMode;
        CustomBitrateNumberBox.Value = ViewModel.CustomBitrateMbps;
        CustomBitratePanel.Visibility = ViewModel.IsCustomBitrateVisible ? Visibility.Visible : Visibility.Collapsed;
        HdrToggle.IsChecked = ViewModel.IsHdrEnabled;
        HdrToggle.IsEnabled = ViewModel.IsHdrAvailable &&
                              !ViewModel.IsRecording &&
                              ViewModel.SourceIsHdr != false;
        TrueHdrPreviewToggle.IsChecked = ViewModel.IsTrueHdrPreviewEnabled;
        TrueHdrPreviewToggle.IsEnabled = ViewModel.IsHdrEnabled && !ViewModel.IsRecording;
        ResetAudioMeterVisuals();
        _audioMeterTargetLevel = Math.Clamp(ViewModel.AudioPeak, 0.0, 1.0);
        AudioClipText.Visibility = ViewModel.AudioClipping ? Visibility.Visible : Visibility.Collapsed;
        RecordButton.IsEnabled = !ViewModel.IsFfmpegMissing;
        RefreshHdrHintText();
        UpdateFpsTelemetryTooltip();
        EnsureDeviceSelection();
        EnsureAudioInputSelection();
        EnsureResolutionSelection();
        EnsureFrameRateSelection();
        EnsureFormatSelection();
        EnsureQualitySelection();
        EnsurePresetSelection();
        EnsureSplitEncodeModeSelection();

        // Wire up selection changes with loop prevention
        DeviceComboBox.SelectionChanged += (s, e) =>
        {
            if (DeviceComboBox.SelectedItem != null &&
                DeviceComboBox.SelectedItem != ViewModel.SelectedDevice)
            {
                ViewModel.SelectedDevice = (ElgatoCapture.Models.CaptureDevice)DeviceComboBox.SelectedItem;
            }
        };

        AudioInputComboBox.SelectionChanged += (s, e) =>
        {
            if (AudioInputComboBox.SelectedItem is ElgatoCapture.Models.AudioInputDevice device &&
                device != ViewModel.SelectedAudioInputDevice)
            {
                ViewModel.SelectedAudioInputDevice = device;
            }
        };

        ResolutionComboBox.SelectionChanged += (s, e) =>
        {
            if (ResolutionComboBox.SelectedItem is ResolutionOption resolution &&
                resolution.IsEnabled &&
                !string.Equals(resolution.Value, ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase))
            {
                ViewModel.SelectedResolution = resolution.Value;
            }
        };

        FrameRateComboBox.SelectionChanged += (s, e) =>
        {
            if (FrameRateComboBox.SelectedItem is FrameRateOption frameRate &&
                frameRate.IsEnabled)
            {
                if (IsAutoFrameRateOption(frameRate))
                {
                    if (!ViewModel.IsAutoFrameRateSelected)
                    {
                        ViewModel.SelectedFrameRate = frameRate.Value;
                    }
                }
                else if (!IsFrameRateMatch(frameRate.Value, ViewModel.SelectedFrameRate))
                {
                    ViewModel.SelectedFrameRate = frameRate.Value;
                }
            }
        };

        FormatComboBox.SelectionChanged += (s, e) =>
        {
            if (FormatComboBox.SelectedItem is string format)
            {
                ViewModel.SelectedRecordingFormat = format;
            }
        };

        QualityComboBox.SelectionChanged += (s, e) =>
        {
            if (QualityComboBox.SelectedItem is string quality)
            {
                ViewModel.SelectedQuality = quality;
            }
        };

        PresetComboBox.SelectionChanged += (s, e) =>
        {
            if (PresetComboBox.SelectedItem is string preset)
            {
                ViewModel.SelectedPreset = preset;
            }
        };

        SplitEncodeComboBox.SelectionChanged += (s, e) =>
        {
            if (SplitEncodeComboBox.SelectedItem is string splitMode)
            {
                ViewModel.SelectedSplitEncodeMode = splitMode;
            }
        };

        CustomBitrateNumberBox.ValueChanged += (s, e) =>
        {
            if (!double.IsNaN(CustomBitrateNumberBox.Value))
            {
                ViewModel.CustomBitrateMbps = CustomBitrateNumberBox.Value;
            }
        };

        HdrToggle.Click += (s, e) => ViewModel.IsHdrEnabled = HdrToggle.IsChecked == true;
        TrueHdrPreviewToggle.Click += (s, e) =>
            ViewModel.IsTrueHdrPreviewEnabled = TrueHdrPreviewToggle.IsChecked == true;
        AudioRecordToggle.Checked += (s, e) => ViewModel.IsAudioEnabled = true;
        AudioRecordToggle.Unchecked += (s, e) => ViewModel.IsAudioEnabled = false;
        AudioPreviewToggle.Checked += (s, e) => ViewModel.IsAudioPreviewEnabled = true;
        AudioPreviewToggle.Unchecked += (s, e) => ViewModel.IsAudioPreviewEnabled = false;
        StatsToggle.Checked += StatsToggle_Checked;
        StatsToggle.Unchecked += StatsToggle_Unchecked;
        CustomAudioToggle.Toggled += (s, e) => ViewModel.IsCustomAudioInputEnabled = CustomAudioToggle.IsOn;
        ShowAllCaptureOptionsToggle.Toggled += (s, e) => ViewModel.ShowAllCaptureOptions = ShowAllCaptureOptionsToggle.IsOn;
        AudioMeterTrack.SizeChanged += (s, e) => AnimateAudioMeterTick();
        ControlBarBorder.SizeChanged += (s, e) => UpdateToggleLabelVisibility(e.NewSize.Width);
        CaptureSettingsGrid.SizeChanged += CaptureSettingsGrid_SizeChanged;
    }

    private void SetupButtonHoverAnimations()
    {
        var buttons = new FrameworkElement[]
        {
            SettingsToggleButton,
            OpenRecordingsButton,
            ScreenshotButton,
            RecordButton,
            PreviewButton,
            HdrToggle,
            AudioRecordToggle,
            TrueHdrPreviewToggle,
            AudioPreviewToggle,
            StatsToggle
        };

        foreach (var button in buttons)
        {
            var isHovered = false;
            button.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
            button.RenderTransform = new ScaleTransform { ScaleX = 1, ScaleY = 1 };

            button.PointerEntered += (_, _) =>
            {
                isHovered = true;
                if (button.RenderTransform is ScaleTransform transform)
                {
                    AnimateScale(transform, 1.08, TimeSpan.FromMilliseconds(100));
                }
            };

            button.PointerExited += (_, _) =>
            {
                isHovered = false;
                if (button.RenderTransform is ScaleTransform transform)
                {
                    AnimateScale(transform, 1.0, TimeSpan.FromMilliseconds(100));
                }
            };

            button.PointerPressed += (_, _) =>
            {
                if (button.RenderTransform is ScaleTransform transform)
                {
                    AnimateScale(transform, 0.95, TimeSpan.FromMilliseconds(60));
                }
            };

            button.PointerReleased += (_, _) =>
            {
                if (button.RenderTransform is ScaleTransform transform)
                {
                    AnimateScale(transform, isHovered ? 1.08 : 1.0, TimeSpan.FromMilliseconds(60));
                }
            };
        }
    }

    private FrameworkElement[] GetEntranceButtons()
    {
        return new FrameworkElement[]
        {
            SettingsToggleButton,
            OpenRecordingsButton,
            ScreenshotButton,
            RecordButton,
            PreviewButton,
            HdrToggle,
            AudioRecordToggle,
            TrueHdrPreviewToggle,
            AudioPreviewToggle,
            StatsToggle
        };
    }

    private void UpdateToggleLabelVisibility(double controlBarWidth)
    {
        var showLabels = controlBarWidth >= ControlBarLabelThreshold;
        if (showLabels == _toggleLabelsVisible) return;
        _toggleLabelsVisible = showLabels;

        var vis = showLabels ? Visibility.Visible : Visibility.Collapsed;
        HdrToggleLabel.Visibility = vis;
        AudioRecordToggleLabel.Visibility = vis;
        PreviewButtonLabel.Visibility = vis;
        HdrPreviewToggleLabel.Visibility = vis;
        AudioPreviewToggleLabel.Visibility = vis;
        StatsToggleLabel.Visibility = vis;
        // Record button is always a circle when idle — no label mode
    }

    private void StatsToggle_Checked(object sender, RoutedEventArgs e)
    {
        if (_isWindowClosing)
        {
            return;
        }

        ShowStatsDockPanel();
        UpdateStatsDock();
        StartStatsDockPolling();
    }

    private void StatsToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        StopStatsDockPolling();
        HideStatsDockPanel();
    }

    private void StartStatsDockPolling()
    {
        _statsPollTimer ??= _dispatcherQueue.CreateTimer();
        _statsPollTimer.Interval = TimeSpan.FromMilliseconds(500);
        _statsPollTimer.IsRepeating = true;
        _statsPollTimer.Tick -= StatsPollTimer_Tick;
        _statsPollTimer.Tick += StatsPollTimer_Tick;
        _statsPollTimer.Start();
    }

    private void StopStatsDockPolling()
    {
        if (_statsPollTimer == null)
        {
            return;
        }

        _statsPollTimer.Stop();
        _statsPollTimer.Tick -= StatsPollTimer_Tick;
        _statsPollTimer = null;
    }

    private void StatsPollTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        UpdateStatsDock();
    }

    private void ShowStatsDockPanel()
    {
        EnsureStatsDockAnimations();
        StopStatsDockAnimation();
        StatsDockPanel.Width = 0;
        StatsDockPanel.Opacity = 0;
        StatsDockPanel.Visibility = Visibility.Visible;
        _statsDockStoryboard = _showStatsDockStoryboard;
        _showStatsDockStoryboard?.Begin();
    }

    private void HideStatsDockPanel(bool immediate = false)
    {
        EnsureStatsDockAnimations();
        StopStatsDockAnimation();
        if (immediate || StatsDockPanel.Visibility != Visibility.Visible)
        {
            StatsDockPanel.Width = 0;
            StatsDockPanel.Visibility = Visibility.Collapsed;
            StatsDockPanel.Opacity = 1;
            return;
        }

        _statsDockStoryboard = _hideStatsDockStoryboard;
        _hideStatsDockStoryboard?.Begin();
    }

    private void StopStatsDockAnimation()
    {
        _statsDockStoryboard?.Stop();
        _statsDockStoryboard = null;
    }

    private void EnsureStatsDockAnimations()
    {
        _showStatsDockStoryboard ??= CreateStatsDockStoryboard(showing: true);
        _hideStatsDockStoryboard ??= CreateStatsDockStoryboard(showing: false);
    }

    private const double StatsDockPanelWidth = 300;

    private Storyboard CreateStatsDockStoryboard(bool showing)
    {
        var durationMs = showing ? 250 : 200;
        var easing = new CubicEase { EasingMode = showing ? EasingMode.EaseOut : EasingMode.EaseIn };
        var duration = TimeSpan.FromMilliseconds(durationMs);

        var storyboard = new Storyboard();

        var widthAnim = new DoubleAnimation
        {
            To = showing ? StatsDockPanelWidth : 0,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(widthAnim, StatsDockPanel);
        Storyboard.SetTargetProperty(widthAnim, "Width");

        var fade = new DoubleAnimation
        {
            To = showing ? 1 : 0,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(fade, StatsDockPanel);
        Storyboard.SetTargetProperty(fade, "Opacity");

        storyboard.Children.Add(widthAnim);
        storyboard.Children.Add(fade);
        storyboard.Completed += (_, _) =>
        {
            if (!ReferenceEquals(_statsDockStoryboard, storyboard))
            {
                return;
            }

            _statsDockStoryboard = null;
            if (showing)
            {
                StatsDockPanel.Width = StatsDockPanelWidth;
                StatsDockPanel.Opacity = 1;
                return;
            }

            StatsDockPanel.Width = 0;
            StatsDockPanel.Visibility = Visibility.Collapsed;
            StatsDockPanel.Opacity = 1;
        };

        return storyboard;
    }

    private void CaptureSettingsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var narrow = e.NewSize.Width < 700;
        if (narrow == _captureSettingsNarrow)
        {
            return;
        }

        _captureSettingsNarrow = narrow;
        if (narrow)
        {
            Grid.SetRow(PresetPanel, 1);
            Grid.SetColumn(PresetPanel, 0);
            Grid.SetRow(SplitPanel, 1);
            Grid.SetColumn(SplitPanel, 1);
            Grid.SetRow(CustomBitratePanel, 1);
            Grid.SetColumn(CustomBitratePanel, 2);
            PresetColumn.Width = new GridLength(0);
            SplitColumn.Width = new GridLength(0);
        }
        else
        {
            Grid.SetRow(PresetPanel, 0);
            Grid.SetColumn(PresetPanel, 4);
            Grid.SetRow(SplitPanel, 0);
            Grid.SetColumn(SplitPanel, 5);
            Grid.SetRow(CustomBitratePanel, 0);
            Grid.SetColumn(CustomBitratePanel, 4);
            PresetColumn.Width = new GridLength(1, GridUnitType.Star);
            SplitColumn.Width = new GridLength(1, GridUnitType.Star);
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ((FrameworkElement)this.Content).Loaded -= MainWindow_Loaded;

        // Uncloak the window — XAML content is now rendered (splash overlay covers everything)
        int cloakFalse = 0;
        DwmSetWindowAttribute(_hwnd, DWMWA_CLOAK, ref cloakFalse, sizeof(int));

        // Start device init immediately — runs behind the splash
        _ = RunUiEventHandlerAsync(async () =>
        {
            Logger.Log("=== MainWindow_Loaded - Starting device enumeration ===");
            try
            {
                await ViewModel.InitializeAsync();
                // LoadSettings just pushed saved volume to CaptureService — capture it, reset to 0
                // so WASAPI playback starts silent. The entrance animation will ramp the slider.
                // NOTE: Do NOT toggle SuppressVolumeSave here — PlaySplashAndEntrance already
                // set it to true, and setting it to false would allow intermediate animation
                // ticks and unrelated SaveSettings() calls to persist PreviewVolume = 0.
                // VolumeSaveOverride ensures any save during the fade writes the real value.
                if (_isVolumeFadingIn)
                {
                    _savedPreviewVolume = ViewModel.PreviewVolume;
                    ViewModel.VolumeSaveOverride = _savedPreviewVolume;
                    ViewModel.PreviewVolume = 0;
                }
                await ViewModel.RefreshDevicesAsync();
            }
            finally
            {
                StartAutomationServices();
            }
        }, nameof(MainWindow_Loaded));

        // Start the splash → entrance sequence
        PlaySplashAndEntrance();
    }

    private void PlaySplashAndEntrance()
    {
        if (_entranceAnimationPlayed) return;
        _entranceAnimationPlayed = true;

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var easingIn = new CubicEase { EasingMode = EasingMode.EaseIn };

        // Phase 1: Splash holds for 600ms, then scales down + fades out over 400ms
        var splashFade = new DoubleAnimation
        {
            From = 1, To = 0,
            BeginTime = TimeSpan.FromMilliseconds(600),
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = easingIn
        };
        Storyboard.SetTarget(splashFade, SplashOverlay);
        Storyboard.SetTargetProperty(splashFade, "Opacity");

        var splashScaleX = new DoubleAnimation
        {
            From = 1.0, To = 0.95,
            BeginTime = TimeSpan.FromMilliseconds(600),
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = easingIn,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(splashScaleX, SplashScale);
        Storyboard.SetTargetProperty(splashScaleX, "ScaleX");

        var splashScaleY = new DoubleAnimation
        {
            From = 1.0, To = 0.95,
            BeginTime = TimeSpan.FromMilliseconds(600),
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = easingIn,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(splashScaleY, SplashScale);
        Storyboard.SetTargetProperty(splashScaleY, "ScaleY");

        var splashSb = new Storyboard();
        splashSb.Children.Add(splashFade);
        splashSb.Children.Add(splashScaleX);
        splashSb.Children.Add(splashScaleY);
        splashSb.Completed += (_, _) =>
        {
            SplashOverlay.Visibility = Visibility.Collapsed;
            PlayEntranceAnimation();
        };
        splashSb.Begin();
    }

    private void PlayEntranceAnimation()
    {
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var storyboard = new Storyboard();

        // 1. Control bar: slide up 20px + fade in (0ms, 350ms)
        var barFade = new DoubleAnimation
        {
            From = 0, To = 1,
            Duration = TimeSpan.FromMilliseconds(350),
            EasingFunction = easing
        };
        Storyboard.SetTarget(barFade, ControlBarBorder);
        Storyboard.SetTargetProperty(barFade, "Opacity");
        storyboard.Children.Add(barFade);

        var barSlide = new DoubleAnimation
        {
            From = 20, To = 0,
            Duration = TimeSpan.FromMilliseconds(350),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(barSlide, (TranslateTransform)ControlBarBorder.RenderTransform);
        Storyboard.SetTargetProperty(barSlide, "Y");
        storyboard.Children.Add(barSlide);

        // 2. Buttons stagger: 50ms offset, 200ms each (starting at 150ms)
        var buttons = GetEntranceButtons();
        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            var beginTime = TimeSpan.FromMilliseconds(150 + (i * 50));
            var duration = TimeSpan.FromMilliseconds(200);

            var buttonFade = new DoubleAnimation
            {
                From = 0, To = 1,
                BeginTime = beginTime, Duration = duration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(buttonFade, button);
            Storyboard.SetTargetProperty(buttonFade, "Opacity");
            storyboard.Children.Add(buttonFade);

            if (button.RenderTransform is ScaleTransform transform)
            {
                var scaleX = new DoubleAnimation
                {
                    From = 0.85, To = 1.0,
                    BeginTime = beginTime, Duration = duration,
                    EasingFunction = easing, EnableDependentAnimation = true
                };
                Storyboard.SetTarget(scaleX, transform);
                Storyboard.SetTargetProperty(scaleX, "ScaleX");
                storyboard.Children.Add(scaleX);

                var scaleY = new DoubleAnimation
                {
                    From = 0.85, To = 1.0,
                    BeginTime = beginTime, Duration = duration,
                    EasingFunction = easing, EnableDependentAnimation = true
                };
                Storyboard.SetTarget(scaleY, transform);
                Storyboard.SetTargetProperty(scaleY, "ScaleY");
                storyboard.Children.Add(scaleY);
            }
        }

        // 3. Stats row: slide down 10px + fade in (600ms begin, 300ms duration)
        var statsFade = new DoubleAnimation
        {
            From = 0, To = 1,
            BeginTime = TimeSpan.FromMilliseconds(600),
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = easing
        };
        Storyboard.SetTarget(statsFade, StatsRow);
        Storyboard.SetTargetProperty(statsFade, "Opacity");
        storyboard.Children.Add(statsFade);

        var statsSlide = new DoubleAnimation
        {
            From = -10, To = 0,
            BeginTime = TimeSpan.FromMilliseconds(600),
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = easing, EnableDependentAnimation = true
        };
        Storyboard.SetTarget(statsSlide, (TranslateTransform)StatsRow.RenderTransform);
        Storyboard.SetTargetProperty(statsSlide, "Y");
        storyboard.Children.Add(statsSlide);

        // 4. Preview area: scale 0.97→1.0 + fade in (900ms begin, 400ms duration)
        var previewFade = new DoubleAnimation
        {
            From = 0, To = 1,
            BeginTime = TimeSpan.FromMilliseconds(900),
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = easing
        };
        Storyboard.SetTarget(previewFade, PreviewBorder);
        Storyboard.SetTargetProperty(previewFade, "Opacity");
        storyboard.Children.Add(previewFade);

        var previewScaleX = new DoubleAnimation
        {
            From = 0.97, To = 1.0,
            BeginTime = TimeSpan.FromMilliseconds(900),
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = easing, EnableDependentAnimation = true
        };
        Storyboard.SetTarget(previewScaleX, PreviewBorderScale);
        Storyboard.SetTargetProperty(previewScaleX, "ScaleX");
        storyboard.Children.Add(previewScaleX);

        var previewScaleY = new DoubleAnimation
        {
            From = 0.97, To = 1.0,
            BeginTime = TimeSpan.FromMilliseconds(900),
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = easing, EnableDependentAnimation = true
        };
        Storyboard.SetTarget(previewScaleY, PreviewBorderScale);
        Storyboard.SetTargetProperty(previewScaleY, "ScaleY");
        storyboard.Children.Add(previewScaleY);

        // 5. Volume slider: fade in audio after everything else has settled (1500ms begin, 800ms ramp)
        if (_savedPreviewVolume > 0 || ViewModel.PreviewVolume > 0)
        {
            var volumeTarget = ViewModel.PreviewVolume > 0 ? ViewModel.PreviewVolume : _savedPreviewVolume;
            _savedPreviewVolume = volumeTarget;
            var volumeAnim = new DoubleAnimation
            {
                From = 0,
                To = volumeTarget * 100,
                BeginTime = TimeSpan.FromMilliseconds(1500),
                Duration = TimeSpan.FromMilliseconds(800),
                EasingFunction = easing,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(volumeAnim, PreviewVolumeSlider);
            Storyboard.SetTargetProperty(volumeAnim, "Value");
            storyboard.Children.Add(volumeAnim);
            ViewModel.SuppressVolumeSave = true;
        }

        storyboard.Completed += (_, _) =>
        {
            _isVolumeFadingIn = false;
            ViewModel.SuppressVolumeSave = false;
            ViewModel.VolumeSaveOverride = null;
            if (_savedPreviewVolume > 0)
            {
                ViewModel.PreviewVolume = _savedPreviewVolume;
            }
        };

        storyboard.Begin();

        // 6. Control bar shadow depth fade-in (Composition animation, compositor thread)
        // Delayed so the bar appears first, then gains depth.
        FadeInShadow(_controlBarShadowVisual, delayMs: 400, durationMs: 500);
    }

    private static void FadeInShadow(SpriteVisual? visual, int delayMs, int durationMs)
    {
        if (visual == null) return;
        var compositor = visual.Compositor;
        var anim = compositor.CreateScalarKeyFrameAnimation();
        anim.InsertKeyFrame(0f, 0f);
        anim.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f)));
        anim.Duration = TimeSpan.FromMilliseconds(durationMs);
        anim.DelayTime = TimeSpan.FromMilliseconds(delayMs);
        visual.StartAnimation("Opacity", anim);
    }

    private static void FadeOutShadow(SpriteVisual? visual, int durationMs)
    {
        if (visual == null) return;
        var compositor = visual.Compositor;
        var anim = compositor.CreateScalarKeyFrameAnimation();
        anim.InsertKeyFrame(1f, 0f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f)));
        anim.Duration = TimeSpan.FromMilliseconds(durationMs);
        visual.StartAnimation("Opacity", anim);
    }

    private void StartAutomationServices()
    {
        if (Interlocked.Exchange(ref _automationServicesStarted, 1) != 0)
        {
            return;
        }

        _automationDiagnosticsHub.Start();
        _automationPipeServer.Start();
        var automationToken = Environment.GetEnvironmentVariable("ELGATOCAPTURE_AUTOMATION_TOKEN");
        var automationPipeName = Environment.GetEnvironmentVariable("ELGATOCAPTURE_AUTOMATION_PIPE");
        if (string.IsNullOrWhiteSpace(automationPipeName))
        {
            automationPipeName = "ElgatoCaptureAutomation";
        }

        Logger.Log(
            $"Automation control ready on pipe '{automationPipeName}' (token required={!string.IsNullOrWhiteSpace(automationToken)}).");
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var nowTick = Environment.TickCount64;
        if (!ViewModel.IsPreviewing ||
            _d3dRenderer == null ||
            PreviewSwapChainPanel.Visibility != Visibility.Visible)
        {
            return;
        }

        var lastLogTick = Interlocked.Read(ref _previewLastResizeLogTick);
        if (nowTick - lastLogTick < 1000)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _previewLastResizeLogTick, nowTick, lastLogTick) == lastLogTick)
        {
            Logger.Log("Preview resize active. Updating compositor transform without resizing swap-chain buffers.");
        }
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (Interlocked.Exchange(ref _windowCloseCleanupStarted, 1) != 0)
        {
            return;
        }

        _isWindowClosing = true;
        StopStatsDockPolling();
        HideStatsDockPanel(immediate: true);
        RecordingGlowPulseStoryboard.Stop();
        RecordingGlowBorder.Opacity = 0;
        RecPulseStoryboard.Stop();

        if (this.Content is FrameworkElement mainContent)
        {
            mainContent.SizeChanged -= MainWindow_SizeChanged;
        }

        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.PreviewStartRequested -= ViewModel_PreviewStartRequested;
        ViewModel.PreviewStopRequested -= ViewModel_PreviewStopRequested;
        ViewModel.PreviewReinitRequested -= ViewModel_PreviewReinitRequested;

        try
        {
            StopPreviewForShutdown();
            ResetPreviewStartupTracking();
        }
        catch (Exception ex)
        {
            Logger.Log($"Preview shutdown cleanup failed: {ex.Message}");
        }

        // Graceful recording stop: if recording is active, attempt a clean stop with timeout
        if (ViewModel.IsRecording)
        {
            Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording active, attempting graceful stop...");
            try
            {
                var stopTask = ViewModel.ToggleRecordingAsync();
                var completed = await Task.WhenAny(stopTask, Task.Delay(5000));
                if (completed == stopTask)
                {
                    await stopTask; // propagate any exception
                    Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording stopped cleanly.");
                }
                else
                {
                    Logger.Log("WINDOW_CLOSE_RECORDING_STOP: timed out after 5s, proceeding with dispose.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"WINDOW_CLOSE_RECORDING_STOP: stop failed: {ex.Message}");
            }
        }

        try
        {
            await _automationPipeServer.DisposeAsync();
            await _automationDiagnosticsHub.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"Automation shutdown cleanup failed: {ex.Message}");
        }

        try
        {
            await ViewModel.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"ViewModel dispose during window close failed: {ex.Message}");
        }
    }

    private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()
    {
        var d3d = _d3dRenderer;
        var nowTick = Environment.TickCount64;
        var framesArrived = Interlocked.Read(ref _previewFramesArrived);
        var framesDisplayed = Interlocked.Read(ref _previewFramesDisplayed);
        var framesDropped = Interlocked.Read(ref _previewFramesDropped);
        var lastPresentedTick = Interlocked.Read(ref _previewLastPresentedTick);
        var gpuActive = d3d != null;
        var gpuElementVisible = PreviewSwapChainPanel.Visibility == Visibility.Visible;
        var cpuElementVisible = PreviewImage.Visibility == Visibility.Visible;
        var rendererAttached = d3d != null || _previewSource != null;
        var placeholderVisible = NoDevicePlaceholder.Visibility == Visibility.Visible;
        var previewPipelineActive = ViewModel.IsPreviewing && rendererAttached;
        var d3dFramesSubmitted = d3d?.FramesSubmitted ?? 0;
        var d3dFramesRendered = d3d?.FramesRendered ?? 0;
        var d3dFramesDropped = d3d?.FramesDropped ?? 0;
        if (gpuActive)
        {
            framesArrived = d3dFramesSubmitted;
            framesDisplayed = d3dFramesRendered;
            framesDropped = d3dFramesDropped;
        }

        var rendererMode = d3d?.RendererMode
            ?? (ViewModel.IsPreviewing ? "CpuSoftwareBitmap" : "None");
        var gpuPlaybackState = "None";
        int gpuNaturalVideoWidth = 0, gpuNaturalVideoHeight = 0;
        double gpuPositionMs = 0;
        if (d3d != null)
        {
            gpuPlaybackState = d3d.IsRendering ? "Rendering" : "Idle";
            gpuNaturalVideoWidth = d3d.NaturalWidth;
            gpuNaturalVideoHeight = d3d.NaturalHeight;
            gpuPositionMs = 0;
        }
        var gpuPositionEventCount = Interlocked.Read(ref _previewStartupPositionEventCount);

        var startupElapsedMs = _previewStartupRequestedUtc.HasValue
            ? Math.Max(0, (DateTimeOffset.UtcNow - _previewStartupRequestedUtc.Value).TotalMilliseconds)
            : (double?)null;
        var startupMissingSignals = _previewStartupMissingSignals;
        if (string.IsNullOrWhiteSpace(startupMissingSignals) &&
            _previewStartupState is PreviewStartupState.WaitingForFirstVisual or PreviewStartupState.Failed)
        {
            startupMissingSignals = BuildPreviewStartupMissingSignals();
        }
        var startupTimedOut = ViewModel.IsPreviewing &&
                              _previewStartupState == PreviewStartupState.WaitingForFirstVisual &&
                              startupElapsedMs.GetValueOrDefault() >= PreviewStartupVisualTimeoutMs;
        var blankSuspected = !gpuActive && previewPipelineActive &&
                             framesArrived > 30 &&
                             framesDisplayed == 0;
        if (!blankSuspected && startupTimedOut)
        {
            blankSuspected = true;
        }
        var stallSuspected = !gpuActive && previewPipelineActive &&
                             lastPresentedTick > 0 &&
                             nowTick - lastPresentedTick > 3000;
        var rendererCadence = d3d?.GetPresentCadenceMetrics(_previewMinPresentationIntervalMs);

        return new PreviewRuntimeSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            IsPreviewing = ViewModel.IsPreviewing,
            GpuActive = gpuActive,
            PlaceholderVisible = placeholderVisible,
            GpuElementVisible = gpuElementVisible,
            CpuElementVisible = cpuElementVisible,
            RendererAttached = rendererAttached,
            StartupState = _previewStartupState.ToString(),
            StartupAttemptId = _previewStartupAttemptId,
            StartupElapsedMs = startupElapsedMs,
            StartupTimeoutMs = PreviewStartupVisualTimeoutMs,
            StartupGpuSignalMediaOpened = _previewGpuSignalMediaOpened,
            StartupGpuSignalFirstFrame = _previewGpuSignalFirstFrame,
            StartupGpuSignalPlaybackAdvancing = _previewGpuSignalPlaybackAdvancing,
            StartupRequiredSignals = _previewStartupRequiredSignals,
            StartupReceivedSignals = _previewStartupReceivedSignals,
            StartupStrategy = _previewStartupStrategy,
            StartupMissingSignals = startupMissingSignals,
            StartupRecoveryAttemptCount = _previewRecoveryAttemptCount,
            StartupLastFailureReason = _previewLastFailureReason,
            FirstVisualConfirmed = _previewFirstVisualConfirmed,
            FramesArrived = framesArrived,
            FramesDisplayed = framesDisplayed,
            FramesDropped = framesDropped,
            DisplayCadenceSampleCount = rendererCadence?.SampleCount ?? 0,
            DisplayCadenceObservedFps = rendererCadence?.ObservedFps ?? 0,
            DisplayCadenceExpectedIntervalMs = rendererCadence?.ExpectedIntervalMs ?? 0,
            DisplayCadenceAverageIntervalMs = rendererCadence?.AverageIntervalMs ?? 0,
            DisplayCadenceP95IntervalMs = rendererCadence?.P95IntervalMs ?? 0,
            DisplayCadenceMaxIntervalMs = rendererCadence?.MaxIntervalMs ?? 0,
            DisplayCadenceJitterStdDevMs = rendererCadence?.JitterStdDevMs ?? 0,
            DisplayCadenceSlowFrameCount = rendererCadence?.SlowFrameCount ?? 0,
            DisplayCadenceSlowFramePercent = rendererCadence?.SlowFramePercent ?? 0,
            BlankSuspected = blankSuspected,
            StallSuspected = stallSuspected,
            RendererMode = rendererMode,
            D3DFramesSubmitted = d3dFramesSubmitted,
            D3DFramesRendered = d3dFramesRendered,
            D3DFramesDropped = d3dFramesDropped,
            D3DInputColorSpace = _d3dRenderer?.InputColorSpaceLabel ?? "None",
            D3DOutputColorSpace = _d3dRenderer?.OutputColorSpaceLabel ?? "None",
            EstimatedPipelineLatencyMs = d3d?.GetEstimatedPipelineLatencyMs() ?? 0,
            GpuPlaybackState = gpuPlaybackState,
            GpuNaturalVideoWidth = gpuNaturalVideoWidth,
            GpuNaturalVideoHeight = gpuNaturalVideoHeight,
            GpuPositionMs = gpuPositionMs,
            GpuPositionEventCount = gpuPositionEventCount
        };
    }

    private void UpdateStatsDock()
    {
        if (_isWindowClosing || StatsDockPanel.Visibility != Visibility.Visible)
        {
            return;
        }

        var snapshot = GetStatsSnapshot();
        var sessionState = snapshot.Recording
            ? "Recording"
            : snapshot.Previewing
                ? "Previewing"
                : "Idle";
        var sourceFps = FormatFps(snapshot.SourceObservedFps);
        var sourceExpectedFps = FormatFps(snapshot.SourceExpectedFps);
        var sourceAvg = $"{FormatMs(snapshot.SourceAvgIntervalMs)} avg";
        var sourceP95 = $"{FormatMs(snapshot.SourceP95IntervalMs)} P95";
        var sourceJitter = FormatMs(snapshot.SourceJitterMs);
        var sourceGaps = $"{FormatCount(snapshot.SourceSevereGaps)} severe";
        var sourceDrops = $"{FormatCount(snapshot.SourceEstDrops)} drops ({FormatPercent(snapshot.SourceEstDropPct)})";
        var previewFps = FormatFps(snapshot.PreviewObservedFps);
        var previewAvg = $"{FormatMs(snapshot.PreviewAvgIntervalMs)} avg";
        var previewP95 = $"{FormatMs(snapshot.PreviewP95IntervalMs)} P95";
        var previewSlow = $"{FormatCount(snapshot.PreviewSlowFrames)} frames ({FormatPercent(snapshot.PreviewSlowPct)})";
        var pipelineLatency = $"{FormatMs(snapshot.PipelineLatencyMs)} avg";
        var sourceDelivered = $"{FormatCount(snapshot.SourceFramesDelivered)} delivered";
        var sourceDropped = $"{FormatCount(snapshot.SourceFramesDropped)} dropped";
        var rendererRendered = $"{FormatCount(snapshot.RendererFramesRendered)} rendered";
        var rendererDropped = $"{FormatCount(snapshot.RendererFramesDropped)} dropped";
        var perfScore = $"{FormatScore(snapshot.PerformanceScore)} / 100";

        var sourceResolution = snapshot.SourceWidth.HasValue && snapshot.SourceHeight.HasValue
            ? $"{snapshot.SourceWidth} x {snapshot.SourceHeight}"
            : "\u2014";
        var sourceFrameRate = snapshot.SourceFrameRateExact.HasValue
            ? $"{snapshot.SourceFrameRateExact.Value:0.##} fps"
            : "\u2014";
        var sourceHdr = snapshot.SourceIsHdr switch
        {
            true => "On",
            false => "Off",
            _ => "\u2014"
        };
        var sourceFormat = snapshot.NegotiatedPixelFormat ?? "\u2014";
        var telemetryOrigin = snapshot.TelemetryOrigin is not null and not "Unknown"
            ? $"{snapshot.TelemetryOrigin} ({snapshot.TelemetryConfidence ?? "?"})"
            : "\u2014";

        SetTextIfChanged(Stats_SessionStateValue, sessionState);
        SetTextIfChanged(Stats_SourceResolutionValue, sourceResolution);
        SetTextIfChanged(Stats_SourceFrameRateValue, sourceFrameRate);
        SetTextIfChanged(Stats_SourceHdrValue, sourceHdr);
        SetTextIfChanged(Stats_SourceFormatValue, sourceFormat);
        SetTextIfChanged(Stats_TelemetryOriginValue, telemetryOrigin);
        SetTextIfChanged(Stats_SourceFpsValue, sourceFps);
        SetTextIfChanged(Stats_SourceExpectedFpsValue, sourceExpectedFps);
        SetTextIfChanged(Stats_SourceAvgValue, sourceAvg);
        SetTextIfChanged(Stats_SourceP95Value, sourceP95);
        SetTextIfChanged(Stats_SourceJitterValue, sourceJitter);
        SetTextIfChanged(Stats_SourceGapsValue, sourceGaps);
        SetTextIfChanged(Stats_SourceDropsValue, sourceDrops);
        SetTextIfChanged(Stats_PreviewFpsValue, previewFps);
        SetTextIfChanged(Stats_PreviewAvgValue, previewAvg);
        SetTextIfChanged(Stats_PreviewP95Value, previewP95);
        SetTextIfChanged(Stats_PreviewSlowValue, previewSlow);
        SetTextIfChanged(Stats_PipelineLatencyValue, pipelineLatency);
        SetTextIfChanged(Stats_SourceDeliveredValue, sourceDelivered);
        SetTextIfChanged(Stats_SourceDroppedValue, sourceDropped);
        SetTextIfChanged(Stats_RendererRenderedValue, rendererRendered);
        SetTextIfChanged(Stats_RendererDroppedValue, rendererDropped);
        SetTextIfChanged(Stats_PerfScoreValue, perfScore);
    }

    private StatsSnapshot GetStatsSnapshot()
    {
        var health = ViewModel.GetCaptureHealthSnapshot();
        var d3d = _d3dRenderer;
        var presentCadence = d3d?.GetPresentCadenceMetrics(_previewMinPresentationIntervalMs);
        var pipelineLatency = d3d?.GetEstimatedPipelineLatencyMs() ?? 0;
        var sourceDropPercent = Sanitize(health.CaptureCadenceEstimatedDropPercent);
        var previewSlowPercent = Sanitize(presentCadence?.SlowFramePercent ?? 0);
        var performanceScore = Math.Clamp(100.0 - sourceDropPercent - previewSlowPercent, 0.0, 100.0);

        return new StatsSnapshot(
            SourceCadenceSamples: health.CaptureCadenceSampleCount,
            SourceObservedFps: Sanitize(health.CaptureCadenceObservedFps),
            SourceExpectedFps: Sanitize(health.ExpectedFrameRate),
            SourceAvgIntervalMs: Sanitize(health.CaptureCadenceAverageIntervalMs),
            SourceP95IntervalMs: Sanitize(health.CaptureCadenceP95IntervalMs),
            SourceMaxIntervalMs: Sanitize(health.CaptureCadenceMaxIntervalMs),
            SourceJitterMs: Sanitize(health.CaptureCadenceJitterStdDevMs),
            SourceSevereGaps: health.CaptureCadenceSevereGapCount,
            SourceEstDrops: health.CaptureCadenceEstimatedDroppedFrames,
            SourceEstDropPct: sourceDropPercent,
            PreviewCadenceSamples: presentCadence?.SampleCount ?? 0,
            PreviewObservedFps: Sanitize(presentCadence?.ObservedFps ?? 0),
            PreviewAvgIntervalMs: Sanitize(presentCadence?.AverageIntervalMs ?? 0),
            PreviewP95IntervalMs: Sanitize(presentCadence?.P95IntervalMs ?? 0),
            PreviewSlowFrames: presentCadence?.SlowFrameCount ?? 0,
            PreviewSlowPct: previewSlowPercent,
            PipelineLatencyMs: Sanitize(pipelineLatency),
            SourceFramesDelivered: health.VideoFramesArrived,
            SourceFramesDropped: health.VideoFramesDropped,
            RendererFramesSubmitted: d3d?.FramesSubmitted ?? 0,
            RendererFramesRendered: d3d?.FramesRendered ?? 0,
            RendererFramesDropped: d3d?.FramesDropped ?? 0,
            PerformanceScore: performanceScore,
            Previewing: ViewModel.IsPreviewing,
            Recording: ViewModel.IsRecording,
            SourceWidth: health.SourceWidth,
            SourceHeight: health.SourceHeight,
            SourceFrameRateExact: health.SourceFrameRateExact,
            SourceIsHdr: health.SourceIsHdr,
            NegotiatedPixelFormat: health.NegotiatedPixelFormat,
            TelemetryOrigin: health.SourceTelemetryOrigin.ToString(),
            TelemetryConfidence: health.SourceTelemetryConfidence.ToString());
    }

    private static string FormatFps(double value)
    {
        return Sanitize(value).ToString("0.00");
    }

    private static string FormatMs(double value)
    {
        return $"{Sanitize(value):0.00}ms";
    }

    private static string FormatPercent(double value)
    {
        return $"{Sanitize(value):0.0}%";
    }

    private static string FormatScore(double value)
    {
        return Sanitize(value).ToString("0.0");
    }

    private static string FormatCount(long value)
    {
        return Math.Max(0, value).ToString("N0");
    }

    private static void SetTextIfChanged(TextBlock target, string value)
    {
        if (!string.Equals(target.Text, value, StringComparison.Ordinal))
        {
            target.Text = value;
        }
    }

    private static double Sanitize(double value)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            return 0;
        }

        return value;
    }

    private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            return GetPreviewRuntimeSnapshot();
        }

        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var completion = new TaskCompletionSource<PreviewRuntimeSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration registration = default;
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    completion.TrySetCanceled(cancellationToken);
                });
            }

            var enqueued = _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        completion.TrySetCanceled(cancellationToken);
                        return;
                    }

                    completion.TrySetResult(GetPreviewRuntimeSnapshot());
                }
                catch (Exception ex)
                {
                    completion.TrySetException(ex);
                }
                finally
                {
                    registration.Dispose();
                }
            });

            if (enqueued)
            {
                return await completion.Task.ConfigureAwait(false);
            }

            registration.Dispose();
            if (attempt >= maxAttempts)
            {
                break;
            }

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException("Failed to enqueue preview snapshot operation.");
    }

    private void StopPreviewForShutdown()
    {
        _isPreviewReinitAnimating = false;
        StopPreviewFadeInTimer();
        ResetPreviewContentTransform();

        // Clean up composition shadow
        _videoShadowVisual = null;
        ElementCompositionPreview.SetElementChildVisual(VideoShadowHost, null);

        // Clean up D3D11 preview
        PreviewContentGrid.SizeChanged -= OnPreviewContentGridSizeChanged;
        var renderer = _d3dRenderer;
        _d3dRenderer = null;
        if (renderer != null)
        {
            PreviewSwapChainPanel.SizeChanged -= OnPreviewSwapChainPanelSizeChanged;
            renderer.FirstFrameRendered -= OnD3DRendererFirstFrameRendered;
            renderer.Stop();
            renderer.Dispose();
        }
        ViewModel.SetPreviewFrameSink(null);
        SetGpuPreviewVisibility(Visibility.Collapsed);
        _previewStartupExpectGpuDualSignals = false;
        _previewGpuSignalMediaOpened = false;
        _previewGpuSignalFirstFrame = false;
        _previewGpuSignalPlaybackAdvancing = false;
        _previewStartupRequiredSignals = PreviewStartupSignalFlags.None;
        _previewStartupReceivedSignals = PreviewStartupSignalFlags.None;
        _previewStartupStrategy = PreviewStartupStrategy.None;
        _previewStartupLastPlaybackPosition = TimeSpan.Zero;
        _previewStartupPositionEventCount = 0;
        _previewStartupPlaybackPositionInitialized = false;

        // Clean up CPU preview
        PreviewImage.Source = null;
        PreviewImage.Visibility = Visibility.Collapsed;
        _previewSource = null;
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(
            () => HandleViewModelPropertyChangedAsync(e),
            $"ViewModel_PropertyChanged:{e.PropertyName}");
    }

    private void ViewModel_PreviewStartRequested(object? sender, EventArgs e)
    {
        _previewStopRequestedByUser = false;
    }

    private async Task ViewModel_PreviewReinitRequested(string reason)
    {
        if (!ViewModel.IsPreviewing)
        {
            return;
        }

        _isPreviewReinitAnimating = true;
        Logger.Log($"PREVIEW_REINIT_ANIMATE_OUT reason={reason}");
        await AnimatePreviewOutAsync();
    }

    private void ViewModel_PreviewStopRequested(object? sender, EventArgs e)
    {
        _previewStopRequestedByUser = _previewStopRequestedByUser || !ViewModel.IsPreviewReinitializing;
        StopPreviewStartupWatchdog();
        StopPreviewStartupOverlay();
    }

    private async Task HandleViewModelPropertyChangedAsync(System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.IsPreviewing):
                if (ViewModel.IsPreviewing)
                {
                    _previewStopRequestedByUser = false;
                    if (string.IsNullOrWhiteSpace(_previewStartupAttemptId) ||
                        IsPreviewStartupFailedState(_previewStartupState) ||
                        _previewStartupState == PreviewStartupState.Idle)
                    {
                        BeginPreviewStartupAttempt();
                    }

                    SetPreviewStartupState(PreviewStartupState.StartingSession);
                    Logger.Log($"PREVIEW_SESSION_STARTED attempt={_previewStartupAttemptId ?? "none"}");
                    if (!ViewModel.IsPreviewReinitializing && !_isPreviewReinitAnimating)
                    {
                        FadeOutElement(NoDevicePlaceholder);
                        StartPreviewStartupOverlay();
                        PreviewContentGrid.Opacity = 0.0;
                        PreviewContentScale.ScaleX = 0.97;
                        PreviewContentScale.ScaleY = 0.97;
                    }
                    SetPreviewStartupState(PreviewStartupState.RendererAttaching);
                    try
                    {
                        await StartPreviewRendererAsync();
                    }
                    catch (Exception ex)
                    {
                        var attachFailureReason = $"renderer-attach-failed:{ex.Message}";
                        SetPreviewStartupState(PreviewStartupState.Failed, attachFailureReason);
                        StopPreviewStartupWatchdog();
                        StopPreviewStartupOverlay();
                        ResetPreviewContentTransform();
                        FadeInElement(NoDevicePlaceholder);
                        Logger.Log($"PREVIEW_RENDERER_ATTACH_FAILED attempt={_previewStartupAttemptId ?? "none"} reason={attachFailureReason}");
                        SchedulePreviewStartupFailureStop(attachFailureReason);
                        throw;
                    }
                    if (!_previewFirstVisualConfirmed)
                    {
                        SetPreviewStartupState(PreviewStartupState.WaitingForFirstVisual);
                        StartPreviewStartupWatchdog();
                    }
                    PreviewButtonIcon.Glyph = "\uE71A";
                    ToolTipService.SetToolTip(PreviewButton, "Stop Preview");
                    TrueHdrPreviewToggle.IsEnabled = ViewModel.IsHdrEnabled && !ViewModel.IsRecording;
                }
                else
                {
                    StopPreviewStartupWatchdog();
                    StopPreviewStartupOverlay();
                    await StopPreviewRendererAsync();
                    if (!ViewModel.IsPreviewReinitializing && !_isPreviewReinitAnimating)
                    {
                        ResetPreviewContentTransform();
                        FadeInElement(NoDevicePlaceholder);
                    }
                    if (ViewModel.IsPreviewReinitializing)
                    {
                        PreviewButtonIcon.Glyph = "\uE71A";
                        ToolTipService.SetToolTip(PreviewButton, "Stop Preview");
                    }
                    else
                    {
                        PreviewButtonIcon.Glyph = "\uE768";
                        ToolTipService.SetToolTip(PreviewButton, "Start Preview");
                    }
                    TrueHdrPreviewToggle.IsEnabled = ViewModel.IsHdrEnabled && !ViewModel.IsRecording;
                    ResetPreviewStartupTracking(preserveReinitAnimation: ViewModel.IsPreviewReinitializing || _isPreviewReinitAnimating);
                }
                break;

            case nameof(MainViewModel.IsPreviewReinitializing):
                if (!ViewModel.IsPreviewReinitializing && _isPreviewReinitAnimating)
                {
                    if (!ViewModel.IsPreviewing)
                    {
                        _isPreviewReinitAnimating = false;
                        StopPreviewStartupOverlay();
                        ResetPreviewContentTransform();
                        FadeInElement(NoDevicePlaceholder);
                    }
                    else if (_previewFirstVisualConfirmed)
                    {
                        Logger.Log($"PREVIEW_REINIT_ANIMATE_RESET attempt={_previewStartupAttemptId ?? "none"} reason=reinit-stop-failed");
                        _isPreviewReinitAnimating = false;
                        StopPreviewStartupOverlay();
                        ResetPreviewContentTransform();
                    }
                }
                else if (!ViewModel.IsPreviewReinitializing && !ViewModel.IsPreviewing)
                {
                    PreviewButtonIcon.Glyph = "\uE768";
                    ToolTipService.SetToolTip(PreviewButton, "Start Preview");
                }
                break;

            case nameof(MainViewModel.IsRecording):
                if (ViewModel.IsRecording)
                {
                    RecordingGlowBorder.Opacity = 1.0;
                    RecordingGlowPulseStoryboard.Begin();
                }
                else
                {
                    RecordingGlowPulseStoryboard.Stop();
                    RecordingGlowBorder.Opacity = 0;
                    ResetAudioMeterVisuals();
                }

                UpdateVideoContentOverlays();
                // Three-state button: hide spinner, show correct content, animated morph
                RecordButtonStartingContent.IsActive = false;
                RecordButtonStartingContent.Visibility = Visibility.Collapsed;
                if (ViewModel.IsRecording)
                {
                    // Circle → pill: show recording content, measure target, animate
                    RecordButtonNormalContent.Visibility = Visibility.Collapsed;
                    RecordButtonRecordingContent.Visibility = Visibility.Visible;
                    RecordButton.Padding = new Thickness(12, 0, 12, 0);
                    RecordButton.Width = double.NaN;
                    RecordButton.UpdateLayout();
                    var targetWidth = RecordButton.ActualWidth;
                    RecordButton.Width = 36;
                    AnimateRecordButtonWidth(36, targetWidth);
                }
                else
                {
                    // Pill → circle: capture current width, animate to 36, swap content on completion
                    var currentWidth = RecordButton.ActualWidth;
                    RecordButton.Width = currentWidth;
                    AnimateRecordButtonWidth(currentWidth, 36, () =>
                    {
                        RecordButtonRecordingContent.Visibility = Visibility.Collapsed;
                        RecordButtonNormalContent.Visibility = Visibility.Visible;
                        RecordButton.Padding = new Thickness(0);
                    });
                }
                AudioRecordToggle.IsEnabled = !ViewModel.IsRecording;
                CustomAudioToggle.IsEnabled = !ViewModel.IsRecording;
                AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled && !ViewModel.IsRecording;
                HdrToggle.IsEnabled = ViewModel.IsHdrAvailable &&
                                      !ViewModel.IsRecording &&
                                      ViewModel.SourceIsHdr != false;
                TrueHdrPreviewToggle.IsEnabled = ViewModel.IsHdrEnabled && !ViewModel.IsRecording;
                // Stats panel always visible — shows "--" when not recording
                RefreshHdrHintText();
                if (ViewModel.IsRecording)
                    RecPulseStoryboard.Begin();
                else
                    RecPulseStoryboard.Stop();
                ApplyWindowTitle();
                break;

            case nameof(MainViewModel.StatusText):
                StatusTextBlock.Text = ViewModel.StatusText;
                break;

            case nameof(MainViewModel.RecordingTime):
                RecordingTimeTextBlock.Text = ViewModel.RecordingTime;
                if (ViewModel.IsRecording)
                    ApplyWindowTitle();
                break;

            case nameof(MainViewModel.DiskSpaceInfo):
                DiskSpaceTextBlock.Text = ViewModel.DiskSpaceInfo;
                break;
            case nameof(MainViewModel.RecordingSizeInfo):
                RecordingSizeTextBlock.Text = ViewModel.RecordingSizeInfo;
                break;
            case nameof(MainViewModel.RecordingBitrateInfo):
                RecordingBitrateTextBlock.Text = ViewModel.RecordingBitrateInfo;
                break;

            case nameof(MainViewModel.OutputPath):
                OutputPathTextBox.Text = ViewModel.OutputPath;
                break;

            case nameof(MainViewModel.AudioPeak):
                _audioMeterTargetLevel = Math.Clamp(ViewModel.AudioPeak, 0.0, 1.0);
                break;

            case nameof(MainViewModel.AudioClipping):
                AudioClipText.Visibility = ViewModel.AudioClipping ? Visibility.Visible : Visibility.Collapsed;
                break;

            case nameof(MainViewModel.SelectedDevice):
                Logger.Log($"=== SelectedDevice PropertyChanged ===");
                Logger.Log($"  ViewModel.SelectedDevice: {ViewModel.SelectedDevice?.Name ?? "NULL"}");
                Logger.Log($"  ViewModel.Devices count: {ViewModel.Devices.Count}");
                Logger.Log($"  DeviceComboBox.Items count: {DeviceComboBox.Items.Count}");
                Logger.Log($"  DeviceComboBox.SelectedItem: {((ElgatoCapture.Models.CaptureDevice?)DeviceComboBox.SelectedItem)?.Name ?? "NULL"}");
                EnsureDeviceSelection();
                break;

            case nameof(MainViewModel.SelectedResolution):
                EnsureResolutionSelection();
                break;

            case nameof(MainViewModel.SelectedFrameRate):
                EnsureFrameRateSelection();
                break;

            case nameof(MainViewModel.IsAutoFrameRateSelected):
                EnsureFrameRateSelection();
                break;

            case nameof(MainViewModel.AvailableResolutions):
                ResolutionComboBox.ItemsSource = ViewModel.AvailableResolutions;
                EnsureResolutionSelection();
                break;

            case nameof(MainViewModel.AvailableFrameRates):
                FrameRateComboBox.ItemsSource = ViewModel.AvailableFrameRates;
                EnsureFrameRateSelection();
                break;

            case nameof(MainViewModel.IsHdrAvailable):
            case nameof(MainViewModel.SourceIsHdr):
                HdrToggle.IsEnabled = ViewModel.IsHdrAvailable &&
                                      !ViewModel.IsRecording &&
                                      ViewModel.SourceIsHdr != false;
                break;

            case nameof(MainViewModel.IsHdrEnabled):
                if (HdrToggle.IsChecked != ViewModel.IsHdrEnabled)
                {
                    HdrToggle.IsChecked = ViewModel.IsHdrEnabled;
                }

                TrueHdrPreviewToggle.IsEnabled = ViewModel.IsHdrEnabled && !ViewModel.IsRecording;
                break;

            case nameof(MainViewModel.IsTrueHdrPreviewEnabled):
                if (TrueHdrPreviewToggle.IsChecked != ViewModel.IsTrueHdrPreviewEnabled)
                {
                    TrueHdrPreviewToggle.IsChecked = ViewModel.IsTrueHdrPreviewEnabled;
                }

                _d3dRenderer?.SetHdrPassthroughEnabled(ViewModel.IsTrueHdrPreviewEnabled);
                break;

            case nameof(MainViewModel.HdrResolutionSupportHint):
            case nameof(MainViewModel.HdrReadinessReason):
            case nameof(MainViewModel.HdrRuntimeState):
                RefreshHdrHintText();
                break;

            case nameof(MainViewModel.SourceTelemetrySummaryText):
            case nameof(MainViewModel.SourceTargetSummaryText):
                UpdateFpsTelemetryTooltip();
                break;

            case nameof(MainViewModel.SourceWidth):
            case nameof(MainViewModel.SourceHeight):
                UpdateVideoContentOverlays();
                break;

            case nameof(MainViewModel.IsCustomBitrateVisible):
                CustomBitratePanel.Visibility = ViewModel.IsCustomBitrateVisible ? Visibility.Visible : Visibility.Collapsed;
                break;

            case nameof(MainViewModel.CustomBitrateMbps):
                if (double.IsNaN(CustomBitrateNumberBox.Value) ||
                    Math.Abs(CustomBitrateNumberBox.Value - ViewModel.CustomBitrateMbps) > 0.01)
                {
                    CustomBitrateNumberBox.Value = ViewModel.CustomBitrateMbps;
                }
                break;

            case nameof(MainViewModel.IsCustomAudioInputEnabled):
                if (CustomAudioToggle.IsOn != ViewModel.IsCustomAudioInputEnabled)
                {
                    CustomAudioToggle.IsOn = ViewModel.IsCustomAudioInputEnabled;
                }
                var isVisible = ViewModel.IsCustomAudioInputEnabled ? Visibility.Visible : Visibility.Collapsed;
                AudioInputLabel.Visibility = isVisible;
                AudioInputComboBox.Visibility = isVisible;
                AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled && !ViewModel.IsRecording;
                break;

            case nameof(MainViewModel.ShowAllCaptureOptions):
                if (ShowAllCaptureOptionsToggle.IsOn != ViewModel.ShowAllCaptureOptions)
                {
                    ShowAllCaptureOptionsToggle.IsOn = ViewModel.ShowAllCaptureOptions;
                }
                break;

            case nameof(MainViewModel.SelectedAudioInputDevice):
                EnsureAudioInputSelection();
                break;

            case nameof(MainViewModel.SelectedRecordingFormat):
                EnsureFormatSelection();
                break;

            case nameof(MainViewModel.SelectedQuality):
                EnsureQualitySelection();
                break;

            case nameof(MainViewModel.AvailablePresets):
                PresetComboBox.ItemsSource = ViewModel.AvailablePresets;
                EnsurePresetSelection();
                break;

            case nameof(MainViewModel.SelectedPreset):
                EnsurePresetSelection();
                break;

            case nameof(MainViewModel.AvailableSplitEncodeModes):
                SplitEncodeComboBox.ItemsSource = ViewModel.AvailableSplitEncodeModes;
                EnsureSplitEncodeModeSelection();
                break;

            case nameof(MainViewModel.SelectedSplitEncodeMode):
                EnsureSplitEncodeModeSelection();
                break;

            case nameof(MainViewModel.LiveResolution):
                LiveResolutionTextBlock.Text = ViewModel.LiveResolution;
                break;

            case nameof(MainViewModel.LiveFrameRate):
                LiveFrameRateTextBlock.Text = ViewModel.LiveFrameRate;
                break;

            case nameof(MainViewModel.LivePixelFormat):
                LivePixelFormatTextBlock.Text = ViewModel.LivePixelFormat;
                break;

            case nameof(MainViewModel.IsAudioEnabled):
                if (AudioRecordToggle.IsChecked != ViewModel.IsAudioEnabled)
                {
                    AudioRecordToggle.IsChecked = ViewModel.IsAudioEnabled;
                }
                AudioPreviewToggle.IsEnabled = ViewModel.IsAudioEnabled;
                if (!ViewModel.IsAudioEnabled && AudioPreviewToggle.IsChecked == true)
                {
                    AudioPreviewToggle.IsChecked = false;
                }
                AnimateAudioMeterDisabled(!ViewModel.IsAudioEnabled);
                break;

            case nameof(MainViewModel.IsAudioPreviewEnabled):
                if (AudioPreviewToggle.IsChecked != ViewModel.IsAudioPreviewEnabled)
                {
                    AudioPreviewToggle.IsChecked = ViewModel.IsAudioPreviewEnabled;
                }
                SetAudioMeterMonitoringState(ViewModel.IsAudioPreviewEnabled);
                break;

            case nameof(MainViewModel.PreviewVolume):
                if (!_isVolumeFadingIn)
                {
                    var volumePct = ViewModel.PreviewVolume * 100;
                    if (PreviewVolumeSlider.Value != volumePct)
                    {
                        PreviewVolumeSlider.Value = volumePct;
                        PreviewVolumeLabel.Text = $"{(int)volumePct}%";
                    }
                }
                break;

            case nameof(MainViewModel.IsRecordingTransitioning):
                RecordButton.IsEnabled = !ViewModel.IsRecordingTransitioning;
                if (ViewModel.IsRecordingTransitioning)
                {
                    if (ViewModel.IsRecording)
                    {
                        // Stopping: freeze pill width so it doesn't collapse when content hides
                        RecordButton.Width = RecordButton.ActualWidth;
                        RecordButtonRecordingContent.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        // Starting: hide idle dot, show spinner in circle
                        RecordButtonNormalContent.Visibility = Visibility.Collapsed;
                    }
                    RecordButtonStartingContent.IsActive = true;
                    RecordButtonStartingContent.Visibility = Visibility.Visible;
                }
                else
                {
                    RecordButtonStartingContent.IsActive = false;
                    RecordButtonStartingContent.Visibility = Visibility.Collapsed;
                    RecordButtonNormalContent.Visibility = ViewModel.IsRecording ? Visibility.Collapsed : Visibility.Visible;
                    RecordButtonRecordingContent.Visibility = ViewModel.IsRecording ? Visibility.Visible : Visibility.Collapsed;
                }
                break;

            case nameof(MainViewModel.IsFfmpegMissing):
                RecordButton.IsEnabled = !ViewModel.IsFfmpegMissing && !ViewModel.IsRecordingTransitioning;
                break;
        }
    }

    private void RefreshHdrHintText()
    {
        var resolutionHint = ViewModel.HdrResolutionSupportHint?.Trim();
        var readinessHint = ViewModel.HdrReadinessReason?.Trim();
        var combinedHint = string.IsNullOrWhiteSpace(readinessHint)
            ? resolutionHint
            : string.IsNullOrWhiteSpace(resolutionHint)
                ? readinessHint
                : $"{readinessHint}{Environment.NewLine}{resolutionHint}";
        if (ViewModel.IsRecording)
        {
            combinedHint = string.IsNullOrWhiteSpace(combinedHint)
                ? "Stop recording before switching between HDR and SDR pipelines."
                : $"{combinedHint}{Environment.NewLine}Stop recording before switching between HDR and SDR pipelines.";
        }
        ToolTipService.SetToolTip(HdrToggle,
            string.IsNullOrWhiteSpace(combinedHint) ? null : combinedHint);
    }

    private void UpdateFpsTelemetryTooltip()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(ViewModel.SourceTelemetrySummaryText))
            parts.Add(ViewModel.SourceTelemetrySummaryText);
        if (!string.IsNullOrWhiteSpace(ViewModel.SourceTargetSummaryText))
            parts.Add(ViewModel.SourceTargetSummaryText);
        ToolTipService.SetToolTip(FrameRateComboBox,
            parts.Count > 0 ? string.Join(Environment.NewLine, parts) : null);
    }

    private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            action();
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        }

        var enqueued = _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                action();
                completion.TrySetResult(null);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
            finally
            {
                registration.Dispose();
            }
        });

        if (!enqueued)
        {
            registration.Dispose();
            completion.TrySetException(new InvalidOperationException("Failed to enqueue window action on the UI thread."));
        }

        return completion.Task;
    }

    private Microsoft.UI.Windowing.AppWindow GetAppWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        return Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
    }

    public Task MinimizeAsync(CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var appWindow = GetAppWindow();
            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.Minimize();
            }
        }, cancellationToken);
    }

    public Task MaximizeAsync(CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var appWindow = GetAppWindow();
            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
        }, cancellationToken);
    }

    public Task RestoreAsync(CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var appWindow = GetAppWindow();
            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                presenter.Restore();
            }
        }, cancellationToken);
    }

    public Task MoveToAsync(int x, int y, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var appWindow = GetAppWindow();
            appWindow.Move(new Windows.Graphics.PointInt32(x, y));
        }, cancellationToken);
    }

    public Task ResizeToAsync(int width, int height, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var appWindow = GetAppWindow();
            appWindow.Resize(new Windows.Graphics.SizeInt32(Math.Max(1, width), Math.Max(1, height)));
        }, cancellationToken);
    }

    public Task SnapToRegionAsync(AutomationWindowAction region, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var appWindow = GetAppWindow();
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
            var work = displayArea.WorkArea;

            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter &&
                presenter.State == Microsoft.UI.Windowing.OverlappedPresenterState.Maximized)
            {
                presenter.Restore();
            }

            int x, y, w, h;
            switch (region)
            {
                case AutomationWindowAction.SnapLeft:
                    x = work.X; y = work.Y; w = work.Width / 2; h = work.Height; break;
                case AutomationWindowAction.SnapRight:
                    x = work.X + work.Width / 2; y = work.Y; w = work.Width - work.Width / 2; h = work.Height; break;
                case AutomationWindowAction.SnapTopLeft:
                    x = work.X; y = work.Y; w = work.Width / 2; h = work.Height / 2; break;
                case AutomationWindowAction.SnapTopRight:
                    x = work.X + work.Width / 2; y = work.Y; w = work.Width - work.Width / 2; h = work.Height / 2; break;
                case AutomationWindowAction.SnapBottomLeft:
                    x = work.X; y = work.Y + work.Height / 2; w = work.Width / 2; h = work.Height - work.Height / 2; break;
                case AutomationWindowAction.SnapBottomRight:
                    x = work.X + work.Width / 2; y = work.Y + work.Height / 2; w = work.Width - work.Width / 2; h = work.Height - work.Height / 2; break;
                case AutomationWindowAction.Center:
                    var curSize = appWindow.Size;
                    w = curSize.Width; h = curSize.Height;
                    x = work.X + (work.Width - w) / 2; y = work.Y + (work.Height - h) / 2; break;
                default:
                    return;
            }

            appWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, w, h));
        }, cancellationToken);
    }

    public Task<WindowScreenshotResult> CaptureWindowScreenshotAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        var completion = new TaskCompletionSource<WindowScreenshotResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                var result = CaptureWindowScreenshotCore(outputPath);
                completion.TrySetResult(result);
            }
            catch (Exception ex)
            {
                completion.TrySetResult(new WindowScreenshotResult
                {
                    Succeeded = false,
                    Message = $"Screenshot failed: {ex.Message}"
                });
            }
        });
        return completion.Task;
    }

    private WindowScreenshotResult CaptureWindowScreenshotCore(string outputPath)
    {
        if (_hwnd == IntPtr.Zero)
        {
            return new WindowScreenshotResult { Succeeded = false, Message = "Window handle not available." };
        }

        if (!GetWindowRect(_hwnd, out var rect))
        {
            return new WindowScreenshotResult { Succeeded = false, Message = "Failed to get window rect." };
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
        {
            return new WindowScreenshotResult { Succeeded = false, Message = $"Invalid window size: {width}x{height}" };
        }

        var hdcWindow = GetDC(_hwnd);
        var hdcMemDC = CreateCompatibleDC(hdcWindow);
        var hBitmap = CreateCompatibleBitmap(hdcWindow, width, height);
        var hOld = SelectObject(hdcMemDC, hBitmap);

        try
        {
            // PW_RENDERFULLCONTENT captures DWM-composited content including D3D swap chains
            if (!PrintWindow(_hwnd, hdcMemDC, PW_RENDERFULLCONTENT))
            {
                return new WindowScreenshotResult { Succeeded = false, Message = "PrintWindow failed." };
            }

            SelectObject(hdcMemDC, hOld);

            // Write as PNG using System.Drawing interop
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            SaveHBitmapAsImage(hBitmap, width, height, outputPath);

            var fileInfo = new FileInfo(outputPath);
            return new WindowScreenshotResult
            {
                Succeeded = true,
                Message = $"Window screenshot saved: {width}x{height}",
                FilePath = outputPath,
                CapturedWidth = width,
                CapturedHeight = height,
                FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0
            };
        }
        finally
        {
            DeleteObject(hBitmap);
            DeleteDC(hdcMemDC);
            ReleaseDC(_hwnd, hdcWindow);
        }
    }

    private static void SaveHBitmapAsImage(IntPtr hBitmap, int width, int height, string outputPath)
    {
        var bmi = new BITMAPINFOHEADER
        {
            biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
            biWidth = width,
            biHeight = -height, // top-down
            biPlanes = 1,
            biBitCount = 32,
            biCompression = 0 // BI_RGB
        };

        var stride = width * 4;
        var pixelData = new byte[stride * height];

        var hdcScreen = GetDC(IntPtr.Zero);
        GetDIBits(hdcScreen, hBitmap, 0, (uint)height, pixelData, ref bmi, 0);
        ReleaseDC(IntPtr.Zero, hdcScreen);

        using var stream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
        if (outputPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            WritePngToStream(stream, width, height, pixelData);
        else
            WriteBmpToStream(stream, width, height, pixelData);
    }

    private static void WritePngToStream(Stream output, int width, int height, byte[] bgra)
    {
        var stride = width * 4;

        // PNG signature
        output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });

        // IHDR chunk
        var ihdr = new byte[13];
        WriteBE32(ihdr, 0, width);
        WriteBE32(ihdr, 4, height);
        ihdr[8] = 8;  // bit depth
        ihdr[9] = 6;  // color type: RGBA
        WritePngChunk(output, new byte[] { 73, 72, 68, 82 }, ihdr); // "IHDR"

        // Raw scanlines: filter byte (0=None) + RGBA pixels per row
        var raw = new byte[(stride + 1) * height];
        for (var y = 0; y < height; y++)
        {
            var rowDst = y * (stride + 1);
            raw[rowDst] = 0; // filter: None
            var rowSrc = y * stride;
            for (var x = 0; x < width; x++)
            {
                var s = rowSrc + x * 4;
                var d = rowDst + 1 + x * 4;
                raw[d]     = bgra[s + 2]; // R (from BGRA B)
                raw[d + 1] = bgra[s + 1]; // G
                raw[d + 2] = bgra[s];     // B (from BGRA R)
                raw[d + 3] = 255;         // A
            }
        }

        // Compress with zlib and write IDAT
        byte[] compressed;
        using (var ms = new MemoryStream())
        {
            using (var zlib = new System.IO.Compression.ZLibStream(ms, System.IO.Compression.CompressionLevel.Fastest, leaveOpen: true))
                zlib.Write(raw);
            compressed = ms.ToArray();
        }
        WritePngChunk(output, new byte[] { 73, 68, 65, 84 }, compressed); // "IDAT"

        // IEND
        WritePngChunk(output, new byte[] { 73, 69, 78, 68 }, Array.Empty<byte>()); // "IEND"
    }

    private static void WritePngChunk(Stream output, byte[] type, byte[] data)
    {
        var buf = new byte[4];
        WriteBE32(buf, 0, data.Length);
        output.Write(buf);
        output.Write(type);
        if (data.Length > 0) output.Write(data);
        var crc = PngCrc32(type, data);
        buf[0] = (byte)(crc >> 24); buf[1] = (byte)(crc >> 16);
        buf[2] = (byte)(crc >> 8);  buf[3] = (byte)crc;
        output.Write(buf);
    }

    private static void WriteBE32(byte[] buf, int off, int val)
    {
        buf[off] = (byte)(val >> 24); buf[off + 1] = (byte)(val >> 16);
        buf[off + 2] = (byte)(val >> 8); buf[off + 3] = (byte)val;
    }

    private static uint PngCrc32(byte[] type, byte[] data)
    {
        uint c = 0xFFFFFFFF;
        foreach (var b in type) c = (c >> 8) ^ _crc32Table[(c ^ b) & 0xFF];
        foreach (var b in data) c = (c >> 8) ^ _crc32Table[(c ^ b) & 0xFF];
        return c ^ 0xFFFFFFFF;
    }

    private static readonly uint[] _crc32Table = InitCrc32Table();
    private static uint[] InitCrc32Table()
    {
        var t = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            var c = i;
            for (var j = 0; j < 8; j++)
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            t[i] = c;
        }
        return t;
    }

    private static void WriteBmpToStream(Stream stream, int width, int height, byte[] bgra)
    {
        var stride = width * 4;
        var pixelDataSize = stride * height;
        var fileSize = 14 + 40 + pixelDataSize;

        using var writer = new BinaryWriter(stream);
        writer.Write((ushort)0x4D42); // 'BM'
        writer.Write(fileSize);
        writer.Write(0);
        writer.Write(14 + 40);

        writer.Write(40);
        writer.Write(width);
        writer.Write(-height); // top-down
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write(0);
        writer.Write(pixelDataSize);
        writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);

        writer.Write(bgra);
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr ho);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(IntPtr hdc, IntPtr hbm, uint start, uint cLines,
        [Out] byte[] lpvBits, ref BITMAPINFOHEADER lpbmi, uint usage);

    private const uint PW_RENDERFULLCONTENT = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
    {
        public uint biSize;
        public int biWidth;
        public int biHeight;
        public ushort biPlanes;
        public ushort biBitCount;
        public uint biCompression;
        public uint biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public uint biClrUsed;
        public uint biClrImportant;
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _windowCloseCleanupStarted) != 0)
        {
            return Task.CompletedTask;
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            RequestWindowClose();
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration registration = default;
        if (cancellationToken.CanBeCanceled)
        {
            registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        }

        var enqueued = _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    completion.TrySetCanceled(cancellationToken);
                    return;
                }

                RequestWindowClose();
                completion.TrySetResult(null);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
            finally
            {
                registration.Dispose();
            }
        });

        if (!enqueued)
        {
            registration.Dispose();
            if (Volatile.Read(ref _windowCloseCleanupStarted) != 0)
            {
                completion.TrySetResult(null);
            }
            else
            {
                completion.TrySetException(new InvalidOperationException("Failed to enqueue window close action on the UI thread."));
            }
        }

        return completion.Task;
    }

    private void RequestWindowClose()
    {
        if (Volatile.Read(ref _windowCloseCleanupStarted) != 0)
        {
            return;
        }

        if (Interlocked.Exchange(ref _windowCloseRequested, 1) != 0)
        {
            return;
        }

        try
        {
            Close();
        }
        catch (Exception ex) when (IsCloseAlreadyInProgressException(ex))
        {
            Logger.Log($"Window close already in progress ({ex.GetType().Name}); treating close request as successful.");
        }
        catch (System.Runtime.InteropServices.COMException ex)
        {
            Logger.Log($"Window.Close COMException (0x{ex.HResult:X8}); using Application.Current.Exit() fallback.");
            Application.Current.Exit();
        }
        catch
        {
            Interlocked.Exchange(ref _windowCloseRequested, 0);
            throw;
        }
    }

    private static bool IsCloseAlreadyInProgressException(Exception ex)
    {
        if (ex is InvalidOperationException && string.IsNullOrWhiteSpace(ex.Message))
        {
            return true;
        }

        var message = ex.Message ?? string.Empty;
        return message.IndexOf("closing", StringComparison.OrdinalIgnoreCase) >= 0 ||
               message.IndexOf("closed", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private async Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)
    {
        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            Logger.LogException(ex);
            ViewModel.StatusText = $"{operationName} failed: {ex.Message}";
        }
    }

    private Task StartPreviewRendererAsync()
    {
        _previewFramesArrived = 0;
        _previewFramesDisplayed = 0;
        _previewFramesDropped = 0;
        _previewLastResizeLogTick = 0;
        _previewLastPresentedTick = 0;
        _previewMinPresentationIntervalMs = ResolvePreviewExpectedIntervalMs();

        var useD3dRenderer = ViewModel.IsPreviewing;
        if (useD3dRenderer)
        {
            // D3D11 Video Processor preview path
            var renderer = new D3D11PreviewRenderer(PreviewSwapChainPanel, _dispatcherQueue);
            renderer.FirstFrameRendered += OnD3DRendererFirstFrameRendered;
            var settings = ViewModel.GetCurrentSettings();
            var sourceProbe = ViewModel.ProbeVideoSource();
            var isHdr = settings != null && HdrOutputPolicy.IsEnabled(settings);
            var width = (int)(settings?.Width ?? 1920);
            var height = (int)(settings?.Height ?? 1080);
            var fps = settings?.FrameRate ?? 60.0;
            var negotiatedWidth = sourceProbe.SessionActive ? sourceProbe.CurrentWidth : 0;
            var negotiatedHeight = sourceProbe.SessionActive ? sourceProbe.CurrentHeight : 0;
            var negotiatedFps = sourceProbe.SessionActive ? sourceProbe.CurrentFrameRate : 0.0;
            var rendererWidth = negotiatedWidth > 0 ? negotiatedWidth : width;
            var rendererHeight = negotiatedHeight > 0 ? negotiatedHeight : height;
            var rendererFps = negotiatedFps > 0 ? negotiatedFps : fps;
            _previewMinPresentationIntervalMs = Math.Max(1L, (long)Math.Round(1000.0 / rendererFps));

            // Wire SizeChanged and make panel visible BEFORE starting the render
            // thread so the renderer has the panel's pixel dimensions from the start.
            _d3dRenderer = renderer;
            SetupVideoFrameShadow();
            PreviewSwapChainPanel.SizeChanged += OnPreviewSwapChainPanelSizeChanged;
            PreviewContentGrid.SizeChanged += OnPreviewContentGridSizeChanged;
            SetGpuPreviewVisibility(Visibility.Visible);
            PreviewImage.Visibility = Visibility.Collapsed;

            // Pre-seed panel size and renderer dimensions.
            // Force layout so the container has ActualWidth/Height, then
            // UpdateVideoContentOverlays sets the panel to fitW x fitH.
            PreviewSwapChainPanel.UpdateLayout();
            UpdateVideoContentOverlays();
            PreviewSwapChainPanel.UpdateLayout();
            var panelW = PreviewSwapChainPanel.ActualWidth;
            var panelH = PreviewSwapChainPanel.ActualHeight;
            if (panelW > 0 && panelH > 0)
            {
                var scale = PreviewSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;
                renderer.OnPanelSizeChanged(panelW, panelH, scale);
            }

            renderer.Start(rendererWidth, rendererHeight, rendererFps, isHdr);
            if (isHdr && ViewModel.IsTrueHdrPreviewEnabled)
            {
                renderer.SetHdrPassthroughEnabled(true);
            }

            ViewModel.SetPreviewFrameSink(_d3dRenderer);
            _previewStartupExpectGpuDualSignals = false; // D3D renderer uses FirstFrameRendered, not MediaPlayer signals
            ConfigurePreviewStartupSignals(
                PreviewStartupStrategy.D3D11VideoProcessor,
                PreviewStartupSignalFlags.FirstVisual);
            _previewRendererAttachedUtc = DateTimeOffset.UtcNow;

            Logger.Log("Preview renderer started (mode=D3D11VideoProcessor).");
            Logger.Log($"PREVIEW_RENDERER_ATTACHED mode=D3D11VideoProcessor attempt={_previewStartupAttemptId ?? "none"}");
        }
        else
        {
            // Fallback CPU preview path: SoftwareBitmapSource -> Image (unchanged)
            SetupVideoFrameShadow();
            PreviewContentGrid.SizeChanged += OnPreviewContentGridSizeChanged;
            ViewModel.SetPreviewFrameSink(null);
            _previewStartupExpectGpuDualSignals = false;
            ConfigurePreviewStartupSignals(PreviewStartupStrategy.CpuSoftwareBitmap, PreviewStartupSignalFlags.FirstVisual);
            _previewSource = new SoftwareBitmapSource();
            _previewRendererAttachedUtc = DateTimeOffset.UtcNow;
            PreviewImage.Source = _previewSource;
            PreviewImage.Visibility = Visibility.Visible;
            SetGpuPreviewVisibility(Visibility.Collapsed);
            Logger.Log($"Preview renderer started (mode=CpuSoftwareBitmap, expectedIntervalMs={_previewMinPresentationIntervalMs}).");
            Logger.Log($"PREVIEW_RENDERER_ATTACHED mode=CpuSoftwareBitmap attempt={_previewStartupAttemptId ?? "none"}");
        }

        return Task.CompletedTask;
    }

    private Task StopPreviewRendererAsync()
    {
        // Clean up composition shadow
        _videoShadowVisual = null;
        ElementCompositionPreview.SetElementChildVisual(VideoShadowHost, null);

        // Clean up D3D11 preview path
        PreviewContentGrid.SizeChanged -= OnPreviewContentGridSizeChanged;
        var renderer = _d3dRenderer;
        _d3dRenderer = null;
        if (renderer != null)
        {
            PreviewSwapChainPanel.SizeChanged -= OnPreviewSwapChainPanelSizeChanged;
            renderer.FirstFrameRendered -= OnD3DRendererFirstFrameRendered;
            renderer.Stop();
            renderer.Dispose();
        }
        ViewModel.SetPreviewFrameSink(null);
        SetGpuPreviewVisibility(Visibility.Collapsed);
        _previewStartupExpectGpuDualSignals = false;
        _previewGpuSignalMediaOpened = false;
        _previewGpuSignalFirstFrame = false;
        _previewGpuSignalPlaybackAdvancing = false;
        _previewStartupRequiredSignals = PreviewStartupSignalFlags.None;
        _previewStartupReceivedSignals = PreviewStartupSignalFlags.None;
        _previewStartupStrategy = PreviewStartupStrategy.None;
        _previewStartupLastPlaybackPosition = TimeSpan.Zero;
        _previewStartupPlaybackPositionInitialized = false;

        // Clean up CPU preview path
        PreviewImage.Source = null;
        PreviewImage.Visibility = Visibility.Collapsed;
        _previewSource = null;

        _previewLastPresentedTick = 0;
        _previewMinPresentationIntervalMs = Math.Max(1L, (long)Math.Round(1000.0 / 60.0));
        Logger.Log("Preview renderer stopped.");
        return Task.CompletedTask;
    }

    private void AnimateAudioMeterTick()
    {
        var target = _audioMeterTargetLevel;
        var nowMs = Environment.TickCount64;

        // Smoothly interpolate display level toward target
        if (target >= _audioMeterDisplayLevel)
        {
            // Attack: fast snap toward peaks
            _audioMeterDisplayLevel += (target - _audioMeterDisplayLevel) * 0.4;
        }
        else
        {
            // Decay: smooth falloff
            _audioMeterDisplayLevel += (target - _audioMeterDisplayLevel) * 0.06;
        }

        // Snap to zero when very close to avoid lingering
        if (_audioMeterDisplayLevel < 0.001)
        {
            _audioMeterDisplayLevel = 0;
        }

        // Peak hold
        if (target >= _audioPeakHoldLevel)
        {
            _audioPeakHoldLevel = target;
            _audioPeakHoldTimestamp = nowMs;
        }
        else if (nowMs - _audioPeakHoldTimestamp > AudioPeakHoldDurationMs)
        {
            var dt = (nowMs - _audioPeakHoldTimestamp - AudioPeakHoldDurationMs) / 1000.0;
            _audioPeakHoldLevel = Math.Max(0, _audioPeakHoldLevel - (AudioPeakHoldDecayPerSecond * dt));
            _audioPeakHoldTimestamp = nowMs - AudioPeakHoldDurationMs;
        }

        // Range tracking
        if (nowMs - _audioRangeResetTimestamp > AudioRangeWindowMs)
        {
            _audioRangeMin = target;
            _audioRangeMax = target;
            _audioRangeResetTimestamp = nowMs;
        }
        else
        {
            if (target < _audioRangeMin) _audioRangeMin = target;
            if (target > _audioRangeMax) _audioRangeMax = target;
        }

        // Update visuals
        var trackWidth = AudioMeterTrack.ActualWidth;
        if (trackWidth <= 0) return;

        var trackHeight = AudioMeterTrack.ActualHeight > 0 ? AudioMeterTrack.ActualHeight : 12;
        AudioMeterClip.Rect = new Windows.Foundation.Rect(0, 0, trackWidth * _audioMeterDisplayLevel, trackHeight);
        AudioPeakHoldTranslate.X = TranslateMarker(trackWidth, _audioPeakHoldLevel, AudioPeakHoldIndicator.Width);
        AudioRangeMinTranslate.X = TranslateMarker(trackWidth, _audioRangeMin, AudioRangeMinMarker.Width);
        AudioRangeMaxTranslate.X = TranslateMarker(trackWidth, _audioRangeMax, AudioRangeMaxMarker.Width);
    }

    private void ResetAudioMeterVisuals()
    {
        _audioPeakHoldLevel = 0;
        _audioPeakHoldTimestamp = 0;
        _audioRangeMin = 1.0;
        _audioRangeMax = 0;
        _audioRangeResetTimestamp = 0;
        _audioMeterDisplayLevel = 0;
        AudioPeakHoldTranslate.X = 0;
        AudioRangeMinTranslate.X = 0;
        AudioRangeMaxTranslate.X = 0;
        _audioMeterTargetLevel = 0;
        _audioMeterDisplayLevel = 0;
    }

    private void InitializeAudioMeterBrushes()
    {
        _audioMeterAnimationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _audioMeterAnimationTimer.Tick += (_, _) => AnimateAudioMeterTick();
        _audioMeterAnimationTimer.Start();

        _audioMeterColorBrush = (LinearGradientBrush)AudioMeterFill.Background;

        _audioMeterGreyBrush = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0, 0.5),
            EndPoint = new Windows.Foundation.Point(1, 0.5)
        };
        _audioMeterGreyBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 96, 96, 96), Offset = 0 });
        _audioMeterGreyBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 120, 120, 120), Offset = 0.55 });
        _audioMeterGreyBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 140, 140, 140), Offset = 0.75 });
        _audioMeterGreyBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 110, 110, 110), Offset = 0.90 });
        _audioMeterGreyBrush.GradientStops.Add(new GradientStop { Color = Windows.UI.Color.FromArgb(255, 90, 90, 90), Offset = 1 });
    }

    private void SetAudioMeterMonitoringState(bool isMonitoring)
    {
        if (_audioMeterColorBrush == null) return;

        AudioMeterFill.Background = isMonitoring ? _audioMeterColorBrush : _audioMeterGreyBrush;
        AudioPeakHoldIndicator.Background = isMonitoring
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 160, 160, 160));
        AudioPeakHoldIndicator.Opacity = isMonitoring ? 0.9 : 0.4;
        AudioRangeMinMarker.Opacity = isMonitoring ? 0.5 : 0.2;
        AudioRangeMaxMarker.Opacity = isMonitoring ? 0.7 : 0.3;
    }

    private void AnimateAudioMeterDisabled(bool isDisabled)
    {
        var targetOpacity = isDisabled ? 0.0 : 1.0;
        var duration = TimeSpan.FromMilliseconds(300);
        var easing = new CubicEase { EasingMode = isDisabled ? EasingMode.EaseIn : EasingMode.EaseOut };

        var storyboard = new Storyboard();

        foreach (var element in new UIElement[] { AudioMeterFill, AudioPeakHoldIndicator, AudioRangeMinMarker, AudioRangeMaxMarker })
        {
            var anim = new DoubleAnimation
            {
                To = targetOpacity,
                Duration = new Duration(duration),
                EasingFunction = easing,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(anim, element);
            Storyboard.SetTargetProperty(anim, "Opacity");
            storyboard.Children.Add(anim);
        }

        if (!isDisabled)
        {
            storyboard.Completed += (_, _) =>
            {
                SetAudioMeterMonitoringState(ViewModel.IsAudioPreviewEnabled);
            };
        }

        storyboard.Begin();
    }

    private static double TranslateMarker(double trackWidth, double level, double markerWidth)
    {
        var clamped = Math.Clamp(level, 0.0, 1.0);
        var availableWidth = Math.Max(0, trackWidth - markerWidth);
        return availableWidth * clamped;
    }

    private void AnimateRecordButtonWidth(double from, double to, Action? onCompleted = null)
    {
        var anim = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(anim, RecordButton);
        Storyboard.SetTargetProperty(anim, "Width");

        var sb = new Storyboard();
        sb.Children.Add(anim);
        sb.Completed += (_, _) =>
        {
            // Set final width explicitly (NaN for pill, 36 for circle)
            RecordButton.Width = to == 36 ? 36 : double.NaN;
            onCompleted?.Invoke();
        };
        sb.Begin();
    }

    private static void AnimateScale(ScaleTransform target, double to, TimeSpan duration)
    {
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

        var scaleX = new DoubleAnimation
        {
            To = to,
            Duration = new Duration(duration),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(scaleX, target);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");

        var scaleY = new DoubleAnimation
        {
            To = to,
            Duration = new Duration(duration),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(scaleY, target);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");

        var storyboard = new Storyboard();
        storyboard.Children.Add(scaleX);
        storyboard.Children.Add(scaleY);
        storyboard.Begin();
    }

    private void ResetPreviewContentTransform()
    {
        PreviewContentGrid.Opacity = 1.0;
        PreviewContentScale.ScaleX = 1.0;
        PreviewContentScale.ScaleY = 1.0;
    }

    private static Task BeginStoryboardAsync(Storyboard storyboard)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        storyboard.Completed += (_, _) => tcs.TrySetResult(true);
        storyboard.Begin();
        return tcs.Task;
    }

    private Task AnimatePreviewOutAsync()
    {
        var duration = TimeSpan.FromMilliseconds(200);

        var fadeOut = new DoubleAnimation
        {
            To = 0.0,
            Duration = new Duration(duration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(fadeOut, PreviewContentGrid);
        Storyboard.SetTargetProperty(fadeOut, "Opacity");

        var scaleX = new DoubleAnimation
        {
            To = 0.97,
            Duration = new Duration(duration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(scaleX, PreviewContentScale);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");

        var scaleY = new DoubleAnimation
        {
            To = 0.97,
            Duration = new Duration(duration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(scaleY, PreviewContentScale);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");

        var storyboard = new Storyboard();
        storyboard.Children.Add(fadeOut);
        storyboard.Children.Add(scaleX);
        storyboard.Children.Add(scaleY);

        // Fade out the video shadow first (shorter duration so it recedes before the preview)
        FadeOutShadow(_videoShadowVisual, durationMs: 150);

        return BeginStoryboardAsync(storyboard);
    }

    private Task AnimatePreviewInAsync()
    {
        var duration = TimeSpan.FromMilliseconds(250);

        var fadeIn = new DoubleAnimation
        {
            To = 1.0,
            Duration = new Duration(duration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(fadeIn, PreviewContentGrid);
        Storyboard.SetTargetProperty(fadeIn, "Opacity");

        var scaleX = new DoubleAnimation
        {
            To = 1.0,
            Duration = new Duration(duration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(scaleX, PreviewContentScale);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");

        var scaleY = new DoubleAnimation
        {
            To = 1.0,
            Duration = new Duration(duration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(scaleY, PreviewContentScale);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");

        var storyboard = new Storyboard();
        storyboard.Children.Add(fadeIn);
        storyboard.Children.Add(scaleX);
        storyboard.Children.Add(scaleY);

        // Video shadow gains depth alongside the preview
        FadeInShadow(_videoShadowVisual, delayMs: 0, durationMs: 400);

        return BeginStoryboardAsync(storyboard);
    }


    private static void FadeOutElement(UIElement element)
    {
        var animation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200))
        };
        var storyboard = new Storyboard();
        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, "Opacity");
        storyboard.Completed += (_, _) =>
        {
            element.Visibility = Visibility.Collapsed;
            element.Opacity = 1.0;
        };
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private static void FadeInElement(UIElement element)
    {
        element.Opacity = 0.0;
        element.Visibility = Visibility.Visible;
        var animation = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200))
        };
        var storyboard = new Storyboard();
        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, "Opacity");
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(async () =>
        {
            RefreshButton.Content = new Microsoft.UI.Xaml.Controls.ProgressRing { Width = 16, Height = 16, IsActive = true };
            RefreshButton.IsEnabled = false;
            try
            {
                await ViewModel.RefreshDevicesAsync();
            }
            finally
            {
                RefreshButton.Content = new Microsoft.UI.Xaml.Controls.FontIcon { Glyph = "\uE72C", FontSize = 14 };
                RefreshButton.IsEnabled = true;
            }
        }, nameof(RefreshButton_Click));
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(async () =>
        {
            if (ViewModel.IsPreviewReinitializing && !ViewModel.IsPreviewing)
            {
                _previewStopRequestedByUser = true;
                ViewModel.CancelPendingPreviewRestart();
                Logger.Log($"PREVIEW_REINIT_CANCEL_REQUESTED attempt={_previewStartupAttemptId ?? "none"}");
                return;
            }

            if (ViewModel.IsPreviewing)
            {
                _previewStopRequestedByUser = true;
                StopPreviewFadeInTimer();
                await AnimatePreviewOutAsync();
                try
                {
                    await ViewModel.StopPreviewAsync(userInitiated: true);
                }
                finally
                {
                    _isPreviewReinitAnimating = false;
                    ResetPreviewContentTransform();
                }
            }
            else
            {
                _previewStopRequestedByUser = false;
                await ViewModel.StartPreviewAsync(userInitiated: true);
            }
        }, nameof(PreviewButton_Click));
    }

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(async () =>
        {
            await ViewModel.ToggleRecordingAsync();

            if (ViewModel.IsRecording)
            {
                var gpuActive = _d3dRenderer != null && PreviewSwapChainPanel.Visibility == Visibility.Visible;
                var cpuActive = _previewSource != null && PreviewImage.Visibility == Visibility.Visible;
                var rendererActive = gpuActive || cpuActive;
                var placeholderVisible = NoDevicePlaceholder.Visibility == Visibility.Visible;
                Logger.Log(
                    $"PreviewStateDuringRecording: rendererActive={rendererActive}, " +
                    $"gpuActive={gpuActive}, cpuActive={cpuActive}, " +
                    $"placeholderVisible={placeholderVisible}");

                if (!rendererActive || placeholderVisible)
                {
                    Logger.Log("WARNING: preview renderer appears inactive while recording.");
                }
            }
        }, nameof(RecordButton_Click));
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(() => ViewModel.BrowseOutputPathAsync(), nameof(BrowseButton_Click));
    }

    private void OpenRecordingsButton_Click(object sender, RoutedEventArgs e)
    {
        var path = ViewModel.OutputPath;
        if (!string.IsNullOrWhiteSpace(path) && System.IO.Directory.Exists(path))
        {
            System.Diagnostics.Process.Start("explorer.exe", path);
        }
    }

    private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
    {
        _ = RunUiEventHandlerAsync(async () =>
        {
            if (!ViewModel.IsPreviewing)
            {
                ViewModel.StatusText = "Start preview before capturing a screenshot";
                return;
            }

            var outputDir = ViewModel.OutputPath;
            if (string.IsNullOrWhiteSpace(outputDir))
                outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "ElgatoCapture");

            Directory.CreateDirectory(outputDir);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var filePath = Path.Combine(outputDir, $"Screenshot_{timestamp}.png");

            ScreenshotButton.IsEnabled = false;
            try
            {
                var result = await ViewModel.CapturePreviewFrameAsync(filePath);
                if (result.Succeeded)
                {
                    ViewModel.StatusText = $"Screenshot saved: {Path.GetFileName(filePath)}";
                    Logger.Log($"SCREENSHOT_SAVED path={filePath} width={result.CapturedWidth} height={result.CapturedHeight}");
                }
                else
                {
                    ViewModel.StatusText = $"Screenshot failed: {result.Message}";
                    Logger.Log($"SCREENSHOT_FAILED reason={result.Message}");
                }
            }
            finally
            {
                ScreenshotButton.IsEnabled = true;
            }
        }, nameof(ScreenshotButton_Click));
    }

    private void SettingsToggleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isSettingsShelfAnimating)
        {
            return;
        }

        if (SettingsOverlayPanel.Visibility == Visibility.Visible)
        {
            HideSettingsShelf();
        }
        else
        {
            ShowSettingsShelf();
        }
    }

    private void ShowSettingsShelf()
    {
        _isSettingsShelfAnimating = true;
        SettingsOverlayPanel.Opacity = 0;
        SettingsOverlayPanel.Visibility = Visibility.Visible;
        SettingsShelfTranslate.Y = 40;
        var storyboard = new Storyboard();
        var fade = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(fade, SettingsOverlayPanel);
        Storyboard.SetTargetProperty(fade, "Opacity");
        var slide = new DoubleAnimation
        {
            From = 40,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(slide, SettingsShelfTranslate);
        Storyboard.SetTargetProperty(slide, "Y");
        storyboard.Children.Add(fade);
        storyboard.Children.Add(slide);
        storyboard.Completed += (_, _) => _isSettingsShelfAnimating = false;
        storyboard.Begin();
    }

    private void HideSettingsShelf()
    {
        _isSettingsShelfAnimating = true;
        var storyboard = new Storyboard();
        var fade = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(fade, SettingsOverlayPanel);
        Storyboard.SetTargetProperty(fade, "Opacity");
        var slide = new DoubleAnimation
        {
            From = 0,
            To = 40,
            Duration = TimeSpan.FromMilliseconds(200),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(slide, SettingsShelfTranslate);
        Storyboard.SetTargetProperty(slide, "Y");
        storyboard.Children.Add(fade);
        storyboard.Children.Add(slide);
        storyboard.Completed += (_, _) =>
        {
            SettingsOverlayPanel.Visibility = Visibility.Collapsed;
            SettingsOverlayPanel.Opacity = 1;
            SettingsShelfTranslate.Y = 0;
            _isSettingsShelfAnimating = false;
        };
        storyboard.Begin();
    }

    #region Minimum window size (Win32 interop)

    private const int GWLP_WNDPROC = -4;
    private const uint WM_GETMINMAXINFO = 0x0024;

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_CLOAK = 13;

    private IntPtr MinSizeWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_GETMINMAXINFO)
        {
            var dpi = GetDpiForWindow(hWnd);
            var scale = dpi / 96.0;
            var mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            mmi.ptMinTrackSize.X = (int)(MinWindowWidth * scale);
            mmi.ptMinTrackSize.Y = (int)(MinWindowHeight * scale);
            Marshal.StructureToPtr(mmi, lParam, false);
        }
        return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
    }

    #endregion
}

