using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class CaptureSelectionBindingControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required MainViewModel ViewModel { get; init; }
    public required ComboBox DeviceComboBox { get; init; }
    public required ComboBox AudioInputComboBox { get; init; }
    public required ComboBox MicrophoneComboBox { get; init; }
    public required ComboBox ResolutionComboBox { get; init; }
    public required ComboBox FrameRateComboBox { get; init; }
    public required ComboBox FormatComboBox { get; init; }
    public required ComboBox QualityComboBox { get; init; }
    public required ComboBox PresetComboBox { get; init; }
    public required ComboBox SplitEncodeComboBox { get; init; }
    public required Button ApplyDeviceButton { get; init; }
    public required StackPanel DeviceAudioControlPanel { get; init; }
    public required ToggleSwitch DeviceAudioModeToggle { get; init; }
    public required StackPanel AnalogAudioGainPanel { get; init; }
    public required Slider AnalogAudioGainSlider { get; init; }
    public required TextBlock AnalogAudioGainValueTextBlock { get; init; }
}

internal sealed class CaptureSelectionBindingController
{
    private const int SyncDevice = 0;
    private const int SyncAudio = 1;
    private const int SyncResolution = 2;
    private const int SyncFrameRate = 3;
    private const int SyncFormat = 4;
    private const int SyncQuality = 5;
    private const int SyncPreset = 6;
    private const int SyncSplitEncode = 7;
    private const int SyncMicrophone = 8;

    private readonly CaptureSelectionBindingControllerContext _context;
    private readonly int[] _selectionSyncQueued = new int[9];

    public CaptureSelectionBindingController(CaptureSelectionBindingControllerContext context)
    {
        _context = context;
    }

    public void AttachDeviceSelectionChangedBinding()
    {
        _context.DeviceComboBox.SelectionChanged += (_, _) => UpdateDeviceApplyButtonState();
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

    public bool TryHandlePropertyChanged(string? propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.SelectedDevice):
                HandleSelectedDevicePropertyChanged();
                return true;

            case nameof(MainViewModel.SelectedResolution):
                EnsureResolutionSelection();
                return true;

            case nameof(MainViewModel.SelectedFrameRate):
            case nameof(MainViewModel.IsAutoFrameRateSelected):
                EnsureFrameRateSelection();
                return true;

            case nameof(MainViewModel.AvailableResolutions):
                HandleAvailableResolutionsPropertyChanged();
                return true;

            case nameof(MainViewModel.AvailableFrameRates):
                HandleAvailableFrameRatesPropertyChanged();
                return true;

            case nameof(MainViewModel.IsDeviceAudioControlSupported):
            case nameof(MainViewModel.SelectedDeviceAudioMode):
            case nameof(MainViewModel.AnalogAudioGainPercent):
            case nameof(MainViewModel.AvailableDeviceAudioModes):
                ApplyDeviceAudioControlState();
                return true;

            case nameof(MainViewModel.SelectedAudioInputDevice):
                EnsureAudioInputSelection();
                return true;

            case nameof(MainViewModel.SelectedMicrophoneDevice):
                EnsureMicrophoneSelection();
                return true;

            case nameof(MainViewModel.SelectedRecordingFormat):
                EnsureFormatSelection();
                return true;

            case nameof(MainViewModel.SelectedQuality):
                EnsureQualitySelection();
                return true;

            case nameof(MainViewModel.AvailablePresets):
                HandleAvailablePresetsPropertyChanged();
                return true;

            case nameof(MainViewModel.SelectedPreset):
                EnsurePresetSelection();
                return true;

            case nameof(MainViewModel.AvailableSplitEncodeModes):
                HandleAvailableSplitEncodeModesPropertyChanged();
                return true;

            case nameof(MainViewModel.SelectedSplitEncodeMode):
                EnsureSplitEncodeModeSelection();
                return true;

            default:
                return false;
        }
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

    public void EnsureDeviceAudioModeSelection()
    {
        if (_context.ViewModel.AvailableDeviceAudioModes.Count == 0)
        {
            return;
        }

        var selectedMode = _context.ViewModel.SelectedDeviceAudioMode;
        var matchingMode = _context.ViewModel.AvailableDeviceAudioModes.FirstOrDefault(mode =>
            string.Equals(mode, selectedMode, StringComparison.OrdinalIgnoreCase))
            ?? _context.ViewModel.AvailableDeviceAudioModes.FirstOrDefault();
        if (matchingMode == null)
        {
            return;
        }

        if (!string.Equals(_context.ViewModel.SelectedDeviceAudioMode, matchingMode, StringComparison.OrdinalIgnoreCase))
        {
            _context.ViewModel.SelectedDeviceAudioMode = matchingMode;
        }

        var shouldBeOn = string.Equals(matchingMode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase);
        if (_context.DeviceAudioModeToggle.IsOn != shouldBeOn)
        {
            _context.DeviceAudioModeToggle.IsOn = shouldBeOn;
        }
    }

