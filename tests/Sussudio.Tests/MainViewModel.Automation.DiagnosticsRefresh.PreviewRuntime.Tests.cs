static partial class Program
{
    private static void AssertDiagnosticsPreviewRuntimeProjectionOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var previewSummary = BuildPreviewRuntimeProjection(previewRuntime, previewHdrState, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "var previewRuntimeFlattening = BuildPreviewRuntimeFlattenedProjection(previewSummary);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "EstimatedPipelineLatencyMs = previewRuntimeFlattening.EstimatedPipelineLatencyMs,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PreviewFramesArrived = previewRuntimeFlattening.FramesArrived,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PreviewCadenceOnePercentLowFps = previewRuntimeFlattening.CadenceOnePercentLowFps,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "PreviewAdapterColorMetadata = previewRuntimeFlattening.AdapterColorMetadata,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningPreviewRuntimeText, "private static PreviewRuntimeFlattenedProjection BuildPreviewRuntimeFlattenedProjection(");
        AssertContains(diagnostics.SnapshotProjectionFlatteningPreviewRuntimeText, "FramesArrived = previewSummary.FramesArrived,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningPreviewRuntimeText, "EstimatedPipelineLatencyMs = previewSummary.EstimatedPipelineLatencyMs,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningPreviewRuntimeText, "CadenceOnePercentLowFps = previewSummary.Cadence.OnePercentLowFps,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningPreviewRuntimeText, "StartupStrategy = previewSummary.Startup.Strategy,");
        AssertContains(diagnostics.SnapshotProjectionFlatteningPreviewRuntimeText, "AdapterColorMetadata = previewSummary.AdapterColorMetadata");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeProjection BuildPreviewRuntimeProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "var cadence = BuildPreviewRuntimeCadenceProjection(previewRuntime);");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Cadence = cadence,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "OnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "SlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "var startup = BuildPreviewRuntimeStartupProjection(previewRuntime);");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Startup = startup,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Strategy = previewRuntime.StartupStrategy.ToString(),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "RendererMode = previewRuntime.RendererMode");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "FramesArrived = previewRuntime.FramesArrived,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Strategy = previewRuntime.StartupStrategy.ToString(),");
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
