using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendPreviewD3DCpuTiming(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D CPU timing: input/upload avg={Get(snapshot, "PreviewD3DInputUploadCpuAvgMs")}ms P95={Get(snapshot, "PreviewD3DInputUploadCpuP95Ms")}ms P99={Get(snapshot, "PreviewD3DInputUploadCpuP99Ms")}ms max={Get(snapshot, "PreviewD3DInputUploadCpuMaxMs")}ms | render-submit avg={Get(snapshot, "PreviewD3DRenderSubmitCpuAvgMs")}ms P95={Get(snapshot, "PreviewD3DRenderSubmitCpuP95Ms")}ms P99={Get(snapshot, "PreviewD3DRenderSubmitCpuP99Ms")}ms max={Get(snapshot, "PreviewD3DRenderSubmitCpuMaxMs")}ms | present-call avg={Get(snapshot, "PreviewD3DPresentCallAvgMs")}ms P95={Get(snapshot, "PreviewD3DPresentCallP95Ms")}ms P99={Get(snapshot, "PreviewD3DPresentCallP99Ms")}ms max={Get(snapshot, "PreviewD3DPresentCallMaxMs")}ms | total-frame avg={Get(snapshot, "PreviewD3DTotalFrameCpuAvgMs")}ms P95={Get(snapshot, "PreviewD3DTotalFrameCpuP95Ms")}ms P99={Get(snapshot, "PreviewD3DTotalFrameCpuP99Ms")}ms max={Get(snapshot, "PreviewD3DTotalFrameCpuMaxMs")}ms samples={Get(snapshot, "PreviewD3DCpuTimingSampleCount")}");
    }

    private static void AppendPreviewD3DPipelineLatency(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D pipeline latency: avg={Get(snapshot, "PreviewD3DPipelineLatencyAvgMs")}ms P95={Get(snapshot, "PreviewD3DPipelineLatencyP95Ms")}ms P99={Get(snapshot, "PreviewD3DPipelineLatencyP99Ms")}ms max={Get(snapshot, "PreviewD3DPipelineLatencyMaxMs")}ms last={Get(snapshot, "PreviewD3DLastRenderedPipelineLatencyMs")}ms samples={Get(snapshot, "PreviewD3DPipelineLatencySampleCount")}");
    }

    private static void AppendPreviewD3DFrameLatencyWait(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D frame-latency wait: enabled={Get(snapshot, "PreviewD3DFrameLatencyWaitEnabled")} handle={Get(snapshot, "PreviewD3DFrameLatencyWaitHandleActive")} calls={Get(snapshot, "PreviewD3DFrameLatencyWaitCallCount")} signaled={Get(snapshot, "PreviewD3DFrameLatencyWaitSignaledCount")} timeouts={Get(snapshot, "PreviewD3DFrameLatencyWaitTimeoutCount")} unexpected={Get(snapshot, "PreviewD3DFrameLatencyWaitUnexpectedResultCount")} lastResult={Get(snapshot, "PreviewD3DFrameLatencyWaitLastResult")} last={Get(snapshot, "PreviewD3DFrameLatencyWaitLastMs")}ms avg={Get(snapshot, "PreviewD3DFrameLatencyWaitAvgMs")}ms P95={Get(snapshot, "PreviewD3DFrameLatencyWaitP95Ms")}ms max={Get(snapshot, "PreviewD3DFrameLatencyWaitMaxMs")}ms samples={Get(snapshot, "PreviewD3DFrameLatencyWaitSampleCount")}");
    }
}
