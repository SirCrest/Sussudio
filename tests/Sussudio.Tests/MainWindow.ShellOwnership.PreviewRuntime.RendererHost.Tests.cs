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
        var previewRendererHostLifecycleText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.Lifecycle.cs").Replace("\r\n", "\n");
        var previewRendererHostD3dText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.D3D.cs").Replace("\r\n", "\n");
        var previewRendererHostReinitText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.Reinit.cs").Replace("\r\n", "\n");
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
        AssertContains(previewRendererHostReinitText, "public Task StopRendererForReinitTeardownAsync()");
        AssertContains(previewRendererHostReinitText, "PREVIEW_REINIT_RENDERER_STOP: stopping render thread before pipeline teardown");
        AssertContains(previewRendererHostReinitText, "catch (TimeoutException ex)");
        AssertContains(previewRendererHostReinitText, "PREVIEW_REINIT_RENDERER_STOP_TIMEOUT: {ex.Message}; continuing reinit with orphan render thread expected to exit shortly.");
        AssertContains(previewRendererHostReinitText, "public void DisposeD3DPreviewRendererForReinit()");
        AssertContains(previewRendererHostReinitText, "renderer.RetireSharedDeviceReferenceForReinit();");
        AssertContains(previewRendererHostReinitText, "private void ReplacePreviewSwapChainPanelSurface()");
        AssertContains(previewRendererHostReinitText, "D3D11_RENDERER_REINIT_UNSAFE_WINDOW");
        AssertContains(previewRendererHostReinitText, "PREVIEW_REINIT_SWAPCHAIN_PANEL_REPLACED");

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
        AssertContains(agentMapText, "PreviewRendererHostController.Lifecycle.cs");
        AssertContains(agentMapText, "PreviewRendererHostController.D3D.cs");
        AssertContains(agentMapText, "PreviewRendererHostController.Reinit.cs");
        AssertContains(cleanupPlanText, "PreviewRendererHostController.Lifecycle.cs");
        AssertContains(cleanupPlanText, "PreviewRendererHostController.D3D.cs");
        AssertContains(cleanupPlanText, "PreviewRendererHostController.Reinit.cs");

        AssertDoesNotContain(previewRendererText, "DisposeD3DPreviewRendererForReinit");
        AssertDoesNotContain(previewRendererText, "var sourceFps = ViewModel.SelectedFormat?.FrameRateExact ?? 0;");
        AssertDoesNotContain(previewRendererText, "var negotiatedWidth = sourceProbe.SessionActive ? sourceProbe.CurrentWidth : 0;");
        AssertDoesNotContain(previewRendererText, "var rendererWidth = negotiatedWidth > 0 ? negotiatedWidth : width;");
        AssertDoesNotContain(mainWindowFamilyText, "private SoftwareBitmapSource? _previewSource;");
        AssertDoesNotContain(mainWindowFamilyText, "private D3D11PreviewRenderer? _d3dRenderer;");
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
        AssertDoesNotContain(mainWindowText, "private static bool IsHdrSubtype");

        return Task.CompletedTask;
    }
}
