using System.Text;
using System.Text.Json;
using Sussudio.Tools;

namespace Sussudio.Tools.Ssctl;

internal static partial class Formatters
{
    private static void AppendSnapshotMjpegPreviewJitterSection(StringBuilder builder, JsonElement snapshot)
    {
        if (!string.Equals(AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterEnabled", "False"), "True", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        builder.AppendLine(
            $"Preview Jitter: target={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTargetDepth")} depth={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterQueueDepth")}/{AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterMaxDepth")} " +
            $"queued={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTotalQueued")} submitted={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTotalSubmitted")} dropped={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTotalDropped")} " +
            $"clearedDrops={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterClearedDropCount")} deadlineDrops={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterDeadlineDropCount")} underflows={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterUnderflowCount")} resumeReprimes={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterResumeReprimeCount")} " +
            $"target+={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTargetIncreaseCount")} target-={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterTargetDecreaseCount")}");
        builder.AppendLine(
            $"Preview Jitter Input: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterInputAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterInputP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterInputMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterInputSampleCount")} samples)");
        builder.AppendLine(
            $"Preview Jitter Output: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterOutputAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterOutputP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterOutputMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterOutputSampleCount")} samples)");
        builder.AppendLine(
            $"Preview Jitter Latency: avg={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLatencyAvgMs")}ms P95={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLatencyP95Ms")}ms max={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLatencyMaxMs")}ms ({AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLatencySampleCount")} samples)");
        builder.AppendLine(
            $"Preview Jitter Ownership: present={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastSelectedPreviewPresentId")} sourceSeq={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastSelectedSourceSequenceNumber")} " +
            $"sourceLatency={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastSelectedSourceLatencyMs")}ms lastDropSeq={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastDroppedSourceSequenceNumber")} reason={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastDropReason")}");
        builder.AppendLine(
            $"Preview Jitter Underflow: reason={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastUnderflowReason")} " +
            $"queue={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastUnderflowQueueDepth")} " +
            $"inputAge={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastUnderflowInputAgeMs")}ms outputAge={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastUnderflowOutputAgeMs")}ms " +
            $"scheduleLateLast={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterLastScheduleLateMs")}ms scheduleLateMax={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterMaxScheduleLateMs")}ms count={AutomationSnapshotFormatter.Get(snapshot, "MjpegPreviewJitterScheduleLateCount")}");
    }
}
