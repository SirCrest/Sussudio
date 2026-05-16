using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static bool IsSnapshotPreviewD3DRendererMode(string rendererMode)
        => rendererMode == "D3D11VideoProcessor" ||
           rendererMode == "Nv12Shader" ||
           rendererMode == "HdrShader" ||
           rendererMode == "HdrPassthrough";

    private static void AppendSnapshotPreviewD3DSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D Swap Chain: {AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DSwapChainAddress", "N/A")}");
        builder.AppendLine($"D3D Frames: {AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFramesSubmitted")} submitted, {AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFramesRendered")} rendered, {AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFramesDropped")} dropped, pending={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPendingFrameCount")}");
        var renderThreadFailures = AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DRenderThreadFailureCount", "0");
        if (renderThreadFailures != "0")
        {
            builder.AppendLine($"D3D Render Thread Failures: {renderThreadFailures} last={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderThreadFailureType")} hr={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderThreadFailureHResult")} msg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderThreadFailureMessage")}");
        }

        builder.AppendLine($"Color: input={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DInputColorSpace")} output={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DOutputColorSpace")}");
        builder.AppendLine($"Frame Time: target={AutomationSnapshotFormatter.FormatIntervalMs(snapshot, "PreviewCadenceExpectedIntervalMs")} avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceAverageIntervalMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceP95IntervalMs")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceMaxIntervalMs")}ms");
        builder.AppendLine($"Average Rate: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceObservedFps")} fps | 5% Low: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceFivePercentLowFps")} fps | 1% Low: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceOnePercentLowFps")} fps | Samples: {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceSampleCount")} over {AutomationSnapshotFormatter.Get(snapshot, "PreviewCadenceSampleDurationMs")}ms");
        builder.AppendLine($"Pacing Classifier: stage={AutomationSnapshotFormatter.Get(snapshot, "PreviewPacingLikelySlowStage")} confidence={AutomationSnapshotFormatter.Get(snapshot, "PreviewPacingSlowStageConfidence")} evidence={AutomationSnapshotFormatter.Get(snapshot, "PreviewPacingSlowStageEvidence", "")}");
        AppendSnapshotPreviewD3DCpuTiming(builder, snapshot);
        AppendSnapshotPreviewD3DPipelineLatency(builder, snapshot);
        AppendSnapshotPreviewD3DFrameLatencyWait(builder, snapshot);
        AppendSnapshotPreviewD3DFrameStats(builder, snapshot);
        AppendSnapshotPreviewD3DFrameOwnership(builder, snapshot);
        AppendSnapshotPreviewSlowFrameDiagnostics(builder, snapshot);
    }
}
