using Microsoft.UI.Xaml;
using System.Threading.Tasks;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing preview renderer host adapter. PreviewRendererHostController owns
// the D3D renderer, CPU fallback source, frame counters, and public/reinit
// disposal path; MainWindow supplies UI elements and startup side-effect adapters.
public sealed partial class MainWindow
{
    private PreviewRendererHostController _previewRendererHostController = null!;
    private PreviewResizeTelemetryController _previewResizeTelemetryController = null!;

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

    private void InitializePreviewResizeTelemetryController()
    {
        _previewResizeTelemetryController = new PreviewResizeTelemetryController();
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _previewResizeTelemetryController.HandleSizeChanged(
            ViewModel.IsPreviewing,
            _previewRendererHostController.HasD3DRenderer,
            PreviewSwapChainPanel.Visibility);
    }

    private void ResetPreviewResizeTelemetry()
        => _previewResizeTelemetryController.Reset();

    private Task StartPreviewRendererAsync()
        => _previewRendererHostController.StartAsync();

    private Task StopPreviewRendererAsync()
        => _previewRendererHostController.StopAsync();

    private void StopPreviewForShutdown()
        => _previewRendererHostController.StopForShutdown();

    public long RendererReinitUnsafeWindows
        => _previewRendererHostController.RendererReinitUnsafeWindows;

    private void DisposeD3DPreviewRendererForReinit()
        => _previewRendererHostController.DisposeD3DPreviewRendererForReinit();
}
