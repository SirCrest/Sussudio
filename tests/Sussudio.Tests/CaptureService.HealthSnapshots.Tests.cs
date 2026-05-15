using System;
using System.Threading.Tasks;

static partial class Program
{
    private static Task CaptureService_HealthSnapshotFlashbackExportFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var flashbackExportText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackExport.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var flashbackExport = CaptureFlashbackExportHealthSnapshotFields(snapshotUtcUnixMs);");
        AssertContains(healthSnapshotText, "FlashbackExportActive = flashbackExport.Active,");
        AssertContains(healthSnapshotText, "FlashbackExportElapsedMs = flashbackExport.ElapsedMs,");
        AssertContains(healthSnapshotText, "FlashbackExportThroughputBytesPerSec = flashbackExport.ThroughputBytesPerSec,");
        AssertContains(healthSnapshotText, "LastExportId = flashbackExport.LastResultId,");
        AssertDoesNotContain(healthSnapshotText, "lock (_flashbackExportDiagnosticsLock)");
        AssertDoesNotContain(healthSnapshotText, "ComputeFlashbackExportElapsedMs(");
        AssertDoesNotContain(healthSnapshotText, "GetFileLengthOrZero(");

        AssertContains(flashbackExportText, "private FlashbackExportHealthSnapshotFields CaptureFlashbackExportHealthSnapshotFields(");
        AssertContains(flashbackExportText, "lock (_flashbackExportDiagnosticsLock)");
        AssertContains(flashbackExportText, "private static long ComputeFlashbackExportElapsedMs(");
        AssertContains(flashbackExportText, "private static long ComputeFlashbackExportLastProgressAgeMs(");
        AssertContains(flashbackExportText, "private static long GetFileLengthOrZero(string? path)");
        AssertContains(flashbackExportText, "var elapsedMs = ComputeFlashbackExportElapsedMs(");
        AssertContains(flashbackExportText, "var lastProgressAgeMs = ComputeFlashbackExportLastProgressAgeMs(");
        AssertContains(flashbackExportText, "var outputBytes = GetFileLengthOrZero(");
        AssertContains(flashbackExportText, "ThroughputBytesPerSec = throughputBytesPerSec");
        AssertContains(flashbackExportText, "FinalizeResult? LastResult");

