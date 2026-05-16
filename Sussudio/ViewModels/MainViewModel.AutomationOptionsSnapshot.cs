using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation-facing option and selected-control-state projection for CLI, MCP,
/// and diagnostics clients.
/// </summary>
public partial class MainViewModel
{
    public Task<AutomationOptionsSnapshot> GetAutomationOptionsSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            var selectedFrameRate = SelectedFrameRate;
            var input = new AutomationOptionsSnapshotInput
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Devices = Devices
                    .Select(device => new AutomationOptionsDeviceInput
                    {
                        Id = device.Id,
                        Name = device.Name
                    })
                    .ToArray(),
                AudioInputDevices = AudioInputDevices
                    .Select(device => new AutomationOptionsDeviceInput
                    {
                        Id = device.Id,
                        Name = device.Name
                    })
                    .ToArray(),
                Resolutions = AvailableResolutions
                    .Select(option => new AutomationOptionsResolutionInput
                    {
                        Value = option.Value,
                        Width = option.Width,
                        Height = option.Height,
                        IsEnabled = option.IsEnabled,
                        DisableReason = option.DisableReason
                    })
                    .ToArray(),
                FrameRates = AvailableFrameRates
                    .Select(option => new AutomationOptionsFrameRateInput
                    {
                        Value = option.Value,
                        FriendlyValue = option.FriendlyValue,
                        ExactValueArg = option.Rational,
                        IsEnabled = option.IsEnabled,
                        DisableReason = option.DisableReason,
                        IsSelected = FrameRateTimingPolicy.IsFrameRateMatch(option.Value, selectedFrameRate)
                    })
                    .ToArray(),
                RecordingFormats = AvailableRecordingFormats.ToArray(),
                Qualities = AvailableQualities.ToArray(),
                Presets = AvailablePresets.ToArray(),
                SplitEncodeModes = AvailableSplitEncodeModes.ToArray(),
                VideoFormats = AvailableVideoFormats.ToArray(),
                SelectedDeviceId = SelectedDevice?.Id,
                SelectedAudioInputDeviceId = SelectedAudioInputDevice?.Id,
                SelectedResolution = SelectedResolution,
                SelectedFrameRate = selectedFrameRate,
                SelectedRecordingFormat = SelectedRecordingFormat,
                SelectedQuality = SelectedQuality,
                SelectedPreset = SelectedPreset,
                SelectedSplitEncodeMode = SelectedSplitEncodeMode,
                SelectedVideoFormat = SelectedVideoFormat,
                MjpegDecoderCount = MjpegDecoderCount,
                ShowAllCaptureOptions = ShowAllCaptureOptions,
                PreviewVolume = PreviewVolume,
                IsStatsVisible = IsStatsVisible
            };

            return AutomationOptionsSnapshotBuilder.Build(input);
        }, cancellationToken);
    }
}