    public void ApplyDeviceAudioControlState()
    {
        _context.DeviceAudioControlPanel.Visibility =
            _context.ViewModel.IsDeviceAudioControlSupported ? Visibility.Visible : Visibility.Collapsed;
        EnsureDeviceAudioModeSelection();

        var analogGain = Math.Clamp(_context.ViewModel.AnalogAudioGainPercent, 0.0, 100.0);
        if (Math.Abs(_context.AnalogAudioGainSlider.Value - analogGain) > 0.1)
        {
            _context.AnalogAudioGainSlider.Value = analogGain;
        }

        _context.AnalogAudioGainValueTextBlock.Text = $"{(int)Math.Round(analogGain)}%";
        var analogModeActive = string.Equals(
            _context.ViewModel.SelectedDeviceAudioMode,
            DeviceAudioMode.Analog,
            StringComparison.OrdinalIgnoreCase);
        _context.AnalogAudioGainPanel.Visibility =
            _context.ViewModel.IsDeviceAudioControlSupported && analogModeActive
                ? Visibility.Visible
                : Visibility.Collapsed;
        _context.AnalogAudioGainSlider.IsEnabled =
            _context.ViewModel.IsDeviceAudioControlSupported &&
            analogModeActive &&
            !_context.ViewModel.IsRecording;
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

    public void HandleAvailableResolutionsPropertyChanged()
    {
        _context.ResolutionComboBox.ItemsSource = _context.ViewModel.AvailableResolutions;
        EnsureResolutionSelection();
    }

    public void HandleAvailableFrameRatesPropertyChanged()
    {
        _context.FrameRateComboBox.ItemsSource = _context.ViewModel.AvailableFrameRates;
        EnsureFrameRateSelection();
    }

    public void HandleAvailablePresetsPropertyChanged()
    {
        _context.PresetComboBox.ItemsSource = _context.ViewModel.AvailablePresets;
        EnsurePresetSelection();
    }

    public void HandleAvailableSplitEncodeModesPropertyChanged()
    {
        _context.SplitEncodeComboBox.ItemsSource = _context.ViewModel.AvailableSplitEncodeModes;
        EnsureSplitEncodeModeSelection();
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

    private static void AttachCollectionSync(INotifyCollectionChanged collection, Action queueSync)
    {
        collection.CollectionChanged += (_, e) =>
        {
            if (e.Action is NotifyCollectionChangedAction.Add
                or NotifyCollectionChangedAction.Reset
                or NotifyCollectionChangedAction.Remove)
            {
                queueSync();
            }
        };
    }

    private void QueueSelectionSync(int syncIndex, Action ensureMethod)
    {
        if (Interlocked.Exchange(ref _selectionSyncQueued[syncIndex], 1) != 0)
        {
            return;
        }

        _context.DispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                ensureMethod();
            }
            finally
            {
                Interlocked.Exchange(ref _selectionSyncQueued[syncIndex], 0);
            }
        });
    }

    private void QueueDeviceSelectionSync() => QueueSelectionSync(SyncDevice, EnsureDeviceSelection);
    private void QueueAudioSelectionSync() => QueueSelectionSync(SyncAudio, EnsureAudioInputSelection);
    private void QueueMicrophoneSelectionSync() => QueueSelectionSync(SyncMicrophone, EnsureMicrophoneSelection);
    private void QueueResolutionSelectionSync() => QueueSelectionSync(SyncResolution, EnsureResolutionSelection);
    private void QueueFrameRateSelectionSync() => QueueSelectionSync(SyncFrameRate, EnsureFrameRateSelection);
    private void QueueFormatSelectionSync() => QueueSelectionSync(SyncFormat, EnsureFormatSelection);
    private void QueueQualitySelectionSync() => QueueSelectionSync(SyncQuality, EnsureQualitySelection);
    private void QueuePresetSelectionSync() => QueueSelectionSync(SyncPreset, EnsurePresetSelection);
    private void QueueSplitEncodeModeSelectionSync() => QueueSelectionSync(SyncSplitEncode, EnsureSplitEncodeModeSelection);

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
