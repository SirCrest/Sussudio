using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

public sealed partial class CaptureServiceHealthSnapshotOwnershipTests
{
    [Fact]
    public void CaptureService_HealthSnapshotAssemblyFields_LiveWithAssembler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "return CaptureHealthSnapshotAssembler.Build(new CaptureHealthSnapshotAssemblyFields");
        AssertContains(healthSnapshotText, "SessionState = CurrentSessionState,");
        AssertContains(healthSnapshotText, "FlashbackExportVerificationFormat = ResolveFlashbackExportVerificationFormat(currentSettings, unifiedVideoCapture),");
        AssertContains(healthSnapshotText, "LastFrameArrivalMs = ComputeTickAge(unifiedVideoCapture?.LastVideoFrameArrivedTick ?? 0),");
        AssertContains(healthSnapshotAssemblerText, "private static class CaptureHealthSnapshotAssembler");
        AssertContains(healthSnapshotAssemblerText, "public static CaptureHealthSnapshot Build(");
        AssertContains(healthSnapshotAssemblerText, "private readonly record struct CaptureHealthSnapshotAssemblyFields");
        AssertContains(healthSnapshotAssemblerText, "public CaptureCadenceHealthSnapshotFields CaptureCadence { get; init; }");
        AssertContains(healthSnapshotAssemblerText, "public FlashbackPlaybackHealthSnapshotFields FlashbackPlayback { get; init; }");
        AssertDoesNotContain(healthSnapshotText, "private readonly record struct CaptureHealthSnapshotAssemblyFields");
        AssertDoesNotContain(healthSnapshotAssemblerText, "private readonly record struct CaptureCadenceHealthSnapshotFields");
        AssertDoesNotContain(healthSnapshotAssemblerText, "private readonly record struct MjpegHealthSnapshotFields");
        AssertDoesNotContain(healthSnapshotAssemblerText, "LibAvRecordingSink? Sink");
        AssertDoesNotContain(healthSnapshotAssemblerText, "var sink = fields.Sink;");
        AssertDoesNotContain(healthSnapshotAssemblerText, "UnifiedVideoCapture? UnifiedVideoCapture");
        AssertDoesNotContain(healthSnapshotAssemblerText, "_sessionState");
        AssertDoesNotContain(healthSnapshotAssemblerText, "_isRecording");
        AssertDoesNotContain(healthSnapshotAssemblerText, "_currentSettings");
        AssertDoesNotContain(healthSnapshotAssemblerText, "ComputeTickAge(");
        AssertContains(healthSnapshotAssemblerText, "TimestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(snapshotUtcUnixMs),");
        AssertDoesNotContain(healthSnapshotText, "return new CaptureHealthSnapshot");
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.HealthSnapshotAssemblyFields.cs")));

    }

    [Fact]
    public void CaptureService_HealthSnapshotCaptureCadenceFields_LiveWithSampler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var captureCadence = BuildCaptureCadenceHealthSnapshotFields(unifiedVideoCapture);");
        AssertContains(healthSnapshotAssemblerText, "CaptureCadenceSampleCount = captureCadence.SampleCount,");
        AssertContains(healthSnapshotAssemblerText, "CaptureCadenceEstimatedDropPercent = captureCadence.EstimatedDropPercent,");
        AssertContains(healthSnapshotText, "private static CaptureCadenceHealthSnapshotFields BuildCaptureCadenceHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "private readonly record struct CaptureCadenceHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "unifiedVideoCapture?.GetSourceCadenceMetrics()");
        AssertContains(healthSnapshotText, "default(MfSourceReaderVideoCapture.SourceCadenceMetrics)");
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.HealthSnapshotCaptureCadence.cs")));

    }

    [Fact]
    public void CaptureService_HealthSnapshotMjpegFields_LiveWithSampler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var videoPipelineResourcesText = ReadRepoFile("Sussudio/Services/Capture/CapturePipelineResources.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var mjpegHealth = CaptureMjpegHealthSnapshotFields(unifiedVideoCapture);");
        AssertContains(healthSnapshotAssemblerText, "MjpegDecodeSampleCount = mjpegHealth.Timing.DecodeSampleCount,");
        AssertContains(healthSnapshotAssemblerText, "MjpegPreviewJitterEnabled = mjpegHealth.PreviewJitter.Enabled,");
        AssertContains(healthSnapshotAssemblerText, "VisualCadenceSampleCount = mjpegHealth.VisualCadence.SampleCount,");
        AssertContains(healthSnapshotAssemblerText, "MjpegPerDecoder = mjpegHealth.PerDecoder,");
        AssertContains(healthSnapshotText, "private MjpegHealthSnapshotFields CaptureMjpegHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "private readonly record struct MjpegHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "_videoPipeline.GetMjpegTimingSnapshot(unifiedVideoCapture)");
        AssertContains(videoPipelineResourcesText, "GetMjpegPipelineTimingSnapshot()");
        AssertContains(healthSnapshotText, "GetMjpegPreviewJitterMetrics()");
        AssertContains(healthSnapshotText, "GetPreviewVisualCadenceMetrics()");
        AssertContains(healthSnapshotText, "FrameFingerprintCadenceTracker.Empty");
        AssertContains(healthSnapshotText, "new MjpegDecoderHealthSnapshot(");
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.HealthSnapshotMjpeg.cs")));

    }

    [Fact]
    public void CaptureService_HealthSnapshotAvSyncFields_LiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var avSyncSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Snapshots.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var avSyncHealth = CaptureAvSyncHealthSnapshotFields();");
        AssertContains(healthSnapshotAssemblerText, "AvSyncCaptureDriftMs = avSyncHealth.CaptureDriftMs,");
        AssertContains(healthSnapshotAssemblerText, "AvSyncCaptureDriftRateMsPerSec = avSyncHealth.CaptureDriftRateMsPerSec,");
        AssertContains(healthSnapshotAssemblerText, "AvSyncEncoderCorrectionSamples = avSyncHealth.EncoderCorrectionSamples");
        AssertDoesNotContain(healthSnapshotText, "var (avSyncDriftMs, avSyncDriftRate) = ComputeAvSyncDrift();");
        AssertDoesNotContain(healthSnapshotText, "var (avSyncEncoderDriftMs, avSyncEncoderCorrectionSamples) = GetEncoderAvSyncDrift();");

        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.HealthSnapshots.AvSync.cs")));
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.SnapshotAvSync.cs")));
        AssertContains(avSyncSnapshotText, "private AvSyncHealthSnapshotFields CaptureAvSyncHealthSnapshotFields()");
        AssertContains(avSyncSnapshotText, "var (captureDriftMs, captureDriftRateMsPerSec) = ComputeAvSyncDrift();");
        AssertContains(avSyncSnapshotText, "var (encoderDriftMs, encoderCorrectionSamples) = GetEncoderAvSyncDrift();");
        AssertContains(avSyncSnapshotText, "private readonly record struct AvSyncHealthSnapshotFields");
        AssertContains(avSyncSnapshotText, "private double _avSyncBaselineDriftMs = double.NaN;");
        AssertContains(avSyncSnapshotText, "private double _avSyncPrevDriftMs;");
        AssertContains(avSyncSnapshotText, "private long _avSyncPrevDriftTick;");
        AssertContains(avSyncSnapshotText, "private double _avSyncDriftRateMsPerSec;");
        AssertContains(avSyncSnapshotText, "private void ResetAvSyncDriftBaseline()");
        AssertDoesNotContain(rootText, "_avSyncBaselineDriftMs");
        AssertDoesNotContain(rootText, "_avSyncPrevDriftMs");
        AssertDoesNotContain(rootText, "_avSyncPrevDriftTick");
        AssertDoesNotContain(rootText, "_avSyncDriftRateMsPerSec");

    }

    [Fact]
    public void CaptureService_HealthSnapshotFlashbackExportFields_LiveWithExportDiagnostics()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var flashbackExportText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportCore.cs")
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
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.HealthSnapshotFlashbackExport.cs")));

    }

    [Fact]
    public void CaptureService_HealthSnapshotFlashbackBufferFields_LiveWithHealthSampler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var flashbackBuffer = CaptureFlashbackBufferHealthSnapshotFields(");
        AssertContains(healthSnapshotAssemblerText, "FlashbackBufferedDurationMs = flashbackBuffer.BufferedDurationMs,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackBackendSettingsStaleReason = flashbackBuffer.BackendSettingsStaleReason,");
        AssertContains(healthSnapshotAssemblerText, "EncoderTargetBitRate = flashbackBuffer.EncoderTargetBitRate,");
        AssertContains(healthSnapshotText, "private FlashbackBufferHealthSnapshotFields CaptureFlashbackBufferHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "ResolveFlashbackBackendSettingsStaleReason(flashbackBackendSettings, currentSettings)");
        AssertContains(healthSnapshotText, "private static string ResolveFlashbackBackendSettingsStaleReason(");
        AssertContains(healthSnapshotText, "bufMgr?.StartupCacheOverBudget ?? false");
        AssertContains(healthSnapshotText, "fbSink?.EncoderFrameRateDenominator");
        AssertContains(healthSnapshotText, "private readonly record struct FlashbackBufferHealthSnapshotFields");
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.HealthSnapshotFlashbackBackend.cs")));

    }

    [Fact]
    public void CaptureService_HealthSnapshotFlashbackQueueFields_LiveWithHealthSampler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var flashbackQueues = CaptureFlashbackQueueHealthSnapshotFields(");
        AssertContains(healthSnapshotAssemblerText, "FlashbackVideoQueueDepth = flashbackQueues.VideoQueueDepth,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackForceRotateActive = flashbackQueues.ForceRotateActive,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackVideoQueueLatencyP99Ms = flashbackQueues.VideoQueueLatencyMetrics.P99Ms,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackGpuQueueLastRejectReason = flashbackQueues.GpuQueueLastRejectReason,");
        AssertContains(healthSnapshotText, "private static FlashbackQueueHealthSnapshotFields CaptureFlashbackQueueHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "fbSink?.VideoQueueOldestFrameAgeMs ?? 0");
        AssertContains(healthSnapshotText, "fbSink?.IsForceRotateActive ?? false");
        AssertContains(healthSnapshotText, "fbSink?.LastGpuQueueRejectReason ?? string.Empty");
        AssertContains(healthSnapshotText, "private readonly record struct FlashbackQueueHealthSnapshotFields");
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.HealthSnapshotFlashbackBackend.cs")));

    }

    [Fact]
    public void CaptureService_HealthSnapshotFlashbackPlaybackFields_LiveWithHealthSampler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var flashbackPlayback = CaptureFlashbackPlaybackHealthSnapshotFields(fbPlayback);");
        AssertContains(healthSnapshotAssemblerText, "FlashbackPlaybackState = flashbackPlayback.State,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackPlaybackDecodeP95Ms = flashbackPlayback.DecodeP95Ms,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackPlaybackLastCommandFailure = flashbackPlayback.LastCommandFailure,");
        AssertContains(healthSnapshotText, "private static FlashbackPlaybackHealthSnapshotFields CaptureFlashbackPlaybackHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "var state = CaptureFlashbackPlaybackStateHealthSnapshotFields(fbPlayback);");
        AssertContains(healthSnapshotText, "var cadence = CaptureFlashbackPlaybackCadenceHealthSnapshotFields(fbPlayback);");
        AssertContains(healthSnapshotText, "var decode = CaptureFlashbackPlaybackDecodeHealthSnapshotFields(fbPlayback);");
        AssertContains(healthSnapshotText, "var audioMaster = CaptureFlashbackPlaybackAudioMasterHealthSnapshotFields(fbPlayback);");
        AssertContains(healthSnapshotText, "var commands = CaptureFlashbackPlaybackCommandHealthSnapshotFields(fbPlayback);");
        AssertContains(healthSnapshotText, "private readonly record struct FlashbackPlaybackHealthSnapshotFields");
        AssertContains(healthSnapshotText, "private static FlashbackPlaybackStateHealthSnapshotFields CaptureFlashbackPlaybackStateHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "private readonly record struct FlashbackPlaybackStateHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "fbPlayback?.State.ToString() ?? \"N/A\"");
        AssertContains(healthSnapshotText, "fbPlayback?.PlaybackFrameCount ?? 0");
        AssertContains(healthSnapshotText, "fbPlayback?.PlaybackThreadAlive ?? false");
        AssertContains(healthSnapshotText, "private static FlashbackPlaybackCadenceHealthSnapshotFields CaptureFlashbackPlaybackCadenceHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "private readonly record struct FlashbackPlaybackCadenceHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "fbPlayback?.GetPlaybackCadenceMetrics() ?? default");
        AssertContains(healthSnapshotText, "private static FlashbackPlaybackDecodeHealthSnapshotFields CaptureFlashbackPlaybackDecodeHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "private readonly record struct FlashbackPlaybackDecodeHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "fbPlayback?.GetPlaybackDecodeMetrics() ?? default");
        AssertContains(healthSnapshotText, "fbPlayback?.PlaybackMaxDecodePhase ?? string.Empty");
        AssertContains(healthSnapshotText, "private static FlashbackPlaybackAudioMasterHealthSnapshotFields CaptureFlashbackPlaybackAudioMasterHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "private readonly record struct FlashbackPlaybackAudioMasterHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "fbPlayback?.PlaybackAudioMasterFallbacks ?? 0");
        AssertContains(healthSnapshotText, "private static FlashbackPlaybackCommandHealthSnapshotFields CaptureFlashbackPlaybackCommandHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "private readonly record struct FlashbackPlaybackCommandHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "fbPlayback?.CommandsEnqueued ?? 0");
        AssertContains(healthSnapshotText, "double[] RecentFrameIntervalsMs");
        AssertContains(healthSnapshotText, "string LastCommandFailure");
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.HealthSnapshotFlashbackPlayback.State.cs")));
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.HealthSnapshotFlashbackPlayback.cs")));

    }

    [Fact]
    public void CaptureService_HealthSnapshotRecordingFields_LiveWithHealthSampler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
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
        AssertContains(healthSnapshotText, "private RecordingHealthSnapshotFields CaptureRecordingHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "GetLastFailureTelemetry()");
        AssertContains(healthSnapshotText, "IsFlashbackRecordingBackendOwnedByRecording()");
        AssertContains(healthSnapshotText, "CaptureActiveRecordingBackendHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "activeRecording.FlashbackVideoQueueLatencyMetrics");
        AssertContains(healthSnapshotText, "sink?.CudaQueueCount ?? 0");
        AssertContains(healthSnapshotText, "sink?.CudaQueueCapacityFrames ?? 0");
        AssertContains(healthSnapshotText, "sink?.CudaQueueMaxDepth ?? 0");
        AssertContains(healthSnapshotText, "sink?.CudaFramesEnqueued ?? 0");
        AssertContains(healthSnapshotText, "sink?.CudaFramesDropped ?? 0");
        AssertContains(healthSnapshotText, "int CudaQueueDepth");
        AssertContains(healthSnapshotText, "long CudaFramesDropped");
        AssertContains(healthSnapshotText, "private readonly record struct RecordingHealthSnapshotFields");
        AssertContains(healthSnapshotText, "private ActiveRecordingBackendHealthSnapshotFields CaptureActiveRecordingBackendHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "var flashbackVideoQueueLatencyMetrics");
        AssertContains(healthSnapshotText, "sink?.VideoQueueLatencyMetrics ??");
        AssertContains(healthSnapshotText, "flashbackIsRecordingBackend ? fbSink?.VideoQueueCount ?? 0 : 0");
        AssertContains(healthSnapshotText, "Interlocked.Read(ref _videoFramesDropped)");
        AssertContains(healthSnapshotText, "private readonly record struct ActiveRecordingBackendHealthSnapshotFields");
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.HealthSnapshotRecordingActiveBackend.cs")));
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.HealthSnapshotRecording.cs")));

    }

    [Fact]
    public void CaptureService_HealthSnapshotSourceTelemetryFields_LiveWithHealthSampler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var sourceTelemetry = CaptureSourceTelemetryHealthSnapshotFields(_latestSourceTelemetry);");
        AssertContains(healthSnapshotAssemblerText, "SourceTelemetryAvailability = sourceTelemetry.Availability,");
        AssertContains(healthSnapshotAssemblerText, "SourceTelemetryBackend = sourceTelemetry.Backend,");
        AssertContains(healthSnapshotAssemblerText, "SourceTelemetryCircuitState = sourceTelemetry.CircuitState,");
        AssertContains(healthSnapshotText, "private SourceTelemetryHealthSnapshotFields CaptureSourceTelemetryHealthSnapshotFields(");
        AssertContains(healthSnapshotText, "ResolveSourceTelemetrySuppressedReason(telemetry) ?? string.Empty");
        AssertContains(healthSnapshotText, "ResolveSourceTelemetryBackend(telemetry)");
        AssertContains(healthSnapshotText, "ResolveSourceTelemetryCircuitState(telemetry.Availability, suppressed)");
        AssertContains(healthSnapshotText, "private readonly record struct SourceTelemetryHealthSnapshotFields");
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.HealthSnapshotSourceTelemetry.cs")));

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
    internal static async Task CaptureHealthSnapshot_PropagatesStructuredSourceTelemetryDetails()
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
