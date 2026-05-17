static partial class Program
{
    private static void AssertDiagnosticsPreviewRuntimeProjectionOwnership(AutomationDiagnosticsHubSourceFamily diagnostics)
    {
        AssertContains(diagnostics.SnapshotProjectionCompositionText, "var previewSummary = BuildPreviewRuntimeProjection(previewRuntime, previewHdrState, captureRuntime);");
        AssertContains(diagnostics.SnapshotProjectionFlatteningText, "EstimatedPipelineLatencyMs = previewSummary.EstimatedPipelineLatencyMs,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "private static PreviewRuntimeProjection BuildPreviewRuntimeProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "var cadence = BuildPreviewRuntimeCadenceProjection(previewRuntime);");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Cadence = cadence,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeCadenceText, "private static PreviewRuntimeCadenceProjection BuildPreviewRuntimeCadenceProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeCadenceText, "OnePercentLowFps = previewRuntime.DisplayCadenceOnePercentLowFps,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeCadenceText, "SlowFramePercent = previewRuntime.DisplayCadenceSlowFramePercent");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "var startup = BuildPreviewRuntimeStartupProjection(previewRuntime);");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "Startup = startup,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeStartupText, "private static PreviewRuntimeStartupProjection BuildPreviewRuntimeStartupProjection(");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeStartupText, "Strategy = previewRuntime.StartupStrategy.ToString(),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeStartupText, "RendererMode = previewRuntime.RendererMode");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "FramesArrived = previewRuntime.FramesArrived,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeStartupText, "Strategy = previewRuntime.StartupStrategy.ToString(),");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "HdrInputDetected = previewHdrState.InputDetected,");
        AssertContains(diagnostics.SnapshotProjectionPreviewRuntimeText, "AdapterColorMetadata = captureRuntime.PreviewColorMetadata");
        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "PreviewFramesArrived = previewRuntime.FramesArrived,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "EstimatedPipelineLatencyMs = (long)previewRuntime.EstimatedPipelineLatencyMs,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "PreviewStartupStrategy = previewRuntime.StartupStrategy.ToString(),");
        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "PreviewHdrInputDetected = previewHdrState.InputDetected,");
        AssertDoesNotContain(diagnostics.SnapshotProjectionCompositionText, "PreviewAdapterColorMetadata = captureRuntime.PreviewColorMetadata,");
    }
}
