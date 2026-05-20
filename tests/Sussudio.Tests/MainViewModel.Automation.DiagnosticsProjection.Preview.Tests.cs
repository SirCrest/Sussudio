using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationDiagnosticsPreviewRuntimeProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningPreviewRuntimeText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.cs")
            .Replace("\r\n", "\n");
        var previewRuntimeProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var previewSummary = BuildPreviewRuntimeProjection(previewRuntime, previewHdrState, captureRuntime);");
        AssertContains(snapshotFlatteningText, "var previewRuntimeFlattening = BuildPreviewRuntimeFlattenedProjection(previewSummary);");
        AssertContains(snapshotFlatteningText, "PreviewFramesArrived = previewRuntimeFlattening.Frame.FramesArrived,");
        AssertContains(snapshotFlatteningText, "EstimatedPipelineLatencyMs = previewRuntimeFlattening.Frame.EstimatedPipelineLatencyMs,");
        AssertContains(snapshotFlatteningText, "PreviewCadenceOnePercentLowFps = previewRuntimeFlattening.Cadence.OnePercentLowFps,");
        AssertContains(snapshotFlatteningText, "PreviewStartupStrategy = previewRuntimeFlattening.Startup.Strategy,");
        AssertContains(snapshotFlatteningText, "PreviewRendererMode = previewRuntimeFlattening.Startup.RendererMode,");
        AssertContains(snapshotFlatteningText, "PreviewGpuPlaybackState = previewRuntimeFlattening.GpuPlayback.PlaybackState,");
        AssertContains(snapshotFlatteningText, "PreviewColorContext = previewRuntimeFlattening.Color.ColorContext,");
        AssertContains(snapshotFlatteningText, "PreviewAdapterColorMetadata = previewRuntimeFlattening.Color.AdapterColorMetadata,");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "private static PreviewRuntimeFlattenedProjection BuildPreviewRuntimeFlattenedProjection(");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "Frame = BuildPreviewRuntimeFrameFlattenedProjection(previewSummary.Frame),");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "Cadence = BuildPreviewRuntimeCadenceFlattenedProjection(previewSummary.Cadence),");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "Surface = BuildPreviewRuntimeSurfaceFlattenedProjection(previewSummary.Surface),");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "Startup = BuildPreviewRuntimeStartupFlattenedProjection(previewSummary.Startup),");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "GpuPlayback = BuildPreviewRuntimeGpuPlaybackFlattenedProjection(previewSummary.GpuPlayback),");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "Color = BuildPreviewRuntimeColorFlattenedProjection(previewSummary.Color)");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "private readonly record struct PreviewRuntimeFlattenedProjection");

        var previewRuntimeFrameProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Frame.cs")
            .Replace("\r\n", "\n");
        var previewRuntimeCadenceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Cadence.cs")
            .Replace("\r\n", "\n");
        var previewRuntimeSurfaceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Surface.cs")
            .Replace("\r\n", "\n");
        var previewRuntimeStartupProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Startup.cs")
            .Replace("\r\n", "\n");
        var previewRuntimeGpuPlaybackProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.GpuPlayback.cs")
            .Replace("\r\n", "\n");
        var previewRuntimeColorProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.Color.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningPreviewRuntimeFrameText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.Frame.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningPreviewRuntimeCadenceText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.Cadence.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningPreviewRuntimeSurfaceText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.Surface.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningPreviewRuntimeStartupText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.Startup.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningPreviewRuntimeGpuPlaybackText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.GpuPlayback.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningPreviewRuntimeColorText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewRuntime.Color.cs")
            .Replace("\r\n", "\n");

        AssertContains(previewRuntimeFrameProjectionText, "private static PreviewRuntimeFrameProjection BuildPreviewRuntimeFrameProjection(");
        AssertContains(previewRuntimeFrameProjectionText, "FramesArrived = previewRuntime.FramesArrived,");
        AssertContains(previewRuntimeFrameProjectionText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs");
        AssertContains(previewRuntimeCadenceProjectionText, "private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(");
        AssertContains(previewRuntimeCadenceProjectionText, "OnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertContains(previewRuntimeCadenceProjectionText, "RecentIntervalsMs = previewRuntime.DisplayCadenceRecentIntervalsMs,");
        AssertContains(previewRuntimeCadenceProjectionText, "SlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent");
        AssertContains(previewRuntimeSurfaceProjectionText, "private static PreviewRuntimeSurfaceProjection BuildPreviewRuntimeSurfaceProjection(");
        AssertContains(previewRuntimeSurfaceProjectionText, "RendererAttached = previewRuntime.RendererAttached");
        AssertContains(previewRuntimeStartupProjectionText, "private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(");
        AssertContains(previewRuntimeStartupProjectionText, "Strategy = previewRuntime.StartupStrategy.ToString(),");
        AssertContains(previewRuntimeStartupProjectionText, "RendererMode = previewRuntime.RendererMode");
        AssertContains(previewRuntimeGpuPlaybackProjectionText, "private static PreviewRuntimeGpuPlaybackProjection BuildPreviewRuntimeGpuPlaybackProjection(");
        AssertContains(previewRuntimeGpuPlaybackProjectionText, "PlaybackState = previewRuntime.GpuPlaybackState,");
        AssertContains(previewRuntimeGpuPlaybackProjectionText, "PositionEventCount = previewRuntime.GpuPositionEventCount");
        AssertContains(previewRuntimeColorProjectionText, "private static PreviewRuntimeColorProjection BuildPreviewRuntimeColorProjection(");
        AssertContains(previewRuntimeColorProjectionText, "HdrInputDetected = previewHdrState.InputDetected,");
        AssertContains(previewRuntimeColorProjectionText, "AdapterColorMetadata = captureRuntime.PreviewColorMetadata");

        AssertContains(snapshotFlatteningPreviewRuntimeFrameText, "private static PreviewRuntimeFrameFlattenedProjection BuildPreviewRuntimeFrameFlattenedProjection(");
        AssertContains(snapshotFlatteningPreviewRuntimeFrameText, "FramesArrived = frame.FramesArrived,");
        AssertContains(snapshotFlatteningPreviewRuntimeFrameText, "EstimatedPipelineLatencyMs = frame.EstimatedPipelineLatencyMs");
        AssertContains(snapshotFlatteningPreviewRuntimeCadenceText, "private static PreviewRuntimeCadenceFlattenedProjection BuildPreviewRuntimeCadenceFlattenedProjection(");
        AssertContains(snapshotFlatteningPreviewRuntimeCadenceText, "OnePercentLowFps = cadence.OnePercentLowFps,");
        AssertContains(snapshotFlatteningPreviewRuntimeCadenceText, "SlowFramePercent = cadence.SlowFramePercent");
        AssertContains(snapshotFlatteningPreviewRuntimeSurfaceText, "private static PreviewRuntimeSurfaceFlattenedProjection BuildPreviewRuntimeSurfaceFlattenedProjection(");
        AssertContains(snapshotFlatteningPreviewRuntimeSurfaceText, "RendererAttached = surface.RendererAttached");
        AssertContains(snapshotFlatteningPreviewRuntimeStartupText, "private static PreviewRuntimeStartupFlattenedProjection BuildPreviewRuntimeStartupFlattenedProjection(");
        AssertContains(snapshotFlatteningPreviewRuntimeStartupText, "Strategy = startup.Strategy,");
        AssertContains(snapshotFlatteningPreviewRuntimeStartupText, "RendererMode = startup.RendererMode");
        AssertContains(snapshotFlatteningPreviewRuntimeGpuPlaybackText, "private static PreviewRuntimeGpuPlaybackFlattenedProjection BuildPreviewRuntimeGpuPlaybackFlattenedProjection(");
        AssertContains(snapshotFlatteningPreviewRuntimeGpuPlaybackText, "PlaybackState = gpuPlayback.PlaybackState,");
        AssertContains(snapshotFlatteningPreviewRuntimeGpuPlaybackText, "PositionEventCount = gpuPlayback.PositionEventCount");
        AssertContains(snapshotFlatteningPreviewRuntimeColorText, "private static PreviewRuntimeColorFlattenedProjection BuildPreviewRuntimeColorFlattenedProjection(");
        AssertContains(snapshotFlatteningPreviewRuntimeColorText, "ColorContext = color.ColorContext,");
        AssertContains(snapshotFlatteningPreviewRuntimeColorText, "AdapterColorMetadata = color.AdapterColorMetadata");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewFramesArrived = previewRuntime.FramesArrived,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewFramesArrived = previewSummary.FramesArrived,");
        AssertDoesNotContain(snapshotFlatteningText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "EstimatedPipelineLatencyMs = previewSummary.EstimatedPipelineLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewCadenceOnePercentLowFps = previewSummary.CadenceOnePercentLowFps,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewCadenceOnePercentLowFps = previewSummary.Cadence.OnePercentLowFps,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewCadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewStartupStrategy = previewSummary.StartupStrategy,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewStartupStrategy = previewSummary.Startup.Strategy,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewRendererMode = previewSummary.RendererMode,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewRendererMode = previewSummary.Startup.RendererMode,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewStartupStrategy = previewRuntime.StartupStrategy.ToString(),");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewGpuPlaybackState = previewRuntime.GpuPlaybackState,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewGpuPlaybackState = previewSummary.GpuPlaybackState,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewColorContext = captureRuntime.NegotiatedPixelFormat,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewColorContext = previewSummary.ColorContext,");

        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeProjection BuildPreviewRuntimeProjection(");
        AssertContains(previewRuntimeProjectionText, "Frame = BuildPreviewRuntimeFrameProjection(previewRuntime),");
        AssertContains(previewRuntimeProjectionText, "Cadence = BuildPreviewRuntimeCadenceProjection(previewRuntime),");
        AssertContains(previewRuntimeProjectionText, "Surface = BuildPreviewRuntimeSurfaceProjection(previewRuntime),");
        AssertContains(previewRuntimeProjectionText, "Startup = BuildPreviewRuntimeStartupProjection(previewRuntime),");
        AssertContains(previewRuntimeProjectionText, "GpuPlayback = BuildPreviewRuntimeGpuPlaybackProjection(previewRuntime),");
        AssertContains(previewRuntimeProjectionText, "Color = BuildPreviewRuntimeColorProjection(previewHdrState, captureRuntime)");
        AssertDoesNotContain(previewRuntimeProjectionText, "CadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertDoesNotContain(previewRuntimeProjectionText, "CadenceSlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent,");
        AssertDoesNotContain(previewRuntimeProjectionText, "StartupStrategy = previewRuntime.StartupStrategy.ToString(),");
        AssertDoesNotContain(previewRuntimeProjectionText, "RendererMode = previewRuntime.RendererMode,");
        AssertDoesNotContain(previewRuntimeProjectionText, "GpuPlaybackState = previewRuntime.GpuPlaybackState,");
        AssertDoesNotContain(previewRuntimeProjectionText, "HdrInputDetected = previewHdrState.InputDetected,");
        AssertDoesNotContain(previewRuntimeProjectionText, "ColorContext = captureRuntime.NegotiatedPixelFormat,");
        AssertContains(previewRuntimeProjectionText, "private readonly record struct PreviewRuntimeProjection");
        AssertContains(previewRuntimeProjectionText, "public PreviewRuntimeFrameProjection Frame { get; init; }");
        AssertContains(previewRuntimeProjectionText, "public PreviewRuntimeColorProjection Color { get; init; }");

        return Task.CompletedTask;
    }

    internal static Task AutomationDiagnosticsPreviewD3DProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningPreviewD3DText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningPreviewD3DCpuTimingText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.CpuTiming.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningPreviewD3DLatencyAndStatsText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.LatencyAndStats.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningPreviewD3DFrameFlowText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.FrameFlow.cs")
            .Replace("\r\n", "\n");
        var previewD3DProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs")
            .Replace("\r\n", "\n");
        var previewD3DFrameFlowProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.FrameFlow.cs")
            .Replace("\r\n", "\n");
        var previewD3DFrameLatencyWaitProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.FrameLatencyWait.cs")
            .Replace("\r\n", "\n");
        var previewD3DFrameStatsProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.FrameStats.cs")
            .Replace("\r\n", "\n");
        var previewD3DPipelineLatencyProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.PipelineLatency.cs")
            .Replace("\r\n", "\n");
        var previewD3DCpuTimingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DCpuTiming.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var previewD3D = BuildPreviewD3DProjection(\n            previewRuntime,\n            recentD3DMissedRefreshes,\n            recentD3DStatsFailures);");
        AssertContains(snapshotFlatteningText, "var previewD3DFlattening = BuildPreviewD3DFlattenedProjection(previewD3D);");
        AssertContains(snapshotFlatteningText, "PreviewD3DPresentSyncInterval = previewD3DFlattening.PresentSyncInterval,");
        AssertContains(snapshotFlatteningText, "PreviewD3DInputUploadCpuP99Ms = previewD3DFlattening.CpuTiming.InputUploadCpuP99Ms,");
        AssertContains(snapshotFlatteningText, "PreviewD3DPipelineLatencyMaxMs = previewD3DFlattening.LatencyAndStats.PipelineLatencyMaxMs,");
        AssertContains(snapshotFlatteningText, "PreviewD3DFrameLatencyWaitTimeoutCount = previewD3DFlattening.LatencyAndStats.FrameLatencyWaitTimeoutCount,");
        AssertContains(snapshotFlatteningText, "PreviewD3DFrameStatsRecentMissedRefreshCount = previewD3DFlattening.LatencyAndStats.FrameStatsRecentMissedRefreshCount,");
        AssertContains(snapshotFlatteningText, "PreviewD3DRecentSlowFrames = previewD3DFlattening.FrameFlow.RecentSlowFrames,");
        AssertContains(snapshotFlatteningText, "PreviewD3DLastRenderedPipelineLatencyMs = previewD3DFlattening.FrameFlow.LastRenderedPipelineLatencyMs,");
        AssertContains(snapshotFlatteningPreviewD3DText, "private static PreviewD3DFlattenedProjection BuildPreviewD3DFlattenedProjection(");
        AssertContains(snapshotFlatteningPreviewD3DText, "CpuTiming = BuildPreviewD3DCpuTimingFlattenedProjection(previewD3D.CpuTiming),");
        AssertContains(snapshotFlatteningPreviewD3DText, "LatencyAndStats = BuildPreviewD3DLatencyAndStatsFlattenedProjection(");
        AssertContains(snapshotFlatteningPreviewD3DText, "FrameFlow = BuildPreviewD3DFrameFlowFlattenedProjection(previewD3D.FrameFlow)");
        AssertContains(snapshotFlatteningPreviewD3DText, "private readonly record struct PreviewD3DFlattenedProjection");
        AssertContains(snapshotFlatteningPreviewD3DText, "public PreviewD3DCpuTimingFlattenedProjection CpuTiming { get; init; }");
        AssertContains(snapshotFlatteningPreviewD3DText, "public PreviewD3DLatencyAndStatsFlattenedProjection LatencyAndStats { get; init; }");
        AssertContains(snapshotFlatteningPreviewD3DText, "public PreviewD3DFrameFlowFlattenedProjection FrameFlow { get; init; }");
        AssertContains(snapshotFlatteningPreviewD3DCpuTimingText, "private static PreviewD3DCpuTimingFlattenedProjection BuildPreviewD3DCpuTimingFlattenedProjection(");
        AssertContains(snapshotFlatteningPreviewD3DCpuTimingText, "InputUploadCpuP99Ms = cpuTiming.InputUploadP99Ms,");
        AssertContains(snapshotFlatteningPreviewD3DCpuTimingText, "private readonly record struct PreviewD3DCpuTimingFlattenedProjection");
        AssertContains(snapshotFlatteningPreviewD3DCpuTimingText, "public double InputUploadCpuP99Ms { get; init; }");
        AssertContains(snapshotFlatteningPreviewD3DCpuTimingText, "public double RenderSubmitCpuP99Ms { get; init; }");
        AssertContains(snapshotFlatteningPreviewD3DCpuTimingText, "public double PresentCallP99Ms { get; init; }");
        AssertContains(snapshotFlatteningPreviewD3DCpuTimingText, "public double TotalFrameCpuP99Ms { get; init; }");
        AssertContains(snapshotFlatteningPreviewD3DLatencyAndStatsText, "private static PreviewD3DLatencyAndStatsFlattenedProjection BuildPreviewD3DLatencyAndStatsFlattenedProjection(");
        AssertContains(snapshotFlatteningPreviewD3DLatencyAndStatsText, "PipelineLatencyMaxMs = pipelineLatency.MaxMs,");
        AssertContains(snapshotFlatteningPreviewD3DLatencyAndStatsText, "FrameLatencyWaitTimeoutCount = frameLatencyWait.TimeoutCount,");
        AssertContains(snapshotFlatteningPreviewD3DLatencyAndStatsText, "FrameStatsRecentMissedRefreshCount = frameStats.RecentMissedRefreshCount,");
        AssertContains(snapshotFlatteningPreviewD3DLatencyAndStatsText, "private readonly record struct PreviewD3DLatencyAndStatsFlattenedProjection");
        AssertContains(snapshotFlatteningPreviewD3DLatencyAndStatsText, "public double PipelineLatencyP99Ms { get; init; }");
        AssertContains(snapshotFlatteningPreviewD3DLatencyAndStatsText, "public long FrameLatencyWaitTimeoutCount { get; init; }");
        AssertContains(snapshotFlatteningPreviewD3DLatencyAndStatsText, "public long FrameStatsRecentMissedRefreshCount { get; init; }");
        AssertContains(snapshotFlatteningPreviewD3DFrameFlowText, "private static PreviewD3DFrameFlowFlattenedProjection BuildPreviewD3DFrameFlowFlattenedProjection(");
        AssertContains(snapshotFlatteningPreviewD3DFrameFlowText, "LastRenderedPipelineLatencyMs = frameFlow.LastRenderedPipelineLatencyMs,");
        AssertContains(snapshotFlatteningPreviewD3DFrameFlowText, "RecentSlowFrames = frameFlow.RecentSlowFrames");
        AssertContains(snapshotFlatteningPreviewD3DFrameFlowText, "private readonly record struct PreviewD3DFrameFlowFlattenedProjection");
        AssertContains(snapshotFlatteningPreviewD3DFrameFlowText, "public long LastSubmittedPreviewPresentId { get; init; }");
        AssertContains(snapshotFlatteningPreviewD3DFrameFlowText, "public double LastRenderedPipelineLatencyMs { get; init; }");
        AssertContains(snapshotFlatteningPreviewD3DFrameFlowText, "public string LastDropReason { get; init; }");
        AssertContains(snapshotFlatteningPreviewD3DFrameFlowText, "public PreviewSlowFrameDiagnostic[] RecentSlowFrames { get; init; }");
        AssertDoesNotContain(snapshotFlatteningPreviewD3DText, "public double InputUploadCpuP99Ms { get; init; }");
        AssertDoesNotContain(snapshotFlatteningPreviewD3DText, "public double PipelineLatencyP99Ms { get; init; }");
        AssertDoesNotContain(snapshotFlatteningPreviewD3DText, "public long LastSubmittedPreviewPresentId { get; init; }");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DPresentSyncInterval = previewRuntime.D3DPresentSyncInterval,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DInputUploadCpuP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DInputUploadCpuP99Ms = previewD3D.InputUploadCpuP99Ms,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DPipelineLatencyMaxMs = previewD3D.PipelineLatencyMaxMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DPipelineLatencyMaxMs = previewD3D.PipelineLatency.MaxMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DPipelineLatencyMaxMs = previewD3D.CpuTiming.PipelineLatencyMaxMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameLatencyWaitTimeoutCount = previewD3D.FrameLatencyWaitTimeoutCount,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameLatencyWaitTimeoutCount = previewD3D.FrameLatencyWait.TimeoutCount,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameStatsRecentMissedRefreshCount = previewD3D.FrameStatsRecentMissedRefreshCount,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameStatsRecentMissedRefreshCount = previewD3D.FrameStats.RecentMissedRefreshCount,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameStatsRecentMissedRefreshCount = recentD3DMissedRefreshes,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DRecentSlowFrames = previewD3D.RecentSlowFrames,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DRecentSlowFrames = previewD3D.FrameFlow.RecentSlowFrames,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DLastRenderedPipelineLatencyMs = previewD3D.LastRenderedPipelineLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DLastRenderedPipelineLatencyMs = previewD3D.FrameFlow.LastRenderedPipelineLatencyMs,");

        AssertContains(previewD3DProjectionText, "private static PreviewD3DProjection BuildPreviewD3DProjection(");
        AssertContains(previewD3DProjectionText, "var cpuTiming = BuildPreviewD3DCpuTimingProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "CpuTiming = cpuTiming,");
        AssertContains(previewD3DProjectionText, "var pipelineLatency = BuildPreviewD3DPipelineLatencyProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "PipelineLatency = pipelineLatency,");
        AssertContains(previewD3DProjectionText, "var frameFlow = BuildPreviewD3DFrameFlowProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "FrameFlow = frameFlow");
        AssertContains(previewD3DProjectionText, "var frameLatencyWait = BuildPreviewD3DFrameLatencyWaitProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "var frameStats = BuildPreviewD3DFrameStatsProjection(");
        AssertContains(previewD3DProjectionText, "FrameLatencyWait = frameLatencyWait,");
        AssertContains(previewD3DProjectionText, "FrameStats = frameStats,");
        AssertDoesNotContain(previewD3DProjectionText, "InputUploadCpuP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,");
        AssertDoesNotContain(previewD3DProjectionText, "PipelineLatencyMaxMs = previewRuntime.D3DPipelineLatencyMaxMs,");
        AssertDoesNotContain(previewD3DProjectionText, "LastRenderedPipelineLatencyMs = previewD3D.D3DLastRenderedPipelineLatencyMs,");
        AssertDoesNotContain(previewD3DProjectionText, "RecentSlowFrames = previewD3D.D3DRecentSlowFrames");
        AssertDoesNotContain(previewD3DProjectionText, "FrameLatencyWaitTimeoutCount = previewD3D.D3DFrameLatencyWaitTimeoutCount,");
        AssertDoesNotContain(previewD3DProjectionText, "FrameStatsRecentMissedRefreshCount = recentD3DMissedRefreshes,");
        AssertContains(previewD3DCpuTimingProjectionText, "private static PreviewD3DCpuTimingProjection BuildPreviewD3DCpuTimingProjection(");
        AssertContains(previewD3DCpuTimingProjectionText, "SampleCount = previewRuntime.D3DCpuTimingSampleCount,");
        AssertContains(previewD3DCpuTimingProjectionText, "InputUploadP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,");
        AssertContains(previewD3DCpuTimingProjectionText, "private readonly record struct PreviewD3DCpuTimingProjection");
        AssertDoesNotContain(previewD3DCpuTimingProjectionText, "PipelineLatencyMaxMs = previewRuntime.D3DPipelineLatencyMaxMs");
        AssertContains(previewD3DPipelineLatencyProjectionText, "private static PreviewD3DPipelineLatencyProjection BuildPreviewD3DPipelineLatencyProjection(");
        AssertContains(previewD3DPipelineLatencyProjectionText, "SampleCount = previewRuntime.D3DPipelineLatencySampleCount,");
        AssertContains(previewD3DPipelineLatencyProjectionText, "MaxMs = previewRuntime.D3DPipelineLatencyMaxMs");
        AssertContains(previewD3DPipelineLatencyProjectionText, "private readonly record struct PreviewD3DPipelineLatencyProjection");
        AssertDoesNotContain(previewD3DProjectionText, "private static PreviewD3DPipelineLatencyProjection BuildPreviewD3DPipelineLatencyProjection(");
        AssertDoesNotContain(previewD3DProjectionText, "private readonly record struct PreviewD3DPipelineLatencyProjection");
        AssertContains(previewD3DFrameFlowProjectionText, "private static PreviewD3DFrameFlowProjection BuildPreviewD3DFrameFlowProjection(");
        AssertContains(previewD3DFrameFlowProjectionText, "LastRenderedPipelineLatencyMs = previewRuntime.D3DLastRenderedPipelineLatencyMs,");
        AssertContains(previewD3DFrameFlowProjectionText, "RecentSlowFrames = previewRuntime.D3DRecentSlowFrames");
        AssertContains(previewD3DFrameFlowProjectionText, "private readonly record struct PreviewD3DFrameFlowProjection");
        AssertDoesNotContain(previewD3DProjectionText, "private static PreviewD3DFrameFlowProjection BuildPreviewD3DFrameFlowProjection(");
        AssertDoesNotContain(previewD3DProjectionText, "private readonly record struct PreviewD3DFrameFlowProjection");
        AssertContains(previewD3DFrameLatencyWaitProjectionText, "private static PreviewD3DFrameLatencyWaitProjection BuildPreviewD3DFrameLatencyWaitProjection(");
        AssertContains(previewD3DFrameLatencyWaitProjectionText, "Enabled = previewRuntime.D3DFrameLatencyWaitEnabled,");
        AssertContains(previewD3DFrameLatencyWaitProjectionText, "TimeoutCount = previewRuntime.D3DFrameLatencyWaitTimeoutCount,");
        AssertContains(previewD3DFrameLatencyWaitProjectionText, "MaxMs = previewRuntime.D3DFrameLatencyWaitMaxMs");
        AssertContains(previewD3DFrameLatencyWaitProjectionText, "private readonly record struct PreviewD3DFrameLatencyWaitProjection");
        AssertDoesNotContain(previewD3DProjectionText, "private static PreviewD3DFrameLatencyWaitProjection BuildPreviewD3DFrameLatencyWaitProjection(");
        AssertDoesNotContain(previewD3DProjectionText, "private readonly record struct PreviewD3DFrameLatencyWaitProjection");

        AssertContains(previewD3DFrameStatsProjectionText, "private static PreviewD3DFrameStatsProjection BuildPreviewD3DFrameStatsProjection(");
        AssertContains(previewD3DFrameStatsProjectionText, "SampleCount = previewRuntime.D3DFrameStatsSampleCount,");
        AssertContains(previewD3DFrameStatsProjectionText, "RecentMissedRefreshCount = recentD3DMissedRefreshes,");
        AssertContains(previewD3DFrameStatsProjectionText, "RecentFailureCount = recentD3DStatsFailures");
        AssertContains(previewD3DFrameStatsProjectionText, "private readonly record struct PreviewD3DFrameStatsProjection");
        AssertDoesNotContain(previewD3DProjectionText, "private static PreviewD3DFrameStatsProjection BuildPreviewD3DFrameStatsProjection(");
        AssertDoesNotContain(previewD3DProjectionText, "private readonly record struct PreviewD3DFrameStatsProjection");
        AssertContains(previewD3DProjectionText, "private readonly record struct PreviewD3DProjection");

        return Task.CompletedTask;
    }

}
