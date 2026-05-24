using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Sussudio.Controllers;
using Sussudio.Models;

namespace Sussudio;

public sealed partial class MainWindow
{
    private PreviewRendererHostController _previewRendererHostController = null!;
    private PreviewResizeTelemetryController _previewResizeTelemetryController = null!;
    private PreviewRuntimeSnapshotSamplingController _previewRuntimeSnapshotSamplingController = null!;
    private PreviewSurfacePresentationController _previewSurfacePresentationController = null!;
    private PreviewSurfaceShadowController _previewSurfaceShadowController = null!;

    // XAML-facing preview surface adapter. PreviewSurfacePresentationController
    // owns content-fit sizing and panel visibility; PreviewSurfaceShadowController
    // owns composition shadows around the video frame and control bar.
    private void InitializePreviewSurfacePresentationController()
    {
        _previewSurfaceShadowController = new PreviewSurfaceShadowController(new PreviewSurfaceShadowControllerContext
        {
            VideoShadowHost = VideoShadowHost,
            ControlBarShadowHost = ControlBarShadowHost,
            ControlBarBorder = ControlBarBorder,
        });

        _previewSurfacePresentationController = new PreviewSurfacePresentationController(
            new PreviewSurfacePresentationControllerContext
            {
                GetPreviewSwapChainPanel = () => PreviewSwapChainPanel,
                PreviewContentGrid = PreviewContentGrid,
                RecordingGlowBorder = RecordingGlowBorder,
            },
            _previewSurfaceShadowController);
    }

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

    private void InitializePreviewRuntimeSnapshotSamplingController()
    {
        _previewRuntimeSnapshotSamplingController = new PreviewRuntimeSnapshotSamplingController(new PreviewRuntimeSnapshotSamplingControllerContext
        {
            UiDispatchController = WindowUiDispatchController,
            ViewModel = ViewModel,
            RendererHostController = _previewRendererHostController,
            StartupSessionController = _previewStartupSessionController,
            StartupSignalCoordinator = _previewStartupSignalCoordinator,
            IsGpuElementVisible = () => PreviewSwapChainPanel.Visibility == Visibility.Visible,
            IsCpuElementVisible = () => PreviewImage.Visibility == Visibility.Visible,
            IsPlaceholderVisible = () => NoDevicePlaceholder.Visibility == Visibility.Visible,
            GetStartupVisualTimeoutMs = () => PreviewStartupVisualTimeoutMs
        });
    }

    private Task StartPreviewRendererAsync()
        => _previewRendererHostController.StartAsync();

    private Task StopPreviewRendererAsync()
        => _previewRendererHostController.StopAsync();

    private void StopPreviewForShutdown()
        => _previewRendererHostController.StopForShutdown();

    public long RendererReinitUnsafeWindows
        => _previewRendererHostController.RendererReinitUnsafeWindows;

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _previewResizeTelemetryController.HandleSizeChanged(
            ViewModel.IsPreviewing,
            _previewRendererHostController.HasD3DRenderer,
            PreviewSwapChainPanel.Visibility);
    }

    private void OnPreviewSwapChainPanelSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Composition transform only - overlay sizing is driven by the container.
        var scale = PreviewSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;
        _previewRendererHostController.OnPanelSizeChanged(e.NewSize.Width, e.NewSize.Height, scale);
    }

    private void OnPreviewContentGridSizeChanged(object sender, SizeChangedEventArgs e)
        => UpdateVideoContentOverlays();

    private void UpdateVideoContentOverlays()
        => _previewSurfacePresentationController.UpdateVideoContentOverlays(ViewModel.SourceWidth, ViewModel.SourceHeight);

    private void SetupVideoFrameShadow()
        => _previewSurfaceShadowController.SetupVideoFrameShadow();

    private void SetupControlBarShadow()
        => _previewSurfaceShadowController.SetupControlBarShadow();

    private void SetGpuPreviewVisibility(Visibility visibility)
        => _previewSurfacePresentationController.SetGpuPreviewVisibility(visibility);

    private void ClearVideoFrameShadow()
        => _previewSurfaceShadowController.ClearVideoFrameShadow();

    private void FadeInVideoFrameShadow(int delayMs, int durationMs)
        => _previewSurfaceShadowController.FadeInVideoFrameShadow(delayMs, durationMs);

    private void FadeOutVideoFrameShadow(int durationMs)
        => _previewSurfaceShadowController.FadeOutVideoFrameShadow(durationMs);

    private void FadeInControlBarShadow(int delayMs, int durationMs)
        => _previewSurfaceShadowController.FadeInControlBarShadow(delayMs, durationMs);

    private void ResetPreviewResizeTelemetry()
        => _previewResizeTelemetryController.Reset();

    private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync(CancellationToken cancellationToken = default)
        => await _previewRuntimeSnapshotSamplingController.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
}
