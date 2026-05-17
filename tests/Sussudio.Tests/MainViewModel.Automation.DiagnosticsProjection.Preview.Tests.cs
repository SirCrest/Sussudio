using System.Threading.Tasks;

static partial class Program
{
    private static Task AutomationDiagnosticsPreviewRuntimeProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var previewRuntimeProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntime.cs")
            .Replace("\r\n", "\n");
        var previewRuntimeCadenceProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntimeCadence.cs")
            .Replace("\r\n", "\n");
        var previewRuntimeStartupProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewRuntimeStartup.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var previewSummary = BuildPreviewRuntimeProjection(previewRuntime, previewHdrState, captureRuntime);");
        AssertContains(snapshotFlatteningText, "PreviewFramesArrived = previewSummary.FramesArrived,");
        AssertContains(snapshotFlatteningText, "EstimatedPipelineLatencyMs = previewSummary.EstimatedPipelineLatencyMs,");
        AssertContains(snapshotFlatteningText, "PreviewCadenceOnePercentLowFps = previewSummary.Cadence.OnePercentLowFps,");
        AssertContains(snapshotFlatteningText, "PreviewStartupStrategy = previewSummary.Startup.Strategy,");
        AssertContains(snapshotFlatteningText, "PreviewRendererMode = previewSummary.Startup.RendererMode,");
        AssertContains(snapshotFlatteningText, "PreviewGpuPlaybackState = previewSummary.GpuPlaybackState,");
        AssertContains(snapshotFlatteningText, "PreviewColorContext = previewSummary.ColorContext,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewFramesArrived = previewRuntime.FramesArrived,");
        AssertDoesNotContain(snapshotFlatteningText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewCadenceOnePercentLowFps = previewSummary.CadenceOnePercentLowFps,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewCadenceOnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewStartupStrategy = previewSummary.StartupStrategy,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewRendererMode = previewSummary.RendererMode,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewStartupStrategy = previewRuntime.StartupStrategy.ToString(),");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewGpuPlaybackState = previewRuntime.GpuPlaybackState,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewColorContext = captureRuntime.NegotiatedPixelFormat,");

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
        AssertContains(previewRuntimeCadenceProjectionText, "private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(");
        AssertContains(previewRuntimeCadenceProjectionText, "OnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertContains(previewRuntimeCadenceProjectionText, "RecentIntervalsMs = previewRuntime.DisplayCadenceRecentIntervalsMs,");
        AssertContains(previewRuntimeCadenceProjectionText, "SlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent");
        AssertContains(previewRuntimeCadenceProjectionText, "private readonly record struct PreviewRuntimeCadenceProjection");
        AssertContains(previewRuntimeStartupProjectionText, "private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(");
        AssertContains(previewRuntimeStartupProjectionText, "Strategy = previewRuntime.StartupStrategy.ToString(),");
        AssertContains(previewRuntimeStartupProjectionText, "FirstVisualConfirmed = previewRuntime.FirstVisualConfirmed,");
        AssertContains(previewRuntimeStartupProjectionText, "RendererMode = previewRuntime.RendererMode");
        AssertContains(previewRuntimeStartupProjectionText, "private readonly record struct PreviewRuntimeStartupProjection");

        return Task.CompletedTask;
    }

