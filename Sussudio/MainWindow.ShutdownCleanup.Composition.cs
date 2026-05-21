using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

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
}
