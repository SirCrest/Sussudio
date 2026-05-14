using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureDiagnosticsSnapshot_DefaultsAndRoundTripsCoreTelemetry()
    {
        var snapshotType = RequireType("Sussudio.Models.CaptureDiagnosticsSnapshot");
        var decoderType = RequireType("Sussudio.Models.MjpegDecoderHealthSnapshot");

        RegisterCaptureDiagnosticsSnapshotProperties(snapshotType);
        AssertDeclaredProperties(
            decoderType,
            new SnapshotPropertySpec[]
            {
                new("WorkerIndex", typeof(int)),
                new("SampleCount", typeof(int)),
                new("AvgMs", typeof(double)),
                new("P95Ms", typeof(double)),
                new("MaxMs", typeof(double))
            });

        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var snapshot = CreateInstance("Sussudio.Models.CaptureDiagnosticsSnapshot");
        var after = DateTimeOffset.UtcNow.AddSeconds(1);
        var timestamp = (DateTimeOffset)GetPropertyValue(snapshot, "TimestampUtc")!;
        if (timestamp < before || timestamp > after)
        {
            throw new InvalidOperationException("CaptureDiagnosticsSnapshot.TimestampUtc should default to current UTC time.");
        }

        AssertEqual(ParseEnum("Sussudio.Models.CaptureSessionState", "Uninitialized"), GetPropertyValue(snapshot, "SessionState"), "CaptureDiagnosticsSnapshot.SessionState default");
        AssertNonNullStringValue(snapshot, "RecordingBackend", "None", "CaptureDiagnosticsSnapshot.RecordingBackend default");
        AssertNonNullStringValue(snapshot, "AudioPathMode", "None", "CaptureDiagnosticsSnapshot.AudioPathMode default");
        AssertNonNullStringValue(snapshot, "MuxResult", "NotAttempted", "CaptureDiagnosticsSnapshot.MuxResult default");
        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Unknown"), GetPropertyValue(snapshot, "SourceTelemetryAvailability"), "CaptureDiagnosticsSnapshot.SourceTelemetryAvailability default");
        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "Unknown"), GetPropertyValue(snapshot, "SourceTelemetryOrigin"), "CaptureDiagnosticsSnapshot.SourceTelemetryOrigin default");
        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "Unknown"), GetPropertyValue(snapshot, "SourceTelemetryConfidence"), "CaptureDiagnosticsSnapshot.SourceTelemetryConfidence default");
        AssertNonNullStringValue(snapshot, "SourceTelemetryOriginDetail", "Unknown", "CaptureDiagnosticsSnapshot.SourceTelemetryOriginDetail default");
        AssertNonNullStringValue(snapshot, "SourceTelemetryBackend", "Unknown", "CaptureDiagnosticsSnapshot.SourceTelemetryBackend default");
        AssertNonNullStringValue(snapshot, "SourceTelemetryCircuitState", "Closed", "CaptureDiagnosticsSnapshot.SourceTelemetryCircuitState default");
        AssertNonNullStringValue(snapshot, "HdrAutoDowngradeReason", string.Empty, "CaptureDiagnosticsSnapshot.HdrAutoDowngradeReason default");
        AssertNonNullStringValue(snapshot, "MjpegPacketHashLastHash", string.Empty, "CaptureDiagnosticsSnapshot.MjpegPacketHashLastHash default");
        AssertNonNullStringValue(snapshot, "MjpegPacketHashPattern", "NoSamples", "CaptureDiagnosticsSnapshot.MjpegPacketHashPattern default");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "MjpegPacketHashRecentInputIntervalsMs")!), "CaptureDiagnosticsSnapshot.MjpegPacketHashRecentInputIntervalsMs default count");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "MjpegPacketHashRecentUniqueIntervalsMs")!), "CaptureDiagnosticsSnapshot.MjpegPacketHashRecentUniqueIntervalsMs default count");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "MjpegPacketHashRecentDuplicateFlags")!), "CaptureDiagnosticsSnapshot.MjpegPacketHashRecentDuplicateFlags default count");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "CaptureCadenceRecentIntervalsMs")!), "CaptureDiagnosticsSnapshot.CaptureCadenceRecentIntervalsMs default count");
        AssertNonNullStringValue(snapshot, "VisualCadenceMotionConfidence", "NoSamples", "CaptureDiagnosticsSnapshot.VisualCadenceMotionConfidence default");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "VisualCadenceRecentOutputIntervalsMs")!), "CaptureDiagnosticsSnapshot.VisualCadenceRecentOutputIntervalsMs default count");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "VisualCadenceRecentChangeIntervalsMs")!), "CaptureDiagnosticsSnapshot.VisualCadenceRecentChangeIntervalsMs default count");
        AssertNonNullStringValue(snapshot, "VisualCenterCadenceMotionConfidence", "NoSamples", "CaptureDiagnosticsSnapshot.VisualCenterCadenceMotionConfidence default");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "VisualCenterCadenceRecentOutputIntervalsMs")!), "CaptureDiagnosticsSnapshot.VisualCenterCadenceRecentOutputIntervalsMs default count");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "VisualCenterCadenceRecentChangeIntervalsMs")!), "CaptureDiagnosticsSnapshot.VisualCenterCadenceRecentChangeIntervalsMs default count");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "MjpegPerDecoder")!), "CaptureDiagnosticsSnapshot.MjpegPerDecoder default count");

        var decoder = CreateMjpegDecoderHealthSnapshot(decoderType, 1, 120, 2.1, 3.4, 5.6);
        var perDecoder = Array.CreateInstance(decoderType, 1);
        perDecoder.SetValue(decoder, 0);
        SetPropertyOrBackingField(snapshot, "SessionState", ParseEnum("Sussudio.Models.CaptureSessionState", "Recording"));
        SetPropertyOrBackingField(snapshot, "IsRecording", true);
        SetPropertyOrBackingField(snapshot, "RecordingBackend", "FFmpeg");
        SetPropertyOrBackingField(snapshot, "NegotiatedWidth", 3840u);
        SetPropertyOrBackingField(snapshot, "NegotiatedHeight", 2160u);
        SetPropertyOrBackingField(snapshot, "SourceTelemetryAvailability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Available"));
        SetPropertyOrBackingField(snapshot, "SourceTelemetryOrigin", ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"));
        SetPropertyOrBackingField(snapshot, "SourceTelemetryConfidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "High"));
        SetPropertyOrBackingField(snapshot, "MjpegDecoderCount", 1);
        SetPropertyOrBackingField(snapshot, "MjpegPerDecoder", perDecoder);
        SetPropertyOrBackingField(snapshot, "VideoDropsQueueSaturated", 2L);
        SetPropertyOrBackingField(snapshot, "RecordingEncodingFailed", true);
        SetPropertyOrBackingField(snapshot, "RecordingEncodingFailureType", "InvalidOperationException");
        SetPropertyOrBackingField(snapshot, "RecordingVideoQueueCapacity", 360);
        SetPropertyOrBackingField(snapshot, "RecordingVideoQueueMaxDepth", 12);
        SetPropertyOrBackingField(snapshot, "RecordingVideoFramesSubmittedToEncoder", 11L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoEncoderPacketsWritten", 10L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoEncoderPts", 12L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoEncoderDroppedFrames", 1L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoSequenceGaps", 2L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoQueueOldestFrameAgeMs", 8L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoQueueLatencyP95Ms", 4.5);
        SetPropertyOrBackingField(snapshot, "RecordingVideoQueueLatencyP99Ms", 6.5);
        SetPropertyOrBackingField(snapshot, "RecordingVideoBackpressureWaitMs", 20L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoBackpressureEvents", 2L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoBackpressureLastWaitMs", 6L);
        SetPropertyOrBackingField(snapshot, "RecordingVideoBackpressureMaxWaitMs", 14L);
        SetPropertyOrBackingField(snapshot, "RecordingGpuFramesDropped", 4L);
        SetPropertyOrBackingField(snapshot, "FlashbackEncodingFailed", true);
        SetPropertyOrBackingField(snapshot, "FlashbackTotalBytesWritten", 2_000_000L);
        SetPropertyOrBackingField(snapshot, "FlashbackTempDriveFreeBytes", 1_000_000L);
        SetPropertyOrBackingField(snapshot, "FlashbackStartupCacheBudgetBytes", 100_000L);
        SetPropertyOrBackingField(snapshot, "FlashbackStartupCacheBytes", 120_000L);
        SetPropertyOrBackingField(snapshot, "FlashbackStartupCacheSessionCount", 3);
        SetPropertyOrBackingField(snapshot, "FlashbackStartupCacheDeletedSessionCount", 2);
        SetPropertyOrBackingField(snapshot, "FlashbackStartupCacheFreedBytes", 80_000L);
        SetPropertyOrBackingField(snapshot, "FlashbackStartupCacheOverBudget", true);
        SetPropertyOrBackingField(snapshot, "FatalCleanupInProgress", true);
        SetPropertyOrBackingField(snapshot, "FlashbackCleanupInProgress", true);
        SetPropertyOrBackingField(snapshot, "FlashbackForceRotateActive", true);
        SetPropertyOrBackingField(snapshot, "FlashbackForceRotateRequested", true);
        SetPropertyOrBackingField(snapshot, "FlashbackForceRotateDraining", true);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoQueueCapacity", 180);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoFramesSubmittedToEncoder", 21L);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoEncoderPacketsWritten", 20L);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoSequenceGaps", 3L);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoQueueOldestFrameAgeMs", 9L);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoQueueLatencyP95Ms", 5.5);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoQueueLatencyP99Ms", 7.5);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoBackpressureWaitMs", 30L);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoBackpressureEvents", 3L);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoBackpressureLastWaitMs", 7L);
        SetPropertyOrBackingField(snapshot, "FlashbackVideoBackpressureMaxWaitMs", 15L);
        SetPropertyOrBackingField(snapshot, "FlashbackGpuFramesDropped", 5L);
        SetPropertyOrBackingField(snapshot, "AudioChunksDropped", 3L);

        var roundTripDecoder = ((Array)GetPropertyValue(snapshot, "MjpegPerDecoder")!).GetValue(0)!;
        AssertEqual(ParseEnum("Sussudio.Models.CaptureSessionState", "Recording"), GetPropertyValue(snapshot, "SessionState"), "CaptureDiagnosticsSnapshot.SessionState round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "IsRecording"), "CaptureDiagnosticsSnapshot.IsRecording round-trip");
        AssertEqual("FFmpeg", GetStringProperty(snapshot, "RecordingBackend"), "CaptureDiagnosticsSnapshot.RecordingBackend round-trip");
        AssertEqual(3840, Convert.ToInt32(GetPropertyValue(snapshot, "NegotiatedWidth")), "CaptureDiagnosticsSnapshot.NegotiatedWidth round-trip");
        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"), GetPropertyValue(snapshot, "SourceTelemetryOrigin"), "CaptureDiagnosticsSnapshot.SourceTelemetryOrigin round-trip");
        AssertEqual(1, GetCountProperty(GetPropertyValue(snapshot, "MjpegPerDecoder")!), "CaptureDiagnosticsSnapshot.MjpegPerDecoder round-trip count");
        AssertEqual(1, GetIntProperty(roundTripDecoder, "WorkerIndex"), "MjpegDecoderHealthSnapshot.WorkerIndex round-trip");
        AssertEqual(120, GetIntProperty(roundTripDecoder, "SampleCount"), "MjpegDecoderHealthSnapshot.SampleCount round-trip");
        AssertEqual(2.1, GetDoubleProperty(roundTripDecoder, "AvgMs"), "MjpegDecoderHealthSnapshot.AvgMs round-trip");
        AssertEqual(3.4, GetDoubleProperty(roundTripDecoder, "P95Ms"), "MjpegDecoderHealthSnapshot.P95Ms round-trip");
        AssertEqual(5.6, GetDoubleProperty(roundTripDecoder, "MaxMs"), "MjpegDecoderHealthSnapshot.MaxMs round-trip");
        AssertEqual(2L, GetLongProperty(snapshot, "VideoDropsQueueSaturated"), "CaptureDiagnosticsSnapshot.VideoDropsQueueSaturated round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "RecordingEncodingFailed"), "CaptureDiagnosticsSnapshot.RecordingEncodingFailed round-trip");
        AssertEqual("InvalidOperationException", GetStringProperty(snapshot, "RecordingEncodingFailureType"), "CaptureDiagnosticsSnapshot.RecordingEncodingFailureType round-trip");
        AssertEqual(360, GetIntProperty(snapshot, "RecordingVideoQueueCapacity"), "CaptureDiagnosticsSnapshot.RecordingVideoQueueCapacity round-trip");
        AssertEqual(12, GetIntProperty(snapshot, "RecordingVideoQueueMaxDepth"), "CaptureDiagnosticsSnapshot.RecordingVideoQueueMaxDepth round-trip");
        AssertEqual(11L, GetLongProperty(snapshot, "RecordingVideoFramesSubmittedToEncoder"), "CaptureDiagnosticsSnapshot.RecordingVideoFramesSubmittedToEncoder round-trip");
        AssertEqual(10L, GetLongProperty(snapshot, "RecordingVideoEncoderPacketsWritten"), "CaptureDiagnosticsSnapshot.RecordingVideoEncoderPacketsWritten round-trip");
        AssertEqual(12L, GetLongProperty(snapshot, "RecordingVideoEncoderPts"), "CaptureDiagnosticsSnapshot.RecordingVideoEncoderPts round-trip");
        AssertEqual(1L, GetLongProperty(snapshot, "RecordingVideoEncoderDroppedFrames"), "CaptureDiagnosticsSnapshot.RecordingVideoEncoderDroppedFrames round-trip");
        AssertEqual(2L, GetLongProperty(snapshot, "RecordingVideoSequenceGaps"), "CaptureDiagnosticsSnapshot.RecordingVideoSequenceGaps round-trip");
        AssertEqual(8L, GetLongProperty(snapshot, "RecordingVideoQueueOldestFrameAgeMs"), "CaptureDiagnosticsSnapshot.RecordingVideoQueueOldestFrameAgeMs round-trip");
        AssertEqual(4.5, GetDoubleProperty(snapshot, "RecordingVideoQueueLatencyP95Ms"), "CaptureDiagnosticsSnapshot.RecordingVideoQueueLatencyP95Ms round-trip");
        AssertEqual(6.5, GetDoubleProperty(snapshot, "RecordingVideoQueueLatencyP99Ms"), "CaptureDiagnosticsSnapshot.RecordingVideoQueueLatencyP99Ms round-trip");
        AssertEqual(20L, GetLongProperty(snapshot, "RecordingVideoBackpressureWaitMs"), "CaptureDiagnosticsSnapshot.RecordingVideoBackpressureWaitMs round-trip");
        AssertEqual(2L, GetLongProperty(snapshot, "RecordingVideoBackpressureEvents"), "CaptureDiagnosticsSnapshot.RecordingVideoBackpressureEvents round-trip");
        AssertEqual(6L, GetLongProperty(snapshot, "RecordingVideoBackpressureLastWaitMs"), "CaptureDiagnosticsSnapshot.RecordingVideoBackpressureLastWaitMs round-trip");
        AssertEqual(14L, GetLongProperty(snapshot, "RecordingVideoBackpressureMaxWaitMs"), "CaptureDiagnosticsSnapshot.RecordingVideoBackpressureMaxWaitMs round-trip");
        AssertEqual(4L, GetLongProperty(snapshot, "RecordingGpuFramesDropped"), "CaptureDiagnosticsSnapshot.RecordingGpuFramesDropped round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "FlashbackEncodingFailed"), "CaptureDiagnosticsSnapshot.FlashbackEncodingFailed round-trip");
        AssertEqual(2_000_000L, GetLongProperty(snapshot, "FlashbackTotalBytesWritten"), "CaptureDiagnosticsSnapshot.FlashbackTotalBytesWritten round-trip");
        AssertEqual(1_000_000L, GetLongProperty(snapshot, "FlashbackTempDriveFreeBytes"), "CaptureDiagnosticsSnapshot.FlashbackTempDriveFreeBytes round-trip");
        AssertEqual(100_000L, GetLongProperty(snapshot, "FlashbackStartupCacheBudgetBytes"), "CaptureDiagnosticsSnapshot.FlashbackStartupCacheBudgetBytes round-trip");
        AssertEqual(120_000L, GetLongProperty(snapshot, "FlashbackStartupCacheBytes"), "CaptureDiagnosticsSnapshot.FlashbackStartupCacheBytes round-trip");
        AssertEqual(3, GetIntProperty(snapshot, "FlashbackStartupCacheSessionCount"), "CaptureDiagnosticsSnapshot.FlashbackStartupCacheSessionCount round-trip");
        AssertEqual(2, GetIntProperty(snapshot, "FlashbackStartupCacheDeletedSessionCount"), "CaptureDiagnosticsSnapshot.FlashbackStartupCacheDeletedSessionCount round-trip");
        AssertEqual(80_000L, GetLongProperty(snapshot, "FlashbackStartupCacheFreedBytes"), "CaptureDiagnosticsSnapshot.FlashbackStartupCacheFreedBytes round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "FlashbackStartupCacheOverBudget"), "CaptureDiagnosticsSnapshot.FlashbackStartupCacheOverBudget round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "FatalCleanupInProgress"), "CaptureDiagnosticsSnapshot.FatalCleanupInProgress round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "FlashbackCleanupInProgress"), "CaptureDiagnosticsSnapshot.FlashbackCleanupInProgress round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "FlashbackForceRotateActive"), "CaptureDiagnosticsSnapshot.FlashbackForceRotateActive round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "FlashbackForceRotateRequested"), "CaptureDiagnosticsSnapshot.FlashbackForceRotateRequested round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "FlashbackForceRotateDraining"), "CaptureDiagnosticsSnapshot.FlashbackForceRotateDraining round-trip");
        AssertEqual(180, GetIntProperty(snapshot, "FlashbackVideoQueueCapacity"), "CaptureDiagnosticsSnapshot.FlashbackVideoQueueCapacity round-trip");
        AssertEqual(21L, GetLongProperty(snapshot, "FlashbackVideoFramesSubmittedToEncoder"), "CaptureDiagnosticsSnapshot.FlashbackVideoFramesSubmittedToEncoder round-trip");
        AssertEqual(20L, GetLongProperty(snapshot, "FlashbackVideoEncoderPacketsWritten"), "CaptureDiagnosticsSnapshot.FlashbackVideoEncoderPacketsWritten round-trip");
        AssertEqual(3L, GetLongProperty(snapshot, "FlashbackVideoSequenceGaps"), "CaptureDiagnosticsSnapshot.FlashbackVideoSequenceGaps round-trip");
        AssertEqual(9L, GetLongProperty(snapshot, "FlashbackVideoQueueOldestFrameAgeMs"), "CaptureDiagnosticsSnapshot.FlashbackVideoQueueOldestFrameAgeMs round-trip");
        AssertEqual(5.5, GetDoubleProperty(snapshot, "FlashbackVideoQueueLatencyP95Ms"), "CaptureDiagnosticsSnapshot.FlashbackVideoQueueLatencyP95Ms round-trip");
        AssertEqual(7.5, GetDoubleProperty(snapshot, "FlashbackVideoQueueLatencyP99Ms"), "CaptureDiagnosticsSnapshot.FlashbackVideoQueueLatencyP99Ms round-trip");
        AssertEqual(30L, GetLongProperty(snapshot, "FlashbackVideoBackpressureWaitMs"), "CaptureDiagnosticsSnapshot.FlashbackVideoBackpressureWaitMs round-trip");
        AssertEqual(3L, GetLongProperty(snapshot, "FlashbackVideoBackpressureEvents"), "CaptureDiagnosticsSnapshot.FlashbackVideoBackpressureEvents round-trip");
        AssertEqual(7L, GetLongProperty(snapshot, "FlashbackVideoBackpressureLastWaitMs"), "CaptureDiagnosticsSnapshot.FlashbackVideoBackpressureLastWaitMs round-trip");
        AssertEqual(15L, GetLongProperty(snapshot, "FlashbackVideoBackpressureMaxWaitMs"), "CaptureDiagnosticsSnapshot.FlashbackVideoBackpressureMaxWaitMs round-trip");
        AssertEqual(5L, GetLongProperty(snapshot, "FlashbackGpuFramesDropped"), "CaptureDiagnosticsSnapshot.FlashbackGpuFramesDropped round-trip");
        AssertEqual(3L, GetLongProperty(snapshot, "AudioChunksDropped"), "CaptureDiagnosticsSnapshot.AudioChunksDropped round-trip");
        var decoderJsonRoundTrip = ReflectionJsonRoundTrip(decoderType, decoder);
        AssertEqual(120, GetIntProperty(decoderJsonRoundTrip, "SampleCount"), "MjpegDecoderHealthSnapshot JSON SampleCount");
        var jsonRoundTrip = ReflectionJsonRoundTrip(snapshotType, snapshot);
        AssertEqual("FFmpeg", GetStringProperty(jsonRoundTrip, "RecordingBackend"), "CaptureDiagnosticsSnapshot JSON RecordingBackend");
        AssertEqual(true, GetBoolProperty(jsonRoundTrip, "RecordingEncodingFailed"), "CaptureDiagnosticsSnapshot JSON RecordingEncodingFailed");
        AssertEqual(2_000_000L, GetLongProperty(jsonRoundTrip, "FlashbackTotalBytesWritten"), "CaptureDiagnosticsSnapshot JSON FlashbackTotalBytesWritten");
        AssertEqual(120_000L, GetLongProperty(jsonRoundTrip, "FlashbackStartupCacheBytes"), "CaptureDiagnosticsSnapshot JSON FlashbackStartupCacheBytes");
        AssertEqual(true, GetBoolProperty(jsonRoundTrip, "FatalCleanupInProgress"), "CaptureDiagnosticsSnapshot JSON FatalCleanupInProgress");
        AssertEqual(true, GetBoolProperty(jsonRoundTrip, "FlashbackCleanupInProgress"), "CaptureDiagnosticsSnapshot JSON FlashbackCleanupInProgress");
        AssertEqual(true, GetBoolProperty(jsonRoundTrip, "FlashbackForceRotateActive"), "CaptureDiagnosticsSnapshot JSON FlashbackForceRotateActive");
        AssertEqual(true, GetBoolProperty(jsonRoundTrip, "FlashbackForceRotateRequested"), "CaptureDiagnosticsSnapshot JSON FlashbackForceRotateRequested");
        AssertEqual(true, GetBoolProperty(jsonRoundTrip, "FlashbackForceRotateDraining"), "CaptureDiagnosticsSnapshot JSON FlashbackForceRotateDraining");
        AssertEqual(180, GetIntProperty(jsonRoundTrip, "FlashbackVideoQueueCapacity"), "CaptureDiagnosticsSnapshot JSON FlashbackVideoQueueCapacity");
        AssertEqual(1, GetCountProperty(GetPropertyValue(jsonRoundTrip, "MjpegPerDecoder")!), "CaptureDiagnosticsSnapshot JSON MjpegPerDecoder count");
        AssertEqual(1, GetIntProperty(GetSingleEnumerableItem(GetPropertyValue(jsonRoundTrip, "MjpegPerDecoder")!), "WorkerIndex"), "CaptureDiagnosticsSnapshot JSON MjpegPerDecoder WorkerIndex");

        return Task.CompletedTask;
    }

}