    private static Task AutomationDiagnosticsPreviewD3DProjection_LivesInFocusedPartial()
    {
        var snapshotProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Composition.cs")
            .Replace("\r\n", "\n");
        var snapshotFlatteningText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.Flattening.cs")
            .Replace("\r\n", "\n");
        var previewD3DProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3D.cs")
            .Replace("\r\n", "\n");
        var previewD3DCpuTimingProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DCpuTiming.cs")
            .Replace("\r\n", "\n");
        var previewD3DFrameFlowProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameFlow.cs")
            .Replace("\r\n", "\n");
        var previewD3DFrameLatencyWaitProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameLatencyWait.cs")
            .Replace("\r\n", "\n");
        var previewD3DFrameStatsProjectionText = ReadRepoFile("Sussudio/Services/Automation/AutomationDiagnosticsHub.SnapshotProjection.PreviewD3DFrameStats.cs")
            .Replace("\r\n", "\n");

        AssertContains(snapshotProjectionText, "var previewD3D = BuildPreviewD3DProjection(\n            previewRuntime,\n            recentD3DMissedRefreshes,\n            recentD3DStatsFailures);");
        AssertContains(snapshotFlatteningText, "PreviewD3DPresentSyncInterval = previewD3D.PresentSyncInterval,");
        AssertContains(snapshotFlatteningText, "PreviewD3DInputUploadCpuP99Ms = previewD3D.CpuTiming.InputUploadP99Ms,");
        AssertContains(snapshotFlatteningText, "PreviewD3DPipelineLatencyMaxMs = previewD3D.CpuTiming.PipelineLatencyMaxMs,");
        AssertContains(snapshotFlatteningText, "PreviewD3DFrameLatencyWaitTimeoutCount = previewD3D.FrameLatencyWait.TimeoutCount,");
        AssertContains(snapshotFlatteningText, "PreviewD3DFrameStatsRecentMissedRefreshCount = previewD3D.FrameStats.RecentMissedRefreshCount,");
        AssertContains(snapshotFlatteningText, "PreviewD3DRecentSlowFrames = previewD3D.FrameFlow.RecentSlowFrames,");
        AssertContains(snapshotFlatteningText, "PreviewD3DLastRenderedPipelineLatencyMs = previewD3D.FrameFlow.LastRenderedPipelineLatencyMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DPresentSyncInterval = previewRuntime.D3DPresentSyncInterval,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DInputUploadCpuP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DInputUploadCpuP99Ms = previewD3D.InputUploadCpuP99Ms,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DPipelineLatencyMaxMs = previewD3D.PipelineLatencyMaxMs,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameLatencyWaitTimeoutCount = previewD3D.FrameLatencyWaitTimeoutCount,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameStatsRecentMissedRefreshCount = previewD3D.FrameStatsRecentMissedRefreshCount,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DFrameStatsRecentMissedRefreshCount = recentD3DMissedRefreshes,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DRecentSlowFrames = previewD3D.RecentSlowFrames,");
        AssertDoesNotContain(snapshotFlatteningText, "PreviewD3DLastRenderedPipelineLatencyMs = previewD3D.LastRenderedPipelineLatencyMs,");

        AssertContains(previewD3DProjectionText, "private static PreviewD3DProjection BuildPreviewD3DProjection(");
        AssertContains(previewD3DProjectionText, "var cpuTiming = BuildPreviewD3DCpuTimingProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "CpuTiming = cpuTiming,");
        AssertContains(previewD3DProjectionText, "var frameFlow = BuildPreviewD3DFrameFlowProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "FrameFlow = frameFlow");
        AssertContains(previewD3DProjectionText, "var frameLatencyWait = BuildPreviewD3DFrameLatencyWaitProjection(previewRuntime);");
        AssertContains(previewD3DProjectionText, "var frameStats = BuildPreviewD3DFrameStatsProjection(");
        AssertContains(previewD3DProjectionText, "FrameLatencyWait = frameLatencyWait,");
        AssertContains(previewD3DProjectionText, "FrameStats = frameStats,");
        AssertDoesNotContain(previewD3DProjectionText, "InputUploadCpuP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,");
        AssertDoesNotContain(previewD3DProjectionText, "PipelineLatencyMaxMs = previewRuntime.D3DPipelineLatencyMaxMs,");
        AssertDoesNotContain(previewD3DProjectionText, "LastRenderedPipelineLatencyMs = previewRuntime.D3DLastRenderedPipelineLatencyMs,");
        AssertDoesNotContain(previewD3DProjectionText, "RecentSlowFrames = previewRuntime.D3DRecentSlowFrames");
        AssertDoesNotContain(previewD3DProjectionText, "FrameLatencyWaitTimeoutCount = previewRuntime.D3DFrameLatencyWaitTimeoutCount,");
        AssertDoesNotContain(previewD3DProjectionText, "FrameStatsRecentMissedRefreshCount = recentD3DMissedRefreshes,");
        AssertContains(previewD3DCpuTimingProjectionText, "private static PreviewD3DCpuTimingProjection BuildPreviewD3DCpuTimingProjection(");
        AssertContains(previewD3DCpuTimingProjectionText, "SampleCount = previewRuntime.D3DCpuTimingSampleCount,");
        AssertContains(previewD3DCpuTimingProjectionText, "InputUploadP99Ms = previewRuntime.D3DInputUploadCpuP99Ms,");
        AssertContains(previewD3DCpuTimingProjectionText, "PipelineLatencyMaxMs = previewRuntime.D3DPipelineLatencyMaxMs");
        AssertContains(previewD3DCpuTimingProjectionText, "private readonly record struct PreviewD3DCpuTimingProjection");
        AssertContains(previewD3DFrameFlowProjectionText, "private static PreviewD3DFrameFlowProjection BuildPreviewD3DFrameFlowProjection(");
        AssertContains(previewD3DFrameFlowProjectionText, "LastRenderedPipelineLatencyMs = previewRuntime.D3DLastRenderedPipelineLatencyMs,");
        AssertContains(previewD3DFrameFlowProjectionText, "RecentSlowFrames = previewRuntime.D3DRecentSlowFrames");
        AssertContains(previewD3DFrameFlowProjectionText, "private readonly record struct PreviewD3DFrameFlowProjection");
        AssertContains(previewD3DFrameLatencyWaitProjectionText, "private static PreviewD3DFrameLatencyWaitProjection BuildPreviewD3DFrameLatencyWaitProjection(");
        AssertContains(previewD3DFrameLatencyWaitProjectionText, "Enabled = previewRuntime.D3DFrameLatencyWaitEnabled,");
        AssertContains(previewD3DFrameLatencyWaitProjectionText, "TimeoutCount = previewRuntime.D3DFrameLatencyWaitTimeoutCount,");
        AssertContains(previewD3DFrameLatencyWaitProjectionText, "MaxMs = previewRuntime.D3DFrameLatencyWaitMaxMs");
        AssertContains(previewD3DFrameLatencyWaitProjectionText, "private readonly record struct PreviewD3DFrameLatencyWaitProjection");

        AssertContains(previewD3DFrameStatsProjectionText, "private static PreviewD3DFrameStatsProjection BuildPreviewD3DFrameStatsProjection(");
        AssertContains(previewD3DFrameStatsProjectionText, "SampleCount = previewRuntime.D3DFrameStatsSampleCount,");
        AssertContains(previewD3DFrameStatsProjectionText, "RecentMissedRefreshCount = recentD3DMissedRefreshes,");
        AssertContains(previewD3DFrameStatsProjectionText, "RecentFailureCount = recentD3DStatsFailures");
        AssertContains(previewD3DFrameStatsProjectionText, "private readonly record struct PreviewD3DFrameStatsProjection");
        AssertContains(previewD3DProjectionText, "private readonly record struct PreviewD3DProjection");

        return Task.CompletedTask;
    }

}
