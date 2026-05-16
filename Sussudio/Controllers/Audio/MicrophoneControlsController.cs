using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

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
