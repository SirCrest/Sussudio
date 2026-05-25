using Xunit;

public sealed partial class CaptureServiceHealthSnapshotOwnershipTests
{
    [Fact]
    public void CaptureService_HealthSnapshotFlashbackExportFields_LiveWithExportDiagnostics()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var flashbackExportText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.FlashbackExportDiagnostics.cs")
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
        Assert.False(System.IO.File.Exists(System.IO.Path.Combine(
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
        Assert.False(System.IO.File.Exists(System.IO.Path.Combine(
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
        Assert.False(System.IO.File.Exists(System.IO.Path.Combine(
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
        Assert.False(System.IO.File.Exists(System.IO.Path.Combine(
            FindRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.HealthSnapshotFlashbackPlayback.State.cs")));
        Assert.False(System.IO.File.Exists(System.IO.Path.Combine(
            FindRepoRoot(),
            "Sussudio",
            "Services",
            "Capture",
            "CaptureService.HealthSnapshotFlashbackPlayback.cs")));

    }
}
