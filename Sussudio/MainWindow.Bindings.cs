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
        AttachAudioMeterActivationBindings();

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
        ApplyInitialAudioControlBindings();
        ShowAllCaptureOptionsToggle.IsChecked = ViewModel.ShowAllCaptureOptions;
        StatsToggle.IsChecked = ViewModel.IsStatsVisible;
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
        ApplyInitialAudioMeterPresentation();
        ApplyAudioClipVisibility();
        RecordButton.IsEnabled = !ViewModel.IsFfmpegMissing;
        RefreshHdrHintText();
        UpdateFpsTelemetryTooltip();
        EnsureDeviceSelection();
        EnsureAudioControlSelections();
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

        AttachAudioSelectionBindings();

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

        AttachRecordingOptionBindings();
        AttachAudioRecordPreviewToggleBindings();
        StatsToggle.Checked += StatsToggle_Checked;
        StatsToggle.Unchecked += StatsToggle_Unchecked;
        FrameTimeOverlayToggle.Checked += FrameTimeOverlayToggle_Checked;
        FrameTimeOverlayToggle.Unchecked += FrameTimeOverlayToggle_Unchecked;
        AttachAudioInputToggleBindings();
        ShowAllCaptureOptionsToggle.Click += (s, e) => ViewModel.ShowAllCaptureOptions = ShowAllCaptureOptionsToggle.IsChecked == true;
        FlashbackGpuDecodeToggle.Toggled += (s, e) => ViewModel.FlashbackGpuDecode = FlashbackGpuDecodeToggle.IsOn;
        AttachDeviceAudioGainAndMeterBindings();
        SetupResponsiveShellLayoutBindings();
        AttachOutputPathDisplay();
        ApplyStatsVisibility(ViewModel.IsStatsVisible, immediate: true);
    }
}
