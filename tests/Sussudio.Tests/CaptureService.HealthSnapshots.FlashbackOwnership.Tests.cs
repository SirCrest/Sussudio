using Xunit;

public sealed partial class CaptureServiceHealthSnapshotOwnershipTests
{
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
    public void CaptureService_HealthSnapshotFlashbackBufferFields_LiveWithFlashbackBackendProjection()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var flashbackBackendText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackBackend.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var flashbackBuffer = CaptureFlashbackBufferHealthSnapshotFields(");
        AssertContains(healthSnapshotAssemblerText, "FlashbackBufferedDurationMs = flashbackBuffer.BufferedDurationMs,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackBackendSettingsStaleReason = flashbackBuffer.BackendSettingsStaleReason,");
        AssertContains(healthSnapshotAssemblerText, "EncoderTargetBitRate = flashbackBuffer.EncoderTargetBitRate,");
        AssertDoesNotContain(healthSnapshotText, "FlashbackBufferedDurationMs = (long)(bufMgr?.BufferedDuration.TotalMilliseconds ?? 0)");
        AssertDoesNotContain(healthSnapshotText, "ResolveFlashbackBackendSettingsStaleReason(flashbackBackendSettings, _currentSettings)");

        AssertContains(flashbackBackendText, "private FlashbackBufferHealthSnapshotFields CaptureFlashbackBufferHealthSnapshotFields(");
        AssertContains(flashbackBackendText, "ResolveFlashbackBackendSettingsStaleReason(flashbackBackendSettings, currentSettings)");
        AssertContains(flashbackBackendText, "private static string ResolveFlashbackBackendSettingsStaleReason(");
        AssertContains(flashbackBackendText, "bufMgr?.StartupCacheOverBudget ?? false");
        AssertContains(flashbackBackendText, "fbSink?.EncoderFrameRateDenominator");
        AssertContains(flashbackBackendText, "private readonly record struct FlashbackBufferHealthSnapshotFields");

    }

