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

    private static Task CaptureHealthSnapshot_ExtendsDiagnosticsWithFlashbackSourceAndAvSync()
    {
        var diagnosticsType = RequireType("Sussudio.Models.CaptureDiagnosticsSnapshot");
        var healthType = RequireType("Sussudio.Models.CaptureHealthSnapshot");
        var detailType = RequireType("Sussudio.Models.SourceTelemetryDetailEntry");
        if (!healthType.IsSealed)
        {
            throw new InvalidOperationException("CaptureHealthSnapshot must remain sealed.");
        }

        if (!diagnosticsType.IsAssignableFrom(healthType))
        {
            throw new InvalidOperationException("CaptureHealthSnapshot must inherit CaptureDiagnosticsSnapshot.");
        }

        RegisterCaptureDiagnosticsSnapshotProperties(diagnosticsType);
        AssertDeclaredProperties(
            healthType,
            new SnapshotPropertySpec[]
            {
                new("FlashbackOutputBytes", typeof(long)),
                NullableString("FlashbackFilePath"),
                new("FlashbackEncodedFrames", typeof(long)),
                new("FlashbackDroppedFrames", typeof(long)),
                new("FlashbackGpuEncoding", typeof(bool)),
                new("FlashbackBackendSettingsStale", typeof(bool)),
                NonNullString("FlashbackBackendSettingsStaleReason"),
                NonNullString("FlashbackBackendActiveFormat"),
                NonNullString("FlashbackBackendRequestedFormat"),
                NonNullString("FlashbackBackendActivePreset"),
                NonNullString("FlashbackBackendRequestedPreset"),
                NullableString("EncoderCodecName"),
                new("EncoderTargetBitRate", typeof(uint)),
                new("EncoderWidth", typeof(int)),
                new("EncoderHeight", typeof(int)),
                new("EncoderFrameRate", typeof(double)),
                new("FlashbackVideoQueueDepth", typeof(int)),
                new("FlashbackAudioQueueDepth", typeof(int)),
                new("FlashbackAudioQueueCapacity", typeof(int)),
                NonNullString("FlashbackPlaybackState"),
                new("FlashbackPlaybackPositionMs", typeof(long)),
                NonNullString("FlashbackDecoderHwAccel"),
                new("FlashbackPlaybackFrameCount", typeof(long)),
                new("FlashbackPlaybackLateFrames", typeof(long)),
                new("FlashbackPlaybackDroppedFrames", typeof(long)),
                new("FlashbackPlaybackAudioMasterDelayDoubles", typeof(long)),
                new("FlashbackPlaybackAudioMasterDelayShrinks", typeof(long)),
                new("FlashbackPlaybackAudioMasterFallbacks", typeof(long)),
                new("FlashbackPlaybackAudioMasterUnavailableFallbacks", typeof(long)),
                new("FlashbackPlaybackAudioMasterStaleFallbacks", typeof(long)),
                new("FlashbackPlaybackAudioMasterDriftOutlierFallbacks", typeof(long)),
                NonNullString("FlashbackPlaybackAudioMasterLastFallbackReason"),
                new("FlashbackPlaybackAudioMasterLastFallbackDriftMs", typeof(double)),
                new("FlashbackPlaybackAudioMasterLastFallbackClockAgeMs", typeof(double)),
                new("FlashbackPlaybackSegmentSwitches", typeof(long)),
                new("FlashbackPlaybackFmp4Reopens", typeof(long)),
                new("FlashbackPlaybackWriteHeadWaits", typeof(long)),
                new("FlashbackPlaybackNearLiveSnaps", typeof(long)),
                new("FlashbackPlaybackDecodeErrorSnaps", typeof(long)),
                new("FlashbackPlaybackSubmitFailures", typeof(long)),
                new("FlashbackPlaybackLastDropUtcUnixMs", typeof(long)),
                NonNullString("FlashbackPlaybackLastDropReason"),
                new("FlashbackPlaybackLastSubmitFailureUtcUnixMs", typeof(long)),
                NonNullString("FlashbackPlaybackLastSubmitFailure"),
                new("FlashbackPlaybackLastSegmentSwitchUtcUnixMs", typeof(long)),
                new("FlashbackPlaybackLastFmp4ReopenUtcUnixMs", typeof(long)),
                new("FlashbackPlaybackLastWriteHeadWaitGapMs", typeof(long)),
                new("FlashbackPlaybackTargetFps", typeof(double)),
                new("FlashbackPlaybackObservedFps", typeof(double)),
                new("FlashbackPlaybackAvgFrameMs", typeof(double)),
                new("FlashbackPlaybackCadenceSampleCount", typeof(int)),
                new("FlashbackPlaybackP95FrameMs", typeof(double)),
                new("FlashbackPlaybackP99FrameMs", typeof(double)),
                new("FlashbackPlaybackMaxFrameMs", typeof(double)),
                new("FlashbackPlaybackSlowFrames", typeof(long)),
                new("FlashbackPlaybackSlowFramePercent", typeof(double)),
                new("FlashbackPlaybackOnePercentLowFps", typeof(double)),
                new("FlashbackPlaybackFivePercentLowFps", typeof(double)),
                new("FlashbackPlaybackSampleDurationMs", typeof(double)),
                new("FlashbackPlaybackRecentFrameIntervalsMs", typeof(double[])),
                new("FlashbackPlaybackPtsCadenceMismatchCount", typeof(long)),
                new("FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs", typeof(long)),
                new("FlashbackPlaybackLastPtsCadenceDeltaMs", typeof(double)),
                new("FlashbackPlaybackLastPtsCadenceExpectedMs", typeof(double)),
                new("FlashbackPlaybackSeekForwardDecodeCapHits", typeof(long)),
                new("FlashbackPlaybackLastSeekHitForwardDecodeCap", typeof(bool)),
                new("FlashbackPlaybackDecodeSampleCount", typeof(int)),
                new("FlashbackPlaybackDecodeAvgMs", typeof(double)),
                new("FlashbackPlaybackDecodeP95Ms", typeof(double)),
                new("FlashbackPlaybackDecodeP99Ms", typeof(double)),
                new("FlashbackPlaybackDecodeMaxMs", typeof(double)),
                NonNullString("FlashbackPlaybackMaxDecodePhase"),
                new("FlashbackPlaybackMaxDecodeReceiveMs", typeof(double)),
                new("FlashbackPlaybackMaxDecodeFeedMs", typeof(double)),
                new("FlashbackPlaybackMaxDecodeReadMs", typeof(double)),
                new("FlashbackPlaybackMaxDecodeSendMs", typeof(double)),
                new("FlashbackPlaybackMaxDecodeAudioMs", typeof(double)),
                new("FlashbackPlaybackMaxDecodeConvertMs", typeof(double)),
                new("FlashbackPlaybackMaxDecodeUtcUnixMs", typeof(long)),
                new("FlashbackPlaybackMaxDecodePositionMs", typeof(long)),
                new("FlashbackAvDriftMs", typeof(double)),
                new("FlashbackPlaybackThreadAlive", typeof(bool)),
                new("FlashbackPlaybackCommandsEnqueued", typeof(long)),
                new("FlashbackPlaybackCommandsProcessed", typeof(long)),
                new("FlashbackPlaybackCommandsDropped", typeof(long)),
                new("FlashbackPlaybackCommandsSkippedNotReady", typeof(long)),
                new("FlashbackPlaybackScrubUpdatesCoalesced", typeof(long)),
                new("FlashbackPlaybackSeekCommandsCoalesced", typeof(long)),
                new("FlashbackPlaybackCommandQueueCapacity", typeof(int)),
                new("FlashbackPlaybackPendingCommands", typeof(int)),
                new("FlashbackPlaybackMaxPendingCommands", typeof(int)),
                new("FlashbackPlaybackLastCommandQueueLatencyMs", typeof(long)),
                new("FlashbackPlaybackMaxCommandQueueLatencyMs", typeof(long)),
                NonNullString("FlashbackPlaybackMaxCommandQueueLatencyCommand"),
                NonNullString("FlashbackPlaybackLastCommandQueued"),
                NonNullString("FlashbackPlaybackLastCommandProcessed"),
                new("FlashbackPlaybackLastCommandQueuedUtcUnixMs", typeof(long)),
                new("FlashbackPlaybackLastCommandProcessedUtcUnixMs", typeof(long)),
                new("FlashbackPlaybackLastCommandFailureUtcUnixMs", typeof(long)),
                NonNullString("FlashbackPlaybackLastCommandFailure"),
                new("FlashbackExportActive", typeof(bool)),
                new("FlashbackExportId", typeof(long)),
                NonNullString("FlashbackExportStatus"),
                NonNullString("FlashbackExportOutputPath"),
                new("FlashbackExportStartedUtcUnixMs", typeof(long)),
                new("FlashbackExportLastProgressUtcUnixMs", typeof(long)),
                new("FlashbackExportCompletedUtcUnixMs", typeof(long)),
                new("FlashbackExportElapsedMs", typeof(long)),
                new("FlashbackExportLastProgressAgeMs", typeof(long)),
                new("FlashbackExportOutputBytes", typeof(long)),
                new("FlashbackExportThroughputBytesPerSec", typeof(double)),
                new("FlashbackExportSegmentsProcessed", typeof(int)),
                new("FlashbackExportTotalSegments", typeof(int)),
                new("FlashbackExportPercent", typeof(double)),
                new("FlashbackExportInPointMs", typeof(long)),
                new("FlashbackExportOutPointMs", typeof(long)),
                NonNullString("FlashbackExportMessage"),
                NonNullString("FlashbackExportFailureKind"),
                new("FlashbackExportForceRotateFallbacks", typeof(long)),
                new("FlashbackExportLastForceRotateFallbackUtcUnixMs", typeof(long)),
                new("FlashbackExportLastForceRotateFallbackSegments", typeof(int)),
                new("FlashbackExportLastForceRotateFallbackInPointMs", typeof(long)),
                new("FlashbackExportLastForceRotateFallbackOutPointMs", typeof(long)),
                NullableString("FlashbackExportVerificationFormat"),
                NullableString("FlashbackCodecDowngradeReason"),
                new("LastExportId", typeof(long)),
                NullableString("LastExportPath"),
                new("LastExportSuccess", typeof(bool?)),
                NullableString("LastExportMessage"),
                NullableString("SourceVideoFormat"),
                NullableString("SourceColorimetry"),
                NullableString("SourceQuantization"),
                NullableString("SourceHdrTransferFunction"),
                new("SourceHdrTransferCode", typeof(int?)),
                NullableString("SourceFirmware"),
                NullableString("SourceAudioFormat"),
                NullableString("SourceAudioSampleRate"),
                NullableString("SourceInputSource"),
                NullableString("SourceUsbHostProtocol"),
                NullableString("SourceHdcpMode"),
                NullableString("SourceHdcpVersion"),
                NullableString("SourceRxTxHdcpVersion"),
                NullableString("SourceRawTimingHex"),
                NonNullRef("SourceTelemetryDetails", typeof(IReadOnlyList<>).MakeGenericType(detailType), SnapshotNullability.NotNull),
                new("LastVideoEnqueueAgeMs", typeof(long)),
                new("LastVideoWriteAgeMs", typeof(long)),
                new("AvSyncCaptureDriftMs", typeof(double?)),
                new("AvSyncCaptureDriftRateMsPerSec", typeof(double?)),
                new("AvSyncEncoderDriftMs", typeof(double?)),
                new("AvSyncEncoderCorrectionSamples", typeof(long?))
            });
        AssertDeclaredProperties(
            detailType,
            new SnapshotPropertySpec[]
            {
                NonNullString("Group"),
                NonNullString("Label"),
                NonNullString("DisplayValue"),
                NullableString("RawValue")
            });

        var health = CreateInstance("Sussudio.Models.CaptureHealthSnapshot");
        AssertNonNullStringValue(health, "RecordingBackend", "None", "CaptureHealthSnapshot inherited RecordingBackend default");
        AssertNonNullStringValue(health, "FlashbackPlaybackState", "N/A", "CaptureHealthSnapshot.FlashbackPlaybackState default");
        AssertNonNullStringValue(health, "FlashbackDecoderHwAccel", "N/A", "CaptureHealthSnapshot.FlashbackDecoderHwAccel default");
        AssertNonNullStringValue(health, "FlashbackPlaybackMaxCommandQueueLatencyCommand", "None", "CaptureHealthSnapshot.FlashbackPlaybackMaxCommandQueueLatencyCommand default");
        AssertNonNullStringValue(health, "FlashbackPlaybackLastCommandQueued", "None", "CaptureHealthSnapshot.FlashbackPlaybackLastCommandQueued default");
        AssertNonNullStringValue(health, "FlashbackPlaybackLastCommandProcessed", "None", "CaptureHealthSnapshot.FlashbackPlaybackLastCommandProcessed default");
        AssertNonNullStringValue(health, "FlashbackExportStatus", "NotStarted", "CaptureHealthSnapshot.FlashbackExportStatus default");
        AssertNonNullStringValue(health, "FlashbackExportFailureKind", string.Empty, "CaptureHealthSnapshot.FlashbackExportFailureKind default");
        AssertEqual(0, GetCountProperty(GetPropertyValue(health, "SourceTelemetryDetails")!), "CaptureHealthSnapshot.SourceTelemetryDetails default count");

        var detailEntry = Activator.CreateInstance(detailType, "Signal", "Colorimetry", "BT.2020", "bt2020")
            ?? throw new InvalidOperationException("Failed to create SourceTelemetryDetailEntry.");
        AssertEqual("Signal", GetStringProperty(detailEntry, "Group"), "SourceTelemetryDetailEntry.Group");
        AssertEqual("Colorimetry", GetStringProperty(detailEntry, "Label"), "SourceTelemetryDetailEntry.Label");
        AssertEqual("BT.2020", GetStringProperty(detailEntry, "DisplayValue"), "SourceTelemetryDetailEntry.DisplayValue");
        AssertEqual("bt2020", GetStringProperty(detailEntry, "RawValue"), "SourceTelemetryDetailEntry.RawValue");
        var details = CreateGenericList(detailType, detailEntry);

        SetPropertyOrBackingField(health, "RecordingBackend", "FFmpeg");
        SetPropertyOrBackingField(health, "FlashbackOutputBytes", 123456L);
        SetPropertyOrBackingField(health, "FlashbackFilePath", "flashback.ts");
        SetPropertyOrBackingField(health, "FlashbackPlaybackState", "Paused");
        SetPropertyOrBackingField(health, "FlashbackDecoderHwAccel", "D3D11");
        SetPropertyOrBackingField(health, "FlashbackPlaybackDroppedFrames", 4L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackSegmentSwitches", 2L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackFmp4Reopens", 3L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackWriteHeadWaits", 5L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackNearLiveSnaps", 1L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackDecodeErrorSnaps", 0L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackSubmitFailures", 6L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastDropUtcUnixMs", 666L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastDropReason", "av_sync_skip");
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastSubmitFailureUtcUnixMs", 777L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastSubmitFailure", "seek:null_texture");
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastSegmentSwitchUtcUnixMs", 123L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastFmp4ReopenUtcUnixMs", 456L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastWriteHeadWaitGapMs", 789L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackTargetFps", 120d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackPtsCadenceMismatchCount", 2L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs", 123456700L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastPtsCadenceDeltaMs", 16.67d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastPtsCadenceExpectedMs", 8.33d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackSeekForwardDecodeCapHits", 3L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastSeekHitForwardDecodeCap", true);
        SetPropertyOrBackingField(health, "FlashbackPlaybackDecodeSampleCount", 120);
        SetPropertyOrBackingField(health, "FlashbackPlaybackDecodeAvgMs", 1.25d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackDecodeP95Ms", 2.5d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackDecodeP99Ms", 3.5d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackDecodeMaxMs", 4.5d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodePhase", "audio");
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodeReceiveMs", 0.5d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodeFeedMs", 4.0d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodeReadMs", 0.75d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodeSendMs", 3.5d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodeAudioMs", 3.25d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodeConvertMs", 0.25d);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodeUtcUnixMs", 123456789L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxDecodePositionMs", 2345L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackCommandsEnqueued", 9L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackScrubUpdatesCoalesced", 7L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackSeekCommandsCoalesced", 8L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackCommandQueueCapacity", 256);
        SetPropertyOrBackingField(health, "FlashbackPlaybackPendingCommands", 2);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxPendingCommands", 5);
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastCommandQueueLatencyMs", 14L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxCommandQueueLatencyMs", 88L);
        SetPropertyOrBackingField(health, "FlashbackPlaybackMaxCommandQueueLatencyCommand", "Play");
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastCommandQueued", "UpdateScrub");
        SetPropertyOrBackingField(health, "FlashbackPlaybackLastCommandFailureUtcUnixMs", 999L);
        SetPropertyOrBackingField(health, "FlashbackVideoQueueRejectedFrames", 11L);
        SetPropertyOrBackingField(health, "FlashbackVideoQueueLastRejectReason", "force_rotate_draining");
        SetPropertyOrBackingField(health, "FlashbackGpuQueueRejectedFrames", 13L);
        SetPropertyOrBackingField(health, "FlashbackGpuQueueLastRejectReason", "encoding_failed:InvalidOperationException");
        SetPropertyOrBackingField(health, "FlashbackBackendSettingsStale", true);
        SetPropertyOrBackingField(health, "FlashbackBackendSettingsStaleReason", "preset:P1->P2");
        SetPropertyOrBackingField(health, "FlashbackBackendActiveFormat", "HevcMp4");
        SetPropertyOrBackingField(health, "FlashbackBackendRequestedFormat", "HevcMp4");
        SetPropertyOrBackingField(health, "FlashbackBackendActivePreset", "P1");
        SetPropertyOrBackingField(health, "FlashbackBackendRequestedPreset", "P2");
        SetPropertyOrBackingField(health, "FlashbackExportActive", true);
        SetPropertyOrBackingField(health, "FlashbackExportStatus", "Running");
        SetPropertyOrBackingField(health, "FlashbackExportFailureKind", "NoMediaWritten");
        SetPropertyOrBackingField(health, "FlashbackExportForceRotateFallbacks", 2L);
        SetPropertyOrBackingField(health, "FlashbackExportLastForceRotateFallbackUtcUnixMs", 12345L);
        SetPropertyOrBackingField(health, "FlashbackExportLastForceRotateFallbackSegments", 3);
        SetPropertyOrBackingField(health, "FlashbackExportLastForceRotateFallbackInPointMs", 1000L);
        SetPropertyOrBackingField(health, "FlashbackExportLastForceRotateFallbackOutPointMs", 9000L);
        SetPropertyOrBackingField(health, "FlashbackExportVerificationFormat", "HevcMp4");
        SetPropertyOrBackingField(health, "FlashbackCodecDowngradeReason", "AV1->HEVC");
        SetPropertyOrBackingField(health, "FlashbackExportPercent", 37.5d);
        SetPropertyOrBackingField(health, "FlashbackExportElapsedMs", 2000L);
        SetPropertyOrBackingField(health, "FlashbackExportLastProgressAgeMs", 100L);
        SetPropertyOrBackingField(health, "FlashbackExportOutputBytes", 1048576L);
        SetPropertyOrBackingField(health, "FlashbackExportThroughputBytesPerSec", 524288d);
        SetPropertyOrBackingField(health, "FlashbackExportSegmentsProcessed", 3);
        SetPropertyOrBackingField(health, "LastExportId", 42L);
        SetPropertyOrBackingField(health, "SourceVideoFormat", "YCbCr422");
        SetPropertyOrBackingField(health, "SourceHdrTransferCode", 2);
        SetPropertyOrBackingField(health, "SourceTelemetryDetails", details);
        SetPropertyOrBackingField(health, "LastVideoEnqueueAgeMs", 17L);
        SetPropertyOrBackingField(health, "AvSyncCaptureDriftMs", -1.5d);
        SetPropertyOrBackingField(health, "AvSyncEncoderCorrectionSamples", 48L);

        var roundTripDetail = GetSingleEnumerableItem(GetPropertyValue(health, "SourceTelemetryDetails")!);
        AssertEqual("FFmpeg", GetStringProperty(health, "RecordingBackend"), "CaptureHealthSnapshot inherited RecordingBackend round-trip");
        AssertEqual(123456L, GetLongProperty(health, "FlashbackOutputBytes"), "CaptureHealthSnapshot.FlashbackOutputBytes round-trip");
        AssertEqual("flashback.ts", GetStringProperty(health, "FlashbackFilePath"), "CaptureHealthSnapshot.FlashbackFilePath round-trip");
        AssertEqual("Paused", GetStringProperty(health, "FlashbackPlaybackState"), "CaptureHealthSnapshot.FlashbackPlaybackState round-trip");
        AssertEqual("D3D11", GetStringProperty(health, "FlashbackDecoderHwAccel"), "CaptureHealthSnapshot.FlashbackDecoderHwAccel round-trip");
        AssertEqual(4L, GetLongProperty(health, "FlashbackPlaybackDroppedFrames"), "CaptureHealthSnapshot.FlashbackPlaybackDroppedFrames round-trip");
        AssertEqual(2L, GetLongProperty(health, "FlashbackPlaybackSegmentSwitches"), "CaptureHealthSnapshot.FlashbackPlaybackSegmentSwitches round-trip");
        AssertEqual(3L, GetLongProperty(health, "FlashbackPlaybackFmp4Reopens"), "CaptureHealthSnapshot.FlashbackPlaybackFmp4Reopens round-trip");
        AssertEqual(5L, GetLongProperty(health, "FlashbackPlaybackWriteHeadWaits"), "CaptureHealthSnapshot.FlashbackPlaybackWriteHeadWaits round-trip");
        AssertEqual(1L, GetLongProperty(health, "FlashbackPlaybackNearLiveSnaps"), "CaptureHealthSnapshot.FlashbackPlaybackNearLiveSnaps round-trip");
        AssertEqual(0L, GetLongProperty(health, "FlashbackPlaybackDecodeErrorSnaps"), "CaptureHealthSnapshot.FlashbackPlaybackDecodeErrorSnaps round-trip");
        AssertEqual(6L, GetLongProperty(health, "FlashbackPlaybackSubmitFailures"), "CaptureHealthSnapshot.FlashbackPlaybackSubmitFailures round-trip");
        AssertEqual(666L, GetLongProperty(health, "FlashbackPlaybackLastDropUtcUnixMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastDropUtcUnixMs round-trip");
        AssertEqual("av_sync_skip", GetStringProperty(health, "FlashbackPlaybackLastDropReason"), "CaptureHealthSnapshot.FlashbackPlaybackLastDropReason round-trip");
        AssertEqual(777L, GetLongProperty(health, "FlashbackPlaybackLastSubmitFailureUtcUnixMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastSubmitFailureUtcUnixMs round-trip");
        AssertEqual("seek:null_texture", GetStringProperty(health, "FlashbackPlaybackLastSubmitFailure"), "CaptureHealthSnapshot.FlashbackPlaybackLastSubmitFailure round-trip");
        AssertEqual(123L, GetLongProperty(health, "FlashbackPlaybackLastSegmentSwitchUtcUnixMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastSegmentSwitchUtcUnixMs round-trip");
        AssertEqual(456L, GetLongProperty(health, "FlashbackPlaybackLastFmp4ReopenUtcUnixMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastFmp4ReopenUtcUnixMs round-trip");
        AssertEqual(789L, GetLongProperty(health, "FlashbackPlaybackLastWriteHeadWaitGapMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastWriteHeadWaitGapMs round-trip");
        AssertEqual(120d, GetDoubleProperty(health, "FlashbackPlaybackTargetFps"), "CaptureHealthSnapshot.FlashbackPlaybackTargetFps round-trip");
        AssertEqual(2L, GetLongProperty(health, "FlashbackPlaybackPtsCadenceMismatchCount"), "CaptureHealthSnapshot.FlashbackPlaybackPtsCadenceMismatchCount round-trip");
        AssertEqual(123456700L, GetLongProperty(health, "FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastPtsCadenceMismatchUtcUnixMs round-trip");
        AssertEqual(16.67d, GetDoubleProperty(health, "FlashbackPlaybackLastPtsCadenceDeltaMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastPtsCadenceDeltaMs round-trip");
        AssertEqual(8.33d, GetDoubleProperty(health, "FlashbackPlaybackLastPtsCadenceExpectedMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastPtsCadenceExpectedMs round-trip");
        AssertEqual(3L, GetLongProperty(health, "FlashbackPlaybackSeekForwardDecodeCapHits"), "CaptureHealthSnapshot.FlashbackPlaybackSeekForwardDecodeCapHits round-trip");
        AssertEqual(true, GetBoolProperty(health, "FlashbackPlaybackLastSeekHitForwardDecodeCap"), "CaptureHealthSnapshot.FlashbackPlaybackLastSeekHitForwardDecodeCap round-trip");
        AssertEqual(120, GetIntProperty(health, "FlashbackPlaybackDecodeSampleCount"), "CaptureHealthSnapshot.FlashbackPlaybackDecodeSampleCount round-trip");
        AssertEqual(1.25d, GetDoubleProperty(health, "FlashbackPlaybackDecodeAvgMs"), "CaptureHealthSnapshot.FlashbackPlaybackDecodeAvgMs round-trip");
        AssertEqual(2.5d, GetDoubleProperty(health, "FlashbackPlaybackDecodeP95Ms"), "CaptureHealthSnapshot.FlashbackPlaybackDecodeP95Ms round-trip");
        AssertEqual(3.5d, GetDoubleProperty(health, "FlashbackPlaybackDecodeP99Ms"), "CaptureHealthSnapshot.FlashbackPlaybackDecodeP99Ms round-trip");
        AssertEqual(4.5d, GetDoubleProperty(health, "FlashbackPlaybackDecodeMaxMs"), "CaptureHealthSnapshot.FlashbackPlaybackDecodeMaxMs round-trip");
        AssertEqual("audio", GetStringProperty(health, "FlashbackPlaybackMaxDecodePhase"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodePhase round-trip");
        AssertEqual(0.5d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeReceiveMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodeReceiveMs round-trip");
        AssertEqual(4.0d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeFeedMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodeFeedMs round-trip");
        AssertEqual(0.75d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeReadMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodeReadMs round-trip");
        AssertEqual(3.5d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeSendMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodeSendMs round-trip");
        AssertEqual(3.25d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeAudioMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodeAudioMs round-trip");
        AssertEqual(0.25d, GetDoubleProperty(health, "FlashbackPlaybackMaxDecodeConvertMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodeConvertMs round-trip");
        AssertEqual(123456789L, GetLongProperty(health, "FlashbackPlaybackMaxDecodeUtcUnixMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodeUtcUnixMs round-trip");
        AssertEqual(2345L, GetLongProperty(health, "FlashbackPlaybackMaxDecodePositionMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxDecodePositionMs round-trip");
        AssertEqual(9L, GetLongProperty(health, "FlashbackPlaybackCommandsEnqueued"), "CaptureHealthSnapshot.FlashbackPlaybackCommandsEnqueued round-trip");
        AssertEqual(7L, GetLongProperty(health, "FlashbackPlaybackScrubUpdatesCoalesced"), "CaptureHealthSnapshot.FlashbackPlaybackScrubUpdatesCoalesced round-trip");
        AssertEqual(8L, GetLongProperty(health, "FlashbackPlaybackSeekCommandsCoalesced"), "CaptureHealthSnapshot.FlashbackPlaybackSeekCommandsCoalesced round-trip");
        AssertEqual(256, GetIntProperty(health, "FlashbackPlaybackCommandQueueCapacity"), "CaptureHealthSnapshot.FlashbackPlaybackCommandQueueCapacity round-trip");
        AssertEqual(2, GetIntProperty(health, "FlashbackPlaybackPendingCommands"), "CaptureHealthSnapshot.FlashbackPlaybackPendingCommands round-trip");
        AssertEqual(5, GetIntProperty(health, "FlashbackPlaybackMaxPendingCommands"), "CaptureHealthSnapshot.FlashbackPlaybackMaxPendingCommands round-trip");
        AssertEqual(14L, GetLongProperty(health, "FlashbackPlaybackLastCommandQueueLatencyMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastCommandQueueLatencyMs round-trip");
        AssertEqual(88L, GetLongProperty(health, "FlashbackPlaybackMaxCommandQueueLatencyMs"), "CaptureHealthSnapshot.FlashbackPlaybackMaxCommandQueueLatencyMs round-trip");
        AssertEqual("Play", GetStringProperty(health, "FlashbackPlaybackMaxCommandQueueLatencyCommand"), "CaptureHealthSnapshot.FlashbackPlaybackMaxCommandQueueLatencyCommand round-trip");
        AssertEqual("UpdateScrub", GetStringProperty(health, "FlashbackPlaybackLastCommandQueued"), "CaptureHealthSnapshot.FlashbackPlaybackLastCommandQueued round-trip");
        AssertEqual(999L, GetLongProperty(health, "FlashbackPlaybackLastCommandFailureUtcUnixMs"), "CaptureHealthSnapshot.FlashbackPlaybackLastCommandFailureUtcUnixMs round-trip");
        AssertEqual(11L, GetLongProperty(health, "FlashbackVideoQueueRejectedFrames"), "CaptureHealthSnapshot.FlashbackVideoQueueRejectedFrames round-trip");
        AssertEqual("force_rotate_draining", GetStringProperty(health, "FlashbackVideoQueueLastRejectReason"), "CaptureHealthSnapshot.FlashbackVideoQueueLastRejectReason round-trip");
        AssertEqual(13L, GetLongProperty(health, "FlashbackGpuQueueRejectedFrames"), "CaptureHealthSnapshot.FlashbackGpuQueueRejectedFrames round-trip");
        AssertEqual("encoding_failed:InvalidOperationException", GetStringProperty(health, "FlashbackGpuQueueLastRejectReason"), "CaptureHealthSnapshot.FlashbackGpuQueueLastRejectReason round-trip");
        AssertEqual(true, GetBoolProperty(health, "FlashbackBackendSettingsStale"), "CaptureHealthSnapshot.FlashbackBackendSettingsStale round-trip");
        AssertEqual("preset:P1->P2", GetStringProperty(health, "FlashbackBackendSettingsStaleReason"), "CaptureHealthSnapshot.FlashbackBackendSettingsStaleReason round-trip");
        AssertEqual("HevcMp4", GetStringProperty(health, "FlashbackBackendActiveFormat"), "CaptureHealthSnapshot.FlashbackBackendActiveFormat round-trip");
        AssertEqual("P2", GetStringProperty(health, "FlashbackBackendRequestedPreset"), "CaptureHealthSnapshot.FlashbackBackendRequestedPreset round-trip");
        AssertEqual(true, GetBoolProperty(health, "FlashbackExportActive"), "CaptureHealthSnapshot.FlashbackExportActive round-trip");
        AssertEqual("Running", GetStringProperty(health, "FlashbackExportStatus"), "CaptureHealthSnapshot.FlashbackExportStatus round-trip");
        AssertEqual("NoMediaWritten", GetStringProperty(health, "FlashbackExportFailureKind"), "CaptureHealthSnapshot.FlashbackExportFailureKind round-trip");
        AssertEqual(2L, GetLongProperty(health, "FlashbackExportForceRotateFallbacks"), "CaptureHealthSnapshot.FlashbackExportForceRotateFallbacks round-trip");
        AssertEqual(12345L, GetLongProperty(health, "FlashbackExportLastForceRotateFallbackUtcUnixMs"), "CaptureHealthSnapshot.FlashbackExportLastForceRotateFallbackUtcUnixMs round-trip");
        AssertEqual(3, GetIntProperty(health, "FlashbackExportLastForceRotateFallbackSegments"), "CaptureHealthSnapshot.FlashbackExportLastForceRotateFallbackSegments round-trip");
        AssertEqual(1000L, GetLongProperty(health, "FlashbackExportLastForceRotateFallbackInPointMs"), "CaptureHealthSnapshot.FlashbackExportLastForceRotateFallbackInPointMs round-trip");
        AssertEqual(9000L, GetLongProperty(health, "FlashbackExportLastForceRotateFallbackOutPointMs"), "CaptureHealthSnapshot.FlashbackExportLastForceRotateFallbackOutPointMs round-trip");
        AssertEqual("HevcMp4", GetStringProperty(health, "FlashbackExportVerificationFormat"), "CaptureHealthSnapshot.FlashbackExportVerificationFormat round-trip");
        AssertEqual("AV1->HEVC", GetStringProperty(health, "FlashbackCodecDowngradeReason"), "CaptureHealthSnapshot.FlashbackCodecDowngradeReason round-trip");
        AssertEqual(37.5d, GetDoubleProperty(health, "FlashbackExportPercent"), "CaptureHealthSnapshot.FlashbackExportPercent round-trip");
        AssertEqual(2000L, GetLongProperty(health, "FlashbackExportElapsedMs"), "CaptureHealthSnapshot.FlashbackExportElapsedMs round-trip");
        AssertEqual(100L, GetLongProperty(health, "FlashbackExportLastProgressAgeMs"), "CaptureHealthSnapshot.FlashbackExportLastProgressAgeMs round-trip");
        AssertEqual(1048576L, GetLongProperty(health, "FlashbackExportOutputBytes"), "CaptureHealthSnapshot.FlashbackExportOutputBytes round-trip");
        AssertEqual(524288d, GetDoubleProperty(health, "FlashbackExportThroughputBytesPerSec"), "CaptureHealthSnapshot.FlashbackExportThroughputBytesPerSec round-trip");
        AssertEqual(3, GetIntProperty(health, "FlashbackExportSegmentsProcessed"), "CaptureHealthSnapshot.FlashbackExportSegmentsProcessed round-trip");
        AssertEqual(42L, GetLongProperty(health, "LastExportId"), "CaptureHealthSnapshot.LastExportId round-trip");
        AssertEqual("YCbCr422", GetStringProperty(health, "SourceVideoFormat"), "CaptureHealthSnapshot.SourceVideoFormat round-trip");
        AssertEqual(2, Convert.ToInt32(GetPropertyValue(health, "SourceHdrTransferCode")), "CaptureHealthSnapshot.SourceHdrTransferCode round-trip");
        AssertEqual(1, GetCountProperty(GetPropertyValue(health, "SourceTelemetryDetails")!), "CaptureHealthSnapshot.SourceTelemetryDetails round-trip count");
        AssertEqual("Signal", GetStringProperty(roundTripDetail, "Group"), "SourceTelemetryDetailEntry.Group round-trip");
        AssertEqual("Colorimetry", GetStringProperty(roundTripDetail, "Label"), "SourceTelemetryDetailEntry.Label round-trip");
        AssertEqual("BT.2020", GetStringProperty(roundTripDetail, "DisplayValue"), "SourceTelemetryDetailEntry.DisplayValue round-trip");
        AssertEqual("bt2020", GetStringProperty(roundTripDetail, "RawValue"), "SourceTelemetryDetailEntry.RawValue round-trip");
        AssertEqual(17L, GetLongProperty(health, "LastVideoEnqueueAgeMs"), "CaptureHealthSnapshot.LastVideoEnqueueAgeMs round-trip");
        AssertEqual(-1.5d, (double)GetPropertyValue(health, "AvSyncCaptureDriftMs")!, "CaptureHealthSnapshot.AvSyncCaptureDriftMs round-trip");
        AssertEqual(48L, Convert.ToInt64(GetPropertyValue(health, "AvSyncEncoderCorrectionSamples")), "CaptureHealthSnapshot.AvSyncEncoderCorrectionSamples round-trip");
        var detailJsonRoundTrip = ReflectionJsonRoundTrip(detailType, detailEntry);
        AssertEqual("BT.2020", GetStringProperty(detailJsonRoundTrip, "DisplayValue"), "SourceTelemetryDetailEntry JSON DisplayValue");
        var jsonRoundTrip = ReflectionJsonRoundTrip(healthType, health);
        AssertEqual("Paused", GetStringProperty(jsonRoundTrip, "FlashbackPlaybackState"), "CaptureHealthSnapshot JSON FlashbackPlaybackState");
        AssertEqual(6L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackSubmitFailures"), "CaptureHealthSnapshot JSON FlashbackPlaybackSubmitFailures");
        AssertEqual(666L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackLastDropUtcUnixMs"), "CaptureHealthSnapshot JSON FlashbackPlaybackLastDropUtcUnixMs");
        AssertEqual("av_sync_skip", GetStringProperty(jsonRoundTrip, "FlashbackPlaybackLastDropReason"), "CaptureHealthSnapshot JSON FlashbackPlaybackLastDropReason");
        AssertEqual(777L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackLastSubmitFailureUtcUnixMs"), "CaptureHealthSnapshot JSON FlashbackPlaybackLastSubmitFailureUtcUnixMs");
        AssertEqual("seek:null_texture", GetStringProperty(jsonRoundTrip, "FlashbackPlaybackLastSubmitFailure"), "CaptureHealthSnapshot JSON FlashbackPlaybackLastSubmitFailure");
        AssertEqual(9L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackCommandsEnqueued"), "CaptureHealthSnapshot JSON FlashbackPlaybackCommandsEnqueued");
        AssertEqual(256, GetIntProperty(jsonRoundTrip, "FlashbackPlaybackCommandQueueCapacity"), "CaptureHealthSnapshot JSON FlashbackPlaybackCommandQueueCapacity");
        AssertEqual(999L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackLastCommandFailureUtcUnixMs"), "CaptureHealthSnapshot JSON FlashbackPlaybackLastCommandFailureUtcUnixMs");
        AssertEqual(11L, GetLongProperty(jsonRoundTrip, "FlashbackVideoQueueRejectedFrames"), "CaptureHealthSnapshot JSON FlashbackVideoQueueRejectedFrames");
        AssertEqual("force_rotate_draining", GetStringProperty(jsonRoundTrip, "FlashbackVideoQueueLastRejectReason"), "CaptureHealthSnapshot JSON FlashbackVideoQueueLastRejectReason");
        AssertEqual(13L, GetLongProperty(jsonRoundTrip, "FlashbackGpuQueueRejectedFrames"), "CaptureHealthSnapshot JSON FlashbackGpuQueueRejectedFrames");
        AssertEqual("encoding_failed:InvalidOperationException", GetStringProperty(jsonRoundTrip, "FlashbackGpuQueueLastRejectReason"), "CaptureHealthSnapshot JSON FlashbackGpuQueueLastRejectReason");
        AssertEqual(2L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackPtsCadenceMismatchCount"), "CaptureHealthSnapshot JSON FlashbackPlaybackPtsCadenceMismatchCount");
        AssertEqual(16.67d, GetDoubleProperty(jsonRoundTrip, "FlashbackPlaybackLastPtsCadenceDeltaMs"), "CaptureHealthSnapshot JSON FlashbackPlaybackLastPtsCadenceDeltaMs");
        AssertEqual(3L, GetLongProperty(jsonRoundTrip, "FlashbackPlaybackSeekForwardDecodeCapHits"), "CaptureHealthSnapshot JSON FlashbackPlaybackSeekForwardDecodeCapHits");
        AssertEqual(true, GetBoolProperty(jsonRoundTrip, "FlashbackPlaybackLastSeekHitForwardDecodeCap"), "CaptureHealthSnapshot JSON FlashbackPlaybackLastSeekHitForwardDecodeCap");
        AssertEqual(true, GetBoolProperty(jsonRoundTrip, "FlashbackBackendSettingsStale"), "CaptureHealthSnapshot JSON FlashbackBackendSettingsStale");
        AssertEqual("preset:P1->P2", GetStringProperty(jsonRoundTrip, "FlashbackBackendSettingsStaleReason"), "CaptureHealthSnapshot JSON FlashbackBackendSettingsStaleReason");
        AssertEqual("Running", GetStringProperty(jsonRoundTrip, "FlashbackExportStatus"), "CaptureHealthSnapshot JSON FlashbackExportStatus");
        AssertEqual("NoMediaWritten", GetStringProperty(jsonRoundTrip, "FlashbackExportFailureKind"), "CaptureHealthSnapshot JSON FlashbackExportFailureKind");
        AssertEqual(2L, GetLongProperty(jsonRoundTrip, "FlashbackExportForceRotateFallbacks"), "CaptureHealthSnapshot JSON FlashbackExportForceRotateFallbacks");
        AssertEqual(3, GetIntProperty(jsonRoundTrip, "FlashbackExportLastForceRotateFallbackSegments"), "CaptureHealthSnapshot JSON FlashbackExportLastForceRotateFallbackSegments");
        AssertEqual("HevcMp4", GetStringProperty(jsonRoundTrip, "FlashbackExportVerificationFormat"), "CaptureHealthSnapshot JSON FlashbackExportVerificationFormat");
        AssertEqual("AV1->HEVC", GetStringProperty(jsonRoundTrip, "FlashbackCodecDowngradeReason"), "CaptureHealthSnapshot JSON FlashbackCodecDowngradeReason");
        AssertEqual(1048576L, GetLongProperty(jsonRoundTrip, "FlashbackExportOutputBytes"), "CaptureHealthSnapshot JSON FlashbackExportOutputBytes");
        AssertEqual(42L, GetLongProperty(jsonRoundTrip, "LastExportId"), "CaptureHealthSnapshot JSON LastExportId");
        AssertEqual("YCbCr422", GetStringProperty(jsonRoundTrip, "SourceVideoFormat"), "CaptureHealthSnapshot JSON SourceVideoFormat");
        AssertEqual(1, GetCountProperty(GetPropertyValue(jsonRoundTrip, "SourceTelemetryDetails")!), "CaptureHealthSnapshot JSON SourceTelemetryDetails count");
        AssertEqual("BT.2020", GetStringProperty(GetSingleEnumerableItem(GetPropertyValue(jsonRoundTrip, "SourceTelemetryDetails")!), "DisplayValue"), "CaptureHealthSnapshot JSON SourceTelemetryDetails DisplayValue");

        return Task.CompletedTask;
    }

    private static Task SourceSignalTelemetrySnapshot_PreservesFullTelemetryContract()
    {
        var snapshotType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var availabilityType = RequireType("Sussudio.Models.SourceTelemetryAvailability");
        var originType = RequireType("Sussudio.Models.SourceTelemetryOrigin");
        var confidenceType = RequireType("Sussudio.Models.SourceTelemetryConfidence");
        var audioAvailabilityType = RequireType("Sussudio.Models.SourceAudioInputAvailability");
        var audioModeType = RequireType("Sussudio.Models.SourceAudioInputMode");
        var detailType = RequireType("Sussudio.Models.SourceTelemetryDetailEntry");

        AssertDeclaredProperties(
            snapshotType,
            new SnapshotPropertySpec[]
            {
                new("TimestampUtc", typeof(DateTimeOffset)),
                new("Availability", availabilityType),
                new("Origin", originType),
                NonNullString("OriginDetail"),
                new("Confidence", confidenceType),
                new("Width", typeof(int?)),
                new("Height", typeof(int?)),
                new("FrameRateExact", typeof(double?)),
                NullableString("FrameRateArg"),
                new("IsHdr", typeof(bool?)),
                NullableString("VideoFormat"),
                NullableString("Colorimetry"),
                NullableString("Quantization"),
                NullableString("HdrTransferFunction"),
                new("HdrTransferCode", typeof(int?)),
                NullableString("Firmware"),
                NullableString("AudioFormat"),
                NullableString("AudioSampleRate"),
                NullableString("InputSource"),
                new("AdcOnOff", typeof(bool?)),
                new("AdcVolumeGain", typeof(int?)),
                new("AnalogGainByte", typeof(int?)),
                new("UacVolumeGain", typeof(int?)),
                new("UacOut1Mute", typeof(bool?)),
                new("UacOut2Mute", typeof(bool?)),
                new("UacOut2MixerSource", typeof(int?)),
                NullableString("UsbHostProtocol"),
                new("TxEdidValid", typeof(bool?)),
                NullableString("HdcpMode"),
                NullableString("HdcpVersion"),
                NullableString("RxTxHdcpVersion"),
                NullableString("CustomerVersion"),
                new("RescueVersion", typeof(int?)),
                NullableString("RawTimingHex"),
                NonNullRef("DetailEntries", typeof(IReadOnlyList<>).MakeGenericType(detailType), SnapshotNullability.NotNull),
                NullableString("DiagnosticSummary"),
                NullableString("EgavInitializeResultName"),
                NullableString("EgavOpenResultName"),
                NullableString("EgavSignalStatusResultName"),
                NullableString("EgavIsVideoHdrResultName"),
                new("AudioInputAvailability", audioAvailabilityType),
                new("AudioInputMode", typeof(Nullable<>).MakeGenericType(audioModeType)),
                NullableString("AudioInputOrigin"),
                GetterOnly("HasDimensions", typeof(bool)),
                GetterOnly("HasFrameRate", typeof(bool)),
                GetterOnly("HasSignalData", typeof(bool))
            });
        AssertDeclaredProperties(
            detailType,
            new SnapshotPropertySpec[]
            {
                NonNullString("Group"),
                NonNullString("Label"),
                NonNullString("DisplayValue"),
                NullableString("RawValue")
            });

        var before = DateTimeOffset.UtcNow.AddSeconds(-1);
        var snapshot = CreateInstance("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var after = DateTimeOffset.UtcNow.AddSeconds(1);
        var timestamp = (DateTimeOffset)GetPropertyValue(snapshot, "TimestampUtc")!;
        if (timestamp < before || timestamp > after)
        {
            throw new InvalidOperationException("SourceSignalTelemetrySnapshot.TimestampUtc should default to current UTC time.");
        }

        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Unknown"), GetPropertyValue(snapshot, "Availability"), "SourceSignalTelemetrySnapshot.Availability default");
        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "Unknown"), GetPropertyValue(snapshot, "Origin"), "SourceSignalTelemetrySnapshot.Origin default");
        AssertNonNullStringValue(snapshot, "OriginDetail", "Unknown", "SourceSignalTelemetrySnapshot.OriginDetail default");
        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "Unknown"), GetPropertyValue(snapshot, "Confidence"), "SourceSignalTelemetrySnapshot.Confidence default");
        AssertEqual(ParseEnum("Sussudio.Models.SourceAudioInputAvailability", "Unavailable"), GetPropertyValue(snapshot, "AudioInputAvailability"), "SourceSignalTelemetrySnapshot.AudioInputAvailability default");
        AssertEqual("not-implemented", GetStringProperty(snapshot, "AudioInputOrigin"), "SourceSignalTelemetrySnapshot.AudioInputOrigin default");
        AssertEqual(0, GetCountProperty(GetPropertyValue(snapshot, "DetailEntries")!), "SourceSignalTelemetrySnapshot.DetailEntries default count");
        AssertEqual(false, GetBoolProperty(snapshot, "HasDimensions"), "SourceSignalTelemetrySnapshot.HasDimensions default");
        AssertEqual(false, GetBoolProperty(snapshot, "HasFrameRate"), "SourceSignalTelemetrySnapshot.HasFrameRate default");
        AssertEqual(false, GetBoolProperty(snapshot, "HasSignalData"), "SourceSignalTelemetrySnapshot.HasSignalData default");
        AssertEqual(string.Empty, InvokeInstanceMethod(snapshot, "GetModeKey") as string, "SourceSignalTelemetrySnapshot.GetModeKey default");

        var detailEntry = Activator.CreateInstance(detailType, "Audio / Input", "Analog Gain", "12 dB", "0C")
            ?? throw new InvalidOperationException("Failed to create SourceTelemetryDetailEntry.");
        var details = CreateGenericList(detailType, detailEntry);
        SetPropertyOrBackingField(snapshot, "Availability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Available"));
        SetPropertyOrBackingField(snapshot, "Origin", ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"));
        SetPropertyOrBackingField(snapshot, "OriginDetail", "NativeXuAtCommandProvider");
        SetPropertyOrBackingField(snapshot, "Confidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "High"));
        SetPropertyOrBackingField(snapshot, "Width", 3840);
        SetPropertyOrBackingField(snapshot, "Height", 2160);
        SetPropertyOrBackingField(snapshot, "FrameRateExact", 120000d / 1001d);
        SetPropertyOrBackingField(snapshot, "FrameRateArg", "120000/1001");
        SetPropertyOrBackingField(snapshot, "IsHdr", true);
        SetPropertyOrBackingField(snapshot, "VideoFormat", "YCbCr422");
        SetPropertyOrBackingField(snapshot, "Colorimetry", "BT.2020");
        SetPropertyOrBackingField(snapshot, "Quantization", "Limited");
        SetPropertyOrBackingField(snapshot, "HdrTransferFunction", "HDR10 / PQ");
        SetPropertyOrBackingField(snapshot, "HdrTransferCode", 2);
        SetPropertyOrBackingField(snapshot, "Firmware", "1.2.3");
        SetPropertyOrBackingField(snapshot, "AudioFormat", "PCM");
        SetPropertyOrBackingField(snapshot, "AudioSampleRate", "48 kHz");
        SetPropertyOrBackingField(snapshot, "InputSource", "HDMI");
        SetPropertyOrBackingField(snapshot, "AdcOnOff", true);
        SetPropertyOrBackingField(snapshot, "AdcVolumeGain", 12);
        SetPropertyOrBackingField(snapshot, "AnalogGainByte", 0x0C);
        SetPropertyOrBackingField(snapshot, "UacVolumeGain", 24);
        SetPropertyOrBackingField(snapshot, "UacOut1Mute", false);
        SetPropertyOrBackingField(snapshot, "UacOut2Mute", true);
        SetPropertyOrBackingField(snapshot, "UacOut2MixerSource", 1);
        SetPropertyOrBackingField(snapshot, "UsbHostProtocol", "Isochronous");
        SetPropertyOrBackingField(snapshot, "TxEdidValid", true);
        SetPropertyOrBackingField(snapshot, "HdcpMode", "Off");
        SetPropertyOrBackingField(snapshot, "HdcpVersion", "0200");
        SetPropertyOrBackingField(snapshot, "RxTxHdcpVersion", "0200/0200");
        SetPropertyOrBackingField(snapshot, "CustomerVersion", "custom-a");
        SetPropertyOrBackingField(snapshot, "RescueVersion", 7);
        SetPropertyOrBackingField(snapshot, "RawTimingHex", "3000CA0830117008");
        SetPropertyOrBackingField(snapshot, "DetailEntries", details);
        SetPropertyOrBackingField(snapshot, "DiagnosticSummary", "ok");
        SetPropertyOrBackingField(snapshot, "EgavInitializeResultName", "Ok");
        SetPropertyOrBackingField(snapshot, "EgavOpenResultName", "Ok");
        SetPropertyOrBackingField(snapshot, "EgavSignalStatusResultName", "Ok");
        SetPropertyOrBackingField(snapshot, "EgavIsVideoHdrResultName", "Ok");
        SetPropertyOrBackingField(snapshot, "AudioInputAvailability", ParseEnum("Sussudio.Models.SourceAudioInputAvailability", "Available"));
        SetPropertyOrBackingField(snapshot, "AudioInputMode", ParseEnum("Sussudio.Models.SourceAudioInputMode", "Analog"));
        SetPropertyOrBackingField(snapshot, "AudioInputOrigin", "native-xu");

        var roundTripDetail = GetSingleEnumerableItem(GetPropertyValue(snapshot, "DetailEntries")!);
        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Available"), GetPropertyValue(snapshot, "Availability"), "SourceSignalTelemetrySnapshot.Availability round-trip");
        AssertEqual(ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"), GetPropertyValue(snapshot, "Origin"), "SourceSignalTelemetrySnapshot.Origin round-trip");
        AssertEqual("NativeXuAtCommandProvider", GetStringProperty(snapshot, "OriginDetail"), "SourceSignalTelemetrySnapshot.OriginDetail round-trip");
        AssertEqual(3840, Convert.ToInt32(GetPropertyValue(snapshot, "Width")), "SourceSignalTelemetrySnapshot.Width round-trip");
        AssertEqual("YCbCr422", GetStringProperty(snapshot, "VideoFormat"), "SourceSignalTelemetrySnapshot.VideoFormat round-trip");
        AssertEqual("HDR10 / PQ", GetStringProperty(snapshot, "HdrTransferFunction"), "SourceSignalTelemetrySnapshot.HdrTransferFunction round-trip");
        AssertEqual("PCM", GetStringProperty(snapshot, "AudioFormat"), "SourceSignalTelemetrySnapshot.AudioFormat round-trip");
        AssertEqual(true, (bool)GetPropertyValue(snapshot, "AdcOnOff")!, "SourceSignalTelemetrySnapshot.AdcOnOff round-trip");
        AssertEqual("0200/0200", GetStringProperty(snapshot, "RxTxHdcpVersion"), "SourceSignalTelemetrySnapshot.RxTxHdcpVersion round-trip");
        AssertEqual(1, GetCountProperty(GetPropertyValue(snapshot, "DetailEntries")!), "SourceSignalTelemetrySnapshot.DetailEntries round-trip count");
        AssertEqual("Audio / Input", GetStringProperty(roundTripDetail, "Group"), "SourceSignalTelemetryDetailEntry.Group round-trip");
        AssertEqual("Analog Gain", GetStringProperty(roundTripDetail, "Label"), "SourceSignalTelemetryDetailEntry.Label round-trip");
        AssertEqual("12 dB", GetStringProperty(roundTripDetail, "DisplayValue"), "SourceSignalTelemetryDetailEntry.DisplayValue round-trip");
        AssertEqual("0C", GetStringProperty(roundTripDetail, "RawValue"), "SourceSignalTelemetryDetailEntry.RawValue round-trip");
        AssertEqual(ParseEnum("Sussudio.Models.SourceAudioInputAvailability", "Available"), GetPropertyValue(snapshot, "AudioInputAvailability"), "SourceSignalTelemetrySnapshot.AudioInputAvailability round-trip");
        AssertEqual(ParseEnum("Sussudio.Models.SourceAudioInputMode", "Analog"), GetPropertyValue(snapshot, "AudioInputMode"), "SourceSignalTelemetrySnapshot.AudioInputMode round-trip");
        AssertEqual("native-xu", GetStringProperty(snapshot, "AudioInputOrigin"), "SourceSignalTelemetrySnapshot.AudioInputOrigin round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "HasDimensions"), "SourceSignalTelemetrySnapshot.HasDimensions round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "HasFrameRate"), "SourceSignalTelemetrySnapshot.HasFrameRate round-trip");
        AssertEqual(true, GetBoolProperty(snapshot, "HasSignalData"), "SourceSignalTelemetrySnapshot.HasSignalData round-trip");
        AssertEqual("3840x2160@120000/1001:hdr", InvokeInstanceMethod(snapshot, "GetModeKey") as string, "SourceSignalTelemetrySnapshot.GetModeKey round-trip");
        var detailJsonRoundTrip = ReflectionJsonRoundTrip(detailType, detailEntry);
        AssertEqual("Analog Gain", GetStringProperty(detailJsonRoundTrip, "Label"), "SourceTelemetryDetailEntry JSON Label");
        var jsonRoundTrip = ReflectionJsonRoundTrip(snapshotType, snapshot);
        AssertEqual("NativeXuAtCommandProvider", GetStringProperty(jsonRoundTrip, "OriginDetail"), "SourceSignalTelemetrySnapshot JSON OriginDetail");
        AssertEqual("YCbCr422", GetStringProperty(jsonRoundTrip, "VideoFormat"), "SourceSignalTelemetrySnapshot JSON VideoFormat");
        AssertEqual("PCM", GetStringProperty(jsonRoundTrip, "AudioFormat"), "SourceSignalTelemetrySnapshot JSON AudioFormat");
        AssertEqual(1, GetCountProperty(GetPropertyValue(jsonRoundTrip, "DetailEntries")!), "SourceSignalTelemetrySnapshot JSON DetailEntries count");
        AssertEqual("Analog Gain", GetStringProperty(GetSingleEnumerableItem(GetPropertyValue(jsonRoundTrip, "DetailEntries")!), "Label"), "SourceSignalTelemetrySnapshot JSON DetailEntries Label");

        return Task.CompletedTask;
    }

    private enum SnapshotSetterExpectation
    {
        InitOnly,
        None
    }

    private enum SnapshotNullability
    {
        NotApplicable,
        NotNull,
        Nullable
    }

    private sealed record SnapshotPropertySpec(
        string Name,
        Type Type,
        SnapshotSetterExpectation Setter = SnapshotSetterExpectation.InitOnly,
        SnapshotNullability Nullability = SnapshotNullability.NotApplicable,
        SnapshotNullability ElementNullability = SnapshotNullability.NotApplicable);

    private static readonly Dictionary<Type, SnapshotPropertySpec[]> SnapshotPropertySpecsByType = new();

    private static void RegisterCaptureDiagnosticsSnapshotProperties(Type snapshotType)
    {
        var decoderType = RequireType("Sussudio.Models.MjpegDecoderHealthSnapshot");
        var sessionStateType = RequireType("Sussudio.Models.CaptureSessionState");
        var availabilityType = RequireType("Sussudio.Models.SourceTelemetryAvailability");
        var originType = RequireType("Sussudio.Models.SourceTelemetryOrigin");
        var confidenceType = RequireType("Sussudio.Models.SourceTelemetryConfidence");

        AssertDeclaredProperties(
            snapshotType,
            new SnapshotPropertySpec[]
            {
                new("TimestampUtc", typeof(DateTimeOffset)),
                new("SessionState", sessionStateType),
                new("IsRecording", typeof(bool)),
                NonNullString("RecordingBackend"),
                NonNullString("AudioPathMode"),
                NonNullString("MuxResult"),
                new("FlashbackActive", typeof(bool)),
                new("FlashbackBufferedDurationMs", typeof(long)),
                new("FlashbackSegmentCount", typeof(int)),
                new("FlashbackDiskBytes", typeof(long)),
                new("FlashbackTotalBytesWritten", typeof(long)),
                new("FlashbackTempDriveFreeBytes", typeof(long)),
                new("FlashbackStartupCacheBudgetBytes", typeof(long)),
                new("FlashbackStartupCacheBytes", typeof(long)),
                new("FlashbackStartupCacheSessionCount", typeof(int)),
                new("FlashbackStartupCacheDeletedSessionCount", typeof(int)),
                new("FlashbackStartupCacheFreedBytes", typeof(long)),
                new("FlashbackStartupCacheOverBudget", typeof(bool)),
                new("RecordingElapsedMs", typeof(long)),
                new("LastFrameArrivalMs", typeof(long)),
                new("EstimatedPipelineLatencyMs", typeof(long)),
                new("ExpectedFrameRate", typeof(double)),
                new("NegotiatedWidth", typeof(uint?)),
                new("NegotiatedHeight", typeof(uint?)),
                new("NegotiatedFrameRate", typeof(double?)),
                NullableString("NegotiatedFrameRateArg"),
                new("NegotiatedFrameRateNumerator", typeof(uint?)),
                new("NegotiatedFrameRateDenominator", typeof(uint?)),
                NullableString("NegotiatedPixelFormat"),
                NullableString("RequestedReaderSubtype"),
                NullableString("ReaderSourceStreamType"),
                NullableString("ReaderSourceSubtype"),
                NullableString("FirstObservedFramePixelFormat"),
                NullableString("LatestObservedFramePixelFormat"),
                new("ObservedP010FrameCount", typeof(long)),
                new("ObservedNv12FrameCount", typeof(long)),
                new("ObservedOtherFrameCount", typeof(long)),
                new("SourceTelemetryAvailability", availabilityType),
                new("SourceTelemetryOrigin", originType),
                new("SourceTelemetryConfidence", confidenceType),
                NonNullString("SourceTelemetryOriginDetail"),
                NullableString("SourceTelemetryDiagnosticSummary"),
                new("SourceTelemetryTimestampUtc", typeof(DateTimeOffset?)),
                NonNullString("SourceTelemetryBackend"),
                new("SourceTelemetrySuppressed", typeof(bool)),
                NullableString("SourceTelemetrySuppressedReason"),
                NonNullString("SourceTelemetryCircuitState"),
                new("SourceWidth", typeof(int?)),
                new("SourceHeight", typeof(int?)),
                new("SourceFrameRateExact", typeof(double?)),
                NullableString("SourceFrameRateArg"),
                new("SourceIsHdr", typeof(bool?)),
                new("HdrAutoDowngraded", typeof(bool)),
                NonNullString("HdrAutoDowngradeReason"),
                new("CaptureCadenceSampleCount", typeof(int)),
                new("CaptureCadenceObservedFps", typeof(double)),
                new("CaptureCadenceExpectedIntervalMs", typeof(double)),
                new("CaptureCadenceAverageIntervalMs", typeof(double)),
                new("CaptureCadenceP95IntervalMs", typeof(double)),
                new("CaptureCadenceP99IntervalMs", typeof(double)),
                new("CaptureCadenceMaxIntervalMs", typeof(double)),
                new("CaptureCadenceOnePercentLowFps", typeof(double)),
                new("CaptureCadenceFivePercentLowFps", typeof(double)),
                new("CaptureCadenceSampleDurationMs", typeof(double)),
                new("CaptureCadenceRecentIntervalsMs", typeof(double[])),
                new("CaptureCadenceJitterStdDevMs", typeof(double)),
                new("CaptureCadenceSevereGapCount", typeof(long)),
                new("CaptureCadenceEstimatedDroppedFrames", typeof(long)),
                new("CaptureCadenceEstimatedDropPercent", typeof(double)),
                new("MjpegDecodeSampleCount", typeof(int)),
                new("MjpegDecodeAvgMs", typeof(double)),
                new("MjpegDecodeP95Ms", typeof(double)),
                new("MjpegDecodeMaxMs", typeof(double)),
                new("MjpegInteropCopySampleCount", typeof(int)),
                new("MjpegInteropCopyAvgMs", typeof(double)),
                new("MjpegInteropCopyP95Ms", typeof(double)),
                new("MjpegInteropCopyMaxMs", typeof(double)),
                new("MjpegCallbackSampleCount", typeof(int)),
                new("MjpegCallbackAvgMs", typeof(double)),
                new("MjpegCallbackP95Ms", typeof(double)),
                new("MjpegCallbackMaxMs", typeof(double)),
                new("MjpegDecoderCount", typeof(int)),
                new("MjpegReorderSampleCount", typeof(int)),
                new("MjpegReorderAvgMs", typeof(double)),
                new("MjpegReorderP95Ms", typeof(double)),
                new("MjpegReorderMaxMs", typeof(double)),
                new("MjpegPipelineSampleCount", typeof(int)),
                new("MjpegPipelineAvgMs", typeof(double)),
                new("MjpegPipelineP95Ms", typeof(double)),
                new("MjpegPipelineMaxMs", typeof(double)),
                new("MjpegTotalDecoded", typeof(long)),
                new("MjpegTotalEmitted", typeof(long)),
                new("MjpegTotalDropped", typeof(long)),
                new("MjpegCompressedFramesQueued", typeof(long)),
                new("MjpegCompressedFramesDequeued", typeof(long)),
                new("MjpegCompressedDropsQueueFull", typeof(long)),
                new("MjpegCompressedDropsByteBudget", typeof(long)),
                new("MjpegCompressedDropsDisposed", typeof(long)),
                new("MjpegDecodeFailures", typeof(long)),
                new("MjpegReorderCollisions", typeof(long)),
                new("MjpegEmitFailures", typeof(long)),
                new("MjpegCompressedQueueDepth", typeof(int)),
                new("MjpegCompressedQueueBytes", typeof(long)),
                new("MjpegCompressedQueueByteBudget", typeof(long)),
                new("MjpegReorderSkips", typeof(long)),
                new("MjpegReorderBufferDepth", typeof(int)),
                new("MjpegPreviewJitterEnabled", typeof(bool)),
                new("MjpegPreviewJitterTargetDepth", typeof(int)),
                new("MjpegPreviewJitterMaxDepth", typeof(int)),
                new("MjpegPreviewJitterQueueDepth", typeof(int)),
                new("MjpegPreviewJitterTotalQueued", typeof(long)),
                new("MjpegPreviewJitterTotalSubmitted", typeof(long)),
                new("MjpegPreviewJitterTotalDropped", typeof(long)),
                new("MjpegPreviewJitterUnderflowCount", typeof(long)),
                new("MjpegPreviewJitterResumeReprimeCount", typeof(long)),
                new("MjpegPreviewJitterInputSampleCount", typeof(int)),
                new("MjpegPreviewJitterInputAvgMs", typeof(double)),
                new("MjpegPreviewJitterInputP95Ms", typeof(double)),
                new("MjpegPreviewJitterInputMaxMs", typeof(double)),
                new("MjpegPreviewJitterOutputSampleCount", typeof(int)),
                new("MjpegPreviewJitterOutputAvgMs", typeof(double)),
                new("MjpegPreviewJitterOutputP95Ms", typeof(double)),
                new("MjpegPreviewJitterOutputMaxMs", typeof(double)),
                new("MjpegPreviewJitterLatencySampleCount", typeof(int)),
                new("MjpegPreviewJitterLatencyAvgMs", typeof(double)),
                new("MjpegPreviewJitterLatencyP95Ms", typeof(double)),
                new("MjpegPreviewJitterLatencyMaxMs", typeof(double)),
                new("MjpegPreviewJitterDeadlineDropCount", typeof(long)),
                new("MjpegPreviewJitterClearedDropCount", typeof(long)),
                new("MjpegPreviewJitterTargetIncreaseCount", typeof(long)),
                new("MjpegPreviewJitterTargetDecreaseCount", typeof(long)),
                new("MjpegPreviewJitterLastSelectedPreviewPresentId", typeof(long)),
                new("MjpegPreviewJitterLastSelectedSourceSequenceNumber", typeof(long)),
                new("MjpegPreviewJitterLastSelectedQpc", typeof(long)),
                new("MjpegPreviewJitterLastSelectedSourceLatencyMs", typeof(double)),
                new("MjpegPreviewJitterLastDroppedSourceSequenceNumber", typeof(long)),
                new("MjpegPreviewJitterLastDropQpc", typeof(long)),
                NonNullString("MjpegPreviewJitterLastDropReason"),
                new("MjpegPreviewJitterLastUnderflowQpc", typeof(long)),
                NonNullString("MjpegPreviewJitterLastUnderflowReason"),
                new("MjpegPreviewJitterLastUnderflowQueueDepth", typeof(int)),
                new("MjpegPreviewJitterLastUnderflowInputAgeMs", typeof(double)),
                new("MjpegPreviewJitterLastUnderflowOutputAgeMs", typeof(double)),
                new("MjpegPreviewJitterLastScheduleLateMs", typeof(double)),
                new("MjpegPreviewJitterMaxScheduleLateMs", typeof(double)),
                new("MjpegPreviewJitterScheduleLateCount", typeof(long)),
                new("MjpegPacketHashSampleCount", typeof(int)),
                new("MjpegPacketHashUniqueFrameCount", typeof(long)),
                new("MjpegPacketHashDuplicateFrameCount", typeof(long)),
                new("MjpegPacketHashLongestDuplicateRun", typeof(long)),
                new("MjpegPacketHashInputObservedFps", typeof(double)),
                new("MjpegPacketHashUniqueObservedFps", typeof(double)),
                new("MjpegPacketHashDuplicateFramePercent", typeof(double)),
                NonNullString("MjpegPacketHashLastHash"),
                new("MjpegPacketHashLastFrameDuplicate", typeof(bool)),
                NonNullString("MjpegPacketHashPattern"),
                NonNullRef("MjpegPacketHashRecentInputIntervalsMs", typeof(double[]), SnapshotNullability.NotNull),
                NonNullRef("MjpegPacketHashRecentUniqueIntervalsMs", typeof(double[]), SnapshotNullability.NotNull),
                NonNullRef("MjpegPacketHashRecentDuplicateFlags", typeof(int[]), SnapshotNullability.NotNull),
                new("VisualCadenceSampleCount", typeof(int)),
                new("VisualCadenceChangedFrameCount", typeof(long)),
                new("VisualCadenceRepeatFrameCount", typeof(long)),
                new("VisualCadenceLongestRepeatRun", typeof(long)),
                new("VisualCadenceOutputObservedFps", typeof(double)),
                new("VisualCadenceChangeObservedFps", typeof(double)),
                new("VisualCadenceRepeatFramePercent", typeof(double)),
                new("VisualCadenceLastDelta", typeof(double)),
                new("VisualCadenceAverageDelta", typeof(double)),
                new("VisualCadenceP95Delta", typeof(double)),
                new("VisualCadenceMotionScore", typeof(double)),
                NonNullString("VisualCadenceMotionConfidence"),
                NonNullRef("VisualCadenceRecentOutputIntervalsMs", typeof(double[]), SnapshotNullability.NotNull),
                NonNullRef("VisualCadenceRecentChangeIntervalsMs", typeof(double[]), SnapshotNullability.NotNull),
                new("VisualCenterCadenceSampleCount", typeof(int)),
                new("VisualCenterCadenceChangedFrameCount", typeof(long)),
                new("VisualCenterCadenceRepeatFrameCount", typeof(long)),
                new("VisualCenterCadenceLongestRepeatRun", typeof(long)),
                new("VisualCenterCadenceOutputObservedFps", typeof(double)),
                new("VisualCenterCadenceChangeObservedFps", typeof(double)),
                new("VisualCenterCadenceRepeatFramePercent", typeof(double)),
                new("VisualCenterCadenceLastDelta", typeof(double)),
                new("VisualCenterCadenceAverageDelta", typeof(double)),
                new("VisualCenterCadenceP95Delta", typeof(double)),
                new("VisualCenterCadenceMotionScore", typeof(double)),
                NonNullString("VisualCenterCadenceMotionConfidence"),
                NonNullRef("VisualCenterCadenceRecentOutputIntervalsMs", typeof(double[]), SnapshotNullability.NotNull),
                NonNullRef("VisualCenterCadenceRecentChangeIntervalsMs", typeof(double[]), SnapshotNullability.NotNull),
                NonNullRef("MjpegPerDecoder", decoderType.MakeArrayType(), SnapshotNullability.NotNull),
                new("ConversionQueueDepth", typeof(int)),
                new("FfmpegVideoQueueDepth", typeof(int)),
                new("FfmpegAudioQueueDepth", typeof(int)),
                new("VideoFramesArrived", typeof(long)),
                new("VideoFramesQueued", typeof(long)),
                new("VideoFramesDropped", typeof(long)),
                new("VideoFramesDroppedBacklog", typeof(long)),
                new("VideoFramesConverted", typeof(long)),
                new("VideoFramesEnqueued", typeof(long)),
                new("VideoDropsQueueSaturated", typeof(long)),
                new("VideoDropsBacklogEviction", typeof(long)),
                new("RecordingEncodingFailed", typeof(bool)),
                NullableString("RecordingEncodingFailureType"),
                NullableString("RecordingEncodingFailureMessage"),
                new("RecordingVideoQueueCapacity", typeof(int)),
                new("RecordingVideoQueueMaxDepth", typeof(int)),
                new("RecordingVideoFramesSubmittedToEncoder", typeof(long)),
                new("RecordingVideoEncoderPts", typeof(long)),
                new("RecordingVideoEncoderPacketsWritten", typeof(long)),
                new("RecordingVideoEncoderDroppedFrames", typeof(long)),
                new("RecordingVideoSequenceGaps", typeof(long)),
                new("RecordingVideoQueueOldestFrameAgeMs", typeof(long)),
                new("RecordingVideoQueueLastLatencyMs", typeof(long)),
                new("RecordingVideoQueueLatencySampleCount", typeof(int)),
                new("RecordingVideoQueueLatencyAvgMs", typeof(double)),
                new("RecordingVideoQueueLatencyP95Ms", typeof(double)),
                new("RecordingVideoQueueLatencyP99Ms", typeof(double)),
                new("RecordingVideoQueueLatencyMaxMs", typeof(double)),
                new("RecordingVideoBackpressureWaitMs", typeof(long)),
                new("RecordingVideoBackpressureEvents", typeof(long)),
                new("RecordingVideoBackpressureLastWaitMs", typeof(long)),
                new("RecordingVideoBackpressureMaxWaitMs", typeof(long)),
                new("RecordingGpuQueueDepth", typeof(int)),
                new("RecordingGpuQueueCapacity", typeof(int)),
                new("RecordingGpuQueueMaxDepth", typeof(int)),
                new("RecordingGpuFramesEnqueued", typeof(long)),
                new("RecordingGpuFramesDropped", typeof(long)),
                new("RecordingCudaQueueDepth", typeof(int)),
                new("RecordingCudaQueueCapacity", typeof(int)),
                new("RecordingCudaQueueMaxDepth", typeof(int)),
                new("RecordingCudaFramesEnqueued", typeof(long)),
                new("RecordingCudaFramesDropped", typeof(long)),
                new("FlashbackEncodingFailed", typeof(bool)),
                NullableString("FlashbackEncodingFailureType"),
                NullableString("FlashbackEncodingFailureMessage"),
                new("FatalCleanupInProgress", typeof(bool)),
                new("FlashbackCleanupInProgress", typeof(bool)),
                new("FlashbackForceRotateActive", typeof(bool)),
                new("FlashbackForceRotateRequested", typeof(bool)),
                new("FlashbackForceRotateDraining", typeof(bool)),
                new("FlashbackVideoQueueCapacity", typeof(int)),
                new("FlashbackVideoQueueMaxDepth", typeof(int)),
                new("FlashbackVideoFramesSubmittedToEncoder", typeof(long)),
                new("FlashbackVideoEncoderPts", typeof(long)),
                new("FlashbackVideoEncoderPacketsWritten", typeof(long)),
                new("FlashbackVideoEncoderDroppedFrames", typeof(long)),
                new("FlashbackVideoSequenceGaps", typeof(long)),
                new("FlashbackVideoQueueRejectedFrames", typeof(long)),
                NonNullString("FlashbackVideoQueueLastRejectReason"),
                new("FlashbackVideoQueueOldestFrameAgeMs", typeof(long)),
                new("FlashbackVideoQueueLastLatencyMs", typeof(long)),
                new("FlashbackVideoQueueLatencySampleCount", typeof(int)),
                new("FlashbackVideoQueueLatencyAvgMs", typeof(double)),
                new("FlashbackVideoQueueLatencyP95Ms", typeof(double)),
                new("FlashbackVideoQueueLatencyP99Ms", typeof(double)),
                new("FlashbackVideoQueueLatencyMaxMs", typeof(double)),
                new("FlashbackVideoBackpressureWaitMs", typeof(long)),
                new("FlashbackVideoBackpressureEvents", typeof(long)),
                new("FlashbackVideoBackpressureLastWaitMs", typeof(long)),
                new("FlashbackVideoBackpressureMaxWaitMs", typeof(long)),
                new("FlashbackGpuQueueDepth", typeof(int)),
                new("FlashbackGpuQueueCapacity", typeof(int)),
                new("FlashbackGpuQueueMaxDepth", typeof(int)),
                new("FlashbackGpuFramesEnqueued", typeof(long)),
                new("FlashbackGpuFramesDropped", typeof(long)),
                new("FlashbackGpuQueueRejectedFrames", typeof(long)),
                NonNullString("FlashbackGpuQueueLastRejectReason"),
                new("AudioDropsQueueSaturated", typeof(long)),
                new("AudioDropsBacklogEviction", typeof(long)),
                new("AudioChunksDropped", typeof(long))
            });
    }

    private static void AssertDeclaredProperties(Type type, SnapshotPropertySpec[] expectedProperties)
    {
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .ToDictionary(property => property.Name, StringComparer.Ordinal);
        var actualNames = properties.Keys.OrderBy(name => name, StringComparer.Ordinal).ToArray();
        var expectedNames = expectedProperties.Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (!actualNames.SequenceEqual(expectedNames))
        {
            throw new InvalidOperationException(
                $"{type.Name} public property set changed. Expected: {string.Join(", ", expectedNames)}; actual: {string.Join(", ", actualNames)}.");
        }

        SnapshotPropertySpecsByType[type] = expectedProperties;
        foreach (var expected in expectedProperties)
        {
            RequireSnapshotProperty(type, expected);
        }
    }

    private static SnapshotPropertySpec NonNullString(string name)
        => new(name, typeof(string), Nullability: SnapshotNullability.NotNull);

    private static SnapshotPropertySpec NullableString(string name)
        => new(name, typeof(string), Nullability: SnapshotNullability.Nullable);

    private static SnapshotPropertySpec NonNullRef(
        string name,
        Type type,
        SnapshotNullability elementNullability = SnapshotNullability.NotApplicable)
        => new(name, type, Nullability: SnapshotNullability.NotNull, ElementNullability: elementNullability);

    private static SnapshotPropertySpec GetterOnly(string name, Type type)
        => new(name, type, SnapshotSetterExpectation.None);

    private static PropertyInfo RequireSnapshotProperty(Type type, SnapshotPropertySpec expected)
    {
        var property = type.GetProperty(expected.Name, BindingFlags.Instance | BindingFlags.Public);
        AssertNotNull(property, $"{type.Name}.{expected.Name}");
        AssertEqual(expected.Type, property!.PropertyType, $"{type.Name}.{expected.Name} property type");
        if (property.GetMethod == null || !property.GetMethod.IsPublic)
        {
            throw new InvalidOperationException($"{type.Name}.{expected.Name} must expose a public getter.");
        }

        if (expected.Setter == SnapshotSetterExpectation.None)
        {
            if (property.SetMethod != null)
            {
                throw new InvalidOperationException($"{type.Name}.{expected.Name} must not expose a setter.");
            }
        }
        else
        {
            if (property.SetMethod == null || !property.SetMethod.IsPublic)
            {
                throw new InvalidOperationException($"{type.Name}.{expected.Name} must expose a public init setter.");
            }

            var isInitOnly = property.SetMethod.ReturnParameter.GetRequiredCustomModifiers()
                .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");
            if (!isInitOnly)
            {
                throw new InvalidOperationException($"{type.Name}.{expected.Name} must be init-only.");
            }
        }

        if (expected.Nullability != SnapshotNullability.NotApplicable)
        {
            var nullability = new NullabilityInfoContext().Create(property);
            var expectedState = expected.Nullability == SnapshotNullability.Nullable
                ? NullabilityState.Nullable
                : NullabilityState.NotNull;
            AssertEqual(expectedState, nullability.ReadState, $"{type.Name}.{expected.Name} read nullability");
            if (expected.Setter == SnapshotSetterExpectation.InitOnly)
            {
                AssertEqual(expectedState, nullability.WriteState, $"{type.Name}.{expected.Name} write nullability");
            }

            if (expected.ElementNullability != SnapshotNullability.NotApplicable)
            {
                var elementNullability = property.PropertyType.IsArray
                    ? nullability.ElementType
                    : nullability.GenericTypeArguments.FirstOrDefault();
                if (elementNullability == null)
                {
                    throw new InvalidOperationException($"{type.Name}.{expected.Name} did not expose element nullability.");
                }

                var expectedElementState = expected.ElementNullability == SnapshotNullability.Nullable
                    ? NullabilityState.Nullable
                    : NullabilityState.NotNull;
                AssertEqual(expectedElementState, elementNullability.ReadState, $"{type.Name}.{expected.Name} element read nullability");
                AssertEqual(expectedElementState, elementNullability.WriteState, $"{type.Name}.{expected.Name} element write nullability");
            }
        }

        return property;
    }

    private static object CreateGenericList(Type elementType, object item)
    {
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = Activator.CreateInstance(listType)
            ?? throw new InvalidOperationException($"Failed to create List<{elementType.Name}>.");
        listType.GetMethod("Add", new[] { elementType })!.Invoke(list, new[] { item });
        return list;
    }

    private static object GetSingleEnumerableItem(object value)
    {
        var items = ((IEnumerable)value).Cast<object>().ToArray();
        AssertEqual(1, items.Length, "IEnumerable item count");
        return items[0];
    }

    private static void AssertNonNullStringValue(
        object instance,
        string propertyName,
        string expectedValue,
        string fieldName)
    {
        var value = GetPropertyValue(instance, propertyName)
            ?? throw new InvalidOperationException($"{fieldName}: expected non-null string value.");
        AssertEqual(expectedValue, value, fieldName);
    }

    // LoggingJsonContext.Tests covers the production source-generated routing; this harness
    // validates the DTO reflection JSON shape because it loads the app in an isolated context.
    private static object ReflectionJsonRoundTrip(Type type, object value)
    {
        var json = JsonSerializer.Serialize(value, type);
        using var document = JsonDocument.Parse(json);
        AssertReflectionJsonPropertyNames(type, document.RootElement);
        return JsonSerializer.Deserialize(json, type)
            ?? throw new InvalidOperationException($"{type.Name} reflection JSON round-trip returned null.");
    }

    private static void AssertReflectionJsonPropertyNames(Type type, JsonElement rootElement)
    {
        if (rootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException($"{type.Name} reflection JSON should serialize as an object.");
        }

        var actualNames = rootElement.EnumerateObject()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        var expectedNames = GetExpectedRegisteredReflectionJsonPropertyNames(type)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        foreach (var expectedName in expectedNames)
        {
            if (!actualNames.Contains(expectedName))
            {
                throw new InvalidOperationException($"{type.Name} reflection JSON missing property '{expectedName}'.");
            }
        }

        var unexpectedNames = actualNames
            .Except(expectedNames, StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        if (unexpectedNames.Length != 0)
        {
            throw new InvalidOperationException(
                $"{type.Name} reflection JSON emitted unexpected properties: {string.Join(", ", unexpectedNames)}.");
        }
    }

    private static IEnumerable<string> GetExpectedRegisteredReflectionJsonPropertyNames(Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetMethod == null || property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            var declaringType = property.DeclaringType ?? type;
            if (!SnapshotPropertySpecsByType.TryGetValue(declaringType, out var expectedProperties))
            {
                throw new InvalidOperationException(
                    $"{type.Name}.{property.Name} reflection JSON check requires registered specs for {declaringType.Name}.");
            }

            var matchedExpectedProperty = expectedProperties.Any(
                expected => string.Equals(expected.Name, property.Name, StringComparison.Ordinal));
            if (!matchedExpectedProperty)
            {
                throw new InvalidOperationException(
                    $"{type.Name}.{property.Name} reflection JSON check was not covered by the registered {declaringType.Name} property specs.");
            }

            yield return property.Name;
        }
    }

    private static object CreateMjpegDecoderHealthSnapshot(
        Type decoderType,
        int workerIndex,
        int sampleCount,
        double avgMs,
        double p95Ms,
        double maxMs)
        => Activator.CreateInstance(decoderType, workerIndex, sampleCount, avgMs, p95Ms, maxMs)
           ?? throw new InvalidOperationException("Failed to create MjpegDecoderHealthSnapshot.");
}
