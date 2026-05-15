using System.Text;
using System.Text.Json;

namespace Sussudio.Tools;

internal static partial class AutomationSnapshotFormatter
{
    private static void AppendVideoPipelineSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine("== Video Pipeline ==");
        builder.AppendLine($"Reader: {Get(snapshot, "VideoReaderActive")} | Ingest: {Get(snapshot, "IngestVideoFramesArrived")} arrived, {Get(snapshot, "IngestVideoFramesWrittenToSink")} to sink");
        builder.AppendLine($"Encoder: {Get(snapshot, "EncoderVideoFramesEnqueued")} enqueued, {Get(snapshot, "EncoderVideoFramesEncoded")} encoded | Queue: {Get(snapshot, "FfmpegVideoQueueDepth")}/{Get(snapshot, "RecordingVideoQueueCapacity")} depth, max={Get(snapshot, "RecordingVideoQueueMaxDepth")} overloads={Get(snapshot, "VideoDropsQueueSaturated")}");
        builder.AppendLine($"Recording Detail: submitted={Get(snapshot, "RecordingVideoFramesSubmittedToEncoder")} packets={Get(snapshot, "RecordingVideoEncoderPacketsWritten")} pts={Get(snapshot, "RecordingVideoEncoderPts")} encoderDrops={Get(snapshot, "RecordingVideoEncoderDroppedFrames")} seqGaps={Get(snapshot, "RecordingVideoSequenceGaps")}");
        builder.AppendLine($"Recording Queue Latency: oldest={Get(snapshot, "RecordingVideoQueueOldestFrameAgeMs")}ms last={Get(snapshot, "RecordingVideoQueueLastLatencyMs")}ms avg={Get(snapshot, "RecordingVideoQueueLatencyAvgMs")}ms P95={Get(snapshot, "RecordingVideoQueueLatencyP95Ms")}ms P99={Get(snapshot, "RecordingVideoQueueLatencyP99Ms")}ms max={Get(snapshot, "RecordingVideoQueueLatencyMaxMs")}ms samples={Get(snapshot, "RecordingVideoQueueLatencySampleCount")}");
        builder.AppendLine($"Recording Backpressure: total={Get(snapshot, "RecordingVideoBackpressureWaitMs")}ms events={Get(snapshot, "RecordingVideoBackpressureEvents")} last={Get(snapshot, "RecordingVideoBackpressureLastWaitMs")}ms max={Get(snapshot, "RecordingVideoBackpressureMaxWaitMs")}ms");
        builder.AppendLine($"Encoder Failure: active={Get(snapshot, "RecordingEncodingFailed")} type={Get(snapshot, "RecordingEncodingFailureType", "None")} msg={Get(snapshot, "RecordingEncodingFailureMessage", "")}");
        builder.AppendLine($"GPU Queue: {Get(snapshot, "RecordingGpuQueueDepth")}/{Get(snapshot, "RecordingGpuQueueCapacity")} max={Get(snapshot, "RecordingGpuQueueMaxDepth")} enq={Get(snapshot, "RecordingGpuFramesEnqueued")} overloads={Get(snapshot, "RecordingGpuFramesDropped")} | CUDA: {Get(snapshot, "RecordingCudaQueueDepth")}/{Get(snapshot, "RecordingCudaQueueCapacity")} max={Get(snapshot, "RecordingCudaQueueMaxDepth")} enq={Get(snapshot, "RecordingCudaFramesEnqueued")} overloads={Get(snapshot, "RecordingCudaFramesDropped")}");
        builder.AppendLine($"Freshness: reader {Get(snapshot, "IngestLastVideoFrameAgeMs")}ms | enqueue {Get(snapshot, "EncoderLastEnqueueAgeMs")}ms | write {Get(snapshot, "EncoderLastWriteAgeMs")}ms");
        builder.AppendLine($"Diagnostics: MemPref={Get(snapshot, "MemoryPreference")} ReqSubtype={Get(snapshot, "VideoRequestedSubtype")} NegSubtype={Get(snapshot, "VideoNegotiatedSubtype")} Errors={Get(snapshot, "VideoIngestErrorCount")}");
        AppendThreadHealthSection(builder, snapshot);
        builder.AppendLine();
    }
}
