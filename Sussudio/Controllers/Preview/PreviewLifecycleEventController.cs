using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Sussudio.Services.Preview;
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

internal sealed class PreviewLifecycleEventControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required Func<bool> ShouldBeginPreviewStartupAttempt { get; init; }
    public required Action BeginPreviewStartupAttempt { get; init; }
    public required Action PrimePreviewAudioFadeIn { get; init; }
    public required Func<bool> IsPreviewReinitAnimating { get; init; }
    public required Action PreparePreviewStartupPresentation { get; init; }
    public required Action StopPreviewStartupWatchdog { get; init; }
    public required Action StartPreviewStartupWatchdog { get; init; }
    public required Action StopPreviewStartupOverlay { get; init; }
    public required Action<PreviewStartupState, string?> SetPreviewStartupState { get; init; }
    public required Func<string> GetPreviewStartupAttemptLabel { get; init; }
    public required Func<Task> StartPreviewRendererAsync { get; init; }
    public required Func<bool> IsPreviewFirstVisualConfirmed { get; init; }
    public required Action RevealPreviewUnavailablePlaceholder { get; init; }
    public required Action<string> SchedulePreviewStartupFailureStop { get; init; }
    public required Action ShowStopPreviewButtonPresentation { get; init; }
    public required Action ShowStartPreviewButtonPresentation { get; init; }
    public required Action ApplyHdrToggleEnabledState { get; init; }
    public required Func<Task> StopPreviewRendererAsync { get; init; }
    public required Action<bool> ResetPreviewStartupTracking { get; init; }
    public required Action HandlePreviewReinitializingChanged { get; init; }
}

internal sealed class PreviewLifecycleEventController
{
    private readonly PreviewLifecycleEventControllerContext _context;
    private bool _stopRequestedByUser;

    public PreviewLifecycleEventController(PreviewLifecycleEventControllerContext context)
    {
        _context = context;
    }

    public bool StopRequestedByUser => _stopRequestedByUser;

    public void SetStopRequestedByUser(bool value)
        => _stopRequestedByUser = value;

    public async Task<bool> TryHandlePropertyChangedAsync(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.IsPreviewing):
                await HandlePreviewingChangedAsync();
                return true;

            case nameof(MainViewModel.IsPreviewReinitializing):
                _context.HandlePreviewReinitializingChanged();
                return true;

            default:
                return false;
        }
    }

    public void HandlePreviewStartRequested()
    {
        _stopRequestedByUser = false;
        if (_context.ShouldBeginPreviewStartupAttempt())
        {
            _context.BeginPreviewStartupAttempt();
        }

        _context.PrimePreviewAudioFadeIn();
        if (!_context.ViewModel.IsPreviewReinitializing && !_context.IsPreviewReinitAnimating())
        {
            _context.PreparePreviewStartupPresentation();
        }
    }

    public void HandlePreviewStopRequested()
    {
        _stopRequestedByUser = _stopRequestedByUser || !_context.ViewModel.IsPreviewReinitializing;
        _context.StopPreviewStartupWatchdog();
        _context.StopPreviewStartupOverlay();
    }

    private async Task HandlePreviewingChangedAsync()
    {
        if (_context.ViewModel.IsPreviewing)
        {
            await HandlePreviewStartedAsync();
            return;
        }

        await HandlePreviewStoppedAsync();
    }

    private async Task HandlePreviewStartedAsync()
    {
        _stopRequestedByUser = false;
        if (_context.ShouldBeginPreviewStartupAttempt())
        {
            _context.BeginPreviewStartupAttempt();
        }

        _context.SetPreviewStartupState(PreviewStartupState.StartingSession, null);
        Logger.Log($"PREVIEW_SESSION_STARTED attempt={_context.GetPreviewStartupAttemptLabel()}");
        if (!_context.ViewModel.IsPreviewReinitializing && !_context.IsPreviewReinitAnimating())
        {
            _context.PreparePreviewStartupPresentation();
        }

        _context.SetPreviewStartupState(PreviewStartupState.RendererAttaching, null);
        try
        {
            await _context.StartPreviewRendererAsync();
        }
        catch (Exception ex)
        {
            var attachFailureReason = $"renderer-attach-failed:{ex.Message}";
            _context.SetPreviewStartupState(PreviewStartupState.Failed, attachFailureReason);
            _context.StopPreviewStartupWatchdog();
            _context.RevealPreviewUnavailablePlaceholder();
            Logger.Log(
                $"PREVIEW_RENDERER_ATTACH_FAILED attempt={_context.GetPreviewStartupAttemptLabel()} " +
                $"reason={attachFailureReason}");
            _context.SchedulePreviewStartupFailureStop(attachFailureReason);
            throw;
        }

        if (!_context.IsPreviewFirstVisualConfirmed())
        {
            _context.SetPreviewStartupState(PreviewStartupState.WaitingForFirstVisual, null);
            _context.StartPreviewStartupWatchdog();
        }

        _context.ShowStopPreviewButtonPresentation();
        _context.ApplyHdrToggleEnabledState();
    }

    private async Task HandlePreviewStoppedAsync()
    {
        _context.StopPreviewStartupWatchdog();
        _context.StopPreviewStartupOverlay();
        // During reinit, the renderer is kept alive (render thread stopped
        // by ViewModel_PreviewRendererStopRequested, instance preserved).
        // StartPreviewRendererAsync will reuse it via Start().
        if (!_context.ViewModel.IsPreviewReinitializing)
        {
            await _context.StopPreviewRendererAsync();
        }

        if (!_context.ViewModel.IsPreviewReinitializing && !_context.IsPreviewReinitAnimating())
        {
            _context.RevealPreviewUnavailablePlaceholder();
        }

        if (_context.ViewModel.IsPreviewReinitializing)
        {
            _context.ShowStopPreviewButtonPresentation();
        }
        else
        {
            _context.ShowStartPreviewButtonPresentation();
        }

        _context.ApplyHdrToggleEnabledState();
        _context.ResetPreviewStartupTracking(
            _context.ViewModel.IsPreviewReinitializing || _context.IsPreviewReinitAnimating());
    }
}

