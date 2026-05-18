using System.Threading.Tasks;

static partial class Program
{
    private static Task PreviewResizeTelemetry_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewResizeTelemetryController.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var shutdownCleanupControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowShutdownCleanupController.cs").Replace("\r\n", "\n");
        var previewRendererText = ReadRepoFile("Sussudio/MainWindow.PreviewRenderer.cs").Replace("\r\n", "\n");

        AssertContains(previewRendererText, "private PreviewResizeTelemetryController _previewResizeTelemetryController = null!;");
        AssertContains(previewRendererText, "private void InitializePreviewResizeTelemetryController()");
        AssertContains(previewRendererText, "private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)");
        AssertContains(previewRendererText, "_previewResizeTelemetryController.HandleSizeChanged(");
        AssertContains(previewRendererText, "ViewModel.IsPreviewing,");
        AssertContains(previewRendererText, "_previewRendererHostController.HasD3DRenderer,");
        AssertContains(previewRendererText, "PreviewSwapChainPanel.Visibility);");
        AssertContains(previewRendererText, "private void ResetPreviewResizeTelemetry()");
        AssertContains(previewRendererText, "=> _previewResizeTelemetryController.Reset();");
        AssertContains(mainWindowText, "InitializePreviewResizeTelemetryController();");
        AssertContains(mainWindowText, "mainContent.SizeChanged += MainWindow_SizeChanged;");
        AssertContains(shutdownCleanupText, "private void DetachMainContentSizeChanged()");
        AssertContains(shutdownCleanupText, "mainContent.SizeChanged -= MainWindow_SizeChanged;");
        AssertContains(shutdownCleanupControllerText, "_context.DetachMainContentSizeChanged();");
        AssertContains(previewRendererText, "ResetPreviewResizeTelemetry = ResetPreviewResizeTelemetry,");
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
        AssertDoesNotContain(previewRendererText, "Interlocked.Read(ref _previewLastResizeLogTick)");
        AssertDoesNotContain(previewRendererText, "Logger.Log(\"Preview resize active.");
        AssertDoesNotContain(closeLifecycleText, "private void MainWindow_SizeChanged(");
        AssertDoesNotContain(closeLifecycleText, "_previewLastResizeLogTick");

