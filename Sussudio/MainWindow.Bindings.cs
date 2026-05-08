using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using WinRT.Interop;
using Sussudio.Services.Audio;
using Sussudio.Services.Automation;
using Sussudio.Services.Capture;
using Sussudio.Services.Configuration;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio;

// Manual binding layer for WinUI controls. The app deliberately avoids x:Bind,
// so this partial maps view-model property changes to concrete UI updates.
public sealed partial class MainWindow
{
    private void EnsureDeviceSelection()
    {
        if (ViewModel.Devices.Count == 0)
        {
            DeviceComboBox.SelectedItem = null;
            return;
        }

        var matchingDevice = ViewModel.SelectedDevice != null
            ? ViewModel.Devices.FirstOrDefault(device =>
                string.Equals(device.Id, ViewModel.SelectedDevice.Id, StringComparison.OrdinalIgnoreCase))
            : null;
        matchingDevice ??= ViewModel.Devices.FirstOrDefault();
        if (matchingDevice == null)
        {
            return;
        }

        if (!ReferenceEquals(ViewModel.SelectedDevice, matchingDevice))
        {
            ViewModel.SelectedDevice = matchingDevice;
        }

        if (!ReferenceEquals(DeviceComboBox.SelectedItem, matchingDevice))
        {
            DeviceComboBox.SelectedItem = matchingDevice;
        }

        UpdateDeviceApplyButtonState();
    }
    private void EnsureAudioInputSelection()
    {
        if (ViewModel.AudioInputDevices.Count == 0)
        {
            AudioInputComboBox.SelectedItem = null;
            return;
        }

        var matchingDevice = ViewModel.SelectedAudioInputDevice != null
            ? ViewModel.AudioInputDevices.FirstOrDefault(device =>
                string.Equals(device.Id, ViewModel.SelectedAudioInputDevice.Id, StringComparison.OrdinalIgnoreCase))
            : null;
        matchingDevice ??= ViewModel.AudioInputDevices.FirstOrDefault();
        if (matchingDevice == null)
        {
            return;
        }

        if (!ReferenceEquals(ViewModel.SelectedAudioInputDevice, matchingDevice))
        {
            ViewModel.SelectedAudioInputDevice = matchingDevice;
        }

        if (!ReferenceEquals(AudioInputComboBox.SelectedItem, matchingDevice))
        {
            AudioInputComboBox.SelectedItem = matchingDevice;
        }
    }
    private void EnsureMicrophoneSelection()
    {
        if (ViewModel.MicrophoneDevices.Count == 0)
        {
            MicrophoneComboBox.SelectedItem = null;
            return;
        }

        var matchingDevice = ViewModel.SelectedMicrophoneDevice != null
            ? ViewModel.MicrophoneDevices.FirstOrDefault(device =>
                string.Equals(device.Id, ViewModel.SelectedMicrophoneDevice.Id, StringComparison.OrdinalIgnoreCase))
            : null;
        matchingDevice ??= ViewModel.MicrophoneDevices.FirstOrDefault();
        if (matchingDevice == null)
        {
            return;
        }

        if (!ReferenceEquals(ViewModel.SelectedMicrophoneDevice, matchingDevice))
        {
            ViewModel.SelectedMicrophoneDevice = matchingDevice;
        }

        if (!ReferenceEquals(MicrophoneComboBox.SelectedItem, matchingDevice))
        {
            MicrophoneComboBox.SelectedItem = matchingDevice;
        }
    }
    private void EnsureDeviceAudioModeSelection()
    {
        if (ViewModel.AvailableDeviceAudioModes.Count == 0)
        {
            return;
        }

        var selectedMode = ViewModel.SelectedDeviceAudioMode;
        var matchingMode = ViewModel.AvailableDeviceAudioModes.FirstOrDefault(mode =>
            string.Equals(mode, selectedMode, StringComparison.OrdinalIgnoreCase))
            ?? ViewModel.AvailableDeviceAudioModes.FirstOrDefault();
        if (matchingMode == null)
        {
            return;
        }

        if (!string.Equals(ViewModel.SelectedDeviceAudioMode, matchingMode, StringComparison.OrdinalIgnoreCase))
        {
            ViewModel.SelectedDeviceAudioMode = matchingMode;
        }

        var shouldBeOn = string.Equals(matchingMode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase);
        if (DeviceAudioModeToggle.IsOn != shouldBeOn)
        {
            DeviceAudioModeToggle.IsOn = shouldBeOn;
        }
    }
    private void ApplyDeviceAudioControlState()
    {
        DeviceAudioControlPanel.Visibility = ViewModel.IsDeviceAudioControlSupported ? Visibility.Visible : Visibility.Collapsed;
        EnsureDeviceAudioModeSelection();

        var analogGain = Math.Clamp(ViewModel.AnalogAudioGainPercent, 0.0, 100.0);
        if (Math.Abs(AnalogAudioGainSlider.Value - analogGain) > 0.1)
        {
            AnalogAudioGainSlider.Value = analogGain;
        }

        AnalogAudioGainValueTextBlock.Text = $"{(int)Math.Round(analogGain)}%";
        var analogModeActive = string.Equals(ViewModel.SelectedDeviceAudioMode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase);
        AnalogAudioGainPanel.Visibility = ViewModel.IsDeviceAudioControlSupported && analogModeActive ? Visibility.Visible : Visibility.Collapsed;
        AnalogAudioGainSlider.IsEnabled = ViewModel.IsDeviceAudioControlSupported && analogModeActive && !ViewModel.IsRecording;
    }
    private void EnsureResolutionSelection()
    {
        if (ViewModel.AvailableResolutions.Count == 0)
        {
            if (ViewModel.SelectedDevice == null || !ViewModel.IsPreviewing)
            {
                ResolutionComboBox.SelectedItem = null;
            }

            return;
        }

        var matchingResolution = ViewModel.AvailableResolutions.FirstOrDefault(option =>
            string.Equals(option.Value, ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase))
            ?? ViewModel.AvailableResolutions.FirstOrDefault(option => option.IsEnabled)
            ?? ViewModel.AvailableResolutions.FirstOrDefault();
        if (matchingResolution == null)
        {
            return;
        }

        if (!string.Equals(matchingResolution.Value, ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase))
        {
            ViewModel.SelectedResolution = matchingResolution.Value;
        }

        if (ResolutionComboBox.SelectedItem is not ResolutionOption selectedResolutionOption ||
            !string.Equals(selectedResolutionOption.Value, matchingResolution.Value, StringComparison.OrdinalIgnoreCase))
        {
            ResolutionComboBox.SelectedItem = matchingResolution;
        }
    }
    private void EnsureFrameRateSelection()
    {
        if (ViewModel.AvailableFrameRates.Count == 0)
        {
            if (ViewModel.SelectedDevice == null || !ViewModel.IsPreviewing)
            {
                FrameRateComboBox.SelectedItem = null;
            }

            return;
        }

        if (ViewModel.IsAutoFrameRateSelected)
        {
            var autoOption = ViewModel.AvailableFrameRates
                .FirstOrDefault(IsAutoFrameRateOption);
            if (autoOption != null)
            {
                if (!ReferenceEquals(FrameRateComboBox.SelectedItem, autoOption))
                {
                    FrameRateComboBox.SelectedItem = autoOption;
                }

                return;
            }
        }

        var matchingRate = ViewModel.AvailableFrameRates
            .FirstOrDefault(option => IsFrameRateMatch(option.Value, ViewModel.SelectedFrameRate))
            ?? ViewModel.AvailableFrameRates.FirstOrDefault(option => option.IsEnabled)
            ?? ViewModel.AvailableFrameRates.FirstOrDefault();
        if (matchingRate == null)
        {
            return;
        }

        if (!IsFrameRateMatch(matchingRate.Value, ViewModel.SelectedFrameRate))
        {
            ViewModel.SelectedFrameRate = matchingRate.Value;
        }

        if (FrameRateComboBox.SelectedItem is not FrameRateOption currentFps ||
            !IsFrameRateMatch(currentFps.Value, matchingRate.Value))
        {
            FrameRateComboBox.SelectedItem = matchingRate;
        }
    }
    private static void EnsureStringComboBoxSelection(
        ComboBox comboBox,
        System.Collections.ObjectModel.ObservableCollection<string> items,
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
    private void EnsureFormatSelection()
    {
        if (ViewModel.AvailableRecordingFormats.Count == 0)
        {
            if (ViewModel.SelectedDevice == null || !ViewModel.IsPreviewing)
            {
                FormatComboBox.SelectedItem = null;
            }

            return;
        }

        EnsureStringComboBoxSelection(FormatComboBox, ViewModel.AvailableRecordingFormats,
            () => ViewModel.SelectedRecordingFormat, v => ViewModel.SelectedRecordingFormat = v);
    }
    private void EnsureQualitySelection() =>
        EnsureStringComboBoxSelection(QualityComboBox, ViewModel.AvailableQualities,
            () => ViewModel.SelectedQuality, v => ViewModel.SelectedQuality = v);
    private void EnsurePresetSelection() =>
        EnsureStringComboBoxSelection(PresetComboBox, ViewModel.AvailablePresets,
            () => ViewModel.SelectedPreset, v => ViewModel.SelectedPreset = v);
    private void EnsureSplitEncodeModeSelection() =>
        EnsureStringComboBoxSelection(SplitEncodeComboBox, ViewModel.AvailableSplitEncodeModes,
            () => ViewModel.SelectedSplitEncodeMode, v => ViewModel.SelectedSplitEncodeMode = v);
    private static void AttachCollectionSync(
        System.Collections.Specialized.INotifyCollectionChanged collection,
        Action queueSync)
    {
        collection.CollectionChanged += (s, e) =>
        {
            if (e.Action is System.Collections.Specialized.NotifyCollectionChangedAction.Add
                or System.Collections.Specialized.NotifyCollectionChangedAction.Reset
                or System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
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

        _dispatcherQueue.TryEnqueue(() =>
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
    private void SetupBindings()
    {
        InitializeAudioMeterBrushes();
        ViewModel.AudioMeterActivated += EnsureAudioMeterTimerRunning;
        ViewModel.MicrophoneMeterActivated += EnsureAudioMeterTimerRunning;

        // Flashback defaults (set in code-behind to avoid XAML parse issues with Toggled handler)
        FlashbackEnabledToggle.IsOn = ViewModel.IsFlashbackEnabled;
        FlashbackGpuDecodeToggle.IsOn = ViewModel.FlashbackGpuDecode;
        ApplyFlashbackTimelineLockout();
        // Sync buffer duration combo to saved setting
        foreach (ComboBoxItem item in FlashbackBufferDurationCombo.Items)
        {
            if (item.Tag is string tag && tag == ViewModel.FlashbackBufferMinutes.ToString())
            {
                FlashbackBufferDurationCombo.SelectedItem = item;
                break;
            }
        }

        // Bind all collections to ComboBoxes
        DeviceComboBox.ItemsSource = ViewModel.Devices;
        AudioInputComboBox.ItemsSource = ViewModel.AudioInputDevices;
        MicrophoneComboBox.ItemsSource = ViewModel.MicrophoneDevices;
        ResolutionComboBox.ItemsSource = ViewModel.AvailableResolutions;
        FrameRateComboBox.ItemsSource = ViewModel.AvailableFrameRates;
        FormatComboBox.ItemsSource = ViewModel.AvailableRecordingFormats;
        QualityComboBox.ItemsSource = ViewModel.AvailableQualities;
        PresetComboBox.ItemsSource = ViewModel.AvailablePresets;
        SplitEncodeComboBox.ItemsSource = ViewModel.AvailableSplitEncodeModes;
        VideoFormatComboBox.ItemsSource = ViewModel.AvailableVideoFormats;
        DecoderCountComboBox.Items.Clear();
        for (var i = 1; i <= 8; i++)
        {
            DecoderCountComboBox.Items.Add(i);
        }

        AttachCollectionSync(ViewModel.Devices, QueueDeviceSelectionSync);
        AttachCollectionSync(ViewModel.AudioInputDevices, QueueAudioSelectionSync);
        AttachCollectionSync(ViewModel.MicrophoneDevices, QueueMicrophoneSelectionSync);
        AttachCollectionSync(ViewModel.AvailableResolutions, QueueResolutionSelectionSync);
        AttachCollectionSync(ViewModel.AvailableFrameRates, QueueFrameRateSelectionSync);
        AttachCollectionSync(ViewModel.AvailableRecordingFormats, QueueFormatSelectionSync);
        AttachCollectionSync(ViewModel.AvailableQualities, QueueQualitySelectionSync);
        AttachCollectionSync(ViewModel.AvailablePresets, QueuePresetSelectionSync);
        AttachCollectionSync(ViewModel.AvailableSplitEncodeModes, QueueSplitEncodeModeSelectionSync);

        // Set initial values
        UpdateOutputPathDisplay();
        DiskSpaceTextBlock.Text = ViewModel.DiskSpaceInfo;
        RecordingSizeTextBlock.Text = ViewModel.RecordingSizeInfo;
        RecordingBitrateTextBlock.Text = ViewModel.RecordingBitrateInfo;
        LiveResolutionTextBlock.Text = ViewModel.LiveResolution;
        LiveFrameRateTextBlock.Text = ViewModel.LiveFrameRate;
        LivePixelFormatTextBlock.Text = ViewModel.LivePixelFormat;
        AudioRecordToggle.IsChecked = ViewModel.IsAudioEnabled;
        AudioPreviewToggle.IsChecked = ViewModel.IsAudioPreviewEnabled;
        AudioPreviewToggle.IsEnabled = ViewModel.IsAudioEnabled;
        SetAudioMeterMonitoringState(ViewModel.IsAudioPreviewActive);
        // Save the user's preferred volume, start at 0 for hidden audio priming.
        PrimePreviewAudioFadeIn();
        PreviewVolumeSlider.ValueChanged += (s, e) =>
        {
            ViewModel.PreviewVolume = e.NewValue / 100.0;
            PreviewVolumeLabel.Text = $"{(int)e.NewValue}%";
        };
        PreviewVolumeSlider.PointerCaptureLost += (s, e) =>
        {
            if (_isVolumeFadingIn || _previewVolumeFadeStoryboard != null)
            {
                // User explicitly grabbed the slider during a preview volume fade.
                // Pause the volume animation so it doesn't overwrite their choice
                // (Stop() would snap properties back to base values).
                CancelPreviewAudioFadeInForUser();
            }
            ViewModel.SavePreviewVolume();
        };
        SyncMicrophoneVolumeControls(ViewModel.MicrophoneVolume);
        MicVolumeSlider.ValueChanged += (s, e) =>
        {
            if (_syncingMicrophoneVolumeControls)
            {
                return;
            }

            _syncingMicrophoneVolumeControls = true;
            try
            {
                if (Math.Abs(ViewModel.MicrophoneVolume - e.NewValue) > 0.01)
                {
                    ViewModel.MicrophoneVolume = e.NewValue;
                }

                SyncMicrophoneVolumeControls(e.NewValue);
            }
            finally
            {
                _syncingMicrophoneVolumeControls = false;
            }
        };
        MicVolumeSlider.PointerCaptureLost += (s, e) => ViewModel.SaveMicrophoneVolume();
        MicVolumeShelfSlider.ValueChanged += (s, e) =>
        {
            if (_syncingMicrophoneVolumeControls)
            {
                return;
            }

            _syncingMicrophoneVolumeControls = true;
            try
            {
                if (Math.Abs(ViewModel.MicrophoneVolume - e.NewValue) > 0.01)
                {
                    ViewModel.MicrophoneVolume = e.NewValue;
                }

                SyncMicrophoneVolumeControls(e.NewValue);
            }
            finally
            {
                _syncingMicrophoneVolumeControls = false;
            }
        };
        MicVolumeShelfSlider.PointerCaptureLost += (s, e) => ViewModel.SaveMicrophoneVolume();
        CustomAudioToggle.IsChecked = ViewModel.IsCustomAudioInputEnabled;
        CustomAudioToggle.IsEnabled = !ViewModel.IsRecording;
        MicrophoneToggle.IsChecked = ViewModel.IsMicrophoneEnabled;
        MicrophoneToggle.IsEnabled = !ViewModel.IsRecording;
        ShowAllCaptureOptionsToggle.IsChecked = ViewModel.ShowAllCaptureOptions;
        StatsToggle.IsChecked = ViewModel.IsStatsVisible;
        AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled && !ViewModel.IsRecording;
        AudioInputComboBox.SelectedItem = ViewModel.SelectedAudioInputDevice;
        MicrophoneComboBox.IsEnabled = ViewModel.IsMicrophoneEnabled && !ViewModel.IsRecording;
        MicrophoneComboBox.SelectedItem = ViewModel.SelectedMicrophoneDevice;
        MicVolumeShelfSlider.IsEnabled = ViewModel.IsMicrophoneEnabled;
        if (ViewModel.IsMicrophoneEnabled)
        {
            DeviceAudioRowTranslate.Y = 0;
            MicMeterRowTranslate.Y = 0;
            MicMeterRow.Opacity = 1;
        }
        else
        {
            DeviceAudioRowTranslate.Y = MicMeterRowHeight / 2;
            HideMicMeterRow(immediate: true);
        }
        ApplyDeviceAudioControlState();
        FormatComboBox.SelectedItem = ViewModel.SelectedRecordingFormat;
        QualityComboBox.SelectedItem = ViewModel.SelectedQuality;
        PresetComboBox.SelectedItem = ViewModel.SelectedPreset;
        SplitEncodeComboBox.SelectedItem = ViewModel.SelectedSplitEncodeMode;
        VideoFormatComboBox.SelectedItem = ViewModel.SelectedVideoFormat;
        _selectedDecoderCount = Math.Clamp(ViewModel.MjpegDecoderCount, 1, 8);
        DecoderCountComboBox.SelectedItem = _selectedDecoderCount;
        CustomBitrateNumberBox.Value = ViewModel.CustomBitrateMbps;
        ApplyBitrateVisibility();
        HdrToggle.IsChecked = ViewModel.IsHdrEnabled;
        TrueHdrPreviewToggle.IsChecked = ViewModel.IsTrueHdrPreviewEnabled;
        ApplyHdrToggleEnabledState();
        ResetAudioMeterVisuals();
        _audioMeterTargetLevel = Math.Clamp(ViewModel.AudioMeterTarget, 0.0, 1.0);
        ApplyAudioClipVisibility();
        RecordButton.IsEnabled = !ViewModel.IsFfmpegMissing;
        RefreshHdrHintText();
        UpdateFpsTelemetryTooltip();
        EnsureDeviceSelection();
        EnsureAudioInputSelection();
        EnsureMicrophoneSelection();
        EnsureDeviceAudioModeSelection();
        EnsureResolutionSelection();
        EnsureFrameRateSelection();
        EnsureFormatSelection();
        EnsureQualitySelection();
        EnsurePresetSelection();
        EnsureSplitEncodeModeSelection();
        UpdateDecoderCountVisibility();

        // Wire up selection changes with loop prevention
        DeviceComboBox.SelectionChanged += (s, e) =>
        {
            UpdateDeviceApplyButtonState();
        };

        AudioInputComboBox.SelectionChanged += (s, e) =>
        {
            if (AudioInputComboBox.SelectedItem is Sussudio.Models.AudioInputDevice device &&
                device != ViewModel.SelectedAudioInputDevice)
            {
                ViewModel.SelectedAudioInputDevice = device;
            }
        };

        MicrophoneComboBox.SelectionChanged += (s, e) =>
        {
            if (MicrophoneComboBox.SelectedItem is Sussudio.Models.AudioInputDevice device &&
                device != ViewModel.SelectedMicrophoneDevice)
            {
                ViewModel.SelectedMicrophoneDevice = device;
            }
        };

        DeviceAudioModeToggle.Toggled += (s, e) =>
        {
            var mode = DeviceAudioModeToggle.IsOn ? DeviceAudioMode.Analog : DeviceAudioMode.Hdmi;
            if (!string.Equals(mode, ViewModel.SelectedDeviceAudioMode, StringComparison.OrdinalIgnoreCase))
            {
                ViewModel.SelectedDeviceAudioMode = mode;
            }
        };

        ResolutionComboBox.SelectionChanged += (s, e) =>
        {
            if (ResolutionComboBox.SelectedItem is ResolutionOption resolution &&
                resolution.IsEnabled &&
                !string.Equals(resolution.Value, ViewModel.SelectedResolution, StringComparison.OrdinalIgnoreCase))
            {
                ViewModel.SelectedResolution = resolution.Value;
            }
        };

        FrameRateComboBox.SelectionChanged += (s, e) =>
        {
            if (FrameRateComboBox.SelectedItem is FrameRateOption frameRate &&
                frameRate.IsEnabled)
            {
                if (IsAutoFrameRateOption(frameRate))
                {
                    if (!ViewModel.IsAutoFrameRateSelected)
                    {
                        ViewModel.SelectedFrameRate = frameRate.Value;
                    }
                }
                else if (!IsFrameRateMatch(frameRate.Value, ViewModel.SelectedFrameRate))
                {
                    ViewModel.SelectedFrameRate = frameRate.Value;
                }
            }

            UpdateDecoderCountVisibility();
        };

        FormatComboBox.SelectionChanged += (s, e) =>
        {
            if (FormatComboBox.SelectedItem is string format)
            {
                ViewModel.SelectedRecordingFormat = format;
            }
        };

        QualityComboBox.SelectionChanged += (s, e) =>
        {
            if (QualityComboBox.SelectedItem is string quality)
            {
                ViewModel.SelectedQuality = quality;
            }
        };

        PresetComboBox.SelectionChanged += (s, e) =>
        {
            if (PresetComboBox.SelectedItem is string preset)
            {
                ViewModel.SelectedPreset = preset;
            }
        };

        SplitEncodeComboBox.SelectionChanged += (s, e) =>
        {
            if (SplitEncodeComboBox.SelectedItem is string splitMode)
            {
                ViewModel.SelectedSplitEncodeMode = splitMode;
            }
        };

        VideoFormatComboBox.SelectionChanged += (s, e) =>
        {
            if (VideoFormatComboBox.SelectedItem is string videoFormat)
            {
                ViewModel.SelectedVideoFormat = videoFormat;
            }

            UpdateDecoderCountVisibility();
        };

        CustomBitrateNumberBox.ValueChanged += (s, e) =>
        {
            if (!double.IsNaN(CustomBitrateNumberBox.Value))
            {
                ViewModel.CustomBitrateMbps = CustomBitrateNumberBox.Value;
            }
        };
        HdrToggle.Click += (s, e) => ViewModel.IsHdrEnabled = HdrToggle.IsChecked == true;
        TrueHdrPreviewToggle.Click += (s, e) =>
            ViewModel.IsTrueHdrPreviewEnabled = TrueHdrPreviewToggle.IsChecked == true;
        AudioRecordToggle.Checked += (s, e) => ViewModel.IsAudioEnabled = true;
        AudioRecordToggle.Unchecked += (s, e) => ViewModel.IsAudioEnabled = false;
        AudioPreviewToggle.Checked += (s, e) => ViewModel.IsAudioPreviewEnabled = true;
        AudioPreviewToggle.Unchecked += (s, e) => ViewModel.IsAudioPreviewEnabled = false;
        StatsToggle.Checked += StatsToggle_Checked;
        StatsToggle.Unchecked += StatsToggle_Unchecked;
        FrameTimeOverlayToggle.Checked += FrameTimeOverlayToggle_Checked;
        FrameTimeOverlayToggle.Unchecked += FrameTimeOverlayToggle_Unchecked;
        CustomAudioToggle.Click += (s, e) => ViewModel.IsCustomAudioInputEnabled = CustomAudioToggle.IsChecked == true;
        MicrophoneToggle.Click += (s, e) => ViewModel.IsMicrophoneEnabled = MicrophoneToggle.IsChecked == true;
        ShowAllCaptureOptionsToggle.Click += (s, e) => ViewModel.ShowAllCaptureOptions = ShowAllCaptureOptionsToggle.IsChecked == true;
        FlashbackGpuDecodeToggle.Toggled += (s, e) => ViewModel.FlashbackGpuDecode = FlashbackGpuDecodeToggle.IsOn;
        AnalogAudioGainSlider.ValueChanged += (s, e) =>
        {
            ViewModel.AnalogAudioGainPercent = e.NewValue;
            AnalogAudioGainValueTextBlock.Text = $"{(int)Math.Round(e.NewValue)}%";
        };
        AudioMeterTrack.SizeChanged += (s, e) => AnimateAudioMeterTick();
        MicMeterTrack.SizeChanged += (s, e) => AnimateAudioMeterTick();
        ControlBarBorder.SizeChanged += (s, e) => UpdateToggleLabelVisibility(e.NewSize.Width);
        CaptureSettingsGrid.SizeChanged += CaptureSettingsGrid_SizeChanged;
        OutputPathTextBox.SizeChanged += (s, e) => UpdateOutputPathDisplay();
        ApplyStatsVisibility(ViewModel.IsStatsVisible, immediate: true);
    }
    private void SyncMicrophoneVolumeControls(double volumePercent)
    {
        var clampedVolume = Math.Clamp(volumePercent, 0.0, 100.0);
        if (Math.Abs(MicVolumeSlider.Value - clampedVolume) > 0.5)
        {
            MicVolumeSlider.Value = clampedVolume;
        }

        if (Math.Abs(MicVolumeShelfSlider.Value - clampedVolume) > 0.5)
        {
            MicVolumeShelfSlider.Value = clampedVolume;
        }

        MicVolumeLabel.Text = $"{(int)Math.Round(clampedVolume)}%";
    }
    private void UpdateMicrophoneControlsVisibility()
    {
        MicVolumeShelfSlider.IsEnabled = ViewModel.IsMicrophoneEnabled;
        if (ViewModel.IsMicrophoneEnabled)
        {
            ShowMicMeterRow();
        }
        else
        {
            HideMicMeterRow(immediate: false);
        }
    }
    private void ShowMicMeterRow()
    {
        EnsureMicMeterRowAnimations();
        StopMicMeterRowAnimation();
        DeviceAudioRowTranslate.Y = MicMeterRowHeight / 2;
        MicMeterRowTranslate.Y = MicMeterRowHeight;
        MicMeterRow.Opacity = 0;
        _micMeterRowStoryboard = _showMicMeterRowStoryboard;
        _showMicMeterRowStoryboard?.Begin();
    }
    private void HideMicMeterRow(bool immediate)
    {
        EnsureMicMeterRowAnimations();
        StopMicMeterRowAnimation();
        if (immediate || MicMeterRow.Opacity == 0)
        {
            DeviceAudioRowTranslate.Y = MicMeterRowHeight / 2;
            MicMeterRowTranslate.Y = MicMeterRowHeight;
            MicMeterRow.Opacity = 0;
            _micMeterDisplayLevel = 0;
            _micMeterTargetLevel = 0;
            MicMeterClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
            return;
        }

        _micMeterRowStoryboard = _hideMicMeterRowStoryboard;
        _hideMicMeterRowStoryboard?.Begin();
    }
    private void StopMicMeterRowAnimation()
    {
        _micMeterRowStoryboard?.Stop();
        _micMeterRowStoryboard = null;
    }
    private void EnsureMicMeterRowAnimations()
    {
        _showMicMeterRowStoryboard ??= CreateMicMeterRowStoryboard(showing: true);
        _hideMicMeterRowStoryboard ??= CreateMicMeterRowStoryboard(showing: false);
    }
    private Storyboard CreateMicMeterRowStoryboard(bool showing)
    {
        var durationMs = showing ? 350 : 250;
        var easing = new CubicEase { EasingMode = showing ? EasingMode.EaseOut : EasingMode.EaseIn };
        var duration = TimeSpan.FromMilliseconds(durationMs);

        var storyboard = new Storyboard();

        // Device audio row: TranslateY 7→0 (show) or 0→7 (hide)
        var deviceSlide = new DoubleAnimation
        {
            To = showing ? 0 : MicMeterRowHeight / 2,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(deviceSlide, DeviceAudioRowTranslate);
        Storyboard.SetTargetProperty(deviceSlide, "Y");

        // Mic meter: TranslateY +14→0 (slides up into view) or 0→+14 (slides down out)
        var slideAnim = new DoubleAnimation
        {
            To = showing ? 0 : MicMeterRowHeight,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(slideAnim, MicMeterRowTranslate);
        Storyboard.SetTargetProperty(slideAnim, "Y");

        var fade = new DoubleAnimation
        {
            To = showing ? 1 : 0,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(fade, MicMeterRow);
        Storyboard.SetTargetProperty(fade, "Opacity");

        storyboard.Children.Add(deviceSlide);
        storyboard.Children.Add(slideAnim);
        storyboard.Children.Add(fade);
        storyboard.Completed += (_, _) =>
        {
            if (!ReferenceEquals(_micMeterRowStoryboard, storyboard))
            {
                return;
            }

            _micMeterRowStoryboard = null;
            if (showing)
            {
                DeviceAudioRowTranslate.Y = 0;
                MicMeterRowTranslate.Y = 0;
                MicMeterRow.Opacity = 1;
                return;
            }

            DeviceAudioRowTranslate.Y = MicMeterRowHeight / 2;
            MicMeterRowTranslate.Y = MicMeterRowHeight;
            MicMeterRow.Opacity = 0;
            _micMeterDisplayLevel = 0;
            _micMeterTargetLevel = 0;
            MicMeterClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
        };

        return storyboard;
    }
    private void UpdateToggleLabelVisibility(double controlBarWidth)
    {
        var showLabels = controlBarWidth >= ControlBarLabelThreshold;
        if (showLabels == _toggleLabelsVisible) return;
        _toggleLabelsVisible = showLabels;

        var vis = showLabels ? Visibility.Visible : Visibility.Collapsed;
        HdrToggleLabel.Visibility = vis;
        AudioRecordToggleLabel.Visibility = vis;
        PreviewButtonLabel.Visibility = vis;
        HdrPreviewToggleLabel.Visibility = vis;
        AudioPreviewToggleLabel.Visibility = vis;
        StatsToggleLabel.Visibility = vis;
        FrameTimeOverlayToggleLabel.Visibility = vis;
        // Record button is always a circle when idle — no label mode
    }
    private void CaptureSettingsGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var narrow = e.NewSize.Width < 700;
        if (narrow == _captureSettingsNarrow)
        {
            return;
        }

        _captureSettingsNarrow = narrow;
        if (narrow)
        {
            VideoFormatColumn.Width = new GridLength(0);
            PresetColumn.Width = new GridLength(0);
            SplitColumn.Width = new GridLength(0);
            Grid.SetRow(VideoFormatPanel, 1);
            Grid.SetColumn(VideoFormatPanel, 1);
            Grid.SetRow(PresetPanel, 1);
            Grid.SetColumn(PresetPanel, 2);
            Grid.SetRow(SplitPanel, 1);
            Grid.SetColumn(SplitPanel, 3);
            Grid.SetRow(CustomBitratePanel, 1);
            Grid.SetColumn(CustomBitratePanel, 2);
        }
        else
        {
            VideoFormatColumn.Width = new GridLength(1, GridUnitType.Star);
            PresetColumn.Width = new GridLength(1, GridUnitType.Star);
            SplitColumn.Width = new GridLength(1, GridUnitType.Star);
            Grid.SetRow(VideoFormatPanel, 0);
            Grid.SetColumn(VideoFormatPanel, 0);
            Grid.SetRow(PresetPanel, 0);
            Grid.SetColumn(PresetPanel, 5);
            Grid.SetRow(SplitPanel, 0);
            Grid.SetColumn(SplitPanel, 6);
            Grid.SetRow(CustomBitratePanel, 0);
            Grid.SetColumn(CustomBitratePanel, 5);
        }
    }
    private void UpdateDecoderCountVisibility()
    {
        var selectedFormat = VideoFormatComboBox.SelectedItem as string ?? ViewModel.SelectedVideoFormat;
        var selectedFrameRate = GetSelectedFriendlyFrameRate();

        // Show decoder count when MJPG is explicitly selected, OR when auto
        // resolves to a format that would use the parallel MJPEG pipeline
        // (i.e. the device's native format is MJPG at high frame rates).
        var isExplicitMjpg = string.Equals(selectedFormat, "MJPG", StringComparison.OrdinalIgnoreCase);
        var isAutoWithMjpgDevice = string.Equals(selectedFormat, "Auto", StringComparison.OrdinalIgnoreCase)
            && string.Equals(ViewModel.SelectedFormat?.PixelFormat, "MJPG", StringComparison.OrdinalIgnoreCase);

        DecoderCountPanel.Visibility =
            (isExplicitMjpg || isAutoWithMjpgDevice) && selectedFrameRate >= 90
                ? Visibility.Visible
                : Visibility.Collapsed;
    }
    private void UpdateOutputPathDisplay()
    {
        var path = ViewModel.OutputPath;
        if (string.IsNullOrEmpty(path))
        {
            OutputPathTextBox.Text = string.Empty;
            return;
        }

        ToolTipService.SetToolTip(OutputPathTextBox, path);

        var availableWidth = OutputPathTextBox.ActualWidth;
        if (availableWidth <= 0)
        {
            OutputPathTextBox.Text = path;
            return;
        }

        // FontSize 12 ≈ 7px per char, minus internal padding
        var maxChars = (int)((availableWidth - 20) / 7);
        if (path.Length <= maxChars)
        {
            OutputPathTextBox.Text = path;
            return;
        }

        var parts = path.Split('\\', '/');
        if (parts.Length <= 2)
        {
            OutputPathTextBox.Text = path;
            return;
        }

        // Progressively truncate: keep root, show as many trailing segments as fit
        var root = parts[0];
        for (int tailCount = parts.Length - 1; tailCount >= 1; tailCount--)
        {
            var tail = string.Join("\\", parts[^tailCount..]);
            var candidate = $"{root}\\...\\{tail}";
            if (candidate.Length <= maxChars)
            {
                OutputPathTextBox.Text = candidate;
                return;
            }
        }

        OutputPathTextBox.Text = $"{root}\\...\\{parts[^1]}";
    }
    private double GetSelectedFriendlyFrameRate()
    {
        if (FrameRateComboBox.SelectedItem is FrameRateOption option)
        {
            if (option.FriendlyValue > 0)
            {
                return option.FriendlyValue;
            }

            if (option.Value > 0)
            {
                return option.Value;
            }
        }

        return ViewModel.SelectedFrameRate;
    }

    private bool HasPendingDeviceSelection()
    {
        if (DeviceComboBox.SelectedItem is not CaptureDevice selectedDevice)
        {
            return false;
        }

        return !string.Equals(selectedDevice.Id, ViewModel.SelectedDevice?.Id, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateDeviceApplyButtonState()
    {
        if (ApplyDeviceButton == null)
        {
            return;
        }

        ApplyDeviceButton.IsEnabled =
            HasPendingDeviceSelection() &&
            !ViewModel.IsRecording &&
            !ViewModel.IsPreviewReinitializing;
    }

    private void DecoderCountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DecoderCountComboBox.SelectedItem is int count)
        {
            _selectedDecoderCount = count;
            if (ViewModel.MjpegDecoderCount != count)
            {
                ViewModel.MjpegDecoderCount = count;
            }
        }
    }
    private void RefreshHdrHintText()
    {
        var resolutionHint = ViewModel.HdrResolutionSupportHint?.Trim();
        var readinessHint = ViewModel.HdrReadinessReason?.Trim();
        var combinedHint = string.IsNullOrWhiteSpace(readinessHint)
            ? resolutionHint
            : string.IsNullOrWhiteSpace(resolutionHint)
                ? readinessHint
                : $"{readinessHint}{Environment.NewLine}{resolutionHint}";
        if (ViewModel.IsRecording)
        {
            combinedHint = string.IsNullOrWhiteSpace(combinedHint)
                ? "Stop recording before switching between HDR and SDR pipelines."
                : $"{combinedHint}{Environment.NewLine}Stop recording before switching between HDR and SDR pipelines.";
        }
        ToolTipService.SetToolTip(HdrToggle,
            string.IsNullOrWhiteSpace(combinedHint) ? null : combinedHint);
    }
    private void UpdateFpsTelemetryTooltip()
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(ViewModel.SourceTelemetrySummaryText))
            parts.Add(ViewModel.SourceTelemetrySummaryText);
        if (!string.IsNullOrWhiteSpace(ViewModel.SourceTargetSummaryText))
            parts.Add(ViewModel.SourceTargetSummaryText);
        ToolTipService.SetToolTip(FrameRateComboBox,
            parts.Count > 0 ? string.Join(Environment.NewLine, parts) : null);
    }

    private void ApplyHdrToggleEnabledState()
    {
        HdrToggle.IsEnabled = ViewModel.IsHdrAvailable &&
                              !ViewModel.IsRecording &&
                              ViewModel.SourceIsHdr != false;
        TrueHdrPreviewToggle.IsEnabled = ViewModel.IsHdrEnabled && !ViewModel.IsRecording;
    }

    private void ApplyBitrateVisibility()
    {
        CustomBitratePanel.Visibility = ViewModel.IsCustomBitrateVisible ? Visibility.Visible : Visibility.Collapsed;
        PresetPanel.Visibility = ViewModel.IsCustomBitrateVisible ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ApplyAudioClipVisibility()
    {
        AudioClipText.Visibility = ViewModel.AudioClipping ? Visibility.Visible : Visibility.Collapsed;
    }
}
