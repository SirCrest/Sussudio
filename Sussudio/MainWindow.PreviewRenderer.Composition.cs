using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private PreviewRendererHostController _previewRendererHostController = null!;

    private void InitializePreviewRendererHostController()
    {
        _previewRendererHostController = new PreviewRendererHostController(new PreviewRendererHostControllerContext
        {
            ViewModel = ViewModel,
            DispatcherQueue = _dispatcherQueue,
            GetPreviewSwapChainPanel = () => PreviewSwapChainPanel,
            SetPreviewSwapChainPanel = panel => PreviewSwapChainPanel = panel,
            PreviewContentGrid = PreviewContentGrid,
            PreviewImage = PreviewImage,
            PreviewContentGridSizeChangedHandler = OnPreviewContentGridSizeChanged,
            PreviewSwapChainPanelSizeChangedHandler = OnPreviewSwapChainPanelSizeChanged,
            IsPreviewReinitAnimating = () => IsPreviewReinitAnimating,
            ClearPreviewReinitAnimatingForShutdown = () =>
            {
                _previewReinitTransitionController.Clear(nameof(StopPreviewForShutdown));
            },
            GetPreviewStartupAttemptLabel = () => PreviewStartupAttemptLabel,
            IsPreviewFirstVisualConfirmed = () => IsPreviewFirstVisualConfirmed,
            ConfirmPreviewFirstVisual = ConfirmPreviewFirstVisual,
            MarkStartupFailed = reason => SetPreviewStartupState(PreviewStartupState.Failed, reason),
            StopPreviewStartupWatchdog = StopPreviewStartupWatchdog,
            RevealPreviewUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,
            SchedulePreviewStartupFailureStop = SchedulePreviewStartupFailureStop,
            ClearVideoFrameShadow = ClearVideoFrameShadow,
            SetupVideoFrameShadow = SetupVideoFrameShadow,
            SetGpuPreviewVisibility = SetGpuPreviewVisibility,
            ResetPreviewSignalState = ResetPreviewSignalState,
            ResetPreviewResizeTelemetry = ResetPreviewResizeTelemetry,
            StopPreviewFadeInTimer = StopPreviewFadeInTimer,
            ResetPreviewContentTransform = ResetPreviewContentTransform,
            UpdateVideoContentOverlays = UpdateVideoContentOverlays,
            MarkPreviewRendererAttached = MarkPreviewRendererAttached,
            ConfigurePreviewStartupSignals = ConfigurePreviewStartupSignals,
            Log = message => Logger.Log(message)
        });
    }
}
