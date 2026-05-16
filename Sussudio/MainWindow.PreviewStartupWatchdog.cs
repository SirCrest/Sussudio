using System;
using System.Threading.Tasks;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing preview startup watchdog adapter. Timer and failure-stop state
// live in PreviewStartupWatchdogController; startup state and readiness signals
// remain in their focused owners.
public sealed partial class MainWindow
{
    private PreviewStartupWatchdogController _previewStartupWatchdogController = null!;

    private int PreviewStartupVisualTimeoutMs => _previewStartupWatchdogController.VisualTimeoutMs;

    private void InitializePreviewStartupWatchdogController()
        => _previewStartupWatchdogController = new PreviewStartupWatchdogController(new PreviewStartupWatchdogControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            IsWaitingForFirstVisual = () => CurrentPreviewStartupState == PreviewStartupState.WaitingForFirstVisual,
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
            BuildTimeoutDiagnosticPayload = BuildPreviewStartupTimeoutDiagnosticPayload,
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

    private string BuildPreviewStartupTimeoutDiagnosticPayload()
        => $"placeholder={NoDevicePlaceholder.Visibility} " +
            $"gpuVisible={PreviewSwapChainPanel.Visibility} cpuVisible={PreviewImage.Visibility} " +
            $"strategy={_previewStartupStrategy} required={PreviewStartupSignalFormatter.FormatSignalList(_previewStartupRequiredSignals)} " +
            $"received={PreviewStartupSignalFormatter.FormatSignalList(_previewStartupReceivedSignals)} " +
            $"missing={PreviewStartupMissingSignals ?? "-"}";
}