        return Task.CompletedTask;
    }

    private static Task PreviewRendererHostController_OwnsRuntimeState()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var mainWindowFamilyText = string.Join(
                "\n",
                Directory.GetFiles(Path.Combine(GetRepoRoot(), "Sussudio"), "MainWindow*.cs")
                    .Select(File.ReadAllText))
            .Replace("\r\n", "\n");
        var previewRendererText = ReadRepoFile("Sussudio/MainWindow.PreviewRenderer.cs").Replace("\r\n", "\n");
        var previewRendererHostControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs").Replace("\r\n", "\n");
        var previewRendererHostLifecycleText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.Lifecycle.cs").Replace("\r\n", "\n");
        var previewRendererHostD3dText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.D3D.cs").Replace("\r\n", "\n");
        var previewRendererHostReinitText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.Reinit.cs").Replace("\r\n", "\n");
        var previewSurfaceText = ReadRepoFile("Sussudio/MainWindow.PreviewSurface.cs").Replace("\r\n", "\n");
        var previewSurfaceControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewSurfacePresentationController.cs").Replace("\r\n", "\n");
        var previewRendererStartupPlanBuilderText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRendererStartupPlanBuilder.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotText = ReadRepoFile("Sussudio/MainWindow.PreviewRuntimeSnapshot.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotController.cs").Replace("\r\n", "\n");
        var previewRuntimeD3DProjectionText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DProjection.cs").Replace("\r\n", "\n");
        var previewRuntimeD3DProjectionBuilderText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DProjection.Builder.cs").Replace("\r\n", "\n");
        var previewSurfaceShadowControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewSurfaceShadowController.cs").Replace("\r\n", "\n");
        var statsSnapshotText = ReadRepoFile("Sussudio/MainWindow.StatsOverlay.cs").Replace("\r\n", "\n");
        var statsSnapshotProviderText = ReadRepoFile("Sussudio/Controllers/Stats/StatsSnapshotProvider.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

        AssertContains(previewRendererText, "private PreviewRendererHostController _previewRendererHostController = null!;");
        AssertContains(previewRendererText, "private void InitializePreviewRendererHostController()");
        AssertContains(previewRendererText, "GetPreviewSwapChainPanel = () => PreviewSwapChainPanel,");
        AssertContains(previewRendererText, "SetPreviewSwapChainPanel = panel => PreviewSwapChainPanel = panel,");
        AssertContains(previewRendererText, "PreviewContentGridSizeChangedHandler = OnPreviewContentGridSizeChanged,");
        AssertContains(previewRendererText, "PreviewSwapChainPanelSizeChangedHandler = OnPreviewSwapChainPanelSizeChanged,");
        AssertContains(previewRendererText, "ClearPreviewReinitAnimatingForShutdown = () =>");
        AssertContains(previewRendererText, "ConfirmPreviewFirstVisual = ConfirmPreviewFirstVisual,");
        AssertContains(previewRendererText, "MarkStartupFailed = reason => SetPreviewStartupState(PreviewStartupState.Failed, reason),");
        AssertContains(previewRendererText, "ConfigurePreviewStartupSignals = ConfigurePreviewStartupSignals,");
        AssertContains(previewRendererText, "private Task StartPreviewRendererAsync()");
        AssertContains(previewRendererText, "=> _previewRendererHostController.StartAsync();");
        AssertContains(previewRendererText, "private Task StopPreviewRendererAsync()");
        AssertContains(previewRendererText, "=> _previewRendererHostController.StopAsync();");
        AssertContains(previewRendererText, "private void StopPreviewForShutdown()");
        AssertContains(previewRendererText, "=> _previewRendererHostController.StopForShutdown();");
        AssertContains(mainWindowText, "InitializePreviewRendererHostController();");

        AssertContains(previewSurfaceText, "XAML-facing preview surface adapter");
        AssertContains(previewSurfaceText, "private PreviewSurfacePresentationController _previewSurfacePresentationController = null!;");
        AssertContains(previewSurfaceText, "private PreviewSurfaceShadowController _previewSurfaceShadowController = null!;");
        AssertContains(previewSurfaceText, "private void InitializePreviewSurfacePresentationController()");
        AssertContains(previewSurfaceText, "private void UpdateVideoContentOverlays()");
        AssertContains(previewSurfaceText, "private void SetupVideoFrameShadow()");
        AssertContains(previewSurfaceText, "private void SetupControlBarShadow()");
        AssertContains(previewSurfaceText, "=> _previewSurfacePresentationController.UpdateVideoContentOverlays(ViewModel.SourceWidth, ViewModel.SourceHeight);");
        AssertContains(previewSurfaceText, "=> _previewSurfacePresentationController.SetGpuPreviewVisibility(visibility);");
        AssertContains(previewSurfaceText, "=> _previewSurfaceShadowController.SetupVideoFrameShadow();");
        AssertContains(previewSurfaceText, "=> _previewSurfaceShadowController.SetupControlBarShadow();");
        AssertContains(previewSurfaceText, "=> _previewSurfaceShadowController.ClearVideoFrameShadow();");
        AssertContains(previewSurfaceText, "=> _previewSurfaceShadowController.FadeInVideoFrameShadow(delayMs, durationMs);");
        AssertContains(previewSurfaceControllerText, "internal sealed class PreviewSurfacePresentationController");
        AssertContains(previewSurfaceShadowControllerText, "internal sealed class PreviewSurfaceShadowController");
        AssertContains(previewSurfaceShadowControllerText, "private SpriteVisual? _videoShadowVisual;");
        AssertContains(previewSurfaceShadowControllerText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertContains(previewSurfaceText, "var scale = PreviewSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;");
        AssertContains(previewSurfaceText, "_previewRendererHostController.OnPanelSizeChanged(e.NewSize.Width, e.NewSize.Height, scale);");
        AssertContains(previewSurfaceControllerText, "public required Func<SwapChainPanel> GetPreviewSwapChainPanel { get; init; }");
        AssertContains(previewSurfaceControllerText, "private readonly PreviewSurfaceShadowController _shadowController;");
        AssertContains(previewSurfaceControllerText, "PreviewSurfaceShadowController shadowController)");
        AssertContains(previewSurfaceControllerText, "var previewSwapChainPanel = _context.GetPreviewSwapChainPanel();");
        AssertContains(previewSurfaceControllerText, "public void UpdateVideoContentOverlays(int? sourceWidth, int? sourceHeight)");
        AssertContains(previewSurfaceControllerText, "_shadowController.ClearVideoFrameBounds();");
        AssertContains(previewSurfaceControllerText, "_shadowController.UpdateVideoFrameBounds(marginH, marginV, fitW, fitH);");
        AssertContains(previewSurfaceShadowControllerText, "public void UpdateVideoFrameBounds(double marginH, double marginV, double fitW, double fitH)");
        AssertContains(previewSurfaceShadowControllerText, "public void ClearVideoFrameBounds()");
        AssertContains(previewSurfaceShadowControllerText, "_videoShadowVisual.Size = Vector2.Zero;");
        AssertContains(previewSurfaceShadowControllerText, "public void SetupVideoFrameShadow()");
        AssertContains(previewSurfaceShadowControllerText, "public void SetupControlBarShadow()");
        AssertContains(previewSurfaceShadowControllerText, "public void ClearVideoFrameShadow()");
        AssertContains(previewSurfaceShadowControllerText, "public void FadeInVideoFrameShadow(int delayMs, int durationMs)");
        AssertContains(previewSurfaceShadowControllerText, "public void FadeInControlBarShadow(int delayMs, int durationMs)");

        AssertContains(previewRendererHostControllerText, "internal sealed class PreviewRendererHostControllerContext");
        AssertContains(previewRendererHostControllerText, "internal sealed partial class PreviewRendererHostController");
        AssertContains(previewRendererHostLifecycleText, "internal sealed partial class PreviewRendererHostController");
        AssertContains(previewRendererHostD3dText, "internal sealed partial class PreviewRendererHostController");
        AssertContains(previewRendererHostReinitText, "internal sealed partial class PreviewRendererHostController");
        AssertContains(previewRendererHostControllerText, "private SoftwareBitmapSource? _previewSource;");
        AssertContains(previewRendererHostControllerText, "private D3D11PreviewRenderer? _d3dRenderer;");
        AssertContains(previewRendererHostControllerText, "private long _previewFramesArrived;");
        AssertContains(previewRendererHostControllerText, "private long _previewFramesDisplayed;");
        AssertContains(previewRendererHostControllerText, "private long _previewFramesDropped;");
        AssertContains(previewRendererHostControllerText, "private long _previewLastPresentedTick;");
        AssertContains(previewRendererHostControllerText, "private double _previewMinPresentationIntervalMs;");
        AssertContains(previewRendererHostControllerText, "private long _lastRendererStopTick;");
        AssertContains(previewRendererHostControllerText, "private long _rendererReinitUnsafeWindows;");
        AssertContains(previewRendererHostControllerText, "public D3D11PreviewRenderer? Renderer => _d3dRenderer;");
        AssertContains(previewRendererHostControllerText, "public bool HasD3DRenderer => _d3dRenderer != null;");
        AssertContains(previewRendererHostControllerText, "public bool IsCpuPreviewSourceAttached => _previewSource != null;");
        AssertContains(previewRendererHostControllerText, "public double PreviewMinPresentationIntervalMs => _previewMinPresentationIntervalMs;");
        AssertContains(previewRendererHostControllerText, "public long RendererReinitUnsafeWindows => Interlocked.Read(ref _rendererReinitUnsafeWindows);");
        AssertContains(previewRendererHostControllerText, "public int? PendingFrameCount => _d3dRenderer?.PendingFrameCount;");
        AssertContains(previewRendererHostLifecycleText, "public Task StartAsync()");
        AssertContains(previewRendererHostLifecycleText, "RecordPreviewRendererReinitUnsafeWindow(_d3dRenderer, _context.IsPreviewReinitAnimating());");
        AssertContains(previewRendererHostLifecycleText, "var startupPlan = BuildPreviewRendererStartupPlan();");
        AssertContains(previewRendererHostLifecycleText, "_previewMinPresentationIntervalMs = startupPlan.PreviewMinPresentationIntervalMs;");
        AssertContains(previewRendererHostLifecycleText, "private PreviewRendererStartupPlan BuildPreviewRendererStartupPlan()");
        AssertContains(previewRendererHostLifecycleText, "PreviewRendererStartupPlanBuilder.Build(");
        AssertContains(previewRendererHostLifecycleText, "private void CleanupPreviewResources()");
        AssertContains(previewRendererHostLifecycleText, "_d3dRenderer = null;");
        AssertContains(previewRendererHostLifecycleText, "public Task StopAsync()");
        AssertContains(previewRendererHostD3dText, "private void StartD3DRenderer(PreviewRendererStartupPlan startupPlan)");
        AssertContains(previewRendererHostD3dText, "renderer.SetExpectedFrameRate(rendererFps);");
        AssertContains(previewRendererHostD3dText, "renderer.Start(rendererWidth, rendererHeight, rendererFps, isHdr);");
        AssertContains(previewRendererHostD3dText, "_context.ViewModel.SetPreviewFrameSink(_d3dRenderer);");
        AssertContains(previewRendererHostD3dText, "PreviewStartupStrategy.D3D11VideoProcessor");
        AssertContains(previewRendererHostD3dText, "_context.MarkPreviewRendererAttached();");
        AssertContains(previewRendererHostD3dText, "private D3D11PreviewRenderer CreateFreshD3DPreviewRenderer(bool replaceSwapChainSurface)");
        AssertContains(previewRendererHostD3dText, "private void OnD3DRendererFirstFrameRendered()");
        AssertContains(previewRendererHostD3dText, "private void OnD3DRendererRenderThreadFailed(string reason)");
        AssertContains(previewRendererHostLifecycleText, "private void StartCpuRenderer()");
        AssertContains(previewRendererHostLifecycleText, "_context.ViewModel.SetPreviewFrameSink(null);");
        AssertContains(previewRendererHostLifecycleText, "_previewSource = new SoftwareBitmapSource();");
        AssertContains(previewRendererHostReinitText, "private void RecordPreviewRendererReinitUnsafeWindow(D3D11PreviewRenderer? previousRenderer, bool reinitAnimating)");
        AssertContains(previewRendererHostReinitText, "private void MarkPreviewRendererStopped()");
        AssertContains(previewRendererHostReinitText, "public void DisposeD3DPreviewRendererForReinit()");
        AssertContains(previewRendererHostReinitText, "renderer.RetireSharedDeviceReferenceForReinit();");
        AssertContains(previewRendererHostReinitText, "private void ReplacePreviewSwapChainPanelSurface()");
        AssertContains(previewRendererHostReinitText, "D3D11_RENDERER_REINIT_UNSAFE_WINDOW");
        AssertContains(previewRendererHostReinitText, "PREVIEW_REINIT_SWAPCHAIN_PANEL_REPLACED");
        AssertContains(agentMapText, "PreviewRendererHostController.Lifecycle.cs");
        AssertContains(agentMapText, "PreviewRendererHostController.D3D.cs");
        AssertContains(agentMapText, "PreviewRendererHostController.Reinit.cs");
        AssertContains(cleanupPlanText, "PreviewRendererHostController.Lifecycle.cs");
        AssertContains(cleanupPlanText, "PreviewRendererHostController.D3D.cs");
        AssertContains(cleanupPlanText, "PreviewRendererHostController.Reinit.cs");
        AssertContains(previewRendererText, "=> _previewRendererHostController.RendererReinitUnsafeWindows;");
        AssertContains(previewRendererText, "=> _previewRendererHostController.DisposeD3DPreviewRendererForReinit();");

        AssertContains(previewRendererStartupPlanBuilderText, "internal sealed record PreviewRendererStartupPlan(");
        AssertContains(previewRendererStartupPlanBuilderText, "internal static class PreviewRendererStartupPlanBuilder");
        AssertContains(previewRendererStartupPlanBuilderText, "private const int DefaultWidth = 1920;");
        AssertContains(previewRendererStartupPlanBuilderText, "private const int DefaultHeight = 1080;");
        AssertContains(previewRendererStartupPlanBuilderText, "private const double DefaultFps = 60.0;");
        AssertContains(previewRendererStartupPlanBuilderText, "public static double ResolveExpectedIntervalMs(MediaFormat? selectedFormat)");
        AssertContains(previewRendererStartupPlanBuilderText, "public static PreviewRendererStartupPlan Build(");
        AssertContains(previewRendererStartupPlanBuilderText, "var negotiatedWidth = sourceProbe?.SessionActive == true ? sourceProbe.CurrentWidth : 0;");
        AssertContains(previewRendererStartupPlanBuilderText, "var rendererWidth = negotiatedWidth > 0 ? negotiatedWidth : settingsWidth;");
        AssertContains(previewRendererStartupPlanBuilderText, "var rendererFps = negotiatedFps > 0 ? negotiatedFps : settingsFps;");

        AssertContains(previewRuntimeSnapshotText, "private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewRuntimeSnapshotText, "=> await WindowUiDispatchController.InvokeWithRetryAsync(");
        AssertContains(previewRuntimeSnapshotText, "GetPreviewRuntimeSnapshot,");
        AssertContains(previewRuntimeSnapshotText, "\"Failed to enqueue preview snapshot operation.\",");
        AssertContains(previewRuntimeSnapshotText, "cancellationToken).ConfigureAwait(false);");
        AssertContains(previewRuntimeSnapshotText, "private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()");
        AssertContains(previewRuntimeSnapshotText, "return PreviewRuntimeSnapshotController.Build(new PreviewRuntimeSnapshotInput");
        AssertContains(previewRuntimeSnapshotText, "D3DRenderer = _previewRendererHostController.Renderer,");
        AssertContains(previewRuntimeSnapshotText, "PreviewSourceAttached = _previewRendererHostController.IsCpuPreviewSourceAttached,");
        AssertContains(previewRuntimeSnapshotText, "GpuElementVisible = PreviewSwapChainPanel.Visibility == Visibility.Visible,");
        AssertContains(previewRuntimeSnapshotText, "FramesArrived = _previewRendererHostController.FramesArrived,");
        AssertContains(previewRuntimeSnapshotText, "PreviewMinPresentationIntervalMs = _previewRendererHostController.PreviewMinPresentationIntervalMs,");
        AssertContains(previewRuntimeSnapshotText, "StartupState = CurrentPreviewStartupState.ToString(),");
        AssertContains(previewRuntimeSnapshotText, "GpuPositionEventCount = PreviewStartupGpuPositionEventCount");
        AssertContains(previewRuntimeSnapshotControllerText, "internal sealed class PreviewRuntimeSnapshotInput");
        AssertContains(previewRuntimeSnapshotControllerText, "internal static class PreviewRuntimeSnapshotController");
        AssertContains(previewRuntimeSnapshotControllerText, "public static PreviewRuntimeSnapshot Build(PreviewRuntimeSnapshotInput input)");
        AssertContains(previewRuntimeSnapshotControllerText, "var d3dProjection = PreviewRuntimeD3DProjection.Build(input);");
        AssertContains(previewRuntimeSnapshotControllerText, "return new PreviewRuntimeSnapshot");
        AssertContains(previewRuntimeSnapshotControllerText, "BlankSuspected = blankSuspected,");
        AssertContains(previewRuntimeSnapshotControllerText, "StallSuspected = stallSuspected,");
        AssertContains(previewRuntimeD3DProjectionText, "internal sealed partial class PreviewRuntimeD3DProjection");
        AssertContains(previewRuntimeD3DProjectionBuilderText, "internal sealed partial class PreviewRuntimeD3DProjection");
        AssertContains(previewRuntimeD3DProjectionBuilderText, "public static PreviewRuntimeD3DProjection Build(PreviewRuntimeSnapshotInput input)");
        AssertContains(previewRuntimeD3DProjectionBuilderText, "var d3d = input.D3DRenderer;");
        AssertContains(previewRuntimeD3DProjectionBuilderText, "var rendererCadence = d3d?.GetPresentCadenceMetrics(input.PreviewMinPresentationIntervalMs);");
        AssertContains(previewRuntimeD3DProjectionBuilderText, "var d3dFrameLatencyWait = d3d?.GetFrameLatencyWaitMetrics();");
        AssertContains(previewRuntimeD3DProjectionBuilderText, "D3DFrameStatsPresentCount = d3dFrameStats?.PresentCount ?? -1,");
        AssertContains(previewRuntimeD3DProjectionBuilderText, "D3DRecentSlowFrames = d3d?.GetRecentSlowFrameDiagnostics() ?? Array.Empty<PreviewSlowFrameDiagnostic>(),");
        AssertContains(previewRuntimeD3DProjectionBuilderText, "EstimatedPipelineLatencyMs = d3dPipelineLatency?.AverageMs ?? 0,");
        AssertContains(previewRuntimeD3DProjectionBuilderText, "GpuPlaybackState = d3d == null ? \"None\" : (d3d.IsRendering ? \"Rendering\" : \"Idle\"),");
        AssertDoesNotContain(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DProjection Build(PreviewRuntimeSnapshotInput input)");

        AssertContains(statsSnapshotText, "GetRenderer = () => _previewRendererHostController.Renderer,");
        AssertContains(statsSnapshotText, "GetPreviewMinPresentationIntervalMs = () => _previewRendererHostController.PreviewMinPresentationIntervalMs");
        AssertContains(statsSnapshotProviderText, "BuildRenderMetrics(_context.GetRenderer(), _context.GetPreviewMinPresentationIntervalMs())");
        AssertContains(statsSnapshotProviderText, "GetPresentCadenceMetrics(previewMinPresentationIntervalMs)");

        AssertDoesNotContain(previewRendererText, "var sourceFps = ViewModel.SelectedFormat?.FrameRateExact ?? 0;");
        AssertDoesNotContain(previewRendererText, "var negotiatedWidth = sourceProbe.SessionActive ? sourceProbe.CurrentWidth : 0;");
        AssertDoesNotContain(previewRendererText, "var rendererWidth = negotiatedWidth > 0 ? negotiatedWidth : width;");
        AssertDoesNotContain(previewRuntimeSnapshotText, "TaskCompletionSource<PreviewRuntimeSnapshot>");
        AssertDoesNotContain(previewRuntimeSnapshotText, "return new PreviewRuntimeSnapshot");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetRenderCpuTimingMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetFrameOwnershipMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetDxgiFrameStatisticsMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetFrameLatencyWaitMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetPipelineLatencyMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "_dispatcherQueue.TryEnqueue");
        AssertDoesNotContain(previewRuntimeSnapshotText, "const int maxAttempts = 3;");
        AssertDoesNotContain(previewRuntimeSnapshotText, "completion.TrySetResult(GetPreviewRuntimeSnapshot());");
        AssertDoesNotContain(previewRuntimeSnapshotText, "await Task.Delay(50, cancellationToken).ConfigureAwait(false);");
        AssertDoesNotContain(mainWindowFamilyText, "private SoftwareBitmapSource? _previewSource;");
        AssertDoesNotContain(mainWindowFamilyText, "private D3D11PreviewRenderer? _d3dRenderer;");
        AssertDoesNotContain(mainWindowText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(mainWindowText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(previewSurfaceText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(previewSurfaceText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(previewSurfaceControllerText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(previewSurfaceControllerText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(previewSurfaceControllerText, "ElementCompositionPreview.SetElementChildVisual");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(previewRendererHostControllerText, "private void StartD3DRenderer(PreviewRendererStartupPlan startupPlan)");
        AssertDoesNotContain(previewRendererHostControllerText, "private void StartCpuRenderer()");
        AssertDoesNotContain(previewRendererHostControllerText, "private void CleanupPreviewResources()");
        AssertDoesNotContain(previewRendererHostControllerText, "private void ReplacePreviewSwapChainPanelSurface()");
        AssertDoesNotContain(mainWindowFamilyText, "private long _previewFramesArrived;");
        AssertDoesNotContain(mainWindowFamilyText, "private long _previewFramesDisplayed;");
        AssertDoesNotContain(mainWindowFamilyText, "private long _previewFramesDropped;");
        AssertDoesNotContain(mainWindowFamilyText, "private long _previewLastPresentedTick;");
        AssertDoesNotContain(mainWindowFamilyText, "private long _lastRendererStopTick;");
        AssertDoesNotContain(mainWindowFamilyText, "private long _rendererReinitUnsafeWindows;");
        AssertDoesNotContain(mainWindowFamilyText, "private double _previewMinPresentationIntervalMs;");
        AssertDoesNotContain(mainWindowFamilyText, "new D3D11PreviewRenderer(");
        AssertDoesNotContain(mainWindowFamilyText, "RetireSharedDeviceReferenceForReinit();");
        AssertDoesNotContain(mainWindowText, "PreviewRendererStartupPlanBuilder.ResolveExpectedIntervalMs");
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

    private static Task PreviewRendererStartupPlanBuilder_PreservesFallbackPolicy()
    {
        var builderType = RequireType("Sussudio.Controllers.PreviewRendererStartupPlanBuilder");
        var mediaFormatType = RequireType("Sussudio.Models.MediaFormat");
        var captureSettingsType = RequireType("Sussudio.Models.CaptureSettings");
        var sourceProbeType = RequireType("Sussudio.Models.VideoSourceProbeResult");
        var hdrOutputModeType = RequireType("Sussudio.Models.HdrOutputMode");
        var build = builderType.GetMethod("Build")
            ?? throw new InvalidOperationException("PreviewRendererStartupPlanBuilder.Build was not found.");
        var resolveExpectedIntervalMs = builderType.GetMethod("ResolveExpectedIntervalMs")
            ?? throw new InvalidOperationException("PreviewRendererStartupPlanBuilder.ResolveExpectedIntervalMs was not found.");

        var fallbackInterval = (double)(resolveExpectedIntervalMs.Invoke(null, new object?[] { null }) ?? 0.0);
        AssertNearlyEqual(1000.0 / 60.0, fallbackInterval, 0.0001, "default preview renderer interval");

        var selectedFormat = Activator.CreateInstance(mediaFormatType)!;
        SetPropertyOrBackingField(selectedFormat, "FrameRate", 30.0);

        var inactivePlan = build.Invoke(null, new object?[] { false, selectedFormat, null, null })!;
        AssertEqual(false, GetBoolProperty(inactivePlan, "UseD3DRenderer"), "inactive preview plan mode");
        AssertEqual(1920, GetIntProperty(inactivePlan, "RendererWidth"), "inactive default width");
        AssertEqual(1080, GetIntProperty(inactivePlan, "RendererHeight"), "inactive default height");
        AssertNearlyEqual(60.0, GetDoubleProperty(inactivePlan, "RendererFps"), 0.0001, "inactive default renderer FPS");
        AssertNearlyEqual(1000.0 / 30.0, GetDoubleProperty(inactivePlan, "PreviewMinPresentationIntervalMs"), 0.0001, "inactive selected-format interval");

        var settings = Activator.CreateInstance(captureSettingsType)!;
        SetPropertyOrBackingField(settings, "Width", (uint)2560);
        SetPropertyOrBackingField(settings, "Height", (uint)1440);
        SetPropertyOrBackingField(settings, "FrameRate", 144.0);
        var inactiveSourceProbe = Activator.CreateInstance(sourceProbeType)!;
        SetPropertyOrBackingField(inactiveSourceProbe, "SessionActive", false);

        var settingsPlan = build.Invoke(null, new object?[] { true, selectedFormat, settings, inactiveSourceProbe })!;
        AssertEqual(true, GetBoolProperty(settingsPlan, "UseD3DRenderer"), "active preview plan mode");
        AssertEqual(2560, GetIntProperty(settingsPlan, "RendererWidth"), "settings fallback width");
        AssertEqual(1440, GetIntProperty(settingsPlan, "RendererHeight"), "settings fallback height");
        AssertNearlyEqual(144.0, GetDoubleProperty(settingsPlan, "RendererFps"), 0.0001, "settings fallback FPS");
        AssertNearlyEqual(1000.0 / 144.0, GetDoubleProperty(settingsPlan, "PreviewMinPresentationIntervalMs"), 0.0001, "settings fallback interval");

        var activeSourceProbe = Activator.CreateInstance(sourceProbeType)!;
        SetPropertyOrBackingField(activeSourceProbe, "SessionActive", true);
        SetPropertyOrBackingField(activeSourceProbe, "CurrentWidth", 3840);
        SetPropertyOrBackingField(activeSourceProbe, "CurrentHeight", 2160);
        SetPropertyOrBackingField(activeSourceProbe, "CurrentFrameRate", 119.88);
        var sourcePlan = build.Invoke(null, new object?[] { true, selectedFormat, settings, activeSourceProbe })!;
        AssertEqual(3840, GetIntProperty(sourcePlan, "RendererWidth"), "active source width");
        AssertEqual(2160, GetIntProperty(sourcePlan, "RendererHeight"), "active source height");
        AssertNearlyEqual(119.88, GetDoubleProperty(sourcePlan, "RendererFps"), 0.0001, "active source FPS");
        AssertNearlyEqual(1000.0 / 119.88, GetDoubleProperty(sourcePlan, "PreviewMinPresentationIntervalMs"), 0.0001, "active source interval");

        var previousForceOff = Environment.GetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF");
        try
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", null);
            SetPropertyOrBackingField(settings, "HdrEnabled", true);
            SetPropertyOrBackingField(settings, "HdrOutputMode", Enum.Parse(hdrOutputModeType, "Hdr10Pq"));
            var hdrPlan = build.Invoke(null, new object?[] { true, selectedFormat, settings, inactiveSourceProbe })!;
            AssertEqual(true, GetBoolProperty(hdrPlan, "IsHdr"), "HDR plan follows HDR output policy");

            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", "true");
            var forceOffPlan = build.Invoke(null, new object?[] { true, selectedFormat, settings, inactiveSourceProbe })!;
            AssertEqual(false, GetBoolProperty(forceOffPlan, "IsHdr"), "HDR plan honors force-off policy");
        }
        finally
        {
            Environment.SetEnvironmentVariable("SUSSUDIO_HDR_OUTPUT_FORCE_OFF", previousForceOff);
        }

        return Task.CompletedTask;
    }
}
