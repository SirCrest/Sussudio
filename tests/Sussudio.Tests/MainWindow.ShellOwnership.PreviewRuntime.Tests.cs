using System.Threading.Tasks;

static partial class Program
{
    private static Task PreviewResizeTelemetry_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var windowSizingText = ReadRepoFile("Sussudio/MainWindow.WindowSizing.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/PreviewResizeTelemetryController.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var previewRendererText = ReadRepoFile("Sussudio/MainWindow.PreviewRenderer.cs").Replace("\r\n", "\n");

        AssertContains(windowSizingText, "private PreviewResizeTelemetryController _previewResizeTelemetryController = null!;");
        AssertContains(windowSizingText, "private void InitializePreviewResizeTelemetryController()");
        AssertContains(windowSizingText, "private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)");
        AssertContains(windowSizingText, "_previewResizeTelemetryController.HandleSizeChanged(");
        AssertContains(windowSizingText, "ViewModel.IsPreviewing,");
        AssertContains(windowSizingText, "_d3dRenderer != null,");
        AssertContains(windowSizingText, "PreviewSwapChainPanel.Visibility);");
        AssertContains(windowSizingText, "private void ResetPreviewResizeTelemetry()");
        AssertContains(windowSizingText, "=> _previewResizeTelemetryController.Reset();");
        AssertContains(mainWindowText, "InitializePreviewResizeTelemetryController();");
        AssertContains(mainWindowText, "mainContent.SizeChanged += MainWindow_SizeChanged;");
        AssertContains(shutdownCleanupText, "mainContent.SizeChanged -= MainWindow_SizeChanged;");
        AssertContains(previewRendererText, "ResetPreviewResizeTelemetry();");
        AssertContains(controllerText, "internal sealed class PreviewResizeTelemetryController");
        AssertContains(controllerText, "private long _previewLastResizeLogTick;");
        AssertContains(controllerText, "public void HandleSizeChanged(bool isPreviewing, bool hasD3dRenderer, Visibility previewVisibility)");
        AssertContains(controllerText, "if (!isPreviewing ||");
        AssertContains(controllerText, "!hasD3dRenderer ||");
        AssertContains(controllerText, "previewVisibility != Visibility.Visible");
        AssertContains(controllerText, "Interlocked.Read(ref _previewLastResizeLogTick)");
        AssertContains(controllerText, "Interlocked.CompareExchange(ref _previewLastResizeLogTick, nowTick, lastLogTick)");
        AssertContains(controllerText, "Preview resize active. Updating compositor transform without resizing swap-chain buffers.");
        AssertContains(controllerText, "public void Reset()");
        AssertContains(controllerText, "Interlocked.Exchange(ref _previewLastResizeLogTick, 0);");
        AssertDoesNotContain(mainWindowText, "private long _previewLastResizeLogTick;");
        AssertDoesNotContain(windowSizingText, "Interlocked.Read(ref _previewLastResizeLogTick)");
        AssertDoesNotContain(windowSizingText, "Logger.Log(\"Preview resize active.");
        AssertDoesNotContain(closeLifecycleText, "private void MainWindow_SizeChanged(");
        AssertDoesNotContain(closeLifecycleText, "_previewLastResizeLogTick");

        return Task.CompletedTask;
    }

