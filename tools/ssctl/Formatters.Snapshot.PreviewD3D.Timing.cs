using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static void AppendSnapshotPreviewD3DCpuTiming(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D CPU timing: input/upload avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DInputUploadCpuAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DInputUploadCpuP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DInputUploadCpuP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DInputUploadCpuMaxMs")}ms | render-submit avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DRenderSubmitCpuAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DRenderSubmitCpuP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DRenderSubmitCpuP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DRenderSubmitCpuMaxMs")}ms | present-call avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPresentCallAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPresentCallP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPresentCallP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPresentCallMaxMs")}ms | total-frame avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DTotalFrameCpuAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DTotalFrameCpuP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DTotalFrameCpuP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DTotalFrameCpuMaxMs")}ms samples={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DCpuTimingSampleCount")}");
    }

    private static void AppendSnapshotPreviewD3DPipelineLatency(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D pipeline latency: avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPipelineLatencyAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPipelineLatencyP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPipelineLatencyP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPipelineLatencyMaxMs")}ms last={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedPipelineLatencyMs")}ms samples={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPipelineLatencySampleCount")}");
    }

    private static void AppendSnapshotPreviewD3DFrameLatencyWait(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D frame-latency wait: enabled={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitEnabled")} handle={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitHandleActive")} calls={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitCallCount")} signaled={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitSignaledCount")} timeouts={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitTimeoutCount")} unexpected={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitUnexpectedResultCount")} lastResult={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitLastResult")} last={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitLastMs")}ms avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitMaxMs")}ms samples={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DFrameLatencyWaitSampleCount")}");
    }
}
