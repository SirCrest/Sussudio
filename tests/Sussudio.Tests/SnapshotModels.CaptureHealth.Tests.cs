using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
    private static SnapshotPropertySpec[] CaptureHealthSnapshotPropertySpecs(Type detailType)
    {
        return new SnapshotPropertySpec[]
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
            new("EncoderFrameRateNumerator", typeof(int?)),
            new("EncoderFrameRateDenominator", typeof(int?)),
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
            new("AvSyncEncoderCorrectionSamples", typeof(long?)),
        };
    }

    private static SnapshotPropertySpec[] CaptureHealthSourceTelemetryDetailPropertySpecs()
    {
        return new SnapshotPropertySpec[]
        {
            NonNullString("Group"),
            NonNullString("Label"),
            NonNullString("DisplayValue"),
            NullableString("RawValue"),
        };
    }

    private static void AssertCaptureHealthSnapshotDefaultsAndInheritance(Type diagnosticsType, Type healthType)
    {
        if (!healthType.IsSealed)
        {
            throw new InvalidOperationException("CaptureHealthSnapshot must remain sealed.");
        }

        if (!diagnosticsType.IsAssignableFrom(healthType))
        {
            throw new InvalidOperationException("CaptureHealthSnapshot must inherit CaptureDiagnosticsSnapshot.");
        }
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
    }

    private static object CreateSourceTelemetryDetailEntry(Type detailType)
    {
        var detailEntry = Activator.CreateInstance(detailType, "Signal", "Colorimetry", "BT.2020", "bt2020")
            ?? throw new InvalidOperationException("Failed to create SourceTelemetryDetailEntry.");
        return detailEntry;
    }

    private static void AssertSourceTelemetryDetailEntryValues(object detailEntry)
    {
        AssertEqual("Signal", GetStringProperty(detailEntry, "Group"), "SourceTelemetryDetailEntry.Group");
        AssertEqual("Colorimetry", GetStringProperty(detailEntry, "Label"), "SourceTelemetryDetailEntry.Label");
        AssertEqual("BT.2020", GetStringProperty(detailEntry, "DisplayValue"), "SourceTelemetryDetailEntry.DisplayValue");
        AssertEqual("bt2020", GetStringProperty(detailEntry, "RawValue"), "SourceTelemetryDetailEntry.RawValue");
    }

    private static void AssertSourceTelemetryDetailEntryJsonRoundTrip(Type detailType, object detailEntry)
    {
        var detailJsonRoundTrip = ReflectionJsonRoundTrip(detailType, detailEntry);
        AssertEqual("BT.2020", GetStringProperty(detailJsonRoundTrip, "DisplayValue"), "SourceTelemetryDetailEntry JSON DisplayValue");
    }

    private static object CreatePopulatedCaptureHealthSnapshot(Type healthType, Type detailType, object detailEntry)
    {
        var health = CreateInstance(healthType.FullName!);
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

        return health;
    }

    private static void AssertCaptureHealthSnapshotRoundTripValues(object health)
    {
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
    }

    private static void AssertCaptureHealthSnapshotJsonRoundTrip(Type healthType, object health)
    {
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
    }

    [Fact]
    public void CaptureHealthSnapshot_ExtendsDiagnosticsWithFlashbackSourceAndAvSync()
    {
        var diagnosticsType = RequireType("Sussudio.Models.CaptureDiagnosticsSnapshot");
        var healthType = RequireType("Sussudio.Models.CaptureHealthSnapshot");
        var detailType = RequireType("Sussudio.Models.SourceTelemetryDetailEntry");
        var healthRootText = ReadRepoFile("Sussudio/Models/Capture/CaptureSnapshotModels.cs");

        AssertCaptureHealthSnapshotDefaultsAndInheritance(diagnosticsType, healthType);
        RegisterCaptureDiagnosticsSnapshotProperties(diagnosticsType);
        AssertDeclaredProperties(healthType, CaptureHealthSnapshotPropertySpecs(detailType));
        AssertDeclaredProperties(detailType, CaptureHealthSourceTelemetryDetailPropertySpecs());
        AssertContains(healthRootText, "public sealed class CaptureHealthSnapshot : CaptureDiagnosticsSnapshot");
        AssertContains(healthRootText, "public IReadOnlyList<SourceTelemetryDetailEntry> SourceTelemetryDetails");
        AssertContains(healthRootText, "public bool FlashbackBackendSettingsStale { get; init; }");
        AssertContains(healthRootText, "public int FlashbackAudioQueueCapacity { get; init; }");
        AssertContains(healthRootText, "public string FlashbackPlaybackState { get; init; } = \"N/A\";");
        AssertContains(healthRootText, "public string FlashbackPlaybackLastCommandFailure { get; init; } = string.Empty;");
        AssertContains(healthRootText, "public string FlashbackExportStatus { get; init; } = \"NotStarted\";");
        AssertContains(healthRootText, "public string? FlashbackExportVerificationFormat { get; init; }");
        AssertDoesNotContain(healthRootText, "partial class CaptureHealthSnapshot");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Models", "Capture", "CaptureHealthSnapshot.cs")),
            "CaptureHealthSnapshot.cs folded into CaptureSnapshotModels.cs");

        var detailEntry = CreateSourceTelemetryDetailEntry(detailType);
        AssertSourceTelemetryDetailEntryValues(detailEntry);
        AssertSourceTelemetryDetailEntryJsonRoundTrip(detailType, detailEntry);

        var health = CreatePopulatedCaptureHealthSnapshot(healthType, detailType, detailEntry);
        AssertCaptureHealthSnapshotRoundTripValues(health);
        AssertCaptureHealthSnapshotJsonRoundTrip(healthType, health);
    }
}