    private static Task PreviewRendererRuntimeState_LivesInRendererPartial()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var previewRendererText = ReadRepoFile("Sussudio/MainWindow.PreviewRenderer.cs").Replace("\r\n", "\n");
        var previewRendererReinitText = ReadRepoFile("Sussudio/MainWindow.PreviewRendererReinit.cs").Replace("\r\n", "\n");
        var previewSurfaceText = ReadRepoFile("Sussudio/MainWindow.PreviewSurface.cs").Replace("\r\n", "\n");
        var previewSurfaceControllerText = ReadRepoFile("Sussudio/Controllers/PreviewSurfacePresentationController.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotText = ReadRepoFile("Sussudio/MainWindow.PreviewRuntimeSnapshot.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotControllerText = ReadRepoFile("Sussudio/Controllers/PreviewRuntimeSnapshotController.cs").Replace("\r\n", "\n");
        var statsSnapshotText = ReadRepoFile("Sussudio/MainWindow.StatsSnapshot.cs").Replace("\r\n", "\n");
        var statsSnapshotProviderText = ReadRepoFile("Sussudio/Controllers/StatsSnapshotProvider.cs").Replace("\r\n", "\n");
        var statsSnapshotProviderRenderMetricsText = ReadRepoFile("Sussudio/Controllers/StatsSnapshotProvider.RenderMetrics.cs").Replace("\r\n", "\n");

        AssertContains(previewRendererText, "private SoftwareBitmapSource? _previewSource;");
        AssertContains(previewRendererText, "private D3D11PreviewRenderer? _d3dRenderer;");
        AssertContains(previewSurfaceText, "XAML-facing preview surface adapter");
        AssertContains(previewSurfaceText, "private PreviewSurfacePresentationController _previewSurfacePresentationController = null!;");
        AssertContains(previewSurfaceText, "private void InitializePreviewSurfacePresentationController()");
        AssertContains(previewSurfaceText, "private void UpdateVideoContentOverlays()");
        AssertContains(previewSurfaceText, "private void SetupVideoFrameShadow()");
        AssertContains(previewSurfaceText, "private void SetupControlBarShadow()");
        AssertContains(previewSurfaceText, "=> _previewSurfacePresentationController.UpdateVideoContentOverlays(ViewModel.SourceWidth, ViewModel.SourceHeight);");
        AssertContains(previewSurfaceText, "=> _previewSurfacePresentationController.SetGpuPreviewVisibility(visibility);");
        AssertContains(previewSurfaceControllerText, "internal sealed class PreviewSurfacePresentationController");
        AssertContains(previewSurfaceControllerText, "private SpriteVisual? _videoShadowVisual;");
        AssertContains(previewSurfaceControllerText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertContains(previewSurfaceText, "var scale = PreviewSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;");
        AssertContains(previewSurfaceText, "_d3dRenderer?.OnPanelSizeChanged(e.NewSize.Width, e.NewSize.Height, scale);");
        AssertContains(previewSurfaceControllerText, "public required Func<SwapChainPanel> GetPreviewSwapChainPanel { get; init; }");
        AssertContains(previewSurfaceControllerText, "var previewSwapChainPanel = _context.GetPreviewSwapChainPanel();");
        AssertContains(previewSurfaceControllerText, "public void UpdateVideoContentOverlays(int? sourceWidth, int? sourceHeight)");
        AssertContains(previewSurfaceControllerText, "public void SetupVideoFrameShadow()");
        AssertContains(previewSurfaceControllerText, "public void SetupControlBarShadow()");
        AssertContains(previewSurfaceControllerText, "public void ClearVideoFrameShadow()");
        AssertContains(previewSurfaceControllerText, "public void FadeInVideoFrameShadow(int delayMs, int durationMs)");
        AssertContains(previewSurfaceControllerText, "public void FadeInControlBarShadow(int delayMs, int durationMs)");
        AssertContains(previewRendererText, "private long _previewFramesArrived;");
        AssertContains(previewRendererText, "private long _previewFramesDisplayed;");
        AssertContains(previewRendererText, "private long _previewFramesDropped;");
        AssertContains(previewRendererText, "private long _previewLastPresentedTick;");
        AssertContains(previewRendererText, "private double _previewMinPresentationIntervalMs;");
        AssertContains(previewRendererReinitText, "private long _lastRendererStopTick;");
        AssertContains(previewRendererReinitText, "private long _rendererReinitUnsafeWindows;");
        AssertContains(previewRendererReinitText, "public long RendererReinitUnsafeWindows => Interlocked.Read(ref _rendererReinitUnsafeWindows);");
        AssertContains(previewRendererReinitText, "private void RecordPreviewRendererReinitUnsafeWindow(D3D11PreviewRenderer? previousRenderer, bool reinitAnimating)");
        AssertContains(previewRendererReinitText, "private void MarkPreviewRendererStopped()");
        AssertContains(previewRendererReinitText, "private void DisposeD3DPreviewRendererForReinit()");
        AssertContains(previewRendererReinitText, "renderer.RetireSharedDeviceReferenceForReinit();");
        AssertContains(previewRendererReinitText, "private void ReplacePreviewSwapChainPanelSurface()");
        AssertContains(previewRendererReinitText, "D3D11_RENDERER_REINIT_UNSAFE_WINDOW");
        AssertContains(previewRendererReinitText, "PREVIEW_REINIT_SWAPCHAIN_PANEL_REPLACED");
        AssertContains(previewRendererText, "RecordPreviewRendererReinitUnsafeWindow(_d3dRenderer, _isPreviewReinitAnimating);");
        AssertContains(previewRendererText, "MarkPreviewRendererStopped();");
        AssertContains(previewRendererText, "private double ResolvePreviewExpectedIntervalMs()");
        AssertContains(previewRuntimeSnapshotText, "private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewRuntimeSnapshotText, "return GetPreviewRuntimeSnapshot();");
        AssertContains(previewRuntimeSnapshotText, "completion.TrySetResult(GetPreviewRuntimeSnapshot());");
        AssertContains(previewRuntimeSnapshotText, "private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()");
        AssertContains(previewRuntimeSnapshotText, "return PreviewRuntimeSnapshotController.Build(new PreviewRuntimeSnapshotInput");
        AssertContains(previewRuntimeSnapshotText, "D3DRenderer = _d3dRenderer,");
        AssertContains(previewRuntimeSnapshotText, "GpuElementVisible = PreviewSwapChainPanel.Visibility == Visibility.Visible,");
        AssertContains(previewRuntimeSnapshotText, "StartupState = _previewStartupState.ToString(),");
        AssertContains(previewRuntimeSnapshotText, "GpuPositionEventCount = Interlocked.Read(ref _previewStartupPositionEventCount)");
        AssertContains(previewRuntimeSnapshotControllerText, "internal sealed class PreviewRuntimeSnapshotInput");
        AssertContains(previewRuntimeSnapshotControllerText, "internal static class PreviewRuntimeSnapshotController");
        AssertContains(previewRuntimeSnapshotControllerText, "public static PreviewRuntimeSnapshot Build(PreviewRuntimeSnapshotInput input)");
        AssertContains(previewRuntimeSnapshotControllerText, "var d3d = input.D3DRenderer;");
        AssertContains(previewRuntimeSnapshotControllerText, "var rendererCadence = d3d?.GetPresentCadenceMetrics(input.PreviewMinPresentationIntervalMs);");
        AssertContains(previewRuntimeSnapshotControllerText, "return new PreviewRuntimeSnapshot");
        AssertContains(previewRuntimeSnapshotControllerText, "BlankSuspected = blankSuspected,");
        AssertContains(previewRuntimeSnapshotControllerText, "StallSuspected = stallSuspected,");
        AssertContains(previewRendererText, "var sourceFps = ViewModel.SelectedFormat?.FrameRateExact ?? 0;");
        AssertContains(previewRendererText, "return Math.Max(1.0, 1000.0 / sourceFps);");
        AssertContains(previewRendererText, "_previewMinPresentationIntervalMs = ResolvePreviewExpectedIntervalMs();");
        AssertContains(statsSnapshotText, "GetPreviewMinPresentationIntervalMs = () => _previewMinPresentationIntervalMs");
        AssertContains(statsSnapshotProviderText, "BuildRenderMetrics(_context.GetRenderer(), _context.GetPreviewMinPresentationIntervalMs())");
        AssertContains(statsSnapshotProviderRenderMetricsText, "GetPresentCadenceMetrics(previewMinPresentationIntervalMs)");
        AssertDoesNotContain(previewRuntimeSnapshotText, "return new PreviewRuntimeSnapshot");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetRenderCpuTimingMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetFrameOwnershipMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetDxgiFrameStatisticsMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetFrameLatencyWaitMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetPipelineLatencyMetrics()");
        AssertDoesNotContain(mainWindowText, "private SoftwareBitmapSource? _previewSource;");
        AssertDoesNotContain(mainWindowText, "private D3D11PreviewRenderer? _d3dRenderer;");
        AssertDoesNotContain(mainWindowText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(mainWindowText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(previewSurfaceText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(previewSurfaceText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(mainWindowText, "private long _previewFramesArrived;");
        AssertDoesNotContain(mainWindowText, "private long _previewFramesDisplayed;");
        AssertDoesNotContain(mainWindowText, "private long _previewFramesDropped;");
        AssertDoesNotContain(mainWindowText, "private long _previewLastPresentedTick;");
        AssertDoesNotContain(mainWindowText, "private long _lastRendererStopTick;");
        AssertDoesNotContain(mainWindowText, "private long _rendererReinitUnsafeWindows;");
        AssertDoesNotContain(mainWindowText, "private double _previewMinPresentationIntervalMs;");
        AssertDoesNotContain(mainWindowText, "public long RendererReinitUnsafeWindows => Interlocked.Read(ref _rendererReinitUnsafeWindows);");
        AssertDoesNotContain(mainWindowText, "private double ResolvePreviewExpectedIntervalMs()");
        AssertDoesNotContain(previewRendererText, "private long _lastRendererStopTick;");
        AssertDoesNotContain(previewRendererText, "private long _rendererReinitUnsafeWindows;");
        AssertDoesNotContain(previewRendererText, "public long RendererReinitUnsafeWindows => Interlocked.Read(ref _rendererReinitUnsafeWindows);");
        AssertDoesNotContain(previewRendererText, "private void ReplacePreviewSwapChainPanelSurface()");
        AssertDoesNotContain(mainWindowText, "private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync");
        AssertDoesNotContain(previewRendererText, "private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync");
        AssertDoesNotContain(previewRendererText, "private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()");
        AssertDoesNotContain(mainWindowText, "private static bool IsHdrSubtype");

        return Task.CompletedTask;
    }
}
