using System;

static partial class Program
{
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
}