    [Fact]
    public void CaptureService_HealthSnapshotFlashbackQueueFields_LiveWithFlashbackBackendProjection()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotAssembler.cs")
            .Replace("\r\n", "\n");
        var flashbackBackendText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackBackend.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var flashbackQueues = CaptureFlashbackQueueHealthSnapshotFields(");
        AssertContains(healthSnapshotAssemblerText, "FlashbackVideoQueueDepth = flashbackQueues.VideoQueueDepth,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackForceRotateActive = flashbackQueues.ForceRotateActive,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackVideoQueueLatencyP99Ms = flashbackQueues.VideoQueueLatencyMetrics.P99Ms,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackGpuQueueLastRejectReason = flashbackQueues.GpuQueueLastRejectReason,");
        AssertDoesNotContain(healthSnapshotText, "FlashbackVideoQueueDepth = fbSink?.VideoQueueCount");
        AssertDoesNotContain(healthSnapshotText, "FlashbackForceRotateActive = fbSink?.IsForceRotateActive");
        AssertDoesNotContain(healthSnapshotText, "FlashbackGpuQueueLastRejectReason = fbSink?.LastGpuQueueRejectReason");

        AssertContains(flashbackBackendText, "private static FlashbackQueueHealthSnapshotFields CaptureFlashbackQueueHealthSnapshotFields(");
        AssertContains(flashbackBackendText, "fbSink?.VideoQueueOldestFrameAgeMs ?? 0");
        AssertContains(flashbackBackendText, "fbSink?.IsForceRotateActive ?? false");
        AssertContains(flashbackBackendText, "fbSink?.LastGpuQueueRejectReason ?? string.Empty");
        AssertContains(flashbackBackendText, "private readonly record struct FlashbackQueueHealthSnapshotFields");

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
        var flashbackPlaybackStateText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshotFlashbackPlayback.State.cs")
            .Replace("\r\n", "\n");

        AssertContains(healthSnapshotText, "var flashbackPlayback = CaptureFlashbackPlaybackHealthSnapshotFields(fbPlayback);");
        AssertContains(healthSnapshotAssemblerText, "FlashbackPlaybackState = flashbackPlayback.State,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackPlaybackDecodeP95Ms = flashbackPlayback.DecodeP95Ms,");
        AssertContains(healthSnapshotAssemblerText, "FlashbackPlaybackLastCommandFailure = flashbackPlayback.LastCommandFailure,");
        AssertDoesNotContain(healthSnapshotText, "var playbackCadence = fbPlayback?.GetPlaybackCadenceMetrics()");
        AssertDoesNotContain(healthSnapshotText, "var playbackDecode = fbPlayback?.GetPlaybackDecodeMetrics()");
        AssertDoesNotContain(healthSnapshotText, "FlashbackPlaybackFrameCount = fbPlayback?.PlaybackFrameCount");

        AssertContains(flashbackPlaybackText, "private static FlashbackPlaybackHealthSnapshotFields CaptureFlashbackPlaybackHealthSnapshotFields(");
        AssertContains(flashbackPlaybackText, "var state = CaptureFlashbackPlaybackStateHealthSnapshotFields(fbPlayback);");
        AssertContains(flashbackPlaybackText, "var cadence = CaptureFlashbackPlaybackCadenceHealthSnapshotFields(fbPlayback);");
        AssertContains(flashbackPlaybackText, "var decode = CaptureFlashbackPlaybackDecodeHealthSnapshotFields(fbPlayback);");
        AssertContains(flashbackPlaybackText, "var audioMaster = CaptureFlashbackPlaybackAudioMasterHealthSnapshotFields(fbPlayback);");
        AssertContains(flashbackPlaybackText, "var commands = CaptureFlashbackPlaybackCommandHealthSnapshotFields(fbPlayback);");
        AssertContains(flashbackPlaybackText, "private readonly record struct FlashbackPlaybackHealthSnapshotFields");
        AssertContains(flashbackPlaybackStateText, "private static FlashbackPlaybackStateHealthSnapshotFields CaptureFlashbackPlaybackStateHealthSnapshotFields(");
        AssertContains(flashbackPlaybackStateText, "private readonly record struct FlashbackPlaybackStateHealthSnapshotFields(");
        AssertContains(flashbackPlaybackStateText, "fbPlayback?.State.ToString() ?? \"N/A\"");
        AssertContains(flashbackPlaybackStateText, "fbPlayback?.PlaybackFrameCount ?? 0");
        AssertContains(flashbackPlaybackStateText, "fbPlayback?.PlaybackThreadAlive ?? false");
        AssertContains(flashbackPlaybackText, "private static FlashbackPlaybackCadenceHealthSnapshotFields CaptureFlashbackPlaybackCadenceHealthSnapshotFields(");
        AssertContains(flashbackPlaybackText, "private readonly record struct FlashbackPlaybackCadenceHealthSnapshotFields(");
        AssertContains(flashbackPlaybackText, "fbPlayback?.GetPlaybackCadenceMetrics() ?? default");
        AssertContains(flashbackPlaybackText, "private static FlashbackPlaybackDecodeHealthSnapshotFields CaptureFlashbackPlaybackDecodeHealthSnapshotFields(");
        AssertContains(flashbackPlaybackText, "private readonly record struct FlashbackPlaybackDecodeHealthSnapshotFields(");
        AssertContains(flashbackPlaybackText, "fbPlayback?.GetPlaybackDecodeMetrics() ?? default");
        AssertContains(flashbackPlaybackText, "fbPlayback?.PlaybackMaxDecodePhase ?? string.Empty");
        AssertContains(flashbackPlaybackText, "private static FlashbackPlaybackAudioMasterHealthSnapshotFields CaptureFlashbackPlaybackAudioMasterHealthSnapshotFields(");
        AssertContains(flashbackPlaybackText, "private readonly record struct FlashbackPlaybackAudioMasterHealthSnapshotFields(");
        AssertContains(flashbackPlaybackText, "fbPlayback?.PlaybackAudioMasterFallbacks ?? 0");
        AssertContains(flashbackPlaybackText, "private static FlashbackPlaybackCommandHealthSnapshotFields CaptureFlashbackPlaybackCommandHealthSnapshotFields(");
        AssertContains(flashbackPlaybackText, "private readonly record struct FlashbackPlaybackCommandHealthSnapshotFields(");
        AssertContains(flashbackPlaybackText, "fbPlayback?.CommandsEnqueued ?? 0");
        AssertContains(flashbackPlaybackText, "double[] RecentFrameIntervalsMs");
        AssertContains(flashbackPlaybackText, "string LastCommandFailure");

    }
}
