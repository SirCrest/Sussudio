using System;
using System.Numerics;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
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

internal sealed class AudioMeterControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required MainViewModel ViewModel { get; init; }
    public required Border AudioMeterTrack { get; init; }
    public required FrameworkElement AudioMeterContent { get; init; }
    public required Border AudioMeterRawFill { get; init; }
    public required Border AudioMeterFill { get; init; }
    public required RectangleGeometry AudioMeterRawClip { get; init; }
    public required RectangleGeometry AudioMeterColorClip { get; init; }
    public required Border AudioPeakHoldIndicator { get; init; }
    public required TranslateTransform AudioPeakHoldTranslate { get; init; }
    public required Border AudioRangeMinMarker { get; init; }
    public required TranslateTransform AudioRangeMinTranslate { get; init; }
    public required Border AudioRangeMaxMarker { get; init; }
    public required TranslateTransform AudioRangeMaxTranslate { get; init; }
    public required Border MicMeterTrack { get; init; }
    public required FrameworkElement MicMeterContent { get; init; }
    public required RectangleGeometry MicMeterClip { get; init; }
}

internal sealed class AudioMeterController
{
    private const long AudioPeakHoldDurationMs = 1500;
    private const double AudioPeakHoldDecayPerSecond = 0.8;
    private const long AudioRangeWindowMs = 3000;
    private static readonly SolidColorBrush MonitoringPeakBrush =
        new(Windows.UI.Color.FromArgb(255, 255, 255, 255));
    private static readonly SolidColorBrush IdlePeakBrush =
        new(Windows.UI.Color.FromArgb(255, 160, 160, 160));

    private readonly AudioMeterControllerContext _context;
    private double _audioPeakHoldLevel;
    private long _audioPeakHoldTimestamp;
    private double _audioRangeMin = 1.0;
    private double _audioRangeMax;
    private long _audioRangeResetTimestamp;
    private double _audioMeterDisplayLevel;
    private double _audioMeterTargetLevel;
    private double _micMeterDisplayLevel;
    private double _micMeterTargetLevel;
    private LinearGradientBrush? _audioMeterColorBrush;
    private DispatcherQueueTimer? _audioMeterAnimationTimer;
    private Storyboard? _audioMeterMonitoringStoryboard;

    public AudioMeterController(AudioMeterControllerContext context)
    {
        _context = context;
    }

    public void Initialize()
    {
        _audioMeterAnimationTimer = _context.DispatcherQueue.CreateTimer();
        _audioMeterAnimationTimer.Interval = TimeSpan.FromMilliseconds(16);
        _audioMeterAnimationTimer.IsRepeating = true;
        _audioMeterAnimationTimer.Tick += (_, _) => AnimateTick();

        _audioMeterColorBrush = (LinearGradientBrush)_context.AudioMeterFill.Background;

        SetupRoundedContentClip(_context.AudioMeterContent, 3f);
        SetupRoundedContentClip(_context.MicMeterContent, 3f);
    }

