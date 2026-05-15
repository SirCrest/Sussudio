using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation-facing option-list projection for CLI, MCP, and diagnostics clients.
/// </summary>
public partial class MainViewModel
{
    public Task<AutomationOptionsSnapshot> GetAutomationOptionsSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() => new AutomationOptionsSnapshot
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Devices = Devices
                .Select(device => new AutomationDeviceOption
                {
                    Id = device.Id,
                    Name = device.Name,
                    IsSelected = string.Equals(device.Id, SelectedDevice?.Id, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray(),
            AudioInputDevices = AudioInputDevices
                .Select(device => new AutomationDeviceOption
                {
                    Id = device.Id,
                    Name = device.Name,
                    IsSelected = string.Equals(device.Id, SelectedAudioInputDevice?.Id, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray(),
            Resolutions = AvailableResolutions
                .Select(option => new AutomationResolutionOption
                {
                    Value = option.Value,
                    Width = (int)option.Width,
                    Height = (int)option.Height,
                    IsEnabled = option.IsEnabled,
                    DisableReason = option.DisableReason ?? string.Empty,
                    IsSelected = string.Equals(option.Value, SelectedResolution, StringComparison.OrdinalIgnoreCase)
                })
                .ToArray(),
            FrameRates = AvailableFrameRates
                .Select(option => new AutomationFrameRateOption
                {
                    Value = option.Value,
                    FriendlyValue = option.FriendlyValue,
                    ExactValueArg = option.Rational ?? string.Empty,
                    IsEnabled = option.IsEnabled,
                    DisableReason = option.DisableReason ?? string.Empty,
                    IsSelected = IsFrameRateMatch(option.Value, SelectedFrameRate)
                })
                .ToArray(),
            RecordingFormats = BuildStringOptions(AvailableRecordingFormats, SelectedRecordingFormat),
            Qualities = BuildStringOptions(AvailableQualities, SelectedQuality),
            Presets = BuildStringOptions(AvailablePresets, SelectedPreset),
            SplitEncodeModes = BuildStringOptions(AvailableSplitEncodeModes, SelectedSplitEncodeMode),
            VideoFormats = BuildStringOptions(AvailableVideoFormats, SelectedVideoFormat),
            MjpegDecoderCounts = Enumerable.Range(1, 8)
                .Select(value => new AutomationIntOption
                {
                    Value = value,
                    IsSelected = value == Math.Clamp(MjpegDecoderCount, 1, 8)
                })
                .ToArray(),
            SelectedDeviceId = SelectedDevice?.Id,
            SelectedAudioInputDeviceId = SelectedAudioInputDevice?.Id,
            SelectedResolution = SelectedResolution,
            SelectedFrameRate = SelectedFrameRate,
            SelectedRecordingFormat = SelectedRecordingFormat,
            SelectedQuality = SelectedQuality,
            SelectedPreset = SelectedPreset,
            SelectedSplitEncodeMode = SelectedSplitEncodeMode,
            SelectedVideoFormat = SelectedVideoFormat,
            MjpegDecoderCount = Math.Clamp(MjpegDecoderCount, 1, 8),
            ShowAllCaptureOptions = ShowAllCaptureOptions,
            PreviewVolumePercent = PreviewVolume * 100.0,
            IsStatsVisible = IsStatsVisible
        }, cancellationToken);
    }

    private static AutomationStringOption[] BuildStringOptions(
        IEnumerable<string> values,
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
