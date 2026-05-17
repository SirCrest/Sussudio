using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class AudioControlBindingControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required ToggleButton AudioRecordToggle { get; init; }
    public required ToggleButton AudioPreviewToggle { get; init; }
    public required Slider PreviewVolumeSlider { get; init; }
    public required TextBlock PreviewVolumeLabel { get; init; }
    public required CheckBox CustomAudioToggle { get; init; }
    public required CheckBox MicrophoneToggle { get; init; }
    public required ComboBox AudioInputComboBox { get; init; }
    public required ComboBox MicrophoneComboBox { get; init; }
    public required ToggleSwitch DeviceAudioModeToggle { get; init; }
    public required Slider AnalogAudioGainSlider { get; init; }
    public required TextBlock AnalogAudioGainValueTextBlock { get; init; }
    public required FrameworkElement AudioMeterTrack { get; init; }
    public required FrameworkElement MicMeterTrack { get; init; }
    public required Action InitializeAudioMeterBrushes { get; init; }
    public required Action EnsureAudioMeterTimerRunning { get; init; }
    public required Action<bool> SetAudioMeterMonitoringState { get; init; }
    public required Action PrimePreviewAudioFadeIn { get; init; }
    public required Func<bool> IsPreviewAudioFadeInActive { get; init; }
    public required Func<bool> IsPreviewAudioFadeAnimationActive { get; init; }
    public required Action CancelPreviewAudioFadeInForUser { get; init; }
    public required Action SetupMicrophoneVolumeBindings { get; init; }
    public required Action ApplyInitialMicrophoneControlsVisibility { get; init; }
    public required Action ApplyDeviceAudioControlState { get; init; }
    public required Action ResetAudioMeterVisuals { get; init; }
    public required Action<double> SetAudioMeterTargetLevel { get; init; }
    public required Action EnsureAudioInputSelection { get; init; }
    public required Action EnsureMicrophoneSelection { get; init; }
    public required Action EnsureDeviceAudioModeSelection { get; init; }
    public required Action AnimateAudioMeterTick { get; init; }
}

internal sealed class AudioControlBindingController
{
    private readonly AudioControlBindingControllerContext _context;

    public AudioControlBindingController(AudioControlBindingControllerContext context)
    {
        _context = context;
    }

    public void AttachAudioMeterActivationBindings()
    {
        _context.InitializeAudioMeterBrushes();
        _context.ViewModel.AudioMeterActivated += _context.EnsureAudioMeterTimerRunning;
        _context.ViewModel.MicrophoneMeterActivated += _context.EnsureAudioMeterTimerRunning;
    }

    public void ApplyInitialAudioControlBindings()
    {
        _context.AudioRecordToggle.IsChecked = _context.ViewModel.IsAudioEnabled;
        _context.AudioPreviewToggle.IsChecked = _context.ViewModel.IsAudioPreviewEnabled;
        _context.AudioPreviewToggle.IsEnabled = _context.ViewModel.IsAudioEnabled;
        _context.SetAudioMeterMonitoringState(_context.ViewModel.IsAudioPreviewActive);
        // Save the user's preferred volume, start at 0 for hidden audio priming.
        _context.PrimePreviewAudioFadeIn();
        _context.PreviewVolumeSlider.ValueChanged += (s, e) =>
        {
            _context.ViewModel.PreviewVolume = e.NewValue / 100.0;
            _context.PreviewVolumeLabel.Text = $"{(int)e.NewValue}%";
        };
        _context.PreviewVolumeSlider.PointerCaptureLost += (s, e) =>
        {
            if (_context.IsPreviewAudioFadeInActive() || _context.IsPreviewAudioFadeAnimationActive())
            {
                // User explicitly grabbed the slider during a preview volume fade.
                // Pause the volume animation so it doesn't overwrite their choice
                // (Stop() would snap properties back to base values).
                _context.CancelPreviewAudioFadeInForUser();
            }

            _context.ViewModel.SavePreviewVolume();
        };
        _context.SetupMicrophoneVolumeBindings();
        _context.CustomAudioToggle.IsChecked = _context.ViewModel.IsCustomAudioInputEnabled;
        _context.CustomAudioToggle.IsEnabled = !_context.ViewModel.IsRecording;
        _context.MicrophoneToggle.IsChecked = _context.ViewModel.IsMicrophoneEnabled;
        _context.MicrophoneToggle.IsEnabled = !_context.ViewModel.IsRecording;
        _context.AudioInputComboBox.IsEnabled = _context.ViewModel.IsCustomAudioInputEnabled && !_context.ViewModel.IsRecording;
        _context.AudioInputComboBox.SelectedItem = _context.ViewModel.SelectedAudioInputDevice;
        _context.MicrophoneComboBox.IsEnabled = _context.ViewModel.IsMicrophoneEnabled && !_context.ViewModel.IsRecording;
        _context.MicrophoneComboBox.SelectedItem = _context.ViewModel.SelectedMicrophoneDevice;
        _context.ApplyInitialMicrophoneControlsVisibility();
        _context.ApplyDeviceAudioControlState();
    }

