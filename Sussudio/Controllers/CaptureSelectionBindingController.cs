using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed partial class CaptureSelectionBindingController
{
    private readonly CaptureSelectionBindingControllerContext _context;

    public CaptureSelectionBindingController(CaptureSelectionBindingControllerContext context)
    {
        _context = context;
    }

    public void AttachCollectionBindings()
    {
        _context.DeviceComboBox.ItemsSource = _context.ViewModel.Devices;
        _context.AudioInputComboBox.ItemsSource = _context.ViewModel.AudioInputDevices;
        _context.MicrophoneComboBox.ItemsSource = _context.ViewModel.MicrophoneDevices;
        _context.ResolutionComboBox.ItemsSource = _context.ViewModel.AvailableResolutions;
        _context.FrameRateComboBox.ItemsSource = _context.ViewModel.AvailableFrameRates;
        _context.FormatComboBox.ItemsSource = _context.ViewModel.AvailableRecordingFormats;
        _context.QualityComboBox.ItemsSource = _context.ViewModel.AvailableQualities;
        _context.PresetComboBox.ItemsSource = _context.ViewModel.AvailablePresets;
        _context.SplitEncodeComboBox.ItemsSource = _context.ViewModel.AvailableSplitEncodeModes;

        AttachCollectionSync(_context.ViewModel.Devices, QueueDeviceSelectionSync);
        AttachCollectionSync(_context.ViewModel.AudioInputDevices, QueueAudioSelectionSync);
        AttachCollectionSync(_context.ViewModel.MicrophoneDevices, QueueMicrophoneSelectionSync);
        AttachCollectionSync(_context.ViewModel.AvailableResolutions, QueueResolutionSelectionSync);
        AttachCollectionSync(_context.ViewModel.AvailableFrameRates, QueueFrameRateSelectionSync);
        AttachCollectionSync(_context.ViewModel.AvailableRecordingFormats, QueueFormatSelectionSync);
        AttachCollectionSync(_context.ViewModel.AvailableQualities, QueueQualitySelectionSync);
        AttachCollectionSync(_context.ViewModel.AvailablePresets, QueuePresetSelectionSync);
        AttachCollectionSync(_context.ViewModel.AvailableSplitEncodeModes, QueueSplitEncodeModeSelectionSync);
    }

    public void AttachRecordingStringSelectionBindings()
    {
        AttachStringSelection(_context.FormatComboBox, value => _context.ViewModel.SelectedRecordingFormat = value);
        AttachStringSelection(_context.QualityComboBox, value => _context.ViewModel.SelectedQuality = value);
        AttachStringSelection(_context.PresetComboBox, value => _context.ViewModel.SelectedPreset = value);
        AttachStringSelection(_context.SplitEncodeComboBox, value => _context.ViewModel.SelectedSplitEncodeMode = value);
    }

    public void EnsureDeviceSelection()
    {
        if (_context.ViewModel.Devices.Count == 0)
        {
            _context.DeviceComboBox.SelectedItem = null;
            return;
        }

        var matchingDevice = _context.ViewModel.SelectedDevice != null
            ? _context.ViewModel.Devices.FirstOrDefault(device =>
                string.Equals(device.Id, _context.ViewModel.SelectedDevice.Id, StringComparison.OrdinalIgnoreCase))
            : null;
        matchingDevice ??= _context.ViewModel.Devices.FirstOrDefault();
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

    public void EnsureAudioInputSelection()
    {
        if (_context.ViewModel.AudioInputDevices.Count == 0)
        {
            _context.AudioInputComboBox.SelectedItem = null;
            return;
        }

        var matchingDevice = _context.ViewModel.SelectedAudioInputDevice != null
            ? _context.ViewModel.AudioInputDevices.FirstOrDefault(device =>
                string.Equals(device.Id, _context.ViewModel.SelectedAudioInputDevice.Id, StringComparison.OrdinalIgnoreCase))
            : null;
        matchingDevice ??= _context.ViewModel.AudioInputDevices.FirstOrDefault();
        if (matchingDevice == null)
        {
            return;
        }

        if (!ReferenceEquals(_context.ViewModel.SelectedAudioInputDevice, matchingDevice))
        {
            _context.ViewModel.SelectedAudioInputDevice = matchingDevice;
        }

        if (!ReferenceEquals(_context.AudioInputComboBox.SelectedItem, matchingDevice))
        {
            _context.AudioInputComboBox.SelectedItem = matchingDevice;
        }
    }

    public void EnsureMicrophoneSelection()
    {
        if (_context.ViewModel.MicrophoneDevices.Count == 0)
        {
            _context.MicrophoneComboBox.SelectedItem = null;
            return;
        }

        var matchingDevice = _context.ViewModel.SelectedMicrophoneDevice != null
            ? _context.ViewModel.MicrophoneDevices.FirstOrDefault(device =>
                string.Equals(device.Id, _context.ViewModel.SelectedMicrophoneDevice.Id, StringComparison.OrdinalIgnoreCase))
            : null;
        matchingDevice ??= _context.ViewModel.MicrophoneDevices.FirstOrDefault();
        if (matchingDevice == null)
        {
            return;
        }

        if (!ReferenceEquals(_context.ViewModel.SelectedMicrophoneDevice, matchingDevice))
        {
            _context.ViewModel.SelectedMicrophoneDevice = matchingDevice;
        }

        if (!ReferenceEquals(_context.MicrophoneComboBox.SelectedItem, matchingDevice))
        {
            _context.MicrophoneComboBox.SelectedItem = matchingDevice;
        }
    }

    public void EnsureResolutionSelection()
    {
        if (_context.ViewModel.AvailableResolutions.Count == 0)
        {
            if (_context.ViewModel.SelectedDevice == null || !_context.ViewModel.IsPreviewing)
            {
                _context.ResolutionComboBox.SelectedItem = null;
            }

            return;
        }

        var matchingResolution = _context.ViewModel.AvailableResolutions.FirstOrDefault(option =>
            string.Equals(option.Value, _context.ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase))
            ?? _context.ViewModel.AvailableResolutions.FirstOrDefault(option => option.IsEnabled)
            ?? _context.ViewModel.AvailableResolutions.FirstOrDefault();
        if (matchingResolution == null)
        {
            return;
        }

        if (!string.Equals(matchingResolution.Value, _context.ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase))
        {
            _context.ViewModel.SelectedResolution = matchingResolution.Value;
        }

        if (_context.ResolutionComboBox.SelectedItem is not ResolutionOption selectedResolutionOption ||
            !string.Equals(selectedResolutionOption.Value, matchingResolution.Value, StringComparison.OrdinalIgnoreCase))
        {
            _context.ResolutionComboBox.SelectedItem = matchingResolution;
        }
    }

    public void EnsureFrameRateSelection()
    {
        if (_context.ViewModel.AvailableFrameRates.Count == 0)
        {
            if (_context.ViewModel.SelectedDevice == null || !_context.ViewModel.IsPreviewing)
            {
                _context.FrameRateComboBox.SelectedItem = null;
            }

            return;
        }

        if (_context.ViewModel.IsAutoFrameRateSelected)
        {
            var autoOption = _context.ViewModel.AvailableFrameRates
                .FirstOrDefault(IsAutoFrameRateOption);
            if (autoOption != null)
            {
                if (!ReferenceEquals(_context.FrameRateComboBox.SelectedItem, autoOption))
                {
                    _context.FrameRateComboBox.SelectedItem = autoOption;
                }

                return;
            }
        }

        var matchingRate = _context.ViewModel.AvailableFrameRates
            .FirstOrDefault(option => IsFrameRateMatch(option.Value, _context.ViewModel.SelectedFrameRate))
            ?? _context.ViewModel.AvailableFrameRates.FirstOrDefault(option => option.IsEnabled)
            ?? _context.ViewModel.AvailableFrameRates.FirstOrDefault();
        if (matchingRate == null)
        {
            return;
        }

        if (!IsFrameRateMatch(matchingRate.Value, _context.ViewModel.SelectedFrameRate))
        {
            _context.ViewModel.SelectedFrameRate = matchingRate.Value;
        }

        if (_context.FrameRateComboBox.SelectedItem is not FrameRateOption currentFps ||
            !IsFrameRateMatch(currentFps.Value, matchingRate.Value))
        {
            _context.FrameRateComboBox.SelectedItem = matchingRate;
        }
    }

    public void EnsureFormatSelection()
    {
        if (_context.ViewModel.AvailableRecordingFormats.Count == 0)
        {
            if (_context.ViewModel.SelectedDevice == null || !_context.ViewModel.IsPreviewing)
            {
                _context.FormatComboBox.SelectedItem = null;
            }

            return;
        }

        EnsureStringComboBoxSelection(
            _context.FormatComboBox,
            _context.ViewModel.AvailableRecordingFormats,
            () => _context.ViewModel.SelectedRecordingFormat,
            value => _context.ViewModel.SelectedRecordingFormat = value);
    }

    public void EnsureQualitySelection() =>
        EnsureStringComboBoxSelection(
            _context.QualityComboBox,
            _context.ViewModel.AvailableQualities,
            () => _context.ViewModel.SelectedQuality,
            value => _context.ViewModel.SelectedQuality = value);

    public void EnsurePresetSelection() =>
        EnsureStringComboBoxSelection(
            _context.PresetComboBox,
            _context.ViewModel.AvailablePresets,
            () => _context.ViewModel.SelectedPreset,
            value => _context.ViewModel.SelectedPreset = value);

    public void EnsureSplitEncodeModeSelection() =>
        EnsureStringComboBoxSelection(
            _context.SplitEncodeComboBox,
            _context.ViewModel.AvailableSplitEncodeModes,
            () => _context.ViewModel.SelectedSplitEncodeMode,
            value => _context.ViewModel.SelectedSplitEncodeMode = value);

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

    private static void EnsureStringComboBoxSelection(
        ComboBox comboBox,
        ObservableCollection<string> items,
        Func<string?> getVmProp,
        Action<string> setVmProp)
    {
        if (items.Count == 0)
        {
            comboBox.SelectedItem = null;
            return;
        }

        var vmValue = getVmProp();
        var match = items.FirstOrDefault(item => string.Equals(item, vmValue, StringComparison.OrdinalIgnoreCase))
            ?? items.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(match))
        {
            return;
        }

        if (!string.Equals(match, vmValue, StringComparison.OrdinalIgnoreCase))
        {
            setVmProp(match);
        }

        if (!string.Equals(comboBox.SelectedItem as string, match, StringComparison.OrdinalIgnoreCase))
        {
            comboBox.SelectedItem = match;
        }
    }

    private static void AttachStringSelection(ComboBox comboBox, Action<string> setVmProp)
    {
        comboBox.SelectionChanged += (s, e) =>
        {
            if (comboBox.SelectedItem is string value)
            {
                setVmProp(value);
            }
        };
    }

    private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)
        => Math.Abs(a - b) < tolerance;

    private static bool IsAutoFrameRateOption(FrameRateOption option)
        => option.Value <= 0 || option.FriendlyValue <= 0;
}
