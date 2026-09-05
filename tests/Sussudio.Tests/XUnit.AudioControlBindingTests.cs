using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Sussudio.Controllers;
using Sussudio.Models;
using Sussudio.ViewModels;
using Xunit;

namespace Sussudio.Tests;

public sealed class AudioControlBindingTests : IDisposable
{
    public AudioControlBindingTests() => Storyboard.Created.Clear();

    public void Dispose() => Storyboard.Created.Clear();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InitialBindingsReflectSelectionsAndRecordingLockouts(bool recording)
    {
        var f = new BindingFixture();
        f.Vm.IsRecording = recording;
        f.Vm.IsAudioEnabled = true;
        f.Vm.IsAudioPreviewEnabled = true;
        f.Vm.IsAudioPreviewActive = true;
        f.Vm.IsCustomAudioInputEnabled = true;
        f.Vm.IsMicrophoneEnabled = true;
        f.Vm.SelectedAudioInputDevice = new AudioInputDevice { Id = "audio" };
        f.Vm.SelectedMicrophoneDevice = new AudioInputDevice { Id = "mic" };

        f.Binding.ApplyInitialAudioControlBindings();

        Assert.True(f.Record.IsChecked);
        Assert.True(f.Preview.IsChecked);
        Assert.True(f.Preview.IsEnabled);
        Assert.True(f.Custom.IsChecked);
        Assert.True(f.Microphone.IsChecked);
        Assert.Equal(!recording, f.Custom.IsEnabled);
        Assert.Equal(!recording, f.Microphone.IsEnabled);
        Assert.Equal(!recording, f.AudioDevices.IsEnabled);
        Assert.Equal(!recording, f.MicrophoneDevices.IsEnabled);
        Assert.Same(f.Vm.SelectedAudioInputDevice, f.AudioDevices.SelectedItem);
        Assert.Same(f.Vm.SelectedMicrophoneDevice, f.MicrophoneDevices.SelectedItem);
        Assert.Equal(new[] { "monitor:True", "prime", "mic-bindings", "mic-visibility", "device-state" }, f.Events);
        Assert.Equal(0, f.Vm.SavePreviewVolumeCount);
    }

    [Fact]
    public void DisabledInputsStayDisabledDuringInitialBinding()
    {
        var f = new BindingFixture();
        f.Binding.ApplyInitialAudioControlBindings();
        Assert.False(f.Preview.IsEnabled);
        Assert.False(f.AudioDevices.IsEnabled);
        Assert.False(f.MicrophoneDevices.IsEnabled);
    }

    [Fact]
    public void RegisteredToggleHandlersRouteOnlyTheirOwnState()
    {
        var f = new BindingFixture();
        f.Binding.AttachAudioRecordPreviewToggleBindings();
        f.Binding.AttachAudioInputToggleBindings();

        f.Record.IsChecked = true;
        Assert.True(f.Vm.IsAudioEnabled);
        Assert.False(f.Vm.IsAudioPreviewEnabled);
        f.Preview.IsChecked = true;
        Assert.True(f.Vm.IsAudioPreviewEnabled);
        f.Record.IsChecked = false;
        Assert.False(f.Vm.IsAudioEnabled);
        Assert.True(f.Vm.IsAudioPreviewEnabled);
        f.Preview.IsChecked = false;
        Assert.False(f.Vm.IsAudioPreviewEnabled);

        f.Custom.IsChecked = true;
        Assert.False(f.Vm.IsCustomAudioInputEnabled);
        f.Custom.RaiseClick();
        Assert.True(f.Vm.IsCustomAudioInputEnabled);
        Assert.False(f.Vm.IsMicrophoneEnabled);
        f.Microphone.IsChecked = true;
        f.Microphone.RaiseClick();
        Assert.True(f.Vm.IsMicrophoneEnabled);
        f.Custom.IsChecked = null;
        f.Custom.RaiseClick();
        f.Microphone.IsChecked = null;
        f.Microphone.RaiseClick();
        Assert.False(f.Vm.IsCustomAudioInputEnabled);
        Assert.False(f.Vm.IsMicrophoneEnabled);
    }

