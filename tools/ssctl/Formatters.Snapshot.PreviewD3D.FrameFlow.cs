using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static void AppendSnapshotPreviewD3DPipelineLatency(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D pipeline latency: avg={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPipelineLatencyAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPipelineLatencyP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPipelineLatencyP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPipelineLatencyMaxMs")}ms last={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedPipelineLatencyMs")}ms samples={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DPipelineLatencySampleCount")}");
    }

    private static void AppendSnapshotPreviewD3DFrameOwnership(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"D3D Ownership: submitted present={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastSubmittedPreviewPresentId")} sourceSeq={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastSubmittedSourceSequenceNumber")} pts={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastSubmittedSourcePtsTicks")} | rendered present={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedPreviewPresentId")} sourceSeq={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedSourceSequenceNumber")} pts={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedSourcePtsTicks")} schedulerToPresent={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedSchedulerToPresentMs")}ms pipeline={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastRenderedPipelineLatencyMs")}ms | lastDrop={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastDropReason")} dropPts={AutomationSnapshotFormatter.Get(snapshot, "PreviewD3DLastDroppedSourcePtsTicks")}");
    }
}
