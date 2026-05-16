using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

public sealed class CaptureServiceHealthSnapshotOwnershipTests
{
    [Fact]
    public void CaptureService_HealthSnapshotAssemblyLivesInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "return AssembleCaptureHealthSnapshot(");
        AssertContains(healthSnapshotText, "new CaptureHealthSnapshotAssemblyFields(");
        AssertContains(healthSnapshotAssemblerText, "private CaptureHealthSnapshot AssembleCaptureHealthSnapshot(");
        AssertContains(healthSnapshotAssemblerText, "private readonly record struct CaptureHealthSnapshotAssemblyFields(");
        AssertDoesNotContain(healthSnapshotAssemblerText, "LibAvRecordingSink? Sink");
        AssertDoesNotContain(healthSnapshotAssemblerText, "var sink = fields.Sink;");
        AssertContains(healthSnapshotAssemblerText, "TimestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(snapshotUtcUnixMs),");
        AssertDoesNotContain(healthSnapshotText, "return new CaptureHealthSnapshot");

    }

    [Fact]
    public void CaptureService_HealthSnapshotFlashbackExportFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var flashbackExportText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackExport.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var flashbackExport = CaptureFlashbackExportHealthSnapshotFields(snapshotUtcUnixMs);");
        AssertContains(healthSnapshotAssemblerText, "FlashbackExportActive = flashbackExport.Active,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackExportElapsedMs = flashbackExport.ElapsedMs,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackExportThroughputBytesPerSec = flashbackExport.ThroughputBytesPerSec,");
        AssertContains(healthSnapshotAssemblerText, "LastExportId = flashbackExport.LastResultId,");
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

    }

    [Fact]
    public void CaptureService_HealthSnapshotFlashbackBufferFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var flashbackBufferText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackBuffer.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var flashbackBuffer = CaptureFlashbackBufferHealthSnapshotFields(");
        AssertContains(healthSnapshotAssemblerText, "FlashbackBufferedDurationMs = flashbackBuffer.BufferedDurationMs,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackBackendSettingsStaleReason = flashbackBuffer.BackendSettingsStaleReason,");
        AssertContains(healthSnapshotAssemblerText, "EncoderTargetBitRate = flashbackBuffer.EncoderTargetBitRate,");
        AssertDoesNotContain(healthSnapshotText, "FlashbackBufferedDurationMs = (long)(bufMgr?.BufferedDuration.TotalMilliseconds ?? 0)");
        AssertDoesNotContain(healthSnapshotText, "ResolveFlashbackBackendSettingsStaleReason(flashbackBackendSettings, _currentSettings)");

        AssertContains(flashbackBufferText, "private FlashbackBufferHealthSnapshotFields CaptureFlashbackBufferHealthSnapshotFields(");
        AssertContains(flashbackBufferText, "ResolveFlashbackBackendSettingsStaleReason(flashbackBackendSettings, currentSettings)");
        AssertContains(flashbackBufferText, "private static string ResolveFlashbackBackendSettingsStaleReason(");
        AssertContains(flashbackBufferText, "bufMgr?.StartupCacheOverBudget ?? false");
        AssertContains(flashbackBufferText, "fbSink?.EncoderFrameRateDenominator");
        AssertContains(flashbackBufferText, "private readonly record struct FlashbackBufferHealthSnapshotFields");

    }

    [Fact]
    public void CaptureService_HealthSnapshotRecordingFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var recordingText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotRecording.cs")
            .Replace("\r\n", "\n");
        var activeBackendText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotRecordingActiveBackend.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var recordingHealth = CaptureRecordingHealthSnapshotFields(sink, fbSink);");
        AssertContains(healthSnapshotAssemblerText, "RecordingEncodingFailed = recordingHealth.EncodingFailed,");
        AssertContains(healthSnapshotAssemblerText, "RecordingVideoQueueLatencyP95Ms = recordingHealth.VideoQueueLatencyMetrics.P95Ms,");
        AssertContains(healthSnapshotAssemblerText, "RecordingCudaQueueDepth = recordingHealth.CudaQueueDepth,");
        AssertContains(healthSnapshotAssemblerText, "RecordingCudaQueueCapacity = recordingHealth.CudaQueueCapacity,");
        AssertContains(healthSnapshotAssemblerText, "RecordingCudaQueueMaxDepth = recordingHealth.CudaQueueMaxDepth,");
        AssertContains(healthSnapshotAssemblerText, "RecordingCudaFramesEnqueued = recordingHealth.CudaFramesEnqueued,");
        AssertContains(healthSnapshotAssemblerText, "RecordingCudaFramesDropped = recordingHealth.CudaFramesDropped,");
        AssertDoesNotContain(healthSnapshotAssemblerText, "RecordingCudaQueueDepth = sink?.CudaQueueCount ?? 0,");
        AssertDoesNotContain(healthSnapshotAssemblerText, "RecordingCudaFramesDropped = sink?.CudaFramesDropped ?? 0,");
        AssertDoesNotContain(healthSnapshotText, "var activeRecordingVideoQueueLatencyMetrics");
        AssertDoesNotContain(healthSnapshotText, "var flashbackIsRecordingBackend = IsFlashbackRecordingBackendOwnedByRecording();");

        AssertContains(recordingText, "private RecordingHealthSnapshotFields CaptureRecordingHealthSnapshotFields(");
        AssertContains(recordingText, "GetLastFailureTelemetry()");
        AssertContains(recordingText, "IsFlashbackRecordingBackendOwnedByRecording()");
        AssertContains(recordingText, "CaptureActiveRecordingBackendHealthSnapshotFields(");
        AssertContains(recordingText, "activeRecording.FlashbackVideoQueueLatencyMetrics");
        AssertContains(recordingText, "sink?.CudaQueueCount ?? 0");
        AssertContains(recordingText, "sink?.CudaQueueCapacityFrames ?? 0");
        AssertContains(recordingText, "sink?.CudaQueueMaxDepth ?? 0");
        AssertContains(recordingText, "sink?.CudaFramesEnqueued ?? 0");
        AssertContains(recordingText, "sink?.CudaFramesDropped ?? 0");
        AssertContains(recordingText, "int CudaQueueDepth");
        AssertContains(recordingText, "long CudaFramesDropped");
        AssertContains(recordingText, "private readonly record struct RecordingHealthSnapshotFields");
        AssertDoesNotContain(recordingText, "var flashbackVideoQueueLatencyMetrics");
        AssertDoesNotContain(recordingText, "sink?.VideoQueueLatencyMetrics ??");
        AssertDoesNotContain(recordingText, "Interlocked.Read(ref _videoFramesDropped)");
        AssertDoesNotContain(recordingText, "flashbackIsRecordingBackend ? fbSink?.VideoQueueCount ?? 0 : 0");

        AssertContains(activeBackendText, "private ActiveRecordingBackendHealthSnapshotFields CaptureActiveRecordingBackendHealthSnapshotFields(");
        AssertContains(activeBackendText, "sink?.VideoQueueLatencyMetrics ??");
        AssertContains(activeBackendText, "flashbackIsRecordingBackend ? fbSink?.VideoQueueCount ?? 0 : 0");
        AssertContains(activeBackendText, "Interlocked.Read(ref _videoFramesDropped)");
        AssertContains(activeBackendText, "private readonly record struct ActiveRecordingBackendHealthSnapshotFields");

    }

    [Fact]
    public void CaptureService_HealthSnapshotFlashbackQueueFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var flashbackQueueText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackQueues.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var flashbackQueues = CaptureFlashbackQueueHealthSnapshotFields(");
        AssertContains(healthSnapshotAssemblerText, "FlashbackVideoQueueDepth = flashbackQueues.VideoQueueDepth,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackForceRotateActive = flashbackQueues.ForceRotateActive,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackVideoQueueLatencyP99Ms = flashbackQueues.VideoQueueLatencyMetrics.P99Ms,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackGpuQueueLastRejectReason = flashbackQueues.GpuQueueLastRejectReason,");
        AssertDoesNotContain(healthSnapshotText, "FlashbackVideoQueueDepth = fbSink?.VideoQueueCount");
        AssertDoesNotContain(healthSnapshotText, "FlashbackForceRotateActive = fbSink?.IsForceRotateActive");
        AssertDoesNotContain(healthSnapshotText, "FlashbackGpuQueueLastRejectReason = fbSink?.LastGpuQueueRejectReason");

        AssertContains(flashbackQueueText, "private static FlashbackQueueHealthSnapshotFields CaptureFlashbackQueueHealthSnapshotFields(");
        AssertContains(flashbackQueueText, "fbSink?.VideoQueueOldestFrameAgeMs ?? 0");
        AssertContains(flashbackQueueText, "fbSink?.IsForceRotateActive ?? false");
        AssertContains(flashbackQueueText, "fbSink?.LastGpuQueueRejectReason ?? string.Empty");
        AssertContains(flashbackQueueText, "private readonly record struct FlashbackQueueHealthSnapshotFields");

    }

    [Fact]
    public void CaptureService_HealthSnapshotFlashbackPlaybackFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var flashbackPlaybackText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackPlayback.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var flashbackPlayback = CaptureFlashbackPlaybackHealthSnapshotFields(fbPlayback);");
        AssertContains(healthSnapshotAssemblerText, "FlashbackPlaybackState = flashbackPlayback.State,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackPlaybackDecodeP95Ms = flashbackPlayback.DecodeP95Ms,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackPlaybackLastCommandFailure = flashbackPlayback.LastCommandFailure,");
        AssertDoesNotContain(healthSnapshotText, "var playbackCadence = fbPlayback?.GetPlaybackCadenceMetrics()");
        AssertDoesNotContain(healthSnapshotText, "var playbackDecode = fbPlayback?.GetPlaybackDecodeMetrics()");
        AssertDoesNotContain(healthSnapshotText, "FlashbackPlaybackFrameCount = fbPlayback?.PlaybackFrameCount");

        AssertContains(flashbackPlaybackText, "private static FlashbackPlaybackHealthSnapshotFields CaptureFlashbackPlaybackHealthSnapshotFields(");
        AssertContains(flashbackPlaybackText, "fbPlayback?.GetPlaybackCadenceMetrics() ?? default");
        AssertContains(flashbackPlaybackText, "fbPlayback?.GetPlaybackDecodeMetrics() ?? default");
        AssertContains(flashbackPlaybackText, "private readonly record struct FlashbackPlaybackHealthSnapshotFields");

    }

    [Fact]
    public void CaptureService_HealthSnapshotSourceTelemetryFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var sourceTelemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotSourceTelemetry.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var sourceTelemetry = CaptureSourceTelemetryHealthSnapshotFields(_latestSourceTelemetry);");
        AssertContains(healthSnapshotAssemblerText, "SourceTelemetryAvailability = sourceTelemetry.Availability,");
        AssertContains(healthSnapshotAssemblerText, "SourceTelemetryBackend = sourceTelemetry.Backend,");
        AssertContains(healthSnapshotAssemblerText, "SourceTelemetryCircuitState = sourceTelemetry.CircuitState,");
        AssertDoesNotContain(healthSnapshotText, "ResolveSourceTelemetrySuppressedReason(_latestSourceTelemetry)");
        AssertDoesNotContain(healthSnapshotText, "ResolveSourceTelemetryCircuitState(");

        AssertContains(sourceTelemetryText, "private SourceTelemetryHealthSnapshotFields CaptureSourceTelemetryHealthSnapshotFields(");
        AssertContains(sourceTelemetryText, "ResolveSourceTelemetrySuppressedReason(telemetry) ?? string.Empty");
        AssertContains(sourceTelemetryText, "ResolveSourceTelemetryBackend(telemetry)");
        AssertContains(sourceTelemetryText, "ResolveSourceTelemetryCircuitState(telemetry.Availability, suppressed)");
        AssertContains(sourceTelemetryText, "private readonly record struct SourceTelemetryHealthSnapshotFields");

    }

    [Fact]
    public void CaptureService_HealthSnapshotCaptureCadenceFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var captureCadenceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotCaptureCadence.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var captureCadence = BuildCaptureCadenceHealthSnapshotFields(unifiedVideoCapture);");
        AssertContains(healthSnapshotAssemblerText, "CaptureCadenceSampleCount = captureCadence.SampleCount,");
        AssertContains(healthSnapshotAssemblerText, "CaptureCadenceEstimatedDropPercent = captureCadence.EstimatedDropPercent,");
        AssertDoesNotContain(healthSnapshotText, "GetSourceCadenceMetrics()");
        AssertDoesNotContain(healthSnapshotText, "MfSourceReaderVideoCapture.SourceCadenceMetrics");

        AssertContains(captureCadenceText, "private static CaptureCadenceHealthSnapshotFields BuildCaptureCadenceHealthSnapshotFields(");
        AssertContains(captureCadenceText, "unifiedVideoCapture?.GetSourceCadenceMetrics()");
        AssertContains(captureCadenceText, "default(MfSourceReaderVideoCapture.SourceCadenceMetrics)");
        AssertContains(captureCadenceText, "private readonly record struct CaptureCadenceHealthSnapshotFields");

    }

    [Fact]
    public void CaptureService_HealthSnapshotMjpegFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var mjpegText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotMjpeg.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var mjpegHealth = CaptureMjpegHealthSnapshotFields(unifiedVideoCapture);");
        AssertContains(healthSnapshotAssemblerText, "MjpegDecodeSampleCount = mjpegHealth.Timing.DecodeSampleCount,");
        AssertContains(healthSnapshotAssemblerText, "MjpegPreviewJitterEnabled = mjpegHealth.PreviewJitter.Enabled,");
        AssertContains(healthSnapshotAssemblerText, "VisualCadenceSampleCount = mjpegHealth.VisualCadence.SampleCount,");
        AssertContains(healthSnapshotAssemblerText, "MjpegPerDecoder = mjpegHealth.PerDecoder,");
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

    }

    [Fact]
    public void CaptureService_HealthSnapshotAvSyncFields_LiveInFocusedPartial()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var avSyncText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.AvSync.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var avSyncHealth = CaptureAvSyncHealthSnapshotFields();");
        AssertContains(healthSnapshotAssemblerText, "AvSyncCaptureDriftMs = avSyncHealth.CaptureDriftMs,");
        AssertContains(healthSnapshotAssemblerText, "AvSyncCaptureDriftRateMsPerSec = avSyncHealth.CaptureDriftRateMsPerSec,");
        AssertContains(healthSnapshotAssemblerText, "AvSyncEncoderCorrectionSamples = avSyncHealth.EncoderCorrectionSamples");
        AssertDoesNotContain(healthSnapshotText, "var (avSyncDriftMs, avSyncDriftRate) = ComputeAvSyncDrift();");
        AssertDoesNotContain(healthSnapshotText, "var (avSyncEncoderDriftMs, avSyncEncoderCorrectionSamples) = GetEncoderAvSyncDrift();");

        AssertContains(avSyncText, "private AvSyncHealthSnapshotFields CaptureAvSyncHealthSnapshotFields()");
        AssertContains(avSyncText, "var (captureDriftMs, captureDriftRateMsPerSec) = ComputeAvSyncDrift();");
        AssertContains(avSyncText, "var (encoderDriftMs, encoderCorrectionSamples) = GetEncoderAvSyncDrift();");
        AssertContains(avSyncText, "private readonly record struct AvSyncHealthSnapshotFields");

    }

    private static void AssertContains(string text, string expected)
        => Assert.Contains(expected, text);

    private static void AssertDoesNotContain(string text, string expected)
        => Assert.DoesNotContain(expected, text);

    private static string ReadRepoFile(string relativePath)
    {
        var path = Path.Combine(FindRepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.ReadAllText(path);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Sussudio.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not find Sussudio repo root.");
    }
}

static partial class Program
{

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
