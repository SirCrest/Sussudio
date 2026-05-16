using System;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed partial class CaptureSelectionBindingController
{
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

    public void EnsureAudioInputSelection()
    {
        if (_context.ViewModel.AudioInputDevices.Count == 0)
        {
            _context.AudioInputComboBox.SelectedItem = null;
            return;
        }

        var matchingDevice = CaptureComboBoxSelectionNormalizer.ResolveAudioInputDeviceSelection(
            _context.ViewModel.AudioInputDevices,
            _context.ViewModel.SelectedAudioInputDevice);
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

        var matchingDevice = CaptureComboBoxSelectionNormalizer.ResolveAudioInputDeviceSelection(
            _context.ViewModel.MicrophoneDevices,
            _context.ViewModel.SelectedMicrophoneDevice);
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

        var matchingResolution = CaptureComboBoxSelectionNormalizer.ResolveResolutionSelection(
            _context.ViewModel.AvailableResolutions,
            _context.ViewModel.SelectedResolution);
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
            var autoOption = CaptureComboBoxSelectionNormalizer.ResolveFrameRateSelection(
                _context.ViewModel.AvailableFrameRates,
                _context.ViewModel.SelectedFrameRate,
                isAutoFrameRateSelected: true);
            if (autoOption != null && CaptureComboBoxSelectionNormalizer.IsAutoFrameRateOption(autoOption))
            {
                if (!ReferenceEquals(_context.FrameRateComboBox.SelectedItem, autoOption))
                {
                    _context.FrameRateComboBox.SelectedItem = autoOption;
                }

                return;
            }
        }

        var matchingRate = CaptureComboBoxSelectionNormalizer.ResolveFrameRateSelection(
            _context.ViewModel.AvailableFrameRates,
            _context.ViewModel.SelectedFrameRate,
            isAutoFrameRateSelected: false);
        if (matchingRate == null)
        {
            return;
        }

        if (!CaptureComboBoxSelectionNormalizer.IsFrameRateMatch(matchingRate.Value, _context.ViewModel.SelectedFrameRate))
        {
            _context.ViewModel.SelectedFrameRate = matchingRate.Value;
        }

        if (_context.FrameRateComboBox.SelectedItem is not FrameRateOption currentFps ||
            !CaptureComboBoxSelectionNormalizer.IsFrameRateMatch(currentFps.Value, matchingRate.Value))
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

        ApplyStringComboBoxSelection(
            _context.FormatComboBox,
            _context.ViewModel.AvailableRecordingFormats,
            () => _context.ViewModel.SelectedRecordingFormat,
            value => _context.ViewModel.SelectedRecordingFormat = value);
    }

    public void EnsureQualitySelection() =>
        ApplyStringComboBoxSelection(
            _context.QualityComboBox,
            _context.ViewModel.AvailableQualities,
            () => _context.ViewModel.SelectedQuality,
            value => _context.ViewModel.SelectedQuality = value);

    public void EnsurePresetSelection() =>
        ApplyStringComboBoxSelection(
            _context.PresetComboBox,
            _context.ViewModel.AvailablePresets,
            () => _context.ViewModel.SelectedPreset,
            value => _context.ViewModel.SelectedPreset = value);

    public void EnsureSplitEncodeModeSelection() =>
        ApplyStringComboBoxSelection(
            _context.SplitEncodeComboBox,
            _context.ViewModel.AvailableSplitEncodeModes,
            () => _context.ViewModel.SelectedSplitEncodeMode,
            value => _context.ViewModel.SelectedSplitEncodeMode = value);

    private static void ApplyStringComboBoxSelection(
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
        var match = CaptureComboBoxSelectionNormalizer.ResolveStringSelection(items, vmValue);
        if (match == null)
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
}
