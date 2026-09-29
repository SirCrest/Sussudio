using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Xunit;

namespace Sussudio.Tests;

public sealed class SnapshotModelsTests
{
    [Fact]
    public void Sussudio_Models_SourceSignalTelemetrySnapshot_RoundTripsValuesAndJson()
    {
        var asm = SussudioAssembly.Load();
        var snapshotType = asm.GetType("Sussudio.Models.SourceSignalTelemetrySnapshot", throwOnError: true)!;
        var detailType = asm.GetType("Sussudio.Models.SourceTelemetryDetailEntry", throwOnError: true)!;
        var detailEntry = Activator.CreateInstance(detailType, "Audio / Input", "Analog Gain", "12 dB", "0C")!;
        var details = CreateGenericList(detailType, detailEntry);
        var snapshot = Activator.CreateInstance(snapshotType)!;

        SetProperty(snapshot, "Availability", ParseEnum(asm, "Sussudio.Models.SourceTelemetryAvailability", "Available"));
        SetProperty(snapshot, "Origin", ParseEnum(asm, "Sussudio.Models.SourceTelemetryOrigin", "NativeXu"));
        SetProperty(snapshot, "OriginDetail", "NativeXuAtCommandProvider");
        SetProperty(snapshot, "Confidence", ParseEnum(asm, "Sussudio.Models.SourceTelemetryConfidence", "High"));
        SetProperty(snapshot, "Width", 3840);
        SetProperty(snapshot, "Height", 2160);
        SetProperty(snapshot, "FrameRateExact", 120000d / 1001d);
        SetProperty(snapshot, "FrameRateArg", "120000/1001");
        SetProperty(snapshot, "IsHdr", true);
        SetProperty(snapshot, "VideoFormat", "YCbCr422");
        SetProperty(snapshot, "Colorimetry", "BT.2020");
        SetProperty(snapshot, "Quantization", "Limited");
        SetProperty(snapshot, "HdrTransferFunction", "HDR10 / PQ");
        SetProperty(snapshot, "HdrTransferCode", 2);
        SetProperty(snapshot, "Firmware", "1.2.3");
        SetProperty(snapshot, "AudioFormat", "PCM");
        SetProperty(snapshot, "AudioSampleRate", "48 kHz");
        SetProperty(snapshot, "InputSource", "HDMI");
        SetProperty(snapshot, "AdcOnOff", true);
        SetProperty(snapshot, "AdcVolumeGain", 12);
        SetProperty(snapshot, "AnalogGainByte", 0x0C);
        SetProperty(snapshot, "UacVolumeGain", 24);
        SetProperty(snapshot, "UacOut1Mute", false);
        SetProperty(snapshot, "UacOut2Mute", true);
        SetProperty(snapshot, "UacOut2MixerSource", 1);
        SetProperty(snapshot, "UsbHostProtocol", "Isochronous");
        SetProperty(snapshot, "TxEdidValid", true);
        SetProperty(snapshot, "HdcpMode", "Off");
        SetProperty(snapshot, "HdcpVersion", "0200");
        SetProperty(snapshot, "RxTxHdcpVersion", "0200/0200");
        SetProperty(snapshot, "CustomerVersion", "custom-a");
        SetProperty(snapshot, "RescueVersion", 7);
        SetProperty(snapshot, "RawTimingHex", "3000CA0830117008");
        SetProperty(snapshot, "DetailEntries", details);
        SetProperty(snapshot, "DiagnosticSummary", "ok");
        SetProperty(snapshot, "EgavInitializeResultName", "Ok");
        SetProperty(snapshot, "EgavOpenResultName", "Ok");
        SetProperty(snapshot, "EgavSignalStatusResultName", "Ok");
        SetProperty(snapshot, "EgavIsVideoHdrResultName", "Ok");
        SetProperty(snapshot, "AudioInputAvailability", ParseEnum(asm, "Sussudio.Models.SourceAudioInputAvailability", "Available"));
        SetProperty(snapshot, "AudioInputMode", ParseEnum(asm, "Sussudio.Models.SourceAudioInputMode", "Analog"));
        SetProperty(snapshot, "AudioInputOrigin", "native-xu");

        var roundTripDetail = ((IEnumerable)GetPropertyValue(snapshot, "DetailEntries")!).Cast<object>().Single();
        Assert.Equal("Available", GetPropertyValue(snapshot, "Availability")!.ToString());
        Assert.Equal("NativeXu", GetPropertyValue(snapshot, "Origin")!.ToString());
        Assert.Equal("NativeXuAtCommandProvider", (string)GetPropertyValue(snapshot, "OriginDetail")!);
        Assert.Equal(3840, (int)GetPropertyValue(snapshot, "Width")!);
        Assert.Equal("YCbCr422", (string)GetPropertyValue(snapshot, "VideoFormat")!);
        Assert.Equal("HDR10 / PQ", (string)GetPropertyValue(snapshot, "HdrTransferFunction")!);
        Assert.Equal("PCM", (string)GetPropertyValue(snapshot, "AudioFormat")!);
        Assert.True((bool)GetPropertyValue(snapshot, "AdcOnOff")!);
        Assert.Equal("0200/0200", (string)GetPropertyValue(snapshot, "RxTxHdcpVersion")!);
        Assert.Equal("Audio / Input", (string)GetPropertyValue(roundTripDetail, "Group")!);
        Assert.Equal("Analog Gain", (string)GetPropertyValue(roundTripDetail, "Label")!);
        Assert.Equal("12 dB", (string)GetPropertyValue(roundTripDetail, "DisplayValue")!);
        Assert.Equal("0C", (string)GetPropertyValue(roundTripDetail, "RawValue")!);
        Assert.Equal("Available", GetPropertyValue(snapshot, "AudioInputAvailability")!.ToString());
        Assert.Equal("Analog", GetPropertyValue(snapshot, "AudioInputMode")!.ToString());
        Assert.Equal("native-xu", (string)GetPropertyValue(snapshot, "AudioInputOrigin")!);
        Assert.True((bool)GetPropertyValue(snapshot, "HasDimensions")!);
        Assert.True((bool)GetPropertyValue(snapshot, "HasFrameRate")!);
        Assert.True((bool)GetPropertyValue(snapshot, "HasSignalData")!);
        Assert.Equal("3840x2160@120000/1001:hdr", InvokeInstanceMethod(snapshot, "GetModeKey"));

        var detailJsonRoundTrip = JsonRoundTrip(detailEntry, detailType);
        Assert.Equal("Analog Gain", (string)GetPropertyValue(detailJsonRoundTrip, "Label")!);
        var snapshotJsonRoundTrip = JsonRoundTrip(snapshot, snapshotType);
        Assert.Equal("NativeXuAtCommandProvider", (string)GetPropertyValue(snapshotJsonRoundTrip, "OriginDetail")!);
        Assert.Equal("YCbCr422", (string)GetPropertyValue(snapshotJsonRoundTrip, "VideoFormat")!);
        Assert.Equal("PCM", (string)GetPropertyValue(snapshotJsonRoundTrip, "AudioFormat")!);
        var jsonDetail = ((IEnumerable)GetPropertyValue(snapshotJsonRoundTrip, "DetailEntries")!).Cast<object>().Single();
        Assert.Equal("Analog Gain", (string)GetPropertyValue(jsonDetail, "Label")!);
    }

    private static object? GetPropertyValue(object instance, string name)
        => instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!.GetValue(instance);

    private static void SetProperty(object instance, string name, object? value)
        => instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)!.SetValue(instance, value);

    private static void SetPropertyOrBackingField(object instance, string name, object? value)
    {
        var property = instance.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = instance.GetType().GetField($"<{name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"{instance.GetType().Name}.{name} backing field not found.");
        field.SetValue(instance, value);
    }

    private static object? InvokeInstanceMethod(object instance, string name)
        => instance.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance)!.Invoke(instance, Array.Empty<object>());

    private static object ParseEnum(Assembly asm, string typeName, string value)
        => Enum.Parse(asm.GetType(typeName, throwOnError: true)!, value);

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static object CreateGenericList(Type itemType, params object[] items)
    {
        var listType = typeof(List<>).MakeGenericType(itemType);
        var list = (IList)Activator.CreateInstance(listType)!;
        foreach (var item in items)
        {
            list.Add(item);
        }

        return list;
    }

    private static object JsonRoundTrip(object instance, Type type)
    {
        var json = JsonSerializer.Serialize(instance, type);
        return JsonSerializer.Deserialize(json, type)
            ?? throw new InvalidOperationException($"{type.FullName} JSON round trip returned null.");
    }

    [Theory]
    [InlineData("Disabled")]
    [InlineData("Buffering")]
    [InlineData("Live")]
    [InlineData("Scrubbing")]
    [InlineData("Playing")]
    [InlineData("Paused")]
    [InlineData(null)]
    public void CaptureHealthPlaybackStatePreservesTypedAndTextBoundaries(string? stateName)
    {
        var healthType = RequireType("Sussudio.Models.CaptureHealthSnapshot");
        var stateType = RequireType("Sussudio.Models.FlashbackPlaybackState");
        var health = Activator.CreateInstance(healthType)!;
        var state = stateName == null ? null : Enum.Parse(stateType, stateName);
        SetPropertyOrBackingField(health, "FlashbackPlaybackState", state);
        Assert.Equal(state, GetPropertyValue(health, "FlashbackPlaybackState"));
        Assert.Equal(stateType, Nullable.GetUnderlyingType(healthType.GetProperty("FlashbackPlaybackState")!.PropertyType));

        var expectedText = stateName ?? "N/A";
        var json = JsonSerializer.Serialize(health, healthType);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(expectedText, document.RootElement.GetProperty("FlashbackPlaybackState").GetString());
        var restored = JsonSerializer.Deserialize(json, healthType)!;
        Assert.Equal(state, GetPropertyValue(restored, "FlashbackPlaybackState"));

        var automation = AutomationSnapshotRegressionFixture.BuildResult(healthType.Assembly, populated: false, healthOverride: health);
        Assert.Equal(807, automation.EnumerateObject().Count());
        Assert.Equal(expectedText, automation.GetProperty("FlashbackPlaybackState").GetString());
        var hubType = healthType.Assembly.GetType("Sussudio.Services.Automation.AutomationDiagnosticsHub", throwOnError: true)!;
        var laneMethod = hubType.GetMethod("BuildFlashbackPlaybackPerformanceLane", BindingFlags.Static | BindingFlags.NonPublic)!;
        var preview = Activator.CreateInstance(laneMethod.GetParameters()[1].ParameterType)!;
        var lane = (string)laneMethod.Invoke(null, new[] { health, preview, (object)120d })!;
        Assert.StartsWith($"playback perf state={expectedText} fps=", lane, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"N/A\"")]
    public void CaptureHealthPlaybackStateReadsAbsentAndLegacyNull(string valueJson)
    {
        var healthType = RequireType("Sussudio.Models.CaptureHealthSnapshot");
        var restored = JsonSerializer.Deserialize($"{{\"FlashbackPlaybackState\":{valueJson}}}", healthType)!;
        Assert.Null(GetPropertyValue(restored, "FlashbackPlaybackState"));
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(restored, healthType));
        Assert.Equal("N/A", document.RootElement.GetProperty("FlashbackPlaybackState").GetString());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("999")]
    [InlineData("true")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("\"\"")]
    [InlineData("\"Unknown\"")]
    [InlineData("\"playing\"")]
    [InlineData("\" Playing \"")]
    [InlineData("\"0\"")]
    [InlineData("\"999\"")]
    public void CaptureHealthPlaybackStateRejectsUndefinedJson(string valueJson)
    {
        var healthType = RequireType("Sussudio.Models.CaptureHealthSnapshot");
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize($"{{\"FlashbackPlaybackState\":{valueJson}}}", healthType));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public void CaptureHealthPlaybackStateRejectsUndefinedEnumWrites(int value)
    {
        var healthType = RequireType("Sussudio.Models.CaptureHealthSnapshot");
        var health = Activator.CreateInstance(healthType)!;
        SetPropertyOrBackingField(health, "FlashbackPlaybackState", Enum.ToObject(RequireType("Sussudio.Models.FlashbackPlaybackState"), value));
        Assert.Throws<JsonException>(() => JsonSerializer.Serialize(health, healthType));
    }
}

