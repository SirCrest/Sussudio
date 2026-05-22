using System.Threading.Tasks;

static partial class Program
{
    internal static Task PreviewRendererHostController_OwnsRuntimeState()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var mainWindowFamilyText = string.Join(
                "\n",
                Directory.GetFiles(Path.Combine(GetRepoRoot(), "Sussudio"), "MainWindow*.cs")
                    .Select(File.ReadAllText))
            .Replace("\r\n", "\n");
        var previewRendererText = ReadMainWindowPreviewRendererAdapterSource();
        var previewRendererHostControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs").Replace("\r\n", "\n");
        var previewRendererStartupPlanBuilderText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRendererStartupPlanBuilder.cs").Replace("\r\n", "\n");
        var statsSnapshotText = Sussudio.Tests.MainWindowStatsOverlaySource.Read();
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
        AssertContains(previewRendererText, "=> _previewRendererHostController.RendererReinitUnsafeWindows;");
        AssertContains(mainWindowText, "InitializePreviewRendererHostController();");

        AssertContains(previewRendererHostControllerText, "internal sealed class PreviewRendererHostControllerContext");
        AssertContains(previewRendererHostControllerText, "internal sealed class PreviewRendererHostController");
        AssertDoesNotContain(previewRendererHostControllerText, "partial class PreviewRendererHostController");
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
        AssertContains(previewRendererHostControllerText, "public Task StartAsync()");
        AssertContains(previewRendererHostControllerText, "RecordPreviewRendererReinitUnsafeWindow(_d3dRenderer, _context.IsPreviewReinitAnimating());");
        AssertContains(previewRendererHostControllerText, "var startupPlan = BuildPreviewRendererStartupPlan();");
        AssertContains(previewRendererHostControllerText, "_previewMinPresentationIntervalMs = startupPlan.PreviewMinPresentationIntervalMs;");
        AssertContains(previewRendererHostControllerText, "private PreviewRendererStartupPlan BuildPreviewRendererStartupPlan()");
        AssertContains(previewRendererHostControllerText, "PreviewRendererStartupPlanBuilder.Build(");
        AssertContains(previewRendererHostControllerText, "private void CleanupPreviewResources()");
        AssertContains(previewRendererHostControllerText, "_d3dRenderer = null;");
        AssertContains(previewRendererHostControllerText, "public Task StopAsync()");
        AssertContains(previewRendererHostControllerText, "private void StartD3DRenderer(PreviewRendererStartupPlan startupPlan)");
        AssertContains(previewRendererHostControllerText, "renderer.SetExpectedFrameRate(rendererFps);");
        AssertContains(previewRendererHostControllerText, "renderer.Start(rendererWidth, rendererHeight, rendererFps, isHdr);");
        AssertContains(previewRendererHostControllerText, "_context.ViewModel.SetPreviewFrameSink(_d3dRenderer);");
        AssertContains(previewRendererHostControllerText, "PreviewStartupStrategy.D3D11VideoProcessor");
        AssertContains(previewRendererHostControllerText, "_context.MarkPreviewRendererAttached();");
        AssertContains(previewRendererHostControllerText, "private D3D11PreviewRenderer CreateFreshD3DPreviewRenderer(bool replaceSwapChainSurface)");
        AssertContains(previewRendererHostControllerText, "private void OnD3DRendererFirstFrameRendered()");
        AssertContains(previewRendererHostControllerText, "private void OnD3DRendererRenderThreadFailed(string reason)");
        AssertContains(previewRendererHostControllerText, "private void StartCpuRenderer()");
        AssertContains(previewRendererHostControllerText, "_context.ViewModel.SetPreviewFrameSink(null);");
        AssertContains(previewRendererHostControllerText, "_previewSource = new SoftwareBitmapSource();");
        AssertContains(previewRendererHostControllerText, "private void RecordPreviewRendererReinitUnsafeWindow(D3D11PreviewRenderer? previousRenderer, bool reinitAnimating)");
        AssertContains(previewRendererHostControllerText, "private void MarkPreviewRendererStopped()");
        AssertContains(previewRendererHostControllerText, "public Task StopRendererForReinitTeardownAsync()");
        AssertContains(previewRendererHostControllerText, "PREVIEW_REINIT_RENDERER_STOP: stopping render thread before pipeline teardown");
        AssertContains(previewRendererHostControllerText, "catch (TimeoutException ex)");
        AssertContains(previewRendererHostControllerText, "PREVIEW_REINIT_RENDERER_STOP_TIMEOUT: {ex.Message}; continuing reinit with orphan render thread expected to exit shortly.");
        AssertContains(previewRendererHostControllerText, "public void DisposeD3DPreviewRendererForReinit()");
        AssertContains(previewRendererHostControllerText, "renderer.RetireSharedDeviceReferenceForReinit();");
        AssertContains(previewRendererHostControllerText, "private void ReplacePreviewSwapChainPanelSurface()");
        AssertContains(previewRendererHostControllerText, "D3D11_RENDERER_REINIT_UNSAFE_WINDOW");
        AssertContains(previewRendererHostControllerText, "PREVIEW_REINIT_SWAPCHAIN_PANEL_REPLACED");

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

        AssertContains(statsSnapshotText, "GetRenderer = () => _previewRendererHostController.Renderer,");
        AssertContains(statsSnapshotText, "GetPreviewMinPresentationIntervalMs = () => _previewRendererHostController.PreviewMinPresentationIntervalMs");
        AssertContains(statsSnapshotProviderText, "BuildRenderMetrics(_context.GetRenderer(), _context.GetPreviewMinPresentationIntervalMs())");
        AssertContains(statsSnapshotProviderText, "GetPresentCadenceMetrics(previewMinPresentationIntervalMs)");
        AssertDoesNotContain(agentMapText, "PreviewRendererHostController.Lifecycle.cs");
        AssertDoesNotContain(agentMapText, "PreviewRendererHostController.D3D.cs");
        AssertDoesNotContain(agentMapText, "PreviewRendererHostController.Reinit.cs");
        AssertDoesNotContain(cleanupPlanText, "PreviewRendererHostController.Lifecycle.cs");
        AssertDoesNotContain(cleanupPlanText, "PreviewRendererHostController.D3D.cs");
        AssertDoesNotContain(cleanupPlanText, "PreviewRendererHostController.Reinit.cs");

        AssertDoesNotContain(previewRendererText, "DisposeD3DPreviewRendererForReinit");
        AssertDoesNotContain(previewRendererText, "var sourceFps = ViewModel.SelectedFormat?.FrameRateExact ?? 0;");
        AssertDoesNotContain(previewRendererText, "var negotiatedWidth = sourceProbe.SessionActive ? sourceProbe.CurrentWidth : 0;");
        AssertDoesNotContain(previewRendererText, "var rendererWidth = negotiatedWidth > 0 ? negotiatedWidth : width;");
        AssertDoesNotContain(mainWindowFamilyText, "private SoftwareBitmapSource? _previewSource;");
        AssertDoesNotContain(mainWindowFamilyText, "private D3D11PreviewRenderer? _d3dRenderer;");
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
        AssertDoesNotContain(mainWindowText, "private static bool IsHdrSubtype");

        return Task.CompletedTask;
    }
}
