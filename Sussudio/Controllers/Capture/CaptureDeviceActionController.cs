using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class CaptureDeviceActionControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required Button RefreshButton { get; init; }
    public required Button ApplyDeviceButton { get; init; }
    public required ComboBox DeviceComboBox { get; init; }
    public required Action UpdateDeviceApplyButtonState { get; init; }
}

internal sealed class CaptureDeviceActionController
{
    private readonly CaptureDeviceActionControllerContext _context;

    public CaptureDeviceActionController(CaptureDeviceActionControllerContext context)
    {
        _context = context;
    }

    public async Task RefreshDevicesAsync()
    {
        _context.RefreshButton.Content = new ProgressRing { Width = 16, Height = 16, IsActive = true };
        _context.RefreshButton.IsEnabled = false;
        try
        {
            await _context.ViewModel.RefreshDevicesAsync();
        }
        finally
        {
            _context.RefreshButton.Content = new FontIcon { Glyph = "\uE72C", FontSize = 14 };
            _context.RefreshButton.IsEnabled = true;
        }
    }

    public async Task ApplySelectedDeviceAsync()
    {
        if (_context.DeviceComboBox.SelectedItem is not CaptureDevice selectedDevice)
        {
            return;
        }

        _context.ApplyDeviceButton.IsEnabled = false;
        try
        {
            await _context.ViewModel.ApplySelectedDeviceAsync(selectedDevice);
        }
        finally
        {
            _context.UpdateDeviceApplyButtonState();
        }
    }
}
