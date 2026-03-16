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
using Microsoft.UI.Xaml.Input;
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
    private static readonly int PreviewStartupVisualTimeoutMs = EnvironmentHelpers.GetIntFromEnv(
        "ELGATOCAPTURE_PREVIEW_START_TIMEOUT_MS",
        PreviewStartupDefaultVisualTimeoutMs,
        PreviewStartupMinVisualTimeoutMs,
        PreviewStartupMaxVisualTimeoutMs);

    public MainViewModel ViewModel { get; }
    private readonly DispatcherQueue _dispatcherQueue;
    private SoftwareBitmapSource? _previewSource;
    private D3D11PreviewRenderer? _d3dRenderer;
    private NvmlMonitor? _nvmlMonitor;
    private SpriteVisual? _videoShadowVisual;
    private SpriteVisual? _controlBarShadowVisual;
    private DispatcherQueueTimer? _statsPollTimer;
    private Storyboard? _statsDockStoryboard;
    private Storyboard? _showStatsDockStoryboard;
    private Storyboard? _hideStatsDockStoryboard;
    private Storyboard? _micMeterRowStoryboard;
    private Storyboard? _showMicMeterRowStoryboard;
    private Storyboard? _hideMicMeterRowStoryboard;
    private const double MicMeterRowHeight = 14;
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
    private readonly int[] _selectionSyncQueued = new int[9];
    private const int SyncDevice = 0, SyncAudio = 1, SyncResolution = 2, SyncFrameRate = 3,
                       SyncFormat = 4, SyncQuality = 5, SyncPreset = 6, SyncSplitEncode = 7,
                       SyncMicrophone = 8;
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
    private bool _isFullScreen;
    private bool _isFullScreenTransitioning;
    private Windows.Graphics.RectInt32 _preFullScreenBounds;
    private Windows.Graphics.PointInt32 _preFullScreenPosition;
    private bool _preFullScreenSettingsVisible;
    private bool _preFullScreenStatsDockVisible;
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
    private double _micMeterDisplayLevel;
    private double _micMeterTargetLevel;
    private bool _syncingMicrophoneVolumeControls;
    private int _selectedDecoderCount = 4;
    private LinearGradientBrush? _audioMeterColorBrush;
    private DispatcherTimer? _audioMeterAnimationTimer;
    private readonly List<DiagnosticRowSlot> _decodeRowPool = new();
    private readonly List<DiagnosticRowSlot> _gpuRowPool = new();
    private readonly List<DiagnosticsPoolSlot> _diagnosticsRowPool = new();
    private TextBlock? _diagnosticsEmptyStateTextBlock;

    private const long AudioPeakHoldDurationMs = 1500;
    private const double AudioPeakHoldDecayPerSecond = 0.8;
    private const long AudioRangeWindowMs = 3000;
    private const int MaxExpectedDecodeRowCount = 14;
    private const int FixedGpuRowCount = 10;

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


    private void ResetPreviewSignalState()
    {
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
        ResetPreviewSignalState();
        Interlocked.Exchange(ref _previewStartupFailureStopScheduled, 0);
        Interlocked.Exchange(ref _previewStartupLastPositionDispatchTick, 0);

        SetPreviewStartupState(PreviewStartupState.StartingSession);
        Logger.Log(
            $"PREVIEW_START_REQUESTED attempt={_previewStartupAttemptId} " +
            $"device={ViewModel.SelectedDevice?.Name ?? "none"}");
    }

    private void ConfigurePreviewStartupSignals(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)
    {
        ResetPreviewSignalState();
        _previewStartupStrategy = strategy;
        _previewStartupRequiredSignals = requiredSignals;
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
        ResetPreviewSignalState();
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

    private void EnsureMicrophoneSelection()
    {
        if (ViewModel.MicrophoneDevices.Count == 0)
        {
            MicrophoneComboBox.SelectedItem = null;
            return;
        }

        var matchingDevice = ViewModel.SelectedMicrophoneDevice != null
            ? ViewModel.MicrophoneDevices.FirstOrDefault(device =>
                string.Equals(device.Id, ViewModel.SelectedMicrophoneDevice.Id, StringComparison.OrdinalIgnoreCase))
            : null;
        matchingDevice ??= ViewModel.MicrophoneDevices.FirstOrDefault();
        if (matchingDevice == null)
        {
            return;
        }

        if (!ReferenceEquals(ViewModel.SelectedMicrophoneDevice, matchingDevice))
        {
            ViewModel.SelectedMicrophoneDevice = matchingDevice;
        }

        if (!ReferenceEquals(MicrophoneComboBox.SelectedItem, matchingDevice))
        {
            MicrophoneComboBox.SelectedItem = matchingDevice;
        }
    }

    private void EnsureDeviceAudioModeSelection()
    {
        if (ViewModel.AvailableDeviceAudioModes.Count == 0)
        {
            return;
        }

        var selectedMode = ViewModel.SelectedDeviceAudioMode;
        var matchingMode = ViewModel.AvailableDeviceAudioModes.FirstOrDefault(mode =>
            string.Equals(mode, selectedMode, StringComparison.OrdinalIgnoreCase))
            ?? ViewModel.AvailableDeviceAudioModes.FirstOrDefault();
        if (matchingMode == null)
        {
            return;
        }

        if (!string.Equals(ViewModel.SelectedDeviceAudioMode, matchingMode, StringComparison.OrdinalIgnoreCase))
        {
            ViewModel.SelectedDeviceAudioMode = matchingMode;
        }

        var shouldBeOn = string.Equals(matchingMode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase);
        if (DeviceAudioModeToggle.IsOn != shouldBeOn)
        {
            DeviceAudioModeToggle.IsOn = shouldBeOn;
        }
    }

    private void ApplyDeviceAudioControlState()
    {
        DeviceAudioControlPanel.Visibility = ViewModel.IsDeviceAudioControlSupported ? Visibility.Visible : Visibility.Collapsed;
        EnsureDeviceAudioModeSelection();

        var analogGain = Math.Clamp(ViewModel.AnalogAudioGainPercent, 0.0, 100.0);
        if (Math.Abs(AnalogAudioGainSlider.Value - analogGain) > 0.1)
        {
            AnalogAudioGainSlider.Value = analogGain;
        }

        AnalogAudioGainValueTextBlock.Text = $"{(int)Math.Round(analogGain)}%";
        var analogModeActive = string.Equals(ViewModel.SelectedDeviceAudioMode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase);
        AnalogAudioGainPanel.Visibility = ViewModel.IsDeviceAudioControlSupported && analogModeActive ? Visibility.Visible : Visibility.Collapsed;
        AnalogAudioGainSlider.IsEnabled = ViewModel.IsDeviceAudioControlSupported && analogModeActive && !ViewModel.IsRecording;
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

    private static void EnsureStringComboBoxSelection(
        ComboBox comboBox,
        System.Collections.ObjectModel.ObservableCollection<string> items,
        Func<string?> getVmProp,
        Action<string> setVmProp)
    {
        if (items.Count == 0)
        {
            comboBox.SelectedItem = null;
            return;
        }

        var vmValue = getVmProp();
        var match = items.FirstOrDefault(item => string.Equals(item, vmValue, StringComparison.OrdinalIgnoreCase))
            ?? items.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(match))
        {
            return;
        }

        if (!string.Equals(match, vmValue, StringComparison.OrdinalIgnoreCase))
        {
            setVmProp(match);
        }

        if (!string.Equals(comboBox.SelectedItem as string, match, StringComparison.OrdinalIgnoreCase))
        {
            comboBox.SelectedItem = match;
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

        EnsureStringComboBoxSelection(FormatComboBox, ViewModel.AvailableRecordingFormats,
            () => ViewModel.SelectedRecordingFormat, v => ViewModel.SelectedRecordingFormat = v);
    }

    private void EnsureQualitySelection() =>
        EnsureStringComboBoxSelection(QualityComboBox, ViewModel.AvailableQualities,
            () => ViewModel.SelectedQuality, v => ViewModel.SelectedQuality = v);

    private void EnsurePresetSelection() =>
        EnsureStringComboBoxSelection(PresetComboBox, ViewModel.AvailablePresets,
            () => ViewModel.SelectedPreset, v => ViewModel.SelectedPreset = v);

    private void EnsureSplitEncodeModeSelection() =>
        EnsureStringComboBoxSelection(SplitEncodeComboBox, ViewModel.AvailableSplitEncodeModes,
            () => ViewModel.SelectedSplitEncodeMode, v => ViewModel.SelectedSplitEncodeMode = v);

    private static void AttachCollectionSync(
        System.Collections.Specialized.INotifyCollectionChanged collection,
        Action queueSync)
    {
        collection.CollectionChanged += (s, e) =>
        {
            if (e.Action is System.Collections.Specialized.NotifyCollectionChangedAction.Add
                or System.Collections.Specialized.NotifyCollectionChangedAction.Reset
                or System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                queueSync();
            }
        };
    }

    private void QueueSelectionSync(int syncIndex, Action ensureMethod)
    {
        if (Interlocked.Exchange(ref _selectionSyncQueued[syncIndex], 1) != 0)
        {
            return;
        }

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                ensureMethod();
            }
            finally
            {
                Interlocked.Exchange(ref _selectionSyncQueued[syncIndex], 0);
            }
        });
    }

    private void QueueDeviceSelectionSync() => QueueSelectionSync(SyncDevice, EnsureDeviceSelection);
    private void QueueAudioSelectionSync() => QueueSelectionSync(SyncAudio, EnsureAudioInputSelection);
    private void QueueMicrophoneSelectionSync() => QueueSelectionSync(SyncMicrophone, EnsureMicrophoneSelection);
    private void QueueResolutionSelectionSync() => QueueSelectionSync(SyncResolution, EnsureResolutionSelection);
    private void QueueFrameRateSelectionSync() => QueueSelectionSync(SyncFrameRate, EnsureFrameRateSelection);
    private void QueueFormatSelectionSync() => QueueSelectionSync(SyncFormat, EnsureFormatSelection);
    private void QueueQualitySelectionSync() => QueueSelectionSync(SyncQuality, EnsureQualitySelection);
    private void QueuePresetSelectionSync() => QueueSelectionSync(SyncPreset, EnsurePresetSelection);
    private void QueueSplitEncodeModeSelectionSync() => QueueSelectionSync(SyncSplitEncode, EnsureSplitEncodeModeSelection);

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
        ViewModel.StatsSectionVisibilityHandler = SetStatsSectionVisible;
        _windowTitleBase = BuildWindowTitleBase();
        ApplyWindowTitle();
        var automationToken = Environment.GetEnvironmentVariable("ELGATOCAPTURE_AUTOMATION_TOKEN");
        var automationPipeName = Environment.GetEnvironmentVariable("ELGATOCAPTURE_AUTOMATION_PIPE");
        if (string.IsNullOrWhiteSpace(automationPipeName))
        {
            automationPipeName = "ElgatoCaptureAutomation";
        }

        _nvmlMonitor = new NvmlMonitor();

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

        // ESC key exits fullscreen
        ((FrameworkElement)Content).KeyDown += OnContentKeyDown;

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
        ViewModel.AudioMeterActivated += EnsureAudioMeterTimerRunning;
        ViewModel.MicrophoneMeterActivated += EnsureAudioMeterTimerRunning;

        // Bind all collections to ComboBoxes
        DeviceComboBox.ItemsSource = ViewModel.Devices;
        AudioInputComboBox.ItemsSource = ViewModel.AudioInputDevices;
        MicrophoneComboBox.ItemsSource = ViewModel.MicrophoneDevices;
        ResolutionComboBox.ItemsSource = ViewModel.AvailableResolutions;
        FrameRateComboBox.ItemsSource = ViewModel.AvailableFrameRates;
        FormatComboBox.ItemsSource = ViewModel.AvailableRecordingFormats;
        QualityComboBox.ItemsSource = ViewModel.AvailableQualities;
        PresetComboBox.ItemsSource = ViewModel.AvailablePresets;
        SplitEncodeComboBox.ItemsSource = ViewModel.AvailableSplitEncodeModes;
        VideoFormatComboBox.ItemsSource = ViewModel.AvailableVideoFormats;
        DecoderCountComboBox.Items.Clear();
        for (var i = 1; i <= 8; i++)
        {
            DecoderCountComboBox.Items.Add(i);
        }

        AttachCollectionSync(ViewModel.Devices, QueueDeviceSelectionSync);
        AttachCollectionSync(ViewModel.AudioInputDevices, QueueAudioSelectionSync);
        AttachCollectionSync(ViewModel.MicrophoneDevices, QueueMicrophoneSelectionSync);
        AttachCollectionSync(ViewModel.AvailableResolutions, QueueResolutionSelectionSync);
        AttachCollectionSync(ViewModel.AvailableFrameRates, QueueFrameRateSelectionSync);
        AttachCollectionSync(ViewModel.AvailableRecordingFormats, QueueFormatSelectionSync);
        AttachCollectionSync(ViewModel.AvailableQualities, QueueQualitySelectionSync);
        AttachCollectionSync(ViewModel.AvailablePresets, QueuePresetSelectionSync);
        AttachCollectionSync(ViewModel.AvailableSplitEncodeModes, QueueSplitEncodeModeSelectionSync);

        // Set initial values
        UpdateOutputPathDisplay();
        DiskSpaceTextBlock.Text = ViewModel.DiskSpaceInfo;
        RecordingSizeTextBlock.Text = ViewModel.RecordingSizeInfo;
        RecordingBitrateTextBlock.Text = ViewModel.RecordingBitrateInfo;
        LiveResolutionTextBlock.Text = ViewModel.LiveResolution;
        LiveFrameRateTextBlock.Text = ViewModel.LiveFrameRate;
        LivePixelFormatTextBlock.Text = ViewModel.LivePixelFormat;
        AudioRecordToggle.IsChecked = ViewModel.IsAudioEnabled;
        AudioPreviewToggle.IsChecked = ViewModel.IsAudioPreviewEnabled;
        AudioPreviewToggle.IsEnabled = ViewModel.IsAudioEnabled;
        SetAudioMeterMonitoringState(ViewModel.IsAudioPreviewActive);
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
        SyncMicrophoneVolumeControls(ViewModel.MicrophoneVolume);
        MicVolumeSlider.ValueChanged += (s, e) =>
        {
            if (_syncingMicrophoneVolumeControls)
            {
                return;
            }

            _syncingMicrophoneVolumeControls = true;
            try
            {
                if (Math.Abs(ViewModel.MicrophoneVolume - e.NewValue) > 0.01)
                {
                    ViewModel.MicrophoneVolume = e.NewValue;
                }

                SyncMicrophoneVolumeControls(e.NewValue);
            }
            finally
            {
                _syncingMicrophoneVolumeControls = false;
            }
        };
        MicVolumeSlider.PointerCaptureLost += (s, e) => ViewModel.SaveMicrophoneVolume();
        MicVolumeShelfSlider.ValueChanged += (s, e) =>
        {
            if (_syncingMicrophoneVolumeControls)
            {
                return;
            }

            _syncingMicrophoneVolumeControls = true;
            try
            {
                if (Math.Abs(ViewModel.MicrophoneVolume - e.NewValue) > 0.01)
                {
                    ViewModel.MicrophoneVolume = e.NewValue;
                }

                SyncMicrophoneVolumeControls(e.NewValue);
            }
            finally
            {
                _syncingMicrophoneVolumeControls = false;
            }
        };
        MicVolumeShelfSlider.PointerCaptureLost += (s, e) => ViewModel.SaveMicrophoneVolume();
        CustomAudioToggle.IsChecked = ViewModel.IsCustomAudioInputEnabled;
        CustomAudioToggle.IsEnabled = !ViewModel.IsRecording;
        MicrophoneToggle.IsChecked = ViewModel.IsMicrophoneEnabled;
        MicrophoneToggle.IsEnabled = !ViewModel.IsRecording;
        ShowAllCaptureOptionsToggle.IsChecked = ViewModel.ShowAllCaptureOptions;
        StatsToggle.IsChecked = ViewModel.IsStatsVisible;
        AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled && !ViewModel.IsRecording;
        AudioInputComboBox.SelectedItem = ViewModel.SelectedAudioInputDevice;
        MicrophoneComboBox.IsEnabled = ViewModel.IsMicrophoneEnabled && !ViewModel.IsRecording;
        MicrophoneComboBox.SelectedItem = ViewModel.SelectedMicrophoneDevice;
        MicVolumeShelfSlider.IsEnabled = ViewModel.IsMicrophoneEnabled;
        if (ViewModel.IsMicrophoneEnabled)
        {
            DeviceAudioRowTranslate.Y = 0;
            MicMeterRowTranslate.Y = 0;
            MicMeterRow.Opacity = 1;
        }
        else
        {
            DeviceAudioRowTranslate.Y = MicMeterRowHeight / 2;
            HideMicMeterRow(immediate: true);
        }
        ApplyDeviceAudioControlState();
        FormatComboBox.SelectedItem = ViewModel.SelectedRecordingFormat;
        QualityComboBox.SelectedItem = ViewModel.SelectedQuality;
        PresetComboBox.SelectedItem = ViewModel.SelectedPreset;
        SplitEncodeComboBox.SelectedItem = ViewModel.SelectedSplitEncodeMode;
        VideoFormatComboBox.SelectedItem = ViewModel.SelectedVideoFormat;
        _selectedDecoderCount = Math.Clamp(ViewModel.MjpegDecoderCount, 1, 8);
        DecoderCountComboBox.SelectedItem = _selectedDecoderCount;
        CustomBitrateNumberBox.Value = ViewModel.CustomBitrateMbps;
        CustomBitratePanel.Visibility = ViewModel.IsCustomBitrateVisible ? Visibility.Visible : Visibility.Collapsed;
        HdrToggle.IsChecked = ViewModel.IsHdrEnabled;
        HdrToggle.IsEnabled = ViewModel.IsHdrAvailable &&
                              !ViewModel.IsRecording &&
                              ViewModel.SourceIsHdr != false;
        TrueHdrPreviewToggle.IsChecked = ViewModel.IsTrueHdrPreviewEnabled;
        TrueHdrPreviewToggle.IsEnabled = ViewModel.IsHdrEnabled && !ViewModel.IsRecording;
        ResetAudioMeterVisuals();
        _audioMeterTargetLevel = Math.Clamp(ViewModel.AudioMeterTarget, 0.0, 1.0);
        AudioClipText.Visibility = ViewModel.AudioClipping ? Visibility.Visible : Visibility.Collapsed;
        RecordButton.IsEnabled = !ViewModel.IsFfmpegMissing;
        RefreshHdrHintText();
        UpdateFpsTelemetryTooltip();
        EnsureDeviceSelection();
        EnsureAudioInputSelection();
        EnsureMicrophoneSelection();
        EnsureDeviceAudioModeSelection();
        EnsureResolutionSelection();
        EnsureFrameRateSelection();
        EnsureFormatSelection();
        EnsureQualitySelection();
        EnsurePresetSelection();
        EnsureSplitEncodeModeSelection();
        UpdateDecoderCountVisibility();

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

        MicrophoneComboBox.SelectionChanged += (s, e) =>
        {
            if (MicrophoneComboBox.SelectedItem is ElgatoCapture.Models.AudioInputDevice device &&
                device != ViewModel.SelectedMicrophoneDevice)
            {
                ViewModel.SelectedMicrophoneDevice = device;
            }
        };

        DeviceAudioModeToggle.Toggled += (s, e) =>
        {
            var mode = DeviceAudioModeToggle.IsOn ? DeviceAudioMode.Analog : DeviceAudioMode.Hdmi;
            if (!string.Equals(mode, ViewModel.SelectedDeviceAudioMode, StringComparison.OrdinalIgnoreCase))
            {
                ViewModel.SelectedDeviceAudioMode = mode;
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

            UpdateDecoderCountVisibility();
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

        VideoFormatComboBox.SelectionChanged += (s, e) =>
        {
            if (VideoFormatComboBox.SelectedItem is string videoFormat)
            {
                ViewModel.SelectedVideoFormat = videoFormat;
            }

            UpdateDecoderCountVisibility();
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
        CustomAudioToggle.Click += (s, e) => ViewModel.IsCustomAudioInputEnabled = CustomAudioToggle.IsChecked == true;
        MicrophoneToggle.Click += (s, e) => ViewModel.IsMicrophoneEnabled = MicrophoneToggle.IsChecked == true;
        ShowAllCaptureOptionsToggle.Click += (s, e) => ViewModel.ShowAllCaptureOptions = ShowAllCaptureOptionsToggle.IsChecked == true;
        AnalogAudioGainSlider.ValueChanged += (s, e) =>
        {
            ViewModel.AnalogAudioGainPercent = e.NewValue;
            AnalogAudioGainValueTextBlock.Text = $"{(int)Math.Round(e.NewValue)}%";
        };
        AudioMeterTrack.SizeChanged += (s, e) => AnimateAudioMeterTick();
        MicMeterTrack.SizeChanged += (s, e) => AnimateAudioMeterTick();
        ControlBarBorder.SizeChanged += (s, e) => UpdateToggleLabelVisibility(e.NewSize.Width);
        CaptureSettingsGrid.SizeChanged += CaptureSettingsGrid_SizeChanged;
        OutputPathTextBox.SizeChanged += (s, e) => UpdateOutputPathDisplay();
        ApplyStatsVisibility(ViewModel.IsStatsVisible, immediate: true);
    }

    private void SyncMicrophoneVolumeControls(double volumePercent)
    {
        var clampedVolume = Math.Clamp(volumePercent, 0.0, 100.0);
        if (Math.Abs(MicVolumeSlider.Value - clampedVolume) > 0.5)
        {
            MicVolumeSlider.Value = clampedVolume;
        }

        if (Math.Abs(MicVolumeShelfSlider.Value - clampedVolume) > 0.5)
        {
            MicVolumeShelfSlider.Value = clampedVolume;
        }

        MicVolumeLabel.Text = $"{(int)Math.Round(clampedVolume)}%";
    }

    private void UpdateMicrophoneControlsVisibility()
    {
        MicVolumeShelfSlider.IsEnabled = ViewModel.IsMicrophoneEnabled;
        if (ViewModel.IsMicrophoneEnabled)
        {
            ShowMicMeterRow();
        }
        else
        {
            HideMicMeterRow(immediate: false);
        }
    }

    private void ShowMicMeterRow()
    {
        EnsureMicMeterRowAnimations();
        StopMicMeterRowAnimation();
        DeviceAudioRowTranslate.Y = MicMeterRowHeight / 2;
        MicMeterRowTranslate.Y = MicMeterRowHeight;
        MicMeterRow.Opacity = 0;
        _micMeterRowStoryboard = _showMicMeterRowStoryboard;
        _showMicMeterRowStoryboard?.Begin();
    }

    private void HideMicMeterRow(bool immediate)
    {
        EnsureMicMeterRowAnimations();
        StopMicMeterRowAnimation();
        if (immediate || MicMeterRow.Opacity == 0)
        {
            DeviceAudioRowTranslate.Y = MicMeterRowHeight / 2;
            MicMeterRowTranslate.Y = MicMeterRowHeight;
            MicMeterRow.Opacity = 0;
            _micMeterDisplayLevel = 0;
            _micMeterTargetLevel = 0;
            MicMeterClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
            return;
        }

        _micMeterRowStoryboard = _hideMicMeterRowStoryboard;
        _hideMicMeterRowStoryboard?.Begin();
    }

    private void StopMicMeterRowAnimation()
    {
        _micMeterRowStoryboard?.Stop();
        _micMeterRowStoryboard = null;
    }

    private void EnsureMicMeterRowAnimations()
    {
        _showMicMeterRowStoryboard ??= CreateMicMeterRowStoryboard(showing: true);
        _hideMicMeterRowStoryboard ??= CreateMicMeterRowStoryboard(showing: false);
    }

    private Storyboard CreateMicMeterRowStoryboard(bool showing)
    {
        var durationMs = showing ? 200 : 150;
        var easing = new CubicEase { EasingMode = showing ? EasingMode.EaseOut : EasingMode.EaseIn };
        var duration = TimeSpan.FromMilliseconds(durationMs);

        var storyboard = new Storyboard();

        // Device audio row: TranslateY 7→0 (show) or 0→7 (hide)
        var deviceSlide = new DoubleAnimation
        {
            To = showing ? 0 : MicMeterRowHeight / 2,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(deviceSlide, DeviceAudioRowTranslate);
        Storyboard.SetTargetProperty(deviceSlide, "Y");

        // Mic meter: TranslateY +14→0 (slides up into view) or 0→+14 (slides down out)
        var slideAnim = new DoubleAnimation
        {
            To = showing ? 0 : MicMeterRowHeight,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(slideAnim, MicMeterRowTranslate);
        Storyboard.SetTargetProperty(slideAnim, "Y");

        var fade = new DoubleAnimation
        {
            To = showing ? 1 : 0,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(fade, MicMeterRow);
        Storyboard.SetTargetProperty(fade, "Opacity");

        storyboard.Children.Add(deviceSlide);
        storyboard.Children.Add(slideAnim);
        storyboard.Children.Add(fade);
        storyboard.Completed += (_, _) =>
        {
            if (!ReferenceEquals(_micMeterRowStoryboard, storyboard))
            {
                return;
            }

            _micMeterRowStoryboard = null;
            if (showing)
            {
                DeviceAudioRowTranslate.Y = 0;
                MicMeterRowTranslate.Y = 0;
                MicMeterRow.Opacity = 1;
                return;
            }

            DeviceAudioRowTranslate.Y = MicMeterRowHeight / 2;
            MicMeterRowTranslate.Y = MicMeterRowHeight;
            MicMeterRow.Opacity = 0;
            _micMeterDisplayLevel = 0;
            _micMeterTargetLevel = 0;
            MicMeterClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
        };

        return storyboard;
    }

    private FrameworkElement[] GetControlBarButtons() => new FrameworkElement[]
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

    private void SetupButtonHoverAnimations()
    {
        foreach (var button in GetControlBarButtons())
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

    private FrameworkElement[] GetEntranceButtons() => GetControlBarButtons();

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

        ViewModel.IsStatsVisible = true;
    }

    private void StatsToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        ViewModel.IsStatsVisible = false;
    }

    private void ApplyStatsVisibility(bool visible, bool immediate = false)
    {
        if (visible)
        {
            ShowStatsDockPanel();
            UpdateStatsDock();
            StartStatsDockPolling();
            return;
        }

        StopStatsDockPolling();
        HideStatsDockPanel(immediate);
    }

    private void StatsSectionHeader_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not Grid header || header.Tag is not string contentName)
        {
            return;
        }

        var content = StatsDockPanel.FindName(contentName) as StackPanel;
        if (content == null)
        {
            return;
        }

        var collapsing = content.Visibility == Visibility.Visible;
        content.Visibility = collapsing ? Visibility.Collapsed : Visibility.Visible;

        var chevronName = contentName.Replace("_Content", "_Chevron", StringComparison.Ordinal);
        if (StatsDockPanel.FindName(chevronName) is FontIcon chevron &&
            chevron.RenderTransform is RotateTransform rotate)
        {
            rotate.Angle = collapsing ? -90 : 0;
        }

        if (!collapsing && ReferenceEquals(content, Diagnostics_Content))
        {
            var snapshot = GetStatsSnapshot();
            UpdateDiagnosticsSection(snapshot.SourceTelemetryDetails ?? Array.Empty<SourceTelemetryDetailEntry>(), snapshot.DiagnosticSummary);
        }
    }

    private void SetStatsSectionVisible(string section, bool visible)
    {
        var contentName = section + "_Content";
        var content = StatsDockPanel.FindName(contentName) as StackPanel;
        if (content == null)
        {
            return;
        }

        content.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        var chevronName = section + "_Chevron";
        if (StatsDockPanel.FindName(chevronName) is FontIcon chevron &&
            chevron.RenderTransform is RotateTransform rotate)
        {
            rotate.Angle = visible ? 0 : -90;
        }

        if (visible && contentName == "Diagnostics_Content")
        {
            var snapshot = GetStatsSnapshot();
            UpdateDiagnosticsSection(snapshot.SourceTelemetryDetails ?? Array.Empty<SourceTelemetryDetailEntry>(), snapshot.DiagnosticSummary);
        }
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
            VideoFormatColumn.Width = new GridLength(0);
            PresetColumn.Width = new GridLength(0);
            SplitColumn.Width = new GridLength(0);
            Grid.SetRow(VideoFormatPanel, 1);
            Grid.SetColumn(VideoFormatPanel, 0);
            Grid.SetRow(PresetPanel, 1);
            Grid.SetColumn(PresetPanel, 2);
            Grid.SetRow(SplitPanel, 1);
            Grid.SetColumn(SplitPanel, 3);
            Grid.SetRow(CustomBitratePanel, 1);
            Grid.SetColumn(CustomBitratePanel, 2);
        }
        else
        {
            VideoFormatColumn.Width = new GridLength(1, GridUnitType.Star);
            PresetColumn.Width = new GridLength(1, GridUnitType.Star);
            SplitColumn.Width = new GridLength(1, GridUnitType.Star);
            Grid.SetRow(VideoFormatPanel, 0);
            Grid.SetColumn(VideoFormatPanel, 0);
            Grid.SetRow(PresetPanel, 0);
            Grid.SetColumn(PresetPanel, 5);
            Grid.SetRow(SplitPanel, 0);
            Grid.SetColumn(SplitPanel, 6);
            Grid.SetRow(CustomBitratePanel, 0);
            Grid.SetColumn(CustomBitratePanel, 5);
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
        ViewModel.AudioMeterActivated -= EnsureAudioMeterTimerRunning;
        ViewModel.MicrophoneMeterActivated -= EnsureAudioMeterTimerRunning;
        _audioMeterAnimationTimer?.Stop();
        _audioMeterAnimationTimer = null;
        StopStatsDockPolling();
        HideStatsDockPanel(immediate: true);
        StopMicMeterRowAnimation();
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

        _nvmlMonitor?.Dispose();

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
        var sourceHdr = FormatSourceHdr(snapshot.SourceIsHdr, snapshot.SourceColorimetry);
        var sourceFormat = snapshot.SourceVideoFormat ?? "\u2014";
        var telemetryOrigin = snapshot.TelemetryOrigin is not null and not "Unknown"
            ? $"{snapshot.TelemetryOrigin} ({snapshot.TelemetryConfidence ?? "?"})"
            : "\u2014";

        var adcOnOff = "\u2014";
        var adcGain = "\u2014";
        if (snapshot.SourceTelemetryDetails is { } details)
        {
            foreach (var d in details)
            {
                if (d.Label == TelemetryLabels.AdcAnalog) adcOnOff = d.DisplayValue;
                else if (d.Label == TelemetryLabels.AnalogGain) adcGain = d.DisplayValue;
            }
        }

        SetTextIfChanged(Stats_SessionStateValue, sessionState);
        SetTextIfChanged(Stats_SourceResolutionValue, sourceResolution);
        SetTextIfChanged(Stats_SourceFrameRateValue, sourceFrameRate);
        SetTextIfChanged(Stats_SourceHdrValue, sourceHdr);
        SetTextIfChanged(Stats_SourceFormatValue, sourceFormat);
        SetTextIfChanged(Stats_TelemetryOriginValue, telemetryOrigin);
        SetTextIfChanged(Stats_AdcOnOffValue, adcOnOff);
        SetTextIfChanged(Stats_AdcGainValue, adcGain);
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
        SetTextIfChanged(Stats_AvSyncDriftValue, FormatSignedMs(snapshot.AvSyncCaptureDriftMs));
        SetTextIfChanged(Stats_AvSyncDriftRateValue, FormatSignedMsPerSec(snapshot.AvSyncCaptureDriftRateMsPerSec));
        var encoderVisible = snapshot.Recording && snapshot.AvSyncEncoderDriftMs.HasValue;
        SetVisibilityIfChanged(Stats_AvSyncEncoderRow, encoderVisible ? Visibility.Visible : Visibility.Collapsed);
        if (encoderVisible)
        {
            var encoderText = $"{FormatSignedMs(snapshot.AvSyncEncoderDriftMs)} ({snapshot.AvSyncEncoderCorrectionSamples ?? 0} corr)";
            SetTextIfChanged(Stats_AvSyncEncoderValue, encoderText);
        }
        UpdateDiagnosticsSection(snapshot.SourceTelemetryDetails ?? Array.Empty<SourceTelemetryDetailEntry>(), snapshot.DiagnosticSummary);
        UpdateDecodeSection();
        UpdateGpuSection();
    }

    private void UpdateDecodeSection()
    {
        var mjpegMetrics = ViewModel.GetMjpegPipelineTimingDetails();
        if (mjpegMetrics is not { DecoderCount: > 0 } mjpeg)
        {
            DecodeSection.Visibility = Visibility.Collapsed;
            CollapseDiagnosticRows(_decodeRowPool);
            return;
        }

        DecodeSection.Visibility = Visibility.Visible;
        EnsureDiagnosticRowPool(Decode_Content, _decodeRowPool, MaxExpectedDecodeRowCount);

        var rowIndex = 0;
        void SetRow(string label, string value)
        {
            EnsureDiagnosticRowPool(Decode_Content, _decodeRowPool, rowIndex + 1);
            UpdateDiagnosticRowSlot(_decodeRowPool[rowIndex], label, value, alt: (rowIndex % 2) != 0);
            rowIndex++;
        }

        var effectiveFrameTimeMs = mjpeg.DecodeAvgMs / mjpeg.DecoderCount;
        var effectiveFps = effectiveFrameTimeMs > 0 ? 1000.0 / effectiveFrameTimeMs : 0;

        SetRow("Throughput", $"{effectiveFrameTimeMs:0.00}ms ({effectiveFps:0}fps peak)");
        SetRow("Decode", $"{mjpeg.DecodeAvgMs:0.00}ms avg ({mjpeg.DecoderCount} threads)");
        SetRow("Reorder", $"{mjpeg.ReorderAvgMs:0.00}ms avg");
        SetRow("Pipeline", $"{mjpeg.PipelineAvgMs:0.00}ms avg");
        SetRow("Frames", $"{mjpeg.TotalEmitted:N0} emitted / {mjpeg.TotalDropped:N0} dropped");
        if (mjpeg.ReorderSkips > 0)
        {
            SetRow("Skips", $"{mjpeg.ReorderSkips:N0}");
        }

        foreach (var worker in mjpeg.PerDecoder)
        {
            SetRow($"Thread {worker.WorkerIndex}", $"{worker.AvgMs:0.00}ms");
        }

        CollapseDiagnosticRows(_decodeRowPool, startIndex: rowIndex);
    }

    private void UpdateGpuSection()
    {
        var nvml = _nvmlMonitor?.GetLatestSnapshot();
        EnsureDiagnosticRowPool(GPU_Content, _gpuRowPool, FixedGpuRowCount);

        var rowIndex = 0;
        void SetRow(string label, string value)
        {
            UpdateDiagnosticRowSlot(_gpuRowPool[rowIndex], label, value, alt: (rowIndex % 2) != 0);
            rowIndex++;
        }

        if (nvml == null)
        {
            SetRow("Status", "NVML not available");
            CollapseDiagnosticRows(_gpuRowPool, startIndex: rowIndex);
            return;
        }

        SetRow("GPU", string.IsNullOrWhiteSpace(nvml.GpuName) ? "\u2014" : nvml.GpuName);
        SetRow("Utilization", $"{nvml.GpuUtilizationPercent ?? 0}% (Mem: {nvml.GpuMemoryUtilizationPercent ?? 0}%)");
        SetRow("NVDEC", $"{nvml.NvdecUtilizationPercent ?? 0}%");
        SetRow("NVENC", $"{nvml.NvencUtilizationPercent ?? 0}%");
        SetRow("PCIe TX", $"{nvml.PcieTxMBps ?? 0:0.0} MB/s");
        SetRow("PCIe RX", $"{nvml.PcieRxMBps ?? 0:0.0} MB/s");
        SetRow("VRAM", $"{nvml.VramUsedMB ?? 0} / {nvml.VramTotalMB ?? 0} MB");
        SetRow("Temperature", $"{nvml.GpuTemperatureC ?? 0}°C");
        SetRow("Power", $"{nvml.GpuPowerW ?? 0:0.0}W");
        SetRow("Clocks", $"{nvml.GpuClockMHz ?? 0} MHz (Mem: {nvml.GpuMemClockMHz ?? 0} MHz)");
        CollapseDiagnosticRows(_gpuRowPool, startIndex: rowIndex);
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
        var telemetryDetails = new List<SourceTelemetryDetailEntry>(health.SourceTelemetryDetails);
        var captureCardFormat = health.ReaderSourceSubtype ?? health.NegotiatedPixelFormat;
        if (!string.IsNullOrWhiteSpace(captureCardFormat))
        {
            telemetryDetails.Add(new SourceTelemetryDetailEntry("Capture Card / UVC", "Capture Format", captureCardFormat));
        }

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
            SourceVideoFormat: health.SourceVideoFormat,
            SourceColorimetry: health.SourceColorimetry,
            ReaderSourceSubtype: health.ReaderSourceSubtype,
            NegotiatedPixelFormat: health.NegotiatedPixelFormat,
            TelemetryOrigin: health.SourceTelemetryOrigin.ToString(),
            TelemetryConfidence: health.SourceTelemetryConfidence.ToString(),
            SourceTelemetryDetails: telemetryDetails,
            DiagnosticSummary: health.SourceTelemetryDiagnosticSummary,
            AvSyncCaptureDriftMs: health.AvSyncCaptureDriftMs,
            AvSyncCaptureDriftRateMsPerSec: health.AvSyncCaptureDriftRateMsPerSec,
            AvSyncEncoderDriftMs: health.AvSyncEncoderDriftMs,
            AvSyncEncoderCorrectionSamples: health.AvSyncEncoderCorrectionSamples);
    }

    private void UpdateDiagnosticsSection(IReadOnlyList<SourceTelemetryDetailEntry> telemetryDetails, string? diagnosticSummary)
    {
        if (Diagnostics_Content.Visibility != Visibility.Visible)
        {
            return;
        }

        EnsureDiagnosticsEmptyState();

        var slotIndex = 0;
        if (telemetryDetails.Count > 0)
        {
            var currentGroup = string.Empty;
            var alt = true;
            foreach (var detail in telemetryDetails)
            {
                EnsureDiagnosticsPoolCapacity(slotIndex + 1);
                var showHeader = !string.Equals(currentGroup, detail.Group, StringComparison.Ordinal);
                if (showHeader)
                {
                    currentGroup = detail.Group;
                    alt = true;
                }

                UpdateDiagnosticsPoolSlot(
                    _diagnosticsRowPool[slotIndex],
                    showHeader ? currentGroup : null,
                    detail.Label,
                    detail.DisplayValue,
                    alt);
                alt = !alt;
                slotIndex++;
            }

            SetVisibilityIfChanged(_diagnosticsEmptyStateTextBlock!, Visibility.Collapsed);
            CollapseDiagnosticsPoolSlots(startIndex: slotIndex);
            return;
        }

        if (string.IsNullOrWhiteSpace(diagnosticSummary))
        {
            SetVisibilityIfChanged(_diagnosticsEmptyStateTextBlock!, Visibility.Visible);
            CollapseDiagnosticsPoolSlots();
            return;
        }

        var entries = ParseDiagnosticSummary(diagnosticSummary);
        var fallbackAlt = true;
        foreach (var (label, value) in entries)
        {
            EnsureDiagnosticsPoolCapacity(slotIndex + 1);
            UpdateDiagnosticsPoolSlot(_diagnosticsRowPool[slotIndex], null, label, value, fallbackAlt);
            fallbackAlt = !fallbackAlt;
            slotIndex++;
        }

        SetVisibilityIfChanged(_diagnosticsEmptyStateTextBlock!, Visibility.Collapsed);
        CollapseDiagnosticsPoolSlots(startIndex: slotIndex);
    }

    private TextBlock CreateDiagnosticGroupHeader(string title)
    {
        return new TextBlock
        {
            Text = title,
            Margin = new Thickness(0, 8, 0, 2),
            Style = (Style)StatsDockPanel.Resources["DockStatsSectionHeaderStyle"]
        };
    }

    private static List<(string Label, string Value)> ParseDiagnosticSummary(string summary)
    {
        if (!summary.StartsWith("nativexu:", StringComparison.OrdinalIgnoreCase))
        {
            return new List<(string Label, string Value)>
            {
                ("Summary", summary.Trim())
            };
        }

        var result = new List<(string Label, string Value)>();
        var parts = summary.Split(':');

        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }

            var eqIndex = part.IndexOf('=');
            if (eqIndex > 0)
            {
                var key = part[..eqIndex].Trim();
                var val = part[(eqIndex + 1)..].Trim();
                var label = key switch
                {
                    "vic" => "VIC Code",
                    "vfreq" => "Vert Freq",
                    "quant" => "Quantization",
                    "hdr2sdr" => "HDR to SDR",
                    "eotf" => "EOTF",
                    "fw" => "Firmware",
                    "audiofmt" => "Audio Format",
                    "audiosrate" => "Audio Sample Rate",
                    "inputsrc" => "Input Source",
                    "usbproto" => "USB Protocol",
                    "usbcdc" => "USB CDC",
                    "usblinkst" => "USB Link State",
                    "usbspeed" => "USB Speed",
                    "txhpd" => "TX Hot Plug",
                    "txvrr" => "TX VRR",
                    "uvctiming" => "UVC Timing",
                    "uvcfmt" => "UVC Format",
                    "uvcerr" => "UVC Error",
                    "hdcpmode" => "HDCP Mode",
                    "hdcpver" => "HDCP Version",
                    "rxtxhdcp" => "RX/TX HDCP",
                    "hdr2sdrext" => "HDR2SDR Status",
                    "hdr2sdrcolor" => "HDR2SDR Color",
                    "colorrangesetting" => "Color Range",
                    "vtem" => "VTEM (VRR)",
                    "biterr" => "Bit Errors",
                    "rawtiming" => "Raw Timing",
                    _ => key
                };
                result.Add((label, val));
                continue;
            }

            var entry = part switch
            {
                "nativexu" => ("Origin", "NativeXu"),
                "hdr" => ("HDR", "Yes"),
                "sdr" => ("HDR", "No"),
                "unknown" => ("HDR", "Unknown"),
                _ when part.Contains('x') && part.Length > 3 && char.IsDigit(part[0]) => ("Resolution", part),
                _ when double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var fps) && fps > 0 =>
                    ("Frame Rate", $"{fps:0.##} Hz"),
                _ => ("Info", part)
            };
            result.Add(entry);
        }

        return result;
    }

    private Border CreateDiagnosticRow(string label, string value, bool alt)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var labelBlock = new TextBlock
        {
            Text = label,
            Style = (Style)StatsDockPanel.Resources["DockStatsLabelStyle"]
        };

        var valueBlock = new TextBlock
        {
            Text = value,
            Style = (Style)StatsDockPanel.Resources["DockStatsValueStyle"],
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(valueBlock, 1);

        grid.Children.Add(labelBlock);
        grid.Children.Add(valueBlock);

        return new Border
        {
            Style = (Style)StatsDockPanel.Resources[alt ? "DockStatsRowAltStyle" : "DockStatsRowStyle"],
            Child = grid
        };
    }

    private sealed record DiagnosticRowSlot(Border Row, TextBlock Label, TextBlock Value);

    private sealed record DiagnosticsPoolSlot(
        Border Row,
        TextBlock? GroupHeader,
        TextBlock Label,
        TextBlock Value);

    private static void SetVisibilityIfChanged(UIElement element, Visibility visibility)
    {
        if (element.Visibility != visibility)
        {
            element.Visibility = visibility;
        }
    }

    private void EnsureDiagnosticRowPool(StackPanel container, List<DiagnosticRowSlot> pool, int requiredCount)
    {
        while (pool.Count < requiredCount)
        {
            var row = CreateDiagnosticRow("", "", alt: false);
            var grid = (Grid)row.Child;
            var labelBlock = (TextBlock)grid.Children[0];
            var valueBlock = (TextBlock)grid.Children[1];
            pool.Add(new DiagnosticRowSlot(row, labelBlock, valueBlock));
            container.Children.Add(row);
        }
    }

    private void UpdateDiagnosticRowSlot(DiagnosticRowSlot slot, string label, string value, bool alt)
    {
        SetTextIfChanged(slot.Label, label);
        SetTextIfChanged(slot.Value, value);
        var targetStyle = (Style)StatsDockPanel.Resources[alt ? "DockStatsRowAltStyle" : "DockStatsRowStyle"];
        if (!ReferenceEquals(slot.Row.Style, targetStyle))
        {
            slot.Row.Style = targetStyle;
        }

        SetVisibilityIfChanged(slot.Row, Visibility.Visible);
    }

    private static void CollapseDiagnosticRows(List<DiagnosticRowSlot> pool, int startIndex = 0)
    {
        for (var i = startIndex; i < pool.Count; i++)
        {
            SetVisibilityIfChanged(pool[i].Row, Visibility.Collapsed);
        }
    }

    private void EnsureDiagnosticsEmptyState()
    {
        if (_diagnosticsEmptyStateTextBlock != null) return;
        _diagnosticsEmptyStateTextBlock = new TextBlock
        {
            Text = "No diagnostics available",
            Style = (Style)StatsDockPanel.Resources["DockStatsLabelStyle"],
            Visibility = Visibility.Collapsed
        };
        Diagnostics_Content.Children.Add(_diagnosticsEmptyStateTextBlock);
    }

    private void EnsureDiagnosticsPoolCapacity(int requiredCount)
    {
        while (_diagnosticsRowPool.Count < requiredCount)
        {
            var row = CreateDiagnosticRow("", "", alt: false);
            var grid = (Grid)row.Child;
            var labelBlock = (TextBlock)grid.Children[0];
            var valueBlock = (TextBlock)grid.Children[1];
            var header = CreateDiagnosticGroupHeader("");
            header.Visibility = Visibility.Collapsed;
            Diagnostics_Content.Children.Add(header);
            Diagnostics_Content.Children.Add(row);
            _diagnosticsRowPool.Add(new DiagnosticsPoolSlot(row, header, labelBlock, valueBlock));
        }
    }

    private void UpdateDiagnosticsPoolSlot(
        DiagnosticsPoolSlot slot,
        string? groupHeader,
        string label,
        string value,
        bool alt)
    {
        if (slot.GroupHeader != null)
        {
            if (groupHeader != null)
            {
                SetTextIfChanged(slot.GroupHeader, groupHeader);
                SetVisibilityIfChanged(slot.GroupHeader, Visibility.Visible);
            }
            else
            {
                SetVisibilityIfChanged(slot.GroupHeader, Visibility.Collapsed);
            }
        }

        SetTextIfChanged(slot.Label, label);
        SetTextIfChanged(slot.Value, value);
        var targetStyle = (Style)StatsDockPanel.Resources[alt ? "DockStatsRowAltStyle" : "DockStatsRowStyle"];
        if (!ReferenceEquals(slot.Row.Style, targetStyle))
        {
            slot.Row.Style = targetStyle;
        }

        SetVisibilityIfChanged(slot.Row, Visibility.Visible);
    }

    private void CollapseDiagnosticsPoolSlots(int startIndex = 0)
    {
        for (var i = startIndex; i < _diagnosticsRowPool.Count; i++)
        {
            var slot = _diagnosticsRowPool[i];
            SetVisibilityIfChanged(slot.Row, Visibility.Collapsed);
            if (slot.GroupHeader != null)
            {
                SetVisibilityIfChanged(slot.GroupHeader, Visibility.Collapsed);
            }
        }
    }

    private static string FormatFps(double value)
    {
        return Sanitize(value).ToString("0.00");
    }

    private static string FormatSourceHdr(bool? isHdr, string? colorimetry)
    {
        return isHdr switch
        {
            true when !string.IsNullOrWhiteSpace(colorimetry) => $"On ({colorimetry})",
            true => "On",
            false => "Off",
            _ => "\u2014"
        };
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

    private static string FormatSignedMs(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value))
        {
            return "\u2014";
        }

        return value.Value >= 0 ? $"+{value.Value:F1}ms" : $"{value.Value:F1}ms";
    }

    private static string FormatSignedMsPerSec(double? value)
    {
        if (!value.HasValue || double.IsNaN(value.Value))
        {
            return "\u2014";
        }

        return value.Value >= 0 ? $"+{value.Value:F2} ms/s" : $"{value.Value:F2} ms/s";
    }

    private static void SetTextIfChanged(TextBlock target, string value)
    {
        if (!string.Equals(target.Text, value, StringComparison.Ordinal))
        {
            target.Text = value;
        }
    }

    private void UpdateDecoderCountVisibility()
    {
        var selectedFormat = VideoFormatComboBox.SelectedItem as string ?? ViewModel.SelectedVideoFormat;
        var selectedFrameRate = GetSelectedFriendlyFrameRate();
        DecoderCountPanel.Visibility =
            string.Equals(selectedFormat, "MJPG", StringComparison.OrdinalIgnoreCase) && selectedFrameRate >= 90
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    private void UpdateOutputPathDisplay()
    {
        var path = ViewModel.OutputPath;
        if (string.IsNullOrEmpty(path))
        {
            OutputPathTextBox.Text = string.Empty;
            return;
        }

        ToolTipService.SetToolTip(OutputPathTextBox, path);

        var availableWidth = OutputPathTextBox.ActualWidth;
        if (availableWidth <= 0)
        {
            OutputPathTextBox.Text = path;
            return;
        }

        // FontSize 12 ≈ 7px per char, minus internal padding
        var maxChars = (int)((availableWidth - 20) / 7);
        if (path.Length <= maxChars)
        {
            OutputPathTextBox.Text = path;
            return;
        }

        var parts = path.Split('\\', '/');
        if (parts.Length <= 2)
        {
            OutputPathTextBox.Text = path;
            return;
        }

        // Progressively truncate: keep root, show as many trailing segments as fit
        var root = parts[0];
        for (int tailCount = parts.Length - 1; tailCount >= 1; tailCount--)
        {
            var tail = string.Join("\\", parts[^tailCount..]);
            var candidate = $"{root}\\...\\{tail}";
            if (candidate.Length <= maxChars)
            {
                OutputPathTextBox.Text = candidate;
                return;
            }
        }

        OutputPathTextBox.Text = $"{root}\\...\\{parts[^1]}";
    }

    private double GetSelectedFriendlyFrameRate()
    {
        if (FrameRateComboBox.SelectedItem is FrameRateOption option)
        {
            if (option.FriendlyValue > 0)
            {
                return option.FriendlyValue;
            }

            if (option.Value > 0)
            {
                return option.Value;
            }
        }

        return ViewModel.SelectedFrameRate;
    }

    private void DecoderCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DecoderCountComboBox.SelectedItem is int count)
        {
            _selectedDecoderCount = count;
            if (ViewModel.MjpegDecoderCount != count)
            {
                ViewModel.MjpegDecoderCount = count;
            }
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

    private void CleanupPreviewResources()
    {
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
        ResetPreviewSignalState();

        // Clean up CPU preview
        PreviewImage.Source = null;
        PreviewImage.Visibility = Visibility.Collapsed;
        _previewSource = null;
    }

    private void StopPreviewForShutdown()
    {
        _isPreviewReinitAnimating = false;
        StopPreviewFadeInTimer();
        ResetPreviewContentTransform();
        CleanupPreviewResources();
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
                MicrophoneToggle.IsEnabled = !ViewModel.IsRecording;
                AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled && !ViewModel.IsRecording;
                MicrophoneComboBox.IsEnabled = ViewModel.IsMicrophoneEnabled && !ViewModel.IsRecording;
                DeviceAudioModeToggle.IsEnabled = ViewModel.IsDeviceAudioControlSupported && !ViewModel.IsRecording;
                AnalogAudioGainSlider.IsEnabled = ViewModel.IsDeviceAudioControlSupported &&
                                                  string.Equals(ViewModel.SelectedDeviceAudioMode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase) &&
                                                  !ViewModel.IsRecording;
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
                UpdateOutputPathDisplay();
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
                if ((CustomAudioToggle.IsChecked == true) != ViewModel.IsCustomAudioInputEnabled)
                {
                    CustomAudioToggle.IsChecked = ViewModel.IsCustomAudioInputEnabled;
                }
                AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled && !ViewModel.IsRecording;
                break;

            case nameof(MainViewModel.IsMicrophoneEnabled):
                if ((MicrophoneToggle.IsChecked == true) != ViewModel.IsMicrophoneEnabled)
                {
                    MicrophoneToggle.IsChecked = ViewModel.IsMicrophoneEnabled;
                }
                MicrophoneComboBox.IsEnabled = ViewModel.IsMicrophoneEnabled && !ViewModel.IsRecording;
                UpdateMicrophoneControlsVisibility();
                break;

            case nameof(MainViewModel.IsDeviceAudioControlSupported):
            case nameof(MainViewModel.SelectedDeviceAudioMode):
            case nameof(MainViewModel.AnalogAudioGainPercent):
            case nameof(MainViewModel.AvailableDeviceAudioModes):
                ApplyDeviceAudioControlState();
                break;

            case nameof(MainViewModel.ShowAllCaptureOptions):
                if ((ShowAllCaptureOptionsToggle.IsChecked == true) != ViewModel.ShowAllCaptureOptions)
                {
                    ShowAllCaptureOptionsToggle.IsChecked = ViewModel.ShowAllCaptureOptions;
                }
                break;

            case nameof(MainViewModel.IsStatsVisible):
                if (StatsToggle.IsChecked != ViewModel.IsStatsVisible)
                {
                    StatsToggle.IsChecked = ViewModel.IsStatsVisible;
                }
                ApplyStatsVisibility(ViewModel.IsStatsVisible);
                break;

            case nameof(MainViewModel.IsSettingsVisible):
                ApplySettingsVisibility(ViewModel.IsSettingsVisible);
                break;

            case nameof(MainViewModel.SelectedAudioInputDevice):
                EnsureAudioInputSelection();
                break;

            case nameof(MainViewModel.SelectedMicrophoneDevice):
                EnsureMicrophoneSelection();
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
                break;

            case nameof(MainViewModel.IsAudioPreviewActive):
                SetAudioMeterMonitoringState(ViewModel.IsAudioPreviewActive);
                break;

            case nameof(MainViewModel.PreviewVolume):
                if (!_isVolumeFadingIn)
                {
                    var volumePct = ViewModel.PreviewVolume * 100;
                    if (PreviewVolumeSlider.Value != volumePct)
                    {
                        PreviewVolumeSlider.Value = volumePct;
                    }

                    PreviewVolumeLabel.Text = $"{(int)volumePct}%";
                }
                break;

            case nameof(MainViewModel.MicrophoneVolume):
                SyncMicrophoneVolumeControls(ViewModel.MicrophoneVolume);
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

    private Task PresenterActionAsync(Action<Microsoft.UI.Windowing.OverlappedPresenter> action, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var appWindow = GetAppWindow();
            if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter)
            {
                action(presenter);
            }
        }, cancellationToken);
    }

    public Task MinimizeAsync(CancellationToken cancellationToken = default) =>
        PresenterActionAsync(p => p.Minimize(), cancellationToken);

    public Task MaximizeAsync(CancellationToken cancellationToken = default) =>
        PresenterActionAsync(p => p.Maximize(), cancellationToken);

    public Task RestoreAsync(CancellationToken cancellationToken = default) =>
        PresenterActionAsync(p => p.Restore(), cancellationToken);

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
            renderer.SetExpectedFrameRate(rendererFps);

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
        CleanupPreviewResources();
        _previewLastPresentedTick = 0;
        _previewMinPresentationIntervalMs = Math.Max(1L, (long)Math.Round(1000.0 / 60.0));
        Logger.Log("Preview renderer stopped.");
        return Task.CompletedTask;
    }

    private void AnimateAudioMeterTick()
    {
        _audioMeterTargetLevel = ViewModel.AudioMeterTarget;
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

        // Update visuals — two-layer meter: raw (grey) + volume-adjusted (color)
        var trackWidth = AudioMeterTrack.ActualWidth;
        if (trackWidth > 0)
        {
            var trackHeight = AudioMeterTrack.ActualHeight > 0 ? AudioMeterTrack.ActualHeight : 8;
            var rawLevel = _audioMeterDisplayLevel;
            var colorLevel = rawLevel * ViewModel.PreviewVolume;

            AudioMeterRawClip.Rect = new Windows.Foundation.Rect(0, 0, trackWidth * rawLevel, trackHeight);
            AudioMeterColorClip.Rect = new Windows.Foundation.Rect(0, 0, trackWidth * colorLevel, trackHeight);

            // Peak hold + range markers track raw signal
            AudioPeakHoldTranslate.X = TranslateMarker(trackWidth, _audioPeakHoldLevel, AudioPeakHoldIndicator.Width);
            AudioRangeMinTranslate.X = TranslateMarker(trackWidth, _audioRangeMin, AudioRangeMinMarker.Width);
            AudioRangeMaxTranslate.X = TranslateMarker(trackWidth, _audioRangeMax, AudioRangeMaxMarker.Width);
        }

        if (ViewModel.IsMicrophoneEnabled)
        {
            _micMeterTargetLevel = Math.Clamp(ViewModel.MicrophoneMeterTarget, 0.0, 1.0);
            if (_micMeterTargetLevel > _micMeterDisplayLevel)
            {
                _micMeterDisplayLevel += (_micMeterTargetLevel - _micMeterDisplayLevel) * 0.4;
            }
            else
            {
                _micMeterDisplayLevel += (_micMeterTargetLevel - _micMeterDisplayLevel) * 0.25;
            }

            if (_micMeterDisplayLevel < 0.001)
            {
                _micMeterDisplayLevel = 0;
            }

            var micTrackWidth = MicMeterTrack.ActualWidth - 2;
            if (micTrackWidth > 0)
            {
                var micFillWidth = _micMeterDisplayLevel * micTrackWidth;
                MicMeterClip.Rect = new Windows.Foundation.Rect(0, 0, micFillWidth, 8);
            }
        }
        else if (_micMeterDisplayLevel != 0 || _micMeterTargetLevel != 0)
        {
            _micMeterDisplayLevel = 0;
            _micMeterTargetLevel = 0;
            MicMeterClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
        }

        if (_audioMeterDisplayLevel == 0 &&
            _audioPeakHoldLevel == 0 &&
            target == 0 &&
            _micMeterDisplayLevel == 0 &&
            _micMeterTargetLevel == 0)
        {
            _audioMeterAnimationTimer?.Stop();
            ViewModel.ResetAudioMeterTimerFlag();
        }
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
        AudioMeterColorClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
        AudioMeterRawClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
        _micMeterDisplayLevel = 0;
        _micMeterTargetLevel = 0;
        MicMeterClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
    }

    private void InitializeAudioMeterBrushes()
    {
        _audioMeterAnimationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _audioMeterAnimationTimer.Tick += (_, _) => AnimateAudioMeterTick();

        _audioMeterColorBrush = (LinearGradientBrush)AudioMeterFill.Background;

        // Clip content Grids to inner rounded rect (track CornerRadius=4, BorderThickness=1 → inner radius 3)
        SetupRoundedContentClip(AudioMeterContent, 3f);
        SetupRoundedContentClip(MicMeterContent, 3f);
    }

    private static void SetupRoundedContentClip(FrameworkElement element, float cornerRadius)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var geo = visual.Compositor.CreateRoundedRectangleGeometry();
        geo.CornerRadius = new Vector2(cornerRadius, cornerRadius);
        visual.Clip = visual.Compositor.CreateGeometricClip(geo);
        element.SizeChanged += (_, e) =>
        {
            geo.Size = new Vector2((float)e.NewSize.Width, (float)e.NewSize.Height);
        };
    }

    private void EnsureAudioMeterTimerRunning()
    {
        if (_audioMeterAnimationTimer is { IsEnabled: false })
        {
            _audioMeterAnimationTimer.Start();
        }
    }

    private void SetAudioMeterMonitoringState(bool isMonitoring)
    {
        if (_audioMeterColorBrush == null) return;

        // Color layer visible only when monitoring; grey raw layer always shows through
        AudioMeterFill.Opacity = isMonitoring ? 1.0 : 0.0;
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

        foreach (var element in new UIElement[] { AudioMeterRawFill, AudioMeterFill, AudioPeakHoldIndicator, AudioRangeMinMarker, AudioRangeMaxMarker })
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
                SetAudioMeterMonitoringState(ViewModel.IsAudioPreviewActive);
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

    private Task AnimatePreviewTransitionAsync(double opacityTarget, double scaleTarget, int durationMs, EasingMode easingMode)
    {
        var duration = TimeSpan.FromMilliseconds(durationMs);
        var easing = new CubicEase { EasingMode = easingMode };

        var fade = new DoubleAnimation { To = opacityTarget, Duration = new Duration(duration), EasingFunction = easing };
        Storyboard.SetTarget(fade, PreviewContentGrid);
        Storyboard.SetTargetProperty(fade, "Opacity");

        var scaleX = new DoubleAnimation { To = scaleTarget, Duration = new Duration(duration), EasingFunction = easing };
        Storyboard.SetTarget(scaleX, PreviewContentScale);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");

        var scaleY = new DoubleAnimation { To = scaleTarget, Duration = new Duration(duration), EasingFunction = easing };
        Storyboard.SetTarget(scaleY, PreviewContentScale);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");

        var storyboard = new Storyboard();
        storyboard.Children.Add(fade);
        storyboard.Children.Add(scaleX);
        storyboard.Children.Add(scaleY);
        return BeginStoryboardAsync(storyboard);
    }

    private Task AnimatePreviewOutAsync()
    {
        FadeOutShadow(_videoShadowVisual, durationMs: 150);
        return AnimatePreviewTransitionAsync(0.0, 0.97, 200, EasingMode.EaseIn);
    }

    private Task AnimatePreviewInAsync()
    {
        FadeInShadow(_videoShadowVisual, delayMs: 0, durationMs: 400);
        return AnimatePreviewTransitionAsync(1.0, 1.0, 250, EasingMode.EaseOut);
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

    private void ApplySettingsVisibility(bool visible)
    {
        if (_isSettingsShelfAnimating)
        {
            return;
        }

        var isCurrentlyVisible = SettingsOverlayPanel.Visibility == Visibility.Visible;
        if (visible == isCurrentlyVisible)
        {
            return;
        }

        if (visible)
        {
            ShowSettingsShelf();
        }
        else
        {
            HideSettingsShelf();
        }
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

    private void AnimateSettingsShelf(bool show)
    {
        _isSettingsShelfAnimating = true;
        var durationMs = show ? 250 : 200;
        var easing = new CubicEase { EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn };
        var duration = TimeSpan.FromMilliseconds(durationMs);

        double targetHeight;
        if (show)
        {
            // Measure natural height without rendering (opacity 0, same sync block)
            SettingsOverlayPanel.Opacity = 0;
            SettingsOverlayPanel.Height = double.NaN;
            SettingsOverlayPanel.Visibility = Visibility.Visible;
            SettingsOverlayPanel.UpdateLayout();
            targetHeight = SettingsOverlayPanel.ActualHeight;
            SettingsOverlayPanel.Height = 0;
        }
        else
        {
            targetHeight = SettingsOverlayPanel.ActualHeight;
            SettingsOverlayPanel.Height = targetHeight; // Pin to numeric value so animation can interpolate
        }

        var heightAnim = new DoubleAnimation
        {
            To = show ? targetHeight : 0,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(heightAnim, SettingsOverlayPanel);
        Storyboard.SetTargetProperty(heightAnim, "Height");

        var fade = new DoubleAnimation
        {
            From = show ? 0 : 1,
            To = show ? 1 : 0,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(fade, SettingsOverlayPanel);
        Storyboard.SetTargetProperty(fade, "Opacity");

        var storyboard = new Storyboard();
        storyboard.Children.Add(heightAnim);
        storyboard.Children.Add(fade);
        storyboard.Completed += (_, _) =>
        {
            if (show)
            {
                SettingsOverlayPanel.Height = double.NaN; // Return to Auto
                SettingsOverlayPanel.Opacity = 1;
            }
            else
            {
                SettingsOverlayPanel.Visibility = Visibility.Collapsed;
                SettingsOverlayPanel.Height = double.NaN;
                SettingsOverlayPanel.Opacity = 1;
            }
            _isSettingsShelfAnimating = false;
        };
        storyboard.Begin();
    }

    private void ShowSettingsShelf() => AnimateSettingsShelf(show: true);
    private void HideSettingsShelf() => AnimateSettingsShelf(show: false);

    #region Full screen mode

    private void OnContentKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape && _isFullScreen)
        {
            e.Handled = true;
            ExitFullScreen();
        }
    }

    private void PreviewBorder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        ToggleFullScreen();
    }

    private void FullScreenMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ToggleFullScreen();
    }

    private void FullScreenButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFullScreen();
    }

    private void ToggleFullScreen()
    {
        if (_isFullScreen)
        {
            ExitFullScreen();
        }
        else
        {
            EnterFullScreen();
        }
    }

    private async void EnterFullScreen()
    {
        if (_isFullScreenTransitioning || _isFullScreen) return;
        _isFullScreenTransitioning = true;

        var appWindow = GetAppWindow();
        _preFullScreenPosition = appWindow.Position;
        _preFullScreenBounds = new Windows.Graphics.RectInt32(
            appWindow.Position.X, appWindow.Position.Y,
            appWindow.Size.Width, appWindow.Size.Height);
        _preFullScreenSettingsVisible = SettingsOverlayPanel.Visibility == Visibility.Visible;
        _preFullScreenStatsDockVisible = StatsDockPanel.Visibility == Visibility.Visible;

        // Capture pre-transition preview rect
        var transform = PreviewBorder.TransformToVisual((UIElement)Content);
        var prePosition = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
        var preW = PreviewBorder.ActualWidth;
        var preH = PreviewBorder.ActualHeight;

        // Commit layout instantly
        ControlBarBorder.Visibility = Visibility.Collapsed;
        ControlBarShadowHost.Visibility = Visibility.Collapsed;
        if (_preFullScreenSettingsVisible)
        {
            SettingsOverlayPanel.Visibility = Visibility.Collapsed;
            SettingsOverlayPanel.Height = double.NaN;
            SettingsOverlayPanel.Opacity = 1;
            _isSettingsShelfAnimating = false;
        }
        if (_preFullScreenStatsDockVisible)
        {
            HideStatsDockPanel(immediate: true);
        }

        PreviewBorder.Margin = new Thickness(0);
        PreviewShadowHost.Margin = new Thickness(0);
        PreviewShadowHost.CornerRadius = new CornerRadius(0);

        ((Grid)Content).Background = new SolidColorBrush(Microsoft.UI.Colors.Black);
        VideoShadowHost.Visibility = Visibility.Collapsed;

        appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);

        // Wait for layout, then animate
        await WaitForSizeChangedAsync(PreviewContentGrid, 200);

        var postTransform = PreviewBorder.TransformToVisual((UIElement)Content);
        var postPosition = postTransform.TransformPoint(new Windows.Foundation.Point(0, 0));
        var postW = PreviewBorder.ActualWidth;
        var postH = PreviewBorder.ActualHeight;

        if (postW > 0 && postH > 0)
        {
            AnimateFullScreenRect(
                prePosition, preW, preH,
                postPosition, postW, postH,
                () =>
                {
                    _isFullScreen = true;
                    _isFullScreenTransitioning = false;
                    UpdateFullScreenButtonState();
                });
        }
        else
        {
            _isFullScreen = true;
            _isFullScreenTransitioning = false;
            UpdateFullScreenButtonState();
        }
    }

    private async void ExitFullScreen()
    {
        if (_isFullScreenTransitioning || !_isFullScreen) return;
        _isFullScreenTransitioning = true;

        var transform = PreviewBorder.TransformToVisual((UIElement)Content);
        var prePosition = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
        var preW = PreviewBorder.ActualWidth;
        var preH = PreviewBorder.ActualHeight;

        // Commit layout restoration
        var appWindow = GetAppWindow();
        appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped);
        appWindow.Move(_preFullScreenPosition);
        appWindow.Resize(new Windows.Graphics.SizeInt32(_preFullScreenBounds.Width, _preFullScreenBounds.Height));

        PreviewBorder.Margin = new Thickness(12, 6, 12, 6);
        PreviewShadowHost.Margin = new Thickness(16);
        PreviewShadowHost.CornerRadius = new CornerRadius(4);

        ControlBarBorder.Visibility = Visibility.Visible;
        ControlBarShadowHost.Visibility = Visibility.Visible;
        if (_preFullScreenSettingsVisible)
        {
            SettingsOverlayPanel.Visibility = Visibility.Visible;
        }
        if (_preFullScreenStatsDockVisible)
        {
            ShowStatsDockPanel();
        }

        ((Grid)Content).Background = null;

        await WaitForSizeChangedAsync(PreviewContentGrid, 200);

        var postTransform = PreviewBorder.TransformToVisual((UIElement)Content);
        var postPosition = postTransform.TransformPoint(new Windows.Foundation.Point(0, 0));
        var postW = PreviewBorder.ActualWidth;
        var postH = PreviewBorder.ActualHeight;

        if (postW > 0 && postH > 0)
        {
            AnimateFullScreenRect(
                prePosition, preW, preH,
                postPosition, postW, postH,
                () =>
                {
                    _isFullScreen = false;
                    _isFullScreenTransitioning = false;
                    UpdateFullScreenButtonState();
                    UpdateVideoContentOverlays();
                    VideoShadowHost.Visibility = Visibility.Visible;
                    FadeInShadow(_videoShadowVisual, delayMs: 0, durationMs: 400);
                });
        }
        else
        {
            _isFullScreen = false;
            _isFullScreenTransitioning = false;
            UpdateFullScreenButtonState();
            UpdateVideoContentOverlays();
            VideoShadowHost.Visibility = Visibility.Visible;
            FadeInShadow(_videoShadowVisual, delayMs: 0, durationMs: 400);
        }
    }

    /// <summary>
    /// Animates PreviewBorder from one rect to another using a single
    /// compositor-thread progress scalar driving both Scale and Offset
    /// via ExpressionAnimations. This guarantees per-frame synchronization
    /// (no arc) and runs at DWM refresh rate (120hz+).
    /// </summary>
    private void AnimateFullScreenRect(
        Windows.Foundation.Point prePos, double preW, double preH,
        Windows.Foundation.Point postPos, double postW, double postH,
        Action onCompleted)
    {
        var scaleX = (float)(preW / postW);
        var scaleY = (float)(preH / postH);
        // Offset compensates for CenterPoint at element center:
        // the scale-induced position shift is (postSize/2)*(1-scale),
        // so offset = positionDelta + (preSize-postSize)/2.
        var offsetX = (float)(prePos.X - postPos.X + (preW - postW) / 2);
        var offsetY = (float)(prePos.Y - postPos.Y + (preH - postH) / 2);

        var visual = ElementCompositionPreview.GetElementVisual(PreviewBorder);
        var compositor = visual.Compositor;

        visual.CenterPoint = new Vector3((float)(postW / 2), (float)(postH / 2), 0);

        // Single progress scalar drives both properties — one timeline,
        // one easing evaluation per frame, zero desync.
        var props = compositor.CreatePropertySet();
        props.InsertScalar("Progress", 0f);

        var duration = TimeSpan.FromMilliseconds(350);
        var easing = compositor.CreateCubicBezierEasingFunction(
            new Vector2(0.2f, 0f), new Vector2(0f, 1f));

        var progressAnim = compositor.CreateScalarKeyFrameAnimation();
        progressAnim.InsertKeyFrame(1f, 1f, easing);
        progressAnim.Duration = duration;

        var scaleExpr = compositor.CreateExpressionAnimation(
            "Vector3(s.X + (1 - s.X) * p.Progress, s.Y + (1 - s.Y) * p.Progress, 1)");
        scaleExpr.SetVector3Parameter("s", new Vector3(scaleX, scaleY, 1));
        scaleExpr.SetReferenceParameter("p", props);

        var offsetExpr = compositor.CreateExpressionAnimation(
            "Vector3(o.X * (1 - p.Progress), o.Y * (1 - p.Progress), 0)");
        offsetExpr.SetVector3Parameter("o", new Vector3(offsetX, offsetY, 0));
        offsetExpr.SetReferenceParameter("p", props);

        // Batch tracks the finite-duration progress animation for Completed.
        var batch = compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
        props.StartAnimation("Progress", progressAnim);
        batch.End();

        // Expression animations run outside the batch (indefinite lifetime,
        // stopped explicitly in Completed).
        visual.StartAnimation("Scale", scaleExpr);
        visual.StartAnimation("Offset", offsetExpr);

        batch.Completed += (_, _) =>
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                visual.StopAnimation("Scale");
                visual.StopAnimation("Offset");
                visual.Scale = Vector3.One;
                visual.Offset = Vector3.Zero;
                visual.CenterPoint = Vector3.Zero;
                onCompleted();
            });
        };
    }

    private static Task WaitForSizeChangedAsync(FrameworkElement element, int timeoutMs)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        SizeChangedEventHandler? handler = null;
        handler = (_, _) =>
        {
            element.SizeChanged -= handler;
            tcs.TrySetResult(true);
        };
        element.SizeChanged += handler;

        // Timeout fallback
        _ = Task.Delay(timeoutMs).ContinueWith(_ =>
        {
            element.DispatcherQueue.TryEnqueue(() =>
            {
                element.SizeChanged -= handler;
                tcs.TrySetResult(false);
            });
        });

        return tcs.Task;
    }

    private void UpdateFullScreenButtonState()
    {
        if (_isFullScreen)
        {
            FullScreenButtonIcon.Glyph = "\uE73F";
            ToolTipService.SetToolTip(FullScreenButton, "Exit full screen");
            FullScreenMenuItem.Text = "Exit Full Screen";
            if (FullScreenMenuItem.Icon is FontIcon icon) icon.Glyph = "\uE73F";
        }
        else
        {
            FullScreenButtonIcon.Glyph = "\uE740";
            ToolTipService.SetToolTip(FullScreenButton, "Full screen");
            FullScreenMenuItem.Text = "Enter Full Screen";
            if (FullScreenMenuItem.Icon is FontIcon icon) icon.Glyph = "\uE740";
        }
    }

    #endregion

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

