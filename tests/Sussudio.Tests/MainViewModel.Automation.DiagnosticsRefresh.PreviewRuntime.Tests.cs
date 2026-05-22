static partial class Program
{
    private static void AssertDiagnosticsPreviewRuntimeProjectionOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var previewSummary = BuildPreviewRuntimeProjection(previewRuntime, previewHdrState, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var previewRuntimeFlattening = BuildPreviewRuntimeFlattenedProjection(previewSummary);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "EstimatedPipelineLatencyMs = previewRuntimeFlattening.Frame.EstimatedPipelineLatencyMs,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PreviewFramesArrived = previewRuntimeFlattening.Frame.FramesArrived,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PreviewCadenceOnePercentLowFps = previewRuntimeFlattening.Cadence.OnePercentLowFps,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PreviewAdapterColorMetadata = previewRuntimeFlattening.Color.AdapterColorMetadata,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFlattenedProjection BuildPreviewRuntimeFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Frame = BuildPreviewRuntimeFrameFlattenedProjection(previewSummary.Frame),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Cadence = BuildPreviewRuntimeCadenceFlattenedProjection(previewSummary.Cadence),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Surface = BuildPreviewRuntimeSurfaceFlattenedProjection(previewSummary.Surface),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Startup = BuildPreviewRuntimeStartupFlattenedProjection(previewSummary.Startup),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "GpuPlayback = BuildPreviewRuntimeGpuPlaybackFlattenedProjection(previewSummary.GpuPlayback),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Color = BuildPreviewRuntimeColorFlattenedProjection(previewSummary.Color)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFrameFlattenedProjection BuildPreviewRuntimeFrameFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "EstimatedPipelineLatencyMs = frame.EstimatedPipelineLatencyMs");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeCadenceText, "private static PreviewRuntimeCadenceFlattenedProjection BuildPreviewRuntimeCadenceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeCadenceText, "OnePercentLowFps = cadence.OnePercentLowFps,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeSurfaceFlattenedProjection BuildPreviewRuntimeSurfaceFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "RendererAttached = surface.RendererAttached");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeStartupText, "private static PreviewRuntimeStartupFlattenedProjection BuildPreviewRuntimeStartupFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeStartupText, "Strategy = startup.Strategy,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeGpuPlaybackFlattenedProjection BuildPreviewRuntimeGpuPlaybackFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "PositionEventCount = gpuPlayback.PositionEventCount");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeColorFlattenedProjection BuildPreviewRuntimeColorFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "AdapterColorMetadata = color.AdapterColorMetadata");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeProjection BuildPreviewRuntimeProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Frame = BuildPreviewRuntimeFrameProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Cadence = BuildPreviewRuntimeCadenceProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Surface = BuildPreviewRuntimeSurfaceProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Startup = BuildPreviewRuntimeStartupProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "GpuPlayback = BuildPreviewRuntimeGpuPlaybackProjection(previewRuntime),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Color = BuildPreviewRuntimeColorProjection(previewHdrState, captureRuntime)");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeFrameProjection BuildPreviewRuntimeFrameProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeCadenceText, "private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeCadenceText, "OnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeCadenceText, "SlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeSurfaceProjection BuildPreviewRuntimeSurfaceProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "RendererAttached = previewRuntime.RendererAttached");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeStartupText, "private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeStartupText, "Strategy = previewRuntime.StartupStrategy.ToString(),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeStartupText, "RendererMode = previewRuntime.RendererMode");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeGpuPlaybackProjection BuildPreviewRuntimeGpuPlaybackProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "PlaybackState = previewRuntime.GpuPlaybackState,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeColorProjection BuildPreviewRuntimeColorProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "HdrInputDetected = previewHdrState.InputDetected,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "AdapterColorMetadata = captureRuntime.PreviewColorMetadata");
        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "PreviewFramesArrived = previewRuntime.FramesArrived,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "PreviewStartupStrategy = previewRuntime.StartupStrategy.ToString(),");
        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "PreviewHdrInputDetected = previewHdrState.InputDetected,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "PreviewAdapterColorMetadata = captureRuntime.PreviewColorMetadata,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PreviewFramesArrived = previewSummary.FramesArrived,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "EstimatedPipelineLatencyMs = previewSummary.EstimatedPipelineLatencyMs,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PreviewCadenceOnePercentLowFps = previewSummary.Cadence.OnePercentLowFps,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PreviewStartupStrategy = previewSummary.Startup.Strategy,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionFlatteningText, "PreviewAdapterColorMetadata = previewSummary.AdapterColorMetadata,");
    }
}
