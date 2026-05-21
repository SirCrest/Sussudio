using System;
using System.Threading.Tasks;
using Sussudio.Controllers;
using Sussudio.Models;

namespace Sussudio;

// XAML-facing adapter for preview startup orchestration. It supplies UI/runtime
// callbacks while focused controllers own session transitions, watchdog timers,
// readiness signals, and first-visual confirmation decisions.
public sealed partial class MainWindow
{
    private PreviewStartupSessionController _previewStartupSessionController = null!;
    private PreviewStartupSignalCoordinator _previewStartupSignalCoordinator = null!;
    private PreviewStartupWatchdogController _previewStartupWatchdogController = null!;

    private void InitializePreviewStartupSessionController()
        => _previewStartupSessionController = new PreviewStartupSessionController(new PreviewStartupSessionControllerContext
        {
            IsPreviewing = () => ViewModel.IsPreviewing,
            IsPreviewStopRequestedByUser = () => IsPreviewStopRequestedByUser,
            GetSelectedDeviceName = () => ViewModel.SelectedDevice?.Name,
            ResetSignalState = ResetPreviewSignalState,
            ResetFailureStopSchedule = ResetPreviewStartupFailureStopSchedule,
            MarkFirstVisualSignalConfirmed = MarkPreviewStartupFirstVisualConfirmed,
            StopWatchdog = StopPreviewStartupWatchdog,
            StopOverlay = StopPreviewStartupOverlay,
            StopFadeInTimer = StopPreviewFadeInTimer,
            ScheduleFadeIn = SchedulePreviewFadeIn,
            CompleteFirstVisualTransition = (attemptLabel, callerName) =>
                _previewReinitTransitionController.CompleteFirstVisualTransition(attemptLabel, callerName),
            ClearReinitTransitionForStartupReset = (preserveReinitAnimation, callerName) =>
                _previewReinitTransitionController.ClearForStartupReset(preserveReinitAnimation, callerName),
            Log = message => Logger.Log(message),
            CreateAttemptId = () => Guid.NewGuid().ToString("N"),
            GetUtcNow = () => DateTimeOffset.UtcNow
        });

    private PreviewStartupState CurrentPreviewStartupState
        => _previewStartupSessionController.State;

    private string PreviewStartupAttemptLabel
        => _previewStartupSessionController.AttemptLabel;

    private string? PreviewStartupAttemptId
        => _previewStartupSessionController.AttemptId;

    private DateTimeOffset? PreviewStartupRequestedUtc
        => _previewStartupSessionController.RequestedUtc;

    private string? PreviewStartupMissingSignals
    {
        get => _previewStartupSessionController.MissingSignals;
        set => _previewStartupSessionController.SetMissingSignals(value);
    }

    private int PreviewStartupRecoveryAttemptCount
        => _previewStartupSessionController.RecoveryAttemptCount;

    private string? PreviewStartupLastFailureReason
        => _previewStartupSessionController.LastFailureReason;

    private bool IsPreviewFirstVisualConfirmed
        => _previewStartupSessionController.FirstVisualConfirmed;

    private bool ShouldBeginPreviewStartupAttempt
        => _previewStartupSessionController.ShouldBeginAttempt;

    private void SetPreviewStartupState(PreviewStartupState state, string? reason = null)
        => _previewStartupSessionController.SetStartupState(state, reason);

    private void MarkPreviewRendererAttached()
        => _previewStartupSessionController.MarkRendererAttached(DateTimeOffset.UtcNow);

    private void BeginPreviewStartupAttempt()
        => _previewStartupSessionController.BeginStartupAttempt();

    private void ConfirmPreviewFirstVisual(string source)
        => _previewStartupSessionController.ConfirmFirstVisual(source);

    private void ResetPreviewStartupTracking(bool keepRecoveryCount = false, bool preserveReinitAnimation = false)
        => _previewStartupSessionController.ResetStartupTracking(keepRecoveryCount, preserveReinitAnimation);

    // XAML-facing preview startup signal adapter.
    private void InitializePreviewStartupSignalCoordinator()
        => _previewStartupSignalCoordinator = new PreviewStartupSignalCoordinator(new PreviewStartupSignalCoordinatorContext
        {
            IsSignalWindowActive = IsPreviewStartupSignalWindowActive,
            IsFirstVisualConfirmed = () => IsPreviewFirstVisualConfirmed,
            GetAttemptLabel = () => PreviewStartupAttemptLabel,
            SetMissingSignals = value => PreviewStartupMissingSignals = value,
            Log = message => Logger.Log(message),
            ConfirmFirstVisual = ConfirmPreviewFirstVisual,
            GetPlaybackSnapshotState = GetPreviewStartupPlaybackSnapshotState
        });

    private PreviewStartupReadinessSignalSnapshot PreviewStartupSignalSnapshot
        => _previewStartupSignalCoordinator.Snapshot;