public sealed class ViewModelBuildersTests
{
    [Fact]
    public void Build_PreservesAutomationOptionsSnapshotContract()
    {
        var asm = SussudioAssembly.Load();
        var builderType = asm.GetType("Sussudio.ViewModels.AutomationOptionsSnapshotBuilder", throwOnError: true)!;
        var inputType = asm.GetType("Sussudio.ViewModels.AutomationOptionsSnapshotInput", throwOnError: true)!;
        var deviceInputType = asm.GetType("Sussudio.ViewModels.AutomationOptionsDeviceInput", throwOnError: true)!;
        var resolutionInputType = asm.GetType("Sussudio.ViewModels.AutomationOptionsResolutionInput", throwOnError: true)!;
        var frameRateInputType = asm.GetType("Sussudio.ViewModels.AutomationOptionsFrameRateInput", throwOnError: true)!;

        var timestamp = new DateTimeOffset(2026, 5, 16, 1, 2, 3, TimeSpan.Zero);
        var input = CreateInput(inputType,
            ("TimestampUtc", timestamp),
            ("Devices", InputArray(deviceInputType,
                CreateInput(deviceInputType, ("Id", "device-a"), ("Name", "Device A")),
                CreateInput(deviceInputType, ("Id", "DEVICE-B"), ("Name", "Device B")))),
            ("AudioInputDevices", InputArray(deviceInputType,
                CreateInput(deviceInputType, ("Id", "audio-a"), ("Name", "Audio A")))),
            ("MicrophoneDevices", InputArray(deviceInputType,
                CreateInput(deviceInputType, ("Id", "mic-a"), ("Name", "Mic A")),
                CreateInput(deviceInputType, ("Id", "MIC-B"), ("Name", "Mic B")))),
            ("Resolutions", InputArray(resolutionInputType,
                CreateInput(resolutionInputType,
                    ("Value", "1920x1080"),
                    ("Width", 1920u),
                    ("Height", 1080u),
                    ("IsEnabled", true),
                    ("DisableReason", null)),
                CreateInput(resolutionInputType,
                    ("Value", "3840x2160"),
                    ("Width", 3840u),
                    ("Height", 2160u),
                    ("IsEnabled", false),
                    ("DisableReason", "Unavailable")))),
            ("FrameRates", InputArray(frameRateInputType,
                CreateInput(frameRateInputType,
                    ("Value", 59.94d),
                    ("FriendlyValue", 60d),
                    ("ExactValueArg", "60000/1001"),
                    ("IsEnabled", true),
                    ("DisableReason", null),
                    ("IsSelected", false)),
                CreateInput(frameRateInputType,
                    ("Value", 60d),
                    ("FriendlyValue", 60d),
                    ("ExactValueArg", null),
                    ("IsEnabled", false),
                    ("DisableReason", null),
                    ("IsSelected", true)))),
            ("RecordingFormats", new[] { "H264", "AV1" }),
            ("Qualities", new[] { "High", "Medium" }),
            ("Presets", new[] { "Quality", "Speed" }),
            ("SplitEncodeModes", new[] { "Auto", "Disabled" }),
            ("VideoFormats", new[] { "Auto", "MJPG" }),
            ("FlashbackBufferMinuteOptions", new[] { 1, 2, 5, 10, 15, 30 }),
            ("SelectedDeviceId", "device-b"),
            ("SelectedAudioInputDeviceId", "AUDIO-A"),
            ("SelectedMicrophoneDeviceId", "mic-b"),
            ("SelectedResolution", "1920X1080"),
            ("SelectedFrameRate", 60d),
            ("SelectedRecordingFormat", "av1"),
            ("SelectedQuality", "medium"),
            ("SelectedPreset", "speed"),
            ("SelectedSplitEncodeMode", "disabled"),
            ("SelectedVideoFormat", "mjpg"),
            ("MjpegDecoderCount", 99),
            ("PreviewVolume", 0.425d),
            ("IsMicrophoneEnabled", true),
            ("MicrophoneVolume", 62.5d),
            ("FlashbackBufferMinutes", 10),
            ("FlashbackGpuDecode", true),
            ("IsFlashbackEnabled", false),
            ("IsStatsVisible", true));

        var build = builderType.GetMethod("Build", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        var snapshot = build.Invoke(null, new[] { input })!;

        Assert.Equal(timestamp, Get(snapshot, "TimestampUtc"));
        Assert.Equal("device-b", Get(snapshot, "SelectedDeviceId"));
        Assert.Equal("AUDIO-A", Get(snapshot, "SelectedAudioInputDeviceId"));
        Assert.Equal("mic-b", Get(snapshot, "SelectedMicrophoneDeviceId"));
        Assert.Equal("1920X1080", Get(snapshot, "SelectedResolution"));
        Assert.Equal(60d, Get(snapshot, "SelectedFrameRate"));
        Assert.Equal(8, Get(snapshot, "MjpegDecoderCount"));
        Assert.Equal(42.5d, Get(snapshot, "PreviewVolumePercent"));
        Assert.True((bool)Get(snapshot, "IsMicrophoneEnabled")!);
        Assert.Equal(62.5d, Get(snapshot, "MicrophoneVolumePercent"));
        Assert.Equal(10, Get(snapshot, "FlashbackBufferMinutes"));
        Assert.True((bool)Get(snapshot, "FlashbackGpuDecode")!);
        Assert.False((bool)Get(snapshot, "IsFlashbackEnabled")!);
        Assert.True((bool)Get(snapshot, "IsStatsVisible")!);

        var devices = (Array)Get(snapshot, "Devices")!;
        Assert.False((bool)Get(devices.GetValue(0)!, "IsSelected")!);
        Assert.True((bool)Get(devices.GetValue(1)!, "IsSelected")!);

        var audioDevices = (Array)Get(snapshot, "AudioInputDevices")!;
        Assert.True((bool)Get(audioDevices.GetValue(0)!, "IsSelected")!);

        var microphoneDevices = (Array)Get(snapshot, "MicrophoneDevices")!;
        Assert.False((bool)Get(microphoneDevices.GetValue(0)!, "IsSelected")!);
        Assert.True((bool)Get(microphoneDevices.GetValue(1)!, "IsSelected")!);

        var resolutions = (Array)Get(snapshot, "Resolutions")!;
        Assert.Equal(string.Empty, Get(resolutions.GetValue(0)!, "DisableReason"));
        Assert.True((bool)Get(resolutions.GetValue(0)!, "IsSelected")!);
        Assert.Equal(3840, Get(resolutions.GetValue(1)!, "Width"));
        Assert.Equal("Unavailable", Get(resolutions.GetValue(1)!, "DisableReason"));

        var frameRates = (Array)Get(snapshot, "FrameRates")!;
        Assert.Equal("60000/1001", Get(frameRates.GetValue(0)!, "ExactValueArg"));
        Assert.Equal(string.Empty, Get(frameRates.GetValue(0)!, "DisableReason"));
        Assert.Equal(string.Empty, Get(frameRates.GetValue(1)!, "ExactValueArg"));
        Assert.True((bool)Get(frameRates.GetValue(1)!, "IsSelected")!);

        var recordingFormats = (Array)Get(snapshot, "RecordingFormats")!;
        Assert.True((bool)Get(recordingFormats.GetValue(1)!, "IsSelected")!);
        Assert.Equal(string.Empty, Get(recordingFormats.GetValue(1)!, "DisableReason"));

        var decoderCounts = (Array)Get(snapshot, "MjpegDecoderCounts")!;
        Assert.Equal(8, decoderCounts.Length);
        for (var i = 0; i < decoderCounts.Length; i++)
        {
            var option = decoderCounts.GetValue(i)!;
            Assert.Equal(i + 1, Get(option, "Value"));
            Assert.Equal(i == 7, (bool)Get(option, "IsSelected")!);
        }

        var flashbackBufferMinuteOptions = (Array)Get(snapshot, "FlashbackBufferMinuteOptions")!;
        Assert.Equal(6, flashbackBufferMinuteOptions.Length);
        Assert.Equal(10, Get(flashbackBufferMinuteOptions.GetValue(3)!, "Value"));
        Assert.True((bool)Get(flashbackBufferMinuteOptions.GetValue(3)!, "IsSelected")!);
    }

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
            ("SessionGeneration", 42L),
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
            ("SourceTelemetryEpoch", 99L),
            ("SourceTelemetrySummaryText", "source summary"),
            ("SourceTargetSummaryText", "target summary"),
            ("SelectedRecordingFormat", "HEVC"),
            ("SelectedQuality", "High"),
            ("SelectedPreset", "P5"),
            ("SelectedSplitEncodeMode", "Auto"),
            ("SelectedVideoFormat", "MJPG"),
            ("CustomBitrateMbps", 42d),
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
        Assert.Equal(42L, Get(snapshot, "CaptureSessionEpoch"));
        Assert.Equal(99L, Get(snapshot, "SourceTelemetryEpoch"));
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

    [Fact]
    public void LiveSignalTextProjection_PreservesPixelFormatFallbackOrder()
    {
        var runtimeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelLifecycleController.cs");
        var capturePresentationText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs");
        var liveSignalText = ReadRepoFile("Sussudio/ViewModels/ViewModelBuilders.cs");
        var builderType = RequireType("Sussudio.ViewModels.LiveSignalTextPresentationBuilder");
        var snapshotType = RequireType("Sussudio.Models.CaptureRuntimeSnapshot");
        var buildMethod = builderType.GetMethod("Build", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build was not found.");

        Assert.Contains("_context.UpdateLiveCaptureInfo(runtimeSnapshot);", runtimeLifecycleControllerText, StringComparison.Ordinal);
        Assert.Contains("_context.ResetLiveCaptureInfo();", runtimeLifecycleControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("IsAudioPreviewActive =", runtimeLifecycleControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("private void UpdateLiveCaptureInfo(", runtimeLifecycleControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("private void ResetLiveCaptureInfo()", runtimeLifecycleControllerText, StringComparison.Ordinal);
        Assert.Contains("private void UpdateLiveCaptureInfo(CaptureRuntimeSnapshot? runtimeSnapshot = null)", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("IsAudioPreviewActive = runtime.IsAudioPreviewActive;", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("var liveSignalText = LiveSignalTextPresentationBuilder.Build(", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("_captureService.EncoderCodecName,", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("LiveInfoUnavailable);", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("LiveResolution = liveSignalText.Resolution;", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("LiveFrameRate = liveSignalText.FrameRate;", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("LivePixelFormat = liveSignalText.PixelFormat;", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("private void ResetLiveCaptureInfo()", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("partial void OnIsPreviewingChanged(bool value)", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("if (!value && !IsRecording)", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("IsAudioPreviewActive = false;", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("LiveResolution = LiveInfoUnavailable;", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("LiveFrameRate = LiveInfoUnavailable;", capturePresentationText, StringComparison.Ordinal);
        Assert.Contains("LivePixelFormat = LiveInfoUnavailable;", capturePresentationText, StringComparison.Ordinal);
        Assert.DoesNotContain("runtime.ReaderSourceSubtype ??", runtimeLifecycleControllerText, StringComparison.Ordinal);
        Assert.DoesNotContain("runtime.LatestObservedFramePixelFormat ??", runtimeLifecycleControllerText, StringComparison.Ordinal);
        Assert.Contains("internal static class LiveSignalTextPresentationBuilder", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("internal static LiveSignalTextPresentation Build(", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("runtime.ActualWidth ?? runtime.NegotiatedWidth ?? runtime.RequestedWidth", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("runtime.ActualHeight ?? runtime.NegotiatedHeight ?? runtime.RequestedHeight", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate ?? runtime.RequestedFrameRate", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("frameRateValue.Value.ToString(\"0.00\")", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("runtime.ReaderSourceSubtype ??", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("runtime.LatestObservedFramePixelFormat ??", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("\"hevc_nvenc\" => \" → HEVC\"", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("\"h264_nvenc\" => \" → H264\"", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("\"av1_nvenc\" => \" → AV1\"", liveSignalText, StringComparison.Ordinal);
        Assert.Contains("? unavailableText", liveSignalText, StringComparison.Ordinal);
        Assert.True(
            liveSignalText.IndexOf("runtime.ReaderSourceSubtype ??", StringComparison.Ordinal) <
            liveSignalText.IndexOf("runtime.LatestObservedFramePixelFormat ??", StringComparison.Ordinal),
            "MainViewModel.LivePixelFormat should prefer ReaderSourceSubtype before LatestObservedFramePixelFormat.");

        var actualRuntime = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create CaptureRuntimeSnapshot.");
        SetPropertyOrBackingField(actualRuntime, "RequestedWidth", (uint?)1280);
        SetPropertyOrBackingField(actualRuntime, "RequestedHeight", (uint?)720);
        SetPropertyOrBackingField(actualRuntime, "NegotiatedWidth", (uint?)1920);
        SetPropertyOrBackingField(actualRuntime, "NegotiatedHeight", (uint?)1080);
        SetPropertyOrBackingField(actualRuntime, "ActualWidth", (uint?)3840);
        SetPropertyOrBackingField(actualRuntime, "ActualHeight", (uint?)2160);
        SetPropertyOrBackingField(actualRuntime, "RequestedFrameRate", 30d);
        SetPropertyOrBackingField(actualRuntime, "NegotiatedFrameRate", 59.94d);
        SetPropertyOrBackingField(actualRuntime, "ActualFrameRate", 119.88d);
        SetPropertyOrBackingField(actualRuntime, "RequestedPixelFormat", "MJPG");
        SetPropertyOrBackingField(actualRuntime, "RequestedReaderSubtype", "YUY2");
        SetPropertyOrBackingField(actualRuntime, "LatestObservedFramePixelFormat", "P010");
        SetPropertyOrBackingField(actualRuntime, "NegotiatedPixelFormat", "NV12");
        SetPropertyOrBackingField(actualRuntime, "VideoNegotiatedSubtype", "I420");
        SetPropertyOrBackingField(actualRuntime, "ReaderSourceSubtype", "RGB32");

        var actualPresentation = buildMethod.Invoke(null, new object?[] { actualRuntime, "av1_nvenc", "\u2014" })
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build returned null.");
        Assert.Equal("3840x2160", GetStringProperty(actualPresentation, "Resolution"));
        Assert.Equal("119.88", GetStringProperty(actualPresentation, "FrameRate"));
        Assert.Equal("RGB32 → AV1", GetStringProperty(actualPresentation, "PixelFormat"));

        var fallbackRuntime = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create fallback CaptureRuntimeSnapshot.");
        SetPropertyOrBackingField(fallbackRuntime, "RequestedWidth", (uint?)1280);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedHeight", (uint?)720);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedFrameRate", 30d);
        SetPropertyOrBackingField(fallbackRuntime, "VideoNegotiatedSubtype", null);
        SetPropertyOrBackingField(fallbackRuntime, "NegotiatedPixelFormat", null);
        SetPropertyOrBackingField(fallbackRuntime, "LatestObservedFramePixelFormat", null);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedReaderSubtype", null);
        SetPropertyOrBackingField(fallbackRuntime, "RequestedPixelFormat", "MJPG");

        var fallbackPresentation = buildMethod.Invoke(null, new object?[] { fallbackRuntime, "hevc_nvenc", "\u2014" })
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build returned null.");
        Assert.Equal("1280x720", GetStringProperty(fallbackPresentation, "Resolution"));
        Assert.Equal("30.00", GetStringProperty(fallbackPresentation, "FrameRate"));
        Assert.Equal("MJPG → HEVC", GetStringProperty(fallbackPresentation, "PixelFormat"));

        var unavailablePresentation = buildMethod.Invoke(
                null,
                new object?[] { CreateLiveSignalUnavailableRuntime(snapshotType), "libx264", "\u2014" })
            ?? throw new InvalidOperationException("LiveSignalTextPresentationBuilder.Build returned null.");
        Assert.Equal("\u2014", GetStringProperty(unavailablePresentation, "Resolution"));
        Assert.Equal("\u2014", GetStringProperty(unavailablePresentation, "FrameRate"));
        Assert.Equal("\u2014", GetStringProperty(unavailablePresentation, "PixelFormat"));
    }

    [Fact]
    public void SourceTelemetryPresentationBuilder_PreservesSummaryAndTargetText()
    {
        var builderType = RequireType("Sussudio.ViewModels.SourceTelemetryPresentationBuilder");
        var snapshotType = RequireType("Sussudio.Models.SourceSignalTelemetrySnapshot");
        var buildSourceSummary = builderType.GetMethod("BuildSourceSummary", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SourceTelemetryPresentationBuilder.BuildSourceSummary was not found.");
        var buildAgeText = builderType.GetMethod("BuildAgeText", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SourceTelemetryPresentationBuilder.BuildAgeText was not found.");
        var buildTargetSummary = builderType.GetMethod("BuildTargetSummary", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SourceTelemetryPresentationBuilder.BuildTargetSummary was not found.");

        var now = new DateTimeOffset(2026, 5, 14, 22, 10, 30, TimeSpan.Zero);
        var unavailable = snapshotType.GetMethod(
            "CreateUnavailable",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(string), typeof(string) },
            modifiers: null)!.Invoke(null, new object?[] { "telemetry-not-started", null })!;
        Assert.Equal(
            "Source: waiting for signal telemetry",
            buildSourceSummary.Invoke(null, new[] { unavailable, now }));

        var full = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create SourceSignalTelemetrySnapshot.");
        SetPropertyOrBackingField(full, "Availability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Available"));
        SetPropertyOrBackingField(full, "Confidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "High"));
        SetPropertyOrBackingField(full, "Width", 3840);
        SetPropertyOrBackingField(full, "Height", 2160);
        SetPropertyOrBackingField(full, "FrameRateExact", 120000d / 1001d);
        SetPropertyOrBackingField(full, "FrameRateArg", "120000/1001");
        SetPropertyOrBackingField(full, "IsHdr", true);
        SetPropertyOrBackingField(full, "TimestampUtc", now.AddSeconds(-17));
        Assert.Equal(
            "Source: 3840x2160 @ 120000/1001 | HDR | Available/High | updated 17s ago",
            buildSourceSummary.Invoke(null, new[] { full, now }));

        var partial = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create partial SourceSignalTelemetrySnapshot.");
        SetPropertyOrBackingField(partial, "Availability", ParseEnum("Sussudio.Models.SourceTelemetryAvailability", "Stale"));
        SetPropertyOrBackingField(partial, "Confidence", ParseEnum("Sussudio.Models.SourceTelemetryConfidence", "Low"));
        SetPropertyOrBackingField(partial, "FrameRateExact", 59.94d);
        SetPropertyOrBackingField(partial, "TimestampUtc", now.AddSeconds(2));
        Assert.Equal(
            "Source: ?x? @ 59.94 | HDR? | Stale/Low | updated now",
            buildSourceSummary.Invoke(null, new[] { partial, now }));

        Assert.Equal("updated ?", buildAgeText.Invoke(null, new object?[] { null, now }));
        Assert.Equal(
            "Target: Auto (3840 x 2160) @ 60 (exact 60000/1001) | HDR=Ready",
            buildTargetSummary.Invoke(null, new object?[] { "Auto (3840 x 2160)", 59.94d, 60d, 60000d / 1001d, "60000/1001", "Ready" }));
        Assert.Equal(
            "Target: 1080p @ 0 (exact ?) | HDR=Unknown",
            buildTargetSummary.Invoke(null, new object?[] { "1080p", 0d, null, null, null, " " }));
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

    private static Array InputArray(Type elementType, params object[] values)
    {
        var array = Array.CreateInstance(elementType, values.Length);
        for (var i = 0; i < values.Length; i++)
        {
            array.SetValue(values[i], i);
        }

        return array;
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

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static object ParseEnum(string typeName, string value)
        => Enum.Parse(RequireType(typeName), value);

    private static void SetPropertyOrBackingField(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Backing field for {propertyName} was not found.");
        field.SetValue(instance, value);
    }

    private static string GetStringProperty(object instance, string propertyName)
        => Get(instance, propertyName) as string
           ?? throw new InvalidOperationException($"{propertyName} was not a string.");

    private static object CreateLiveSignalUnavailableRuntime(Type snapshotType)
    {
        var runtime = Activator.CreateInstance(snapshotType)
            ?? throw new InvalidOperationException("Failed to create unavailable CaptureRuntimeSnapshot.");
        SetPropertyOrBackingField(runtime, "VideoNegotiatedSubtype", null);
        SetPropertyOrBackingField(runtime, "NegotiatedPixelFormat", null);
        SetPropertyOrBackingField(runtime, "LatestObservedFramePixelFormat", null);
        SetPropertyOrBackingField(runtime, "RequestedReaderSubtype", null);
        SetPropertyOrBackingField(runtime, "RequestedPixelFormat", null);
        return runtime;
    }

    private static string ReadRepoFile(string relativePath)
        => RuntimeContractSource.ReadRepoFile(relativePath).Replace("\r\n", "\n");
}

public sealed class CaptureConfigurationModelsTests
{
    private enum SetterExpectation
    {
        Set,
        InitOnly,
        None
    }

    private enum NullabilityExpectation
    {
        NotApplicable,
        NotNull,
        Nullable
    }

    private enum PropertyScope
    {
        Instance,
        Static
    }

    private sealed record PropertySpec(
        string Name,
        Type Type,
        SetterExpectation Setter,
        NullabilityExpectation Nullability = NullabilityExpectation.NotApplicable,
        NullabilityExpectation ElementNullability = NullabilityExpectation.NotApplicable,
        PropertyScope Scope = PropertyScope.Instance,
        bool IsRequired = false);


    private static PropertySpec Property(
        string name,
        Type type,
        SetterExpectation setter,
        NullabilityExpectation nullability = NullabilityExpectation.NotApplicable,
        NullabilityExpectation elementNullability = NullabilityExpectation.NotApplicable,
        PropertyScope scope = PropertyScope.Instance,
        bool isRequired = false)
        => new(name, type, setter, nullability, elementNullability, scope, isRequired);

    private static PropertySpec String(string name, SetterExpectation setter, NullabilityExpectation nullability)
        => Property(name, typeof(string), setter, nullability);

    private static PropertySpec RequiredString(string name, SetterExpectation setter)
        => Property(name, typeof(string), setter, NullabilityExpectation.NotNull, isRequired: true);

    private static PropertySpec RequiredProperty(string name, Type type, SetterExpectation setter)
        => Property(name, type, setter, isRequired: true);

    private static void AssertDeclaredProperties(Type type, IReadOnlyCollection<PropertySpec> expectedProperties)
    {
        var instanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
        var staticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;
        var actualNames = type.GetProperties(instanceFlags | staticFlags)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var expectedNames = expectedProperties
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expectedNames, actualNames);

        foreach (var expected in expectedProperties)
        {
            var flags = expected.Scope == PropertyScope.Static ? staticFlags : instanceFlags;
            var property = type.GetProperty(expected.Name, flags);
            Assert.NotNull(property);
            Assert.Equal(expected.Type, property!.PropertyType);
            Assert.Equal(expected.IsRequired, property.GetCustomAttribute<RequiredMemberAttribute>() != null);
            Assert.NotNull(property.GetMethod);
            Assert.True(property.GetMethod!.IsPublic);

            if (expected.Setter == SetterExpectation.None)
            {
                Assert.Null(property.SetMethod);
            }
            else
            {
                Assert.NotNull(property.SetMethod);
                Assert.True(property.SetMethod!.IsPublic);
                Assert.Equal(expected.Setter == SetterExpectation.InitOnly, IsInitOnlySetter(property));
            }

            AssertNullability(type, property, expected);
        }
    }

    private static void AssertNullability(Type type, PropertyInfo property, PropertySpec expected)
    {
        if (expected.Nullability == NullabilityExpectation.NotApplicable)
        {
            return;
        }

        var nullability = new NullabilityInfoContext().Create(property);
        var expectedState = expected.Nullability == NullabilityExpectation.Nullable
            ? NullabilityState.Nullable
            : NullabilityState.NotNull;
        Assert.Equal(expectedState, nullability.ReadState);
        if (expected.Setter != SetterExpectation.None)
        {
            Assert.Equal(expectedState, nullability.WriteState);
        }

        if (expected.ElementNullability == NullabilityExpectation.NotApplicable)
        {
            return;
        }

        var elementNullability = property.PropertyType.IsArray
            ? nullability.ElementType
            : nullability.GenericTypeArguments.FirstOrDefault();
        Assert.NotNull(elementNullability);
        var expectedElementState = expected.ElementNullability == NullabilityExpectation.Nullable
            ? NullabilityState.Nullable
            : NullabilityState.NotNull;
        Assert.Equal(expectedElementState, elementNullability!.ReadState);
        Assert.Equal(expectedElementState, elementNullability.WriteState);
    }

    private static bool IsInitOnlySetter(PropertyInfo property)
        => property.SetMethod?.ReturnParameter.GetRequiredCustomModifiers()
            .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit") == true;

    private static Type RequireType(Assembly asm, string name)
        => asm.GetType(name, throwOnError: true)!;

    private static MethodInfo RequireMethod(Type type, string name, BindingFlags flags)
    {
        var method = type.GetMethod(name, flags);
        Assert.NotNull(method);
        return method!;
    }

    private static object CreateInstance(Type type)
        => Activator.CreateInstance(type, nonPublic: true)!;

    private static object? Get(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance | ReflectionFlags.Static)!.GetValue(instance);

    private static T Get<T>(object instance, string propertyName)
        => (T)Get(instance, propertyName)!;

    private static void Set(object instance, string propertyName, object? value)
    {
        var type = instance.GetType();
        var property = type.GetProperty(propertyName, ReflectionFlags.Instance | ReflectionFlags.Static);
        if (property?.SetMethod != null)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = type.GetField($"<{propertyName}>k__BackingField", ReflectionFlags.Instance | ReflectionFlags.Static)
            ?? type.GetField($"_{char.ToLowerInvariant(propertyName[0])}{propertyName[1..]}", ReflectionFlags.Instance | ReflectionFlags.Static)
            ?? type.GetField(propertyName, ReflectionFlags.Instance | ReflectionFlags.Static);
        Assert.NotNull(field);
        field!.SetValue(instance, value);
    }

    private static object? Invoke(object instance, string methodName)
        => RequireMethod(instance.GetType(), methodName, ReflectionFlags.Instance).Invoke(instance, Array.Empty<object>());

    private static object ParseEnum(Assembly asm, string typeName, string value)
        => Enum.Parse(RequireType(asm, typeName), value);

    private static void AssertEnumValues(Type enumType, params (string Name, int Value)[] expectedValues)
    {
        Assert.Equal(expectedValues.Length, Enum.GetNames(enumType).Length);
        foreach (var (name, value) in expectedValues)
        {
            Assert.Equal(value, Convert.ToInt32(Enum.Parse(enumType, name)));
        }
    }

    [Fact]
    public void CaptureModeOptions_PreserveDisplayTextAndMetadata()
    {
        var asm = SussudioAssembly.Load();
        var resolutionType = RequireType(asm, "Sussudio.Models.ResolutionOption");
        var frameRateType = RequireType(asm, "Sussudio.Models.FrameRateOption");

        AssertDeclaredProperties(
            resolutionType,
            new[]
            {
                RequiredString("Value", SetterExpectation.InitOnly),
                RequiredProperty("Width", typeof(uint), SetterExpectation.InitOnly),
                RequiredProperty("Height", typeof(uint), SetterExpectation.InitOnly),
                Property("IsEnabled", typeof(bool), SetterExpectation.InitOnly),
                String("DisableReason", SetterExpectation.InitOnly, NullabilityExpectation.NotNull),
                String("DisplayTextOverride", SetterExpectation.InitOnly, NullabilityExpectation.Nullable),
                String("DisplayText", SetterExpectation.None, NullabilityExpectation.NotNull)
            });
        AssertDeclaredProperties(
            frameRateType,
            new[]
            {
                RequiredProperty("FriendlyValue", typeof(double), SetterExpectation.InitOnly),
                RequiredProperty("Value", typeof(double), SetterExpectation.InitOnly),
                String("Rational", SetterExpectation.InitOnly, NullabilityExpectation.NotNull),
                Property("Numerator", typeof(uint?), SetterExpectation.InitOnly),
                Property("Denominator", typeof(uint?), SetterExpectation.InitOnly),
                Property("IsEnabled", typeof(bool), SetterExpectation.InitOnly),
                String("DisableReason", SetterExpectation.InitOnly, NullabilityExpectation.NotNull),
                String("DisplayTextOverride", SetterExpectation.InitOnly, NullabilityExpectation.Nullable),
                String("DisplayText", SetterExpectation.None, NullabilityExpectation.NotNull)
            });

        var resolution = CreateInstance(resolutionType);
        Set(resolution, "Value", "3840x2160");
        Set(resolution, "Width", 3840u);
        Set(resolution, "Height", 2160u);
        Assert.Equal("3840x2160", Get<string>(resolution, "DisplayText"));
        Assert.Equal(string.Empty, Get<string>(resolution, "DisableReason"));

        Set(resolution, "DisplayTextOverride", "4K UHD");
        Assert.Equal("4K UHD", Get<string>(resolution, "DisplayText"));
        Set(resolution, "DisplayTextOverride", "   ");
        Assert.Equal("3840x2160", Get<string>(resolution, "DisplayText"));

        var frameRate = CreateInstance(frameRateType);
        Set(frameRate, "FriendlyValue", 59.94d);
        Set(frameRate, "Value", 60000d / 1001d);
        Set(frameRate, "Rational", "60000/1001");
        Set(frameRate, "Numerator", 60000u);
        Set(frameRate, "Denominator", 1001u);
        Assert.Equal("60", Get<string>(frameRate, "DisplayText"));
        Set(frameRate, "DisplayTextOverride", "59.94");
        Assert.Equal("59.94", Get<string>(frameRate, "DisplayText"));
        Assert.Equal("60000/1001", Get<string>(frameRate, "Rational"));
    }

    [Fact]
    public void CaptureModeOptionsBuilder_BuildsResolutionAndVideoFormatOptions()
    {
        var asm = SussudioAssembly.Load();
        var builderType = RequireType(asm, "Sussudio.ViewModels.CaptureModeOptionsBuilder");
        var mediaFormatType = RequireType(asm, "Sussudio.Models.MediaFormat");
        var telemetryType = RequireType(asm, "Sussudio.Models.SourceSignalTelemetrySnapshot");
        var buildResolutionOptions = RequireMethod(builderType, "BuildResolutionOptions", ReflectionFlags.Static);
        var buildVideoFormatOptions = RequireMethod(builderType, "BuildVideoFormatOptions", ReflectionFlags.Static);

        var formatsByResolution = CreateResolutionFormatDictionary(mediaFormatType);
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "3840x2160",
            CreateMediaFormat(mediaFormatType, 3840, 2160, 60, "P010", isHdr: true));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1920x1080",
            CreateMediaFormat(mediaFormatType, 1920, 1080, 60, "NV12", isHdr: false));
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "1280x1024",
            CreateMediaFormat(mediaFormatType, 1280, 1024, 60, "P010", isHdr: true));

        var telemetry = CreateInstance(telemetryType);
        Set(telemetry, "Width", 1920);
        Set(telemetry, "Height", 1080);

        var filteredOptions = ((IEnumerable)buildResolutionOptions.Invoke(
                null,
                new object?[] { formatsByResolution, true, false, telemetry })!)
            .Cast<object>()
            .ToArray();
        Assert.Equal(2, filteredOptions.Length);
        Assert.Equal("3840x2160", Get<string>(filteredOptions[0], "Value"));
        Assert.True(Get<bool>(filteredOptions[0], "IsEnabled"));
        var sdrOnlyResolution = filteredOptions.Single(option => Get<string>(option, "Value") == "1920x1080");
        Assert.False(Get<bool>(sdrOnlyResolution, "IsEnabled"));
        Assert.Equal("HDR mode is not supported at this resolution.", Get<string>(sdrOnlyResolution, "DisableReason"));

        var unfilteredOptions = ((IEnumerable)buildResolutionOptions.Invoke(
                null,
                new object?[] { formatsByResolution, true, true, telemetry })!)
            .Cast<object>()
            .ToArray();
        Assert.Contains(unfilteredOptions, option => Get<string>(option, "Value") == "1280x1024");

        var videoFormats = CreateMediaFormatList(
            mediaFormatType,
            CreateMediaFormat(mediaFormatType, 3840, 2160, 120, "mjpg", isHdr: false),
            CreateMediaFormat(mediaFormatType, 3840, 2160, 120, "NV12", isHdr: false),
            CreateMediaFormat(mediaFormatType, 3840, 2160, 120, "nv12", isHdr: false),
            CreateMediaFormat(mediaFormatType, 3840, 2160, 60, "P010", isHdr: true),
            CreateMediaFormat(mediaFormatType, 3840, 2160, 60, " ", isHdr: false));
        var videoOptions = ((IEnumerable)buildVideoFormatOptions.Invoke(null, new object?[] { videoFormats })!)
            .Cast<string>()
            .ToArray();
        Assert.Equal(new[] { "Auto", "NV12", "MJPG", "P010" }, videoOptions);
    }

    [Theory]
    [InlineData(null, 1080, 1280u, 1024u, true)]
    [InlineData(1920, null, 1280u, 1024u, true)]
    [InlineData(0, 1080, 1280u, 1024u, true)]
    [InlineData(1920, 0, 1280u, 1024u, true)]
    [InlineData(-1920, 1080, 1280u, 1024u, true)]
    [InlineData(1920, 1080, 0u, 1024u, true)]
    [InlineData(1920, 1080, 1280u, 0u, true)]
    [InlineData(1920, 1080, 3840u, 2160u, true)]
    [InlineData(1920, 1080, 3840u, 2159u, false)]
    [InlineData(65536, 1, 65536u, 65537u, false)]
    [InlineData(int.MaxValue, int.MaxValue - 1, 4294967294u, 4294967292u, true)]
    [InlineData(int.MaxValue, int.MaxValue - 1, 4294967294u, 4294967293u, false)]
    [InlineData(int.MaxValue, int.MaxValue, uint.MaxValue, uint.MaxValue, true)]
    public void CaptureModeOptionsBuilder_PreservesAspectRatioBoundaryBehavior(
        int? sourceWidth,
        int? sourceHeight,
        uint optionWidth,
        uint optionHeight,
        bool expectedIncluded)
    {
        var asm = SussudioAssembly.Load();
        var builderType = RequireType(asm, "Sussudio.ViewModels.CaptureModeOptionsBuilder");
        var mediaFormatType = RequireType(asm, "Sussudio.Models.MediaFormat");
        var telemetryType = RequireType(asm, "Sussudio.Models.SourceSignalTelemetrySnapshot");
        var buildResolutionOptions = RequireMethod(builderType, "BuildResolutionOptions", ReflectionFlags.Static);
        var formatsByResolution = CreateResolutionFormatDictionary(mediaFormatType);
        AddResolutionFormats(
            formatsByResolution,
            mediaFormatType,
            "candidate",
            CreateMediaFormat(mediaFormatType, optionWidth, optionHeight, 60, "NV12", isHdr: false));
        var telemetry = CreateInstance(telemetryType);
        Set(telemetry, "Width", sourceWidth);
        Set(telemetry, "Height", sourceHeight);

        var options = ((IEnumerable)buildResolutionOptions.Invoke(
                null,
                new object?[] { formatsByResolution, false, false, telemetry })!)
            .Cast<object>()
            .ToArray();

        Assert.Equal(expectedIncluded, options.Length == 1);
    }

    [Fact]
    public void DeviceModeSupportPolicy_AppliesElgato4KXHdrUsbLimits()
    {
        var asm = SussudioAssembly.Load();
        var policyType = RequireType(asm, "Sussudio.ViewModels.DeviceModeSupportPolicy");
        var deviceType = RequireType(asm, "Sussudio.Models.CaptureDevice");
        var mediaFormatType = RequireType(asm, "Sussudio.Models.MediaFormat");
        var isSupported = RequireMethod(policyType, "IsSupported", ReflectionFlags.Static);
        var describeUnsupported = RequireMethod(policyType, "DescribeUnsupported", ReflectionFlags.Static);

        var device = CreateInstance(deviceType);
        Set(device, "Id", @"\\?\usb#vid_0fd9&pid_009b&mi_00#unit-test");
        Set(device, "Name", "Game Capture 4K X");

        Assert.True((bool)isSupported.Invoke(null, new[]
        {
            device,
            CreateMediaFormat(mediaFormatType, 3840, 2160, 30, "P010", isHdr: true)
        })!);
        Assert.True((bool)isSupported.Invoke(null, new[]
        {
            device,
            CreateMediaFormat(mediaFormatType, 2560, 1440, 60, "P010", isHdr: true)
        })!);
        Assert.False((bool)isSupported.Invoke(null, new[]
        {
            device,
            CreateMediaFormat(mediaFormatType, 3840, 2160, 120, "P010", isHdr: true)
        })!);
        Assert.False((bool)isSupported.Invoke(null, new[]
        {
            device,
            CreateMediaFormat(mediaFormatType, 2560, 1440, 120, "P010", isHdr: true)
        })!);
        Assert.True((bool)isSupported.Invoke(null, new[]
        {
            device,
            CreateMediaFormat(mediaFormatType, 3840, 2160, 120, "MJPG", isHdr: false)
        })!);

        var reason = (string)describeUnsupported.Invoke(null, new[]
        {
            device,
            CreateMediaFormat(mediaFormatType, 3840, 2160, 120, "P010", isHdr: true)
        })!;
        Assert.Contains("4K30 or 1440p60", reason);
    }

    [Fact]
    public void CaptureSettings_DefaultsAndOutputContracts()
    {
        var asm = SussudioAssembly.Load();
        var settingsType = RequireType(asm, "Sussudio.Models.CaptureSettings");
        var recordingFormatType = RequireType(asm, "Sussudio.Models.RecordingFormat");
        var videoQualityType = RequireType(asm, "Sussudio.Models.VideoQuality");
        var hdrOutputModeType = RequireType(asm, "Sussudio.Models.HdrOutputMode");
        var previewModeType = RequireType(asm, "Sussudio.Models.PreviewMode");
        var splitEncodeSupportType = RequireType(asm, "Sussudio.Models.SplitEncodeSupport");
        var nvencPresetType = RequireType(asm, "Sussudio.Models.NvencPreset");
        var splitEncodeModeType = RequireType(asm, "Sussudio.Models.SplitEncodeMode");

        AssertEnumValues(recordingFormatType, ("H264Mp4", 0), ("HevcMp4", 1), ("Av1Mp4", 2));
        AssertEnumValues(videoQualityType, ("Auto", 0), ("Low", 1), ("Medium", 2), ("High", 3), ("SuperHigh", 4), ("Custom", 5));
        AssertEnumValues(hdrOutputModeType, ("Off", 0), ("Hdr10Pq", 1));
        AssertEnumValues(previewModeType, ("GpuFast", 0), ("TrueHdr", 1));
        AssertEnumValues(nvencPresetType, ("Auto", 0), ("P1", 1), ("P2", 2), ("P3", 3), ("P4", 4), ("P5", 5), ("P6", 6), ("P7", 7), ("Fast", 8), ("Slow", 9));
        AssertEnumValues(splitEncodeModeType, ("Auto", 0), ("Disabled", 1), ("TwoWay", 2), ("ThreeWay", 3), ("ForcedOn", 4));

        AssertDeclaredProperties(
            settingsType,
            new[]
            {
                Property("Width", typeof(uint), SetterExpectation.Set),
                Property("Height", typeof(uint), SetterExpectation.Set),
                Property("FrameRate", typeof(double), SetterExpectation.Set),
                String("RequestedFrameRateArg", SetterExpectation.Set, NullabilityExpectation.Nullable),
                Property("RequestedFrameRateNumerator", typeof(uint?), SetterExpectation.Set),
                Property("RequestedFrameRateDenominator", typeof(uint?), SetterExpectation.Set),
                String("RequestedPixelFormat", SetterExpectation.Set, NullabilityExpectation.Nullable),
                Property("Format", recordingFormatType, SetterExpectation.Set),
                Property("Quality", videoQualityType, SetterExpectation.Set),
                Property("NvencPreset", nvencPresetType, SetterExpectation.Set),
                Property("SplitEncodeMode", splitEncodeModeType, SetterExpectation.Set),
                Property("CustomBitrateMbps", typeof(double), SetterExpectation.Set),
                Property("HdrEnabled", typeof(bool), SetterExpectation.Set),
                Property("HdrOutputMode", hdrOutputModeType, SetterExpectation.Set),
                Property("HdrNominalPeakNits", typeof(int), SetterExpectation.Set),
                Property("HdrMaxCll", typeof(int), SetterExpectation.Set),
                Property("HdrMaxFall", typeof(int), SetterExpectation.Set),
                String("HdrMasterDisplayMetadata", SetterExpectation.Set, NullabilityExpectation.NotNull),
                Property("PreviewMode", previewModeType, SetterExpectation.Set),
                String("OutputPath", SetterExpectation.Set, NullabilityExpectation.NotNull),
                Property("AudioEnabled", typeof(bool), SetterExpectation.Set),
                Property("UseCustomAudioInput", typeof(bool), SetterExpectation.Set),
                String("AudioDeviceId", SetterExpectation.Set, NullabilityExpectation.Nullable),
                String("AudioDeviceName", SetterExpectation.Set, NullabilityExpectation.Nullable),
                Property("MicrophoneEnabled", typeof(bool), SetterExpectation.Set),
                String("MicrophoneDeviceId", SetterExpectation.Set, NullabilityExpectation.Nullable),
                String("MicrophoneDeviceName", SetterExpectation.Set, NullabilityExpectation.Nullable),
                Property("ForceMjpegDecode", typeof(bool), SetterExpectation.Set),
                Property("FlashbackGpuDecode", typeof(bool), SetterExpectation.Set),
                Property("FlashbackBufferMinutes", typeof(int), SetterExpectation.Set),
                Property("MjpegDecoderCount", typeof(int), SetterExpectation.Set),
                Property("UseMjpegHighFrameRateMode", typeof(bool), SetterExpectation.None)
            });
        AssertDeclaredProperties(
            splitEncodeSupportType,
            new[]
            {
                Property("Supports2Way", typeof(bool), SetterExpectation.InitOnly),
                Property("Supports3Way", typeof(bool), SetterExpectation.InitOnly),
                Property("NvencUnavailable", splitEncodeSupportType, SetterExpectation.None, scope: PropertyScope.Static)
            });

        var settings = CreateInstance(settingsType);
        Assert.Equal(1920u, Get<uint>(settings, "Width"));
        Assert.Equal(1080u, Get<uint>(settings, "Height"));
        Assert.Equal(60d, Get<double>(settings, "FrameRate"));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.RecordingFormat", "H264Mp4"), Get(settings, "Format"));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.VideoQuality", "High"), Get(settings, "Quality"));
        Assert.Equal("Auto", Get(settings, "NvencPreset")!.ToString());
        Assert.Equal("Auto", Get(settings, "SplitEncodeMode")!.ToString());
        Assert.Equal(50d, Get<double>(settings, "CustomBitrateMbps"));
        Assert.False(Get<bool>(settings, "HdrEnabled"));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.HdrOutputMode", "Hdr10Pq"), Get(settings, "HdrOutputMode"));
        Assert.Equal(1000, Get<int>(settings, "HdrNominalPeakNits"));
        Assert.Equal(string.Empty, Get<string>(settings, "HdrMasterDisplayMetadata"));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.PreviewMode", "GpuFast"), Get(settings, "PreviewMode"));
        Assert.True(Get<bool>(settings, "AudioEnabled"));
        Assert.False(Get<bool>(settings, "UseCustomAudioInput"));
        Assert.False(Get<bool>(settings, "MicrophoneEnabled"));
        Assert.False(Get<bool>(settings, "ForceMjpegDecode"));
        Assert.True(Get<bool>(settings, "FlashbackGpuDecode"));
        Assert.Equal(5, Get<int>(settings, "FlashbackBufferMinutes"));
        Assert.Equal(6, Get<int>(settings, "MjpegDecoderCount"));
        Assert.False(Get<bool>(settings, "UseMjpegHighFrameRateMode"));

        var outputDir = Path.Combine(Path.GetTempPath(), $"capture_settings_{Guid.NewGuid():N}");
        Set(settings, "OutputPath", outputDir);
        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "HevcMp4"));
        var fullPath = Invoke(settings, "GetFullOutputPath")!.ToString()!;
        Assert.Equal(outputDir, Path.GetDirectoryName(fullPath));
        Assert.Contains("_HEVC.mp4", Path.GetFileName(fullPath), StringComparison.Ordinal);

        var splitSupport = Activator.CreateInstance(splitEncodeSupportType, true, false)!;
        Assert.True(Get<bool>(splitSupport, "Supports2Way"));
        Assert.False(Get<bool>(splitSupport, "Supports3Way"));
        var nvencUnavailable = splitEncodeSupportType.GetProperty("NvencUnavailable", ReflectionFlags.Static)!.GetValue(null)!;
        Assert.False(Get<bool>(nvencUnavailable, "Supports2Way"));
        Assert.False(Get<bool>(nvencUnavailable, "Supports3Way"));
    }

    [Fact]
    public void CaptureSettings_MjpegHighFrameRateMode_HandlesForceCaseAndInstanceState()
    {
        var asm = SussudioAssembly.Load();
        var settingsType = RequireType(asm, "Sussudio.Models.CaptureSettings");
        var method = RequireMethod(settingsType, "IsMjpegHighFrameRateMode", BindingFlags.Public | BindingFlags.Static);

        Assert.True((bool)method.Invoke(null, new object?[] { "mjpg", 3840u, 2160u, 100d, false, false })!);
        Assert.True((bool)method.Invoke(null, new object?[] { "MJPG", 1920u, 1080u, 60d, false, true })!);
        Assert.False((bool)method.Invoke(null, new object?[] { "NV12", 3840u, 2160u, 120d, false, true })!);
        Assert.False((bool)method.Invoke(null, new object?[] { "MJPG", 3840u, 2160u, 120d, true, true })!);

        var settings = CreateInstance(settingsType);
        Set(settings, "RequestedPixelFormat", "MJPG");
        Set(settings, "Width", 3840u);
        Set(settings, "Height", 2160u);
        Set(settings, "FrameRate", 120d);
        Set(settings, "HdrEnabled", false);
        Assert.True(Get<bool>(settings, "UseMjpegHighFrameRateMode"));
        Set(settings, "HdrEnabled", true);
        Assert.False(Get<bool>(settings, "UseMjpegHighFrameRateMode"));
        Set(settings, "Width", 1920u);
        Set(settings, "Height", 1080u);
        Set(settings, "FrameRate", 60d);
        Set(settings, "HdrEnabled", false);
        Set(settings, "ForceMjpegDecode", true);
        Assert.True(Get<bool>(settings, "UseMjpegHighFrameRateMode"));
        Set(settings, "HdrEnabled", true);
        Assert.False(Get<bool>(settings, "UseMjpegHighFrameRateMode"));
    }

    [Fact]
    public void CaptureSettings_MjpegHighFrameRateMode_RequiresSdr4k120StyleRequest()
    {
        var settings = CreateInstance(RequireType(SussudioAssembly.Load(), "Sussudio.Models.CaptureSettings"));
        Set(settings, "Width", 3840u);
        Set(settings, "Height", 2160u);
        Set(settings, "FrameRate", 120d);
        Set(settings, "RequestedPixelFormat", "MJPG");
        Set(settings, "HdrEnabled", false);
        Assert.True(Get<bool>(settings, "UseMjpegHighFrameRateMode"));

        Set(settings, "HdrEnabled", true);
        Assert.False(Get<bool>(settings, "UseMjpegHighFrameRateMode"));

        Set(settings, "HdrEnabled", false);
        Set(settings, "Width", 1920u);
        Assert.False(Get<bool>(settings, "UseMjpegHighFrameRateMode"));
    }

    [Fact]
    public void CaptureSettings_GetTargetBitrate_ScalesByResolutionAndFrameRate()
    {
        var asm = SussudioAssembly.Load();
        var settings = CreateInstance(RequireType(asm, "Sussudio.Models.CaptureSettings"));
        Set(settings, "Width", 3840u);
        Set(settings, "Height", 2160u);
        Set(settings, "FrameRate", 60.0);
        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "H264Mp4"));
        Set(settings, "Quality", ParseEnum(asm, "Sussudio.Models.VideoQuality", "High"));

        var bps = Convert.ToUInt32(Invoke(settings, "GetTargetBitrate"));
        Assert.InRange(bps, 150_000_000u, 200_000_000u);

        Set(settings, "Width", 1920u);
        Set(settings, "Height", 1080u);
        Set(settings, "FrameRate", 30.0);
        var lowBitrate = Convert.ToUInt32(Invoke(settings, "GetTargetBitrate"));
        Assert.InRange(lowBitrate, 24_000_000u, 26_000_000u);
    }

    [Fact]
    public void CaptureSettings_GetTargetBitrate_AppliesCodecEfficiency()
    {
        var asm = SussudioAssembly.Load();
        var settings = CreateInstance(RequireType(asm, "Sussudio.Models.CaptureSettings"));
        Set(settings, "Width", 1920u);
        Set(settings, "Height", 1080u);
        Set(settings, "FrameRate", 60.0);
        Set(settings, "Quality", ParseEnum(asm, "Sussudio.Models.VideoQuality", "High"));

        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "H264Mp4"));
        var h264 = Convert.ToUInt32(Invoke(settings, "GetTargetBitrate"));
        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "HevcMp4"));
        var hevc = Convert.ToUInt32(Invoke(settings, "GetTargetBitrate"));
        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "Av1Mp4"));
        var av1 = Convert.ToUInt32(Invoke(settings, "GetTargetBitrate"));

        Assert.True(hevc < h264);
        Assert.True(av1 < hevc);
    }

    [Fact]
    public void CaptureSettings_GetTargetBitrate_ClampsCustomQuality()
    {
        var asm = SussudioAssembly.Load();
        var settings = CreateInstance(RequireType(asm, "Sussudio.Models.CaptureSettings"));
        Set(settings, "Quality", ParseEnum(asm, "Sussudio.Models.VideoQuality", "Custom"));

        Set(settings, "CustomBitrateMbps", 999.0);
        Assert.Equal(300_000_000u, Convert.ToUInt32(Invoke(settings, "GetTargetBitrate")));
        Set(settings, "CustomBitrateMbps", 0.1);
        Assert.Equal(1_000_000u, Convert.ToUInt32(Invoke(settings, "GetTargetBitrate")));
    }

    [Fact]
    public void CaptureSettings_GetOutputFileName_IncludesFormatSuffix()
    {
        var asm = SussudioAssembly.Load();
        var settings = CreateInstance(RequireType(asm, "Sussudio.Models.CaptureSettings"));

        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "Av1Mp4"));
        var av1Name = Invoke(settings, "GetOutputFileName")!.ToString()!;
        Assert.Contains("_AV1.", av1Name, StringComparison.Ordinal);
        Assert.Contains(".mp4", av1Name, StringComparison.Ordinal);

        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "HevcMp4"));
        Assert.Contains("_HEVC.", Invoke(settings, "GetOutputFileName")!.ToString()!, StringComparison.Ordinal);

        Set(settings, "Format", ParseEnum(asm, "Sussudio.Models.RecordingFormat", "H264Mp4"));
        Assert.Contains("_H264.", Invoke(settings, "GetOutputFileName")!.ToString()!, StringComparison.Ordinal);
    }

    [Fact]
    public void CaptureSettings_MjpegHfrMode_RequiresSdrAndMjpgPixelFormat()
    {
        var settingsType = RequireType(SussudioAssembly.Load(), "Sussudio.Models.CaptureSettings");
        var method = RequireMethod(settingsType, "IsMjpegHighFrameRateMode", BindingFlags.Public | BindingFlags.Static);

        Assert.True((bool)method.Invoke(null, new object?[] { "MJPG", 3840u, 2160u, 120.0, false, false })!);
        Assert.False((bool)method.Invoke(null, new object?[] { "MJPG", 3840u, 2160u, 120.0, true, false })!);
        Assert.False((bool)method.Invoke(null, new object?[] { "NV12", 3840u, 2160u, 120.0, false, false })!);
        Assert.False((bool)method.Invoke(null, new object?[] { "MJPG", 1920u, 1080u, 60.0, false, false })!);
    }

    [Fact]
    public void RecordingSettingsSelectionPolicy_PreservesHdrAndSdrChoices()
    {
        AssertRecordingFormatSelection(
            "HDR filters H.264 and falls back to HEVC",
            detectedFormats: new[] { "H.264", "HEVC", "AV1" },
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: "H.264",
            isHdrEnabled: true,
            expectedFormats: new[] { "HEVC", "AV1" },
            expectedSelectedFormat: "HEVC");

        AssertRecordingFormatSelection(
            "HDR preserves existing AV1 selection",
            detectedFormats: new[] { "H.264", "HEVC", "AV1" },
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: "AV1",
            isHdrEnabled: true,
            expectedFormats: new[] { "HEVC", "AV1" },
            expectedSelectedFormat: "AV1");

        AssertRecordingFormatSelection(
            "HDR falls back to AV1 when HEVC is unavailable",
            detectedFormats: new[] { "H.264", "AV1" },
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: "H.264",
            isHdrEnabled: true,
            expectedFormats: new[] { "AV1" },
            expectedSelectedFormat: "AV1");

        AssertRecordingFormatSelection(
            "HDR preserves last known real formats when refresh has no HDR formats",
            detectedFormats: new[] { "H.264" },
            currentAvailableFormats: new[] { "HEVC", "AV1" },
            selectedFormat: "H.264",
            isHdrEnabled: true,
            expectedFormats: new[] { "HEVC", "AV1" },
            expectedSelectedFormat: "HEVC");

        AssertRecordingFormatSelection(
            "SDR preserves valid current format",
            detectedFormats: new[] { "H.264", "HEVC" },
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: "HEVC",
            isHdrEnabled: false,
            expectedFormats: new[] { "H.264", "HEVC" },
            expectedSelectedFormat: "HEVC");

        AssertRecordingFormatSelection(
            "SDR prefers H.264 when current format is unavailable",
            detectedFormats: new[] { "HEVC", "H264" },
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: "VP9",
            isHdrEnabled: false,
            expectedFormats: new[] { "HEVC", "H264" },
            expectedSelectedFormat: "H264");

        AssertRecordingFormatSelection(
            "Empty SDR capabilities fall back to default format",
            detectedFormats: Array.Empty<string>(),
            currentAvailableFormats: Array.Empty<string>(),
            selectedFormat: null,
            isHdrEnabled: false,
            expectedFormats: new[] { "H.264" },
            expectedSelectedFormat: "H.264");
    }

    [Fact]
    public void RecordingSettingsSelectionPolicy_ParsesModelValues()
    {
        var asm = SussudioAssembly.Load();
        var policyType = RequireType(asm, "Sussudio.ViewModels.RecordingSettingsSelectionPolicy");
        var parseRecordingFormat = RequireMethod(policyType, "ParseRecordingFormat", ReflectionFlags.Static);
        var parseVideoQuality = RequireMethod(policyType, "ParseVideoQuality", ReflectionFlags.Static);
        var clampCustomBitrate = RequireMethod(policyType, "ClampCustomBitrateMbps", ReflectionFlags.Static);

        Assert.Equal(ParseEnum(asm, "Sussudio.Models.RecordingFormat", "H264Mp4"), parseRecordingFormat.Invoke(null, new object?[] { "H.264" }));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.RecordingFormat", "HevcMp4"), parseRecordingFormat.Invoke(null, new object?[] { "HEVC" }));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.RecordingFormat", "Av1Mp4"), parseRecordingFormat.Invoke(null, new object?[] { "AV1" }));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.VideoQuality", "Auto"), parseVideoQuality.Invoke(null, new object?[] { "Auto" }));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.VideoQuality", "SuperHigh"), parseVideoQuality.Invoke(null, new object?[] { "Super High" }));
        Assert.Equal(ParseEnum(asm, "Sussudio.Models.VideoQuality", "High"), parseVideoQuality.Invoke(null, new object?[] { "unexpected" }));
        Assert.Equal(1d, (double)clampCustomBitrate.Invoke(null, new object?[] { -5d })!);
        Assert.Equal(42d, (double)clampCustomBitrate.Invoke(null, new object?[] { 42d })!);
        Assert.Equal(300d, (double)clampCustomBitrate.Invoke(null, new object?[] { 999d })!);
    }

    [Fact]
    public void EncoderSupport_ComputesAvailabilityAndPreferredEncoders()
    {
        var supportType = RequireType(SussudioAssembly.Load(), "Sussudio.Models.EncoderSupport");
        AssertDeclaredProperties(
            supportType,
            new[]
            {
                Property("HasH264Nvenc", typeof(bool), SetterExpectation.InitOnly),
                Property("HasHevcNvenc", typeof(bool), SetterExpectation.InitOnly),
                Property("HasAv1Nvenc", typeof(bool), SetterExpectation.InitOnly),
                Property("HasLibX264", typeof(bool), SetterExpectation.InitOnly),
                Property("HasLibX265", typeof(bool), SetterExpectation.InitOnly),
                Property("HasLibSvtAv1", typeof(bool), SetterExpectation.InitOnly),
                Property("HasLibAomAv1", typeof(bool), SetterExpectation.InitOnly),
                Property("HasH264", typeof(bool), SetterExpectation.None),
                Property("HasHevc", typeof(bool), SetterExpectation.None),
                Property("HasAv1", typeof(bool), SetterExpectation.None),
                String("PreferredAv1Encoder", SetterExpectation.None, NullabilityExpectation.Nullable),
                Property("Empty", supportType, SetterExpectation.None, scope: PropertyScope.Static)
            });

        var empty = supportType.GetProperty("Empty", ReflectionFlags.Static)!.GetValue(null)!;
        Assert.False(Get<bool>(empty, "HasH264"));
        Assert.False(Get<bool>(empty, "HasHevc"));
        Assert.False(Get<bool>(empty, "HasAv1"));
        Assert.Null(Get(empty, "PreferredAv1Encoder"));

        var nvencAv1 = CreateInstance(supportType);
        Set(nvencAv1, "HasAv1Nvenc", true);
        Set(nvencAv1, "HasLibSvtAv1", true);
        Assert.True(Get<bool>(nvencAv1, "HasAv1"));
        Assert.Equal("av1_nvenc", Get<string>(nvencAv1, "PreferredAv1Encoder"));

        var svtAv1 = CreateInstance(supportType);
        Set(svtAv1, "HasLibSvtAv1", true);
        Set(svtAv1, "HasLibAomAv1", true);
        Assert.Equal("libsvtav1", Get<string>(svtAv1, "PreferredAv1Encoder"));

        var softwareFallbacks = CreateInstance(supportType);
        Set(softwareFallbacks, "HasLibX264", true);
        Set(softwareFallbacks, "HasLibX265", true);
        Set(softwareFallbacks, "HasLibAomAv1", true);
        Assert.True(Get<bool>(softwareFallbacks, "HasH264"));
        Assert.True(Get<bool>(softwareFallbacks, "HasHevc"));
        Assert.Equal("libaom-av1", Get<string>(softwareFallbacks, "PreferredAv1Encoder"));
    }

    [Theory]
    [InlineData(null, false, false)]
    [InlineData("", false, false)]
    [InlineData(" \t\r\n", false, false)]
    [InlineData("\u2003\u00a0", false, false)]
    [InlineData("p010", true, true)]
    [InlineData("P016", true, true)]
    [InlineData("i010", true, true)]
    [InlineData("y210", true, true)]
    [InlineData("Y410", true, true)]
    [InlineData("y416", true, true)]
    [InlineData("r10G10b10", true, true)]
    [InlineData("xR10", true, true)]
    [InlineData("prefix-p010-suffix", true, true)]
    [InlineData(" \tXr10 texture\n", true, true)]
    [InlineData("R10G10B10A2", true, true)]
    [InlineData("BT2020", false, true)]
    [InlineData("st2084", false, true)]
    [InlineData("hdr", false, true)]
    [InlineData("Nv12 (bT2020 sT2084 hDr)", false, true)]
    [InlineData("HDR10", false, true)]
    [InlineData("notHDR", false, true)]
    [InlineData("HDR/P010", true, true)]
    [InlineData("BT.2020", false, false)]
    [InlineData("ST 2084", false, false)]
    [InlineData("P 010", false, false)]
    [InlineData("NV12", false, false)]
    [InlineData("YUY2", false, false)]
    [InlineData("MJPG", false, false)]
    [InlineData("BGRA8", false, false)]
    [InlineData("RGB32", false, false)]
    [InlineData("I420", false, false)]
    [InlineData("P012", false, false)]
    [InlineData("Y216", false, false)]
    [InlineData("P01", false, false)]
    [InlineData("unknown", false, false)]
    public void MediaFormat_PixelFormatPredicatesPreserveSubtypeAndHdrMarkerDistinctions(
        string? pixelFormat, bool expectedTrue10Bit, bool expectedHdr)
    {
        var mediaFormatType = RequireType(SussudioAssembly.Load(), "Sussudio.Models.MediaFormat");
        var isTrue10Bit = RequireMethod(mediaFormatType, "IsTrue10BitPixelFormat", ReflectionFlags.Static)
            .CreateDelegate<Func<string?, bool>>();
        var isHdr = RequireMethod(mediaFormatType, "IsHdrPixelFormat", ReflectionFlags.Static)
            .CreateDelegate<Func<string?, bool>>();

        Assert.Equal(expectedTrue10Bit, isTrue10Bit(pixelFormat));
        Assert.Equal(expectedHdr, isHdr(pixelFormat));
    }

    [Fact]
    public void MediaFormat_Equality_WithMatchingRationalFrameRates()
    {
        var mediaFormatType = RequireType(SussudioAssembly.Load(), "Sussudio.Models.MediaFormat");
        var a = CreateMediaFormat(
            mediaFormatType,
            width: 1920u,
            height: 1080u,
            frameRateNumerator: 60000u,
            frameRateDenominator: 1001u,
            pixelFormat: "NV12",
            isHdr: false);
        var b = CreateMediaFormat(
            mediaFormatType,
            width: 1920u,
            height: 1080u,
            frameRateNumerator: 60000u,
            frameRateDenominator: 1001u,
            pixelFormat: "NV12",
            isHdr: false);

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void MediaFormat_Inequality_WhenDimensionsDiffer()
    {
        var mediaFormatType = RequireType(SussudioAssembly.Load(), "Sussudio.Models.MediaFormat");
        var a = CreateMediaFormat(
            mediaFormatType,
            width: 1920u,
            height: 1080u,
            frameRate: 60.0,
            pixelFormat: "NV12",
            isHdr: false);
        var b = CreateMediaFormat(
            mediaFormatType,
            width: 3840u,
            height: 2160u,
            frameRate: 60.0,
            pixelFormat: "NV12",
            isHdr: false);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void MediaFormat_GetHashCode_ConsistencyForEqualObjects()
    {
        var mediaFormatType = RequireType(SussudioAssembly.Load(), "Sussudio.Models.MediaFormat");
        var a = CreateMediaFormat(
            mediaFormatType,
            width: 3840u,
            height: 2160u,
            frameRateNumerator: 120000u,
            frameRateDenominator: 1001u,
            pixelFormat: "P010",
            isHdr: true);
        var b = CreateMediaFormat(
            mediaFormatType,
            width: 3840u,
            height: 2160u,
            frameRateNumerator: 120000u,
            frameRateDenominator: 1001u,
            pixelFormat: "P010",
            isHdr: true);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    private static object CreateResolutionFormatDictionary(Type mediaFormatType)
        => Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(
               typeof(string),
               typeof(List<>).MakeGenericType(mediaFormatType)))!;

    private static void AddResolutionFormats(object formatsByResolution, Type mediaFormatType, string resolutionKey, params object[] formats)
        => ((IDictionary)formatsByResolution).Add(resolutionKey, CreateMediaFormatList(mediaFormatType, formats));

    private static object CreateMediaFormatList(Type mediaFormatType, params object[] formats)
    {
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(mediaFormatType))!;
        foreach (var format in formats)
        {
            list.Add(format);
        }

        return list;
    }

    private static object CreateMediaFormat(Type mediaFormatType, uint width, uint height, double frameRate, string pixelFormat, bool isHdr)
    {
        var format = CreateInstance(mediaFormatType);
        Set(format, "Width", width);
        Set(format, "Height", height);
        Set(format, "FrameRate", frameRate);
        Set(format, "PixelFormat", pixelFormat);
        Set(format, "IsHdr", isHdr);
        return format;
    }

    private static object CreateMediaFormat(
        Type mediaFormatType,
        uint width,
        uint height,
        string pixelFormat,
        bool isHdr,
        double? frameRate = null,
        uint? frameRateNumerator = null,
        uint? frameRateDenominator = null)
    {
        var format = CreateInstance(mediaFormatType);
        Set(format, "Width", width);
        Set(format, "Height", height);
        if (frameRate.HasValue)
        {
            Set(format, "FrameRate", frameRate.Value);
        }

        if (frameRateNumerator.HasValue)
        {
            Set(format, "FrameRateNumerator", frameRateNumerator.Value);
        }

        if (frameRateDenominator.HasValue)
        {
            Set(format, "FrameRateDenominator", frameRateDenominator.Value);
        }

        Set(format, "PixelFormat", pixelFormat);
        Set(format, "IsHdr", isHdr);
        return format;
    }

    private static void AssertRecordingFormatSelection(
        string fieldName,
        string[] detectedFormats,
        string[] currentAvailableFormats,
        string? selectedFormat,
        bool isHdrEnabled,
        string[] expectedFormats,
        string expectedSelectedFormat)
    {
        var asm = SussudioAssembly.Load();
        var policyType = RequireType(asm, "Sussudio.ViewModels.RecordingSettingsSelectionPolicy");
        var select = RequireMethod(policyType, "Select", ReflectionFlags.Static);
        var selection = select.Invoke(
                null,
                new object?[]
                {
                    detectedFormats,
                    currentAvailableFormats,
                    selectedFormat,
                    isHdrEnabled,
                    "H.264",
                    "HEVC",
                    "AV1"
                })!;

        var availableFormats = ((IEnumerable)Get(selection, "AvailableFormats")!)
            .Cast<string>()
            .ToArray();
        var selected = Get<string>(selection, "SelectedFormat");
        Assert.Equal(expectedFormats, availableFormats);
        Assert.Equal(expectedSelectedFormat, selected);
    }
}

public class StatsPresentationTests
{
    [Fact]
    public void FrameTimeGraph_UsesBudgetRangeThatPreservesVisibleHitches()
    {
        var scale120 = Sussudio.Controllers.FrameTimeGraphScale.FromExpectedFps(120);
        Assert.Equal(1000.0 / 120, scale120.FrameBudgetMs, 4);
        Assert.True(scale120.MaximumFrameTimeMs > 3 * scale120.FrameBudgetMs);
        var fallback = Sussudio.Controllers.FrameTimeGraphScale.FromExpectedFps(0);
        Assert.Equal(60, fallback.ExpectedFps);
        Assert.True(fallback.MaximumFrameTimeMs > 50);
    }

    [Fact]
    public void DockEncoderPresentation_FormatsCodecAndBitrate()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var snapshotType = RequireType("Sussudio.ViewModels.StatsSnapshot");
        var buildDockPresentation = builderType.GetMethod("BuildDockPresentation", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("BuildDockPresentation was not found.");

        object Build(string? codecName, bool recording = true)
        {
            var snapshot = CreateUninitializedObject(snapshotType);
            SetPropertyBackingField(snapshot, "Recording", recording);
            SetPropertyBackingField(snapshot, "EncoderCodecName", codecName);
            SetPropertyBackingField(snapshot, "EncoderWidth", 3840);
            SetPropertyBackingField(snapshot, "EncoderHeight", 2160);
            SetPropertyBackingField(snapshot, "EncoderFrameRate", 59.94);
            SetPropertyBackingField(snapshot, "EncoderTargetBitRate", 50_000_000u);
            SetPropertyBackingField(snapshot, "AvSyncEncoderDriftMs", (double?)2.25d);
            SetPropertyBackingField(snapshot, "AvSyncEncoderCorrectionSamples", (long?)7L);
            SetPropertyBackingField(snapshot, "VisualCadenceMotionConfidence", string.Empty);

            return buildDockPresentation.Invoke(null, new[] { snapshot })
                ?? throw new InvalidOperationException("BuildDockPresentation returned null.");
        }

        var hevc = Build("hevc_nvenc");
        Assert.True(GetBoolProperty(hevc, "EncoderActive"));
        Assert.Equal("HEVC (NVENC)", GetStringProperty(hevc, "EncoderCodec"));
        Assert.Equal("3840 x 2160", GetStringProperty(hevc, "EncoderResolution"));
        Assert.Equal("59.94 fps", GetStringProperty(hevc, "EncoderFrameRate"));
        Assert.Equal("50 Mbps", GetStringProperty(hevc, "EncoderBitrate"));
        Assert.True(GetBoolProperty(hevc, "EncoderDriftVisible"));
        Assert.Equal("+2.2ms (7 corr)", GetStringProperty(hevc, "EncoderDrift"));

        var av1 = Build("av1_nvenc");
        Assert.Equal("AV1 (NVENC)", GetStringProperty(av1, "EncoderCodec"));

        var passthrough = Build("software_custom");
        Assert.Equal("software_custom", GetStringProperty(passthrough, "EncoderCodec"));

        var inactive = Build(null);
        Assert.False(GetBoolProperty(inactive, "EncoderActive"));
        Assert.Equal(string.Empty, GetStringProperty(inactive, "EncoderCodec"));

        var idleDrift = Build("h264_nvenc", recording: false);
        Assert.False(GetBoolProperty(idleDrift, "EncoderDriftVisible"));
        Assert.Equal(string.Empty, GetStringProperty(idleDrift, "EncoderDrift"));
    }

    [Fact]
    public void WindowPresentation_FormatsDetachedWindowText()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var snapshotType = RequireType("Sussudio.ViewModels.StatsSnapshot");
        var buildWindowPresentation = builderType.GetMethod("BuildStatsWindowPresentation", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("BuildStatsWindowPresentation was not found.");

        var snapshot = CreateUninitializedObject(snapshotType);
        SetPropertyBackingField(snapshot, "Previewing", true);
        SetPropertyBackingField(snapshot, "Recording", false);
        SetPropertyBackingField(snapshot, "DiagnosticHealthStatus", "Healthy");
        SetPropertyBackingField(snapshot, "DiagnosticLikelyStage", "none");
        SetPropertyBackingField(snapshot, "DiagnosticEvidence", string.Empty);
        SetPropertyBackingField(snapshot, "DiagnosticSummary", "All monitored frame lanes are within current thresholds.");
        SetPropertyBackingField(snapshot, "SourceWidth", (int?)3840);
        SetPropertyBackingField(snapshot, "SourceHeight", (int?)2160);
        SetPropertyBackingField(snapshot, "SourceFrameRateExact", (double?)119.88d);
        SetPropertyBackingField(snapshot, "SourceIsHdr", (bool?)true);
        SetPropertyBackingField(snapshot, "SourceColorimetry", "BT.2020");
        SetPropertyBackingField(snapshot, "SourceVideoFormat", "YCbCr422");
        SetPropertyBackingField(snapshot, "TelemetryOrigin", "NativeXu");
        SetPropertyBackingField(snapshot, "TelemetryConfidence", "High");
        SetPropertyBackingField(snapshot, "SourceObservedFps", 119.8d);
        SetPropertyBackingField(snapshot, "SourceExpectedFps", 120d);
        SetPropertyBackingField(snapshot, "SourceAvgIntervalMs", 8.333d);
        SetPropertyBackingField(snapshot, "SourceP95IntervalMs", 8.75d);
        SetPropertyBackingField(snapshot, "SourceJitterMs", 0.125d);
        SetPropertyBackingField(snapshot, "SourceSevereGaps", 2L);
        SetPropertyBackingField(snapshot, "SourceEstDrops", 3L);
        SetPropertyBackingField(snapshot, "SourceEstDropPct", 0.25d);
        SetPropertyBackingField(snapshot, "PreviewObservedFps", 118.2d);
        SetPropertyBackingField(snapshot, "PreviewAvgIntervalMs", 8.44d);
        SetPropertyBackingField(snapshot, "PreviewP95IntervalMs", 9.1d);
        SetPropertyBackingField(snapshot, "PreviewSlowFrames", 4L);
        SetPropertyBackingField(snapshot, "PreviewSlowPct", 1.5d);
        SetPropertyBackingField(snapshot, "PipelineLatencyMs", 3.4d);
        SetPropertyBackingField(snapshot, "SourceFramesDelivered", 500L);
        SetPropertyBackingField(snapshot, "SourceFramesDropped", 5L);
        SetPropertyBackingField(snapshot, "RendererFramesRendered", 490L);
        SetPropertyBackingField(snapshot, "RendererFramesDropped", 6L);
        SetPropertyBackingField(snapshot, "PerformanceScore", 98.75d);

        var presentation = buildWindowPresentation.Invoke(null, new[] { snapshot })
            ?? throw new InvalidOperationException("BuildStatsWindowPresentation returned null.");

        Assert.Equal("Previewing", GetStringProperty(presentation, "SessionState"));
        Assert.Equal("Healthy", GetStringProperty(presentation, "DiagnosticStatus"));
        Assert.Equal("All monitored frame lanes are within current thresholds.", GetStringProperty(presentation, "DiagnosticEvidence"));
        Assert.Equal("3840 x 2160", GetStringProperty(presentation, "SourceResolution"));
        Assert.Equal("119.88 fps", GetStringProperty(presentation, "SourceFrameRate"));
        Assert.Equal("On (BT.2020)", GetStringProperty(presentation, "SourceHdr"));
        Assert.Equal("YCbCr422", GetStringProperty(presentation, "SourceFormat"));
        Assert.Equal("NativeXu (High)", GetStringProperty(presentation, "TelemetryOrigin"));
        Assert.Equal("119.80", GetStringProperty(presentation, "SourceFps"));
        Assert.Equal("8.33ms avg", GetStringProperty(presentation, "SourceAvg"));
        Assert.Equal("3 drops (0.3%)", GetStringProperty(presentation, "SourceDrops"));
        Assert.Equal("4 frames (1.5%)", GetStringProperty(presentation, "PreviewSlow"));
        Assert.Equal("3.40ms avg", GetStringProperty(presentation, "PipelineLatency"));
        Assert.Equal("98.8 / 100", GetStringProperty(presentation, "PerformanceScore"));

        var telemetryDetails = GetPropertyValue(presentation, "TelemetryDetails")
            ?? throw new InvalidOperationException("StatsWindowPresentation.TelemetryDetails was null.");
        Assert.True(GetBoolProperty(telemetryDetails, "IsEmpty"));
        Assert.Equal("All monitored frame lanes are within current thresholds.", GetStringProperty(telemetryDetails, "EmptyText"));
    }

    [Fact]
    public void VisualPresentation_TreatsExpectedDisplayRepeatAsGood()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var snapshotType = RequireType("Sussudio.ViewModels.StatsSnapshot");
        var buildDockPresentation = builderType.GetMethod("BuildDockPresentation", ReflectionFlags.Static)
            ?? throw new InvalidOperationException("BuildDockPresentation was not found.");

        var snapshot = CreateUninitializedObject(snapshotType);
        SetPropertyBackingField(snapshot, "Previewing", true);
        SetPropertyBackingField(snapshot, "SourceExpectedFps", 60d);
        SetPropertyBackingField(snapshot, "SourceFrameRateExact", (double?)60d);
        SetPropertyBackingField(snapshot, "VisualCadenceSamples", 120);
        SetPropertyBackingField(snapshot, "VisualCadenceOutputFps", 120d);
        SetPropertyBackingField(snapshot, "VisualCadenceChangeFps", 60d);
        SetPropertyBackingField(snapshot, "VisualCadenceRepeatPercent", 50d);
        SetPropertyBackingField(snapshot, "VisualCadenceLongestRepeatRun", 1L);
        SetPropertyBackingField(snapshot, "VisualCadenceMotionScore", 12.5d);
        SetPropertyBackingField(snapshot, "VisualCadenceMotionConfidence", "High");

        var presentation = buildDockPresentation.Invoke(null, new[] { snapshot })
            ?? throw new InvalidOperationException("BuildDockPresentation returned null.");

        Assert.Equal("120 Hz", GetStringProperty(presentation, "SummaryVisualFps"));
        Assert.Equal("crop 120 Hz", GetStringProperty(presentation, "VisualFps"));
        Assert.Equal("12.5% px / High", GetStringProperty(presentation, "VisualMotion"));
        Assert.Equal("Good", GetPropertyValue(presentation, "SummaryVisualFpsStatus")?.ToString());
        Assert.Equal("Good", GetPropertyValue(presentation, "VisualFpsStatus")?.ToString());
    }

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static object CreateUninitializedObject(Type type)
        => RuntimeHelpers.GetUninitializedObject(type);

    private static void SetPropertyBackingField(object instance, string propertyName, object? value)
    {
        var field = instance.GetType().GetField($"<{propertyName}>k__BackingField", ReflectionFlags.Instance)
            ?? throw new InvalidOperationException($"Backing field for {propertyName} was not found.");
        field.SetValue(instance, value);
    }

    private static object? GetPropertyValue(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance)!.GetValue(instance);

    private static string GetStringProperty(object instance, string propertyName)
        => GetPropertyValue(instance, propertyName) as string
           ?? throw new InvalidOperationException($"{propertyName} was not a string.");

    private static bool GetBoolProperty(object instance, string propertyName)
        => (bool)(GetPropertyValue(instance, propertyName)
                  ?? throw new InvalidOperationException($"{propertyName} was not a bool."));

}

public class StatsHardwareRowsTests
{
    [Fact]
    public void HardwareRowsInputProvider_ReturnsNoDecodeInputWithoutMetrics()
    {
        var providerType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputProvider");
        var getDecodeRowsInput = providerType.GetMethod("GetDecodeRowsInput", ReflectionFlags.Instance)
                                 ?? throw new InvalidOperationException("StatsHardwareRowsInputProvider.GetDecodeRowsInput not found.");
        var nullMetricsProvider = CreateStatsHardwareRowsInputProvider(null, 3, null);
        Assert.Null(getDecodeRowsInput.Invoke(nullMetricsProvider, null));
    }

    [Fact]
    public void HardwareRowsInputProvider_ReturnsNoDecodeInputWithoutDecoders()
    {
        var providerType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputProvider");
        var getDecodeRowsInput = providerType.GetMethod("GetDecodeRowsInput", ReflectionFlags.Instance)
                                 ?? throw new InvalidOperationException("StatsHardwareRowsInputProvider.GetDecodeRowsInput not found.");

        var zeroDecoderProvider = CreateStatsHardwareRowsInputProvider(
            CreateStatsHardwarePipelineTimingMetrics(decoderCount: 0),
            3,
            null);
        Assert.Null(getDecodeRowsInput.Invoke(zeroDecoderProvider, null));
    }

    [Fact]
    public void HardwareRowsInputProvider_ProjectsPendingPreviewFrames()
    {
        var providerType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputProvider");
        var getDecodeRowsInput = providerType.GetMethod("GetDecodeRowsInput", ReflectionFlags.Instance)
                                 ?? throw new InvalidOperationException("StatsHardwareRowsInputProvider.GetDecodeRowsInput not found.");

        var validProvider = CreateStatsHardwareRowsInputProvider(
            CreateStatsHardwarePipelineTimingMetrics(),
            7,
            null);
        var decodeInput = getDecodeRowsInput.Invoke(validProvider, null)
                          ?? throw new InvalidOperationException("Provider returned null decode input for valid metrics.");
        Assert.Equal(7, Convert.ToInt32(GetPropertyValue(decodeInput, "PendingPreviewFrameCount"), CultureInfo.InvariantCulture));
    }

    [Fact]
    public void HardwareRowsInputProvider_ReturnsNoGpuInputWithoutGpuTelemetry()
    {
        var providerType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputProvider");
        var getGpuRowsInput = providerType.GetMethod("GetGpuRowsInput", ReflectionFlags.Instance)
                              ?? throw new InvalidOperationException("StatsHardwareRowsInputProvider.GetGpuRowsInput not found.");
        var validProvider = CreateStatsHardwareRowsInputProvider(CreateStatsHardwarePipelineTimingMetrics(), 7, null);
        Assert.Null(getGpuRowsInput.Invoke(validProvider, null));
    }

    [Fact]
    public void HardwareRowsPresentation_FormatsDecodeRows()
    {
        using var culture = CultureScope.Use("en-US");

        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var buildDecodeRows = builderType.GetMethod("BuildHardwareDecodeRows", ReflectionFlags.Static)
                              ?? throw new InvalidOperationException("StatsPresentationBuilder.BuildHardwareDecodeRows not found.");
        var decodeRows = StatsHardwareRowsToMap(buildDecodeRows.Invoke(null, new object?[]
        {
            CreateStatsHardwareMjpegMetrics()
        }) ?? throw new InvalidOperationException("BuildHardwareDecodeRows returned null."));

        Assert.Equal("4.00ms (250fps peak)", decodeRows["Throughput"]);
        Assert.Equal("8.00 / 12.25ms  avg/P95 (2T)", decodeRows["Decode"]);
        Assert.Equal("0.75 / 1.50ms  avg/P95", decodeRows["Reorder"]);
        Assert.Equal("9.25 / 15.50ms  avg/P95", decodeRows["Pipeline"]);
        Assert.Equal("1,234 emitted / 56 dropped", decodeRows["Frames"]);
        Assert.Equal("compressed=4 (5.0/10.0MB)  reorder=6  preview=3  skips=2", decodeRows["Buffers"]);
        Assert.Equal("4.50 / 7.75ms", decodeRows["Thread 0"]);
        Assert.Equal("5.25 / 8.50ms", decodeRows["Thread 1"]);
    }

    [Fact]
    public void HardwareRowsPresentation_ReportsUnavailableGpuTelemetry()
    {
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var buildGpuRows = builderType.GetMethod("BuildHardwareGpuRows", ReflectionFlags.Static)
                           ?? throw new InvalidOperationException("StatsPresentationBuilder.BuildHardwareGpuRows not found.");
        var inputBuilderType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputBuilder");
        var buildGpuInput = inputBuilderType.GetMethod("BuildGpuRowsInput", ReflectionFlags.Static)
                            ?? throw new InvalidOperationException("StatsHardwareRowsInputBuilder.BuildGpuRowsInput not found.");

        var unavailableGpuRows = StatsHardwareRowsToMap(buildGpuRows.Invoke(null, new object?[] { null })
                                 ?? throw new InvalidOperationException("BuildHardwareGpuRows returned null for unavailable snapshot."));
        Assert.Equal("NVML not available", unavailableGpuRows["Status"]);
        Assert.Null(buildGpuInput.Invoke(null, new object?[] { null }));
    }

    [Fact]
    public void HardwareRowsPresentation_FormatsGpuRows()
    {
        using var culture = CultureScope.Use("en-US");
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var buildGpuRows = builderType.GetMethod("BuildHardwareGpuRows", ReflectionFlags.Static)
                           ?? throw new InvalidOperationException("StatsPresentationBuilder.BuildHardwareGpuRows not found.");

        var gpuRows = StatsHardwareRowsToMap(buildGpuRows.Invoke(null, new[]
        {
            CreateStatsHardwareNvmlSnapshot()
        }) ?? throw new InvalidOperationException("BuildHardwareGpuRows returned null."));

        Assert.Equal("RTX Test", gpuRows["GPU"]);
        Assert.Equal("41% (Mem: 52%)", gpuRows["Utilization"]);
        Assert.Equal("13%", gpuRows["NVDEC"]);
        Assert.Equal("17%", gpuRows["NVENC"]);
        Assert.Equal("1.5 MB/s", gpuRows["PCIe TX"]);
        Assert.Equal("2.0 MB/s", gpuRows["PCIe RX"]);
        Assert.Equal("3 / 12 MB", gpuRows["VRAM"]);
        Assert.Equal("66\u00B0C", gpuRows["Temperature"]);
        Assert.Equal("123.5W", gpuRows["Power"]);
        Assert.Equal("2500 MHz (Mem: 7000 MHz)", gpuRows["Clocks"]);
    }

    [Fact]
    public void HardwareRowsPresentation_UsesFallbacksForMissingGpuFields()
    {
        using var culture = CultureScope.Use("en-US");
        var builderType = RequireType("Sussudio.ViewModels.StatsPresentationBuilder");
        var buildGpuRows = builderType.GetMethod("BuildHardwareGpuRows", ReflectionFlags.Static)
                           ?? throw new InvalidOperationException("StatsPresentationBuilder.BuildHardwareGpuRows not found.");

        var fallbackGpuRows = StatsHardwareRowsToMap(buildGpuRows.Invoke(null, new[]
        {
            CreateStatsHardwareNvmlSnapshotWithFallbacks()
        }) ?? throw new InvalidOperationException("BuildHardwareGpuRows returned null for fallback snapshot."));

        Assert.Equal("\u2014", fallbackGpuRows["GPU"]);
        Assert.Equal("0% (Mem: 0%)", fallbackGpuRows["Utilization"]);
        Assert.Equal("0.0 MB/s", fallbackGpuRows["PCIe TX"]);
        Assert.Equal("0 / 0 MB", fallbackGpuRows["VRAM"]);
    }

    private static Type RequireType(string typeName)
        => SussudioAssembly.Load().GetType(typeName, throwOnError: true)!;

    private static object CreateStatsHardwareNvmlSnapshot()
    {
        var inputBuilderType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputBuilder");
        var buildGpuInput = inputBuilderType.GetMethod("BuildGpuRowsInput", ReflectionFlags.Static)
                            ?? throw new InvalidOperationException("StatsHardwareRowsInputBuilder.BuildGpuRowsInput not found.");

        return buildGpuInput.Invoke(null, new[] { CreateStatsHardwareNvmlTelemetrySnapshot() })
               ?? throw new InvalidOperationException("BuildGpuRowsInput returned null.");
    }

    private static object CreateStatsHardwareNvmlTelemetrySnapshot()
        => InvokeStatsHardwareConstructor(
            RequireType("Sussudio.Services.Gpu.NvmlSnapshot"),
            "RTX Test",
            (uint?)41,
            (uint?)52,
            (uint?)13,
            (uint?)17,
            (uint?)1536,
            (uint?)2048,
            (ulong?)(3UL * 1024UL * 1024UL),
            (ulong?)(12UL * 1024UL * 1024UL),
            (uint?)66,
            (uint?)123456,
            (uint?)2500,
            (uint?)7000);

    private static object CreateStatsHardwareNvmlSnapshotWithFallbacks()
    {
        var inputBuilderType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputBuilder");
        var buildGpuInput = inputBuilderType.GetMethod("BuildGpuRowsInput", ReflectionFlags.Static)
                            ?? throw new InvalidOperationException("StatsHardwareRowsInputBuilder.BuildGpuRowsInput not found.");

        return buildGpuInput.Invoke(null, new[] { CreateStatsHardwareNvmlTelemetrySnapshotWithFallbacks() })
               ?? throw new InvalidOperationException("BuildGpuRowsInput returned null for fallback snapshot.");
    }

    private static object CreateStatsHardwareNvmlTelemetrySnapshotWithFallbacks()
        => InvokeStatsHardwareConstructor(
            RequireType("Sussudio.Services.Gpu.NvmlSnapshot"),
            " ",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

    private static object CreateStatsHardwareMjpegMetrics()
    {
        var inputBuilderType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputBuilder");
        var buildDecodeInput = inputBuilderType.GetMethod("BuildDecodeRowsInput", ReflectionFlags.Static)
                               ?? throw new InvalidOperationException("StatsHardwareRowsInputBuilder.BuildDecodeRowsInput not found.");

        return buildDecodeInput.Invoke(null, new object?[] { CreateStatsHardwarePipelineTimingMetrics(), (int?)3 })
               ?? throw new InvalidOperationException("BuildDecodeRowsInput returned null.");
    }

    private static object CreateStatsHardwarePipelineTimingMetrics(int decoderCount = 2)
    {
        var metricsType = RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline+PipelineTimingMetrics");
        var perDecoderType = RequireType("Sussudio.Services.Capture.Mjpeg.ParallelMjpegDecodePipeline+PerDecoderMetrics");
        var perDecoder = Array.CreateInstance(perDecoderType, decoderCount);
        if (decoderCount > 0)
        {
            perDecoder.SetValue(InvokeStatsHardwareConstructor(perDecoderType, 0, 5, 4.5d, 7.75d, 9.5d), 0);
        }

        if (decoderCount > 1)
        {
            perDecoder.SetValue(InvokeStatsHardwareConstructor(perDecoderType, 1, 4, 5.25d, 8.5d, 10.25d), 1);
        }

        return InvokeStatsHardwareConstructor(
            metricsType,
            decoderCount,
            9,
            8.0d,
            12.25d,
            14.0d,
            7,
            0.75d,
            1.5d,
            2.25d,
            11,
            9.25d,
            15.5d,
            20.0d,
            1500L,
            1234L,
            56L,
            1240L,
            1234L,
            1L,
            2L,
            3L,
            4L,
            5L,
            6L,
            7L,
            4,
            5L * 1024L * 1024L,
            10L * 1024L * 1024L,
            2L,
            6,
            8,
            6L * 1024L * 1024L,
            1L,
            perDecoder);
    }

    private static object CreateStatsHardwareRowsInputProvider(
        object? mjpegMetrics,
        int? pendingPreviewFrameCount,
        object? nvmlSnapshot)
    {
        var contextType = RequireType("Sussudio.Controllers.StatsOverlayHardwareSourceContext");
        var providerType = RequireType("Sussudio.Controllers.StatsHardwareRowsInputProvider");
        var context = Activator.CreateInstance(contextType)
                      ?? throw new InvalidOperationException("Failed to create StatsOverlayHardwareSourceContext.");

        SetPropertyOrBackingField(
            context,
            "GetMjpegPipelineTimingDetails",
            CreateStatsHardwareProviderCallback(contextType, "GetMjpegPipelineTimingDetails", () => mjpegMetrics));
        SetPropertyOrBackingField(
            context,
            "GetPendingPreviewFrameCount",
            CreateStatsHardwareProviderCallback(contextType, "GetPendingPreviewFrameCount", () => pendingPreviewFrameCount));
        SetPropertyOrBackingField(
            context,
            "GetNvmlSnapshot",
            CreateStatsHardwareProviderCallback(contextType, "GetNvmlSnapshot", () => nvmlSnapshot));

        return Activator.CreateInstance(providerType, context)
               ?? throw new InvalidOperationException("Failed to create StatsHardwareRowsInputProvider.");
    }

    private static Delegate CreateStatsHardwareProviderCallback(
        Type contextType,
        string propertyName,
        Func<object?> callback)
    {
        var property = contextType.GetProperty(propertyName, ReflectionFlags.Instance)
                       ?? throw new InvalidOperationException($"Missing provider context property '{propertyName}'.");
        var invoke = typeof(Func<object?>).GetMethod(nameof(Func<object?>.Invoke))
                     ?? throw new InvalidOperationException("Missing Func<object?>.Invoke.");
        var returnType = property.PropertyType.GetMethod(nameof(Func<object?>.Invoke))?.ReturnType
                         ?? throw new InvalidOperationException($"Provider context property '{propertyName}' was not a delegate.");
        var callbackValue = Expression.Call(Expression.Constant(callback), invoke);
        var convertedValue = Expression.Convert(callbackValue, returnType);
        return Expression.Lambda(property.PropertyType, convertedValue).Compile();
    }

    private static object InvokeStatsHardwareConstructor(Type type, params object?[] arguments)
    {
        var constructor = type.GetConstructors(ReflectionFlags.Instance)
            .Single(candidate => candidate.GetParameters().Length == arguments.Length);
        return constructor.Invoke(arguments);
    }

    private static Dictionary<string, string> StatsHardwareRowsToMap(object rows)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in (IEnumerable)rows)
        {
            if (row == null)
            {
                continue;
            }

            map[GetStringProperty(row, "Label")] = GetStringProperty(row, "Value");
        }

        return map;
    }

    private static void SetPropertyOrBackingField(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance);
        if (property?.CanWrite == true)
        {
            property.SetValue(instance, value);
            return;
        }

        var field = instance.GetType().GetField($"<{propertyName}>k__BackingField", ReflectionFlags.Instance)
                    ?? throw new InvalidOperationException($"Property or backing field '{propertyName}' was not found.");
        field.SetValue(instance, value);
    }

    private static object? GetPropertyValue(object instance, string propertyName)
        => instance.GetType().GetProperty(propertyName, ReflectionFlags.Instance)!.GetValue(instance);

    private static string GetStringProperty(object instance, string propertyName)
        => GetPropertyValue(instance, propertyName) as string
           ?? throw new InvalidOperationException($"{propertyName} was not a string.");

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previousCulture;
        private readonly CultureInfo _previousUiCulture;

        private CultureScope(CultureInfo culture)
        {
            _previousCulture = CultureInfo.CurrentCulture;
            _previousUiCulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public static CultureScope Use(string cultureName)
            => new(CultureInfo.GetCultureInfo(cultureName));

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _previousCulture;
            CultureInfo.CurrentUICulture = _previousUiCulture;
        }
    }
}
