using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static void AppendSnapshotFlashbackEncodingHealthSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"Flashback Queue Latency: oldest={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueOldestFrameAgeMs")}ms last={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLastLatencyMs")}ms avg={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLatencyAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLatencyP95Ms")}ms P99={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLatencyP99Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLatencyMaxMs")}ms samples={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLatencySampleCount")} rejected={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueRejectedFrames")} lastReject={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoQueueLastRejectReason", "")}");
        builder.AppendLine($"Flashback Backpressure: total={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoBackpressureWaitMs")}ms events={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoBackpressureEvents")} last={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoBackpressureLastWaitMs")}ms max={AutomationSnapshotFormatter.Get(snapshot, "FlashbackVideoBackpressureMaxWaitMs")}ms");
        builder.AppendLine($"Flashback Failure: active={AutomationSnapshotFormatter.Get(snapshot, "FlashbackEncodingFailed")} type={AutomationSnapshotFormatter.Get(snapshot, "FlashbackEncodingFailureType", "None")} msg={AutomationSnapshotFormatter.Get(snapshot, "FlashbackEncodingFailureMessage", "")}");
        builder.AppendLine($"Flashback GPU Queue: {AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuQueueCapacity")} max={AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuQueueMaxDepth")} enq={AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuFramesEnqueued")} overloads={AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuFramesDropped")} rejected={AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuQueueRejectedFrames")} lastReject={AutomationSnapshotFormatter.Get(snapshot, "FlashbackGpuQueueLastRejectReason", "")}");
    }
}
