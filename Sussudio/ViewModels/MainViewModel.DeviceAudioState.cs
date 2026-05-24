using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Sussudio.Models;
using Sussudio.Services.Telemetry;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private string? _pendingSavedDeviceAudioMode;
    private double? _pendingSavedAnalogAudioGainPercent;
    private bool _isRefreshingDeviceAudioControls;

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableDeviceAudioModes { get; set; } = new()
    {
        DeviceAudioMode.Hdmi,
        DeviceAudioMode.Analog
    };

    [ObservableProperty]
    public partial bool IsDeviceAudioControlSupported { get; set; }

    [ObservableProperty]
    public partial string SelectedDeviceAudioMode { get; set; } = DeviceAudioMode.Hdmi;

    [ObservableProperty]
    public partial double AnalogAudioGainPercent { get; set; } = 50;

    partial void OnSelectedDeviceAudioModeChanged(string value)
        => _deviceAudioRequestController.HandleSelectedDeviceAudioModeChanged(value);

    partial void OnAnalogAudioGainPercentChanged(double value)
        => _deviceAudioRequestController.HandleAnalogAudioGainPercentChanged(value);

    private void RequestDeviceAudioControlsRefresh(CaptureDevice? targetDevice)
        => _deviceAudioRequestController.RequestDeviceAudioControlsRefresh(targetDevice);

    private void RequestAnalogGainFlashPersist(CaptureDevice device, byte gainByte)
        => _deviceAudioRequestController.ScheduleAnalogGainFlashPersist(device, gainByte);

    private void CancelPendingAudioControlWork()
        => _deviceAudioRequestController.CancelPendingAudioControlWork();

    private bool IsCurrentSelectedDevice(CaptureDevice device)
    {
        var selected = SelectedDevice;
        if (selected == null)
        {
            return false;
        }

        return string.Equals(selected.Id, device.Id, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(selected.NativeXuInterfacePath, device.NativeXuInterfacePath, StringComparison.OrdinalIgnoreCase);
    }

    private void WithAudioControlRefreshSuppressed(Action action)
    {
        _isRefreshingDeviceAudioControls = true;
        try
        {
            action();
        }
        finally
        {
            _isRefreshingDeviceAudioControls = false;
        }
    }

    private string NormalizeDeviceAudioMode(string? mode)
        => string.Equals(mode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase)
            ? DeviceAudioMode.Analog
            : DeviceAudioMode.Hdmi;

    private async Task<bool> ApplyAnalogAudioGainAsync(
        string reason,
        double? explicitPercent = null,
        bool persistSettings = true,
        CaptureDevice? targetDevice = null,
        CancellationToken cancellationToken = default)
    {
        var device = targetDevice ?? SelectedDevice;
        if (device == null || !IsDeviceAudioControlSupported)
        {
            return false;
        }

        if (!IsCurrentSelectedDevice(device))
        {
            Logger.Log($"Analog audio gain skipped because selected device changed ({reason})");
            return false;
        }

        var gainPercent = Math.Clamp(explicitPercent ?? AnalogAudioGainPercent, 0.0, 100.0);
        var gainByte = DeviceAudioGainMapper.PercentToGainByte(gainPercent);
        Logger.Log($"=== Updating analog audio gain ({reason}) ===");
        Logger.Log($"  GainPercent: {gainPercent:0} GainByte: 0x{gainByte:X2}");

        var applied = await NativeXuAtCommandProvider.SetAnalogGainAsync(device, gainByte, persistFlash: false, cancellationToken).ConfigureAwait(false);

        if (!applied)
        {
            StatusText = $"Analog audio gain change failed ({gainPercent:0}%)";
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!IsCurrentSelectedDevice(device))
        {
            Logger.Log($"Analog audio gain result ignored because selected device changed ({reason})");
            return false;
        }

        StatusText = $"Analog audio gain set to {gainPercent:0}%";
        WithAudioControlRefreshSuppressed(() => AnalogAudioGainPercent = gainPercent);
        RequestAnalogGainFlashPersist(device, gainByte);

        if (persistSettings)
        {
            SaveSettings();
        }

        return true;
    }
}

internal static class DeviceAudioGainMapper
{
    private const double GainCurveK = 4.0;

    internal static byte PercentToGainByte(double percent)
    {
        var x = Math.Clamp(percent / 100.0, 0.0, 1.0);
        var curved = Math.Log(1.0 + x * (Math.Exp(GainCurveK) - 1.0)) / GainCurveK;
        return (byte)Math.Clamp(Math.Round(curved * 255.0), 0, 255);
    }

    internal static double GainByteToPercent(byte gainByte)
    {
        var y = gainByte / 255.0;
        var x = (Math.Exp(GainCurveK * y) - 1.0) / (Math.Exp(GainCurveK) - 1.0);
        return Math.Clamp(x * 100.0, 0.0, 100.0);
    }
}
