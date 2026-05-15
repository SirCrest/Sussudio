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
        var previewRendererStartupPlanBuilderText = ReadRepoFile("Sussudio/Controllers/PreviewRendererStartupPlanBuilder.cs").Replace("\r\n", "\n");
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
        AssertContains(previewRendererText, "private PreviewRendererStartupPlan BuildPreviewRendererStartupPlan()");
        AssertContains(previewRendererText, "PreviewRendererStartupPlanBuilder.Build(");
        AssertContains(previewRendererText, "var startupPlan = BuildPreviewRendererStartupPlan();");
        AssertContains(previewRendererText, "_previewMinPresentationIntervalMs = startupPlan.PreviewMinPresentationIntervalMs;");
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
        AssertContains(previewRuntimeSnapshotText, "return GetPreviewRuntimeSnapshot();");
        AssertContains(previewRuntimeSnapshotText, "completion.TrySetResult(GetPreviewRuntimeSnapshot());");
        AssertContains(previewRuntimeSnapshotText, "private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()");
        AssertContains(previewRuntimeSnapshotText, "return PreviewRuntimeSnapshotController.Build(new PreviewRuntimeSnapshotInput");
        AssertContains(previewRuntimeSnapshotText, "D3DRenderer = _d3dRenderer,");
        AssertContains(previewRuntimeSnapshotText, "GpuElementVisible = PreviewSwapChainPanel.Visibility == Visibility.Visible,");
        AssertContains(previewRuntimeSnapshotText, "StartupState = CurrentPreviewStartupState.ToString(),");
        AssertContains(previewRuntimeSnapshotText, "GpuPositionEventCount = Interlocked.Read(ref _previewStartupPositionEventCount)");
        AssertContains(previewRuntimeSnapshotControllerText, "internal sealed class PreviewRuntimeSnapshotInput");
        AssertContains(previewRuntimeSnapshotControllerText, "internal static class PreviewRuntimeSnapshotController");
        AssertContains(previewRuntimeSnapshotControllerText, "public static PreviewRuntimeSnapshot Build(PreviewRuntimeSnapshotInput input)");
        AssertContains(previewRuntimeSnapshotControllerText, "var d3d = input.D3DRenderer;");
        AssertContains(previewRuntimeSnapshotControllerText, "var rendererCadence = d3d?.GetPresentCadenceMetrics(input.PreviewMinPresentationIntervalMs);");
        AssertContains(previewRuntimeSnapshotControllerText, "return new PreviewRuntimeSnapshot");
        AssertContains(previewRuntimeSnapshotControllerText, "BlankSuspected = blankSuspected,");
        AssertContains(previewRuntimeSnapshotControllerText, "StallSuspected = stallSuspected,");
        AssertDoesNotContain(previewRendererText, "var sourceFps = ViewModel.SelectedFormat?.FrameRateExact ?? 0;");
        AssertDoesNotContain(previewRendererText, "var negotiatedWidth = sourceProbe.SessionActive ? sourceProbe.CurrentWidth : 0;");
        AssertDoesNotContain(previewRendererText, "var rendererWidth = negotiatedWidth > 0 ? negotiatedWidth : width;");
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
