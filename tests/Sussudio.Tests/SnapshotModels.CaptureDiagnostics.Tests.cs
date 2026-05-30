using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    [Fact]
    public void CaptureDiagnosticsSnapshot_DefaultsAndRoundTripsCoreTelemetry()
    {
        var diagnosticsRootText = ReadRepoFile("Sussudio/Models/Capture/CaptureSnapshotModels.cs");
        AssertContains(diagnosticsRootText, "public class CaptureDiagnosticsSnapshot");
        AssertContains(diagnosticsRootText, "public SourceTelemetryAvailability SourceTelemetryAvailability { get; init; } = SourceTelemetryAvailability.Unknown;");
        AssertContains(diagnosticsRootText, "public bool? SourceIsHdr { get; init; }");
        AssertContains(diagnosticsRootText, "public int CaptureCadenceSampleCount { get; init; }");
        AssertContains(diagnosticsRootText, "public double[] CaptureCadenceRecentIntervalsMs { get; init; } = Array.Empty<double>();");
        AssertContains(diagnosticsRootText, "public int RecordingVideoQueueCapacity { get; init; }");
        AssertContains(diagnosticsRootText, "public long AudioChunksDropped { get; init; }");
        AssertContains(diagnosticsRootText, "public bool FlashbackActive { get; init; }");
        AssertContains(diagnosticsRootText, "public bool FlashbackForceRotateActive { get; init; }");
        AssertContains(diagnosticsRootText, "public sealed record MjpegDecoderHealthSnapshot(");
        AssertContains(diagnosticsRootText, "public int MjpegDecodeSampleCount { get; init; }");
        AssertContains(diagnosticsRootText, "public double[] VisualCenterCadenceRecentChangeIntervalsMs { get; init; } = Array.Empty<double>();");
        AssertDoesNotContain(diagnosticsRootText, "partial class CaptureDiagnosticsSnapshot");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Models", "Capture", "CaptureDiagnosticsSnapshot.cs")),
            "CaptureDiagnosticsSnapshot.cs folded into CaptureSnapshotModels.cs");

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

    }

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