    public void AnimateTick()
    {
        _audioMeterTargetLevel = _context.ViewModel.AudioMeterTarget;
        var target = _audioMeterTargetLevel;
        var nowMs = Environment.TickCount64;

        if (target >= _audioMeterDisplayLevel)
        {
            _audioMeterDisplayLevel += (target - _audioMeterDisplayLevel) * 0.4;
        }
        else
        {
            _audioMeterDisplayLevel += (target - _audioMeterDisplayLevel) * 0.06;
        }

        if (_audioMeterDisplayLevel < 0.001)
        {
            _audioMeterDisplayLevel = 0;
        }

        if (target >= _audioPeakHoldLevel)
        {
            _audioPeakHoldLevel = target;
            _audioPeakHoldTimestamp = nowMs;
        }
        else if (nowMs - _audioPeakHoldTimestamp > AudioPeakHoldDurationMs)
        {
            var dt = (nowMs - _audioPeakHoldTimestamp - AudioPeakHoldDurationMs) / 1000.0;
            _audioPeakHoldLevel = Math.Max(0, _audioPeakHoldLevel - (AudioPeakHoldDecayPerSecond * dt));
            _audioPeakHoldTimestamp = nowMs - AudioPeakHoldDurationMs;
        }

        if (nowMs - _audioRangeResetTimestamp > AudioRangeWindowMs)
        {
            _audioRangeMin = target;
            _audioRangeMax = target;
            _audioRangeResetTimestamp = nowMs;
        }
        else
        {
            if (target < _audioRangeMin) _audioRangeMin = target;
            if (target > _audioRangeMax) _audioRangeMax = target;
        }

        var trackWidth = _context.AudioMeterTrack.ActualWidth;
        if (trackWidth > 0)
        {
            var trackHeight = _context.AudioMeterTrack.ActualHeight > 0 ? _context.AudioMeterTrack.ActualHeight : 8;
            var rawLevel = _audioMeterDisplayLevel;
            var colorLevel = rawLevel * _context.ViewModel.PreviewVolume;

            _context.AudioMeterRawClip.Rect = new Windows.Foundation.Rect(0, 0, trackWidth * rawLevel, trackHeight);
            _context.AudioMeterColorClip.Rect = new Windows.Foundation.Rect(0, 0, trackWidth * colorLevel, trackHeight);

            _context.AudioPeakHoldTranslate.X = TranslateMarker(trackWidth, _audioPeakHoldLevel, _context.AudioPeakHoldIndicator.Width);
            _context.AudioRangeMinTranslate.X = TranslateMarker(trackWidth, _audioRangeMin, _context.AudioRangeMinMarker.Width);
            _context.AudioRangeMaxTranslate.X = TranslateMarker(trackWidth, _audioRangeMax, _context.AudioRangeMaxMarker.Width);
        }

        if (_context.ViewModel.IsMicrophoneEnabled)
        {
            _micMeterTargetLevel = Math.Clamp(_context.ViewModel.MicrophoneMeterTarget, 0.0, 1.0);
            if (_micMeterTargetLevel > _micMeterDisplayLevel)
            {
                _micMeterDisplayLevel += (_micMeterTargetLevel - _micMeterDisplayLevel) * 0.4;
            }
            else
            {
                _micMeterDisplayLevel += (_micMeterTargetLevel - _micMeterDisplayLevel) * 0.25;
            }

            if (_micMeterDisplayLevel < 0.001)
            {
                _micMeterDisplayLevel = 0;
            }

            var micTrackWidth = _context.MicMeterTrack.ActualWidth - 2;
            if (micTrackWidth > 0)
            {
                var micFillWidth = _micMeterDisplayLevel * micTrackWidth;
                _context.MicMeterClip.Rect = new Windows.Foundation.Rect(0, 0, micFillWidth, 8);
            }
        }
        else if (_micMeterDisplayLevel != 0 || _micMeterTargetLevel != 0)
        {
            ResetMicrophoneVisuals();
        }

        if (_audioMeterDisplayLevel == 0 &&
            _audioPeakHoldLevel == 0 &&
            target == 0 &&
            _micMeterDisplayLevel == 0 &&
            _micMeterTargetLevel == 0)
        {
            _audioMeterAnimationTimer?.Stop();
            _context.ViewModel.ResetAudioMeterTimerFlag();
        }
    }

    public void ResetVisuals()
    {
        _audioPeakHoldLevel = 0;
        _audioPeakHoldTimestamp = 0;
        _audioRangeMin = 1.0;
        _audioRangeMax = 0;
        _audioRangeResetTimestamp = 0;
        _audioMeterDisplayLevel = 0;
        _context.AudioPeakHoldTranslate.X = 0;
        _context.AudioRangeMinTranslate.X = 0;
        _context.AudioRangeMaxTranslate.X = 0;
        _audioMeterTargetLevel = 0;
        _context.AudioMeterColorClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
        _context.AudioMeterRawClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
        ResetMicrophoneVisuals();
    }

