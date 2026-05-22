using System;
using System.Reflection;
using Xunit;

namespace Sussudio.Tests;

public sealed class AutomationOptionsSnapshotBuilderTests
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
            ("SelectedDeviceId", "device-b"),
            ("SelectedAudioInputDeviceId", "AUDIO-A"),
            ("SelectedResolution", "1920X1080"),
            ("SelectedFrameRate", 60d),
            ("SelectedRecordingFormat", "av1"),
            ("SelectedQuality", "medium"),
            ("SelectedPreset", "speed"),
            ("SelectedSplitEncodeMode", "disabled"),
            ("SelectedVideoFormat", "mjpg"),
            ("MjpegDecoderCount", 99),
            ("PreviewVolume", 0.425d),
            ("IsStatsVisible", true));

        var build = builderType.GetMethod("Build", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        var snapshot = build.Invoke(null, new[] { input })!;

        Assert.Equal(timestamp, Get(snapshot, "TimestampUtc"));
        Assert.Equal("device-b", Get(snapshot, "SelectedDeviceId"));
        Assert.Equal("AUDIO-A", Get(snapshot, "SelectedAudioInputDeviceId"));
        Assert.Equal("1920X1080", Get(snapshot, "SelectedResolution"));
        Assert.Equal(60d, Get(snapshot, "SelectedFrameRate"));
        Assert.Equal(8, Get(snapshot, "MjpegDecoderCount"));
        Assert.Equal(42.5d, Get(snapshot, "PreviewVolumePercent"));
        Assert.True((bool)Get(snapshot, "IsStatsVisible")!);

        var devices = (Array)Get(snapshot, "Devices")!;
        Assert.False((bool)Get(devices.GetValue(0)!, "IsSelected")!);
        Assert.True((bool)Get(devices.GetValue(1)!, "IsSelected")!);

        var audioDevices = (Array)Get(snapshot, "AudioInputDevices")!;
        Assert.True((bool)Get(audioDevices.GetValue(0)!, "IsSelected")!);

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
}
