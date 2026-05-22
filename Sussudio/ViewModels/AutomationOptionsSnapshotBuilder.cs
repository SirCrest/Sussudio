using System;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

internal static class AutomationOptionsSnapshotBuilder
{
    internal static AutomationOptionsSnapshot Build(AutomationOptionsSnapshotInput input)
    {
        var clampedDecoderCount = Math.Clamp(input.MjpegDecoderCount, 1, 8);

        return new AutomationOptionsSnapshot
        {
            TimestampUtc = input.TimestampUtc,
            Devices = input.Devices
                .Select(device => new AutomationDeviceOption
                {
                    Id = device.Id,
                    Name = device.Name,
                    IsSelected = string.Equals(device.Id, input.SelectedDeviceId, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray(),
            AudioInputDevices = input.AudioInputDevices
                .Select(device => new AutomationDeviceOption
                {
                    Id = device.Id,
                    Name = device.Name,
                    IsSelected = string.Equals(device.Id, input.SelectedAudioInputDeviceId, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray(),
            Resolutions = input.Resolutions
                .Select(option => new AutomationResolutionOption
                {
                    Value = option.Value,
                    Width = (int)option.Width,
                    Height = (int)option.Height,
                    IsEnabled = option.IsEnabled,
                    DisableReason = option.DisableReason ?? string.Empty,
                    IsSelected = string.Equals(option.Value, input.SelectedResolution, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray(),
            FrameRates = input.FrameRates
                .Select(option => new AutomationFrameRateOption
                {
                    Value = option.Value,
                    FriendlyValue = option.FriendlyValue,
                    ExactValueArg = option.ExactValueArg ?? string.Empty,
                    IsEnabled = option.IsEnabled,
                    DisableReason = option.DisableReason ?? string.Empty,
                    IsSelected = option.IsSelected
                })
                .ToArray(),
            RecordingFormats = BuildStringOptions(input.RecordingFormats, input.SelectedRecordingFormat),
            Qualities = BuildStringOptions(input.Qualities, input.SelectedQuality),
            Presets = BuildStringOptions(input.Presets, input.SelectedPreset),
            SplitEncodeModes = BuildStringOptions(input.SplitEncodeModes, input.SelectedSplitEncodeMode),
            VideoFormats = BuildStringOptions(input.VideoFormats, input.SelectedVideoFormat),
            MjpegDecoderCounts = Enumerable.Range(1, 8)
                .Select(value => new AutomationIntOption
                {
                    Value = value,
                    IsSelected = value == clampedDecoderCount
                })
                .ToArray(),
            SelectedDeviceId = input.SelectedDeviceId,
            SelectedAudioInputDeviceId = input.SelectedAudioInputDeviceId,
            SelectedResolution = input.SelectedResolution,
            SelectedFrameRate = input.SelectedFrameRate,
            SelectedRecordingFormat = input.SelectedRecordingFormat,
            SelectedQuality = input.SelectedQuality,
            SelectedPreset = input.SelectedPreset,
            SelectedSplitEncodeMode = input.SelectedSplitEncodeMode,
            SelectedVideoFormat = input.SelectedVideoFormat,
            MjpegDecoderCount = clampedDecoderCount,
            PreviewVolumePercent = input.PreviewVolume * 100.0,
            IsStatsVisible = input.IsStatsVisible
        };
    }

    private static AutomationStringOption[] BuildStringOptions(
        string[] values,
        string selectedValue)
    {
        return values
            .Select(value => new AutomationStringOption
            {
                Value = value,
                Label = value,
                IsEnabled = true,
                DisableReason = string.Empty,
                IsSelected = string.Equals(value, selectedValue, StringComparison.OrdinalIgnoreCase)
            })
            .ToArray();
    }
}

internal sealed class AutomationOptionsSnapshotInput
{
    public DateTimeOffset TimestampUtc { get; init; }
    public AutomationOptionsDeviceInput[] Devices { get; init; } = Array.Empty<AutomationOptionsDeviceInput>();
    public AutomationOptionsDeviceInput[] AudioInputDevices { get; init; } = Array.Empty<AutomationOptionsDeviceInput>();
    public AutomationOptionsResolutionInput[] Resolutions { get; init; } = Array.Empty<AutomationOptionsResolutionInput>();
    public AutomationOptionsFrameRateInput[] FrameRates { get; init; } = Array.Empty<AutomationOptionsFrameRateInput>();
    public string[] RecordingFormats { get; init; } = Array.Empty<string>();
    public string[] Qualities { get; init; } = Array.Empty<string>();
    public string[] Presets { get; init; } = Array.Empty<string>();
    public string[] SplitEncodeModes { get; init; } = Array.Empty<string>();
    public string[] VideoFormats { get; init; } = Array.Empty<string>();
    public string? SelectedDeviceId { get; init; }
    public string? SelectedAudioInputDeviceId { get; init; }
    public string? SelectedResolution { get; init; }
    public double SelectedFrameRate { get; init; }
    public string SelectedRecordingFormat { get; init; } = string.Empty;
    public string SelectedQuality { get; init; } = string.Empty;
    public string SelectedPreset { get; init; } = string.Empty;
    public string SelectedSplitEncodeMode { get; init; } = string.Empty;
    public string SelectedVideoFormat { get; init; } = string.Empty;
    public int MjpegDecoderCount { get; init; }
    public double PreviewVolume { get; init; }
    public bool IsStatsVisible { get; init; }
}

internal sealed class AutomationOptionsDeviceInput
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

internal sealed class AutomationOptionsResolutionInput
{
    public string Value { get; init; } = string.Empty;
    public uint Width { get; init; }
    public uint Height { get; init; }
    public bool IsEnabled { get; init; }
    public string? DisableReason { get; init; }
}

internal sealed class AutomationOptionsFrameRateInput
{
    public double Value { get; init; }
    public double FriendlyValue { get; init; }
    public string? ExactValueArg { get; init; }
    public bool IsEnabled { get; init; }
    public string? DisableReason { get; init; }
    public bool IsSelected { get; init; }
}