        return Task.CompletedTask;
    }

    private static Task CaptureService_HealthSnapshotFlashbackBufferFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var flashbackBufferText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackBuffer.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var flashbackBuffer = CaptureFlashbackBufferHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "FlashbackBufferedDurationMs = flashbackBuffer.BufferedDurationMs,");
        AssertContains(healthSnapshotText, "FlashbackBackendSettingsStaleReason = flashbackBuffer.BackendSettingsStaleReason,");
        AssertContains(healthSnapshotText, "EncoderTargetBitRate = flashbackBuffer.EncoderTargetBitRate,");
        AssertDoesNotContain(healthSnapshotText, "FlashbackBufferedDurationMs = (long)(bufMgr?.BufferedDuration.TotalMilliseconds ?? 0)");
        AssertDoesNotContain(healthSnapshotText, "ResolveFlashbackBackendSettingsStaleReason(flashbackBackendSettings, _currentSettings)");

        AssertContains(flashbackBufferText, "private FlashbackBufferHealthSnapshotFields CaptureFlashbackBufferHealthSnapshotFields(");
        AssertContains(flashbackBufferText, "ResolveFlashbackBackendSettingsStaleReason(flashbackBackendSettings, currentSettings)");
        AssertContains(flashbackBufferText, "private static string ResolveFlashbackBackendSettingsStaleReason(");
        AssertContains(flashbackBufferText, "bufMgr?.StartupCacheOverBudget ?? false");
        AssertContains(flashbackBufferText, "fbSink?.EncoderFrameRateDenominator");
        AssertContains(flashbackBufferText, "private readonly record struct FlashbackBufferHealthSnapshotFields");

        return Task.CompletedTask;
    }

    private static Task CaptureService_HealthSnapshotRecordingFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var recordingText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotRecording.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var recordingHealth = CaptureRecordingHealthSnapshotFields(sink, fbSink);");
        AssertContains(healthSnapshotText, "RecordingEncodingFailed = recordingHealth.EncodingFailed,");
        AssertContains(healthSnapshotText, "RecordingVideoQueueLatencyP95Ms = recordingHealth.VideoQueueLatencyMetrics.P95Ms,");
        AssertDoesNotContain(healthSnapshotText, "var activeRecordingVideoQueueLatencyMetrics");
        AssertDoesNotContain(healthSnapshotText, "var flashbackIsRecordingBackend = IsFlashbackRecordingBackendOwnedByRecording();");

        AssertContains(recordingText, "private RecordingHealthSnapshotFields CaptureRecordingHealthSnapshotFields(");
        AssertContains(recordingText, "GetLastFailureTelemetry()");
        AssertContains(recordingText, "IsFlashbackRecordingBackendOwnedByRecording()");
        AssertContains(recordingText, "Interlocked.Read(ref _videoFramesDropped)");
        AssertContains(recordingText, "private readonly record struct RecordingHealthSnapshotFields");

        return Task.CompletedTask;
    }

    private static Task CaptureService_HealthSnapshotFlashbackQueueFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var flashbackQueueText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackQueues.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var flashbackQueues = CaptureFlashbackQueueHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "FlashbackVideoQueueDepth = flashbackQueues.VideoQueueDepth,");
        AssertContains(healthSnapshotText, "FlashbackForceRotateActive = flashbackQueues.ForceRotateActive,");
        AssertContains(healthSnapshotText, "FlashbackVideoQueueLatencyP99Ms = flashbackQueues.VideoQueueLatencyMetrics.P99Ms,");
        AssertContains(healthSnapshotText, "FlashbackGpuQueueLastRejectReason = flashbackQueues.GpuQueueLastRejectReason,");
        AssertDoesNotContain(healthSnapshotText, "FlashbackVideoQueueDepth = fbSink?.VideoQueueCount");
        AssertDoesNotContain(healthSnapshotText, "FlashbackForceRotateActive = fbSink?.IsForceRotateActive");
        AssertDoesNotContain(healthSnapshotText, "FlashbackGpuQueueLastRejectReason = fbSink?.LastGpuQueueRejectReason");

        AssertContains(flashbackQueueText, "private static FlashbackQueueHealthSnapshotFields CaptureFlashbackQueueHealthSnapshotFields(");
        AssertContains(flashbackQueueText, "fbSink?.VideoQueueOldestFrameAgeMs ?? 0");
        AssertContains(flashbackQueueText, "fbSink?.IsForceRotateActive ?? false");
        AssertContains(flashbackQueueText, "fbSink?.LastGpuQueueRejectReason ?? string.Empty");
        AssertContains(flashbackQueueText, "private readonly record struct FlashbackQueueHealthSnapshotFields");

        return Task.CompletedTask;
    }

    private static Task CaptureService_HealthSnapshotFlashbackPlaybackFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackPlayback.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var flashbackPlayback = CaptureFlashbackPlaybackHealthSnapshotFields(fbPlayback);");
        AssertContains(healthSnapshotText, "FlashbackPlaybackState = flashbackPlayback.State,");
        AssertContains(healthSnapshotText, "FlashbackPlaybackDecodeP95Ms = flashbackPlayback.DecodeP95Ms,");
        AssertContains(healthSnapshotText, "FlashbackPlaybackLastCommandFailure = flashbackPlayback.LastCommandFailure,");
        AssertDoesNotContain(healthSnapshotText, "var playbackCadence = fbPlayback?.GetPlaybackCadenceMetrics()");
        AssertDoesNotContain(healthSnapshotText, "var playbackDecode = fbPlayback?.GetPlaybackDecodeMetrics()");
        AssertDoesNotContain(healthSnapshotText, "FlashbackPlaybackFrameCount = fbPlayback?.PlaybackFrameCount");

        AssertContains(flashbackPlaybackText, "private static FlashbackPlaybackHealthSnapshotFields CaptureFlashbackPlaybackHealthSnapshotFields(");
        AssertContains(flashbackPlaybackText, "fbPlayback?.GetPlaybackCadenceMetrics() ?? default");
        AssertContains(flashbackPlaybackText, "fbPlayback?.GetPlaybackDecodeMetrics() ?? default");
        AssertContains(flashbackPlaybackText, "private readonly record struct FlashbackPlaybackHealthSnapshotFields");

        return Task.CompletedTask;
    }

    private static Task CaptureService_HealthSnapshotSourceTelemetryFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var sourceTelemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotSourceTelemetry.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var sourceTelemetry = CaptureSourceTelemetryHealthSnapshotFields(_latestSourceTelemetry);");
        AssertContains(healthSnapshotText, "SourceTelemetryAvailability = sourceTelemetry.Availability,");
        AssertContains(healthSnapshotText, "SourceTelemetryBackend = sourceTelemetry.Backend,");
        AssertContains(healthSnapshotText, "SourceTelemetryCircuitState = sourceTelemetry.CircuitState,");
        AssertDoesNotContain(healthSnapshotText, "ResolveSourceTelemetrySuppressedReason(_latestSourceTelemetry)");
        AssertDoesNotContain(healthSnapshotText, "ResolveSourceTelemetryCircuitState(");

        AssertContains(sourceTelemetryText, "private SourceTelemetryHealthSnapshotFields CaptureSourceTelemetryHealthSnapshotFields(");
        AssertContains(sourceTelemetryText, "ResolveSourceTelemetrySuppressedReason(telemetry) ?? string.Empty");
        AssertContains(sourceTelemetryText, "ResolveSourceTelemetryBackend(telemetry)");
        AssertContains(sourceTelemetryText, "ResolveSourceTelemetryCircuitState(telemetry.Availability, suppressed)");
        AssertContains(sourceTelemetryText, "private readonly record struct SourceTelemetryHealthSnapshotFields");

        return Task.CompletedTask;
    }

    private static Task CaptureService_HealthSnapshotCaptureCadenceFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var captureCadenceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotCaptureCadence.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var captureCadence = BuildCaptureCadenceHealthSnapshotFields(unifiedVideoCapture);");
        AssertContains(healthSnapshotText, "CaptureCadenceSampleCount = captureCadence.SampleCount,");
        AssertContains(healthSnapshotText, "CaptureCadenceEstimatedDropPercent = captureCadence.EstimatedDropPercent,");
        AssertDoesNotContain(healthSnapshotText, "GetSourceCadenceMetrics()");
        AssertDoesNotContain(healthSnapshotText, "MfSourceReaderVideoCapture.SourceCadenceMetrics");

        AssertContains(captureCadenceText, "private static CaptureCadenceHealthSnapshotFields BuildCaptureCadenceHealthSnapshotFields(");
        AssertContains(captureCadenceText, "unifiedVideoCapture?.GetSourceCadenceMetrics()");
        AssertContains(captureCadenceText, "default(MfSourceReaderVideoCapture.SourceCadenceMetrics)");
        AssertContains(captureCadenceText, "private readonly record struct CaptureCadenceHealthSnapshotFields");

        return Task.CompletedTask;
    }

    private static Task CaptureService_HealthSnapshotMjpegFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var mjpegText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotMjpeg.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var mjpegHealth = CaptureMjpegHealthSnapshotFields(unifiedVideoCapture);");
        AssertContains(healthSnapshotText, "MjpegDecodeSampleCount = mjpegHealth.Timing.DecodeSampleCount,");
        AssertContains(healthSnapshotText, "MjpegPreviewJitterEnabled = mjpegHealth.PreviewJitter.Enabled,");
        AssertContains(healthSnapshotText, "VisualCadenceSampleCount = mjpegHealth.VisualCadence.SampleCount,");
        AssertContains(healthSnapshotText, "MjpegPerDecoder = mjpegHealth.PerDecoder,");
        AssertDoesNotContain(healthSnapshotText, "GetMjpegPipelineTimingSnapshot()");
        AssertDoesNotContain(healthSnapshotText, "GetMjpegPreviewJitterMetrics()");
        AssertDoesNotContain(healthSnapshotText, "FrameFingerprintCadenceTracker.Empty");
        AssertDoesNotContain(healthSnapshotText, "new MjpegDecoderHealthSnapshot(");

        AssertContains(mjpegText, "private MjpegHealthSnapshotFields CaptureMjpegHealthSnapshotFields(");
        AssertContains(mjpegText, "GetMjpegPipelineTimingSnapshot()");
        AssertContains(mjpegText, "GetMjpegPreviewJitterMetrics()");
        AssertContains(mjpegText, "GetPreviewVisualCadenceMetrics()");
        AssertContains(mjpegText, "FrameFingerprintCadenceTracker.Empty");
        AssertContains(mjpegText, "new MjpegDecoderHealthSnapshot(");
        AssertContains(mjpegText, "private readonly record struct MjpegHealthSnapshotFields");

        return Task.CompletedTask;
    }

    private static Task CaptureService_HealthSnapshotAvSyncFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var avSyncText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.AvSync.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var avSyncHealth = CaptureAvSyncHealthSnapshotFields();");
        AssertContains(healthSnapshotText, "AvSyncCaptureDriftMs = avSyncHealth.CaptureDriftMs,");
        AssertContains(healthSnapshotText, "AvSyncCaptureDriftRateMsPerSec = avSyncHealth.CaptureDriftRateMsPerSec,");
        AssertContains(healthSnapshotText, "AvSyncEncoderCorrectionSamples = avSyncHealth.EncoderCorrectionSamples");
        AssertDoesNotContain(healthSnapshotText, "var (avSyncDriftMs, avSyncDriftRate) = ComputeAvSyncDrift();");
        AssertDoesNotContain(healthSnapshotText, "var (avSyncEncoderDriftMs, avSyncEncoderCorrectionSamples) = GetEncoderAvSyncDrift();");

        AssertContains(avSyncText, "private AvSyncHealthSnapshotFields CaptureAvSyncHealthSnapshotFields()");
        AssertContains(avSyncText, "var (captureDriftMs, captureDriftRateMsPerSec) = ComputeAvSyncDrift();");
        AssertContains(avSyncText, "var (encoderDriftMs, encoderCorrectionSamples) = GetEncoderAvSyncDrift();");
        AssertContains(avSyncText, "private readonly record struct AvSyncHealthSnapshotFields");

        return Task.CompletedTask;
    }

    private static async Task CaptureHealthSnapshot_PropagatesStructuredSourceTelemetryDetails()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var sourceTelemetry = CreateInstance("Sussudio.Models.SourceSignalTelemetrySnapshot");
        SetPropertyOrBackingField(sourceTelemetry, "Availability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Available"));
        SetPropertyOrBackingField(sourceTelemetry, "Origin", ParseEnum("Sussudio.Models.SourceTelemetryOrigin", "NativeXu"));
        SetPropertyOrBackingField(sourceTelemetry, "Confidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "High"));
        SetPropertyOrBackingField(sourceTelemetry, "Width", 3840);
        SetPropertyOrBackingField(sourceTelemetry, "Height", 2160);
        SetPropertyOrBackingField(sourceTelemetry, "FrameRateExact", 119.88d);
        SetPropertyOrBackingField(sourceTelemetry, "IsHdr", true);
        SetPropertyOrBackingField(sourceTelemetry, "VideoFormat", "YCbCr422");
        SetPropertyOrBackingField(sourceTelemetry, "Colorimetry", "BT.2020");
        SetPropertyOrBackingField(sourceTelemetry, "Quantization", "Limited");
        SetPropertyOrBackingField(sourceTelemetry, "HdrTransferFunction", "HDR10 / PQ");
        SetPropertyOrBackingField(sourceTelemetry, "HdrTransferCode", 2);
        SetPropertyOrBackingField(sourceTelemetry, "AudioFormat", "Unknown (2)");
        SetPropertyOrBackingField(sourceTelemetry, "AudioSampleRate", "Unknown (7)");
        SetPropertyOrBackingField(sourceTelemetry, "InputSource", "HDMI (0)");
        SetPropertyOrBackingField(sourceTelemetry, "UsbHostProtocol", "Isochronous (2)");
        SetPropertyOrBackingField(sourceTelemetry, "HdcpMode", "Unknown (1)");
        SetPropertyOrBackingField(sourceTelemetry, "HdcpVersion", "0200");
        SetPropertyOrBackingField(sourceTelemetry, "RxTxHdcpVersion", "Unknown (3)");
        SetPropertyOrBackingField(sourceTelemetry, "RawTimingHex", "3000CA0830117008");

        var detailEntryType = RequireType("Sussudio.Models.SourceTelemetryDetailEntry");
        var detailEntry = Activator.CreateInstance(detailEntryType, "Signal Details", "Quantization", "Limited", "Limited")
            ?? throw new InvalidOperationException("SourceTelemetryDetailEntry instance creation failed.");
        var detailArray = Array.CreateInstance(detailEntryType, 1);
        detailArray.SetValue(detailEntry, 0);
        SetPropertyOrBackingField(sourceTelemetry, "DetailEntries", detailArray);

        SetPrivateField(captureService, "_latestSourceTelemetry", sourceTelemetry);

        var health = InvokeInstanceMethod(captureService, "GetHealthSnapshot");
        AssertEqual("YCbCr422", GetStringProperty(health, "SourceVideoFormat"), "SourceVideoFormat");
        AssertEqual("BT.2020", GetStringProperty(health, "SourceColorimetry"), "SourceColorimetry");
        AssertEqual("Limited", GetStringProperty(health, "SourceQuantization"), "SourceQuantization");
        AssertEqual("HDR10 / PQ", GetStringProperty(health, "SourceHdrTransferFunction"), "SourceHdrTransferFunction");
        AssertEqual("Unknown (2)", GetStringProperty(health, "SourceAudioFormat"), "SourceAudioFormat");
        AssertEqual("Unknown (7)", GetStringProperty(health, "SourceAudioSampleRate"), "SourceAudioSampleRate");
        AssertEqual("HDMI (0)", GetStringProperty(health, "SourceInputSource"), "SourceInputSource");
        AssertEqual("Isochronous (2)", GetStringProperty(health, "SourceUsbHostProtocol"), "SourceUsbHostProtocol");
        AssertEqual("Unknown (1)", GetStringProperty(health, "SourceHdcpMode"), "SourceHdcpMode");
        AssertEqual("0200", GetStringProperty(health, "SourceHdcpVersion"), "SourceHdcpVersion");
        AssertEqual("Unknown (3)", GetStringProperty(health, "SourceRxTxHdcpVersion"), "SourceRxTxHdcpVersion");
        AssertEqual("3000CA0830117008", GetStringProperty(health, "SourceRawTimingHex"), "SourceRawTimingHex");

        var details = GetPropertyValue(health, "SourceTelemetryDetails") as System.Collections.IEnumerable
            ?? throw new InvalidOperationException("SourceTelemetryDetails should be enumerable.");
        var detailCount = 0;
        foreach (var _ in details)
        {
            detailCount++;
        }

        AssertEqual(1, detailCount, "SourceTelemetryDetails.Count");
        await DisposeAsync(captureService).ConfigureAwait(false);
    }

}
