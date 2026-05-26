using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task PreviewResizeTelemetry_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs").Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.ShellChrome.Composition.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadMainWindowCompositionSource();
        var shutdownCleanupControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseLifecycleController.cs").Replace("\r\n", "\n");
        var previewRendererText = ReadMainWindowPreviewRendererAdapterSource();

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

    internal static Task PreviewRendererStartupPlanBuilder_PreservesFallbackPolicy()
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

    internal static Task PreviewSurfacePresentationAndShadow_LiveInControllers()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewRendererText = ReadMainWindowPreviewRendererAdapterSource();
        var previewSurfaceControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewSurfacePresentationController.cs").Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewSurface.cs")),
            "preview surface XAML adapter lives with preview renderer composition");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "PreviewSurfaceShadowController.cs")),
            "preview surface shadow controller lives with preview surface presentation owner");
        AssertContains(previewRendererText, "XAML-facing preview surface adapter");
        AssertContains(previewRendererText, "private PreviewSurfacePresentationController _previewSurfacePresentationController = null!;");
        AssertContains(previewRendererText, "private PreviewSurfaceShadowController _previewSurfaceShadowController = null!;");
        AssertContains(previewRendererText, "private void InitializePreviewSurfacePresentationController()");
        AssertContains(previewRendererText, "private void UpdateVideoContentOverlays()");
        AssertContains(previewRendererText, "private void SetupVideoFrameShadow()");
        AssertContains(previewRendererText, "private void SetupControlBarShadow()");
        AssertContains(previewRendererText, "=> _previewSurfacePresentationController.UpdateVideoContentOverlays(ViewModel.SourceWidth, ViewModel.SourceHeight);");
        AssertContains(previewRendererText, "=> _previewSurfacePresentationController.SetGpuPreviewVisibility(visibility);");
        AssertContains(previewRendererText, "=> _previewSurfaceShadowController.SetupVideoFrameShadow();");
        AssertContains(previewRendererText, "=> _previewSurfaceShadowController.SetupControlBarShadow();");
        AssertContains(previewRendererText, "=> _previewSurfaceShadowController.ClearVideoFrameShadow();");
        AssertContains(previewRendererText, "=> _previewSurfaceShadowController.FadeInVideoFrameShadow(delayMs, durationMs);");
        AssertContains(previewRendererText, "var scale = PreviewSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;");
        AssertContains(previewRendererText, "_previewRendererHostController.OnPanelSizeChanged(e.NewSize.Width, e.NewSize.Height, scale);");

        AssertContains(previewSurfaceControllerText, "internal sealed class PreviewSurfacePresentationController");
        AssertContains(previewSurfaceControllerText, "public required Func<SwapChainPanel> GetPreviewSwapChainPanel { get; init; }");
        AssertContains(previewSurfaceControllerText, "private readonly PreviewSurfaceShadowController _shadowController;");
        AssertContains(previewSurfaceControllerText, "PreviewSurfaceShadowController shadowController)");
        AssertContains(previewSurfaceControllerText, "var previewSwapChainPanel = _context.GetPreviewSwapChainPanel();");
        AssertContains(previewSurfaceControllerText, "public void UpdateVideoContentOverlays(int? sourceWidth, int? sourceHeight)");
        AssertContains(previewSurfaceControllerText, "_shadowController.ClearVideoFrameBounds();");
        AssertContains(previewSurfaceControllerText, "_shadowController.UpdateVideoFrameBounds(marginH, marginV, fitW, fitH);");

        AssertContains(previewSurfaceControllerText, "internal sealed class PreviewSurfaceShadowController");
        AssertContains(previewSurfaceControllerText, "private SpriteVisual? _videoShadowVisual;");
        AssertContains(previewSurfaceControllerText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertContains(previewSurfaceControllerText, "public void UpdateVideoFrameBounds(double marginH, double marginV, double fitW, double fitH)");
        AssertContains(previewSurfaceControllerText, "public void ClearVideoFrameBounds()");
        AssertContains(previewSurfaceControllerText, "_videoShadowVisual.Size = Vector2.Zero;");
        AssertContains(previewSurfaceControllerText, "public void SetupVideoFrameShadow()");
        AssertContains(previewSurfaceControllerText, "public void SetupControlBarShadow()");
        AssertContains(previewSurfaceControllerText, "public void ClearVideoFrameShadow()");
        AssertContains(previewSurfaceControllerText, "public void FadeInVideoFrameShadow(int delayMs, int durationMs)");
        AssertContains(previewSurfaceControllerText, "public void FadeInControlBarShadow(int delayMs, int durationMs)");

        AssertDoesNotContain(mainWindowText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(mainWindowText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _controlBarShadowVisual;");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DProjection_OwnsPolicyGroups()
    {
        var previewRuntimeD3DProjectionText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeD3DProjection.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

        AssertContains(previewRuntimeD3DProjectionText, "internal sealed class PreviewRuntimeD3DProjection");
        AssertContains(previewRuntimeD3DProjectionText, "public bool GpuActive { get; private set; }");
        AssertContains(previewRuntimeD3DProjectionText, "public long D3DFramesDropped { get; private set; }");
        AssertContains(previewRuntimeD3DProjectionText, "private void ApplyFrameCounters(PreviewRuntimeD3DFrameCounters frameCounters)");
        AssertContains(previewRuntimeD3DProjectionText, "FramesArrived = frameCounters.FramesArrived;");
        AssertContains(previewRuntimeD3DProjectionText, "public string RendererMode { get; private set; } = \"None\";");
        AssertContains(previewRuntimeD3DProjectionText, "public PreviewSlowFrameDiagnostic[] D3DRecentSlowFrames { get; private set; } = Array.Empty<PreviewSlowFrameDiagnostic>();");
        AssertContains(previewRuntimeD3DProjectionText, "public string GpuPlaybackState { get; private set; } = \"None\";");
        AssertContains(previewRuntimeD3DProjectionText, "private void ApplyRendererState(PreviewRuntimeD3DRendererState rendererState)");
        AssertContains(previewRuntimeD3DProjectionText, "GpuPlaybackState = rendererState.GpuPlaybackState;");
        AssertContains(previewRuntimeD3DProjectionText, "public double[] DisplayCadenceRecentIntervalsMs { get; private set; } = Array.Empty<double>();");
        AssertContains(previewRuntimeD3DProjectionText, "private void ApplyDisplayCadence(PreviewRuntimeD3DDisplayCadence displayCadence)");
        AssertContains(previewRuntimeD3DProjectionText, "DisplayCadenceRecentIntervalsMs = displayCadence.RecentIntervalsMs;");
        AssertContains(previewRuntimeD3DProjectionText, "public double D3DInputUploadCpuAvgMs { get; private set; }");
        AssertContains(previewRuntimeD3DProjectionText, "private void ApplyRenderCpuTiming(PreviewRuntimeD3DRenderCpuTiming renderCpuTiming)");
        AssertContains(previewRuntimeD3DProjectionText, "D3DInputUploadCpuAvgMs = renderCpuTiming.InputUploadAverageMs;");
        AssertContains(previewRuntimeD3DProjectionText, "public double EstimatedPipelineLatencyMs { get; private set; }");
        AssertContains(previewRuntimeD3DProjectionText, "private void ApplyPipelineLatency(PreviewRuntimeD3DPipelineLatency pipelineLatency)");
        AssertContains(previewRuntimeD3DProjectionText, "EstimatedPipelineLatencyMs = pipelineLatency.EstimatedPipelineLatencyMs;");
        AssertContains(previewRuntimeD3DProjectionText, "public long D3DLastSubmittedPreviewPresentId { get; private set; }");
        AssertContains(previewRuntimeD3DProjectionText, "private void ApplyFrameOwnership(PreviewRuntimeD3DFrameOwnership frameOwnership)");
        AssertContains(previewRuntimeD3DProjectionText, "D3DLastSubmittedSourceSequenceNumber = frameOwnership.LastSubmittedSourceSequenceNumber;");
        AssertContains(previewRuntimeD3DProjectionText, "public long D3DFrameStatsPresentCount { get; private set; }");
        AssertContains(previewRuntimeD3DProjectionText, "private void ApplyFrameStatistics(PreviewRuntimeD3DFrameStatistics frameStatistics)");
        AssertContains(previewRuntimeD3DProjectionText, "D3DFrameStatsPresentCount = frameStatistics.PresentCount;");
        AssertContains(previewRuntimeD3DProjectionText, "public bool D3DFrameLatencyWaitEnabled { get; private set; }");
        AssertContains(previewRuntimeD3DProjectionText, "private void ApplyFrameLatencyWait(PreviewRuntimeD3DFrameLatencyWait frameLatencyWait)");
        AssertContains(previewRuntimeD3DProjectionText, "D3DFrameLatencyWaitEnabled = frameLatencyWait.Enabled;");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DProjection Build(PreviewRuntimeSnapshotInput input)");
        AssertContains(previewRuntimeD3DProjectionText, "var frameCounters = PreviewRuntimeD3DFrameCounterPolicy.Evaluate(input);");
        AssertContains(previewRuntimeD3DProjectionText, "var d3d = input.D3DRenderer;");
        AssertContains(previewRuntimeD3DProjectionText, "var rendererState = PreviewRuntimeD3DRendererStatePolicy.Evaluate(d3d, input.IsPreviewing);");
        AssertContains(previewRuntimeD3DProjectionText, "var displayCadence = PreviewRuntimeD3DDisplayCadencePolicy.Evaluate(d3d, input.PreviewMinPresentationIntervalMs);");
        AssertContains(previewRuntimeD3DProjectionText, "var renderCpuTiming = PreviewRuntimeD3DRenderCpuTimingPolicy.Evaluate(d3d);");
        AssertContains(previewRuntimeD3DProjectionText, "var pipelineLatency = PreviewRuntimeD3DPipelineLatencyPolicy.Evaluate(d3d);");
        AssertContains(previewRuntimeD3DProjectionText, "var frameOwnership = PreviewRuntimeD3DFrameOwnershipPolicy.Evaluate(d3d);");
        AssertContains(previewRuntimeD3DProjectionText, "var frameStatistics = PreviewRuntimeD3DFrameStatisticsPolicy.Evaluate(d3d);");
        AssertContains(previewRuntimeD3DProjectionText, "var frameLatencyWait = PreviewRuntimeD3DFrameLatencyWaitPolicy.Evaluate(d3d);");
        AssertContains(previewRuntimeD3DProjectionText, "var projection = new PreviewRuntimeD3DProjection();");
        AssertContains(previewRuntimeD3DProjectionText, "projection.ApplyFrameCounters(frameCounters);");
        AssertContains(previewRuntimeD3DProjectionText, "projection.ApplyRendererState(rendererState);");
        AssertContains(previewRuntimeD3DProjectionText, "projection.ApplyDisplayCadence(displayCadence);");
        AssertContains(previewRuntimeD3DProjectionText, "projection.ApplyRenderCpuTiming(renderCpuTiming);");
        AssertContains(previewRuntimeD3DProjectionText, "projection.ApplyPipelineLatency(pipelineLatency);");
        AssertContains(previewRuntimeD3DProjectionText, "projection.ApplyFrameLatencyWait(frameLatencyWait);");
        AssertContains(previewRuntimeD3DProjectionText, "projection.ApplyFrameStatistics(frameStatistics);");
        AssertContains(previewRuntimeD3DProjectionText, "projection.ApplyFrameOwnership(frameOwnership);");
        AssertContains(previewRuntimeD3DProjectionText, "return projection;");
        AssertContains(previewRuntimeD3DProjectionText, "internal static class PreviewRuntimeD3DFrameCounterPolicy");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DFrameCounters Evaluate(PreviewRuntimeSnapshotInput input)");
        AssertContains(previewRuntimeD3DProjectionText, "FramesArrived: gpuActive ? d3dFramesSubmitted : input.FramesArrived,");
        AssertContains(previewRuntimeD3DProjectionText, "internal static class PreviewRuntimeD3DRendererStatePolicy");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DRendererState Evaluate(D3D11PreviewRenderer? d3d, bool isPreviewing)");
        AssertContains(previewRuntimeD3DProjectionText, "RendererMode: d3d?.RendererMode ?? (isPreviewing ? \"CpuSoftwareBitmap\" : \"None\"),");
        AssertContains(previewRuntimeD3DProjectionText, "RecentSlowFrames: d3d?.GetRecentSlowFrameDiagnostics() ?? Array.Empty<PreviewSlowFrameDiagnostic>(),");
        AssertContains(previewRuntimeD3DProjectionText, "internal static class PreviewRuntimeD3DDisplayCadencePolicy");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DDisplayCadence Evaluate(");
        AssertContains(previewRuntimeD3DProjectionText, "RecentIntervalsMs: displayCadence?.RecentIntervalsMs ?? Array.Empty<double>(),");
        AssertContains(previewRuntimeD3DProjectionText, "internal static class PreviewRuntimeD3DRenderCpuTimingPolicy");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DRenderCpuTiming Evaluate(D3D11PreviewRenderer? d3d)");
        AssertContains(previewRuntimeD3DProjectionText, "SampleCount: renderCpuTiming?.TotalFrame.SampleCount ?? 0,");
        AssertContains(previewRuntimeD3DProjectionText, "internal static class PreviewRuntimeD3DPipelineLatencyPolicy");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DPipelineLatency Evaluate(D3D11PreviewRenderer? d3d)");
        AssertContains(previewRuntimeD3DProjectionText, "EstimatedPipelineLatencyMs: pipelineLatency?.AverageMs ?? 0);");
        AssertContains(previewRuntimeD3DProjectionText, "internal static class PreviewRuntimeD3DFrameStatisticsPolicy");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DFrameStatistics Evaluate(D3D11PreviewRenderer? d3d)");
        AssertContains(previewRuntimeD3DProjectionText, "PresentCount: frameStats?.PresentCount ?? -1,");
        AssertContains(previewRuntimeD3DProjectionText, "internal static class PreviewRuntimeD3DFrameLatencyWaitPolicy");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DFrameLatencyWait Evaluate(D3D11PreviewRenderer? d3d)");
        AssertContains(previewRuntimeD3DProjectionText, "SampleCount: frameLatencyWait?.Timing.SampleCount ?? 0,");
        AssertContains(previewRuntimeD3DProjectionText, "internal static class PreviewRuntimeD3DFrameOwnershipPolicy");
        AssertContains(previewRuntimeD3DProjectionText, "public static PreviewRuntimeD3DFrameOwnership Evaluate(D3D11PreviewRenderer? d3d)");
        AssertContains(previewRuntimeD3DProjectionText, "LastSubmittedSourceSequenceNumber: frameOwnership?.LastSubmittedSourceSequenceNumber ?? -1,");
        AssertContains(previewRuntimeD3DProjectionText, "LastDroppedSourceSequenceNumber: frameOwnership?.LastDroppedSourceSequenceNumber ?? -1,");

        AssertContains(agentMapText, "PreviewRuntimeD3DProjection.cs");
        AssertContains(agentMapText, "owns the renderer projection data contract, D3D policy records");
        AssertContains(agentMapText, "assignment from evaluated policy records");
        AssertContains(cleanupPlanText, "PreviewRuntimeD3DProjection.cs");
        AssertContains(cleanupPlanText, "renderer projection data contract, D3D policy records");
        AssertContains(cleanupPlanText, "evaluated policy records");
        foreach (var removedFile in new[]
        {
            "PreviewRuntimeD3DFrameCounterPolicy.cs",
            "PreviewRuntimeD3DRendererStatePolicy.cs",
            "PreviewRuntimeD3DDisplayCadencePolicy.cs",
            "PreviewRuntimeD3DRenderCpuTimingPolicy.cs",
            "PreviewRuntimeD3DPipelineLatencyPolicy.cs",
            "PreviewRuntimeD3DFrameOwnershipPolicy.cs",
            "PreviewRuntimeD3DFrameStatisticsPolicy.cs",
            "PreviewRuntimeD3DFrameLatencyWaitPolicy.cs"
        })
        {
            AssertEqual(
                false,
                File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "Renderer", removedFile)),
                $"{removedFile} folded into PreviewRuntimeD3DProjection.cs");
        }

        return Task.CompletedTask;
    }

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
        var previewRendererStartupPlanBuilderText = previewRendererHostControllerText;
        var statsSnapshotText = Sussudio.Tests.MainWindowStatsOverlaySource.Read();
        var statsSnapshotProviderText = ReadRepoFile("Sussudio/Controllers/Stats/StatsOverlayCompositionController.cs").Replace("\r\n", "\n");
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

    internal static Task PreviewRuntimeSnapshotController_OwnsSnapshotMapping()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewRendererText = ReadMainWindowPreviewRendererAdapterSource();
        var previewRuntimeSnapshotText = previewRendererText;
        var previewRuntimeSnapshotInitialization = ExtractMemberCode(previewRuntimeSnapshotText, "InitializePreviewRuntimeSnapshotSamplingController");
        var previewRuntimeSnapshotControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotController.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotSamplingControllerText = previewRuntimeSnapshotControllerText;
        var previewRuntimeSnapshotMapperText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotMapper.cs").Replace("\r\n", "\n");
        var previewRuntimeSnapshotSurfaceProjectionPolicyText = previewRuntimeSnapshotMapperText;
        var previewRuntimeSnapshotStartupProjectionPolicyText = previewRuntimeSnapshotMapperText;
        var previewRuntimeSnapshotGpuPlaybackProjectionPolicyText = previewRuntimeSnapshotMapperText;
        var previewRuntimeSnapshotHealthPolicyText = previewRuntimeSnapshotControllerText;
        var previewRuntimeSnapshotHealthInputFactoryText = previewRuntimeSnapshotHealthPolicyText;
        var previewRuntimeSnapshotModelText = ReadRepoFile("Sussudio/Models/Automation/PreviewRuntimeSnapshot.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

        AssertContains(previewRuntimeSnapshotText, "private PreviewRuntimeSnapshotSamplingController _previewRuntimeSnapshotSamplingController = null!;");
        AssertContains(previewRuntimeSnapshotText, "private void InitializePreviewRuntimeSnapshotSamplingController()");
        AssertContains(previewRuntimeSnapshotText, "UiDispatchController = WindowUiDispatchController,");
        AssertContains(previewRuntimeSnapshotText, "RendererHostController = _previewRendererHostController,");
        AssertContains(previewRuntimeSnapshotText, "StartupSessionController = _previewStartupSessionController,");
        AssertContains(previewRuntimeSnapshotText, "StartupSignalCoordinator = _previewStartupSignalCoordinator,");
        AssertContains(previewRuntimeSnapshotText, "IsGpuElementVisible = () => PreviewSwapChainPanel.Visibility == Visibility.Visible,");
        AssertContains(previewRuntimeSnapshotText, "GetStartupVisualTimeoutMs = () => PreviewStartupVisualTimeoutMs");
        AssertContains(previewRuntimeSnapshotText, "private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewRuntimeSnapshotText, "=> await _previewRuntimeSnapshotSamplingController.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(mainWindowText, "InitializePreviewRuntimeSnapshotSamplingController();");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "internal sealed class PreviewRuntimeSnapshotSamplingControllerContext");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "public required WindowUiDispatchController UiDispatchController { get; init; }");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "public required PreviewRendererHostController RendererHostController { get; init; }");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "public required PreviewStartupSessionController StartupSessionController { get; init; }");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "public required PreviewStartupSignalCoordinator StartupSignalCoordinator { get; init; }");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "internal sealed class PreviewRuntimeSnapshotSamplingController");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "public Task<PreviewRuntimeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "_context.UiDispatchController.InvokeWithRetryAsync(");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "BuildSnapshot,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "\"Failed to enqueue preview snapshot operation.\",");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "private PreviewRuntimeSnapshot BuildSnapshot()");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "var startupSignalSnapshot = startupSignals.Snapshot;");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "startupSession.ShouldRefreshMissingSignalsForSnapshot");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "startupMissingSignals = startupSignals.BuildMissingSignals();");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "return PreviewRuntimeSnapshotController.Build(new PreviewRuntimeSnapshotInput");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "D3DRenderer = rendererHost.Renderer,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "PreviewSourceAttached = rendererHost.IsCpuPreviewSourceAttached,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "GpuElementVisible = _context.IsGpuElementVisible(),");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "FramesArrived = rendererHost.FramesArrived,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "PreviewMinPresentationIntervalMs = rendererHost.PreviewMinPresentationIntervalMs,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "StartupState = startupSession.State.ToString(),");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "IsStartupWaitingForFirstVisual = startupSession.IsWaitingForFirstVisual,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "StartupGpuSignalMediaOpened = startupSignalSnapshot.GpuSignalMediaOpened,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "GpuPositionEventCount = startupSignals.PositionEventCount");
        AssertContains(previewRuntimeSnapshotControllerText, "internal static class PreviewRuntimeSnapshotController");
        AssertContains(previewRuntimeSnapshotControllerText, "internal sealed class PreviewRuntimeSnapshotInput");
        AssertContains(previewRuntimeSnapshotControllerText, "public D3D11PreviewRenderer? D3DRenderer { get; init; }");
        AssertContains(previewRuntimeSnapshotControllerText, "public PreviewStartupSignalFlags StartupRequiredSignals { get; init; }");
        AssertContains(previewRuntimeSnapshotControllerText, "public long GpuPositionEventCount { get; init; }");
        AssertContains(previewRuntimeSnapshotControllerText, "public static PreviewRuntimeSnapshot Build(PreviewRuntimeSnapshotInput input)");
        AssertContains(previewRuntimeSnapshotControllerText, "var d3dProjection = PreviewRuntimeD3DProjection.Build(input);");
        AssertContains(previewRuntimeSnapshotControllerText, "var healthInput = PreviewRuntimeSnapshotHealthInputFactory.Build(");
        AssertContains(previewRuntimeSnapshotControllerText, "Environment.TickCount64,");
        AssertContains(previewRuntimeSnapshotControllerText, "var health = PreviewRuntimeSnapshotHealthPolicy.Evaluate(healthInput);");
        AssertContains(previewRuntimeSnapshotControllerText, "return PreviewRuntimeSnapshotMapper.Build(input, d3dProjection, health, DateTimeOffset.UtcNow);");
        AssertContains(previewRuntimeSnapshotMapperText, "internal static class PreviewRuntimeSnapshotMapper");
        AssertContains(previewRuntimeSnapshotMapperText, "public static PreviewRuntimeSnapshot Build(");
        AssertContains(previewRuntimeSnapshotMapperText, "var surface = PreviewRuntimeSnapshotSurfaceProjectionPolicy.Evaluate(input, d3dProjection, health);");
        AssertContains(previewRuntimeSnapshotMapperText, "var startup = PreviewRuntimeSnapshotStartupProjectionPolicy.Evaluate(input, health);");
        AssertContains(previewRuntimeSnapshotMapperText, "var gpuPlayback = PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy.Evaluate(input, d3dProjection);");
        AssertContains(previewRuntimeSnapshotMapperText, "return new PreviewRuntimeSnapshot");
        AssertContains(previewRuntimeSnapshotMapperText, "TimestampUtc = timestampUtc,");
        AssertContains(previewRuntimeSnapshotMapperText, "IsPreviewing = surface.IsPreviewing,");
        AssertContains(previewRuntimeSnapshotMapperText, "FramesArrived = surface.FramesArrived,");
        AssertContains(previewRuntimeSnapshotMapperText, "StartupState = startup.State,");
        AssertContains(previewRuntimeSnapshotMapperText, "StartupElapsedMs = startup.ElapsedMs,");
        AssertContains(previewRuntimeSnapshotMapperText, "BlankSuspected = surface.BlankSuspected,");
        AssertContains(previewRuntimeSnapshotMapperText, "StallSuspected = surface.StallSuspected,");
        AssertContains(previewRuntimeSnapshotMapperText, "GpuPlaybackState = gpuPlayback.PlaybackState,");
        AssertContains(previewRuntimeSnapshotMapperText, "GpuPositionEventCount = gpuPlayback.PositionEventCount");
        AssertContains(previewRuntimeSnapshotSurfaceProjectionPolicyText, "internal static class PreviewRuntimeSnapshotSurfaceProjectionPolicy");
        AssertContains(previewRuntimeSnapshotSurfaceProjectionPolicyText, "public static PreviewRuntimeSnapshotSurfaceProjection Evaluate(");
        AssertContains(previewRuntimeSnapshotSurfaceProjectionPolicyText, "GpuActive: d3dProjection.GpuActive,");
        AssertContains(previewRuntimeSnapshotSurfaceProjectionPolicyText, "BlankSuspected: health.BlankSuspected,");
        AssertContains(previewRuntimeSnapshotStartupProjectionPolicyText, "internal static class PreviewRuntimeSnapshotStartupProjectionPolicy");
        AssertContains(previewRuntimeSnapshotStartupProjectionPolicyText, "public static PreviewRuntimeSnapshotStartupProjection Evaluate(");
        AssertContains(previewRuntimeSnapshotStartupProjectionPolicyText, "ElapsedMs: health.StartupElapsedMs,");
        AssertContains(previewRuntimeSnapshotStartupProjectionPolicyText, "RecoveryAttemptCount: input.StartupRecoveryAttemptCount,");
        AssertContains(previewRuntimeSnapshotGpuPlaybackProjectionPolicyText, "internal static class PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy");
        AssertContains(previewRuntimeSnapshotGpuPlaybackProjectionPolicyText, "public static PreviewRuntimeSnapshotGpuPlaybackProjection Evaluate(");
        AssertContains(previewRuntimeSnapshotGpuPlaybackProjectionPolicyText, "PlaybackState: d3dProjection.GpuPlaybackState,");
        AssertContains(previewRuntimeSnapshotGpuPlaybackProjectionPolicyText, "PositionEventCount: input.GpuPositionEventCount);");
        AssertContains(previewRuntimeSnapshotHealthInputFactoryText, "internal static class PreviewRuntimeSnapshotHealthInputFactory");
        AssertContains(previewRuntimeSnapshotHealthInputFactoryText, "public static PreviewRuntimeSnapshotHealthInput Build(");
        AssertContains(previewRuntimeSnapshotHealthInputFactoryText, "RendererAttached = d3dProjection.RendererAttached,");
        AssertContains(previewRuntimeSnapshotHealthInputFactoryText, "CurrentTick = currentTick,");
        AssertContains(previewRuntimeSnapshotHealthInputFactoryText, "UtcNow = utcNow");
        AssertContains(previewRuntimeSnapshotHealthPolicyText, "internal static class PreviewRuntimeSnapshotHealthPolicy");
        AssertContains(previewRuntimeSnapshotHealthPolicyText, "public static PreviewRuntimeSnapshotHealth Evaluate(PreviewRuntimeSnapshotHealthInput input)");
        AssertContains(previewRuntimeSnapshotHealthPolicyText, "var startupTimedOut = input.IsPreviewing");
        AssertContains(previewRuntimeSnapshotHealthPolicyText, "input.FramesArrived > 30");
        AssertContains(previewRuntimeSnapshotHealthPolicyText, "input.CurrentTick - input.LastPresentedTick > 3000");
        AssertContains(previewRuntimeSnapshotModelText, "public sealed class PreviewRuntimeSnapshot");
        AssertContains(previewRuntimeSnapshotModelText, "public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;");
        AssertContains(previewRuntimeSnapshotModelText, "public bool RendererAttached { get; init; }");
        AssertContains(previewRuntimeSnapshotModelText, "public string StartupState { get; init; } = \"Idle\";");
        AssertContains(previewRuntimeSnapshotModelText, "public PreviewStartupSignalFlags StartupRequiredSignals { get; init; }");
        AssertContains(previewRuntimeSnapshotModelText, "public double[] DisplayCadenceRecentIntervalsMs { get; init; } = Array.Empty<double>();");
        AssertContains(previewRuntimeSnapshotModelText, "public string RendererMode { get; init; } = \"None\";");
        AssertContains(previewRuntimeSnapshotModelText, "public string D3DSwapChainAddress { get; init; } = string.Empty;");
        AssertContains(previewRuntimeSnapshotModelText, "public PreviewSlowFrameDiagnostic[] D3DRecentSlowFrames { get; init; } = Array.Empty<PreviewSlowFrameDiagnostic>();");
        AssertContains(previewRuntimeSnapshotModelText, "public string GpuPlaybackState { get; init; } = \"None\";");
        AssertDoesNotContain(previewRuntimeSnapshotModelText, "partial class PreviewRuntimeSnapshot");
        AssertContains(agentMapText, "MainWindow.PreviewLifecycle.Composition.cs");
        AssertContains(agentMapText, "PreviewRuntimeSnapshotController.cs");
        AssertContains(agentMapText, "PreviewRuntimeSnapshotMapper.cs");
        AssertContains(agentMapText, "surface/startup/GPU playback projection policies");
        AssertContains(agentMapText, "health input factory");
        AssertContains(cleanupPlanText, "MainWindow.PreviewLifecycle.Composition.cs");
        AssertContains(cleanupPlanText, "surface/frame");
        AssertContains(cleanupPlanText, "display cadence");
        AssertContains(cleanupPlanText, "D3D renderer diagnostics");
        AssertContains(cleanupPlanText, "PreviewRuntimeSnapshotController.cs");
        AssertContains(cleanupPlanText, "PreviewRuntimeSnapshotMapper.cs");
        AssertContains(cleanupPlanText, "surface/startup/GPU playback projection policies");
        AssertContains(cleanupPlanText, "health input factory");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "GpuActive = d3dProjection.GpuActive,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "FramesArrived = d3dProjection.FramesArrived,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "BlankSuspected = health.BlankSuspected,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "StallSuspected = health.StallSuspected,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "StartupElapsedMs = health.StartupElapsedMs,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "StartupRecoveryAttemptCount = input.StartupRecoveryAttemptCount,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "GpuPlaybackState = d3dProjection.GpuPlaybackState,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "GpuPositionEventCount = input.GpuPositionEventCount");
        AssertDoesNotContain(previewRuntimeSnapshotControllerText, "return new PreviewRuntimeSnapshot\n        {");
        AssertDoesNotContain(previewRuntimeSnapshotControllerText, "BlankSuspected = health.BlankSuspected,");
        AssertDoesNotContain(previewRuntimeSnapshotControllerText, "StallSuspected = health.StallSuspected,");
        AssertDoesNotContain(previewRuntimeSnapshotText, "TaskCompletionSource<PreviewRuntimeSnapshot>");
        AssertDoesNotContain(previewRuntimeSnapshotText, "return new PreviewRuntimeSnapshot");
        AssertDoesNotContain(previewRuntimeSnapshotText, "new PreviewRuntimeSnapshotInput");
        AssertDoesNotContain(previewRuntimeSnapshotInitialization, "BuildPreviewStartupMissingSignals()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "FramesArrived = _previewRendererHostController.FramesArrived,");
        AssertDoesNotContain(previewRuntimeSnapshotSamplingControllerText, "TaskCompletionSource<PreviewRuntimeSnapshot>");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetRenderCpuTimingMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetFrameOwnershipMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetDxgiFrameStatisticsMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetFrameLatencyWaitMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "GetPipelineLatencyMetrics()");
        AssertDoesNotContain(previewRuntimeSnapshotText, "_dispatcherQueue.TryEnqueue");
        AssertDoesNotContain(previewRuntimeSnapshotText, "const int maxAttempts = 3;");
        AssertDoesNotContain(previewRuntimeSnapshotText, "completion.TrySetResult(GetPreviewRuntimeSnapshot());");
        AssertDoesNotContain(previewRuntimeSnapshotText, "await Task.Delay(50, cancellationToken).ConfigureAwait(false);");
        AssertDoesNotContain(previewRuntimeSnapshotText, "CurrentPreviewStartupState is PreviewStartupState.WaitingForFirstVisual or PreviewStartupState.Failed");
        AssertDoesNotContain(previewRuntimeSnapshotText, "IsStartupWaitingForFirstVisual = CurrentPreviewStartupState == PreviewStartupState.WaitingForFirstVisual");
        AssertDoesNotContain(mainWindowText, "private async Task<PreviewRuntimeSnapshot> GetPreviewRuntimeSnapshotAsync");
        AssertDoesNotContain(previewRendererText, "private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "Renderer", "PreviewRuntimeSnapshotSamplingController.cs")),
            "preview runtime snapshot sampling lives with the snapshot controller");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewRuntimeSnapshot.cs")),
            "preview runtime snapshot adapter lives with the preview renderer composition");

        return Task.CompletedTask;
    }
}