internal sealed class PreviewButtonActionControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required Action<bool> SetPreviewStopRequestedByUser { get; init; }
    public required Func<string?> GetPreviewStartupAttemptId { get; init; }
    public required Action StopPreviewFadeInTimer { get; init; }
    public required Func<Task> StartPreviewAudioFadeOutAsync { get; init; }
    public required Func<Task> AnimatePreviewOutAsync { get; init; }
    public required Action<string> ClearPreviewReinitAnimation { get; init; }
    public required Action ResetPreviewContentTransform { get; init; }
    public required Action RevealPreviewUnavailablePlaceholder { get; init; }
}

internal sealed class PreviewButtonActionController
{
    private readonly PreviewButtonActionControllerContext _context;

    public PreviewButtonActionController(PreviewButtonActionControllerContext context)
    {
        _context = context;
    }

    public async Task TogglePreviewAsync(string operationName)
    {
        var viewModel = _context.ViewModel;
        if (viewModel.IsPreviewReinitializing && !viewModel.IsPreviewing)
        {
            _context.SetPreviewStopRequestedByUser(true);
            viewModel.CancelPendingPreviewRestart();
            Logger.Log($"PREVIEW_REINIT_CANCEL_REQUESTED attempt={_context.GetPreviewStartupAttemptId() ?? "none"}", operationName);
            return;
        }

        if (viewModel.IsPreviewing)
        {
            _context.SetPreviewStopRequestedByUser(true);
            _context.StopPreviewFadeInTimer();
            var audioFadeOutTask = _context.StartPreviewAudioFadeOutAsync();
            var previewFadeOutTask = _context.AnimatePreviewOutAsync();
            await Task.WhenAll(audioFadeOutTask, previewFadeOutTask);
            try
            {
                await viewModel.StopPreviewAsync(userInitiated: true);
            }
            finally
            {
                _context.ClearPreviewReinitAnimation(operationName);
                _context.ResetPreviewContentTransform();
            }

            return;
        }

        _context.SetPreviewStopRequestedByUser(false);
        await viewModel.StartPreviewAsync(userInitiated: true);
        if (!viewModel.IsPreviewing)
        {
            _context.RevealPreviewUnavailablePlaceholder();
        }
    }
}

internal sealed class PreviewButtonPresentationControllerContext
{
    public required Button PreviewButton { get; init; }
    public required FontIcon PreviewButtonIcon { get; init; }
}

internal sealed class PreviewButtonPresentationController
{
    private const string StopPreviewGlyph = "\uE71A";
    private const string StartPreviewGlyph = "\uE768";

    private readonly PreviewButtonPresentationControllerContext _context;

    public PreviewButtonPresentationController(PreviewButtonPresentationControllerContext context)
    {
        _context = context;
    }

    public void ShowStopPreview()
    {
        _context.PreviewButtonIcon.Glyph = StopPreviewGlyph;
        ToolTipService.SetToolTip(_context.PreviewButton, "Stop Preview");
    }

    public void ShowStartPreview()
    {
        _context.PreviewButtonIcon.Glyph = StartPreviewGlyph;
        ToolTipService.SetToolTip(_context.PreviewButton, "Start Preview");
    }
}

internal sealed class PreviewFadeInControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required Func<D3D11PreviewRenderer?> GetRenderer { get; init; }
    public required Func<Task> AnimatePreviewInAsync { get; init; }
    public required Action StartPreviewAudioFadeIn { get; init; }
}

internal sealed class PreviewFadeInController
{
    private const int PreviewFadeInFrameThreshold = 3;

    private readonly PreviewFadeInControllerContext _context;
    private DispatcherQueueTimer? _timer;

    public PreviewFadeInController(PreviewFadeInControllerContext context)
    {
        _context = context;
    }

    public void Schedule()
    {
        Stop();

        var renderer = _context.GetRenderer();
        if (renderer == null)
        {
            _timer = _context.DispatcherQueue.CreateTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(50);
            _timer.IsRepeating = false;
            _timer.Tick += (_, _) =>
            {
                Stop();
                _ = _context.AnimatePreviewInAsync();
                _context.StartPreviewAudioFadeIn();
            };
            _timer.Start();
            return;
        }

        var baselineFrames = renderer.FramesRendered;
        _timer = _context.DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.IsRepeating = true;
        _timer.Tick += (_, _) =>
        {
            var current = _context.GetRenderer();
            if (current == null || current != renderer)
            {
                Stop();
                _ = _context.AnimatePreviewInAsync();
                _context.StartPreviewAudioFadeIn();
                return;
            }

            var rendered = current.FramesRendered - baselineFrames;
            if (rendered >= PreviewFadeInFrameThreshold)
            {
                Stop();
                Logger.Log($"PREVIEW_FADE_IN_READY framesRendered={rendered} baseline={baselineFrames}");
                _ = _context.AnimatePreviewInAsync();
                _context.StartPreviewAudioFadeIn();
            }
        };
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }
}
