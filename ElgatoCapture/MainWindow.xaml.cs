using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.Services;
using ElgatoCapture.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Media.Playback;
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
    private const int PreviewStartupOverlayUpdateIntervalMs = 100;
    private static readonly TimeSpan PreviewStartupPlaybackAdvanceThreshold = TimeSpan.FromMilliseconds(33);
    private static readonly int PreviewStartupVisualTimeoutMs = ResolveStartupSetting(
        "ELGATOCAPTURE_PREVIEW_START_TIMEOUT_MS",
        PreviewStartupDefaultVisualTimeoutMs,
        PreviewStartupMinVisualTimeoutMs,
        PreviewStartupMaxVisualTimeoutMs);

    public MainViewModel ViewModel { get; }
    private readonly DispatcherQueue _dispatcherQueue;
    private SoftwareBitmapSource? _previewSource;
    private MediaPlayer? _previewMediaPlayer;
    private long _previewFramesArrived;
    private long _previewFramesDisplayed;
    private long _previewFramesDropped;
    private long _previewLastLogTick;
    private long _previewLastResizeLogTick;
    private long _previewLastPresentedTick;
    private long _previewResizeSuppressUntilTick;
    private int _previewUiInFlight;
    private readonly object _previewCadenceLock = new();
    private readonly double[] _previewDisplayIntervalWindowMs = new double[300];
    private int _previewDisplayIntervalCount;
    private int _previewDisplayIntervalIndex;
    private long _previewLastDisplayTick;
    private int _windowCloseRequested;
    private int _windowCloseCleanupStarted;
    private long _previewMinPresentationIntervalMs;
    private readonly int _previewResizeDebounceMs;
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
    private DispatcherQueueTimer? _previewStartupOverlayTimer;
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
    private MediaPlaybackState _previewStartupLastPlaybackState = MediaPlaybackState.None;
    private bool _previewStartupInitialPlayIssued;
    private bool _previewStartupPausedRecoveryIssued;
    private bool _previewStartupPlaybackPositionInitialized;
    private int _previewStartupFailureStopScheduled;
    private long _previewStartupLastPositionDispatchTick;
    private bool _previewStopRequestedByUser;
    private bool _isWindowClosing;
    private bool _toggleLabelsVisible;
    private bool _isSettingsShelfAnimating;
    private bool _captureSettingsNarrow;
    private const double ControlBarLabelThreshold = 900.0;
    private const int MinWindowWidth = 780;
    private const int MinWindowHeight = 450;
    private WndProcDelegate? _minSizeWndProc;
    private IntPtr _originalWndProc;

    private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)
        => Math.Abs(a - b) < tolerance;

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
        _previewStartupLastPlaybackState = MediaPlaybackState.None;
        _previewStartupInitialPlayIssued = false;
        _previewStartupPausedRecoveryIssued = false;
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
        _previewStartupLastPlaybackState = MediaPlaybackState.None;
        _previewStartupInitialPlayIssued = false;
        _previewStartupPausedRecoveryIssued = false;
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
        PreviewLoadingOverlay.Visibility = Visibility.Visible;
        UpdatePreviewLoadingOverlayText();

        _previewStartupOverlayTimer ??= _dispatcherQueue.CreateTimer();
        _previewStartupOverlayTimer.Interval = TimeSpan.FromMilliseconds(PreviewStartupOverlayUpdateIntervalMs);
        _previewStartupOverlayTimer.IsRepeating = true;
        _previewStartupOverlayTimer.Tick -= PreviewStartupOverlayTimer_Tick;
        _previewStartupOverlayTimer.Tick += PreviewStartupOverlayTimer_Tick;
        _previewStartupOverlayTimer.Start();
    }

    private void StopPreviewStartupOverlay()
    {
        _previewStartupOverlayTimer?.Stop();
        PreviewLoadingOverlay.Visibility = Visibility.Collapsed;
        PreviewLoadingText.Text = "Starting preview...";
    }

    private void PreviewStartupOverlayTimer_Tick(object? sender, object e)
    {
        UpdatePreviewLoadingOverlayText();
    }

    private void UpdatePreviewLoadingOverlayText()
    {
        if (!ViewModel.IsPreviewing ||
            _previewStartupState is PreviewStartupState.Rendering or PreviewStartupState.Failed ||
            _previewFirstVisualConfirmed)
        {
            return;
        }

        double remainingMs = PreviewStartupVisualTimeoutMs;
        if (_previewStartupRequestedUtc.HasValue)
        {
            remainingMs = Math.Max(0d, PreviewStartupVisualTimeoutMs - (DateTimeOffset.UtcNow - _previewStartupRequestedUtc.Value).TotalMilliseconds);
        }

        PreviewLoadingText.Text =
            $"Waiting for video engine...{Environment.NewLine}" +
            $"Timeout in {remainingMs / 1000.0:0.0}s";
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
            _previewMediaPlayer?.PlaybackSession.PositionChanged -= PreviewPlaybackSession_PositionChanged;
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
        var player = _previewMediaPlayer;
        var session = player?.PlaybackSession;
        if (session == null)
        {
            Logger.Log(
                $"PREVIEW_START_PLAYBACK_SNAPSHOT attempt={_previewStartupAttemptId ?? "none"} " +
                $"reason={reason} player=null");
            return;
        }

        Logger.Log(
            $"PREVIEW_START_PLAYBACK_SNAPSHOT attempt={_previewStartupAttemptId ?? "none"} " +
            $"reason={reason} state={session.PlaybackState} " +
            $"positionMs={session.Position.TotalMilliseconds:0.###} " +
            $"gpuVisible={PreviewPlayerElement.Visibility} " +
            $"required={BuildPreviewStartupSignalList(_previewStartupRequiredSignals)} " +
            $"received={BuildPreviewStartupSignalList(_previewStartupReceivedSignals)} " +
            $"missing={BuildPreviewStartupMissingSignals()}");
    }

    private void EnsurePreviewPlaybackStarted(string reason, bool recoveryAttempt)
    {
        if (!ViewModel.IsPreviewing || _previewStopRequestedByUser || _isWindowClosing)
        {
            Logger.Log(
                $"PREVIEW_START_PLAY_SKIPPED attempt={_previewStartupAttemptId ?? "none"} " +
                $"reason={reason} startupActive={ViewModel.IsPreviewing} stopRequested={_previewStopRequestedByUser} closing={_isWindowClosing}");
            return;
        }

        var player = _previewMediaPlayer;
        if (player == null)
        {
            Logger.Log(
                $"PREVIEW_START_PLAY_SKIPPED attempt={_previewStartupAttemptId ?? "none"} " +
                $"reason={reason} player=null");
            return;
        }

        if (recoveryAttempt)
        {
            if (_previewStartupPausedRecoveryIssued)
            {
                Logger.Log(
                    $"PREVIEW_START_PLAY_SKIPPED attempt={_previewStartupAttemptId ?? "none"} " +
                    $"reason={reason} recovery=already-issued");
                return;
            }

            _previewStartupPausedRecoveryIssued = true;
            _previewRecoveryAttemptCount++;
        }
        else
        {
            if (_previewStartupInitialPlayIssued)
            {
                Logger.Log(
                    $"PREVIEW_START_PLAY_SKIPPED attempt={_previewStartupAttemptId ?? "none"} " +
                    $"reason={reason} initial=already-issued");
                return;
            }

            _previewStartupInitialPlayIssued = true;
        }

        Logger.Log(
            $"PREVIEW_START_PLAY_ISSUED attempt={_previewStartupAttemptId ?? "none"} " +
            $"reason={reason} recovery={recoveryAttempt} state={player.PlaybackSession.PlaybackState} " +
            $"positionMs={player.PlaybackSession.Position.TotalMilliseconds:0.###}");
        player.Play();
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
                await ViewModel.StopPreviewAsync();
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
            $"gpuVisible={PreviewPlayerElement.Visibility} cpuVisible={PreviewImage.Visibility} " +
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

        _previewFirstVisualConfirmed = true;
        _previewStartupReceivedSignals |= PreviewStartupSignalFlags.FirstVisual;
        _previewFirstVisualUtc = DateTimeOffset.UtcNow;
        SetPreviewStartupState(PreviewStartupState.Rendering);
        StopPreviewStartupWatchdog();
        StopPreviewStartupOverlay();
        _previewStartupMissingSignals = string.Empty;
        var elapsedMs = _previewStartupRequestedUtc.HasValue
            ? (DateTimeOffset.UtcNow - _previewStartupRequestedUtc.Value).TotalMilliseconds
            : 0;
        Logger.Log(
            $"PREVIEW_FIRST_VISUAL_CONFIRMED attempt={_previewStartupAttemptId ?? "none"} " +
            $"source={source} elapsedMs={elapsedMs:0} recovery={_previewRecoveryAttemptCount}");
    }

    private void ResetPreviewStartupTracking(bool keepRecoveryCount = false)
    {
        StopPreviewStartupWatchdog();
        StopPreviewStartupOverlay();
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
        _previewStartupLastPlaybackState = MediaPlaybackState.None;
        _previewStartupInitialPlayIssued = false;
        _previewStartupPausedRecoveryIssued = false;
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

    private void PreviewMediaPlayer_MediaOpened(MediaPlayer sender, object args)
    {
        Logger.Log(
            $"PREVIEW_MEDIA_EVENT event=MediaOpened attempt={_previewStartupAttemptId ?? "none"} " +
            $"state={sender.PlaybackSession.PlaybackState} " +
            $"positionMs={sender.PlaybackSession.Position.TotalMilliseconds:0.###} " +
            $"gpuVisible={PreviewPlayerElement.Visibility}");
        _dispatcherQueue.TryEnqueue(MarkGpuStartupSignalMediaOpened);
    }

    private void PreviewPlaybackSession_PlaybackStateChanged(MediaPlaybackSession sender, object args)
    {
        var state = sender.PlaybackState;
        if (state == _previewStartupLastPlaybackState && !IsPreviewStartupSignalWindowActive())
        {
            return;
        }

        _previewStartupLastPlaybackState = state;
        Logger.Log(
            $"PREVIEW_MEDIA_EVENT event=PlaybackStateChanged attempt={_previewStartupAttemptId ?? "none"} " +
            $"state={state} positionMs={sender.Position.TotalMilliseconds:0.###} " +
            $"startupActive={IsPreviewStartupSignalWindowActive()}");

        if (state == MediaPlaybackState.Paused &&
            IsPreviewStartupSignalWindowActive() &&
            !_previewStopRequestedByUser &&
            !_isWindowClosing)
        {
            if (_dispatcherQueue.HasThreadAccess)
            {
                EnsurePreviewPlaybackStarted("PlaybackStateChanged:Paused", recoveryAttempt: true);
            }
            else
            {
                _dispatcherQueue.TryEnqueue(() =>
                    EnsurePreviewPlaybackStarted("PlaybackStateChanged:Paused", recoveryAttempt: true));
            }
        }
    }

    private void PreviewPlaybackSession_PositionChanged(MediaPlaybackSession sender, object args)
    {
        if (_previewGpuSignalPlaybackAdvancing || !_previewStartupExpectGpuDualSignals)
        {
            Logger.Log(
                $"PREVIEW_MEDIA_EVENT event=PositionChangedIgnored attempt={_previewStartupAttemptId ?? "none"} " +
                $"reason={( _previewGpuSignalPlaybackAdvancing ? "already-signaled" : "not-gpu-startup")} " +
                $"positionMs={sender.Position.TotalMilliseconds:0.###}");
            return;
        }

        var position = sender.Position;
        var eventCount = Interlocked.Increment(ref _previewStartupPositionEventCount);
        if (eventCount <= 120 || eventCount % 60 == 0)
        {
            Logger.Log(
                $"PREVIEW_MEDIA_EVENT event=PositionChanged attempt={_previewStartupAttemptId ?? "none"} " +
                $"count={eventCount} positionMs={position.TotalMilliseconds:0.###}");
        }

        var nowTick = Environment.TickCount64;
        var lastDispatchTick = Interlocked.Read(ref _previewStartupLastPositionDispatchTick);
        if (nowTick - lastDispatchTick < 100)
        {
            return;
        }

        Interlocked.Exchange(ref _previewStartupLastPositionDispatchTick, nowTick);
        _dispatcherQueue.TryEnqueue(() => MarkGpuStartupSignalPlaybackAdvancing(position));
    }

    private void PreviewMediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            if (!_previewStartupExpectGpuDualSignals || _previewFirstVisualConfirmed || !ViewModel.IsPreviewing)
            {
                return;
            }

            _previewStartupMissingSignals = BuildPreviewStartupMissingSignals();
            var failureReason = string.IsNullOrWhiteSpace(args.ErrorMessage)
                ? "media-player-failed"
                : $"media-player-failed:{args.ErrorMessage}";
            SetPreviewStartupState(PreviewStartupState.Failed, failureReason);
            StopPreviewStartupWatchdog();
            StopPreviewStartupOverlay();
            ViewModel.StatusText = "Preview failed to start (media pipeline error).";
            Logger.Log($"PREVIEW_START_MEDIA_FAILED attempt={_previewStartupAttemptId ?? "none"} reason={failureReason}");
            SchedulePreviewStartupFailureStop(failureReason);
        });
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
            return "Elgato Capture";
        }

        var buildTime = File.GetLastWriteTime(exePath);
        if (buildTime == DateTime.MinValue)
        {
            return "Elgato Capture";
        }

        return $"Elgato Capture (build {buildTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)})";
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
        _previewResizeDebounceMs = MainViewModel.GetIntFromEnv("ELGATOCAPTURE_PREVIEW_RESIZE_DEBOUNCE_MS", defaultValue: 250, minValue: 50, maxValue: 2000);

        // Set window handle for folder picker
        var hwnd = WindowNative.GetWindowHandle(this);
        ViewModel.SetWindowHandle(hwnd);

        // Enforce minimum window size via WM_GETMINMAXINFO
        _minSizeWndProc = MinSizeWndProc;
        _originalWndProc = GetWindowLongPtr(hwnd, GWLP_WNDPROC);
        SetWindowLongPtr(hwnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_minSizeWndProc));

        // Set initial window size and constraints
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
            Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd));

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
        ViewModel.PreviewFrameReady += ViewModel_PreviewFrameReady;
        ViewModel.PreviewStartRequested += ViewModel_PreviewStartRequested;
        ViewModel.PreviewStopRequested += ViewModel_PreviewStopRequested;

        // Wire up UI controls to ViewModel
        SetupBindings();

        // Refresh devices on load - use Loaded event to ensure XAML is fully parsed
        var mainContent = (FrameworkElement)this.Content;
        mainContent.Loaded += MainWindow_Loaded;
        mainContent.SizeChanged += MainWindow_SizeChanged;
        Closed += MainWindow_Closed;

    }

    private void SetupBindings()
    {
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
        CustomAudioToggle.IsOn = ViewModel.IsCustomAudioInputEnabled;
        CustomAudioToggle.IsEnabled = !ViewModel.IsRecording;
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
        HdrToggle.IsEnabled = ViewModel.IsHdrAvailable && !ViewModel.IsRecording;
        TrueHdrPreviewToggle.IsChecked = ViewModel.IsTrueHdrPreviewEnabled;
        TrueHdrPreviewToggle.IsEnabled = !ViewModel.IsRecording && !ViewModel.IsPreviewing;
        UpdateAudioMeterLevel(ViewModel.AudioPeak);
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
                frameRate.IsEnabled &&
                !IsFrameRateMatch(frameRate.Value, ViewModel.SelectedFrameRate))
            {
                ViewModel.SelectedFrameRate = frameRate.Value;
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
        TrueHdrPreviewToggle.Click += (s, e) => ViewModel.IsTrueHdrPreviewEnabled = TrueHdrPreviewToggle.IsChecked == true;
        AudioRecordToggle.Checked += (s, e) => ViewModel.IsAudioEnabled = true;
        AudioRecordToggle.Unchecked += (s, e) => ViewModel.IsAudioEnabled = false;
        AudioPreviewToggle.Checked += (s, e) => ViewModel.IsAudioPreviewEnabled = true;
        AudioPreviewToggle.Unchecked += (s, e) => ViewModel.IsAudioPreviewEnabled = false;
        CustomAudioToggle.Toggled += (s, e) => ViewModel.IsCustomAudioInputEnabled = CustomAudioToggle.IsOn;
        AudioMeterTrack.SizeChanged += (s, e) => UpdateAudioMeterLevel(ViewModel.AudioPeak);
        ControlBarBorder.SizeChanged += (s, e) => UpdateToggleLabelVisibility(e.NewSize.Width);
        CaptureSettingsGrid.SizeChanged += CaptureSettingsGrid_SizeChanged;
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
        RecordButtonLabel.Visibility = vis;
        RecordButtonStopLabel.Visibility = vis;
        RecordButton.MinWidth = showLabels ? 120 : 44;
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
        // Unsubscribe immediately - we only want this to run once
        ((FrameworkElement)this.Content).Loaded -= MainWindow_Loaded;

        _ = RunUiEventHandlerAsync(async () =>
        {
            Logger.Log("=== MainWindow_Loaded - Starting device enumeration ===");
            try
            {
                await ViewModel.InitializeAsync();
                await ViewModel.RefreshDevicesAsync();
            }
            finally
            {
                StartAutomationServices();
            }
        }, nameof(MainWindow_Loaded));
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
        Interlocked.Exchange(ref _previewResizeSuppressUntilTick, nowTick + _previewResizeDebounceMs);

        if (!ViewModel.IsPreviewing)
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
            Logger.Log($"Preview resize active. Suppressing frame presents for {_previewResizeDebounceMs}ms.");
        }
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (Interlocked.Exchange(ref _windowCloseCleanupStarted, 1) != 0)
        {
            return;
        }

        _isWindowClosing = true;

        if (this.Content is FrameworkElement mainContent)
        {
            mainContent.SizeChanged -= MainWindow_SizeChanged;
        }

        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.PreviewFrameReady -= ViewModel_PreviewFrameReady;
        ViewModel.PreviewStartRequested -= ViewModel_PreviewStartRequested;
        ViewModel.PreviewStopRequested -= ViewModel_PreviewStopRequested;

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

    private readonly record struct PreviewCadenceMetrics(
        int SampleCount,
        double ObservedFps,
        double ExpectedIntervalMs,
        double AverageIntervalMs,
        double P95IntervalMs,
        double MaxIntervalMs,
        double JitterStdDevMs,
        long SlowFrameCount,
        double SlowFramePercent);

    private void TrackPreviewDisplayCadence()
    {
        var nowTick = Stopwatch.GetTimestamp();
        var previousTick = Interlocked.Exchange(ref _previewLastDisplayTick, nowTick);
        if (previousTick <= 0)
        {
            return;
        }

        var intervalMs = (nowTick - previousTick) * 1000.0 / Stopwatch.Frequency;
        if (intervalMs <= 0 || intervalMs > 5000)
        {
            return;
        }

        lock (_previewCadenceLock)
        {
            _previewDisplayIntervalWindowMs[_previewDisplayIntervalIndex] = intervalMs;
            _previewDisplayIntervalIndex = (_previewDisplayIntervalIndex + 1) % _previewDisplayIntervalWindowMs.Length;
            if (_previewDisplayIntervalCount < _previewDisplayIntervalWindowMs.Length)
            {
                _previewDisplayIntervalCount++;
            }
        }
    }

    private void ResetPreviewCadenceTracking()
    {
        Interlocked.Exchange(ref _previewLastDisplayTick, 0);
        lock (_previewCadenceLock)
        {
            Array.Clear(_previewDisplayIntervalWindowMs, 0, _previewDisplayIntervalWindowMs.Length);
            _previewDisplayIntervalCount = 0;
            _previewDisplayIntervalIndex = 0;
        }
    }

    private PreviewCadenceMetrics GetPreviewCadenceMetrics(double expectedIntervalMs)
    {
        double[] samples;
        lock (_previewCadenceLock)
        {
            if (_previewDisplayIntervalCount <= 0)
            {
                return new PreviewCadenceMetrics(
                    SampleCount: 0,
                    ObservedFps: 0,
                    ExpectedIntervalMs: expectedIntervalMs,
                    AverageIntervalMs: 0,
                    P95IntervalMs: 0,
                    MaxIntervalMs: 0,
                    JitterStdDevMs: 0,
                    SlowFrameCount: 0,
                    SlowFramePercent: 0);
            }

            samples = new double[_previewDisplayIntervalCount];
            for (var i = 0; i < _previewDisplayIntervalCount; i++)
            {
                var ringIndex = (_previewDisplayIntervalIndex - _previewDisplayIntervalCount + i + _previewDisplayIntervalWindowMs.Length)
                    % _previewDisplayIntervalWindowMs.Length;
                samples[i] = _previewDisplayIntervalWindowMs[ringIndex];
            }
        }

        var sampleCount = samples.Length;
        var sum = 0.0;
        var max = 0.0;
        for (var i = 0; i < sampleCount; i++)
        {
            sum += samples[i];
            if (samples[i] > max)
            {
                max = samples[i];
            }
        }

        var average = sum / sampleCount;
        var observedFps = average > double.Epsilon ? 1000.0 / average : 0;
        var targetIntervalMs = expectedIntervalMs > 0 ? expectedIntervalMs : average;
        var slowThresholdMs = targetIntervalMs * 1.6;

        long slowFrameCount = 0;
        var varianceSum = 0.0;
        for (var i = 0; i < sampleCount; i++)
        {
            var delta = samples[i] - average;
            varianceSum += delta * delta;
            if (samples[i] >= slowThresholdMs)
            {
                slowFrameCount++;
            }
        }

        var jitterStdDevMs = Math.Sqrt(varianceSum / sampleCount);
        var sorted = (double[])samples.Clone();
        Array.Sort(sorted);
        var p95Index = (int)Math.Ceiling((sorted.Length - 1) * 0.95);
        var p95IntervalMs = sorted[Math.Clamp(p95Index, 0, sorted.Length - 1)];
        var slowPercent = slowFrameCount <= 0
            ? 0
            : (double)slowFrameCount / Math.Max(1, sampleCount) * 100.0;

        return new PreviewCadenceMetrics(
            SampleCount: sampleCount,
            ObservedFps: observedFps,
            ExpectedIntervalMs: targetIntervalMs,
            AverageIntervalMs: average,
            P95IntervalMs: p95IntervalMs,
            MaxIntervalMs: max,
            JitterStdDevMs: jitterStdDevMs,
            SlowFrameCount: slowFrameCount,
            SlowFramePercent: slowPercent);
    }

    private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()
    {
        var nowTick = Environment.TickCount64;
        var framesArrived = Interlocked.Read(ref _previewFramesArrived);
        var framesDisplayed = Interlocked.Read(ref _previewFramesDisplayed);
        var framesDropped = Interlocked.Read(ref _previewFramesDropped);
        var sourceReaderPreviewAdapter = ViewModel.ActiveSourceReaderPreviewAdapter;
        var lastPresentedTick = Interlocked.Read(ref _previewLastPresentedTick);
        var gpuActive = _previewMediaPlayer != null;
        var gpuElementVisible = PreviewPlayerElement.Visibility == Visibility.Visible;
        var cpuElementVisible = PreviewImage.Visibility == Visibility.Visible;
        var rendererAttached = _previewMediaPlayer != null || _previewSource != null;
        var placeholderVisible = NoDevicePlaceholder.Visibility == Visibility.Visible;
        var frameReaderActive = ViewModel.IsPreviewing && (_previewSource != null || _previewGpuSignalFirstFrame);
        var rendererMode = gpuActive ? "GpuMediaSource"
            : ViewModel.IsPreviewing ? "CpuSoftwareBitmap"
            : "None";
        // GPU preview is handled entirely by MediaPlayer — no frame-level blank/stall detection
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
        var blankSuspected = !gpuActive && frameReaderActive &&
                             framesArrived > 30 &&
                             framesDisplayed == 0;
        if (!blankSuspected && startupTimedOut)
        {
            blankSuspected = true;
        }
        var stallSuspected = !gpuActive && frameReaderActive &&
                             lastPresentedTick > 0 &&
                             nowTick - lastPresentedTick > 3000;
        var cadence = GetPreviewCadenceMetrics(_previewMinPresentationIntervalMs);

        return new PreviewRuntimeSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            IsPreviewing = ViewModel.IsPreviewing,
            GpuActive = gpuActive,
            FrameReaderActive = frameReaderActive,
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
            SourceReaderAdapterFramesEnqueued = sourceReaderPreviewAdapter?.FramesEnqueued ?? 0,
            SourceReaderAdapterSamplesDelivered = sourceReaderPreviewAdapter?.SamplesDelivered ?? 0,
            SourceReaderAdapterSamplesTimedOut = sourceReaderPreviewAdapter?.SamplesTimedOut ?? 0,
            DisplayCadenceSampleCount = cadence.SampleCount,
            DisplayCadenceObservedFps = cadence.ObservedFps,
            DisplayCadenceExpectedIntervalMs = cadence.ExpectedIntervalMs,
            DisplayCadenceAverageIntervalMs = cadence.AverageIntervalMs,
            DisplayCadenceP95IntervalMs = cadence.P95IntervalMs,
            DisplayCadenceMaxIntervalMs = cadence.MaxIntervalMs,
            DisplayCadenceJitterStdDevMs = cadence.JitterStdDevMs,
            DisplayCadenceSlowFrameCount = cadence.SlowFrameCount,
            DisplayCadenceSlowFramePercent = cadence.SlowFramePercent,
            BlankSuspected = blankSuspected,
            StallSuspected = stallSuspected,
            RendererMode = rendererMode
        };
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
        // Clean up GPU preview
        var player = _previewMediaPlayer;
        _previewMediaPlayer = null;
        if (player != null)
        {
            player.MediaOpened -= PreviewMediaPlayer_MediaOpened;
            player.MediaFailed -= PreviewMediaPlayer_MediaFailed;
            player.PlaybackSession.PositionChanged -= PreviewPlaybackSession_PositionChanged;
            player.PlaybackSession.PlaybackStateChanged -= PreviewPlaybackSession_PlaybackStateChanged;
            player.Pause();
            player.Source = null;
            PreviewPlayerElement.SetMediaPlayer(null!);
            player.Dispose();
        }
        PreviewPlayerElement.Visibility = Visibility.Collapsed;
        _previewStartupExpectGpuDualSignals = false;
        _previewGpuSignalMediaOpened = false;
        _previewGpuSignalFirstFrame = false;
        _previewGpuSignalPlaybackAdvancing = false;
        _previewStartupRequiredSignals = PreviewStartupSignalFlags.None;
        _previewStartupReceivedSignals = PreviewStartupSignalFlags.None;
        _previewStartupStrategy = PreviewStartupStrategy.None;
        _previewStartupLastPlaybackPosition = TimeSpan.Zero;
        _previewStartupPositionEventCount = 0;
        _previewStartupLastPlaybackState = MediaPlaybackState.None;
        _previewStartupPlaybackPositionInitialized = false;

        // Clean up CPU preview
        PreviewImage.Source = null;
        PreviewImage.Visibility = Visibility.Collapsed;
        _previewSource = null;
        ResetPreviewCadenceTracking();
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

    private void ViewModel_PreviewStopRequested(object? sender, EventArgs e)
    {
        _previewStopRequestedByUser = true;
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
                    FadeOutElement(NoDevicePlaceholder);
                    StartPreviewStartupOverlay();
                    SetPreviewStartupState(PreviewStartupState.RendererAttaching);
                    await StartPreviewRendererAsync();
                    if (!_previewFirstVisualConfirmed)
                    {
                        SetPreviewStartupState(PreviewStartupState.WaitingForFirstVisual);
                        StartPreviewStartupWatchdog();
                    }
                    PreviewButtonIcon.Glyph = "\uE71A";
                    ToolTipService.SetToolTip(PreviewButton, "Stop Preview");
                    TrueHdrPreviewToggle.IsEnabled = !ViewModel.IsRecording && !ViewModel.IsPreviewing;
                }
                else
                {
                    StopPreviewStartupWatchdog();
                    StopPreviewStartupOverlay();
                    await StopPreviewRendererAsync();
                    FadeInElement(NoDevicePlaceholder);
                    PreviewButtonIcon.Glyph = "\uE768";
                    ToolTipService.SetToolTip(PreviewButton, "Start Preview");
                    TrueHdrPreviewToggle.IsEnabled = !ViewModel.IsRecording && !ViewModel.IsPreviewing;
                    ResetPreviewStartupTracking();
                }
                break;

            case nameof(MainViewModel.PreviewPlaybackSource):
                // GPU preview source hot-swap - reset startup tracking so the state machine monitors the new source
                if (ViewModel.IsPreviewing && ViewModel.PreviewPlaybackSource != null)
                {
                    await StopPreviewRendererAsync();
                    BeginPreviewStartupAttempt();
                    SetPreviewStartupState(PreviewStartupState.RendererAttaching);
                    await StartPreviewRendererAsync();
                    if (!_previewFirstVisualConfirmed)
                    {
                        SetPreviewStartupState(PreviewStartupState.WaitingForFirstVisual);
                        StartPreviewStartupWatchdog();
                    }
                }
                else if (ViewModel.PreviewPlaybackSource == null && !ViewModel.IsPreviewing)
                {
                    await StopPreviewRendererAsync();
                }
                break;

            case nameof(MainViewModel.IsRecording):
                RecordingIndicator.Visibility = ViewModel.IsRecording ? Visibility.Visible : Visibility.Collapsed;
                // Toggle record button content between normal and recording states
                RecordButtonNormalContent.Visibility = ViewModel.IsRecording ? Visibility.Collapsed : Visibility.Visible;
                RecordButtonRecordingContent.Visibility = ViewModel.IsRecording ? Visibility.Visible : Visibility.Collapsed;
                AudioRecordToggle.IsEnabled = !ViewModel.IsRecording;
                CustomAudioToggle.IsEnabled = !ViewModel.IsRecording;
                AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled && !ViewModel.IsRecording;
                HdrToggle.IsEnabled = ViewModel.IsHdrAvailable && !ViewModel.IsRecording;
                TrueHdrPreviewToggle.IsEnabled = !ViewModel.IsRecording && !ViewModel.IsPreviewing;
                RecordingStatsPanel.Visibility = ViewModel.IsRecording ? Visibility.Visible : Visibility.Collapsed;
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
                UpdateAudioMeterLevel(ViewModel.AudioPeak);
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

            case nameof(MainViewModel.AvailableResolutions):
                ResolutionComboBox.ItemsSource = ViewModel.AvailableResolutions;
                EnsureResolutionSelection();
                break;

            case nameof(MainViewModel.AvailableFrameRates):
                FrameRateComboBox.ItemsSource = ViewModel.AvailableFrameRates;
                EnsureFrameRateSelection();
                break;

            case nameof(MainViewModel.IsHdrAvailable):
                HdrToggle.IsEnabled = ViewModel.IsHdrAvailable && !ViewModel.IsRecording;
                break;

            case nameof(MainViewModel.IsHdrEnabled):
                if (HdrToggle.IsChecked != ViewModel.IsHdrEnabled)
                {
                    HdrToggle.IsChecked = ViewModel.IsHdrEnabled;
                }
                break;

            case nameof(MainViewModel.IsTrueHdrPreviewEnabled):
                if (TrueHdrPreviewToggle.IsChecked != ViewModel.IsTrueHdrPreviewEnabled)
                {
                    TrueHdrPreviewToggle.IsChecked = ViewModel.IsTrueHdrPreviewEnabled;
                }
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
                break;

            case nameof(MainViewModel.IsAudioPreviewEnabled):
                if (AudioPreviewToggle.IsChecked != ViewModel.IsAudioPreviewEnabled)
                {
                    AudioPreviewToggle.IsChecked = ViewModel.IsAudioPreviewEnabled;
                }
                break;

            case nameof(MainViewModel.IsRecordingTransitioning):
                RecordButton.IsEnabled = !ViewModel.IsRecordingTransitioning;
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
        _previewLastLogTick = 0;
        _previewLastResizeLogTick = 0;
        _previewLastPresentedTick = 0;
        _previewResizeSuppressUntilTick = 0;
        _previewUiInFlight = 0;
        ResetPreviewCadenceTracking();
        _previewMinPresentationIntervalMs = ResolvePreviewExpectedIntervalMs();

        var playbackSource = ViewModel.PreviewPlaybackSource;
        if (playbackSource != null)
        {
            // GPU preview path: MediaPlayer -> MediaPlayerElement
            var player = new MediaPlayer();
            player.MediaOpened += PreviewMediaPlayer_MediaOpened;
            player.MediaFailed += PreviewMediaPlayer_MediaFailed;
            player.PlaybackSession.PositionChanged += PreviewPlaybackSession_PositionChanged;
            player.PlaybackSession.PlaybackStateChanged += PreviewPlaybackSession_PlaybackStateChanged;
            player.IsVideoFrameServerEnabled = false;
            player.AutoPlay = true;
            player.Source = playbackSource;
            _previewMediaPlayer = player;
            _previewStartupExpectGpuDualSignals = true;
            // MediaPlayer.MediaOpened does not fire reliably for live MediaFrameSource-backed
            // MediaSource objects (confirmed absent from logs across multiple runs). PlaybackAdvancing
            // (position moving) is a strictly stronger signal — if position advances the media is
            // provably open and playing. MediaOpened handler is kept for telemetry only.
            ConfigurePreviewStartupSignals(
                PreviewStartupStrategy.GpuMediaSourceNoFrameReader,
                PreviewStartupSignalFlags.PlaybackAdvancing);
            _previewRendererAttachedUtc = DateTimeOffset.UtcNow;
            PreviewPlayerElement.SetMediaPlayer(player);
            PreviewPlayerElement.Visibility = Visibility.Visible;
            PreviewImage.Visibility = Visibility.Collapsed;
            Logger.Log("Preview renderer started (mode=GpuMediaSource).");
            Logger.Log($"PREVIEW_RENDERER_ATTACHED mode=GpuMediaSource attempt={_previewStartupAttemptId ?? "none"}");
            EnsurePreviewPlaybackStarted("RendererAttach", recoveryAttempt: false);
        }
        else
        {
            // Fallback CPU preview path: SoftwareBitmapSource -> Image
            _previewStartupExpectGpuDualSignals = false;
            ConfigurePreviewStartupSignals(PreviewStartupStrategy.CpuSoftwareBitmap, PreviewStartupSignalFlags.FirstVisual);
            _previewSource = new SoftwareBitmapSource();
            _previewRendererAttachedUtc = DateTimeOffset.UtcNow;
            PreviewImage.Source = _previewSource;
            PreviewImage.Visibility = Visibility.Visible;
            PreviewPlayerElement.Visibility = Visibility.Collapsed;
            Logger.Log($"Preview renderer started (mode=CpuSoftwareBitmap, expectedIntervalMs={_previewMinPresentationIntervalMs}).");
            Logger.Log($"PREVIEW_RENDERER_ATTACHED mode=CpuSoftwareBitmap attempt={_previewStartupAttemptId ?? "none"}");
        }

        return Task.CompletedTask;
    }

    private Task StopPreviewRendererAsync()
    {
        // Clean up GPU preview path
        var player = _previewMediaPlayer;
        _previewMediaPlayer = null;
        if (player != null)
        {
            player.MediaOpened -= PreviewMediaPlayer_MediaOpened;
            player.MediaFailed -= PreviewMediaPlayer_MediaFailed;
            player.PlaybackSession.PositionChanged -= PreviewPlaybackSession_PositionChanged;
            player.PlaybackSession.PlaybackStateChanged -= PreviewPlaybackSession_PlaybackStateChanged;
            player.Pause();
            player.Source = null;
            PreviewPlayerElement.SetMediaPlayer(null!);
            player.Dispose();
        }
        PreviewPlayerElement.Visibility = Visibility.Collapsed;
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
        _previewResizeSuppressUntilTick = 0;
        _previewUiInFlight = 0;
        ResetPreviewCadenceTracking();
        _previewMinPresentationIntervalMs = Math.Max(1L, (long)Math.Round(1000.0 / 60.0));
        Logger.Log("Preview renderer stopped.");
        return Task.CompletedTask;
    }

    private async void ViewModel_PreviewFrameReady(object? sender, PreviewFrame frame)
    {
        Interlocked.Increment(ref _previewFramesArrived);
        if (_previewStartupExpectGpuDualSignals &&
            !_previewGpuSignalFirstFrame &&
            IsPreviewStartupSignalWindowActive())
        {
            _dispatcherQueue.TryEnqueue(MarkGpuStartupSignalFirstFrame);
        }

        if (_previewSource == null)
        {
            return;
        }

        var nowTick = Environment.TickCount64;
        var resizeSuppressUntil = Interlocked.Read(ref _previewResizeSuppressUntilTick);
        if (nowTick < resizeSuppressUntil)
        {
            Interlocked.Increment(ref _previewFramesDropped);
            return;
        }

        if (Interlocked.CompareExchange(ref _previewUiInFlight, 1, 0) != 0)
        {
            Interlocked.Increment(ref _previewFramesDropped);
            MaybeLogPreviewStats(nowTick, queueDelayMs: -1, setMs: -1);
            return;
        }

        SoftwareBitmap? bitmap = null;
        try
        {
            bitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, (int)frame.Width, (int)frame.Height, BitmapAlphaMode.Premultiplied);
            bitmap.CopyFromBuffer(frame.Buffer.AsBuffer());
        }
        catch (Exception ex)
        {
            Logger.Log($"Preview frame conversion failed: {ex.Message}");
            Interlocked.Increment(ref _previewFramesDropped);
            bitmap?.Dispose();
            Interlocked.Exchange(ref _previewUiInFlight, 0);
            return;
        }

        var enqueueTick = Environment.TickCount64;
        var enqueued = _dispatcherQueue.TryEnqueue(async () =>
        {
            var uiStartTick = Environment.TickCount64;
            var queueDelayMs = uiStartTick - enqueueTick;
            var setStopwatch = Stopwatch.StartNew();
            try
            {
                if (_previewSource != null)
                {
                    await _previewSource.SetBitmapAsync(bitmap);
                    Interlocked.Increment(ref _previewFramesDisplayed);
                    Interlocked.Exchange(ref _previewLastPresentedTick, uiStartTick);
                    TrackPreviewDisplayCadence();
                    ConfirmPreviewFirstVisual("CpuSoftwareBitmap");
                }
                else
                {
                    Interlocked.Increment(ref _previewFramesDropped);
                }
            }
            catch (Exception ex)
            {
                if (ex is not TaskCanceledException && ex is not OperationCanceledException)
                {
                    Logger.Log($"Preview render failed: {ex.Message}");
                }

                Interlocked.Increment(ref _previewFramesDropped);
            }
            finally
            {
                setStopwatch.Stop();
                Interlocked.Exchange(ref _previewUiInFlight, 0);
                MaybeLogPreviewStats(uiStartTick, queueDelayMs, (long)setStopwatch.ElapsedMilliseconds);
                bitmap.Dispose();
            }
        });

        if (!enqueued)
        {
            Interlocked.Increment(ref _previewFramesDropped);
            Interlocked.Exchange(ref _previewUiInFlight, 0);
            bitmap.Dispose();
        }
    }

    private void MaybeLogPreviewStats(long nowTick, long queueDelayMs, long setMs)
    {
        var inFlightNow = Volatile.Read(ref _previewUiInFlight);
        var issue = inFlightNow > 1 || queueDelayMs >= 50 || setMs >= 50;
        if (!issue)
        {
            return;
        }

        var last = Interlocked.Read(ref _previewLastLogTick);
        if (nowTick - last < 1000)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _previewLastLogTick, nowTick, last) != last)
        {
            return;
        }

        var arrived = Interlocked.Read(ref _previewFramesArrived);
        var displayed = Interlocked.Read(ref _previewFramesDisplayed);
        var dropped = Interlocked.Read(ref _previewFramesDropped);
        var queueDelayText = queueDelayMs >= 0 ? $"{queueDelayMs}ms" : "n/a";
        var setText = setMs >= 0 ? $"{setMs}ms" : "n/a";
        Logger.Log($"Preview UI stall: inFlight={inFlightNow} queueDelay={queueDelayText} setMs={setText} arrived={arrived} displayed={displayed} dropped={dropped}");
    }

    private void UpdateAudioMeterLevel(double level)
    {
        var clamped = Math.Clamp(level, 0.0, 1.0);
        var trackWidth = AudioMeterTrack.ActualWidth;
        if (trackWidth <= 0) return;
        AudioMeterClip.Rect = new Windows.Foundation.Rect(0, 0, trackWidth * clamped, 8);
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
            if (ViewModel.IsPreviewing)
            {
                _previewStopRequestedByUser = true;
                await ViewModel.StopPreviewAsync();
            }
            else
            {
                _previewStopRequestedByUser = false;
                await ViewModel.StartPreviewAsync();
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
                var gpuActive = _previewMediaPlayer != null && PreviewPlayerElement.Visibility == Visibility.Visible;
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
