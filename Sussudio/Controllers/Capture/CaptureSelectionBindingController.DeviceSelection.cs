using System;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed partial class CaptureSelectionBindingController
{
    public void AttachDeviceSelectionChangedBinding()
    {
        _context.DeviceComboBox.SelectionChanged += (_, _) => UpdateDeviceApplyButtonState();
    }

    public void EnsureDeviceSelection()
    {
        if (_context.ViewModel.Devices.Count == 0)
        {
            _context.DeviceComboBox.SelectedItem = null;
            return;
        }

        var matchingDevice = CaptureComboBoxSelectionNormalizer.ResolveCaptureDeviceSelection(
            _context.ViewModel.Devices,
            _context.ViewModel.SelectedDevice);
        if (matchingDevice == null)
        {
            return;
        }

        if (!ReferenceEquals(_context.ViewModel.SelectedDevice, matchingDevice))
        {
            _context.ViewModel.SelectedDevice = matchingDevice;
        }

        if (!ReferenceEquals(_context.DeviceComboBox.SelectedItem, matchingDevice))
        {
            _context.DeviceComboBox.SelectedItem = matchingDevice;
        }

        UpdateDeviceApplyButtonState();
    }

    public void HandleSelectedDevicePropertyChanged()
    {
        var selectedDevice = (CaptureDevice?)_context.DeviceComboBox.SelectedItem;
        if (!string.Equals(selectedDevice?.Id, _context.ViewModel.SelectedDevice?.Id, StringComparison.Ordinal))
        {
            Sussudio.Logger.Log(
                $"DEVICE_SELECTION_SYNC viewModel='{_context.ViewModel.SelectedDevice?.Name ?? "NULL"}' combo='{selectedDevice?.Name ?? "NULL"}' devices={_context.ViewModel.Devices.Count} comboItems={_context.DeviceComboBox.Items.Count}");
        }

        EnsureDeviceSelection();
        UpdateDeviceApplyButtonState();
    }

    public bool HasPendingDeviceSelection()
    {
        if (_context.DeviceComboBox.SelectedItem is not CaptureDevice selectedDevice)
        {
            return false;
        }

        return !string.Equals(
            selectedDevice.Id,
            _context.ViewModel.SelectedDevice?.Id,
            StringComparison.OrdinalIgnoreCase);
    }

    public void UpdateDeviceApplyButtonState()
    {
        if (_context.ApplyDeviceButton == null)
        {
            return;
        }

        _context.ApplyDeviceButton.IsEnabled =
            HasPendingDeviceSelection() &&
            !_context.ViewModel.IsRecording &&
            !_context.ViewModel.IsPreviewReinitializing;
    }
}
