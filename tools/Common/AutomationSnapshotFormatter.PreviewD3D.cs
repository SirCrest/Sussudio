using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static bool IsPreviewD3DRendererMode(string rendererMode)
        => rendererMode == "D3D11VideoProcessor" ||
           rendererMode == "Nv12Shader" ||
           rendererMode == "HdrShader" ||
           rendererMode == "HdrPassthrough";

    private static void AppendPreviewD3DSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D Swap Chain: {Get(snapshot, "PreviewD3DSwapChainAddress", "N/A")}");
        builder.AppendLine($"D3D Frames: {Get(snapshot, "PreviewD3DFramesSubmitted")} submitted, {Get(snapshot, "PreviewD3DFramesRendered")} rendered, {Get(snapshot, "PreviewD3DFramesDropped")} dropped, pending={Get(snapshot, "PreviewD3DPendingFrameCount")}");
        var renderThreadFailures = Get(snapshot, "PreviewD3DRenderThreadFailureCount", "0");
        if (renderThreadFailures != "0")
        {
            builder.AppendLine($"D3D Render Thread Failures: {renderThreadFailures} last={Get(snapshot, "PreviewD3DLastRenderThreadFailureType")} hr={Get(snapshot, "PreviewD3DLastRenderThreadFailureHResult")} msg={Get(snapshot, "PreviewD3DLastRenderThreadFailureMessage")}");
        }

        builder.AppendLine($"Color: input={Get(snapshot, "PreviewD3DInputColorSpace")} output={Get(snapshot, "PreviewD3DOutputColorSpace")}");
        builder.AppendLine($"Frame Time: target={FormatIntervalMs(snapshot, "PreviewCadenceExpectedIntervalMs")} avg={Get(snapshot, "PreviewCadenceAverageIntervalMs")}ms P95={Get(snapshot, "PreviewCadenceP95IntervalMs")}ms max={Get(snapshot, "PreviewCadenceMaxIntervalMs")}ms");
        builder.AppendLine($"Average Rate: {Get(snapshot, "PreviewCadenceObservedFps")} fps | 5% Low: {Get(snapshot, "PreviewCadenceFivePercentLowFps")} fps | 1% Low: {Get(snapshot, "PreviewCadenceOnePercentLowFps")} fps | Samples: {Get(snapshot, "PreviewCadenceSampleCount")} over {Get(snapshot, "PreviewCadenceSampleDurationMs")}ms");
        builder.AppendLine($"Pacing Classifier: stage={Get(snapshot, "PreviewPacingLikelySlowStage")} confidence={Get(snapshot, "PreviewPacingSlowStageConfidence")} evidence={Get(snapshot, "PreviewPacingSlowStageEvidence", "")}");
        AppendPreviewD3DCpuTiming(builder, snapshot);
        AppendPreviewD3DPipelineLatency(builder, snapshot);
        AppendPreviewD3DFrameLatencyWait(builder, snapshot);
        AppendPreviewD3DFrameStats(builder, snapshot);
        AppendPreviewD3DFrameOwnership(builder, snapshot);
        AppendPreviewSlowFrameDiagnostics(builder, snapshot);
    }
}
