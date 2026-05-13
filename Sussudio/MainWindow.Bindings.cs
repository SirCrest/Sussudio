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
        AttachCaptureSelectionBindings();
        VideoFormatComboBox.ItemsSource = ViewModel.AvailableVideoFormats;
        DecoderCountComboBox.Items.Clear();
        for (var i = 1; i <= 8; i++)
        {
            DecoderCountComboBox.Items.Add(i);
        }

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
            if (IsPreviewAudioFadeInActive || IsPreviewAudioFadeAnimationActive)
            {
                // User explicitly grabbed the slider during a preview volume fade.
                // Pause the volume animation so it doesn't overwrite their choice
                // (Stop() would snap properties back to base values).
                CancelPreviewAudioFadeInForUser();
            }
            ViewModel.SavePreviewVolume();
        };
        SetupMicrophoneVolumeBindings();
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
        ApplyInitialMicrophoneControlsVisibility();
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
        SetAudioMeterTargetLevel(ViewModel.AudioMeterTarget);
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
        SetupResponsiveShellLayoutBindings();
        OutputPathTextBox.SizeChanged += (s, e) => UpdateOutputPathDisplay();
        ApplyStatsVisibility(ViewModel.IsStatsVisible, immediate: true);
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
}
