using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendMjpegPreviewJitterSection(StringBuilder builder, JsonElement snapshot)
    {
        if (!string.Equals(Get(snapshot, "MjpegPreviewJitterEnabled", "False"), "True", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        builder.AppendLine(
            $"Preview Jitter: target={Get(snapshot, "MjpegPreviewJitterTargetDepth")} depth={Get(snapshot, "MjpegPreviewJitterQueueDepth")}/{Get(snapshot, "MjpegPreviewJitterMaxDepth")} " +
            $"queued={Get(snapshot, "MjpegPreviewJitterTotalQueued")} submitted={Get(snapshot, "MjpegPreviewJitterTotalSubmitted")} dropped={Get(snapshot, "MjpegPreviewJitterTotalDropped")} " +
            $"clearedDrops={Get(snapshot, "MjpegPreviewJitterClearedDropCount")} deadlineDrops={Get(snapshot, "MjpegPreviewJitterDeadlineDropCount")} underflows={Get(snapshot, "MjpegPreviewJitterUnderflowCount")} resumeReprimes={Get(snapshot, "MjpegPreviewJitterResumeReprimeCount")} " +
            $"target+={Get(snapshot, "MjpegPreviewJitterTargetIncreaseCount")} target-={Get(snapshot, "MjpegPreviewJitterTargetDecreaseCount")}");
        builder.AppendLine(
            $"Preview Jitter Input: avg={Get(snapshot, "MjpegPreviewJitterInputAvgMs")}ms P95={Get(snapshot, "MjpegPreviewJitterInputP95Ms")}ms max={Get(snapshot, "MjpegPreviewJitterInputMaxMs")}ms ({Get(snapshot, "MjpegPreviewJitterInputSampleCount")} samples)");
        builder.AppendLine(
            $"Preview Jitter Output: avg={Get(snapshot, "MjpegPreviewJitterOutputAvgMs")}ms P95={Get(snapshot, "MjpegPreviewJitterOutputP95Ms")}ms max={Get(snapshot, "MjpegPreviewJitterOutputMaxMs")}ms ({Get(snapshot, "MjpegPreviewJitterOutputSampleCount")} samples)");
        builder.AppendLine(
            $"Preview Jitter Latency: avg={Get(snapshot, "MjpegPreviewJitterLatencyAvgMs")}ms P95={Get(snapshot, "MjpegPreviewJitterLatencyP95Ms")}ms max={Get(snapshot, "MjpegPreviewJitterLatencyMaxMs")}ms ({Get(snapshot, "MjpegPreviewJitterLatencySampleCount")} samples)");
        builder.AppendLine(
            $"Preview Jitter Ownership: present={Get(snapshot, "MjpegPreviewJitterLastSelectedPreviewPresentId")} sourceSeq={Get(snapshot, "MjpegPreviewJitterLastSelectedSourceSequenceNumber")} " +
            $"sourceLatency={Get(snapshot, "MjpegPreviewJitterLastSelectedSourceLatencyMs")}ms lastDropSeq={Get(snapshot, "MjpegPreviewJitterLastDroppedSourceSequenceNumber")} reason={Get(snapshot, "MjpegPreviewJitterLastDropReason")}");
        builder.AppendLine(
            $"Preview Jitter Underflow: reason={Get(snapshot, "MjpegPreviewJitterLastUnderflowReason")} " +
            $"queue={Get(snapshot, "MjpegPreviewJitterLastUnderflowQueueDepth")} " +
            $"inputAge={Get(snapshot, "MjpegPreviewJitterLastUnderflowInputAgeMs")}ms outputAge={Get(snapshot, "MjpegPreviewJitterLastUnderflowOutputAgeMs")}ms " +
            $"scheduleLateLast={Get(snapshot, "MjpegPreviewJitterLastScheduleLateMs")}ms scheduleLateMax={Get(snapshot, "MjpegPreviewJitterMaxScheduleLateMs")}ms count={Get(snapshot, "MjpegPreviewJitterScheduleLateCount")}");
    }
}