    [Fact]
    public void SelectionHandlersIgnoreUntypedAndAlreadySelectedValues()
    {
        var f = new BindingFixture();
        var audio = new AudioInputDevice { Id = "audio" };
        var microphone = new AudioInputDevice { Id = "mic" };
        f.Vm.SelectedAudioInputDevice = audio;
        f.Vm.SelectedMicrophoneDevice = microphone;
        f.Binding.AttachAudioSelectionBindings();
        var audioSets = f.Vm.SelectedAudioInputDeviceSetCount;
        var micSets = f.Vm.SelectedMicrophoneDeviceSetCount;

        f.AudioDevices.SelectedItem = audio;
        f.MicrophoneDevices.SelectedItem = microphone;
        f.AudioDevices.SelectedItem = "not a device";
        f.MicrophoneDevices.SelectedItem = null;
        Assert.Equal(audioSets, f.Vm.SelectedAudioInputDeviceSetCount);
        Assert.Equal(micSets, f.Vm.SelectedMicrophoneDeviceSetCount);

        var replacement = new AudioInputDevice { Id = "replacement" };
        f.AudioDevices.SelectedItem = replacement;
        f.MicrophoneDevices.SelectedItem = replacement;
        Assert.Same(replacement, f.Vm.SelectedAudioInputDevice);
        Assert.Same(replacement, f.Vm.SelectedMicrophoneDevice);
        Assert.Equal(audioSets + 1, f.Vm.SelectedAudioInputDeviceSetCount);
        Assert.Equal(micSets + 1, f.Vm.SelectedMicrophoneDeviceSetCount);

        f.Vm.SelectedDeviceAudioMode = "analog";
        var modeSets = f.Vm.SelectedDeviceAudioModeSetCount;
        f.Mode.IsOn = true;
        Assert.Equal(modeSets, f.Vm.SelectedDeviceAudioModeSetCount);
        f.Mode.IsOn = false;
        Assert.Equal(DeviceAudioMode.Hdmi, f.Vm.SelectedDeviceAudioMode);
        Assert.Equal(modeSets + 1, f.Vm.SelectedDeviceAudioModeSetCount);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void PreviewVolumeSavesAfterCancelingAnyUserInterruptedFade(bool fadeIn, bool animation)
    {
        var f = new BindingFixture { FadeIn = fadeIn, FadeAnimation = animation };
        f.Binding.ApplyInitialAudioControlBindings();
        f.Events.Clear();
        f.Vm.OnSavePreviewVolume = () => f.Events.Add("save");

        f.Volume.Value = 42.9;
        Assert.Equal(0.429, f.Vm.PreviewVolume, 8);
        Assert.Equal("42%", f.VolumeLabel.Text);
        Assert.Equal(0, f.Vm.SavePreviewVolumeCount);
        f.Volume.RaisePointerCaptureLost();

        Assert.Equal(1, f.Vm.SavePreviewVolumeCount);
        Assert.Equal(fadeIn || animation ? new[] { "cancel", "save" } : new[] { "save" }, f.Events);
    }

    [Fact]
    public void GainAndMeterBindingsKeepRoundingAndCallbackRoutes()
    {
        var f = new BindingFixture();
        f.Binding.AttachDeviceAudioGainAndMeterBindings();
        f.Gain.Value = 42.6;
        Assert.Equal(42.6, f.Vm.AnalogAudioGainPercent);
        Assert.Equal("43%", f.GainLabel.Text);
        f.AudioTrack.RaiseSizeChanged(100, 8);
        f.MicTrack.RaiseSizeChanged(100, 8);
        Assert.Equal(new[] { "animate", "animate" }, f.Events);
    }

    [Fact]
    public void ActivationAndInitialMeterCallbacksKeepTheirOrder()
    {
        var f = new BindingFixture();
        f.Vm.AudioMeterTarget = 0.4;
        f.Binding.AttachAudioMeterActivationBindings();
        f.Vm.RaiseAudioMeterActivated();
        f.Vm.RaiseMicrophoneMeterActivated();
        f.Binding.ApplyInitialAudioMeterPresentation();
        f.Binding.EnsureAudioControlSelections();
        Assert.Equal(new[] { "brushes", "timer", "timer", "reset", "target", "audio-selection", "mic-selection", "mode-selection" }, f.Events);
        Assert.Equal(0.4, f.LastTarget);
    }

    [Fact]
    public void PresentationDispatchHonorsRecordingAndFadeState()
    {
        var f = new BindingFixture();
        f.Vm.IsRecording = true;
        f.Vm.IsCustomAudioInputEnabled = true;
        f.Vm.IsMicrophoneEnabled = true;
        Assert.True(f.Presentation.TryHandlePropertyChanged(nameof(MainViewModel.IsCustomAudioInputEnabled)));
        Assert.True(f.Custom.IsChecked);
        Assert.False(f.AudioDevices.IsEnabled);
        Assert.True(f.Presentation.TryHandlePropertyChanged(nameof(MainViewModel.IsMicrophoneEnabled)));
        Assert.True(f.Microphone.IsChecked);
        Assert.False(f.MicrophoneDevices.IsEnabled);
        Assert.Contains("mic-visibility", f.Events);

        f.Vm.IsRecording = false;
        f.Presentation.HandleCustomAudioInputEnabledChanged();
        f.Presentation.HandleMicrophoneEnabledChanged();
        Assert.True(f.AudioDevices.IsEnabled);
        Assert.True(f.MicrophoneDevices.IsEnabled);

        f.Volume.Value = 10;
        f.VolumeLabel.Text = "unchanged";
        f.Vm.PreviewVolume = 0.8;
        f.FadeIn = true;
        Assert.True(f.Presentation.TryHandlePropertyChanged(nameof(MainViewModel.PreviewVolume)));
        Assert.Equal(10, f.Volume.Value);
        Assert.Equal("unchanged", f.VolumeLabel.Text);
        f.FadeIn = false;
        f.Presentation.HandlePreviewVolumeChanged();
        Assert.Equal(80, f.Volume.Value);
        Assert.Equal("80%", f.VolumeLabel.Text);
        f.Vm.MicrophoneVolume = 35;
        Assert.True(f.Presentation.TryHandlePropertyChanged(nameof(MainViewModel.MicrophoneVolume)));
        Assert.Equal(35, f.LastMicVolume);
        Assert.False(f.Presentation.TryHandlePropertyChanged("UnrelatedProperty"));
    }

    [Fact]
    public void AudioPresentationSeparatesEnabledRequestedAndActiveStates()
    {
        var f = new BindingFixture();
        f.Preview.IsChecked = true;
        Assert.True(f.Presentation.TryHandlePropertyChanged(nameof(MainViewModel.IsAudioEnabled)));
        Assert.False(f.Record.IsChecked);
        Assert.False(f.Preview.IsEnabled);
        Assert.False(f.Preview.IsChecked);
        Assert.Contains("disabled:True", f.Events);
        f.Vm.IsAudioPreviewEnabled = true;
        Assert.True(f.Presentation.TryHandlePropertyChanged(nameof(MainViewModel.IsAudioPreviewEnabled)));
        Assert.True(f.Preview.IsChecked);
        f.Vm.IsAudioPreviewActive = true;
        Assert.True(f.Presentation.TryHandlePropertyChanged(nameof(MainViewModel.IsAudioPreviewActive)));
        Assert.Contains("monitor:True", f.Events);
    }

    [Fact]
    public void MicrophoneSlidersSynchronizeWithoutReentrantModelWrites()
    {
        var f = new MicrophoneFixture();
        f.Vm.MicrophoneVolume = 35;
        f.Controller.AttachVolumeBindings();
        Assert.Equal(35, f.Slider.Value);
        Assert.Equal(35, f.Shelf.Value);
        var initialWrites = f.Vm.MicrophoneVolumeSetCount;
        f.Slider.Value = 61;
        Assert.Equal(61, f.Shelf.Value);
        Assert.Equal(61, f.Vm.MicrophoneVolume);
        Assert.Equal(initialWrites + 1, f.Vm.MicrophoneVolumeSetCount);
        f.Shelf.Value = 22;
        Assert.Equal(22, f.Slider.Value);
        Assert.Equal("22%", f.Label.Text);
        Assert.Equal(initialWrites + 2, f.Vm.MicrophoneVolumeSetCount);
        Assert.Equal(0, f.Vm.SaveMicrophoneVolumeCount);
        f.Slider.RaisePointerCaptureLost();
        f.Shelf.RaisePointerCaptureLost();
        Assert.Equal(2, f.Vm.SaveMicrophoneVolumeCount);
    }

    [Theory]
    [InlineData(-20, 0)]
    [InlineData(120, 100)]
    public void InitialMicrophonePresentationClampsWithoutSaving(double input, double displayed)
    {
        var f = new MicrophoneFixture();
        f.Vm.MicrophoneVolume = input;
        f.Controller.AttachVolumeBindings();
        Assert.Equal(displayed, f.Slider.Value);
        Assert.Equal(displayed, f.Shelf.Value);
        Assert.Equal(input, f.Vm.MicrophoneVolume);
        Assert.Equal(0, f.Vm.SaveMicrophoneVolumeCount);
    }

    [Fact]
    public void StaleMicrophoneShowCompletionCannotOverrideActiveHide()
    {
        var f = new MicrophoneFixture();
        var firstNew = Storyboard.Created.Count;
        f.Vm.IsMicrophoneEnabled = true;
        f.Controller.UpdateVisibility();
        var show = Storyboard.Created.Skip(firstNew).Single(s => s.BeginCount == 1);
        f.Row.Opacity = 1;
        f.Vm.IsMicrophoneEnabled = false;
        f.Controller.UpdateVisibility();
        var hide = Storyboard.Created.Skip(firstNew).Single(s => !ReferenceEquals(s, show));
        Assert.Equal(1, show.StopCount);
        Assert.Equal(1, hide.BeginCount);
        show.Complete();
        Assert.Equal(1, f.Row.Opacity);
        Assert.Equal(0, f.Resets);
        hide.Complete();
        Assert.Equal(0, f.Row.Opacity);
        Assert.Equal(7, f.DeviceTranslate.Y);
        Assert.Equal(14, f.MicTranslate.Y);
        Assert.Equal(1, f.Resets);
    }

    [Fact]
    public void ZeroMeterTickStopsTimerAndResetClearsPresentation()
    {
        var vm = new MainViewModel();
        var queue = new DispatcherQueue();
        var rawClip = new RectangleGeometry();
        var colorClip = new RectangleGeometry();
        var micClip = new RectangleGeometry();
        var peak = new TranslateTransform { X = 5 };
        var minimum = new TranslateTransform { X = 6 };
        var maximum = new TranslateTransform { X = 7 };
        var meter = new AudioMeterController(new AudioMeterControllerContext
        {
            DispatcherQueue = queue, ViewModel = vm,
            AudioMeterTrack = new Border(), AudioMeterContent = new FrameworkElement(),
            AudioMeterRawFill = new Border(), AudioMeterFill = new Border { Background = new LinearGradientBrush() },
            AudioMeterRawClip = rawClip, AudioMeterColorClip = colorClip,
            AudioPeakHoldIndicator = new Border(), AudioPeakHoldTranslate = peak,
            AudioRangeMinMarker = new Border(), AudioRangeMinTranslate = minimum,
            AudioRangeMaxMarker = new Border(), AudioRangeMaxTranslate = maximum,
            MicMeterTrack = new Border(), MicMeterContent = new FrameworkElement(), MicMeterClip = micClip
        });
        meter.Initialize();
        var timer = queue.LastCreatedTimer!;
        Assert.Equal(TimeSpan.FromMilliseconds(16), timer.Interval);
        Assert.True(timer.IsRepeating);
        meter.EnsureTimerRunning();
        meter.EnsureTimerRunning();
        Assert.Equal(1, timer.StartCount);
        timer.FireTick();
        Assert.Equal(1, timer.StopCount);
        Assert.Equal(1, vm.ResetAudioMeterTimerFlagCount);
        meter.ResetVisuals();
        Assert.Equal(0, rawClip.Rect.Width);
        Assert.Equal(0, colorClip.Rect.Width);
        Assert.Equal(0, micClip.Rect.Width);
        Assert.Equal(0, peak.X);
        Assert.Equal(0, minimum.X);
        Assert.Equal(0, maximum.X);
    }

    private sealed class MicrophoneFixture
    {
        internal MainViewModel Vm { get; } = new();
        internal Slider Slider { get; } = new();
        internal Slider Shelf { get; } = new();
        internal TextBlock Label { get; } = new();
        internal Grid Row { get; } = new();
        internal TranslateTransform DeviceTranslate { get; } = new();
        internal TranslateTransform MicTranslate { get; } = new();
        internal int Resets;
        internal MicrophoneControlsController Controller { get; }

        internal MicrophoneFixture() => Controller = new(new MicrophoneControlsControllerContext
        {
            ViewModel = Vm, MicVolumeSlider = Slider, MicVolumeShelfSlider = Shelf, MicVolumeLabel = Label,
            MicMeterRow = Row, DeviceAudioRowTranslate = DeviceTranslate, MicMeterRowTranslate = MicTranslate,
            ResetMicrophoneMeterVisuals = () => Resets++
        });
    }

    private sealed class BindingFixture
    {
        internal MainViewModel Vm { get; } = new();
        internal ToggleButton Record { get; } = new();
        internal ToggleButton Preview { get; } = new();
        internal Slider Volume { get; } = new();
        internal TextBlock VolumeLabel { get; } = new();
        internal CheckBox Custom { get; } = new();
        internal CheckBox Microphone { get; } = new();
        internal ComboBox AudioDevices { get; } = new();
        internal ComboBox MicrophoneDevices { get; } = new();
        internal ToggleSwitch Mode { get; } = new();
        internal Slider Gain { get; } = new();
        internal TextBlock GainLabel { get; } = new();
        internal FrameworkElement AudioTrack { get; } = new();
        internal FrameworkElement MicTrack { get; } = new();
        internal List<string> Events { get; } = new();
        internal bool FadeIn;
        internal bool FadeAnimation;
        internal double LastTarget;
        internal double LastMicVolume;
        internal AudioControlBindingController Binding { get; }
        internal AudioControlPresentationController Presentation { get; }

        internal BindingFixture()
        {
            Binding = new(new AudioControlBindingControllerContext
            {
                ViewModel = Vm, AudioRecordToggle = Record, AudioPreviewToggle = Preview,
                PreviewVolumeSlider = Volume, PreviewVolumeLabel = VolumeLabel, CustomAudioToggle = Custom,
                MicrophoneToggle = Microphone, AudioInputComboBox = AudioDevices, MicrophoneComboBox = MicrophoneDevices,
                DeviceAudioModeToggle = Mode, AnalogAudioGainSlider = Gain, AnalogAudioGainValueTextBlock = GainLabel,
                AudioMeterTrack = AudioTrack, MicMeterTrack = MicTrack,
                InitializeAudioMeterBrushes = () => Events.Add("brushes"), EnsureAudioMeterTimerRunning = () => Events.Add("timer"),
                SetAudioMeterMonitoringState = value => Events.Add($"monitor:{value}"), PrimePreviewAudioFadeIn = () => Events.Add("prime"),
                IsPreviewAudioFadeInActive = () => FadeIn, IsPreviewAudioFadeAnimationActive = () => FadeAnimation,
                CancelPreviewAudioFadeInForUser = () => Events.Add("cancel"), SetupMicrophoneVolumeBindings = () => Events.Add("mic-bindings"),
                ApplyInitialMicrophoneControlsVisibility = () => Events.Add("mic-visibility"), ApplyDeviceAudioControlState = () => Events.Add("device-state"),
                ResetAudioMeterVisuals = () => Events.Add("reset"), SetAudioMeterTargetLevel = value => { LastTarget = value; Events.Add("target"); },
                EnsureAudioInputSelection = () => Events.Add("audio-selection"), EnsureMicrophoneSelection = () => Events.Add("mic-selection"),
                EnsureDeviceAudioModeSelection = () => Events.Add("mode-selection"), AnimateAudioMeterTick = () => Events.Add("animate")
            });
            Presentation = new(new AudioControlPresentationControllerContext
            {
                ViewModel = Vm, CustomAudioToggle = Custom, AudioInputComboBox = AudioDevices,
                MicrophoneToggle = Microphone, MicrophoneComboBox = MicrophoneDevices,
                AudioRecordToggle = Record, AudioPreviewToggle = Preview, PreviewVolumeSlider = Volume, PreviewVolumeLabel = VolumeLabel,
                IsPreviewAudioFadeInActive = () => FadeIn, SetAudioMeterMonitoringState = value => Events.Add($"monitor:{value}"),
                AnimateAudioMeterDisabled = value => Events.Add($"disabled:{value}"), UpdateMicrophoneControlsVisibility = () => Events.Add("mic-visibility"),
                SyncMicrophoneVolumeControls = value => LastMicVolume = value
            });
        }
    }
}
