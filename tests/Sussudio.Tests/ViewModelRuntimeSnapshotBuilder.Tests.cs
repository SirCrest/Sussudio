using System;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class ViewModelRuntimeSnapshotBuilderTests
{
    [Fact]
    public void Build_PreservesViewModelRuntimeSnapshotContract()
    {
        var asm = SussudioAssembly.Load();
        var builderType = asm.GetType("Sussudio.ViewModels.ViewModelRuntimeSnapshotBuilder", throwOnError: true)!;
        var inputType = asm.GetType("Sussudio.ViewModels.ViewModelRuntimeSnapshotInput", throwOnError: true)!;
        var sessionSnapshotType = asm.GetType("Sussudio.Services.Capture.CaptureSessionSnapshot", throwOnError: true)!;
        var commandOutcomeType = asm.GetType("Sussudio.Services.Capture.CaptureCommandOutcome", throwOnError: true)!;

        var timestamp = new DateTimeOffset(2026, 5, 16, 12, 0, 10, TimeSpan.Zero);
        var telemetryTimestamp = timestamp.AddSeconds(-12);
        var sessionSnapshot = CreateInput(sessionSnapshotType,
            ("CommandsEnqueued", 11L),
            ("CommandsCompleted", 7L),
            ("CommandsFailed", 2L),
            ("CommandsCanceled", 1L),
            ("CommandsCoalesced", 3L),
            ("PendingCommands", 4),
            ("MaxPendingCommands", 6),
            ("OldestPendingCommandAgeMs", 123L),
            ("LastCommandQueueLatencyMs", 45L),
            ("MaxCommandQueueLatencyMs", 67L),
            ("LastOutcome", Enum.Parse(commandOutcomeType, "Completed")));

        var input = CreateInput(inputType,
            ("TimestampUtc", timestamp),
            ("SessionSnapshot", sessionSnapshot),
            ("IsInitialized", true),
            ("IsPreviewing", true),
            ("IsRecording", false),
            ("IsAudioEnabled", true),
            ("IsAudioPreviewEnabled", true),
            ("IsCustomAudioInputEnabled", false),
            ("StatusText", "Ready"),
            ("SelectedDeviceId", "device-1"),
            ("SelectedDeviceName", "Device One"),
            ("SelectedAudioInputDeviceId", "audio-1"),
            ("SelectedAudioInputDeviceName", "Audio One"),
            ("SelectedResolution", "3840x2160"),
            ("SelectedFrameRate", 119.88d),
            ("SelectedFriendlyFrameRate", 120d),
            ("SelectedExactFrameRate", 119.88d),
            ("SelectedExactFrameRateArg", "120000/1001"),
            ("DisabledResolutionReason", "resolution reason"),
            ("DisabledFrameRateReason", "frame reason"),
            ("HdrResolutionSupportHint", "HDR OK"),
            ("DetectedSourceFrameRate", 119.88d),
            ("DetectedSourceFrameRateArg", "120000/1001"),
            ("SourceFrameRateOrigin", "Telemetry"),
            ("SourceWidth", 3840),
            ("SourceHeight", 2160),
            ("SourceIsHdr", true),
            ("SourceTelemetryAvailability", "Available"),
            ("SourceTelemetryOriginDetail", "Native"),
            ("SourceTelemetryConfidence", "High"),
            ("SourceTelemetryDiagnosticSummary", "clean"),
            ("SourceTelemetryTimestampUtc", telemetryTimestamp),
            ("SourceTelemetrySummaryText", "source summary"),
            ("SourceTargetSummaryText", "target summary"),
            ("SelectedRecordingFormat", "HEVC"),
            ("SelectedQuality", "High"),
            ("SelectedPreset", "P5"),
            ("SelectedSplitEncodeMode", "Auto"),
            ("SelectedVideoFormat", "MJPG"),
            ("CustomBitrateMbps", 42d),
            ("ShowAllCaptureOptions", true),
            ("PreviewVolume", 0.375d),
            ("IsStatsVisible", true),
            ("IsHdrAvailable", true),
            ("IsHdrEnabled", true),
            ("HdrRuntimeState", "Active"),
            ("HdrReadinessReason", "ready"),
            ("LiveResolution", "3840x2160"),
            ("LiveFrameRate", "119.88"),
            ("LivePixelFormat", "P010"),
            ("OutputPath", "C:\\Capture"),
            ("RecordingTime", "00:01:02"),
            ("RecordingSizeInfo", "10 MB"),
            ("RecordingBitrateInfo", "100 Mbps"),
            ("AudioPeak", 0.75d),
            ("AudioClipping", true));

        var build = builderType.GetMethod("Build", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        var snapshot = build.Invoke(null, new[] { input })!;

        Assert.Equal(timestamp, Get(snapshot, "TimestampUtc"));
        Assert.True((bool)Get(snapshot, "IsInitialized")!);
        Assert.True((bool)Get(snapshot, "IsPreviewing")!);
        Assert.Equal("Ready", Get(snapshot, "StatusText"));
        Assert.Equal("device-1", Get(snapshot, "SelectedDeviceId"));
        Assert.Equal("3840x2160", Get(snapshot, "SelectedResolution"));
        Assert.Equal(119.88d, Get(snapshot, "SelectedFrameRate"));
        Assert.Equal("120000/1001", Get(snapshot, "SelectedExactFrameRateArg"));
        Assert.Equal(true, Get(snapshot, "SourceIsHdr"));
        Assert.Equal(12, Get(snapshot, "SourceTelemetryAgeSeconds"));
        Assert.Equal("source summary", Get(snapshot, "SourceTelemetrySummaryText"));
        Assert.Equal("HEVC", Get(snapshot, "SelectedRecordingFormat"));
        Assert.Equal(37.5d, Get(snapshot, "PreviewVolumePercent"));
        Assert.True((bool)Get(snapshot, "IsStatsVisible")!);
        Assert.Equal("Active", Get(snapshot, "HdrRuntimeState"));
        Assert.Equal("P010", Get(snapshot, "LivePixelFormat"));
        Assert.Equal("C:\\Capture", Get(snapshot, "OutputPath"));
        Assert.Equal("00:01:02", Get(snapshot, "RecordingTime"));
        Assert.Equal(0.75d, Get(snapshot, "AudioPeak"));
        Assert.True((bool)Get(snapshot, "AudioClipping")!);

        Assert.Equal(11L, Get(snapshot, "CaptureCommandCommandsEnqueued"));
        Assert.Equal(7L, Get(snapshot, "CaptureCommandCommandsCompleted"));
        Assert.Equal(2L, Get(snapshot, "CaptureCommandCommandsFailed"));
        Assert.Equal(1L, Get(snapshot, "CaptureCommandCommandsCanceled"));
        Assert.Equal(3L, Get(snapshot, "CaptureCommandCommandsCoalesced"));
        Assert.Equal(4, Get(snapshot, "CaptureCommandPendingCommands"));
        Assert.Equal(6, Get(snapshot, "CaptureCommandMaxPendingCommands"));
        Assert.Equal(123L, Get(snapshot, "CaptureCommandOldestPendingCommandAgeMs"));
        Assert.Equal(45L, Get(snapshot, "CaptureCommandLastQueueLatencyMs"));
        Assert.Equal(67L, Get(snapshot, "CaptureCommandMaxQueueLatencyMs"));
        Assert.Equal("None", Get(snapshot, "CaptureCommandLastCommand"));
        Assert.Equal("Completed", Get(snapshot, "CaptureCommandLastOutcome"));
        Assert.Equal(string.Empty, Get(snapshot, "CaptureCommandLastCorrelationId"));
        Assert.Equal(string.Empty, Get(snapshot, "CaptureCommandLastError"));
    }

    private static object CreateInput(Type type, params (string Property, object? Value)[] values)
    {
        var instance = Activator.CreateInstance(type)
                       ?? throw new InvalidOperationException($"Failed to create {type.FullName}.");
        foreach (var (property, value) in values)
        {
            Set(instance, property, value);
        }

        return instance;
    }

    private static void Set(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                       ?? throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} was not found.");
        property.SetValue(instance, value);
    }

    private static object? Get(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                       ?? throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} was not found.");
        return property.GetValue(instance);
    }
}