    public void ApplyInitialAudioMeterPresentation()
    {
        _context.ResetAudioMeterVisuals();
        _context.SetAudioMeterTargetLevel(_context.ViewModel.AudioMeterTarget);
    }

    public void EnsureAudioControlSelections()
    {
        _context.EnsureAudioInputSelection();
        _context.EnsureMicrophoneSelection();
        _context.EnsureDeviceAudioModeSelection();
    }

    public void AttachAudioSelectionBindings()
    {
        _context.AudioInputComboBox.SelectionChanged += (s, e) =>
        {
            if (_context.AudioInputComboBox.SelectedItem is AudioInputDevice device &&
                device != _context.ViewModel.SelectedAudioInputDevice)
            {
                _context.ViewModel.SelectedAudioInputDevice = device;
            }
        };

        _context.MicrophoneComboBox.SelectionChanged += (s, e) =>
        {
            if (_context.MicrophoneComboBox.SelectedItem is AudioInputDevice device &&
                device != _context.ViewModel.SelectedMicrophoneDevice)
            {
                _context.ViewModel.SelectedMicrophoneDevice = device;
            }
        };

        _context.DeviceAudioModeToggle.Toggled += (s, e) =>
        {
            var mode = _context.DeviceAudioModeToggle.IsOn ? DeviceAudioMode.Analog : DeviceAudioMode.Hdmi;
            if (!string.Equals(mode, _context.ViewModel.SelectedDeviceAudioMode, StringComparison.OrdinalIgnoreCase))
            {
                _context.ViewModel.SelectedDeviceAudioMode = mode;
            }
        };
    }

    public void AttachAudioRecordPreviewToggleBindings()
    {
        _context.AudioRecordToggle.Checked += (s, e) => _context.ViewModel.IsAudioEnabled = true;
        _context.AudioRecordToggle.Unchecked += (s, e) => _context.ViewModel.IsAudioEnabled = false;
        _context.AudioPreviewToggle.Checked += (s, e) => _context.ViewModel.IsAudioPreviewEnabled = true;
        _context.AudioPreviewToggle.Unchecked += (s, e) => _context.ViewModel.IsAudioPreviewEnabled = false;
    }

    public void AttachAudioInputToggleBindings()
    {
        _context.CustomAudioToggle.Click += (s, e) => _context.ViewModel.IsCustomAudioInputEnabled = _context.CustomAudioToggle.IsChecked == true;
        _context.MicrophoneToggle.Click += (s, e) => _context.ViewModel.IsMicrophoneEnabled = _context.MicrophoneToggle.IsChecked == true;
    }

    public void AttachDeviceAudioGainAndMeterBindings()
    {
        _context.AnalogAudioGainSlider.ValueChanged += (s, e) =>
        {
            _context.ViewModel.AnalogAudioGainPercent = e.NewValue;
            _context.AnalogAudioGainValueTextBlock.Text = $"{(int)Math.Round(e.NewValue)}%";
        };
        _context.AudioMeterTrack.SizeChanged += (s, e) => _context.AnimateAudioMeterTick();
        _context.MicMeterTrack.SizeChanged += (s, e) => _context.AnimateAudioMeterTick();
    }
}
