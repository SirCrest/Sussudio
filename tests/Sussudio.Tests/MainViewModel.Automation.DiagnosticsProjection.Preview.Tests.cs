using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationDiagnosticsPreviewRuntimeProjection_LivesInFocusedPartial()
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
        AssertContains(snapshotFlatteningText, "PreviewFramesArrived = previewRuntimeFlattening.FramesArrived,");
        AssertContains(snapshotFlatteningText, "EstimatedPipelineLatencyMs = previewRuntimeFlattening.EstimatedPipelineLatencyMs,");
        AssertContains(snapshotFlatteningText, "PreviewCadenceOnePercentLowFps = previewRuntimeFlattening.CadenceOnePercentLowFps,");
        AssertContains(snapshotFlatteningText, "PreviewStartupStrategy = previewRuntimeFlattening.StartupStrategy,");
        AssertContains(snapshotFlatteningText, "PreviewRendererMode = previewRuntimeFlattening.RendererMode,");
        AssertContains(snapshotFlatteningText, "PreviewGpuPlaybackState = previewRuntimeFlattening.GpuPlaybackState,");
        AssertContains(snapshotFlatteningText, "PreviewColorContext = previewRuntimeFlattening.ColorContext,");
        AssertContains(snapshotFlatteningText, "PreviewAdapterColorMetadata = previewRuntimeFlattening.AdapterColorMetadata,");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "private static PreviewRuntimeFlattenedProjection BuildPreviewRuntimeFlattenedProjection(");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "FramesArrived = previewSummary.FramesArrived,");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "EstimatedPipelineLatencyMs = previewSummary.EstimatedPipelineLatencyMs,");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "CadenceOnePercentLowFps = previewSummary.Cadence.OnePercentLowFps,");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "StartupStrategy = previewSummary.Startup.Strategy,");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "RendererMode = previewSummary.Startup.RendererMode,");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "GpuPlaybackState = previewSummary.GpuPlaybackState,");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "ColorContext = previewSummary.ColorContext,");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "AdapterColorMetadata = previewSummary.AdapterColorMetadata");
        AssertContains(snapshotFlatteningPreviewRuntimeText, "private readonly record struct PreviewRuntimeFlattenedProjection");
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
        AssertContains(previewRuntimeProjectionText, "var cadence = BuildPreviewRuntimeCadenceProjection(previewRuntime);");
        AssertContains(previewRuntimeProjectionText, "Cadence = cadence,");
        AssertContains(previewRuntimeProjectionText, "var startup = BuildPreviewRuntimeStartupProjection(previewRuntime);");
        AssertContains(previewRuntimeProjectionText, "Startup = startup,");
        AssertContains(previewRuntimeProjectionText, "FramesArrived = previewRuntime.FramesArrived,");
        AssertContains(previewRuntimeProjectionText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs,");
        AssertDoesNotContain(previewRuntimeProjectionText, "CadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertDoesNotContain(previewRuntimeProjectionText, "CadenceSlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent,");
        AssertDoesNotContain(previewRuntimeProjectionText, "StartupStrategy = previewRuntime.StartupStrategy.ToString(),");
        AssertDoesNotContain(previewRuntimeProjectionText, "RendererMode = previewRuntime.RendererMode,");
        AssertContains(previewRuntimeProjectionText, "GpuPlaybackState = previewRuntime.GpuPlaybackState,");
        AssertContains(previewRuntimeProjectionText, "HdrInputDetected = previewHdrState.InputDetected,");
        AssertContains(previewRuntimeProjectionText, "ColorContext = captureRuntime.NegotiatedPixelFormat,");
        AssertContains(previewRuntimeProjectionText, "private readonly record struct PreviewRuntimeProjection");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(");
        AssertContains(previewRuntimeProjectionText, "OnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertContains(previewRuntimeProjectionText, "RecentIntervalsMs = previewRuntime.DisplayCadenceRecentIntervalsMs,");
        AssertContains(previewRuntimeProjectionText, "SlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent");
        AssertContains(previewRuntimeProjectionText, "private readonly record struct PreviewRuntimeCadenceProjection");
        AssertContains(previewRuntimeProjectionText, "private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(");
        AssertContains(previewRuntimeProjectionText, "Strategy = previewRuntime.StartupStrategy.ToString(),");
        AssertContains(previewRuntimeProjectionText, "FirstVisualConfirmed = previewRuntime.FirstVisualConfirmed,");
        AssertContains(previewRuntimeProjectionText, "RendererMode = previewRuntime.RendererMode");
        AssertContains(previewRuntimeProjectionText, "private readonly record struct PreviewRuntimeStartupProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsPreviewD3DProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningPreviewD3DText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.PreviewD3D.cs")
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
        AssertContains(snapshotFlatteningText, "PreviewD3DInputUploadCpuP99Ms = previewD3DFlattening.InputUploadCpuP99Ms,");
        AssertContains(snapshotFlatteningText, "PreviewD3DPipelineLatencyMaxMs = previewD3DFlattening.PipelineLatencyMaxMs,");
        AssertContains(snapshotFlatteningText, "PreviewD3DFrameLatencyWaitTimeoutCount = previewD3DFlattening.FrameLatencyWaitTimeoutCount,");
        AssertContains(snapshotFlatteningText, "PreviewD3DFrameStatsRecentMissedRefreshCount = previewD3DFlattening.FrameStatsRecentMissedRefreshCount,");
        AssertContains(snapshotFlatteningText, "PreviewD3DRecentSlowFrames = previewD3DFlattening.RecentSlowFrames,");
        AssertContains(snapshotFlatteningText, "PreviewD3DLastRenderedPipelineLatencyMs = previewD3DFlattening.LastRenderedPipelineLatencyMs,");
        AssertContains(snapshotFlatteningPreviewD3DText, "private static PreviewD3DFlattenedProjection BuildPreviewD3DFlattenedProjection(");
        AssertContains(snapshotFlatteningPreviewD3DText, "InputUploadCpuP99Ms = previewD3D.CpuTiming.InputUploadP99Ms,");
        AssertContains(snapshotFlatteningPreviewD3DText, "PipelineLatencyMaxMs = previewD3D.PipelineLatency.MaxMs,");
        AssertContains(snapshotFlatteningPreviewD3DText, "FrameLatencyWaitTimeoutCount = previewD3D.FrameLatencyWait.TimeoutCount,");
        AssertContains(snapshotFlatteningPreviewD3DText, "FrameStatsRecentMissedRefreshCount = previewD3D.FrameStats.RecentMissedRefreshCount,");
        AssertContains(snapshotFlatteningPreviewD3DText, "RecentSlowFrames = previewD3D.FrameFlow.RecentSlowFrames");
        AssertContains(snapshotFlatteningPreviewD3DText, "private readonly record struct PreviewD3DFlattenedProjection");
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
