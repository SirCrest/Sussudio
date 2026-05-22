using System.IO;
using System.Threading.Tasks;

static partial class Program
{
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
}
