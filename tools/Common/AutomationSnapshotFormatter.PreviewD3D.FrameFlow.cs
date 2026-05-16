using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendPreviewD3DPipelineLatency(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D pipeline latency: avg={Get(snapshot, "PreviewD3DPipelineLatencyAvgMs")}ms P95={Get(snapshot, "PreviewD3DPipelineLatencyP95Ms")}ms P99={Get(snapshot, "PreviewD3DPipelineLatencyP99Ms")}ms max={Get(snapshot, "PreviewD3DPipelineLatencyMaxMs")}ms last={Get(snapshot, "PreviewD3DLastRenderedPipelineLatencyMs")}ms samples={Get(snapshot, "PreviewD3DPipelineLatencySampleCount")}");
    }

    private static void AppendPreviewD3DFrameOwnership(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D Ownership: submitted present={Get(snapshot, "PreviewD3DLastSubmittedPreviewPresentId")} sourceSeq={Get(snapshot, "PreviewD3DLastSubmittedSourceSequenceNumber")} pts={Get(snapshot, "PreviewD3DLastSubmittedSourcePtsTicks")} | rendered present={Get(snapshot, "PreviewD3DLastRenderedPreviewPresentId")} sourceSeq={Get(snapshot, "PreviewD3DLastRenderedSourceSequenceNumber")} pts={Get(snapshot, "PreviewD3DLastRenderedSourcePtsTicks")} schedulerToPresent={Get(snapshot, "PreviewD3DLastRenderedSchedulerToPresentMs")}ms pipeline={Get(snapshot, "PreviewD3DLastRenderedPipelineLatencyMs")}ms | lastDrop={Get(snapshot, "PreviewD3DLastDropReason")} dropPts={Get(snapshot, "PreviewD3DLastDroppedSourcePtsTicks")}");
    }
}
