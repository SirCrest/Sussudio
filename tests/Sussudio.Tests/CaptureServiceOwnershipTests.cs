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
    public async Task IdleCaptureOwners_ExposeEmptyMetricsAndTypedAbsentPlayback()
    {
        var assembly = Sussudio.Tests.SussudioAssembly.Load();
        var captureType = assembly.GetType("Sussudio.Services.Capture.CaptureService", throwOnError: true)!;
        var unifiedType = assembly.GetType("Sussudio.Services.Capture.UnifiedVideoCapture", throwOnError: true)!;
        await using var capture = (IAsyncDisposable)Activator.CreateInstance(captureType)!;
        await using var unified = (IAsyncDisposable)Activator.CreateInstance(unifiedType, nonPublic: true)!;

        var sourceCadence = unifiedType.GetMethod("GetSourceCadenceMetrics")!.Invoke(unified, null)!;
        Assert.Empty(Assert.IsType<double[]>(GetMetricProperty(sourceCadence, "RecentIntervalsMs")));
        Assert.Equal(0, GetMetricProperty(sourceCadence, "SampleCount"));
        Assert.Equal(0d, GetMetricProperty(sourceCadence, "ExpectedIntervalMs"));
        Assert.Equal(0d, GetMetricProperty(sourceCadence, "ObservedFps"));
        var jitter = unifiedType.GetMethod("GetMjpegPreviewJitterMetrics")!.Invoke(unified, null)!;
        Assert.Equal(string.Empty, GetMetricProperty(jitter, "LastDropReason"));
        Assert.Equal(string.Empty, GetMetricProperty(jitter, "LastUnderflowReason"));
        Assert.Equal(false, GetMetricProperty(jitter, "Enabled"));
        Assert.Equal(0, GetMetricProperty(jitter, "QueueDepth"));
        Assert.Equal(0L, GetMetricProperty(jitter, "TotalDropped"));

        var beforeSnapshotMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var health = captureType.GetMethod("GetHealthSnapshot")!.Invoke(capture, null)!;
        var afterSnapshotMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Assert.Equal(GetMetricProperty(capture, "SessionState"), GetMetricProperty(health, "SessionState"));
        Assert.Null(GetMetricProperty(health, "FlashbackExportVerificationFormat"));
        Assert.Equal(-1L, GetMetricProperty(health, "LastFrameArrivalMs"));
        Assert.InRange(((DateTimeOffset)GetMetricProperty(health, "TimestampUtc")!).ToUnixTimeMilliseconds(),
            beforeSnapshotMs, afterSnapshotMs);
        foreach (var property in new[] { "CaptureCadenceRecentIntervalsMs", "FlashbackPlaybackRecentFrameIntervalsMs" })
        {
            Assert.Empty(Assert.IsType<double[]>(GetMetricProperty(health, property)));
        }
        Assert.Equal(string.Empty, GetMetricProperty(health, "MjpegPreviewJitterLastDropReason"));
        Assert.Equal(string.Empty, GetMetricProperty(health, "MjpegPreviewJitterLastUnderflowReason"));
        Assert.Null(GetMetricProperty(health, "FlashbackPlaybackState"));
        Assert.Equal(0, GetMetricProperty(health, "CaptureCadenceSampleCount"));
        Assert.Equal(0d, GetMetricProperty(health, "CaptureCadenceExpectedIntervalMs"));
        Assert.Equal(0, GetMetricProperty(health, "FlashbackPlaybackCadenceSampleCount"));
        Assert.Equal(0L, GetMetricProperty(health, "FlashbackPlaybackSlowFrames"));
        Assert.Equal(0L, GetMetricProperty(health, "FlashbackPlaybackFrameCount"));
        Assert.Equal(0L, GetMetricProperty(health, "MjpegPreviewJitterTotalDropped"));

        var snapshot = Sussudio.Tests.AutomationSnapshotRegressionFixture.BuildResult(
            assembly, populated: false, healthOverride: health);
        foreach (var property in new[] { "CaptureCadenceRecentIntervalsMs", "FlashbackPlaybackRecentFrameIntervalsMs" })
        {
            Assert.Equal(System.Text.Json.JsonValueKind.Array, snapshot.GetProperty(property).ValueKind);
            Assert.Equal(0, snapshot.GetProperty(property).GetArrayLength());
        }
        Assert.Equal("N/A", snapshot.GetProperty("FlashbackPlaybackState").GetString());
        Assert.Equal(string.Empty, snapshot.GetProperty("MjpegPreviewJitterLastDropReason").GetString());
        Assert.Equal(string.Empty, snapshot.GetProperty("MjpegPreviewJitterLastUnderflowReason").GetString());
    }

    [Fact]
    public async Task SourceReaderWithoutSamples_RetainsConfiguredExpectedCadence()
    {
        var type = Sussudio.Tests.SussudioAssembly.Load().GetType(
            "Sussudio.Services.Capture.MfSourceReaderVideoCapture", throwOnError: true)!;
        await using var capture = (IAsyncDisposable)Activator.CreateInstance(type)!;
        type.GetMethod("SetExpectedFrameRate")!.Invoke(capture, new object[] { 120d });
        var cadence = type.GetMethod("GetSourceCadenceMetrics")!.Invoke(capture, null)!;

        Assert.Equal(1000d / 120, GetMetricProperty(cadence, "ExpectedIntervalMs"));
        Assert.Equal(0, GetMetricProperty(cadence, "SampleCount"));
        Assert.Equal(0d, GetMetricProperty(cadence, "ObservedFps"));
        Assert.Empty(Assert.IsType<double[]>(GetMetricProperty(cadence, "RecentIntervalsMs")));
    }

    [Fact]
    public void PlaybackWithoutSamples_RetainsAccumulatedSlowFrames()
    {
        var assembly = Sussudio.Tests.SussudioAssembly.Load();
        var bufferType = assembly.GetType("Sussudio.Services.Flashback.FlashbackBufferManager", throwOnError: true)!;
        var playbackType = assembly.GetType("Sussudio.Services.Flashback.FlashbackPlaybackController", throwOnError: true)!;
        using var buffer = (IDisposable)Activator.CreateInstance(bufferType, new object?[] { null })!;
        using var playback = (IDisposable)Activator.CreateInstance(playbackType, buffer)!;
        playbackType.GetField("_playbackSlowFrameCount", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(playback, 7L);
        var cadence = playbackType.GetMethod("GetPlaybackCadenceMetrics")!.Invoke(playback, null)!;

        Assert.Equal(7L, GetMetricProperty(cadence, "SlowFrameCount"));
        Assert.Equal(0, GetMetricProperty(cadence, "SampleCount"));
        Assert.Equal(0d, GetMetricProperty(cadence, "OnePercentLowFps"));
        Assert.Empty(Assert.IsType<double[]>(GetMetricProperty(cadence, "RecentFrameIntervalsMs")));
    }

    [Fact]
    public async Task HealthSnapshotObservesCurrentSessionSettingsAndArrivalAge()
    {
        const BindingFlags instanceFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var assembly = Sussudio.Tests.SussudioAssembly.Load();
        var captureType = assembly.GetType("Sussudio.Services.Capture.CaptureService", throwOnError: true)!;
        var unifiedType = assembly.GetType("Sussudio.Services.Capture.UnifiedVideoCapture", throwOnError: true)!;
        var settingsType = assembly.GetType("Sussudio.Models.CaptureSettings", throwOnError: true)!;
        await using var capture = (IAsyncDisposable)Activator.CreateInstance(captureType)!;
        await using var unified = (IAsyncDisposable)Activator.CreateInstance(unifiedType, nonPublic: true)!;
        var pipeline = captureType.GetField("_videoPipeline", instanceFlags)!.GetValue(capture)!;
        var ownedCapture = pipeline.GetType().GetProperty("Capture", instanceFlags)!;
        var settingsField = captureType.GetField("_currentSettings", instanceFlags)!;
        var settings = Activator.CreateInstance(settingsType)!;
        var formatProperty = settingsType.GetProperty("Format")!;
        var priorState = GetMetricProperty(capture, "SessionState");
        captureType.GetMethod("EnterFaultedState", instanceFlags)!.Invoke(capture, null);
        var currentState = GetMetricProperty(capture, "SessionState");
        Assert.NotEqual(priorState, currentState);
        Assert.Equal("Faulted", currentState!.ToString());

        try
        {
            ownedCapture.SetValue(pipeline, unified);
            settingsField.SetValue(capture, settings);
            var arrivalTick = Math.Max(1, Environment.TickCount64 - 50);
            unifiedType.GetField("_lastVideoFrameArrivedTick", instanceFlags)!.SetValue(unified, arrivalTick);
            foreach (var format in Enum.GetValues(formatProperty.PropertyType))
            {
                formatProperty.SetValue(settings, format);
                var beforeTick = Environment.TickCount64;
                var beforeSnapshotMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var health = captureType.GetMethod("GetHealthSnapshot")!.Invoke(capture, null)!;
                var afterSnapshotMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var afterTick = Environment.TickCount64;

                Assert.Equal(currentState, GetMetricProperty(health, "SessionState"));
                Assert.Equal(format.ToString(), GetMetricProperty(health, "FlashbackExportVerificationFormat"));
                Assert.InRange((long)GetMetricProperty(health, "LastFrameArrivalMs")!,
                    Math.Max(0, beforeTick - arrivalTick), Math.Max(0, afterTick - arrivalTick));
                Assert.InRange(((DateTimeOffset)GetMetricProperty(health, "TimestampUtc")!).ToUnixTimeMilliseconds(),
                    beforeSnapshotMs, afterSnapshotMs);
            }
        }
        finally
        {
            ownedCapture.SetValue(pipeline, null);
            settingsField.SetValue(capture, null);
        }
    }

    private static object? GetMetricProperty(object value, string name)
        => value.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!.GetValue(value);

    [Fact]
    public void CaptureService_HealthSnapshotAssemblyFields_LiveWithHealthSampler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var getHealthSnapshotText = ExtractMemberCode(healthSnapshotText, "GetHealthSnapshot");
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");

        Assert.Contains("return CaptureHealthSnapshotAssembler.Build(new CaptureHealthSnapshotAssemblyFields", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private static class CaptureHealthSnapshotAssembler", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public static CaptureHealthSnapshot Build(", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("private readonly record struct CaptureHealthSnapshotAssemblyFields", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public CaptureCadenceHealthSnapshotFields CaptureCadence { get; init; }", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("public FlashbackPlaybackHealthSnapshotFields FlashbackPlayback { get; init; }", healthSnapshotText, StringComparison.Ordinal);
        Assert.DoesNotContain("LibAvRecordingSink? Sink", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.DoesNotContain("var sink = fields.Sink;", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.DoesNotContain("UnifiedVideoCapture? UnifiedVideoCapture", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.DoesNotContain("_sessionState", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.DoesNotContain("_isRecording", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.DoesNotContain("_currentSettings", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.DoesNotContain("ComputeTickAge(", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.DoesNotContain("return new CaptureHealthSnapshot", getHealthSnapshotText, StringComparison.Ordinal);

    }

    [Fact]
    public void CaptureService_HealthSnapshotCaptureCadenceFields_LiveWithSampler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");

        Assert.Contains("var captureCadence = BuildCaptureCadenceHealthSnapshotFields(unifiedVideoCapture);", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("CaptureCadenceSampleCount = captureCadence.SampleCount,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("CaptureCadenceEstimatedDropPercent = captureCadence.EstimatedDropPercent,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("private static CaptureCadenceHealthSnapshotFields BuildCaptureCadenceHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private readonly record struct CaptureCadenceHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("unifiedVideoCapture?.GetSourceCadenceMetrics()", healthSnapshotText, StringComparison.Ordinal);

    }

    [Fact]
    public void CaptureService_HealthSnapshotMjpegFields_LiveWithSampler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");
        var videoPipelineResourcesText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");

        Assert.Contains("var mjpegHealth = CaptureMjpegHealthSnapshotFields(unifiedVideoCapture);", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("MjpegDecodeSampleCount = mjpegHealth.Timing.DecodeSampleCount,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("MjpegPreviewJitterEnabled = mjpegHealth.PreviewJitter.Enabled,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("VisualCadenceSampleCount = mjpegHealth.VisualCadence.SampleCount,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("MjpegPerDecoder = mjpegHealth.PerDecoder,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("private MjpegHealthSnapshotFields CaptureMjpegHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private readonly record struct MjpegHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("_videoPipeline.GetMjpegTimingSnapshot(unifiedVideoCapture)", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("GetMjpegPipelineTimingSnapshot()", videoPipelineResourcesText, StringComparison.Ordinal);
        Assert.Contains("GetMjpegPreviewJitterMetrics()", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("GetPreviewVisualCadenceMetrics()", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("FrameFingerprintCadenceTracker.Empty", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("new MjpegDecoderHealthSnapshot(", healthSnapshotText, StringComparison.Ordinal);

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

        Assert.Contains("var avSyncHealth = CaptureAvSyncHealthSnapshotFields();", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("AvSyncCaptureDriftMs = avSyncHealth.CaptureDriftMs,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("AvSyncCaptureDriftRateMsPerSec = avSyncHealth.CaptureDriftRateMsPerSec,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("AvSyncEncoderCorrectionSamples = avSyncHealth.EncoderCorrectionSamples", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.DoesNotContain("var (avSyncDriftMs, avSyncDriftRate) = ComputeAvSyncDrift();", healthSnapshotText, StringComparison.Ordinal);
        Assert.DoesNotContain("var (avSyncEncoderDriftMs, avSyncEncoderCorrectionSamples) = GetEncoderAvSyncDrift();", healthSnapshotText, StringComparison.Ordinal);

        Assert.Contains("private AvSyncHealthSnapshotFields CaptureAvSyncHealthSnapshotFields()", avSyncSnapshotText, StringComparison.Ordinal);
        Assert.Contains("var (captureDriftMs, captureDriftRateMsPerSec) = GetAvSyncDrift();", avSyncSnapshotText, StringComparison.Ordinal);
        Assert.Contains("var (encoderDriftMs, encoderCorrectionSamples) = GetEncoderAvSyncDrift();", avSyncSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private readonly record struct AvSyncHealthSnapshotFields", avSyncSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private double _avSyncBaselineDriftMs = double.NaN;", avSyncSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private double _avSyncPrevDriftMs;", avSyncSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private long _avSyncPrevDriftTick;", avSyncSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private double _avSyncDriftRateMsPerSec;", avSyncSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private void ResetAvSyncDriftBaseline()", avSyncSnapshotText, StringComparison.Ordinal);
        Assert.DoesNotContain("_avSyncBaselineDriftMs", rootText, StringComparison.Ordinal);
        Assert.DoesNotContain("_avSyncPrevDriftMs", rootText, StringComparison.Ordinal);
        Assert.DoesNotContain("_avSyncPrevDriftTick", rootText, StringComparison.Ordinal);
        Assert.DoesNotContain("_avSyncDriftRateMsPerSec", rootText, StringComparison.Ordinal);

    }

    [Fact]
    public void CaptureService_HealthSnapshotFlashbackExportFields_LiveWithExportDiagnostics()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");
        var flashbackExportStateText = ReadRepoFile("Sussudio/Services/Flashback/FlashbackExportState.cs")
            .Replace("\r\n", "\n");

        Assert.Contains("var flashbackExport = _flashbackExport.CaptureHealthSnapshotFields(snapshotUtcUnixMs);", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("FlashbackExportActive = flashbackExport.Active,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("FlashbackExportElapsedMs = flashbackExport.ElapsedMs,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("FlashbackExportThroughputBytesPerSec = flashbackExport.ThroughputBytesPerSec,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("LastExportId = flashbackExport.LastResultId,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.DoesNotContain("lock (_flashbackExportDiagnosticsLock)", healthSnapshotText, StringComparison.Ordinal);
        Assert.DoesNotContain("ComputeElapsedMs(", healthSnapshotText, StringComparison.Ordinal);
        Assert.DoesNotContain("GetFileLengthOrZero(", healthSnapshotText, StringComparison.Ordinal);

        Assert.Contains("public HealthSnapshotFields CaptureHealthSnapshotFields(", flashbackExportStateText, StringComparison.Ordinal);
        Assert.Contains("lock (_flashbackExportDiagnosticsLock)", flashbackExportStateText, StringComparison.Ordinal);
        Assert.Contains("public static long ComputeElapsedMs(", flashbackExportStateText, StringComparison.Ordinal);
        Assert.Contains("public static long ComputeLastProgressAgeMs(", flashbackExportStateText, StringComparison.Ordinal);
        Assert.Contains("public static long GetFileLengthOrZero(string? path)", flashbackExportStateText, StringComparison.Ordinal);
        Assert.Contains("var elapsedMs = ComputeElapsedMs(", flashbackExportStateText, StringComparison.Ordinal);
        Assert.Contains("var lastProgressAgeMs = ComputeLastProgressAgeMs(", flashbackExportStateText, StringComparison.Ordinal);
        Assert.Contains("var outputBytes = GetFileLengthOrZero(", flashbackExportStateText, StringComparison.Ordinal);
        Assert.Contains("ThroughputBytesPerSec = throughputBytesPerSec", flashbackExportStateText, StringComparison.Ordinal);
        Assert.Contains("FinalizeResult? LastResult", flashbackExportStateText, StringComparison.Ordinal);

    }

    [Fact]
    public void CaptureService_HealthSnapshotFlashbackBufferFields_LiveWithHealthSampler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");

        Assert.Contains("var flashbackBuffer = CaptureFlashbackBufferHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("FlashbackBufferedDurationMs = flashbackBuffer.BufferedDurationMs,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("FlashbackBackendSettingsStaleReason = flashbackBuffer.BackendSettingsStaleReason,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("EncoderTargetBitRate = flashbackBuffer.EncoderTargetBitRate,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("private FlashbackBufferHealthSnapshotFields CaptureFlashbackBufferHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("ResolveFlashbackBackendSettingsStaleReason(flashbackBackendSettings, currentSettings)", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private static string ResolveFlashbackBackendSettingsStaleReason(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("bufMgr?.StartupCacheOverBudget ?? false", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("fbSink?.EncoderFrameRateDenominator", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private readonly record struct FlashbackBufferHealthSnapshotFields", healthSnapshotText, StringComparison.Ordinal);

    }

    [Fact]
    public void CaptureService_HealthSnapshotFlashbackQueueFields_LiveWithHealthSampler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");

        Assert.Contains("var flashbackQueues = CaptureFlashbackQueueHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("FlashbackVideoQueueDepth = flashbackQueues.VideoQueueDepth,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("FlashbackForceRotateActive = flashbackQueues.ForceRotateActive,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("FlashbackVideoQueueLatencyP99Ms = flashbackQueues.VideoQueueLatencyMetrics.P99Ms,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("FlashbackGpuQueueLastRejectReason = flashbackQueues.GpuQueueLastRejectReason,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("private static FlashbackQueueHealthSnapshotFields CaptureFlashbackQueueHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("fbSink?.VideoQueueOldestFrameAgeMs ?? 0", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("fbSink?.IsForceRotateActive ?? false", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("fbSink?.LastGpuQueueRejectReason ?? string.Empty", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private readonly record struct FlashbackQueueHealthSnapshotFields", healthSnapshotText, StringComparison.Ordinal);

    }

    [Fact]
    public void CaptureService_HealthSnapshotFlashbackPlaybackFields_LiveWithHealthSampler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");

        Assert.Contains("var flashbackPlayback = CaptureFlashbackPlaybackHealthSnapshotFields(fbPlayback);", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("FlashbackPlaybackState = flashbackPlayback.State,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("FlashbackPlaybackDecodeP95Ms = flashbackPlayback.DecodeP95Ms,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("FlashbackPlaybackLastCommandFailure = flashbackPlayback.LastCommandFailure,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("private static FlashbackPlaybackHealthSnapshotFields CaptureFlashbackPlaybackHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("var state = CaptureFlashbackPlaybackStateHealthSnapshotFields(fbPlayback);", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("var cadence = CaptureFlashbackPlaybackCadenceHealthSnapshotFields(fbPlayback);", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("var decode = CaptureFlashbackPlaybackDecodeHealthSnapshotFields(fbPlayback);", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("var audioMaster = CaptureFlashbackPlaybackAudioMasterHealthSnapshotFields(fbPlayback);", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("var commands = CaptureFlashbackPlaybackCommandHealthSnapshotFields(fbPlayback);", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private readonly record struct FlashbackPlaybackHealthSnapshotFields", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private static FlashbackPlaybackStateHealthSnapshotFields CaptureFlashbackPlaybackStateHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private readonly record struct FlashbackPlaybackStateHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("fbPlayback?.State,", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("fbPlayback?.PlaybackFrameCount ?? 0", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("fbPlayback?.PlaybackThreadAlive ?? false", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private static FlashbackPlaybackCadenceHealthSnapshotFields CaptureFlashbackPlaybackCadenceHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private readonly record struct FlashbackPlaybackCadenceHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private static FlashbackPlaybackDecodeHealthSnapshotFields CaptureFlashbackPlaybackDecodeHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private readonly record struct FlashbackPlaybackDecodeHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("fbPlayback?.GetPlaybackDecodeMetrics() ?? default", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("fbPlayback?.PlaybackMaxDecodePhase ?? string.Empty", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private static FlashbackPlaybackAudioMasterHealthSnapshotFields CaptureFlashbackPlaybackAudioMasterHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private readonly record struct FlashbackPlaybackAudioMasterHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("fbPlayback?.PlaybackAudioMasterFallbacks ?? 0", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private static FlashbackPlaybackCommandHealthSnapshotFields CaptureFlashbackPlaybackCommandHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private readonly record struct FlashbackPlaybackCommandHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("fbPlayback?.CommandsEnqueued ?? 0", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("double[] RecentFrameIntervalsMs", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("string LastCommandFailure", healthSnapshotText, StringComparison.Ordinal);

    }

    [Fact]
    public void CaptureService_HealthSnapshotRecordingFields_LiveWithHealthSampler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");

        Assert.Contains("var recordingHealth = CaptureRecordingHealthSnapshotFields(sink, fbSink);", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("RecordingEncodingFailed = recordingHealth.EncodingFailed,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("RecordingVideoQueueLatencyP95Ms = recordingHealth.VideoQueueLatencyMetrics.P95Ms,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("RecordingCudaQueueDepth = recordingHealth.CudaQueueDepth,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("RecordingCudaQueueCapacity = recordingHealth.CudaQueueCapacity,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("RecordingCudaQueueMaxDepth = recordingHealth.CudaQueueMaxDepth,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("RecordingCudaFramesEnqueued = recordingHealth.CudaFramesEnqueued,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("RecordingCudaFramesDropped = recordingHealth.CudaFramesDropped,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.DoesNotContain("RecordingCudaQueueDepth = sink?.CudaQueueCount ?? 0,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.DoesNotContain("RecordingCudaFramesDropped = sink?.CudaFramesDropped ?? 0,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("private RecordingHealthSnapshotFields CaptureRecordingHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("GetLastFailureTelemetry()", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("IsFlashbackRecordingBackendOwnedByRecording()", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("CaptureActiveRecordingBackendHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("activeRecording.FlashbackVideoQueueLatencyMetrics", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("sink?.CudaQueueCount ?? 0", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("sink?.CudaQueueCapacityFrames ?? 0", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("sink?.CudaQueueMaxDepth ?? 0", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("sink?.CudaFramesEnqueued ?? 0", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("sink?.CudaFramesDropped ?? 0", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("int CudaQueueDepth", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("long CudaFramesDropped", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private readonly record struct RecordingHealthSnapshotFields", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private ActiveRecordingBackendHealthSnapshotFields CaptureActiveRecordingBackendHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("var flashbackVideoQueueLatencyMetrics", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("sink?.VideoQueueLatencyMetrics ??", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("flashbackIsRecordingBackend ? fbSink?.VideoQueueCount ?? 0 : 0", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("Interlocked.Read(ref _videoFramesDropped)", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private readonly record struct ActiveRecordingBackendHealthSnapshotFields", healthSnapshotText, StringComparison.Ordinal);

    }

    [Fact]
    public void CaptureService_HealthSnapshotSourceTelemetryFields_LiveWithHealthSampler()
    {
        var healthSnapshotText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.HealthSnapshots.cs")
            .Replace("\r\n", "\n");
        var healthSnapshotAssemblerText = ExtractMemberCode(healthSnapshotText, "Build");

        Assert.Contains("var sourceTelemetry = CaptureSourceTelemetryHealthSnapshotFields(_latestSourceTelemetry);", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("SourceTelemetryAvailability = sourceTelemetry.Availability,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("SourceTelemetryBackend = sourceTelemetry.Backend,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("SourceTelemetryCircuitState = sourceTelemetry.CircuitState,", healthSnapshotAssemblerText, StringComparison.Ordinal);
        Assert.Contains("private SourceTelemetryHealthSnapshotFields CaptureSourceTelemetryHealthSnapshotFields(", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("ResolveSourceTelemetrySuppressedReason(telemetry) ?? string.Empty", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("ResolveSourceTelemetryBackend(telemetry)", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("ResolveSourceTelemetryCircuitState(telemetry.Availability, suppressed)", healthSnapshotText, StringComparison.Ordinal);
        Assert.Contains("private readonly record struct SourceTelemetryHealthSnapshotFields", healthSnapshotText, StringComparison.Ordinal);

    }

    internal static string ExtractMemberCode(string source, string memberName)
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
        var cleanupText = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");

        Assert.Contains("private readonly object _recordingFailureTelemetryLock = new();", cleanupText, StringComparison.Ordinal);
        Assert.Contains("private bool _lastRecordingEncodingFailed;", cleanupText, StringComparison.Ordinal);
        Assert.Contains("private string? _lastRecordingEncodingFailureType;", cleanupText, StringComparison.Ordinal);
        Assert.Contains("private string? _lastRecordingEncodingFailureMessage;", cleanupText, StringComparison.Ordinal);
        Assert.Contains("private bool _lastFlashbackEncodingFailed;", cleanupText, StringComparison.Ordinal);
        Assert.Contains("private string? _lastFlashbackEncodingFailureType;", cleanupText, StringComparison.Ordinal);
        Assert.Contains("private string? _lastFlashbackEncodingFailureMessage;", cleanupText, StringComparison.Ordinal);
        Assert.Contains("private void RecordLastRecordingFailure(Exception ex)", cleanupText, StringComparison.Ordinal);
        Assert.Contains("private void RecordLastFlashbackFailure(Exception ex)", cleanupText, StringComparison.Ordinal);
        Assert.Contains("private void ClearLastRecordingFailure()", cleanupText, StringComparison.Ordinal);
        Assert.Contains("private void ClearLastFlashbackFailure()", cleanupText, StringComparison.Ordinal);
        Assert.Contains("private void BeginFatalCaptureCleanup(Exception ex)", cleanupText, StringComparison.Ordinal);
        Assert.Contains("EnterCleanupState();", cleanupText, StringComparison.Ordinal);
        Assert.Contains("EnterFaultedState();", cleanupText, StringComparison.Ordinal);
        Assert.Contains("GetLastFailureTelemetry()", cleanupText, StringComparison.Ordinal);
    }

    [Fact]
    public void CaptureService_FlashbackBackendFailureCleanup_LivesWithCleanupLifecycleWithoutSessionStateWrites()
    {
        var source = ReadRepoFile("Sussudio/Services/Capture/CaptureService.cs")
            .Replace("\r\n", "\n");
        var cleanupText = CaptureServiceHealthSnapshotOwnershipTests.ExtractMemberCode(
            source,
            "private void BeginFlashbackBackendCleanup");

        Assert.Contains("private static bool IsGpuDeviceLost(Exception ex)", source, StringComparison.Ordinal);
        Assert.Contains("_flashbackBackend.PreserveRecoverySegments(\"backend_fatal\");", cleanupText, StringComparison.Ordinal);
        Assert.DoesNotContain("_sessionState =", cleanupText, StringComparison.Ordinal);
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
        long loadShedDrops = 0,
        long decodeFailures = 0,
        long reorderCollisions = 0,
        long emitFailures = 0,
        int compressedQueueDepth = 0,
        long compressedQueueBytes = 0,
        long compressedQueueByteBudget = 0,
        int peakReorderDepth = 0,
        long peakCompressedQueueBytes = 0,
        long reorderRingForceDrops = 0)
    {
        var type = RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline+PipelineTimingMetrics");
        var perDecoderArray = Array.CreateInstance(
            RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline+PerDecoderMetrics"),
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
                   loadShedDrops,
                   decodeFailures,
                   reorderCollisions,
                   emitFailures,
                   compressedQueueDepth,
                   compressedQueueBytes,
                   compressedQueueByteBudget,
                   reorderSkips,
                   reorderBufferDepth,
                   peakReorderDepth,
                   peakCompressedQueueBytes,
                   reorderRingForceDrops,
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
        var type = RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline+PerDecoderMetrics");
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
        AssertDoesNotContain(rootText, "ResetObservedPixelTelemetry();");
        AssertContains(rootText, "ResetCachedMjpegTimingMetrics();");
        AssertContains(rootText, "_latestSourceTelemetry = BuildFallbackTelemetry();");
        AssertContains(rootText, "await RefreshSourceTelemetryAsync(transitionToken).ConfigureAwait(false);");
        AssertContains(rootText, "TryCorrectFrameRateFromTelemetry();");
        AssertContains(rootText, "StatusChanged?.Invoke(this, \"Initialized\");");
        AssertContains(telemetryText, "private SourceSignalTelemetrySnapshot BuildFallbackTelemetry()");
        AssertContains(telemetryText, "private static SourceSignalTelemetrySnapshot MergeTelemetryWithFallback(");
        AssertContains(telemetryText, "private void TryCorrectFrameRateFromTelemetry()");
        AssertContains(telemetryText, "private static string ResolveFrameRateArg(");
        AssertContains(telemetryText, "private void SetActualCaptureFrameRate(");
        AssertContains(telemetryText, "private static (int? Numerator, int? Denominator, double EffectiveFrameRate) ResolveCaptureDeliveryFrameRateParts(");
        foreach (var path in new[] { "Sussudio/Services/Capture/CaptureService.PreviewLifecycle.cs", "Sussudio/Services/Capture/CaptureService.RecordingLifecycle.cs" })
        {
            var source = ReadRepoFile(path);
            AssertContains(source, "SetActualCaptureFrameRate(settings, unifiedVideoCapture.Fps > 0 ? unifiedVideoCapture.Fps :");
            AssertDoesNotContain(source, "TryCorrectFrameRateFromTelemetry();");
        }
        AssertContains(telemetryText, "private void CaptureEncoderRuntimeTelemetry(");

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
        AssertContains(transitionExecutionText, "public long SessionGeneration => CaptureSnapshotProducerEpoch();");
        AssertContains(transitionExecutionText, "private long CaptureSnapshotProducerEpoch()");
        AssertOccursBefore(
            transitionExecutionText,
            "lock (_captureSnapshotProducerEpochLock)",
            "var signature = BuildCaptureSnapshotProducerSignature();");
        AssertContains(transitionExecutionText, "private CaptureSnapshotProducerSignature BuildCaptureSnapshotProducerSignature()");
        AssertContains(transitionExecutionText, "_isRecording,");
        AssertContains(transitionExecutionText, "_isVideoPreviewActive,");
        AssertContains(transitionExecutionText, "var recordingOutcome = CaptureRecordingOutcomeSnapshot();");
        AssertContains(transitionExecutionText, "recordingOutcome.OutputPath,");
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
        AssertContains(cleanupText, "public ValueTask DisposeAsync()");
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

        return Task.CompletedTask;
    }

    internal static Task CaptureService_SnapshotProducerEpoch_AdvancesWhenRecordingStateChanges()
    {
        var captureService = CreateInstance("Sussudio.Services.Capture.CaptureService");
        var sessionGenerationProperty = captureService.GetType().GetProperty("SessionGeneration", BindingFlags.Instance | BindingFlags.Public)
            ?? throw new InvalidOperationException("CaptureService.SessionGeneration not found.");

        var initialEpoch = Convert.ToInt64(sessionGenerationProperty.GetValue(captureService));
        SetPrivateField(captureService, "_isRecording", true);
        var recordingEpoch = Convert.ToInt64(sessionGenerationProperty.GetValue(captureService));

        AssertEqual(
            true,
            recordingEpoch > initialEpoch,
            "Capture snapshot producer epoch should advance when _isRecording changes inside an active transition.");
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
        AssertContains(audioGraphText, "private async Task<WasapiAudioCapture?> StartPreviewAudioGraphAsync(");
        AssertContains(audioGraphText, "private async Task StartPreviewMicrophoneMonitorAsync(");
        AssertContains(audioGraphText, "private async Task RollbackPreviewAudioCaptureStartupAsync(");
        AssertContains(stopText, "public Task StopVideoPreviewAsync(CancellationToken cancellationToken = default)");
        AssertContains(stopText, "private Task StopVideoPreviewCoreAsync(bool teardownPipeline, CancellationToken cancellationToken = default)");
        AssertContains(stopText, "private async Task DisposePreviewPipelineAsync(");
        AssertContains(videoPipelineResourcesText, "internal sealed class CaptureVideoPipelineResources");
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
        AssertDoesNotContain(freshPipelineText, "new WasapiAudioCapture()");
        AssertDoesNotContain(freshPipelineText, "micCapture.AudioLevelUpdated += OnMicrophoneAudioLevelUpdated;");

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
        AssertContains(resourceText, "public WasapiAudioCapture? ProgramCapture;");
        AssertContains(resourceText, "public WasapiAudioCapture? MicrophoneCapture;");
        AssertContains(resourceText, "public WasapiAudioPlayback? Playback;");
        AssertContains(resourceText, "public float PreviewVolume = 1.0f;");
        AssertContains(resourceText, "private PreviewAudioCaptureFaultSnapshot? _captureFault;");
        AssertContains(resourceText, "public void RecordCaptureFault(");
        AssertContains(resourceText, "public PreviewAudioCaptureFaultSnapshot ConsumeCaptureFault()");
        AssertContains(resourceText, "Interlocked.Exchange(ref _captureFault, null)");
        AssertDoesNotContain(resourceText, "private bool _captureFaulted;");
        AssertDoesNotContain(resourceText, "private string? _captureFaultMessage;");
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

        AssertContains(resourceText, "public async Task StartPlaybackAsync(");
        AssertContains(resourceText, "public void StopPlayback(");
        AssertContains(resourceText, "public void DetachCapture(");
        AssertContains(resourceText, "private static void SafeClearCapturePlayback(");
        AssertContains(resourceText, "private static void DisposePlaybackBestEffort(");

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
        AssertContains(microphoneRootText, "_previewAudioGraph.AttachCaptureFailure(micCapture, \"microphone\", OnWasapiCaptureFailed);");
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
        AssertDoesNotContain(finalizationText, "_previewAudioGraph.AttachCaptureFailure(micCapture,");

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
