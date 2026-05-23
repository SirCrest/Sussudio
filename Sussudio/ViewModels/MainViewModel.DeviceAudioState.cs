using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Sussudio.Models;

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
}
