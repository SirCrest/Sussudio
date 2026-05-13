using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class PreviewAudioFadeControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required Slider PreviewVolumeSlider { get; init; }
    public required TextBlock PreviewVolumeLabel { get; init; }
}

internal sealed class PreviewAudioFadeController
{
    private readonly PreviewAudioFadeControllerContext _context;
    private double _savedPreviewVolume;
    private bool _isFadingIn;
    private Storyboard? _volumeFadeStoryboard;

    public PreviewAudioFadeController(PreviewAudioFadeControllerContext context)
    {
        _context = context;
    }

    public bool IsFadingIn => _isFadingIn;

    public bool IsAnimationActive => _volumeFadeStoryboard is not null;

    public void PrimeFadeIn()
    {
        var volumeTarget = _context.ViewModel.PreviewVolume > 0
            ? _context.ViewModel.PreviewVolume
            : _savedPreviewVolume;
        volumeTarget = Math.Clamp(volumeTarget, 0.0, 1.0);
        if (volumeTarget <= 0)
        {
            _savedPreviewVolume = 0;
            _isFadingIn = false;
            _context.ViewModel.VolumeSaveOverride = null;
            _context.PreviewVolumeSlider.Value = 0;
            _context.PreviewVolumeLabel.Text = "0%";
            return;
        }

        _savedPreviewVolume = volumeTarget;
        _isFadingIn = true;
        _context.ViewModel.VolumeSaveOverride = volumeTarget;
        _context.ViewModel.SuppressVolumeSave = true;
        try
        {
            _context.ViewModel.PreviewVolume = 0;
            _context.PreviewVolumeSlider.Value = 0;
            _context.PreviewVolumeLabel.Text = "0%";
        }
        finally
        {
            _context.ViewModel.SuppressVolumeSave = false;
        }

        Sussudio.Logger.Log($"PREVIEW_AUDIO_FADE_PRIMED targetPct={volumeTarget * 100:0}");
    }

    public void StartFadeIn(int durationMs = 900)
    {
        if (!_isFadingIn)
        {
            return;
        }

        var volumeTarget = Math.Clamp(_savedPreviewVolume, 0.0, 1.0);
        if (volumeTarget <= 0)
        {
            CompleteFadeIn(applyTarget: false);
            return;
        }

        _volumeFadeStoryboard?.Stop();
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var volumeAnimation = new DoubleAnimation
        {
            From = _context.PreviewVolumeSlider.Value,
            To = volumeTarget * 100,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = easing,
            EnableDependentAnimation = true,
        };
        Storyboard.SetTarget(volumeAnimation, _context.PreviewVolumeSlider);
        Storyboard.SetTargetProperty(volumeAnimation, "Value");

        var storyboard = new Storyboard();
        storyboard.Children.Add(volumeAnimation);
        storyboard.Completed += (_, _) => CompleteFadeIn(applyTarget: true);
        _volumeFadeStoryboard = storyboard;
        _context.ViewModel.SuppressVolumeSave = true;
        _context.ViewModel.VolumeSaveOverride = volumeTarget;
        Sussudio.Logger.Log($"PREVIEW_AUDIO_FADE_IN_STARTED targetPct={volumeTarget * 100:0} durationMs={durationMs}");
        storyboard.Begin();
    }

    public async Task StartFadeOutAsync(int durationMs = 450)
    {
        var volumeTarget = _context.ViewModel.PreviewVolume > 0
            ? _context.ViewModel.PreviewVolume
            : _savedPreviewVolume;
        volumeTarget = Math.Clamp(volumeTarget, 0.0, 1.0);
        if (volumeTarget > 0)
        {
            _savedPreviewVolume = volumeTarget;
            _context.ViewModel.VolumeSaveOverride = volumeTarget;
        }

        _isFadingIn = false;
        _volumeFadeStoryboard?.Stop();
        if (_context.PreviewVolumeSlider.Value <= 0.001 && _context.ViewModel.PreviewVolume <= 0.001)
        {
            _context.ViewModel.PreviewVolume = 0;
            _context.PreviewVolumeSlider.Value = 0;
            _context.PreviewVolumeLabel.Text = "0%";
            return;
        }

        var easing = new CubicEase { EasingMode = EasingMode.EaseIn };
        var volumeAnimation = new DoubleAnimation
        {
            From = _context.PreviewVolumeSlider.Value,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = easing,
            EnableDependentAnimation = true,
        };
        Storyboard.SetTarget(volumeAnimation, _context.PreviewVolumeSlider);
        Storyboard.SetTargetProperty(volumeAnimation, "Value");

        var storyboard = new Storyboard();
        storyboard.Children.Add(volumeAnimation);
        _volumeFadeStoryboard = storyboard;
        _context.ViewModel.SuppressVolumeSave = true;
        Sussudio.Logger.Log($"PREVIEW_AUDIO_FADE_OUT_STARTED fromPct={_context.PreviewVolumeSlider.Value:0} durationMs={durationMs}");
        await BeginStoryboardAsync(storyboard);
        _volumeFadeStoryboard = null;
        _context.ViewModel.PreviewVolume = 0;
        _context.PreviewVolumeSlider.Value = 0;
        _context.PreviewVolumeLabel.Text = "0%";
        _context.ViewModel.SuppressVolumeSave = false;
        Sussudio.Logger.Log("PREVIEW_AUDIO_FADE_OUT_COMPLETED");
    }

    public void CancelFadeInForUser()
    {
        _volumeFadeStoryboard?.Pause();
        _volumeFadeStoryboard = null;
        _isFadingIn = false;
        _context.ViewModel.SuppressVolumeSave = false;
        _context.ViewModel.VolumeSaveOverride = null;
        _savedPreviewVolume = _context.ViewModel.PreviewVolume;
    }

    private void CompleteFadeIn(bool applyTarget)
    {
        _volumeFadeStoryboard = null;
        _isFadingIn = false;
        _context.ViewModel.SuppressVolumeSave = false;
        _context.ViewModel.VolumeSaveOverride = null;
        if (applyTarget && _savedPreviewVolume > 0)
        {
            _context.ViewModel.PreviewVolume = _savedPreviewVolume;
            _context.PreviewVolumeSlider.Value = _savedPreviewVolume * 100;
            _context.PreviewVolumeLabel.Text = $"{(int)(_savedPreviewVolume * 100)}%";
        }
    }

    private static Task BeginStoryboardAsync(Storyboard storyboard)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        storyboard.Completed += (_, _) => completion.TrySetResult(true);
        storyboard.Begin();
        return completion.Task;
    }
}