    public void ResetMicrophoneVisuals()
    {
        _micMeterDisplayLevel = 0;
        _micMeterTargetLevel = 0;
        _context.MicMeterClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
    }

    public void SetAudioMeterTargetLevel(double targetLevel)
    {
        _audioMeterTargetLevel = Math.Clamp(targetLevel, 0.0, 1.0);
    }

    public void EnsureTimerRunning()
    {
        if (_audioMeterAnimationTimer is { IsRunning: false })
        {
            _audioMeterAnimationTimer.Start();
        }
    }

    public void StopTimer()
    {
        _audioMeterAnimationTimer?.Stop();
        _audioMeterAnimationTimer = null;
    }

    public void SetMonitoringState(bool isMonitoring)
    {
        if (_audioMeterColorBrush == null) return;

        _audioMeterMonitoringStoryboard?.Stop();

        _context.AudioPeakHoldIndicator.Background = isMonitoring ? MonitoringPeakBrush : IdlePeakBrush;

        var duration = TimeSpan.FromMilliseconds(isMonitoring ? 260 : 220);
        var easing = new CubicEase { EasingMode = isMonitoring ? EasingMode.EaseOut : EasingMode.EaseIn };
        var storyboard = new Storyboard();

        AddOpacityAnimation(storyboard, _context.AudioMeterFill, isMonitoring ? 1.0 : 0.0, duration, easing);
        AddOpacityAnimation(storyboard, _context.AudioPeakHoldIndicator, isMonitoring ? 0.9 : 0.4, duration, easing);
        AddOpacityAnimation(storyboard, _context.AudioRangeMinMarker, isMonitoring ? 0.5 : 0.2, duration, easing);
        AddOpacityAnimation(storyboard, _context.AudioRangeMaxMarker, isMonitoring ? 0.7 : 0.3, duration, easing);

        _audioMeterMonitoringStoryboard = storyboard;
        storyboard.Completed += (_, _) =>
        {
            _audioMeterMonitoringStoryboard = null;
        };
        storyboard.Begin();
    }

    public void AnimateDisabled(bool isDisabled)
    {
        _audioMeterMonitoringStoryboard?.Stop();
        _audioMeterMonitoringStoryboard = null;
        var targetOpacity = isDisabled ? 0.0 : 1.0;
        var duration = TimeSpan.FromMilliseconds(300);
        var easing = new CubicEase { EasingMode = isDisabled ? EasingMode.EaseIn : EasingMode.EaseOut };

        var storyboard = new Storyboard();

        foreach (var element in new UIElement[]
                 {
                     _context.AudioMeterRawFill,
                     _context.AudioMeterFill,
                     _context.AudioPeakHoldIndicator,
                     _context.AudioRangeMinMarker,
                     _context.AudioRangeMaxMarker
                 })
        {
            var anim = new DoubleAnimation
            {
                To = targetOpacity,
                Duration = new Duration(duration),
                EasingFunction = easing,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(anim, element);
            Storyboard.SetTargetProperty(anim, "Opacity");
            storyboard.Children.Add(anim);
        }

        if (!isDisabled)
        {
            storyboard.Completed += (_, _) =>
            {
                SetMonitoringState(_context.ViewModel.IsAudioPreviewActive);
            };
        }

        storyboard.Begin();
    }

    public static double TranslateMarker(double trackWidth, double level, double markerWidth)
    {
        var clamped = Math.Clamp(level, 0.0, 1.0);
        var availableWidth = Math.Max(0, trackWidth - markerWidth);
        return availableWidth * clamped;
    }

    private static void SetupRoundedContentClip(FrameworkElement element, float cornerRadius)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        var geo = visual.Compositor.CreateRoundedRectangleGeometry();
        geo.CornerRadius = new Vector2(cornerRadius, cornerRadius);
        visual.Clip = visual.Compositor.CreateGeometricClip(geo);
        element.SizeChanged += (_, e) =>
        {
            geo.Size = new Vector2((float)e.NewSize.Width, (float)e.NewSize.Height);
        };
    }

