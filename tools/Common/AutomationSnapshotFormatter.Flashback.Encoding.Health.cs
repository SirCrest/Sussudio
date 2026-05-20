using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendFlashbackEncodingHealthSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine($"Flashback Queue Latency: oldest={Get(snapshot, "FlashbackVideoQueueOldestFrameAgeMs")}ms last={Get(snapshot, "FlashbackVideoQueueLastLatencyMs")}ms avg={Get(snapshot, "FlashbackVideoQueueLatencyAvgMs")}ms P95={Get(snapshot, "FlashbackVideoQueueLatencyP95Ms")}ms P99={Get(snapshot, "FlashbackVideoQueueLatencyP99Ms")}ms max={Get(snapshot, "FlashbackVideoQueueLatencyMaxMs")}ms samples={Get(snapshot, "FlashbackVideoQueueLatencySampleCount")} rejected={Get(snapshot, "FlashbackVideoQueueRejectedFrames")} lastReject={Get(snapshot, "FlashbackVideoQueueLastRejectReason", "")}");
        builder.AppendLine($"Flashback Backpressure: total={Get(snapshot, "FlashbackVideoBackpressureWaitMs")}ms events={Get(snapshot, "FlashbackVideoBackpressureEvents")} last={Get(snapshot, "FlashbackVideoBackpressureLastWaitMs")}ms max={Get(snapshot, "FlashbackVideoBackpressureMaxWaitMs")}ms");
        builder.AppendLine($"Flashback Failure: active={Get(snapshot, "FlashbackEncodingFailed")} type={Get(snapshot, "FlashbackEncodingFailureType", "None")} msg={Get(snapshot, "FlashbackEncodingFailureMessage", "")}");
        builder.AppendLine($"Flashback GPU Queue: {Get(snapshot, "FlashbackGpuQueueDepth")}/{Get(snapshot, "FlashbackGpuQueueCapacity")} max={Get(snapshot, "FlashbackGpuQueueMaxDepth")} enq={Get(snapshot, "FlashbackGpuFramesEnqueued")} overloads={Get(snapshot, "FlashbackGpuFramesDropped")} rejected={Get(snapshot, "FlashbackGpuQueueRejectedFrames")} lastReject={Get(snapshot, "FlashbackGpuQueueLastRejectReason", "")}");
    }
}