    private bool _previewGpuSignalMediaOpened => PreviewStartupSignalSnapshot.GpuSignalMediaOpened;
    private bool _previewGpuSignalFirstFrame => PreviewStartupSignalSnapshot.GpuSignalFirstFrame;
    private bool _previewGpuSignalPlaybackAdvancing => PreviewStartupSignalSnapshot.GpuSignalPlaybackAdvancing;
    private PreviewStartupSignalFlags _previewStartupRequiredSignals => PreviewStartupSignalSnapshot.RequiredSignals;
    private PreviewStartupSignalFlags _previewStartupReceivedSignals => PreviewStartupSignalSnapshot.ReceivedSignals;
    private PreviewStartupStrategy _previewStartupStrategy => PreviewStartupSignalSnapshot.Strategy;
    private long PreviewStartupGpuPositionEventCount => _previewStartupSignalCoordinator.PositionEventCount;

    private bool IsPreviewStartupSignalWindowActive()
        => _previewStartupSessionController.IsSignalWindowActive(ViewModel.IsPreviewing);

    private void ResetPreviewSignalState()
        => _previewStartupSignalCoordinator.Reset();

    private void ConfigurePreviewStartupSignals(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)
        => _previewStartupSignalCoordinator.Configure(strategy, requiredSignals);

    private string BuildPreviewStartupMissingSignals()
        => _previewStartupSignalCoordinator.BuildMissingSignals();

    private void MarkPreviewStartupFirstVisualConfirmed()
        => _previewStartupSignalCoordinator.MarkFirstVisualConfirmed();

    private void MarkGpuStartupSignal(PreviewStartupSignalFlags signal, string signalName)
        => _previewStartupSignalCoordinator.MarkGpuStartupSignal(signal, signalName);

    private void MarkGpuStartupSignalMediaOpened()
        => MarkGpuStartupSignal(PreviewStartupSignalFlags.MediaOpened, "MediaOpened");

    private void MarkGpuStartupSignalFirstFrame()
        => _previewStartupSignalCoordinator.MarkGpuStartupSignalFirstFrame();

    private void MarkGpuStartupSignalPlaybackAdvancing(TimeSpan position)
        => _previewStartupSignalCoordinator.MarkGpuStartupSignalPlaybackAdvancing(position);

    private void LogPreviewStartupPlaybackSnapshot(string reason)
        => _previewStartupSignalCoordinator.LogPlaybackSnapshot(reason);

    private PreviewStartupPlaybackSnapshotState GetPreviewStartupPlaybackSnapshotState()
    {
        var renderer = _previewRendererHostController.Renderer;
        return new PreviewStartupPlaybackSnapshotState(
            renderer != null,
            renderer?.IsRendering == true,
            PreviewSwapChainPanel.Visibility.ToString());
    }

    private int PreviewStartupVisualTimeoutMs => _previewStartupWatchdogController.VisualTimeoutMs;

    private void InitializePreviewStartupWatchdogController()
        => _previewStartupWatchdogController = new PreviewStartupWatchdogController(new PreviewStartupWatchdogControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            IsWaitingForFirstVisual = () => _previewStartupSessionController.IsWaitingForFirstVisual,
            IsSignalWindowActive = IsPreviewStartupSignalWindowActive,
            IsWindowClosing = () => _isWindowClosing,
            IsPreviewStopRequestedByUser = () => IsPreviewStopRequestedByUser,
            IsPreviewing = () => ViewModel.IsPreviewing,
            GetElapsedMilliseconds = () => _previewStartupSessionController.GetElapsedMilliseconds(DateTimeOffset.UtcNow),
            GetAttemptLabel = () => PreviewStartupAttemptLabel,
            BuildMissingSignals = BuildPreviewStartupMissingSignals,
            GetMissingSignals = () => PreviewStartupMissingSignals,
            SetMissingSignals = value => PreviewStartupMissingSignals = value,
            MarkStartupFailed = reason => SetPreviewStartupState(PreviewStartupState.Failed, reason),
            GetTimeoutDiagnosticSnapshot = GetPreviewStartupTimeoutDiagnosticSnapshot,
            LogPlaybackSnapshot = LogPreviewStartupPlaybackSnapshot,
            StopStartupOverlay = StopPreviewStartupOverlay,
            SetStatusText = value => ViewModel.StatusText = value,
            StopPreviewForFailureAsync = _ => ViewModel.StopPreviewAsync(userInitiated: true, teardownPipeline: true),
            RunUiEventHandlerAsync = RunUiEventHandlerAsync
        });

    private void StopPreviewStartupWatchdog()
        => _previewStartupWatchdogController.Stop();

    private void StartPreviewStartupWatchdog()
        => _previewStartupWatchdogController.Start();

    private void SchedulePreviewStartupFailureStop(string reason)
        => _previewStartupWatchdogController.ScheduleFailureStop(reason);

    private void ResetPreviewStartupFailureStopSchedule()
        => _previewStartupWatchdogController.ResetFailureStopSchedule();

    private PreviewStartupTimeoutDiagnosticSnapshot GetPreviewStartupTimeoutDiagnosticSnapshot()
        => new(
            NoDevicePlaceholder.Visibility.ToString(),
            PreviewSwapChainPanel.Visibility.ToString(),
            PreviewImage.Visibility.ToString(),
            _previewStartupStrategy,
            _previewStartupRequiredSignals,
            _previewStartupReceivedSignals,
            PreviewStartupMissingSignals);
}
