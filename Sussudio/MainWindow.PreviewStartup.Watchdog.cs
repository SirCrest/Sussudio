using System;
using Sussudio.Controllers;
using Sussudio.Models;

namespace Sussudio;

// XAML-facing preview startup watchdog adapter.
public sealed partial class MainWindow
{
    private PreviewStartupWatchdogController _previewStartupWatchdogController = null!;

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