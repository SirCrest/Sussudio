using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal static class CaptureComboBoxSelectionNormalizer
{
    public static CaptureDevice? ResolveCaptureDeviceSelection(
        IReadOnlyList<CaptureDevice> devices,
        CaptureDevice? selectedDevice)
        => ResolveDeviceSelection(devices, selectedDevice?.Id, device => device.Id);

    public static AudioInputDevice? ResolveAudioInputDeviceSelection(
        IReadOnlyList<AudioInputDevice> devices,
        AudioInputDevice? selectedDevice)
        => ResolveDeviceSelection(devices, selectedDevice?.Id, device => device.Id);

    public static ResolutionOption? ResolveResolutionSelection(
        IReadOnlyList<ResolutionOption> options,
        string? selectedResolution)
    {
        if (options.Count == 0)
        {
            return null;
        }

        return options.FirstOrDefault(option =>
                string.Equals(option.Value, selectedResolution, StringComparison.OrdinalIgnoreCase))
            ?? options.FirstOrDefault(option => option.IsEnabled)
            ?? options.FirstOrDefault();
    }

    public static FrameRateOption? ResolveFrameRateSelection(
        IReadOnlyList<FrameRateOption> options,
        double selectedFrameRate,
        bool isAutoFrameRateSelected)
    {
        if (options.Count == 0)
        {
            return null;
        }

        if (isAutoFrameRateSelected)
        {
            var autoOption = options.FirstOrDefault(IsAutoFrameRateOption);
            if (autoOption != null)
            {
                return autoOption;
            }
        }

        return options.FirstOrDefault(option => IsFrameRateMatch(option.Value, selectedFrameRate))
            ?? options.FirstOrDefault(option => option.IsEnabled)
            ?? options.FirstOrDefault();
    }

    public static string? ResolveStringSelection(
        IReadOnlyList<string> items,
        string? selectedValue)
    {
        if (items.Count == 0)
        {
            return null;
        }

        var match = items.FirstOrDefault(item => string.Equals(item, selectedValue, StringComparison.OrdinalIgnoreCase))
            ?? items.FirstOrDefault();
        return string.IsNullOrWhiteSpace(match) ? null : match;
    }

    public static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)
        => Math.Abs(a - b) < tolerance;

    public static bool IsAutoFrameRateOption(FrameRateOption option)
        => option.Value <= 0 || option.FriendlyValue <= 0;

    private static TDevice? ResolveDeviceSelection<TDevice>(
        IReadOnlyList<TDevice> devices,
        string? selectedDeviceId,
        Func<TDevice, string?> getDeviceId)
        where TDevice : class
    {
        if (devices.Count == 0)
        {
            return default;
        }

        var matchingDevice = selectedDeviceId != null
            ? devices.FirstOrDefault(device =>
                string.Equals(getDeviceId(device), selectedDeviceId, StringComparison.OrdinalIgnoreCase))
            : default;
        return matchingDevice ?? devices.FirstOrDefault();
    }
}
