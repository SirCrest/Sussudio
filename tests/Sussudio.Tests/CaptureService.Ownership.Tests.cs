using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public sealed class CaptureServiceHealthSnapshotOwnershipTests
{
    [Fact]
    public void CaptureService_HealthSnapshotAssemblyFields_LiveWithHealthSampler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var getHealthSnapshotText = ExtractMemberCode(healthSnapshotText, "GetHealthSnapshot");
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");

        AssertContains(healthSnapshotText, "return CaptureHealthSnapshotAssembler.Build(new CaptureHealthSnapshotAssemblyFields");
        AssertContains(healthSnapshotText, "SessionState = CurrentSessionState,");
        AssertContains(healthSnapshotText, "FlashbackExportVerificationFormat = ResolveFlashbackExportVerificationFormat(currentSettings, unifiedVideoCapture),");
        AssertContains(healthSnapshotText, "LastFrameArrivalMs = ComputeTickAge(unifiedVideoCapture?.LastVideoFrameArrivedTick ?? 0),");
        AssertContains(healthSnapshotText, "private static class CaptureHealthSnapshotAssembler");
        AssertContains(healthSnapshotAssemblerText, "public static CaptureHealthSnapshot Build(");
        AssertContains(healthSnapshotText, "private readonly record struct CaptureHealthSnapshotAssemblyFields");
        AssertContains(healthSnapshotText, "public CaptureCadenceHealthSnapshotFields CaptureCadence { get; init; }");
        AssertContains(healthSnapshotText, "public FlashbackPlaybackHealthSnapshotFields FlashbackPlayback { get; init; }");
        AssertDoesNotContain(healthSnapshotAssemblerText, "LibAvRecordingSink? Sink");
        AssertDoesNotContain(healthSnapshotAssemblerText, "var sink = fields.Sink;");
        AssertDoesNotContain(healthSnapshotAssemblerText, "UnifiedVideoCapture? UnifiedVideoCapture");
        AssertDoesNotContain(healthSnapshotAssemblerText, "_sessionState");
        AssertDoesNotContain(healthSnapshotAssemblerText, "_isRecording");
        AssertDoesNotContain(healthSnapshotAssemblerText, "_currentSettings");
        AssertDoesNotContain(healthSnapshotAssemblerText, "ComputeTickAge(");
        AssertContains(healthSnapshotAssemblerText, "TimestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(snapshotUtcUnixMs),");
        AssertDoesNotContain(getHealthSnapshotText, "return new CaptureHealthSnapshot");
        Assert.False(File.Exists(Path.Combine(
            FindRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.HealthSnapshotAssembler.cs")));
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
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");

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
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");
        var videoPipelineResourcesText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
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
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");
        var avSyncSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
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
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");
        var flashbackExportText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs")
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
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");

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
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");

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
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");

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
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");

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
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");

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

    private static string ExtractMemberCode(string source, string memberName)
    {
        var signatureIndex = source.IndexOf($" {memberName}(", StringComparison.Ordinal);
        if (signatureIndex < 0)
        {
            throw new InvalidOperationException($"Member '{memberName}' was not found.");
        }

        var lineStart = source.LastIndexOf('\n', signatureIndex);
        var start = lineStart < 0 ? 0 : lineStart + 1;
        var braceStart = source.IndexOf('{', signatureIndex);
        if (braceStart < 0)
        {
            throw new InvalidOperationException($"Member '{memberName}' did not have a body.");
        }

        var depth = 0;
        for (var i = braceStart; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source[start..(i + 1)];
                }
            }
        }

        throw new InvalidOperationException($"Member '{memberName}' body was not closed.");
    }

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

public sealed class CaptureServiceLifecycleOwnershipTests
{
    [Fact]
    public void CaptureService_LastFailureTelemetryState_LivesWithCleanupLifecycle()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var cleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");

        var fieldNames = new[]
        {
            "_recordingFailureTelemetryLock",
            "_lastRecordingEncodingFailed",
            "_lastRecordingEncodingFailureType",
            "_lastRecordingEncodingFailureMessage",
            "_lastFlashbackEncodingFailed",
            "_lastFlashbackEncodingFailureType",
            "_lastFlashbackEncodingFailureMessage",
        };

        foreach (var fieldName in fieldNames)
        {
            AssertContains(rootText, fieldName);
            AssertContains(cleanupText, fieldName);
        }

        AssertContains(cleanupText, "private readonly object _recordingFailureTelemetryLock = new();");
        AssertContains(cleanupText, "private bool _lastRecordingEncodingFailed;");
        AssertContains(cleanupText, "private string? _lastRecordingEncodingFailureType;");
        AssertContains(cleanupText, "private string? _lastRecordingEncodingFailureMessage;");
        AssertContains(cleanupText, "private bool _lastFlashbackEncodingFailed;");
        AssertContains(cleanupText, "private string? _lastFlashbackEncodingFailureType;");
        AssertContains(cleanupText, "private string? _lastFlashbackEncodingFailureMessage;");
        AssertContains(cleanupText, "private void RecordLastRecordingFailure(Exception ex)");
        AssertContains(cleanupText, "private void RecordLastFlashbackFailure(Exception ex)");
        AssertContains(cleanupText, "private void ClearLastRecordingFailure()");
        AssertContains(cleanupText, "private void ClearLastFlashbackFailure()");
        AssertContains(cleanupText, "private void BeginFatalCaptureCleanup(Exception ex)");
        AssertContains(cleanupText, "EnterCleanupState();");
        AssertContains(cleanupText, "EnterFaultedState();");
        AssertContains(cleanupText, "GetLastFailureTelemetry()");
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Failures.cs")),
            "fatal failure cleanup folded into CaptureService.cs");
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Cleanup.cs")),
            "cleanup lifecycle folded into CaptureService.cs");
    }

    [Fact]
    public void CaptureService_FlashbackBackendFailureCleanup_LivesWithCleanupLifecycleWithoutSessionStateWrites()
    {
        var cleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");

        AssertContains(cleanupText, "private void BeginFatalCaptureCleanup(Exception ex)");
        AssertContains(cleanupText, "private void BeginFlashbackBackendCleanup(Exception ex)");
        AssertContains(cleanupText, "private static bool IsGpuDeviceLost(Exception ex)");
        AssertContains(cleanupText, "_flashbackBackend.PreserveRecoverySegments(\"device_lost\");");
        AssertDoesNotContain(cleanupText, "_sessionState =");
        Assert.False(
            File.Exists(Path.Combine(FindRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackBackendFailureCleanup.cs")),
            "Flashback backend failure cleanup folded into CaptureService.cs");
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

    internal static async Task GetHealthSnapshot_UsesCachedMjpegTimingMetricsWhenCaptureIsGone()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var videoPipeline = GetPrivateField(captureService, "_videoPipeline")
            ?? throw new InvalidOperationException("CaptureService video pipeline resources were missing.");
        SetPrivateField(
            videoPipeline,
            "<LastMjpegPipelineTimingMetrics>k__BackingField",
            CreateMjpegTimingMetrics(
                decodeSampleCount: 7,
                decodeAvgMs: 1.5,
                decodeP95Ms: 2.5,
                decodeMaxMs: 3.5,
                interopCopySampleCount: 5,
                interopCopyAvgMs: 4.5,
                interopCopyP95Ms: 5.5,
                interopCopyMaxMs: 6.5,
                callbackSampleCount: 9,
                callbackAvgMs: 7.5,
                callbackP95Ms: 8.5,
                callbackMaxMs: 9.5));
        SetPrivateField(
            videoPipeline,
            "<LastFullMjpegPipelineTimingMetrics>k__BackingField",
            CreateFullMjpegPipelineTimingMetrics(
                decoderCount: 3,
                decodeSampleCount: 17,
                decodeAvgMs: 4.1,
                decodeP95Ms: 4.6,
                decodeMaxMs: 5.2,
                reorderSampleCount: 19,
                reorderAvgMs: 0.7,
                reorderP95Ms: 1.1,
                reorderMaxMs: 1.8,
                pipelineSampleCount: 23,
                pipelineAvgMs: 5.1,
                pipelineP95Ms: 5.7,
                pipelineMaxMs: 6.4,
                totalDecoded: 101,
                totalEmitted: 97,
                totalDropped: 4,
                reorderSkips: 2,
                reorderBufferDepth: 1,
                perDecoder: new[]
                {
                    CreatePerDecoderMetrics(0, 31, 4.0, 4.4, 4.9),
                    CreatePerDecoderMetrics(1, 33, 4.2, 4.7, 5.3),
                    CreatePerDecoderMetrics(2, 35, 4.1, 4.8, 5.4)
                }));
        SetPropertyOrBackingField(videoPipeline, "Capture", null);

        var snapshot = InvokeInstanceMethod(captureService, "GetHealthSnapshot");
        AssertEqual(7L, GetLongProperty(snapshot, "MjpegDecodeSampleCount"), "MjpegDecodeSampleCount");
        AssertEqual(5L, GetLongProperty(snapshot, "MjpegInteropCopySampleCount"), "MjpegInteropCopySampleCount");
        AssertEqual(9L, GetLongProperty(snapshot, "MjpegCallbackSampleCount"), "MjpegCallbackSampleCount");
        AssertEqual(1.5, GetDoubleProperty(snapshot, "MjpegDecodeAvgMs"), "MjpegDecodeAvgMs");
        AssertEqual(8.5, GetDoubleProperty(snapshot, "MjpegCallbackP95Ms"), "MjpegCallbackP95Ms");
        AssertEqual(3L, GetLongProperty(snapshot, "MjpegDecoderCount"), "MjpegDecoderCount");
        AssertEqual(19L, GetLongProperty(snapshot, "MjpegReorderSampleCount"), "MjpegReorderSampleCount");
        AssertEqual(23L, GetLongProperty(snapshot, "MjpegPipelineSampleCount"), "MjpegPipelineSampleCount");
        AssertEqual(101L, GetLongProperty(snapshot, "MjpegTotalDecoded"), "MjpegTotalDecoded");
        AssertEqual(97L, GetLongProperty(snapshot, "MjpegTotalEmitted"), "MjpegTotalEmitted");
        AssertEqual(4L, GetLongProperty(snapshot, "MjpegTotalDropped"), "MjpegTotalDropped");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegCompressedFramesQueued"), "MjpegCompressedFramesQueued");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegCompressedDropsQueueFull"), "MjpegCompressedDropsQueueFull");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegCompressedDropsDisposed"), "MjpegCompressedDropsDisposed");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegDecodeFailures"), "MjpegDecodeFailures");
        AssertEqual(2L, GetLongProperty(snapshot, "MjpegReorderSkips"), "MjpegReorderSkips");
        AssertEqual(1L, GetLongProperty(snapshot, "MjpegReorderBufferDepth"), "MjpegReorderBufferDepth");
        AssertEqual(0.7, GetDoubleProperty(snapshot, "MjpegReorderAvgMs"), "MjpegReorderAvgMs");
        AssertEqual(5.7, GetDoubleProperty(snapshot, "MjpegPipelineP95Ms"), "MjpegPipelineP95Ms");

        var perDecoder = GetPropertyValue(snapshot, "MjpegPerDecoder") as Array
            ?? throw new InvalidOperationException("MjpegPerDecoder was not an array.");
        AssertEqual(3, perDecoder.Length, "MjpegPerDecoder.Length");
        AssertEqual(1, GetIntProperty(perDecoder.GetValue(1)!, "WorkerIndex"), "MjpegPerDecoder[1].WorkerIndex");
        AssertEqual(33L, GetLongProperty(perDecoder.GetValue(1)!, "SampleCount"), "MjpegPerDecoder[1].SampleCount");
        AssertEqual(4.8, GetDoubleProperty(perDecoder.GetValue(2)!, "P95Ms"), "MjpegPerDecoder[2].P95Ms");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    internal static async Task GetDiagnosticsSnapshot_PropagatesMjpegTimingMetrics()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        var videoPipeline = GetPrivateField(captureService, "_videoPipeline")
            ?? throw new InvalidOperationException("CaptureService video pipeline resources were missing.");
        SetPrivateField(
            videoPipeline,
            "<LastMjpegPipelineTimingMetrics>k__BackingField",
            CreateMjpegTimingMetrics(
                decodeSampleCount: 11,
                decodeAvgMs: 10.1,
                decodeP95Ms: 10.2,
                decodeMaxMs: 10.3,
                interopCopySampleCount: 12,
                interopCopyAvgMs: 11.1,
                interopCopyP95Ms: 11.2,
                interopCopyMaxMs: 11.3,
                callbackSampleCount: 13,
                callbackAvgMs: 12.1,
                callbackP95Ms: 12.2,
                callbackMaxMs: 12.3));
        SetPrivateField(
            videoPipeline,
            "<LastFullMjpegPipelineTimingMetrics>k__BackingField",
            CreateFullMjpegPipelineTimingMetrics(
                decoderCount: 4,
                decodeSampleCount: 40,
                decodeAvgMs: 6.1,
                decodeP95Ms: 7.1,
                decodeMaxMs: 8.2,
                reorderSampleCount: 41,
                reorderAvgMs: 1.2,
                reorderP95Ms: 1.9,
                reorderMaxMs: 2.8,
                pipelineSampleCount: 42,
                pipelineAvgMs: 7.4,
                pipelineP95Ms: 8.6,
                pipelineMaxMs: 9.9,
                totalDecoded: 400,
                totalEmitted: 390,
                totalDropped: 10,
                reorderSkips: 3,
                reorderBufferDepth: 2,
                perDecoder: new[]
                {
                    CreatePerDecoderMetrics(0, 100, 5.8, 6.7, 7.8),
                    CreatePerDecoderMetrics(1, 101, 6.0, 7.0, 8.0),
                    CreatePerDecoderMetrics(2, 99, 6.2, 7.2, 8.3),
                    CreatePerDecoderMetrics(3, 100, 6.4, 7.4, 8.5)
                }));
        SetPropertyOrBackingField(videoPipeline, "Capture", null);

        var snapshot = InvokeInstanceMethod(captureService, "GetDiagnosticsSnapshot");
        AssertEqual(11L, GetLongProperty(snapshot, "MjpegDecodeSampleCount"), "MjpegDecodeSampleCount");
        AssertEqual(12L, GetLongProperty(snapshot, "MjpegInteropCopySampleCount"), "MjpegInteropCopySampleCount");
        AssertEqual(13L, GetLongProperty(snapshot, "MjpegCallbackSampleCount"), "MjpegCallbackSampleCount");
        AssertEqual(10.2, GetDoubleProperty(snapshot, "MjpegDecodeP95Ms"), "MjpegDecodeP95Ms");
        AssertEqual(12.3, GetDoubleProperty(snapshot, "MjpegCallbackMaxMs"), "MjpegCallbackMaxMs");
        AssertEqual(4L, GetLongProperty(snapshot, "MjpegDecoderCount"), "MjpegDecoderCount");
        AssertEqual(41L, GetLongProperty(snapshot, "MjpegReorderSampleCount"), "MjpegReorderSampleCount");
        AssertEqual(42L, GetLongProperty(snapshot, "MjpegPipelineSampleCount"), "MjpegPipelineSampleCount");
        AssertEqual(400L, GetLongProperty(snapshot, "MjpegTotalDecoded"), "MjpegTotalDecoded");
        AssertEqual(390L, GetLongProperty(snapshot, "MjpegTotalEmitted"), "MjpegTotalEmitted");
        AssertEqual(10L, GetLongProperty(snapshot, "MjpegTotalDropped"), "MjpegTotalDropped");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegCompressedFramesDequeued"), "MjpegCompressedFramesDequeued");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegCompressedDropsByteBudget"), "MjpegCompressedDropsByteBudget");
        AssertEqual(0L, GetLongProperty(snapshot, "MjpegEmitFailures"), "MjpegEmitFailures");
        AssertEqual(3L, GetLongProperty(snapshot, "MjpegReorderSkips"), "MjpegReorderSkips");
        AssertEqual(2L, GetLongProperty(snapshot, "MjpegReorderBufferDepth"), "MjpegReorderBufferDepth");
        AssertEqual(7.4, GetDoubleProperty(snapshot, "MjpegPipelineAvgMs"), "MjpegPipelineAvgMs");

        var perDecoder = GetPropertyValue(snapshot, "MjpegPerDecoder") as Array
            ?? throw new InvalidOperationException("MjpegPerDecoder was not an array.");
        AssertEqual(4, perDecoder.Length, "MjpegPerDecoder.Length");
        AssertEqual(99L, GetLongProperty(perDecoder.GetValue(2)!, "SampleCount"), "MjpegPerDecoder[2].SampleCount");
        AssertEqual(8.5, GetDoubleProperty(perDecoder.GetValue(3)!, "MaxMs"), "MjpegPerDecoder[3].MaxMs");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    private static object CreateMjpegTimingMetrics(
        int decodeSampleCount,
        double decodeAvgMs,
        double decodeP95Ms,
        double decodeMaxMs,
        int interopCopySampleCount,
        double interopCopyAvgMs,
        double interopCopyP95Ms,
        double interopCopyMaxMs,
        int callbackSampleCount,
        double callbackAvgMs,
        double callbackP95Ms,
        double callbackMaxMs)
    {
        var type = RequireType("Sussudio.Services.Capture.UnifiedVideoCapture+MjpegPipelineTimingMetrics");
        return Activator.CreateInstance(
                   type,
                   decodeSampleCount,
                   decodeAvgMs,
                   decodeP95Ms,
                   decodeMaxMs,
                   interopCopySampleCount,
                   interopCopyAvgMs,
                   interopCopyP95Ms,
                   interopCopyMaxMs,
                   callbackSampleCount,
                   callbackAvgMs,
                   callbackP95Ms,
                   callbackMaxMs)
               ?? throw new InvalidOperationException("Failed to create MjpegPipelineTimingMetrics.");
    }

    private static object CreateFullMjpegPipelineTimingMetrics(
        int decoderCount,
        int decodeSampleCount,
        double decodeAvgMs,
        double decodeP95Ms,
        double decodeMaxMs,
        int reorderSampleCount,
        double reorderAvgMs,
        double reorderP95Ms,
        double reorderMaxMs,
        int pipelineSampleCount,
        double pipelineAvgMs,
        double pipelineP95Ms,
        double pipelineMaxMs,
        long totalDecoded,
        long totalEmitted,
        long totalDropped,
        long reorderSkips,
        int reorderBufferDepth,
        object[] perDecoder,
        long compressedFramesQueued = 0,
        long compressedFramesDequeued = 0,
        long compressedDropsQueueFull = 0,
        long compressedDropsByteBudget = 0,
        long compressedDropsDisposed = 0,
        long decodeFailures = 0,
        long reorderCollisions = 0,
        long emitFailures = 0,
        int compressedQueueDepth = 0,
        long compressedQueueBytes = 0,
        long compressedQueueByteBudget = 0)
    {
        var type = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline+PipelineTimingMetrics");
        var perDecoderArray = Array.CreateInstance(
            RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline+PerDecoderMetrics"),
            perDecoder.Length);
        for (var i = 0; i < perDecoder.Length; i++)
        {
            perDecoderArray.SetValue(perDecoder[i], i);
        }

        return Activator.CreateInstance(
                   type,
                   decoderCount,
                   decodeSampleCount,
                   decodeAvgMs,
                   decodeP95Ms,
                   decodeMaxMs,
                   reorderSampleCount,
                   reorderAvgMs,
                   reorderP95Ms,
                   reorderMaxMs,
                   pipelineSampleCount,
                   pipelineAvgMs,
                   pipelineP95Ms,
                   pipelineMaxMs,
                   totalDecoded,
                   totalEmitted,
                   totalDropped,
                   compressedFramesQueued,
                   compressedFramesDequeued,
                   compressedDropsQueueFull,
                   compressedDropsByteBudget,
                   compressedDropsDisposed,
                   decodeFailures,
                   reorderCollisions,
                   emitFailures,
                   compressedQueueDepth,
                   compressedQueueBytes,
                   compressedQueueByteBudget,
                   reorderSkips,
                   reorderBufferDepth,
                   perDecoderArray)
               ?? throw new InvalidOperationException("Failed to create full MJPEG pipeline timing metrics.");
    }

    private static object CreatePerDecoderMetrics(
        int workerIndex,
        int sampleCount,
        double avgMs,
        double p95Ms,
        double maxMs)
    {
        var type = RequireType("Sussudio.Services.Gpu.ParallelMjpegDecodePipeline+PerDecoderMetrics");
        return Activator.CreateInstance(type, workerIndex, sampleCount, avgMs, p95Ms, maxMs)
               ?? throw new InvalidOperationException("Failed to create per-decoder MJPEG metrics.");
    }

    internal static Task CaptureService_InitializationLivesWithServiceRoot()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var telemetryText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RuntimeSnapshots.cs")
            .Replace("\r\n", "\n");

        AssertContains(rootText, "private static ISourceSignalTelemetryProvider CreateDefaultTelemetryProvider()");
        AssertContains(rootText, "public Task InitializeAsync(CaptureDevice device, CaptureSettings settings, CancellationToken cancellationToken = default)");
        AssertContains(rootText, "=> RunTransitionAsync(CaptureSessionState.Initializing, async transitionToken =>");
        AssertContains(rootText, "_audioDeviceId = settings.UseCustomAudioInput ? settings.AudioDeviceId : device.AudioDeviceId;");
        AssertContains(rootText, "_actualPixelFormat = settings.RequestedPixelFormat ?? (settings.HdrEnabled ? \"P010\" : \"NV12\");");
        AssertContains(rootText, "ResetObservedPixelTelemetry();");
        AssertContains(rootText, "ResetCachedMjpegTimingMetrics();");
        AssertContains(rootText, "_latestSourceTelemetry = BuildFallbackTelemetry();");
        AssertContains(rootText, "await RefreshSourceTelemetryAsync(transitionToken).ConfigureAwait(false);");
        AssertContains(rootText, "TryCorrectFrameRateFromTelemetry();");
        AssertContains(rootText, "StatusChanged?.Invoke(this, \"Initialized\");");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Initialization.cs")),
            "old initialization partial removed");
        AssertContains(telemetryText, "private SourceSignalTelemetrySnapshot BuildFallbackTelemetry()");
        AssertContains(telemetryText, "private static SourceSignalTelemetrySnapshot MergeTelemetryWithFallback(");
        AssertContains(telemetryText, "private void TryCorrectFrameRateFromTelemetry()");
        AssertContains(telemetryText, "private static string ResolveFrameRateArg(");
        AssertContains(telemetryText, "private void CaptureEncoderRuntimeTelemetry(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.TelemetryFallback.cs")),
            "old telemetry fallback partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.CaptureFormatTelemetry.cs")),
            "old capture-format telemetry partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Telemetry.cs")),
            "source telemetry polling folded into CaptureService.RuntimeSnapshots.cs");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_SessionStateWritesRouteThroughCoordination()
    {
        var captureServiceFiles = Directory
            .GetFiles(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture"), "CaptureService*.cs")
            .Select(path => new
            {
                FileName = Path.GetFileName(path),
                RelativePath = Path.GetRelativePath(GetRepoRoot(), path).Replace('\\', '/')
            })
            .ToArray();

        var directWriterCount = captureServiceFiles.Sum(file => Regex.Matches(
            ReadRepoCodeWithoutCommentsOrStrings(file.RelativePath),
            @"\b_sessionState\s*=").Count);

        AssertEqual(0, directWriterCount, "CaptureService direct _sessionState writer count");

        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs").Replace("\r\n", "\n");
        var transitionExecutionText = rootText;
        var stateMachineText = ReadRepoFile("Sussudio/Models/Capture/CaptureModels.cs").Replace("\r\n", "\n");
        var cleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs").Replace("\r\n", "\n");
        var resourceReleaseText = cleanupText;
        var failureCleanupText = cleanupText;

        AssertContains(rootText, "private readonly CaptureSessionStateMachine _sessionStateMachine = new();");
        AssertContains(rootText, "public CaptureSessionState SessionState => CurrentSessionState;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.TransitionExecution.cs")),
            "CaptureService transition transaction helpers stay folded into CaptureService.cs");
        AssertContains(transitionExecutionText, "private async Task RunTransitionAsync(");
        AssertContains(transitionExecutionText, "await _sessionTransitionLock.WaitAsync(cancellationToken).ConfigureAwait(false);");
        AssertContains(transitionExecutionText, "ReleaseSemaphoreBestEffort(_sessionTransitionLock, \"session_transition\");");
        AssertContains(transitionExecutionText, "private void EnterTransitionState(CaptureSessionState transitionState)");
        AssertContains(transitionExecutionText, "=> _sessionStateMachine.EnterTransition(transitionState);");
        AssertContains(transitionExecutionText, "private void ResolveSessionSteadyState()");
        AssertContains(transitionExecutionText, "=> _sessionStateMachine.ResolveSteadyState(BuildSteadyStateInputs());");
        AssertContains(transitionExecutionText, "private CaptureSessionState CurrentSessionState");
        AssertContains(transitionExecutionText, "=> _sessionStateMachine.State;");
        AssertContains(transitionExecutionText, "private long CurrentSessionGeneration");
        AssertContains(transitionExecutionText, "=> _sessionStateMachine.Generation;");
        AssertContains(transitionExecutionText, "private CaptureSessionSteadyStateInputs BuildSteadyStateInputs()");
        AssertContains(transitionExecutionText, "private void EnterCleanupState()");
        AssertContains(transitionExecutionText, "=> _sessionStateMachine.EnterCleanup();");
        AssertContains(transitionExecutionText, "private void EnterFaultedState()");
        AssertContains(transitionExecutionText, "=> _sessionStateMachine.EnterFaulted();");
        AssertContains(transitionExecutionText, "private void EnterDisposedState()");
        AssertContains(transitionExecutionText, "=> _sessionStateMachine.EnterDisposed();");
        AssertContains(transitionExecutionText, "private void ResetSessionStateAfterCleanup()");
        AssertContains(transitionExecutionText, "=> _sessionStateMachine.ResetAfterCleanup(_isDisposed != 0);");
        AssertContains(stateMachineText, "internal sealed class CaptureSessionStateMachine");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionStateMachine.cs")),
            "mutable capture session state machine lives with capture model owner");
        AssertContains(stateMachineText, "private CaptureSessionState _state = CaptureSessionState.Uninitialized;");
        AssertContains(stateMachineText, "private long _generation;");
        AssertContains(stateMachineText, "public long Generation => Interlocked.Read(ref _generation);");
        AssertContains(stateMachineText, "public void EnterTransition(CaptureSessionState transitionState)");
        AssertContains(stateMachineText, "CaptureSessionTransitionPolicy.ThrowIfDisallowed(_state, transitionState);");
        AssertContains(stateMachineText, "Interlocked.Increment(ref _generation);");
        AssertContains(stateMachineText, "_state = transitionState;");
        AssertContains(stateMachineText, "public void ResolveSteadyState(CaptureSessionSteadyStateInputs inputs)");
        AssertContains(stateMachineText, "=> _state = CaptureSessionTransitionPolicy.ResolveSteadyState(");
        AssertContains(stateMachineText, "public void ResetAfterCleanup(bool isDisposed)");
        AssertContains(stateMachineText, "=> _state = isDisposed ? CaptureSessionState.Disposed : CaptureSessionState.Uninitialized;");
        AssertOccursBefore(
            stateMachineText,
            "CaptureSessionTransitionPolicy.ThrowIfDisallowed(_state, transitionState);",
            "_state = transitionState;");
        AssertContains(cleanupText, "private async Task CleanupForDisposalAsync()");
        AssertContains(cleanupText, "EnterCleanupState();");
        AssertContains(cleanupText, "await CleanupCoreAsync(CancellationToken.None).ConfigureAwait(false);");
        AssertContains(cleanupText, "public void Dispose()");
        AssertContains(cleanupText, "public async ValueTask DisposeAsync()");
        AssertEqual(false, File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.ResourceRelease.cs")), "CaptureService resource-release helpers stay folded into CaptureService.cs");
        AssertContains(resourceReleaseText, "private void DisposeCoordinationLocksBestEffort()");
        AssertContains(resourceReleaseText, "private static void DisposeSemaphoreBestEffort(SemaphoreSlim semaphore, string operation)");
        AssertContains(resourceReleaseText, "private static void ReleaseSemaphoreBestEffort(SemaphoreSlim semaphore, string operation)");
        AssertContains(resourceReleaseText, "private void ReleaseFlashbackBackendLeaseIfHeld(ref bool backendLeaseHeld)");
        AssertContains(resourceReleaseText, "private void ReleaseFlashbackExportOperationLockIfHeld(ref bool exportOperationLockHeld)");
        AssertContains(resourceReleaseText, "private static void ResumeFlashbackEvictionBestEffort(FlashbackBufferManager? bufferManager, string operation)");
        AssertContains(resourceReleaseText, "CAPTURE_SERVICE_SEMAPHORE_RELEASE_WARN");
        AssertContains(resourceReleaseText, "CAPTURE_SERVICE_SEMAPHORE_DISPOSE_WARN");
        AssertContains(resourceReleaseText, "FLASHBACK_EVICTION_RESUME_WARN");
        AssertContains(cleanupText, "EnterDisposedState();");
        AssertContains(
            cleanupText,
            "ResetSessionStateAfterCleanup();");
        AssertDoesNotContain(cleanupText, "_sessionState =");

        var fatalCleanupText = ExtractMemberCode(failureCleanupText, "BeginFatalCaptureCleanup");
        AssertContains(fatalCleanupText, "EnterCleanupState();");
        AssertContains(fatalCleanupText, "EnterFaultedState();");
        AssertDoesNotContain(failureCleanupText, "_sessionState =");
        AssertContains(failureCleanupText, "private void BeginFlashbackBackendCleanup(Exception ex)");
        AssertContains(failureCleanupText, "private static bool IsGpuDeviceLost(Exception ex)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.FlashbackBackendFailureCleanup.cs")),
            "CaptureService Flashback backend failure cleanup folded into cleanup lifecycle");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Failures.cs")),
            "CaptureService failure callbacks folded into CaptureService.cs");

        return Task.CompletedTask;
    }

    internal static async Task CaptureService_StrictHfrFatalHandler_ClearsActiveSessionState()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);
        SetPrivateField(captureService, "_isVideoPreviewActive", true);
        SetPrivateField(captureService, "_isAudioPreviewActive", true);
        SetPrivateField(captureService, "_isRecording", true);

        InvokeNonPublicInstanceMethod(
            captureService,
            "OnUnifiedVideoCaptureFatalError",
            new object?[] { null, new InvalidOperationException("synthetic hfr failure") });

        await WaitForConditionAsync(
            () =>
                string.Equals(GetPropertyValue(captureService, "SessionState")?.ToString(), "Faulted", StringComparison.Ordinal) &&
                !GetBoolProperty(captureService, "IsInitialized") &&
                !GetBoolProperty(captureService, "IsVideoPreviewActive") &&
                !GetBoolProperty(captureService, "IsAudioPreviewActive") &&
                !GetBoolProperty(captureService, "IsRecording"),
            "CaptureService fatal cleanup").ConfigureAwait(false);

        AssertEqual("Faulted", GetPropertyValue(captureService, "SessionState")?.ToString(), "SessionState");
        AssertEqual(false, GetBoolProperty(captureService, "IsInitialized"), "IsInitialized");
        AssertEqual(false, GetBoolProperty(captureService, "IsVideoPreviewActive"), "IsVideoPreviewActive");
        AssertEqual(false, GetBoolProperty(captureService, "IsAudioPreviewActive"), "IsAudioPreviewActive");
        AssertEqual(false, GetBoolProperty(captureService, "IsRecording"), "IsRecording");

        await DisposeAsync(captureService).ConfigureAwait(false);
    }

    internal static Task CaptureSessionTransitionPolicy_DefinesCoreLifecycleRules()
    {
        var policyType = RequireType("Sussudio.Models.CaptureSessionTransitionPolicy");
        var stateType = RequireType("Sussudio.Models.CaptureSessionState");
        var canEnter = policyType.GetMethod(
            "CanEnterTransition",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { stateType, stateType },
            modifiers: null)
            ?? throw new InvalidOperationException("CaptureSessionTransitionPolicy.CanEnterTransition not found.");

        var states = new[]
        {
            "Uninitialized",
            "Initializing",
            "Ready",
            "Previewing",
            "Recording",
            "CleaningUp",
            "Faulted",
            "Disposed"
        };

        var allowedTransitions = new HashSet<string>
        {
            "Uninitialized->Uninitialized",
            "Uninitialized->Initializing",
            "Uninitialized->Ready",
            "Uninitialized->Previewing",
            "Uninitialized->CleaningUp",
            "Initializing->Initializing",
            "Initializing->Ready",
            "Initializing->Previewing",
            "Initializing->CleaningUp",
            "Ready->Initializing",
            "Ready->Ready",
            "Ready->Previewing",
            "Ready->Recording",
            "Ready->CleaningUp",
            "Previewing->Initializing",
            "Previewing->Ready",
            "Previewing->Previewing",
            "Previewing->Recording",
            "Previewing->CleaningUp",
            "Recording->Initializing",
            "Recording->Ready",
            "Recording->Previewing",
            "Recording->Recording",
            "Recording->CleaningUp",
            "CleaningUp->CleaningUp",
            "Faulted->Initializing",
            "Faulted->Ready",
            "Faulted->Previewing",
            "Faulted->CleaningUp",
            "Faulted->Faulted"
        };

        foreach (var currentState in states)
        {
            foreach (var transitionState in states)
            {
                var key = $"{currentState}->{transitionState}";
                AssertCanEnterTransition(
                    canEnter,
                    stateType,
                    currentState,
                    transitionState,
                    expected: allowedTransitions.Contains(key));
            }
        }

        return Task.CompletedTask;
    }

    internal static Task CaptureSessionTransitionPolicy_ResolvesSteadyStateFromRuntimeFlags()
    {
        var policyType = RequireType("Sussudio.Models.CaptureSessionTransitionPolicy");
        var method = policyType.GetMethod(
            "ResolveSteadyState",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(bool), typeof(bool), typeof(bool), typeof(bool), typeof(bool) },
            modifiers: null)
            ?? throw new InvalidOperationException("CaptureSessionTransitionPolicy.ResolveSteadyState not found.");

        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Disposed"),
            ResolveState(method, isDisposed: true, isRecording: true, isVideoPreviewActive: true, isAudioPreviewActive: true, isInitialized: true),
            "Disposed steady state precedence");
        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Recording"),
            ResolveState(method, isDisposed: false, isRecording: true, isVideoPreviewActive: true, isAudioPreviewActive: true, isInitialized: true),
            "Recording steady state precedence");
        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Previewing"),
            ResolveState(method, isDisposed: false, isRecording: false, isVideoPreviewActive: false, isAudioPreviewActive: true, isInitialized: true),
            "Audio preview steady state");
        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Ready"),
            ResolveState(method, isDisposed: false, isRecording: false, isVideoPreviewActive: false, isAudioPreviewActive: false, isInitialized: true),
            "Initialized steady state");
        AssertEqual(
            ParseEnum("Sussudio.Models.CaptureSessionState", "Uninitialized"),
            ResolveState(method, isDisposed: false, isRecording: false, isVideoPreviewActive: false, isAudioPreviewActive: false, isInitialized: false),
            "Uninitialized steady state");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_RunTransition_UsesTransitionPolicy()
    {
        var transitionExecutionText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs");
        var stateMachineText = ReadRepoFile("Sussudio/Models/Capture/CaptureModels.cs");

        AssertContains(
            transitionExecutionText,
            "private async Task RunTransitionAsync(");
        AssertContains(
            transitionExecutionText,
            "_sessionStateMachine.EnterTransition(transitionState);");
        AssertContains(
            transitionExecutionText,
            "_sessionStateMachine.ResolveSteadyState(BuildSteadyStateInputs());");
        AssertContains(
            stateMachineText,
            "CaptureSessionTransitionPolicy.ThrowIfDisallowed(_state, transitionState);");
        AssertContains(
            stateMachineText,
            "CaptureSessionTransitionPolicy.ResolveSteadyState(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureSessionStateMachine.cs")),
            "capture session state machine folded into capture model owner");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_InPlaceMutationsUseCurrentStateTransition()
    {
        var currentStateTransitionOwners = new[]
        {
            "Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs",
            "Sussudio/Services/Capture/CaptureService.Flashback.cs"
        };

        foreach (var owner in currentStateTransitionOwners)
        {
            var ownerText = ReadRepoFile(owner);
            AssertContains(ownerText, "RunTransitionAsync(CurrentSessionState,");
        }

        var lifecycleTransitionOwners = new[]
        {
            "Sussudio/Services/Capture/CaptureService.cs",
            "Sussudio/Services/Capture/CaptureService.cs",
            "Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs",
            "Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs"
        };

        foreach (var owner in lifecycleTransitionOwners)
        {
            var ownerText = ReadRepoFile(owner);
            AssertDoesNotContain(ownerText, "RunTransitionAsync(CurrentSessionState,");
        }

        return Task.CompletedTask;
    }


    private static readonly string[] CaptureServiceAudioFiles =
    {
        "Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs",
        "Sussudio/Services/Capture/CaptureService.cs"
    };

    private static string ReadCaptureServiceAudioSource()
        => string.Join(
            "\n",
            CaptureServiceAudioFiles.Select(file => ReadRepoFile(file).Replace("\r\n", "\n")));

    private static string ReadCaptureServiceAudioCodeWithoutCommentsOrStrings()
        => string.Join(
            "\n",
            CaptureServiceAudioFiles.Select(ReadRepoCodeWithoutCommentsOrStrings));

    internal static Task PreviewStopCompatibilityOverloads_ArePreserved()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n")
            + "\n" + ReadCaptureServicePreviewLifecycleSource()
            + "\n" + ReadCaptureServiceAudioSource();
        var coordinatorText = ReadCaptureSessionCoordinatorSource();
        var viewModelPreviewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");

        AssertContains(captureServiceText, "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureServiceText, "public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureServiceText, "if (!_isVideoPreviewActive)\n            {\n                if (teardownPipeline)\n                {\n                    await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: false).ConfigureAwait(false);\n                }\n\n                return;\n            }");
        AssertContains(captureServiceText, "public Task StopAudioPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(captureServiceText, "public Task StopAudioPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(captureServiceText, "public Task StopVideoPreviewAsync(bool");
        AssertDoesNotContain(captureServiceText, "public Task StopAudioPreviewAsync(bool");
        AssertContains(coordinatorText, "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(coordinatorText, "public Task StopVideoPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertContains(coordinatorText, "public Task StopAudioPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(coordinatorText, "public Task StopAudioPreviewWithTeardownAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(coordinatorText, "public Task StopVideoPreviewAsync(bool");
        AssertDoesNotContain(coordinatorText, "public Task StopAudioPreviewAsync(bool");
        AssertContains(viewModelPreviewStateText, "public Task StopPreviewAsync()\n        => StopPreviewAsync(userInitiated: true, teardownPipeline: false, CancellationToken.None);");
        AssertContains(viewModelPreviewStateText, "public Task StopPreviewAsync(bool userInitiated)\n        => StopPreviewAsync(userInitiated, teardownPipeline: false, CancellationToken.None);");

        return Task.CompletedTask;
    }

    internal static Task PreviewStopApiSurface_HasNoDefaultLiteralAmbiguity()
    {
        AssertPreviewStopSurface("Sussudio.Services.Capture.CaptureService");
        AssertPreviewStopSurface("Sussudio.Services.Capture.CaptureSessionCoordinator");
        return Task.CompletedTask;
    }

    private static void AssertPreviewStopSurface(string typeName)
    {
        var type = RequireType(typeName);
        AssertStopSurface(type, "StopVideoPreviewAsync", "StopVideoPreviewWithTeardownAsync");
        AssertStopSurface(type, "StopAudioPreviewAsync", "StopAudioPreviewWithTeardownAsync");
    }

    private static void AssertStopSurface(Type type, string stopMethodName, string teardownMethodName)
    {
        var publicInstance = BindingFlags.Instance | BindingFlags.Public;
        var oneParameterStopOverloads = type.GetMethods(publicInstance)
            .Where(method => method.Name == stopMethodName && method.GetParameters().Length == 1)
            .ToArray();

        AssertEqual(1, oneParameterStopOverloads.Length, $"{type.FullName}.{stopMethodName} one-parameter overload count");
        AssertEqual(
            typeof(CancellationToken).FullName,
            oneParameterStopOverloads[0].GetParameters()[0].ParameterType.FullName,
            $"{type.FullName}.{stopMethodName} single parameter");

        var boolFirstParameterOverloads = type.GetMethods(publicInstance)
            .Where(method =>
            {
                if (method.Name != stopMethodName)
                {
                    return false;
                }

                var parameters = method.GetParameters();
                return parameters.Length > 0 && parameters[0].ParameterType == typeof(bool);
            })
            .ToArray();
        AssertEqual(0, boolFirstParameterOverloads.Length, $"{type.FullName}.{stopMethodName} bool-first overload count");

        var teardownMethod = type.GetMethod(teardownMethodName, publicInstance, binder: null, types: new[] { typeof(CancellationToken) }, modifiers: null);
        AssertNotNull(teardownMethod, $"{type.FullName}.{teardownMethodName}(CancellationToken)");
    }

    internal static Task PreviewStartup_ToleratesMissingAudioCaptureDevices()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs").Replace("\r\n", "\n");

        AssertContains(captureServiceText, "if (settings.AudioEnabled && !string.IsNullOrWhiteSpace(audioDeviceId))");
        AssertContains(captureServiceText, "Audio preview requested but no audio capture device is available; continuing with video-only preview.");
        AssertDoesNotContain(captureServiceText, "Audio preview is enabled but no audio capture device is available.");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_PreviewLifecycleLivesInCohesiveOwner()
    {
        var startText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs").Replace("\r\n", "\n");
        var audioGraphText = startText;
        var stopText = startText;
        var freshPipelineText = ExtractTextBetween(
            startText,
            "private async Task StartFreshPreviewPipelineAsync(",
            "private async Task DisposePreviewPipelineAsync(");
        var videoPipelineResourcesText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs").Replace("\r\n", "\n");
        var flashbackPreviewBackendText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs").Replace("\r\n", "\n");
        var cleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs").Replace("\r\n", "\n");
        var libAvFinalizeText = (
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs"))
            .Replace("\r\n", "\n");
        var recordingRollbackText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs").Replace("\r\n", "\n");

        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewLifecycle.cs")),
            "video and audio preview lifecycle share one owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewStart.cs")),
            "old preview start partial folded into preview lifecycle owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.AudioPreviewLifecycle.cs")),
            "old audio preview partial folded into preview lifecycle owner");
        AssertContains(startText, "public Task StartVideoPreviewAsync(CaptureSettings settings, CancellationToken cancellationToken = default)");
        AssertContains(startText, "await RecyclePreviewPipelineForStartAsync(");
        AssertContains(startText, "if (await TryStartPreviewFromRetainedPipelineAsync(settings, transitionToken).ConfigureAwait(false))");
        AssertContains(startText, "await StartFreshPreviewPipelineAsync(");
        AssertContains(startText, "private async Task RecyclePreviewPipelineForStartAsync(");
        AssertContains(startText, "PREVIEW_START recycle_pipeline=1 reason=settings_changed");
        AssertContains(startText, "PREVIEW_START recycle_pipeline=1 reason=flashback_disabled");
        AssertContains(startText, "PREVIEW_START recycle_flashback=1 reason=flashback_settings_changed");
        AssertContains(startText, "private async Task<bool> TryStartPreviewFromRetainedPipelineAsync(");
        AssertContains(startText, "FLASHBACK_FAST_PATH_FORMAT_MISMATCH");
        AssertContains(startText, "await EnsureFlashbackAudioInputsAsync(settings, transitionToken, \"preview_fast_path\")");
        AssertContains(startText, "private async Task StartFreshPreviewPipelineAsync(");
        AssertContains(startText, "await StartPreviewAudioGraphAsync(settings, audioDeviceId, transitionToken)");
        AssertContains(startText, "var previewStartRollbackToken = CancellationToken.None;");
        AssertContains(startText, "private bool CanReuseVideoCaptureForPreview(UnifiedVideoCapture capture, CaptureSettings settings)");
        AssertContains(startText, "private static bool CanReuseFlashbackBackend(CaptureSettings current, CaptureSettings next)");
        AssertContains(startText, "private static CaptureSettings CloneCaptureSettings(CaptureSettings source)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewStart.Recycle.cs")),
            "old preview-start recycle partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewStart.FastPath.cs")),
            "old preview-start fast-path partial removed");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewStart.FreshPipeline.cs")),
            "old preview-start fresh-pipeline partial removed");
        AssertContains(audioGraphText, "private async Task<WasapiAudioCapture?> StartPreviewAudioGraphAsync(");
        AssertContains(audioGraphText, "private async Task StartPreviewMicrophoneMonitorAsync(");
        AssertContains(audioGraphText, "private async Task RollbackPreviewAudioCaptureStartupAsync(");
        AssertContains(stopText, "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(stopText, "private Task StopVideoPreviewCoreAsync(bool teardownPipeline, CancellationToken cancellationToken = default)");
        AssertContains(stopText, "private async Task DisposePreviewPipelineAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewStop.cs")),
            "preview stop and disposal folded into preview lifecycle owner");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewReuse.cs")),
            "preview reuse helper partial folded into preview start");
        AssertContains(videoPipelineResourcesText, "internal sealed class CaptureVideoPipelineResources");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureVideoPipelineResources.cs")),
            "video pipeline resources folded into CaptureService.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CapturePipelineResources.cs")),
            "capture pipeline resources folded into CaptureService.cs");
        AssertContains(videoPipelineResourcesText, "public UnifiedVideoCapture? Capture { get; set; }");
        AssertContains(videoPipelineResourcesText, "public IPreviewFrameSink? PreviewFrameSink { get; set; }");
        AssertContains(videoPipelineResourcesText, "public UnifiedVideoCapture.MjpegPipelineTimingMetrics LastMjpegPipelineTimingMetrics { get; private set; }");
        AssertContains(videoPipelineResourcesText, "public ParallelMjpegDecodePipeline.PipelineTimingMetrics? LastFullMjpegPipelineTimingMetrics { get; private set; }");
        AssertContains(videoPipelineResourcesText, "public void CacheMjpegTimingMetrics(UnifiedVideoCapture? capture)");
        AssertContains(videoPipelineResourcesText, "public CaptureMjpegTimingSnapshot GetMjpegTimingSnapshot(UnifiedVideoCapture? capture)");
        AssertContains(videoPipelineResourcesText, "public Task ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(videoPipelineResourcesText, "UNIFIED_VIDEO_DEFERRED_PREVIEW_DETACH_WARN");
        AssertContains(videoPipelineResourcesText, "UNIFIED_VIDEO_DEFERRED_CLEANUP_END");
        AssertContains(
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs"),
            "private readonly CaptureVideoPipelineResources _videoPipeline = new();");
        AssertDoesNotContain(
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs"),
            "_unifiedVideoCapture");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.VideoPipelineLifecycle.cs")),
            "video pipeline lifecycle helper partial folded into preview start");
        AssertContains(startText, "internal void SetPreviewFrameSink(IPreviewFrameSink? sink)");
        AssertContains(startText, "private void AttachUnifiedVideoCapture(UnifiedVideoCapture unifiedVideoCapture)");
        AssertContains(startText, "private void DetachUnifiedVideoCapture(UnifiedVideoCapture? unifiedVideoCapture)");
        AssertContains(startText, "private void CacheMjpegTimingMetrics(UnifiedVideoCapture? unifiedVideoCapture)");
        AssertDoesNotContain(startText, "private IPreviewFrameSink? _previewFrameSink");
        AssertDoesNotContain(startText, "private Task ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(stopText, "_recordingBackend.ClearPendingLibAvDrainIfCompletedSuccessfully();");
        AssertContains(startText, "private void TryApplySharedPreviewDevice(UnifiedVideoCapture? capture, IPreviewFrameSink? sink)");
        AssertContains(startText, "_videoPipeline.CacheMjpegTimingMetrics(unifiedVideoCapture);");
        AssertContains(cleanupText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(stopText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(libAvFinalizeText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertContains(recordingRollbackText, "_videoPipeline.ScheduleDeferredUnifiedVideoCaptureCleanup(");
        AssertDoesNotContain(startText, "private UnifiedVideoCapture.MjpegPipelineTimingMetrics _lastMjpegPipelineTimingMetrics;");
        AssertDoesNotContain(startText, "private ParallelMjpegDecodePipeline.PipelineTimingMetrics? _lastFullMjpegPipelineTimingMetrics;");
        AssertDoesNotContain(flashbackPreviewBackendText, "ScheduleDeferredUnifiedVideoCaptureCleanup");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewPipeline.cs")),
            "old preview pipeline partial removed after video lifecycle promotion");
        AssertDoesNotContain(freshPipelineText, "new WasapiAudioCapture()");
        AssertDoesNotContain(freshPipelineText, "micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.PreviewDisposal.cs")),
            "old preview disposal partial removed");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_AudioOwnershipLivesWithPreviewLifecycleOwner()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs");
        var audioPreviewText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs");
        var resourceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs");

        AssertContains(rootText, "private readonly PreviewAudioGraphResources _previewAudioGraph = new();");
        AssertContains(rootText, "internal sealed class PreviewAudioGraphResources");
        AssertContains(resourceText, "internal sealed class PreviewAudioGraphResources");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "PreviewAudioGraphResources.cs")),
            "preview audio graph resources folded into CaptureService.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CapturePipelineResources.cs")),
            "capture pipeline resources folded into CaptureService.cs");
        AssertContains(resourceText, "public WasapiAudioCapture? ProgramCapture;");
        AssertContains(resourceText, "public WasapiAudioCapture? MicrophoneCapture;");
        AssertContains(resourceText, "public WasapiAudioPlayback? Playback;");
        AssertContains(resourceText, "public float PreviewVolume = 1.0f;");
        AssertContains(resourceText, "private bool _captureFaulted;");
        AssertContains(resourceText, "private string? _captureFaultMessage;");
        AssertContains(resourceText, "public void RecordCaptureFault(");
        AssertContains(resourceText, "public PreviewAudioCaptureFaultSnapshot ConsumeCaptureFault()");
        AssertDoesNotContain(rootText, "get => _previewAudioGraph.ProgramCapture;");
        AssertDoesNotContain(rootText, "get => _previewAudioGraph.MicrophoneCapture;");
        AssertDoesNotContain(rootText, "get => _previewAudioGraph.Playback;");
        AssertDoesNotContain(rootText, "private WasapiAudioCapture? _wasapiAudioCapture");
        AssertDoesNotContain(rootText, "private WasapiAudioCapture? _microphoneCapture");
        AssertDoesNotContain(rootText, "private WasapiAudioPlayback? _wasapiAudioPlayback");
        AssertDoesNotContain(rootText, "private float _previewVolume");
        AssertDoesNotContain(rootText, "private bool _isMonitoringMuted");
        AssertDoesNotContain(rootText, "private bool _wasapiAudioCaptureFaulted;");
        AssertDoesNotContain(rootText, "private string? _wasapiAudioCaptureFaultMessage;");
        AssertContains(audioPreviewText, "public void SetPreviewVolume(");
        AssertContains(audioPreviewText, "public void SetMonitoringMuted(");
        AssertContains(audioPreviewText, "private void OnWasapiAudioLevelUpdated(");
        AssertContains(audioPreviewText, "private void OnWasapiCaptureFailed(");
        AssertContains(audioPreviewText, "public Task StartAudioPreviewAsync(");
        AssertContains(audioPreviewText, "public Task StopAudioPreviewAsync(");
        AssertContains(audioPreviewText, "public Task StopAudioPreviewWithTeardownAsync(");
        AssertContains(audioPreviewText, "private Task StopAudioPreviewCoreAsync(");
        AssertContains(audioPreviewText, "public Task UpdateAudioInputAsync(");
        AssertContains(audioPreviewText, "Logger.Log($\"Live audio input switch:");
        AssertContains(audioPreviewText, "Logger.Log(\"AUDIO_INPUT_SWITCH_CANCEL_DEFERRED\");");
        AssertContains(audioPreviewText, "public Task UpdateMicrophoneMonitorAsync(");
        AssertContains(audioPreviewText, "RunTransitionAsync(CurrentSessionState,");
        AssertContains(audioPreviewText, "private async Task DisposeMicrophoneCaptureAsync()");
        AssertContains(audioPreviewText, "private void OnMicrophoneAudioLevelUpdated(");
        AssertContains(audioPreviewText, "private async Task RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(audioPreviewText, "private readonly record struct MicrophoneMonitorRestartOptions(");
        AssertDoesNotContain(audioPreviewText, "private async Task StartWasapiPlaybackAsync(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.Audio.cs")),
            "old audio event projection partial removed after audio preview lifecycle consolidation");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.AudioInputSwitching.cs")),
            "live audio input switching folded into CaptureService.PreviewLifecycle.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.MicrophoneMonitor.cs")),
            "microphone monitor state and restart folded into CaptureService.PreviewLifecycle.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.MicrophoneMonitor.Update.cs")),
            "old microphone monitor update partial removed after monitor consolidation");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.MicrophoneMonitor.Restart.cs")),
            "old microphone monitor restart partial removed after monitor consolidation");

        AssertContains(resourceText, "public async Task StartPlaybackAsync(");
        AssertContains(resourceText, "public void StopPlayback(");
        AssertContains(resourceText, "public void DetachCapture(");
        AssertContains(resourceText, "private static void SafeClearCapturePlayback(");
        AssertContains(resourceText, "private static void DisposePlaybackBestEffort(");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Capture", "CaptureService.WasapiPlayback.cs")),
            "old WASAPI playback partial removed after PreviewAudioGraphResources promotion");

        return Task.CompletedTask;
    }

    internal static Task CaptureService_MicrophoneRestartAfterRecordingLivesInPreviewLifecycleOwner()
    {
        var flashbackFinalizationText = ExtractTextBetween(
            ReadRepoFile("Sussudio/Services/Capture/CaptureService.Flashback.cs").Replace("\r\n", "\n"),
            "private async Task<FinalizeResult> FinalizeFlashbackRecordingAsync(",
            "private async Task<OperationCanceledException?> ReconcileFlashbackBackendAfterRecordingFinalizeAsync(");
        var recordingLifecycleText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs")
            .Replace("\r\n", "\n");
        var libAvFinalizationText = ExtractTextBetween(
            recordingLifecycleText,
            "private async Task<OperationCanceledException?> RestoreLibAvPreviewFeaturesAfterRecordingAsync(",
            "private readonly record struct LibAvFinalizeStepResult(");
        var finalizationText = string.Join(
            "\n",
            flashbackFinalizationText,
            libAvFinalizationText)
            .Replace("\r\n", "\n");
        var microphoneRootText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(microphoneRootText, "private readonly record struct MicrophoneMonitorRestartOptions(");
        AssertContains(microphoneRootText, "private async Task RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(microphoneRootText, "new WasapiAudioCapture()");
        AssertContains(microphoneRootText, "micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertContains(microphoneRootText, "micCapture.CaptureFailed += OnWasapiCaptureFailed;");
        AssertContains(microphoneRootText, "micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));");
        AssertContains(microphoneRootText, "FLASHBACK_MIC_ATTACH_OK reason='{options.FlashbackAttachReason}'");
        AssertContains(microphoneRootText, "Logger.Log($\"{options.RestartLogEvent} device='\" + (_micMonitorDeviceName ?? \"?\") + \"'\");");
        AssertContains(microphoneRootText, "Logger.Log($\"{options.DisposeWarningEvent} type={disposeEx.GetType().Name} msg={disposeEx.Message}\");");
        AssertOccursBefore(
            microphoneRootText,
            "micCapture.SetAudioWriter(samples => fbSink.WriteMicrophoneAudioAsync(samples));",
            "_previewAudioGraph.MicrophoneCapture = micCapture;");

        AssertContains(finalizationText, "await RestartMicrophoneMonitorAfterRecordingAsync(");
        AssertContains(finalizationText, "OnlyWhenMissing: true,");
        AssertContains(finalizationText, "DisposeWarningEvent: \"FLASHBACK_MIC_RESTART_DISPOSE_WARN\"");
        AssertContains(finalizationText, "OnlyWhenMissing: false,");
        AssertContains(finalizationText, "FlashbackAttachReason: \"mic_monitor_restart\",");
        AssertContains(finalizationText, "RestartLogEvent: \"MIC_MONITOR_RESTART\",");
        AssertContains(finalizationText, "DisposeWarningEvent: \"MIC_MONITOR_RESTART_DISPOSE_WARN\"");
        AssertDoesNotContain(finalizationText, "WasapiAudioCapture? micCapture = null;");
        AssertDoesNotContain(finalizationText, "micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");
        AssertDoesNotContain(finalizationText, "micCapture.CaptureFailed += OnWasapiCaptureFailed;");

        return Task.CompletedTask;
    }

    internal static async Task AudioPreview_RemainsInactive_WhenNoAudioCaptureDeviceExists()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var device = BuildDevice();
        SetPropertyOrBackingField(device, "AudioDeviceId", null);
        SetPropertyOrBackingField(device, "AudioDeviceName", null);
        var settings = BuildSettings(hdrEnabled: false);

        await InvokeInitializeAsync(captureService, device, settings).ConfigureAwait(false);

        string? lastStatus = null;
        var handler = new EventHandler<string>((_, status) => lastStatus = status);
        var statusChanged = captureService.GetType().GetEvent("StatusChanged", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("CaptureService.StatusChanged event not found.");
        statusChanged.AddEventHandler(captureService, handler);

        try
        {
            var startAudioPreview = captureService.GetType().GetMethod(
                "StartAudioPreviewAsync",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(CancellationToken) },
                modifiers: null);
            if (startAudioPreview == null)
            {
                throw new InvalidOperationException("CaptureService.StartAudioPreviewAsync method not found.");
            }

            if (startAudioPreview.Invoke(captureService, new object?[] { CancellationToken.None }) is not Task task)
            {
                throw new InvalidOperationException("CaptureService.StartAudioPreviewAsync did not return a Task.");
            }

            await task.ConfigureAwait(false);

            AssertEqual(false, GetBoolProperty(captureService, "IsAudioPreviewActive"), "IsAudioPreviewActive");
            AssertEqual("Audio preview unavailable", lastStatus, "StatusChanged");
        }
        finally
        {
            statusChanged.RemoveEventHandler(captureService, handler);
            await DisposeAsync(captureService).ConfigureAwait(false);
        }
    }

    internal static Task PreviewBackendLog_ReflectsVideoOnlyFallback()
    {
        var captureServiceText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs").Replace("\r\n", "\n");

        AssertContains(captureServiceText, "_previewAudioGraph.ProgramCapture != null");
        AssertContains(captureServiceText, "\"Preview backend active: IMFSourceReader video + WASAPI audio ingest.\"");
        AssertContains(captureServiceText, "\"Preview backend active: IMFSourceReader video only (no audio capture endpoint).\"");

        return Task.CompletedTask;
    }
}
