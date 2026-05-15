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
        InitializeCaptureOptionCollections();

        // Set initial values
        UpdateOutputPathDisplay();
        ApplyInitialStatusStripPresentation();
        UpdateLiveSignalInfoVisibility();
        ApplyInitialAudioControlBindings();
        ShowAllCaptureOptionsToggle.IsChecked = ViewModel.ShowAllCaptureOptions;
        StatsToggle.IsChecked = ViewModel.IsStatsVisible;
        ApplyInitialCaptureOptionSelections();
        ApplyInitialAudioMeterPresentation();
        ApplyAudioClipVisibility();
        RecordButton.IsEnabled = !ViewModel.IsFfmpegMissing;
        RefreshHdrHintText();
        UpdateFpsTelemetryTooltip();
        EnsureDeviceSelection();
        EnsureAudioControlSelections();
        EnsureInitialCaptureOptionSelections();

        // Wire up selection changes with loop prevention
        DeviceComboBox.SelectionChanged += (s, e) =>
        {
            UpdateDeviceApplyButtonState();
        };

        AttachAudioSelectionBindings();
        AttachCaptureModeSelectionBindings();

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
