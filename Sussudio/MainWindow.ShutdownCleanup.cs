using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// Post-close shutdown cleanup. Recording finalization side effects live in
// WindowCloseRecordingFinalizationController; close state/completion lives in
// WindowCloseLifecycleController.
public sealed partial class MainWindow
{
    private WindowShutdownCleanupController _windowShutdownCleanupController = null!;

    private void InitializeWindowShutdownCleanupController()
    {
        _windowShutdownCleanupController = new WindowShutdownCleanupController(new WindowShutdownCleanupControllerContext
        {
            LifecycleController = _windowCloseLifecycleController,
            IsRecording = () => ViewModel.IsRecording,
            IsPreviewing = () => ViewModel.IsPreviewing,
            CancelNativeShellRevealAfterFirstFrame = CancelNativeShellRevealAfterFirstFrame,
            CompleteWindowCloseRequest = () => CompleteWindowCloseRequest(),
            DetachMeterActivationHandlers = DetachMeterActivationHandlers,
            StopTimers = StopShutdownTimers,
            StopStatsOverlay = StopStatsOverlayForShutdown,
            StopRecordingVisuals = StopRecordingVisualsForShutdown,
            DetachMainContentSizeChanged = DetachMainContentSizeChanged,
            DetachViewModelEventHandlers = DetachViewModelEventHandlers,
            StopPreviewForShutdown = StopPreviewForShutdown,
            ResetPreviewStartupTracking = () => ResetPreviewStartupTracking(),
            StopRecordingAfterClosedBestEffortAsync = () => _windowCloseRecordingFinalizationController.StopAfterClosedBestEffortAsync(
                ViewModel,
                Content as FrameworkElement),
            DisposeAutomationHostAsync = () => _automationHostLifecycleController.DisposeAsync(),
            DisposeNvmlMonitor = () => _nvmlMonitor?.Dispose(),
            DisposeViewModelAsync = ViewModel.DisposeAsync
        });
    }

    private async void MainWindow_Closed(object sender, WindowEventArgs args)
        => await _windowShutdownCleanupController.RunAsync();

    private void DetachMeterActivationHandlers()
    {
        ViewModel.AudioMeterActivated -= EnsureAudioMeterTimerRunning;
        ViewModel.MicrophoneMeterActivated -= EnsureAudioMeterTimerRunning;
    }

    private void StopShutdownTimers()
    {
        StopAudioMeterTimer();
        StopLiveSignalInfoTimers();
        StopFullScreenAutoHideTimer();
        StopFlashbackStatusPolling();
    }

    private void StopStatsOverlayForShutdown()
    {
        DetachStatsOverlayToggleBindings();
        StopStatsDockPolling();
        HideStatsDockPanel(immediate: true);
    }

    private void StopRecordingVisualsForShutdown()
    {
        StopMicMeterRowAnimation();
        RecordingGlowPulseStoryboard.Stop();
        RecordingGlowBorder.Opacity = 0;
        RecPulseStoryboard.Stop();
    }

    private void DetachMainContentSizeChanged()
    {
        if (Content is FrameworkElement mainContent)
        {
            mainContent.SizeChanged -= MainWindow_SizeChanged;
        }
    }

    private void DetachViewModelEventHandlers()
    {
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.PreviewStartRequested -= ViewModel_PreviewStartRequested;
        ViewModel.PreviewStopRequested -= ViewModel_PreviewStopRequested;
        ViewModel.PreviewReinitRequested -= ViewModel_PreviewReinitRequested;
        ViewModel.PreviewRendererStopRequested -= ViewModel_PreviewRendererStopRequested;
    }
}
