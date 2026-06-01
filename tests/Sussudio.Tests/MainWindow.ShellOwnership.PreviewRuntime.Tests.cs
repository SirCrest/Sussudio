using System.IO;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task PreviewResizeTelemetry_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRendererHostController.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadMainWindowCompositionSource();
        var shutdownCleanupControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowControllers.cs").Replace("\r\n", "\n");
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
        AssertDoesNotContain(shutdownCleanupControllerText, "private void MainWindow_SizeChanged(");
        AssertDoesNotContain(shutdownCleanupControllerText, "_previewLastResizeLogTick");

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
        var previewSurfaceControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs").Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewSurface.cs")),
            "preview surface XAML adapter lives with preview renderer composition");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "PreviewSurfaceShadowController.cs")),
            "preview surface shadow controller lives with preview surface presentation owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "PreviewSurfacePresentationController.cs")),
            "preview surface presentation folded into PreviewTransitionAnimationController.cs");
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
        var previewRuntimeSnapshotControllerBuildText = ExtractMemberCode(previewRuntimeSnapshotControllerText, "Build");
        var previewRuntimeSnapshotSamplingControllerText = previewRuntimeSnapshotControllerText;
        var previewRuntimeSnapshotMapperText = ExtractTextBetween(
            previewRuntimeSnapshotControllerText,
            "internal static class PreviewRuntimeSnapshotMapper",
            "internal sealed class PreviewRuntimeSnapshotHealthInput");
        var previewRuntimeSnapshotSurfaceProjectionPolicyText = previewRuntimeSnapshotControllerText;
        var previewRuntimeSnapshotStartupProjectionPolicyText = previewRuntimeSnapshotControllerText;
        var previewRuntimeSnapshotGpuPlaybackProjectionPolicyText = previewRuntimeSnapshotControllerText;
        var previewRuntimeSnapshotHealthPolicyText = previewRuntimeSnapshotControllerText;
        var previewRuntimeSnapshotHealthInputFactoryText = previewRuntimeSnapshotHealthPolicyText;
        var previewRuntimeSnapshotModelText = ReadRepoFile("Sussudio/Models/Automation/AutomationRuntimeModels.cs").Replace("\r\n", "\n");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Models", "Automation", "PreviewRuntimeSnapshot.cs")),
            "preview runtime DTO folded into AutomationRuntimeModels.cs");
        AssertContains(agentMapText, "MainWindow.Composition.cs");
        AssertContains(agentMapText, "PreviewRuntimeSnapshotController.cs");
        AssertDoesNotContain(agentMapText, "PreviewRuntimeSnapshotMapper.cs");
        AssertContains(agentMapText, "surface/startup/GPU playback projection policies");
        AssertContains(agentMapText, "health input factory");
        AssertContains(cleanupPlanText, "MainWindow.Composition.cs");
        AssertContains(cleanupPlanText, "surface/frame");
        AssertContains(cleanupPlanText, "display cadence");
        AssertContains(cleanupPlanText, "D3D renderer diagnostics");
        AssertContains(cleanupPlanText, "PreviewRuntimeSnapshotController.cs");
        AssertDoesNotContain(cleanupPlanText, "PreviewRuntimeSnapshotMapper.cs");
        AssertContains(cleanupPlanText, "surface/startup/GPU playback projection policies");
        AssertContains(cleanupPlanText, "health input factory");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "Renderer", "PreviewRuntimeSnapshotMapper.cs")),
            "Preview runtime snapshot mapper stays folded into the snapshot controller owner");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "GpuActive = d3dProjection.GpuActive,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "FramesArrived = d3dProjection.FramesArrived,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "BlankSuspected = health.BlankSuspected,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "StallSuspected = health.StallSuspected,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "StartupElapsedMs = health.StartupElapsedMs,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "StartupRecoveryAttemptCount = input.StartupRecoveryAttemptCount,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "GpuPlaybackState = d3dProjection.GpuPlaybackState,");
        AssertDoesNotContain(previewRuntimeSnapshotMapperText, "GpuPositionEventCount = input.GpuPositionEventCount");
        AssertDoesNotContain(previewRuntimeSnapshotControllerBuildText, "return new PreviewRuntimeSnapshot\n        {");
        AssertDoesNotContain(previewRuntimeSnapshotControllerBuildText, "BlankSuspected = health.BlankSuspected,");
        AssertDoesNotContain(previewRuntimeSnapshotControllerBuildText, "StallSuspected = health.StallSuspected,");
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


    internal static Task PreviewRuntimeD3DFrameCounterPolicy_PreservesCpuFallbackCounters()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DFrameCounterPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameCounterPolicy.Evaluate not found.");

        var attachedInput = Activator.CreateInstance(inputType)
                            ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(attachedInput, "D3DRenderer", null);
        SetPropertyOrBackingField(attachedInput, "PreviewSourceAttached", true);
        SetPropertyOrBackingField(attachedInput, "FramesArrived", 31L);
        SetPropertyOrBackingField(attachedInput, "FramesDisplayed", 17L);
        SetPropertyOrBackingField(attachedInput, "FramesDropped", 4L);

        var attachedCounters = evaluate.Invoke(null, new[] { attachedInput })
                               ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameCounterPolicy returned null.");
        AssertEqual(false, GetBoolProperty(attachedCounters, "GpuActive"), "CPU fallback reports GPU inactive");
        AssertEqual(true, GetBoolProperty(attachedCounters, "RendererAttached"), "CPU fallback keeps renderer attached");
        AssertEqual(31L, GetLongProperty(attachedCounters, "FramesArrived"), "CPU fallback frames arrived");
        AssertEqual(17L, GetLongProperty(attachedCounters, "FramesDisplayed"), "CPU fallback frames displayed");
        AssertEqual(4L, GetLongProperty(attachedCounters, "FramesDropped"), "CPU fallback frames dropped");
        AssertEqual(0L, GetLongProperty(attachedCounters, "D3DFramesSubmitted"), "null D3D submitted counter");
        AssertEqual(0L, GetLongProperty(attachedCounters, "D3DFramesRendered"), "null D3D rendered counter");
        AssertEqual(0L, GetLongProperty(attachedCounters, "D3DFramesDropped"), "null D3D dropped counter");

        var detachedInput = Activator.CreateInstance(inputType)
                            ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(detachedInput, "D3DRenderer", null);
        SetPropertyOrBackingField(detachedInput, "PreviewSourceAttached", false);

        var detachedCounters = evaluate.Invoke(null, new[] { detachedInput })
                               ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameCounterPolicy returned null.");
        AssertEqual(false, GetBoolProperty(detachedCounters, "RendererAttached"), "null D3D without CPU source is detached");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DProjectionBuilder_AppliesPolicyGroups()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var projectionType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DProjection");
        var build = projectionType.GetMethod("Build", BindingFlags.Public | BindingFlags.Static)
                    ?? throw new InvalidOperationException("PreviewRuntimeD3DProjection.Build not found.");

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "D3DRenderer", null);
        SetPropertyOrBackingField(input, "PreviewSourceAttached", true);
        SetPropertyOrBackingField(input, "IsPreviewing", true);
        SetPropertyOrBackingField(input, "FramesArrived", 31L);
        SetPropertyOrBackingField(input, "FramesDisplayed", 17L);
        SetPropertyOrBackingField(input, "FramesDropped", 4L);
        SetPropertyOrBackingField(input, "PreviewMinPresentationIntervalMs", 8.33d);

        var projection = build.Invoke(null, new[] { input })
                         ?? throw new InvalidOperationException("PreviewRuntimeD3DProjection.Build returned null.");
        AssertEqual(false, GetBoolProperty(projection, "GpuActive"), "builder applies frame-counter GPU state");
        AssertEqual(true, GetBoolProperty(projection, "RendererAttached"), "builder applies CPU fallback attachment");
        AssertEqual(31L, GetLongProperty(projection, "FramesArrived"), "builder applies frame-counter arrived value");
        AssertEqual("CpuSoftwareBitmap", GetStringProperty(projection, "RendererMode"), "builder applies renderer-state fallback");
        AssertEqual(0, GetIntProperty(projection, "DisplayCadenceSampleCount"), "builder applies display cadence defaults");
        AssertEqual(0d, GetDoubleProperty(projection, "D3DInputUploadCpuAvgMs"), "builder applies render CPU timing defaults");
        AssertEqual(0d, GetDoubleProperty(projection, "EstimatedPipelineLatencyMs"), "builder applies pipeline latency defaults");
        AssertEqual(false, GetBoolProperty(projection, "D3DFrameLatencyWaitEnabled"), "builder applies frame-latency wait defaults");
        AssertEqual(-1L, GetLongProperty(projection, "D3DFrameStatsPresentCount"), "builder applies frame-stat sentinels");
        AssertEqual(-1L, GetLongProperty(projection, "D3DLastSubmittedSourceSequenceNumber"), "builder applies frame-ownership sentinels");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DFrameStatisticsPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DFrameStatisticsPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameStatisticsPolicy.Evaluate not found.");

        var frameStatistics = evaluate.Invoke(null, new object[] { null! })
                              ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameStatisticsPolicy returned null.");
        AssertEqual(0L, GetLongProperty(frameStatistics, "SampleCount"), "null D3D frame-stat sample count");
        AssertEqual(0L, GetLongProperty(frameStatistics, "SuccessCount"), "null D3D frame-stat success count");
        AssertEqual(0L, GetLongProperty(frameStatistics, "FailureCount"), "null D3D frame-stat failure count");
        AssertEqual(string.Empty, GetStringProperty(frameStatistics, "LastError"), "null D3D frame-stat last error");
        AssertEqual(-1L, GetLongProperty(frameStatistics, "PresentCount"), "null D3D present-count sentinel");
        AssertEqual(-1L, GetLongProperty(frameStatistics, "PresentRefreshCount"), "null D3D present-refresh sentinel");
        AssertEqual(-1L, GetLongProperty(frameStatistics, "SyncRefreshCount"), "null D3D sync-refresh sentinel");
        AssertEqual(0L, GetLongProperty(frameStatistics, "SyncQpcTime"), "null D3D sync QPC time");
        AssertEqual(0L, GetLongProperty(frameStatistics, "LastPresentDelta"), "null D3D present delta");
        AssertEqual(0L, GetLongProperty(frameStatistics, "LastPresentRefreshDelta"), "null D3D present-refresh delta");
        AssertEqual(0L, GetLongProperty(frameStatistics, "LastSyncRefreshDelta"), "null D3D sync-refresh delta");
        AssertEqual(0L, GetLongProperty(frameStatistics, "MissedRefreshCount"), "null D3D missed-refresh count");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DFrameLatencyWaitPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DFrameLatencyWaitPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameLatencyWaitPolicy.Evaluate not found.");

        var frameLatencyWait = evaluate.Invoke(null, new object[] { null! })
                               ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameLatencyWaitPolicy returned null.");
        AssertEqual(false, GetBoolProperty(frameLatencyWait, "Enabled"), "null D3D frame-latency wait enabled");
        AssertEqual(false, GetBoolProperty(frameLatencyWait, "HandleActive"), "null D3D frame-latency wait handle active");
        AssertEqual(0L, GetLongProperty(frameLatencyWait, "CallCount"), "null D3D frame-latency wait call count");
        AssertEqual(0L, GetLongProperty(frameLatencyWait, "SignaledCount"), "null D3D frame-latency wait signaled count");
        AssertEqual(0L, GetLongProperty(frameLatencyWait, "TimeoutCount"), "null D3D frame-latency wait timeout count");
        AssertEqual(0L, GetLongProperty(frameLatencyWait, "UnexpectedResultCount"), "null D3D frame-latency wait unexpected-result count");
        AssertEqual(0u, GetPropertyValue(frameLatencyWait, "LastResult"), "null D3D frame-latency wait last result");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "LastWaitMs"), "null D3D frame-latency wait last wait");
        AssertEqual(0, GetIntProperty(frameLatencyWait, "SampleCount"), "null D3D frame-latency wait sample count");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "AverageMs"), "null D3D frame-latency wait average");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "P95Ms"), "null D3D frame-latency wait p95");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "P99Ms"), "null D3D frame-latency wait p99");
        AssertEqual(0d, GetDoubleProperty(frameLatencyWait, "MaxMs"), "null D3D frame-latency wait max");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DFrameOwnershipPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DFrameOwnershipPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameOwnershipPolicy.Evaluate not found.");

        var frameOwnership = evaluate.Invoke(null, new object[] { null! })
                             ?? throw new InvalidOperationException("PreviewRuntimeD3DFrameOwnershipPolicy returned null.");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastSubmittedPreviewPresentId"), "null D3D submitted present id");
        AssertEqual(-1L, GetLongProperty(frameOwnership, "LastSubmittedSourceSequenceNumber"), "null D3D submitted source sequence sentinel");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastSubmittedSourcePtsTicks"), "null D3D submitted source PTS");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastSubmittedQpc"), "null D3D submitted QPC");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastSubmittedUtcUnixMs"), "null D3D submitted UTC");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastRenderedPreviewPresentId"), "null D3D rendered present id");
        AssertEqual(-1L, GetLongProperty(frameOwnership, "LastRenderedSourceSequenceNumber"), "null D3D rendered source sequence sentinel");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastRenderedSourcePtsTicks"), "null D3D rendered source PTS");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastRenderedQpc"), "null D3D rendered QPC");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastRenderedUtcUnixMs"), "null D3D rendered UTC");
        AssertEqual(0d, GetDoubleProperty(frameOwnership, "LastRenderedSchedulerToPresentMs"), "null D3D scheduler-to-present");
        AssertEqual(0d, GetDoubleProperty(frameOwnership, "LastRenderedPipelineLatencyMs"), "null D3D pipeline latency");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastDroppedPreviewPresentId"), "null D3D dropped present id");
        AssertEqual(-1L, GetLongProperty(frameOwnership, "LastDroppedSourceSequenceNumber"), "null D3D dropped source sequence sentinel");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastDroppedSourcePtsTicks"), "null D3D dropped source PTS");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastDroppedQpc"), "null D3D dropped QPC");
        AssertEqual(0L, GetLongProperty(frameOwnership, "LastDroppedUtcUnixMs"), "null D3D dropped UTC");
        AssertEqual(string.Empty, GetStringProperty(frameOwnership, "LastDropReason"), "null D3D drop reason");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DRendererStatePolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DRendererStatePolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DRendererStatePolicy.Evaluate not found.");

        var previewingState = evaluate.Invoke(null, new object[] { null!, true })
                              ?? throw new InvalidOperationException("PreviewRuntimeD3DRendererStatePolicy returned null.");
        AssertEqual("CpuSoftwareBitmap", GetStringProperty(previewingState, "RendererMode"), "null D3D previewing renderer mode");
        AssertEqual(0, GetIntProperty(previewingState, "PresentSyncInterval"), "null D3D present sync interval");
        AssertEqual(0, GetIntProperty(previewingState, "MaxFrameLatency"), "null D3D max frame latency");
        AssertEqual(0, GetIntProperty(previewingState, "SwapChainBufferCount"), "null D3D swap-chain buffer count");
        AssertEqual(string.Empty, GetStringProperty(previewingState, "SwapChainAddress"), "null D3D swap-chain address");
        AssertEqual(0L, GetLongProperty(previewingState, "RenderThreadFailureCount"), "null D3D render-thread failure count");
        AssertEqual(string.Empty, GetStringProperty(previewingState, "LastRenderThreadFailureType"), "null D3D failure type");
        AssertEqual(string.Empty, GetStringProperty(previewingState, "LastRenderThreadFailureMessage"), "null D3D failure message");
        AssertEqual(0, GetIntProperty(previewingState, "LastRenderThreadFailureHResult"), "null D3D failure HRESULT");
        AssertEqual(0, GetIntProperty(previewingState, "PendingFrameCount"), "null D3D pending frame count");
        AssertEqual("None", GetStringProperty(previewingState, "InputColorSpace"), "null D3D input color space");
        AssertEqual("None", GetStringProperty(previewingState, "OutputColorSpace"), "null D3D output color space");
        var recentSlowFrames = GetPropertyValue(previewingState, "RecentSlowFrames") as Array
                               ?? throw new InvalidOperationException("RecentSlowFrames was not an array.");
        AssertEqual(0, recentSlowFrames.Length, "null D3D recent slow-frame count");
        AssertEqual("None", GetStringProperty(previewingState, "GpuPlaybackState"), "null D3D GPU playback state");
        AssertEqual(0, GetIntProperty(previewingState, "NaturalVideoWidth"), "null D3D natural video width");
        AssertEqual(0, GetIntProperty(previewingState, "NaturalVideoHeight"), "null D3D natural video height");
        AssertEqual(0d, GetDoubleProperty(previewingState, "PositionMs"), "null D3D GPU position");

        var idleState = evaluate.Invoke(null, new object[] { null!, false })
                        ?? throw new InvalidOperationException("PreviewRuntimeD3DRendererStatePolicy returned null for idle.");
        AssertEqual("None", GetStringProperty(idleState, "RendererMode"), "null D3D idle renderer mode");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DDisplayCadencePolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DDisplayCadencePolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DDisplayCadencePolicy.Evaluate not found.");

        var displayCadence = evaluate.Invoke(null, new object[] { null!, 8.33d })
                             ?? throw new InvalidOperationException("PreviewRuntimeD3DDisplayCadencePolicy returned null.");
        AssertEqual(0, GetIntProperty(displayCadence, "SampleCount"), "null D3D display cadence sample count");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "ObservedFps"), "null D3D display cadence observed fps");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "ExpectedIntervalMs"), "null D3D display cadence expected interval");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "AverageIntervalMs"), "null D3D display cadence average interval");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "P95IntervalMs"), "null D3D display cadence p95 interval");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "P99IntervalMs"), "null D3D display cadence p99 interval");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "MaxIntervalMs"), "null D3D display cadence max interval");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "OnePercentLowFps"), "null D3D display cadence one-percent low");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "FivePercentLowFps"), "null D3D display cadence five-percent low");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "SampleDurationMs"), "null D3D display cadence sample duration");
        var recentIntervals = GetPropertyValue(displayCadence, "RecentIntervalsMs") as Array
                              ?? throw new InvalidOperationException("RecentIntervalsMs was not an array.");
        AssertEqual(0, recentIntervals.Length, "null D3D display cadence recent interval count");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "JitterStdDevMs"), "null D3D display cadence jitter");
        AssertEqual(0L, GetLongProperty(displayCadence, "SlowFrameCount"), "null D3D display cadence slow-frame count");
        AssertEqual(0d, GetDoubleProperty(displayCadence, "SlowFramePercent"), "null D3D display cadence slow-frame percent");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DRenderCpuTimingPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DRenderCpuTimingPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DRenderCpuTimingPolicy.Evaluate not found.");

        var renderCpuTiming = evaluate.Invoke(null, new object[] { null! })
                              ?? throw new InvalidOperationException("PreviewRuntimeD3DRenderCpuTimingPolicy returned null.");
        AssertEqual(0, GetIntProperty(renderCpuTiming, "SampleCount"), "null D3D render CPU timing sample count");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "InputUploadAverageMs"), "null D3D input-upload average");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "InputUploadP95Ms"), "null D3D input-upload p95");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "InputUploadP99Ms"), "null D3D input-upload p99");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "InputUploadMaxMs"), "null D3D input-upload max");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "RenderSubmitAverageMs"), "null D3D render-submit average");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "RenderSubmitP95Ms"), "null D3D render-submit p95");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "RenderSubmitP99Ms"), "null D3D render-submit p99");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "RenderSubmitMaxMs"), "null D3D render-submit max");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "PresentCallAverageMs"), "null D3D present-call average");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "PresentCallP95Ms"), "null D3D present-call p95");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "PresentCallP99Ms"), "null D3D present-call p99");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "PresentCallMaxMs"), "null D3D present-call max");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "TotalFrameAverageMs"), "null D3D total-frame average");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "TotalFrameP95Ms"), "null D3D total-frame p95");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "TotalFrameP99Ms"), "null D3D total-frame p99");
        AssertEqual(0d, GetDoubleProperty(renderCpuTiming, "TotalFrameMaxMs"), "null D3D total-frame max");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeD3DPipelineLatencyPolicy_PreservesNullRendererDefaults()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DPipelineLatencyPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeD3DPipelineLatencyPolicy.Evaluate not found.");

        var pipelineLatency = evaluate.Invoke(null, new object[] { null! })
                              ?? throw new InvalidOperationException("PreviewRuntimeD3DPipelineLatencyPolicy returned null.");
        AssertEqual(0, GetIntProperty(pipelineLatency, "SampleCount"), "null D3D pipeline latency sample count");
        AssertEqual(0d, GetDoubleProperty(pipelineLatency, "AverageMs"), "null D3D pipeline latency average");
        AssertEqual(0d, GetDoubleProperty(pipelineLatency, "P95Ms"), "null D3D pipeline latency p95");
        AssertEqual(0d, GetDoubleProperty(pipelineLatency, "P99Ms"), "null D3D pipeline latency p99");
        AssertEqual(0d, GetDoubleProperty(pipelineLatency, "MaxMs"), "null D3D pipeline latency max");
        AssertEqual(0d, GetDoubleProperty(pipelineLatency, "EstimatedPipelineLatencyMs"), "null estimated pipeline latency");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotHealthPolicy_PreservesSuspicionRules()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealthInput");
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealthPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthPolicy.Evaluate not found.");
        var now = DateTimeOffset.UtcNow;

        var cpuPathInput = Activator.CreateInstance(inputType)
                           ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealthInput.");
        SetPropertyOrBackingField(cpuPathInput, "IsPreviewing", true);
        SetPropertyOrBackingField(cpuPathInput, "IsStartupWaitingForFirstVisual", true);
        SetPropertyOrBackingField(cpuPathInput, "StartupRequestedUtc", now.AddMilliseconds(-2000));
        SetPropertyOrBackingField(cpuPathInput, "StartupTimeoutMs", 1000);
        SetPropertyOrBackingField(cpuPathInput, "RendererAttached", true);
        SetPropertyOrBackingField(cpuPathInput, "GpuActive", false);
        SetPropertyOrBackingField(cpuPathInput, "FramesArrived", 31L);
        SetPropertyOrBackingField(cpuPathInput, "FramesDisplayed", 0L);
        SetPropertyOrBackingField(cpuPathInput, "LastPresentedTick", 1000L);
        SetPropertyOrBackingField(cpuPathInput, "CurrentTick", 4001L);
        SetPropertyOrBackingField(cpuPathInput, "UtcNow", now);

        var cpuPathHealth = evaluate.Invoke(null, new[] { cpuPathInput })
                            ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthPolicy returned null.");
        AssertEqual(true, GetDoubleProperty(cpuPathHealth, "StartupElapsedMs") >= 2000, "startup elapsed uses supplied clock");
        AssertEqual(true, GetBoolProperty(cpuPathHealth, "BlankSuspected"), "CPU path blank suspected");
        AssertEqual(true, GetBoolProperty(cpuPathHealth, "StallSuspected"), "CPU path stall suspected");

        var gpuPathInput = Activator.CreateInstance(inputType)
                           ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealthInput.");
        SetPropertyOrBackingField(gpuPathInput, "IsPreviewing", true);
        SetPropertyOrBackingField(gpuPathInput, "RendererAttached", true);
        SetPropertyOrBackingField(gpuPathInput, "GpuActive", true);
        SetPropertyOrBackingField(gpuPathInput, "FramesArrived", 31L);
        SetPropertyOrBackingField(gpuPathInput, "FramesDisplayed", 0L);
        SetPropertyOrBackingField(gpuPathInput, "LastPresentedTick", 1000L);
        SetPropertyOrBackingField(gpuPathInput, "CurrentTick", 4001L);
        SetPropertyOrBackingField(gpuPathInput, "UtcNow", now);

        var gpuPathHealth = evaluate.Invoke(null, new[] { gpuPathInput })
                            ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthPolicy returned null.");
        AssertEqual(false, GetBoolProperty(gpuPathHealth, "BlankSuspected"), "GPU path does not use CPU blank suspicion");
        AssertEqual(false, GetBoolProperty(gpuPathHealth, "StallSuspected"), "GPU path does not use CPU stall suspicion");

        var timeoutInput = Activator.CreateInstance(inputType)
                           ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealthInput.");
        SetPropertyOrBackingField(timeoutInput, "IsPreviewing", true);
        SetPropertyOrBackingField(timeoutInput, "IsStartupWaitingForFirstVisual", true);
        SetPropertyOrBackingField(timeoutInput, "StartupRequestedUtc", now.AddMilliseconds(-1500));
        SetPropertyOrBackingField(timeoutInput, "StartupTimeoutMs", 1000);
        SetPropertyOrBackingField(timeoutInput, "RendererAttached", true);
        SetPropertyOrBackingField(timeoutInput, "GpuActive", false);
        SetPropertyOrBackingField(timeoutInput, "FramesArrived", 0L);
        SetPropertyOrBackingField(timeoutInput, "FramesDisplayed", 0L);
        SetPropertyOrBackingField(timeoutInput, "CurrentTick", 4001L);
        SetPropertyOrBackingField(timeoutInput, "UtcNow", now);

        var timeoutHealth = evaluate.Invoke(null, new[] { timeoutInput })
                            ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthPolicy returned null.");
        AssertEqual(true, GetBoolProperty(timeoutHealth, "BlankSuspected"), "startup timeout marks blank suspected");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotHealthInputFactory_ProjectsControllerInputs()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var projectionType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DProjection");
        var factoryType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealthInputFactory");
        var build = factoryType.GetMethod("Build", BindingFlags.Public | BindingFlags.Static)
                    ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthInputFactory.Build not found.");
        var now = DateTimeOffset.UtcNow;

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "IsPreviewing", true);
        SetPropertyOrBackingField(input, "IsStartupWaitingForFirstVisual", true);
        SetPropertyOrBackingField(input, "StartupRequestedUtc", now.AddMilliseconds(-2500));
        SetPropertyOrBackingField(input, "StartupTimeoutMs", 1200);
        SetPropertyOrBackingField(input, "LastPresentedTick", 42L);

        var projection = Activator.CreateInstance(projectionType)
                         ?? throw new InvalidOperationException("Failed to create PreviewRuntimeD3DProjection.");
        SetPropertyOrBackingField(projection, "RendererAttached", true);
        SetPropertyOrBackingField(projection, "GpuActive", false);
        SetPropertyOrBackingField(projection, "FramesArrived", 55L);
        SetPropertyOrBackingField(projection, "FramesDisplayed", 6L);

        var healthInput = build.Invoke(null, new object[] { input, projection, 999L, now })
                          ?? throw new InvalidOperationException("PreviewRuntimeSnapshotHealthInputFactory returned null.");
        AssertEqual(true, GetBoolProperty(healthInput, "IsPreviewing"), "health input previewing");
        AssertEqual(true, GetBoolProperty(healthInput, "IsStartupWaitingForFirstVisual"), "health input waiting for first visual");
        AssertEqual(GetPropertyValue(input, "StartupRequestedUtc"), GetPropertyValue(healthInput, "StartupRequestedUtc"), "health input startup request time");
        AssertEqual(1200, GetIntProperty(healthInput, "StartupTimeoutMs"), "health input startup timeout");
        AssertEqual(true, GetBoolProperty(healthInput, "RendererAttached"), "health input renderer attached");
        AssertEqual(false, GetBoolProperty(healthInput, "GpuActive"), "health input GPU active");
        AssertEqual(55L, GetLongProperty(healthInput, "FramesArrived"), "health input frames arrived");
        AssertEqual(6L, GetLongProperty(healthInput, "FramesDisplayed"), "health input frames displayed");
        AssertEqual(42L, GetLongProperty(healthInput, "LastPresentedTick"), "health input last presented tick");
        AssertEqual(999L, GetLongProperty(healthInput, "CurrentTick"), "health input current tick");
        AssertEqual(now, GetPropertyValue(healthInput, "UtcNow"), "health input clock");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotSurfaceProjectionPolicy_PreservesVisibilityAndHealthFields()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var projectionType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DProjection");
        var healthType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealth");
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotSurfaceProjectionPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeSnapshotSurfaceProjectionPolicy.Evaluate not found.");

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "IsPreviewing", true);
        SetPropertyOrBackingField(input, "PlaceholderVisible", false);
        SetPropertyOrBackingField(input, "GpuElementVisible", true);
        SetPropertyOrBackingField(input, "CpuElementVisible", false);

        var d3dProjection = Activator.CreateInstance(projectionType)
                            ?? throw new InvalidOperationException("Failed to create PreviewRuntimeD3DProjection.");
        SetPropertyOrBackingField(d3dProjection, "GpuActive", true);
        SetPropertyOrBackingField(d3dProjection, "RendererAttached", true);
        SetPropertyOrBackingField(d3dProjection, "FramesArrived", 101L);
        SetPropertyOrBackingField(d3dProjection, "FramesDisplayed", 99L);
        SetPropertyOrBackingField(d3dProjection, "FramesDropped", 2L);

        var health = Activator.CreateInstance(healthType, new object?[] { null, true, false })
                     ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealth.");
        var surface = evaluate.Invoke(null, new object?[] { input, d3dProjection, health })
                      ?? throw new InvalidOperationException("PreviewRuntimeSnapshotSurfaceProjectionPolicy returned null.");

        AssertEqual(true, GetBoolProperty(surface, "IsPreviewing"), "surface projection previewing");
        AssertEqual(true, GetBoolProperty(surface, "GpuActive"), "surface projection GPU active");
        AssertEqual(false, GetBoolProperty(surface, "PlaceholderVisible"), "surface projection placeholder visible");
        AssertEqual(true, GetBoolProperty(surface, "GpuElementVisible"), "surface projection GPU element visible");
        AssertEqual(false, GetBoolProperty(surface, "CpuElementVisible"), "surface projection CPU element visible");
        AssertEqual(true, GetBoolProperty(surface, "RendererAttached"), "surface projection renderer attached");
        AssertEqual(101L, GetLongProperty(surface, "FramesArrived"), "surface projection frames arrived");
        AssertEqual(99L, GetLongProperty(surface, "FramesDisplayed"), "surface projection frames displayed");
        AssertEqual(2L, GetLongProperty(surface, "FramesDropped"), "surface projection frames dropped");
        AssertEqual(true, GetBoolProperty(surface, "BlankSuspected"), "surface projection blank suspected");
        AssertEqual(false, GetBoolProperty(surface, "StallSuspected"), "surface projection stall suspected");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotStartupProjectionPolicy_PreservesSampledStartupFields()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var healthType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotHealth");
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotStartupProjectionPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeSnapshotStartupProjectionPolicy.Evaluate not found.");
        var requiredSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", "FirstVisual");
        var receivedSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", "MediaOpened");
        var startupStrategy = ParseEnum("Sussudio.Models.PreviewStartupStrategy", "D3D11VideoProcessor");

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "StartupState", "WaitingForFirstVisual");
        SetPropertyOrBackingField(input, "StartupAttemptId", "attempt-42");
        SetPropertyOrBackingField(input, "StartupTimeoutMs", 1250);
        SetPropertyOrBackingField(input, "StartupGpuSignalMediaOpened", true);
        SetPropertyOrBackingField(input, "StartupGpuSignalFirstFrame", false);
        SetPropertyOrBackingField(input, "StartupGpuSignalPlaybackAdvancing", true);
        SetPropertyOrBackingField(input, "StartupRequiredSignals", requiredSignals);
        SetPropertyOrBackingField(input, "StartupReceivedSignals", receivedSignals);
        SetPropertyOrBackingField(input, "StartupStrategy", startupStrategy);
        SetPropertyOrBackingField(input, "StartupMissingSignals", "FirstVisual");
        SetPropertyOrBackingField(input, "StartupRecoveryAttemptCount", 5);
        SetPropertyOrBackingField(input, "StartupLastFailureReason", "visual-timeout");
        SetPropertyOrBackingField(input, "FirstVisualConfirmed", true);

        var health = Activator.CreateInstance(healthType, new object?[] { 456.25d, true, false })
                     ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotHealth.");
        var startup = evaluate.Invoke(null, new object?[] { input, health })
                      ?? throw new InvalidOperationException("PreviewRuntimeSnapshotStartupProjectionPolicy returned null.");

        AssertEqual("WaitingForFirstVisual", GetStringProperty(startup, "State"), "startup projection state");
        AssertEqual("attempt-42", GetStringProperty(startup, "AttemptId"), "startup projection attempt id");
        AssertEqual(456.25d, GetDoubleProperty(startup, "ElapsedMs"), "startup projection elapsed");
        AssertEqual(1250, GetIntProperty(startup, "TimeoutMs"), "startup projection timeout");
        AssertEqual(true, GetBoolProperty(startup, "GpuSignalMediaOpened"), "startup projection media opened signal");
        AssertEqual(false, GetBoolProperty(startup, "GpuSignalFirstFrame"), "startup projection first frame signal");
        AssertEqual(true, GetBoolProperty(startup, "GpuSignalPlaybackAdvancing"), "startup projection playback signal");
        AssertEqual(requiredSignals, GetPropertyValue(startup, "RequiredSignals"), "startup projection required signals");
        AssertEqual(receivedSignals, GetPropertyValue(startup, "ReceivedSignals"), "startup projection received signals");
        AssertEqual(startupStrategy, GetPropertyValue(startup, "Strategy"), "startup projection strategy");
        AssertEqual("FirstVisual", GetStringProperty(startup, "MissingSignals"), "startup projection missing signals");
        AssertEqual(5, GetIntProperty(startup, "RecoveryAttemptCount"), "startup projection recovery count");
        AssertEqual("visual-timeout", GetStringProperty(startup, "LastFailureReason"), "startup projection failure reason");
        AssertEqual(true, GetBoolProperty(startup, "FirstVisualConfirmed"), "startup projection first visual confirmed");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy_PreservesRendererAndEventFields()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var projectionType = RequireType("Sussudio.Controllers.PreviewRuntimeD3DProjection");
        var policyType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy");
        var evaluate = policyType.GetMethod("Evaluate", BindingFlags.Public | BindingFlags.Static)
                       ?? throw new InvalidOperationException("PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy.Evaluate not found.");

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "GpuPositionEventCount", 42L);

        var d3dProjection = Activator.CreateInstance(projectionType)
                            ?? throw new InvalidOperationException("Failed to create PreviewRuntimeD3DProjection.");
        SetPropertyOrBackingField(d3dProjection, "GpuPlaybackState", "Rendering");
        SetPropertyOrBackingField(d3dProjection, "GpuNaturalVideoWidth", 3840);
        SetPropertyOrBackingField(d3dProjection, "GpuNaturalVideoHeight", 2160);
        SetPropertyOrBackingField(d3dProjection, "GpuPositionMs", 1234.5d);

        var gpuPlayback = evaluate.Invoke(null, new object?[] { input, d3dProjection })
                          ?? throw new InvalidOperationException("PreviewRuntimeSnapshotGpuPlaybackProjectionPolicy returned null.");

        AssertEqual("Rendering", GetStringProperty(gpuPlayback, "PlaybackState"), "GPU playback projection state");
        AssertEqual(3840, GetIntProperty(gpuPlayback, "NaturalVideoWidth"), "GPU playback projection natural width");
        AssertEqual(2160, GetIntProperty(gpuPlayback, "NaturalVideoHeight"), "GPU playback projection natural height");
        AssertEqual(1234.5d, GetDoubleProperty(gpuPlayback, "PositionMs"), "GPU playback projection position");
        AssertEqual(42L, GetLongProperty(gpuPlayback, "PositionEventCount"), "GPU playback projection event count");

        return Task.CompletedTask;
    }

    internal static Task PreviewRuntimeSnapshotController_PreservesNullD3dProjectionPolicy()
    {
        var inputType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotInput");
        var controllerType = RequireType("Sussudio.Controllers.PreviewRuntimeSnapshotController");
        var build = controllerType.GetMethod("Build", BindingFlags.Public | BindingFlags.Static)
                    ?? throw new InvalidOperationException("PreviewRuntimeSnapshotController.Build not found.");
        var requiredSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", "FirstVisual");
        var receivedSignals = ParseEnum("Sussudio.Models.PreviewStartupSignalFlags", "None");
        var startupStrategy = ParseEnum("Sussudio.Models.PreviewStartupStrategy", "D3D11VideoProcessor");

        var input = Activator.CreateInstance(inputType)
                    ?? throw new InvalidOperationException("Failed to create PreviewRuntimeSnapshotInput.");
        SetPropertyOrBackingField(input, "D3DRenderer", null);
        SetPropertyOrBackingField(input, "IsPreviewing", true);
        SetPropertyOrBackingField(input, "PreviewSourceAttached", true);
        SetPropertyOrBackingField(input, "GpuElementVisible", false);
        SetPropertyOrBackingField(input, "CpuElementVisible", true);
        SetPropertyOrBackingField(input, "PlaceholderVisible", false);
        SetPropertyOrBackingField(input, "FramesArrived", 31L);
        SetPropertyOrBackingField(input, "FramesDisplayed", 0L);
        SetPropertyOrBackingField(input, "FramesDropped", 2L);
        SetPropertyOrBackingField(input, "LastPresentedTick", Environment.TickCount64 - 4000);
        SetPropertyOrBackingField(input, "PreviewMinPresentationIntervalMs", 8.33d);
        SetPropertyOrBackingField(input, "StartupState", "WaitingForFirstVisual");
        SetPropertyOrBackingField(input, "IsStartupWaitingForFirstVisual", true);
        SetPropertyOrBackingField(input, "StartupAttemptId", "attempt-1");
        SetPropertyOrBackingField(input, "StartupRequestedUtc", DateTimeOffset.UtcNow.AddMilliseconds(-2000));
        SetPropertyOrBackingField(input, "StartupTimeoutMs", 1000);
        SetPropertyOrBackingField(input, "StartupGpuSignalMediaOpened", true);
        SetPropertyOrBackingField(input, "StartupGpuSignalFirstFrame", false);
        SetPropertyOrBackingField(input, "StartupGpuSignalPlaybackAdvancing", false);
        SetPropertyOrBackingField(input, "StartupRequiredSignals", requiredSignals);
        SetPropertyOrBackingField(input, "StartupReceivedSignals", receivedSignals);
        SetPropertyOrBackingField(input, "StartupStrategy", startupStrategy);
        SetPropertyOrBackingField(input, "StartupMissingSignals", "FirstVisual");
        SetPropertyOrBackingField(input, "StartupRecoveryAttemptCount", 3);
        SetPropertyOrBackingField(input, "StartupLastFailureReason", "timeout");
        SetPropertyOrBackingField(input, "FirstVisualConfirmed", false);
        SetPropertyOrBackingField(input, "GpuPositionEventCount", 7L);

        var snapshot = build.Invoke(null, new[] { input })
                       ?? throw new InvalidOperationException("PreviewRuntimeSnapshotController.Build returned null.");

        AssertEqual(true, GetBoolProperty(snapshot, "IsPreviewing"), "snapshot IsPreviewing");
        AssertEqual(false, GetBoolProperty(snapshot, "GpuActive"), "snapshot GpuActive");
        AssertEqual(true, GetBoolProperty(snapshot, "RendererAttached"), "snapshot RendererAttached");
        AssertEqual(false, GetBoolProperty(snapshot, "GpuElementVisible"), "snapshot GpuElementVisible");
        AssertEqual(true, GetBoolProperty(snapshot, "CpuElementVisible"), "snapshot CpuElementVisible");
        AssertEqual("CpuSoftwareBitmap", GetStringProperty(snapshot, "RendererMode"), "CPU renderer mode");
        AssertEqual("WaitingForFirstVisual", GetStringProperty(snapshot, "StartupState"), "startup state passthrough");
        AssertEqual("attempt-1", GetStringProperty(snapshot, "StartupAttemptId"), "startup attempt passthrough");
        AssertEqual("FirstVisual", GetStringProperty(snapshot, "StartupMissingSignals"), "missing signals passthrough");
        AssertEqual(requiredSignals, GetPropertyValue(snapshot, "StartupRequiredSignals"), "required startup signals");
        AssertEqual(receivedSignals, GetPropertyValue(snapshot, "StartupReceivedSignals"), "received startup signals");
        AssertEqual(startupStrategy, GetPropertyValue(snapshot, "StartupStrategy"), "startup strategy");
        AssertEqual(3, GetIntProperty(snapshot, "StartupRecoveryAttemptCount"), "startup recovery count");
        AssertEqual("timeout", GetStringProperty(snapshot, "StartupLastFailureReason"), "startup failure reason");
        AssertEqual(true, GetBoolProperty(snapshot, "StartupGpuSignalMediaOpened"), "media opened signal");
        AssertEqual(false, GetBoolProperty(snapshot, "StartupGpuSignalFirstFrame"), "first-frame signal");
        AssertEqual(false, GetBoolProperty(snapshot, "StartupGpuSignalPlaybackAdvancing"), "playback advancing signal");
        AssertEqual(true, GetDoubleProperty(snapshot, "StartupElapsedMs") >= 0, "startup elapsed is non-negative");
        AssertEqual(true, GetBoolProperty(snapshot, "BlankSuspected"), "blank suspected when CPU path receives frames but displays none");
        AssertEqual(true, GetBoolProperty(snapshot, "StallSuspected"), "stall suspected after stale last-presented tick");
        AssertEqual(31L, GetLongProperty(snapshot, "FramesArrived"), "frames arrived passthrough");
        AssertEqual(0L, GetLongProperty(snapshot, "FramesDisplayed"), "frames displayed passthrough");
        AssertEqual(2L, GetLongProperty(snapshot, "FramesDropped"), "frames dropped passthrough");
        AssertEqual(0, GetIntProperty(snapshot, "DisplayCadenceSampleCount"), "no D3D cadence samples");
        AssertEqual(-1L, GetLongProperty(snapshot, "D3DFrameStatsPresentCount"), "D3D present-count sentinel");
        AssertEqual(-1L, GetLongProperty(snapshot, "D3DFrameStatsPresentRefreshCount"), "D3D present-refresh sentinel");
        AssertEqual(-1L, GetLongProperty(snapshot, "D3DFrameStatsSyncRefreshCount"), "D3D sync-refresh sentinel");
        AssertEqual("None", GetStringProperty(snapshot, "D3DInputColorSpace"), "D3D input color fallback");
        AssertEqual("None", GetStringProperty(snapshot, "D3DOutputColorSpace"), "D3D output color fallback");
        AssertEqual("None", GetStringProperty(snapshot, "GpuPlaybackState"), "GPU playback fallback");
        AssertEqual(7L, GetLongProperty(snapshot, "GpuPositionEventCount"), "GPU position event count");

        return Task.CompletedTask;
    }
}
