using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    [ObservableProperty]
    public partial ObservableCollection<AudioInputDevice> AudioInputDevices { get; set; } = new();

    [ObservableProperty]
    public partial AudioInputDevice? SelectedAudioInputDevice { get; set; }

    [ObservableProperty]
    public partial bool IsCustomAudioInputEnabled { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<AudioInputDevice> MicrophoneDevices { get; set; } = new();

    [ObservableProperty]
    public partial bool IsMicrophoneEnabled { get; set; }

    [ObservableProperty]
    public partial AudioInputDevice? SelectedMicrophoneDevice { get; set; }

    private string? _pendingSavedAudioDeviceId;
    private string? _pendingSavedMicrophoneDeviceId;
    private double? _pendingSavedMicrophoneVolume;
    private string? _pendingSavedMicrophoneVolumeDeviceId;
    private int _audioEnabledChangeGeneration;
    private bool _suppressAudioPreviewEnabledChangeOperation;
    private bool _suppressMicrophoneMonitorUpdate;

    [ObservableProperty]
    public partial bool IsAudioEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsAudioPreviewEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsAudioPreviewActive { get; set; }

    [ObservableProperty]
    public partial double PreviewVolume { get; set; } = 1.0;

    [ObservableProperty]
    public partial double MicrophoneVolume { get; set; } = 100.0;

    [ObservableProperty]
    public partial double AudioPeak { get; set; }

    [ObservableProperty]
    public partial bool AudioClipping { get; set; }
}
