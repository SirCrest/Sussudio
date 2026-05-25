using System;
using System.IO;
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
