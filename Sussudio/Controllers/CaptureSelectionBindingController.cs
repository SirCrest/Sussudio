using System;
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
}