    private static void AddOpacityAnimation(
        Storyboard storyboard,
        UIElement element,
        double targetOpacity,
        TimeSpan duration,
        EasingFunctionBase easing)
    {
        var anim = new DoubleAnimation
        {
            To = targetOpacity,
            Duration = new Duration(duration),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(anim, element);
        Storyboard.SetTargetProperty(anim, "Opacity");
        storyboard.Children.Add(anim);
    }
}

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

internal sealed class MicrophoneControlsControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required Slider MicVolumeSlider { get; init; }
    public required Slider MicVolumeShelfSlider { get; init; }
    public required TextBlock MicVolumeLabel { get; init; }
    public required Grid MicMeterRow { get; init; }
    public required TranslateTransform DeviceAudioRowTranslate { get; init; }
    public required TranslateTransform MicMeterRowTranslate { get; init; }
    public required Action ResetMicrophoneMeterVisuals { get; init; }
}

internal sealed class MicrophoneControlsController
{
    private const double MicMeterRowHeight = 14;

    private readonly MicrophoneControlsControllerContext _context;
    private bool _syncingVolumeControls;
    private Storyboard? _activeRowStoryboard;
    private Storyboard? _showRowStoryboard;
    private Storyboard? _hideRowStoryboard;

    public MicrophoneControlsController(MicrophoneControlsControllerContext context)
    {
        _context = context;
    }

    public void AttachVolumeBindings()
    {
        SyncVolumeControls(_context.ViewModel.MicrophoneVolume);

        _context.MicVolumeSlider.ValueChanged += (_, e) => ApplyVolumeSliderChange(e.NewValue);
        _context.MicVolumeSlider.PointerCaptureLost += (_, _) => _context.ViewModel.SaveMicrophoneVolume();
        _context.MicVolumeShelfSlider.ValueChanged += (_, e) => ApplyVolumeSliderChange(e.NewValue);
        _context.MicVolumeShelfSlider.PointerCaptureLost += (_, _) => _context.ViewModel.SaveMicrophoneVolume();
    }

    public void SyncVolumeControls(double volumePercent)
    {
        var clampedVolume = Math.Clamp(volumePercent, 0.0, 100.0);
        if (Math.Abs(_context.MicVolumeSlider.Value - clampedVolume) > 0.5)
        {
            _context.MicVolumeSlider.Value = clampedVolume;
        }

        if (Math.Abs(_context.MicVolumeShelfSlider.Value - clampedVolume) > 0.5)
        {
            _context.MicVolumeShelfSlider.Value = clampedVolume;
        }

        _context.MicVolumeLabel.Text = $"{(int)Math.Round(clampedVolume)}%";
    }

    public void ApplyInitialVisibility()
    {
        _context.MicVolumeShelfSlider.IsEnabled = _context.ViewModel.IsMicrophoneEnabled;
        if (_context.ViewModel.IsMicrophoneEnabled)
        {
            _context.DeviceAudioRowTranslate.Y = 0;
            _context.MicMeterRowTranslate.Y = 0;
            _context.MicMeterRow.Opacity = 1;
        }
        else
        {
            _context.DeviceAudioRowTranslate.Y = MicMeterRowHeight / 2;
            HideRow(immediate: true);
        }
    }

    public void UpdateVisibility()
    {
        _context.MicVolumeShelfSlider.IsEnabled = _context.ViewModel.IsMicrophoneEnabled;
        if (_context.ViewModel.IsMicrophoneEnabled)
        {
            ShowRow();
        }
        else
        {
            HideRow(immediate: false);
        }
    }

