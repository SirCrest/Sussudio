using System;

namespace Sussudio.Tests;

public partial class SnapshotModelsTests
{
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
}