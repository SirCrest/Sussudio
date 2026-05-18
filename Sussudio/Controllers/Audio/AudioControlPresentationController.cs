using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class AudioControlPresentationControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required CheckBox CustomAudioToggle { get; init; }
    public required ComboBox AudioInputComboBox { get; init; }
    public required CheckBox MicrophoneToggle { get; init; }
    public required ComboBox MicrophoneComboBox { get; init; }
    public required ToggleButton AudioRecordToggle { get; init; }
    public required ToggleButton AudioPreviewToggle { get; init; }
    public required Slider PreviewVolumeSlider { get; init; }
    public required TextBlock PreviewVolumeLabel { get; init; }
    public required Func<bool> IsPreviewAudioFadeInActive { get; init; }
    public required Action<bool> SetAudioMeterMonitoringState { get; init; }
    public required Action<bool> AnimateAudioMeterDisabled { get; init; }
    public required Action UpdateMicrophoneControlsVisibility { get; init; }
    public required Action<double> SyncMicrophoneVolumeControls { get; init; }
}

internal sealed class AudioControlPresentationController
{
    private readonly AudioControlPresentationControllerContext _context;

    public AudioControlPresentationController(AudioControlPresentationControllerContext context)
    {
        _context = context;
    }

    public bool TryHandlePropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.IsCustomAudioInputEnabled):
                HandleCustomAudioInputEnabledChanged();
                return true;

            case nameof(MainViewModel.IsMicrophoneEnabled):
                HandleMicrophoneEnabledChanged();
                return true;

            case nameof(MainViewModel.IsAudioEnabled):
                HandleAudioEnabledChanged();
                return true;

            case nameof(MainViewModel.IsAudioPreviewEnabled):
                HandleAudioPreviewEnabledChanged();
                return true;

            case nameof(MainViewModel.IsAudioPreviewActive):
                HandleAudioPreviewActiveChanged();
                return true;

            case nameof(MainViewModel.PreviewVolume):
                HandlePreviewVolumeChanged();
                return true;

            case nameof(MainViewModel.MicrophoneVolume):
                HandleMicrophoneVolumeChanged();
                return true;

            default:
                return false;
        }
    }

    public void HandleCustomAudioInputEnabledChanged()
    {
        if ((_context.CustomAudioToggle.IsChecked == true) != _context.ViewModel.IsCustomAudioInputEnabled)
        {
            _context.CustomAudioToggle.IsChecked = _context.ViewModel.IsCustomAudioInputEnabled;
        }

        _context.AudioInputComboBox.IsEnabled = _context.ViewModel.IsCustomAudioInputEnabled && !_context.ViewModel.IsRecording;
    }

    public void HandleMicrophoneEnabledChanged()
    {
        if ((_context.MicrophoneToggle.IsChecked == true) != _context.ViewModel.IsMicrophoneEnabled)
        {
            _context.MicrophoneToggle.IsChecked = _context.ViewModel.IsMicrophoneEnabled;
        }

        _context.MicrophoneComboBox.IsEnabled = _context.ViewModel.IsMicrophoneEnabled && !_context.ViewModel.IsRecording;
        _context.UpdateMicrophoneControlsVisibility();
    }

    public void HandleAudioEnabledChanged()
    {
        if (_context.AudioRecordToggle.IsChecked != _context.ViewModel.IsAudioEnabled)
        {
            _context.AudioRecordToggle.IsChecked = _context.ViewModel.IsAudioEnabled;
        }

        _context.AudioPreviewToggle.IsEnabled = _context.ViewModel.IsAudioEnabled;
        if (!_context.ViewModel.IsAudioEnabled && _context.AudioPreviewToggle.IsChecked == true)
        {
            _context.AudioPreviewToggle.IsChecked = false;
        }

        _context.AnimateAudioMeterDisabled(!_context.ViewModel.IsAudioEnabled);
    }

    public void HandleAudioPreviewEnabledChanged()
    {
        if (_context.AudioPreviewToggle.IsChecked != _context.ViewModel.IsAudioPreviewEnabled)
        {
            _context.AudioPreviewToggle.IsChecked = _context.ViewModel.IsAudioPreviewEnabled;
        }
    }

    public void HandleAudioPreviewActiveChanged()
    {
        _context.SetAudioMeterMonitoringState(_context.ViewModel.IsAudioPreviewActive);
    }

    public void HandlePreviewVolumeChanged()
    {
        if (_context.IsPreviewAudioFadeInActive())
        {
            return;
        }

        var volumePct = _context.ViewModel.PreviewVolume * 100;
        if (_context.PreviewVolumeSlider.Value != volumePct)
        {
            _context.PreviewVolumeSlider.Value = volumePct;
        }

        _context.PreviewVolumeLabel.Text = $"{(int)volumePct}%";
    }

    public void HandleMicrophoneVolumeChanged()
    {
        _context.SyncMicrophoneVolumeControls(_context.ViewModel.MicrophoneVolume);
    }
}
