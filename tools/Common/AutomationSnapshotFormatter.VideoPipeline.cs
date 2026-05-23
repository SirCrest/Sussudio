using System.Text;
using System.Text.Json;
using System;

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

    private static void AppendThreadHealthSection(StringBuilder builder, JsonElement snapshot)
    {
        builder.AppendLine();
        builder.AppendLine("== Thread Health ==");
        AppendSourceReaderThreadHealthLine(builder, snapshot);
        AppendWasapiCaptureThreadHealthLine(builder, snapshot);
        AppendWasapiPlaybackThreadHealthLine(builder, snapshot);
    }

    private static void AppendSourceReaderThreadHealthLine(StringBuilder builder, JsonElement snapshot)
    {
        var sourceReaderLastFrameAgeMs = ComputeTickAgeMs(GetLong(snapshot, "SourceReaderLastFrameTickMs"));
        var sourceReaderOutstanding = Get(snapshot, "SourceReaderReadOutstanding");
        var sourceReaderOutstandingSuffix = string.Equals(sourceReaderOutstanding, "true", StringComparison.OrdinalIgnoreCase)
            ? $" outstandingFor={Get(snapshot, "SourceReaderReadOutstandingMs")}ms"
            : string.Empty;
        builder.AppendLine(
            $"Source Reader: outstanding={sourceReaderOutstanding}{sourceReaderOutstandingSuffix} " +
            $"lastFrame={sourceReaderLastFrameAgeMs}ms ago channelDepth={Get(snapshot, "SourceReaderFrameChannelDepth")}");
    }

    private static void AppendWasapiCaptureThreadHealthLine(StringBuilder builder, JsonElement snapshot)
    {
        var wasapiCaptureLastCallbackAgeMs = ComputeTickAgeMs(GetLong(snapshot, "WasapiCaptureLastCallbackTickMs"));
        builder.AppendLine(
            $"WASAPI Capture: callbacks={Get(snapshot, "WasapiCaptureCallbackCount")} " +
            $"interval={Get(snapshot, "WasapiCaptureCallbackAvgIntervalMs")}ms/avg {Get(snapshot, "WasapiCaptureCallbackMaxIntervalMs")}ms/max " +
            $"silence={Get(snapshot, "WasapiCaptureCallbackSilenceCount")} " +
            $"lastCallback={wasapiCaptureLastCallbackAgeMs}ms ago " +
            $"levelEvents={Get(snapshot, "WasapiCaptureAudioLevelEventsFired")} " +
            $"glitches={Get(snapshot, "WasapiCaptureAudioGlitchCount")} " +
            $"disc={Get(snapshot, "WasapiCaptureAudioDiscontinuityCount")} " +
            $"tsErr={Get(snapshot, "WasapiCaptureAudioTimestampErrorCount")} " +
            $"severeGaps={Get(snapshot, "WasapiCaptureCallbackSevereGapCount")}");
    }

    private static void AppendWasapiPlaybackThreadHealthLine(StringBuilder builder, JsonElement snapshot)
    {
        var wasapiPlaybackLastRenderAgeMs = ComputeTickAgeMs(GetLong(snapshot, "WasapiPlaybackLastRenderTickMs"));
        builder.AppendLine(
            $"WASAPI Playback: callbacks={Get(snapshot, "WasapiPlaybackRenderCallbackCount")} " +
            $"silence={Get(snapshot, "WasapiPlaybackRenderSilenceCount")} " +
            $"queueDepth={Get(snapshot, "WasapiPlaybackQueueDepth")} " +
            $"queueMs={Get(snapshot, "WasapiPlaybackQueueDurationMs")} " +
            $"activeMs={Get(snapshot, "WasapiPlaybackActiveChunkDurationMs")} " +
            $"endpointMs={Get(snapshot, "WasapiPlaybackEndpointQueuedDurationMs")} " +
            $"bufferedMs={Get(snapshot, "WasapiPlaybackBufferedDurationMs")} " +
            $"streamLatencyMs={Get(snapshot, "WasapiPlaybackStreamLatencyMs")} " +
            $"drops={Get(snapshot, "WasapiPlaybackQueueDropCount")} " +
            $"lastCallback={wasapiPlaybackLastRenderAgeMs}ms ago");
    }
}