    public void StopRowAnimation()
    {
        _activeRowStoryboard?.Stop();
        _activeRowStoryboard = null;
    }

    private void ApplyVolumeSliderChange(double newValue)
    {
        if (_syncingVolumeControls)
        {
            return;
        }

        _syncingVolumeControls = true;
        try
        {
            if (Math.Abs(_context.ViewModel.MicrophoneVolume - newValue) > 0.01)
            {
                _context.ViewModel.MicrophoneVolume = newValue;
            }

            SyncVolumeControls(newValue);
        }
        finally
        {
            _syncingVolumeControls = false;
        }
    }

    private void ShowRow()
    {
        EnsureRowAnimations();
        StopRowAnimation();
        _context.DeviceAudioRowTranslate.Y = MicMeterRowHeight / 2;
        _context.MicMeterRowTranslate.Y = MicMeterRowHeight;
        _context.MicMeterRow.Opacity = 0;
        _activeRowStoryboard = _showRowStoryboard;
        _showRowStoryboard?.Begin();
    }

    private void HideRow(bool immediate)
    {
        EnsureRowAnimations();
        StopRowAnimation();
        if (immediate || _context.MicMeterRow.Opacity == 0)
        {
            _context.DeviceAudioRowTranslate.Y = MicMeterRowHeight / 2;
            _context.MicMeterRowTranslate.Y = MicMeterRowHeight;
            _context.MicMeterRow.Opacity = 0;
            _context.ResetMicrophoneMeterVisuals();
            return;
        }

        _activeRowStoryboard = _hideRowStoryboard;
        _hideRowStoryboard?.Begin();
    }

    private void EnsureRowAnimations()
    {
        _showRowStoryboard ??= CreateRowStoryboard(showing: true);
        _hideRowStoryboard ??= CreateRowStoryboard(showing: false);
    }

    private Storyboard CreateRowStoryboard(bool showing)
    {
        var durationMs = showing ? 350 : 250;
        var easing = new CubicEase { EasingMode = showing ? EasingMode.EaseOut : EasingMode.EaseIn };
        var duration = TimeSpan.FromMilliseconds(durationMs);

        var storyboard = new Storyboard();

        var deviceSlide = new DoubleAnimation
        {
            To = showing ? 0 : MicMeterRowHeight / 2,
            Duration = duration,
            EasingFunction = easing,
        };
        Storyboard.SetTarget(deviceSlide, _context.DeviceAudioRowTranslate);
        Storyboard.SetTargetProperty(deviceSlide, "Y");

        var slide = new DoubleAnimation
        {
            To = showing ? 0 : MicMeterRowHeight,
            Duration = duration,
            EasingFunction = easing,
        };
        Storyboard.SetTarget(slide, _context.MicMeterRowTranslate);
        Storyboard.SetTargetProperty(slide, "Y");

        var fade = new DoubleAnimation
        {
            To = showing ? 1 : 0,
            Duration = duration,
            EasingFunction = easing,
        };
        Storyboard.SetTarget(fade, _context.MicMeterRow);
        Storyboard.SetTargetProperty(fade, "Opacity");

        storyboard.Children.Add(deviceSlide);
        storyboard.Children.Add(slide);
        storyboard.Children.Add(fade);
        storyboard.Completed += (_, _) =>
        {
            if (!ReferenceEquals(_activeRowStoryboard, storyboard))
            {
                return;
            }

            _activeRowStoryboard = null;
            if (showing)
            {
                _context.DeviceAudioRowTranslate.Y = 0;
                _context.MicMeterRowTranslate.Y = 0;
                _context.MicMeterRow.Opacity = 1;
                return;
            }

            _context.DeviceAudioRowTranslate.Y = MicMeterRowHeight / 2;
            _context.MicMeterRowTranslate.Y = MicMeterRowHeight;
            _context.MicMeterRow.Opacity = 0;
            _context.ResetMicrophoneMeterVisuals();
        };

        return storyboard;
    }
}
