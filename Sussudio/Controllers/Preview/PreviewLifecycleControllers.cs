using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Sussudio.Services.Preview;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class PreviewAudioFadeControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required PreviewAudioVolumeTransitionController VolumeController { get; init; }
}

// WinUI supplies the existing easing/timing; the volume owner admits each frame.
internal sealed class PreviewAudioFadeController
{
    private readonly PreviewAudioFadeControllerContext _context;
    private PreviewAudioVolumeOperation _operation;
    private bool _isFadingIn;
    private FadeRun? _activeFade;

    public PreviewAudioFadeController(PreviewAudioFadeControllerContext context)
    {
        _context = context;
    }

    public bool IsFadingIn => _isFadingIn;
    public bool IsAnimationActive => _activeFade != null;

    public void PrimeFadeIn()
    {
        StopActiveFade();
        _operation = _context.VolumeController.PrimeForAudioTransition("preview_start");
        // Even a zero target can be changed by the user before first-frame readiness.
        _isFadingIn = _operation.Generation != 0;
        Sussudio.Logger.Log($"PREVIEW_AUDIO_FADE_PRIMED targetPct={_context.VolumeController.RequestedVolume * 100:0}");
    }

    public void StartFadeIn(int durationMs = 900)
    {
        if (!_isFadingIn) return;
        _ = StartStoryboardAsync(_operation, muteOutput: false, durationMs);
    }

    public Task StartFadeOutAsync(int durationMs = 450)
    {
        StopActiveFade();
        _isFadingIn = false;
        _operation = _context.VolumeController.BeginTransition("preview_stop");
        return StartStoryboardAsync(_operation, muteOutput: true, durationMs);
    }

    private Task StartStoryboardAsync(PreviewAudioVolumeOperation operation, bool muteOutput, int durationMs)
    {
        StopActiveFade();
        var candidate = _context.VolumeController.BeginWriter(operation, muteOutput);
        if (candidate is not { } writer)
        {
            _isFadingIn = false;
            return Task.CompletedTask;
        }
        if (Math.Abs(writer.StartingVolume - writer.TargetVolume) <= 0.001)
        {
            _context.VolumeController.TryCompleteWriter(writer);
            _isFadingIn = false;
            return Task.CompletedTask;
        }

        var animationValue = new PreviewAudioAnimationValue();
        var volumeAnimation = new DoubleAnimation
        {
            From = writer.StartingVolume,
            To = writer.TargetVolume,
            Duration = TimeSpan.FromMilliseconds(durationMs),
            EasingFunction = new CubicEase { EasingMode = muteOutput ? EasingMode.EaseIn : EasingMode.EaseOut },
            EnableDependentAnimation = true,
        };
        Storyboard.SetTarget(volumeAnimation, animationValue);
        Storyboard.SetTargetProperty(volumeAnimation, nameof(PreviewAudioAnimationValue.Value));
        var storyboard = new Storyboard();
        storyboard.Children.Add(volumeAnimation);
        var run = new FadeRun(storyboard, animationValue, writer);
        animationValue.ValueChanged = value => _context.VolumeController.TryApplyTransient(writer, value);
        run.CompletedHandler = (_, _) => Finish(run, completed: true);
        storyboard.Completed += run.CompletedHandler;
        _activeFade = run;
        run.Cancellation = writer.CancellationToken.Register(() =>
        {
            if (_context.DispatcherQueue.HasThreadAccess)
            {
                Finish(run, completed: false);
            }
            else if (!_context.DispatcherQueue.TryEnqueue(() => Finish(run, completed: false)))
            {
                // The window is gone. Its revoked lease rejects late frames, and
                // a stop operation must not wait forever for a dead dispatcher.
                run.Completion.TrySetResult(true);
            }
        });
        if (!run.Finished)
        {
            Sussudio.Logger.Log(muteOutput
                ? $"PREVIEW_AUDIO_FADE_OUT_STARTED fromPct={writer.StartingVolume * 100:0} durationMs={durationMs}"
                : $"PREVIEW_AUDIO_FADE_IN_STARTED targetPct={writer.TargetVolume * 100:0} durationMs={durationMs}");
            try
            {
                storyboard.Begin();
            }
            catch (Exception ex)
            {
                Sussudio.Logger.LogException(ex);
                // A failed visual animation must still settle its target and
                // release the stop caller to finish backend teardown.
                Finish(run, completed: true);
            }
        }
        else run.Cancellation.Dispose();
        return run.Completion.Task;
    }

    private void StopActiveFade()
    {
        if (_activeFade is { } run) Finish(run, completed: false);
    }

    private void Finish(FadeRun run, bool completed)
    {
        if (run.Finished) return;
        run.Finished = true;
        run.Storyboard.Completed -= run.CompletedHandler;
        run.AnimationValue.ValueChanged = null;
        try
        {
            if (completed) _context.VolumeController.TryCompleteWriter(run.Writer);
            else _context.VolumeController.EndWriter(run.Writer);
        }
        catch (Exception ex)
        {
            Sussudio.Logger.LogException(ex);
        }
        finally
        {
            try { run.Storyboard.Stop(); }
            catch (Exception ex) { Sussudio.Logger.LogException(ex); }
            run.Cancellation.Dispose();
            if (ReferenceEquals(_activeFade, run))
            {
                _activeFade = null;
                _isFadingIn = false;
            }
            // Superseding a visual fade does not cancel the requested backend stop.
            run.Completion.TrySetResult(true);
        }
        if (completed && run.Writer.MutesOutput) Sussudio.Logger.Log("PREVIEW_AUDIO_FADE_OUT_COMPLETED");
    }

    private sealed class FadeRun(Storyboard storyboard, PreviewAudioAnimationValue animationValue, PreviewAudioVolumeWriter writer)
    {
        public Storyboard Storyboard { get; } = storyboard;
        public PreviewAudioAnimationValue AnimationValue { get; } = animationValue;
        public PreviewAudioVolumeWriter Writer { get; } = writer;
        public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public EventHandler<object>? CompletedHandler;
        public CancellationTokenRegistration Cancellation;
        public bool Finished;
    }

    private sealed class PreviewAudioAnimationValue : DependencyObject
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            nameof(Value), typeof(double), typeof(PreviewAudioAnimationValue),
            new PropertyMetadata(0.0, (sender, args) =>
                ((PreviewAudioAnimationValue)sender).ValueChanged?.Invoke((double)args.NewValue)));

        public Action<double>? ValueChanged { get; set; }
        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
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
        try
        {
            await viewModel.StartPreviewAsync(userInitiated: true);
        }
        finally
        {
            if (!viewModel.IsPreviewing)
            {
                _context.RevealPreviewUnavailablePlaceholder();
            }
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

internal sealed class PreviewTransitionAnimationControllerContext
{
    public required UIElement PreviewBorder { get; init; }
    public required ScaleTransform PreviewBorderScale { get; init; }
    public required UIElement PreviewContentGrid { get; init; }
    public required ScaleTransform PreviewContentScale { get; init; }
    public required UIElement NoDevicePlaceholder { get; init; }
    public required Action StopPreviewFadeInTimer { get; init; }
    public required Action StartPreviewStartupOverlay { get; init; }
    public required Action StopPreviewStartupOverlay { get; init; }
    public required Action<int> FadeOutVideoFrameShadow { get; init; }
    public required Action<int, int> FadeInVideoFrameShadow { get; init; }
    public required Func<bool> GetIsFlashbackEnabled { get; init; }
    public required Action<bool> UpdateFlashbackKeepAliveHint { get; init; }
}

internal sealed class PreviewTransitionAnimationController
{
    private readonly PreviewTransitionAnimationControllerContext _context;

    public PreviewTransitionAnimationController(PreviewTransitionAnimationControllerContext context)
    {
        _context = context;
    }

    public void AddPreviewShellEntranceAnimations(Storyboard storyboard, EasingFunctionBase easing, int beginMs, int durationMs)
    {
        var beginTime = TimeSpan.FromMilliseconds(beginMs);
        var duration = TimeSpan.FromMilliseconds(durationMs);

        var previewFade = new DoubleAnimation
        {
            To = 1,
            BeginTime = beginTime,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(previewFade, _context.PreviewBorder);
        Storyboard.SetTargetProperty(previewFade, "Opacity");
        storyboard.Children.Add(previewFade);

        var previewScaleX = new DoubleAnimation
        {
            To = 1.0,
            BeginTime = beginTime,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(previewScaleX, _context.PreviewBorderScale);
        Storyboard.SetTargetProperty(previewScaleX, "ScaleX");
        storyboard.Children.Add(previewScaleX);

        var previewScaleY = new DoubleAnimation
        {
            To = 1.0,
            BeginTime = beginTime,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(previewScaleY, _context.PreviewBorderScale);
        Storyboard.SetTargetProperty(previewScaleY, "ScaleY");
        storyboard.Children.Add(previewScaleY);
    }

    public void ResetPreviewContentTransform()
    {
        _context.PreviewContentGrid.Opacity = 1.0;
        _context.PreviewContentScale.ScaleX = 1.0;
        _context.PreviewContentScale.ScaleY = 1.0;
    }

    public Task AnimatePreviewOutAsync()
    {
        _context.FadeOutVideoFrameShadow(150);
        return AnimatePreviewTransitionAsync(0.0, 0.97, 200, EasingMode.EaseIn);
    }

    public Task AnimatePreviewInAsync()
    {
        _context.FadeInVideoFrameShadow(0, 400);
        return Task.WhenAll(
            AnimatePreviewShellInAsync(350),
            AnimatePreviewTransitionAsync(1.0, 1.0, 250, EasingMode.EaseOut));
    }

    public Task AnimatePreviewShellInAsync(int durationMs)
    {
        if (_context.PreviewBorder.Opacity >= 0.999 &&
            Math.Abs(_context.PreviewBorderScale.ScaleX - 1.0) < 0.001 &&
            Math.Abs(_context.PreviewBorderScale.ScaleY - 1.0) < 0.001)
        {
            return Task.CompletedTask;
        }

        var storyboard = new Storyboard();
        AddPreviewShellEntranceAnimations(
            storyboard,
            new CubicEase { EasingMode = EasingMode.EaseOut },
            beginMs: 0,
            durationMs: durationMs);
        return BeginStoryboardAsync(storyboard);
    }

    public void PrepareStartupPresentation()
    {
        _context.StopPreviewFadeInTimer();
        FadeOutElement(_context.NoDevicePlaceholder);
        _context.StartPreviewStartupOverlay();
        _context.PreviewContentGrid.Opacity = 0.0;
        _context.PreviewContentScale.ScaleX = 0.97;
        _context.PreviewContentScale.ScaleY = 0.97;
    }

    public void RevealUnavailablePlaceholder()
    {
        _context.StopPreviewStartupOverlay();
        _context.StopPreviewFadeInTimer();
        ResetPreviewContentTransform();
        _ = AnimatePreviewShellInAsync(300);
        _context.UpdateFlashbackKeepAliveHint(_context.GetIsFlashbackEnabled());
        FadeInElement(_context.NoDevicePlaceholder);
    }

    public static void FadeOutElement(UIElement element)
    {
        var animation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200))
        };
        var storyboard = new Storyboard();
        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, "Opacity");
        storyboard.Completed += (_, _) =>
        {
            element.Visibility = Visibility.Collapsed;
            element.Opacity = 1.0;
        };
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    public static void FadeInElement(UIElement element)
    {
        element.Opacity = 0.0;
        element.Visibility = Visibility.Visible;
        var animation = new DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(200))
        };
        var storyboard = new Storyboard();
        Storyboard.SetTarget(animation, element);
        Storyboard.SetTargetProperty(animation, "Opacity");
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private Task AnimatePreviewTransitionAsync(double opacityTarget, double scaleTarget, int durationMs, EasingMode easingMode)
    {
        var duration = TimeSpan.FromMilliseconds(durationMs);
        var easing = new CubicEase { EasingMode = easingMode };

        var fade = new DoubleAnimation { To = opacityTarget, Duration = new Duration(duration), EasingFunction = easing };
        Storyboard.SetTarget(fade, _context.PreviewContentGrid);
        Storyboard.SetTargetProperty(fade, "Opacity");

        var scaleX = new DoubleAnimation { To = scaleTarget, Duration = new Duration(duration), EasingFunction = easing };
        Storyboard.SetTarget(scaleX, _context.PreviewContentScale);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");

        var scaleY = new DoubleAnimation { To = scaleTarget, Duration = new Duration(duration), EasingFunction = easing };
        Storyboard.SetTarget(scaleY, _context.PreviewContentScale);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");

        var storyboard = new Storyboard();
        storyboard.Children.Add(fade);
        storyboard.Children.Add(scaleX);
        storyboard.Children.Add(scaleY);
        return BeginStoryboardAsync(storyboard);
    }

    private static Task BeginStoryboardAsync(Storyboard storyboard)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        storyboard.Completed += (_, _) => tcs.TrySetResult(true);
        storyboard.Begin();
        return tcs.Task;
    }
}

internal sealed class PreviewStartupOverlayControllerContext
{
    public required Panel PreviewLoadingOverlay { get; init; }
}

internal sealed class PreviewStartupOverlayController
{
    private readonly PreviewStartupOverlayControllerContext _context;

    public PreviewStartupOverlayController(PreviewStartupOverlayControllerContext context)
    {
        _context = context;
    }

    public void Start()
    {
        var ring = (ProgressRing)_context.PreviewLoadingOverlay.Children[0];
        ring.IsActive = true;
        PreviewTransitionAnimationController.FadeInElement(_context.PreviewLoadingOverlay);
    }

    public void Stop(bool isPreviewReinitAnimating)
    {
        if (_context.PreviewLoadingOverlay.Visibility == Visibility.Collapsed)
        {
            return;
        }

        var ring = (ProgressRing)_context.PreviewLoadingOverlay.Children[0];
        ring.IsActive = false;
        if (isPreviewReinitAnimating)
        {
            _context.PreviewLoadingOverlay.Visibility = Visibility.Collapsed;
            _context.PreviewLoadingOverlay.Opacity = 1.0;
            return;
        }

        PreviewTransitionAnimationController.FadeOutElement(_context.PreviewLoadingOverlay);
    }
}

internal enum PreviewReinitCompletionPresentation
{
    None,
    RevealUnavailablePlaceholder,
    ResetConfirmedVisual,
    ShowStartPreviewButton
}

internal sealed class PreviewReinitCompletionPresentationContext
{
    public required bool IsPreviewReinitializing { get; init; }

    public required bool IsPreviewing { get; init; }

    public required bool IsFirstVisualConfirmed { get; init; }

    public required string AttemptLabel { get; init; }

    public required string CallerName { get; init; }

    public required Action UpdateDeviceApplyButtonState { get; init; }

    public required Action RevealUnavailablePlaceholder { get; init; }

    public required Action StopPreviewStartupOverlay { get; init; }

    public required Action ResetPreviewContentTransform { get; init; }

    public required Action ShowStartPreviewButtonPresentation { get; init; }
}

internal sealed class PreviewReinitTransitionController
{
    public bool IsAnimating { get; private set; }

    public void BeginAnimateOut(string reason, string callerName)
    {
        IsAnimating = true;
        Logger.Log($"D3D11_RENDERER_REINIT_FLAG flag=true caller={callerName}");
        Logger.Log($"PREVIEW_REINIT_ANIMATE_OUT reason={reason}");
    }

    public PreviewReinitCompletionPresentation GetCompletionPresentation(
        bool isPreviewReinitializing,
        bool isPreviewing,
        bool isFirstVisualConfirmed)
    {
        if (!isPreviewReinitializing && IsAnimating)
        {
            if (!isPreviewing)
            {
                return PreviewReinitCompletionPresentation.RevealUnavailablePlaceholder;
            }

            if (isFirstVisualConfirmed)
            {
                return PreviewReinitCompletionPresentation.ResetConfirmedVisual;
            }
        }
        else if (!isPreviewReinitializing && !isPreviewing)
        {
            return PreviewReinitCompletionPresentation.ShowStartPreviewButton;
        }

        return PreviewReinitCompletionPresentation.None;
    }

    public void HandleReinitializingChanged(PreviewReinitCompletionPresentationContext context)
    {
        context.UpdateDeviceApplyButtonState();
        switch (GetCompletionPresentation(
            context.IsPreviewReinitializing,
            context.IsPreviewing,
            context.IsFirstVisualConfirmed))
        {
            case PreviewReinitCompletionPresentation.RevealUnavailablePlaceholder:
                Clear(context.CallerName, logWhenInactive: false);
                context.RevealUnavailablePlaceholder();
                break;

            case PreviewReinitCompletionPresentation.ResetConfirmedVisual:
                ResetConfirmedVisualTransition(
                    context.AttemptLabel,
                    "reinit-stop-failed",
                    context.CallerName);
                context.StopPreviewStartupOverlay();
                context.ResetPreviewContentTransform();
                break;

            case PreviewReinitCompletionPresentation.ShowStartPreviewButton:
                context.ShowStartPreviewButtonPresentation();
                break;
        }
    }

    public void CompleteFirstVisualTransition(string attemptLabel, string callerName)
    {
        if (!IsAnimating)
        {
            return;
        }

        Logger.Log($"PREVIEW_REINIT_ANIMATE_IN attempt={attemptLabel}");
        Clear(callerName, logWhenInactive: false);
    }

    public void ResetConfirmedVisualTransition(string attemptLabel, string reason, string callerName)
    {
        Logger.Log($"PREVIEW_REINIT_ANIMATE_RESET attempt={attemptLabel} reason={reason}");
        Clear(callerName, logWhenInactive: false);
    }

    public void ClearForStartupReset(bool preserveReinitAnimation, string callerName)
    {
        if (preserveReinitAnimation)
        {
            return;
        }

        Clear(callerName);
    }

    public void Clear(string callerName, bool logWhenInactive = true, string? operationName = null)
    {
        if (!IsAnimating && !logWhenInactive)
        {
            return;
        }

        IsAnimating = false;
        var message = $"D3D11_RENDERER_REINIT_FLAG flag=false caller={callerName}";
        if (operationName is null)
        {
            Logger.Log(message);
        }
        else
        {
            Logger.Log(message, operationName);
        }
    }
}

internal sealed class PreviewSurfacePresentationControllerContext
{
    public required Func<SwapChainPanel> GetPreviewSwapChainPanel { get; init; }
    public required FrameworkElement PreviewContentGrid { get; init; }
    public required FrameworkElement RecordingGlowBorder { get; init; }
}

internal sealed class PreviewSurfacePresentationController
{
    private readonly PreviewSurfacePresentationControllerContext _context;
    private readonly PreviewSurfaceShadowController _shadowController;

    public PreviewSurfacePresentationController(
        PreviewSurfacePresentationControllerContext context,
        PreviewSurfaceShadowController shadowController)
    {
        _context = context;
        _shadowController = shadowController;
    }

    public void UpdateVideoContentOverlays(int? sourceWidth, int? sourceHeight)
    {
        var srcW = (double)(sourceWidth ?? 0);
        var srcH = (double)(sourceHeight ?? 0);
        // Use the container size, not the SwapChainPanel, because the panel is
        // explicitly sized to the fitted video rect.
        var dstW = _context.PreviewContentGrid.ActualWidth;
        var dstH = _context.PreviewContentGrid.ActualHeight;

        if (dstW <= 0 || dstH <= 0)
        {
            _context.RecordingGlowBorder.Margin = new Thickness(0);
            _shadowController.ClearVideoFrameBounds();

            return;
        }

        double fitW, fitH;
        if (srcW <= 0 || srcH <= 0)
        {
            // Source dimensions unknown - fill the container (same as old Stretch behavior).
            fitW = dstW;
            fitH = dstH;
        }
        else
        {
            var srcAspect = srcW / srcH;
            var dstAspect = dstW / dstH;

            if (srcAspect > dstAspect)
            {
                fitW = dstW;
                fitH = dstW / srcAspect;
            }
            else
            {
                fitH = dstH;
                fitW = dstH * srcAspect;
            }
        }

        // Resize SwapChainPanel to exactly the video content area (no letterbox).
        var previewSwapChainPanel = _context.GetPreviewSwapChainPanel();
        previewSwapChainPanel.Width = fitW;
        previewSwapChainPanel.Height = fitH;

        var marginH = (dstW - fitW) / 2;
        var marginV = (dstH - fitH) / 2;
        var videoMargin = new Thickness(marginH, marginV, marginH, marginV);
        _context.RecordingGlowBorder.Margin = videoMargin;

        _shadowController.UpdateVideoFrameBounds(marginH, marginV, fitW, fitH);
    }

    public void SetGpuPreviewVisibility(Visibility visibility)
    {
        // PreviewLetterboxBackground stays Collapsed - letterbox areas must be
        // transparent so the Composition DropShadow is visible around the video.
        _context.GetPreviewSwapChainPanel().Visibility = visibility;
    }
}

internal sealed class PreviewSurfaceShadowControllerContext
{
    public required UIElement VideoShadowHost { get; init; }
    public required UIElement ControlBarShadowHost { get; init; }
    public required FrameworkElement ControlBarBorder { get; init; }
}

internal sealed class PreviewSurfaceShadowController
{
    private readonly PreviewSurfaceShadowControllerContext _context;
    private SpriteVisual? _videoShadowVisual;
    private SpriteVisual? _controlBarShadowVisual;

    public PreviewSurfaceShadowController(PreviewSurfaceShadowControllerContext context)
    {
        _context = context;
    }

    public void UpdateVideoFrameBounds(double marginH, double marginV, double fitW, double fitH)
    {
        if (_videoShadowVisual == null)
        {
            return;
        }

        const float borderMarginH = 12f; // PreviewBorder Margin left/right
        const float borderMarginV = 6f;  // PreviewBorder Margin top/bottom
        const float hostMargin = 16f;    // PreviewShadowHost Margin
        _videoShadowVisual.Offset = new Vector3(
            borderMarginH + hostMargin + (float)marginH,
            borderMarginV + hostMargin + (float)marginV,
            0);
        _videoShadowVisual.Size = new Vector2(Math.Max(0, (float)fitW), Math.Max(0, (float)fitH));
    }

    public void ClearVideoFrameBounds()
    {
        if (_videoShadowVisual != null)
        {
            _videoShadowVisual.Size = Vector2.Zero;
        }
    }

    public void SetupVideoFrameShadow()
    {
        var compositor = ElementCompositionPreview.GetElementVisual(_context.VideoShadowHost).Compositor;

        var shadow = compositor.CreateDropShadow();
        shadow.BlurRadius = 16;
        shadow.Color = Windows.UI.Color.FromArgb(160, 0, 0, 0);
        shadow.Offset = new Vector3(0, 2, 0);
        shadow.Mask = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));

        var spriteVisual = compositor.CreateSpriteVisual();
        spriteVisual.Shadow = shadow;

        spriteVisual.Opacity = 0f; // Start invisible - faded in with preview entrance.
        _videoShadowVisual = spriteVisual;
        ElementCompositionPreview.SetElementChildVisual(_context.VideoShadowHost, spriteVisual);
    }

    public void SetupControlBarShadow()
    {
        var compositor = ElementCompositionPreview.GetElementVisual(_context.ControlBarShadowHost).Compositor;

        var shadow = compositor.CreateDropShadow();
        shadow.BlurRadius = 12;
        shadow.Color = Windows.UI.Color.FromArgb(120, 0, 0, 0);
        shadow.Offset = new Vector3(0, 1, 0);
        shadow.Mask = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0));

        var spriteVisual = compositor.CreateSpriteVisual();
        spriteVisual.Shadow = shadow;
        spriteVisual.Opacity = 0f; // Start invisible - faded in with control bar entrance.

        _controlBarShadowVisual = spriteVisual;
        ElementCompositionPreview.SetElementChildVisual(_context.ControlBarShadowHost, spriteVisual);

        // Track control bar size changes to keep the shadow aligned.
        _context.ControlBarBorder.SizeChanged += (_, e) =>
        {
            if (_controlBarShadowVisual == null)
            {
                return;
            }

            var margin = _context.ControlBarBorder.Margin;
            _controlBarShadowVisual.Offset = new Vector3((float)margin.Left, (float)margin.Top, 0);
            _controlBarShadowVisual.Size = new Vector2((float)e.NewSize.Width, (float)e.NewSize.Height);
        };
    }

    public void ClearVideoFrameShadow()
    {
        _videoShadowVisual = null;
        ElementCompositionPreview.SetElementChildVisual(_context.VideoShadowHost, null);
    }

    public void FadeInVideoFrameShadow(int delayMs, int durationMs)
        => FadeIn(_videoShadowVisual, delayMs, durationMs);

    public void FadeOutVideoFrameShadow(int durationMs)
        => FadeOut(_videoShadowVisual, durationMs);

    public void FadeInControlBarShadow(int delayMs, int durationMs)
        => FadeIn(_controlBarShadowVisual, delayMs, durationMs);

    private static void FadeIn(SpriteVisual? visual, int delayMs, int durationMs)
    {
        if (visual == null)
        {
            return;
        }

        var compositor = visual.Compositor;
        var animation = compositor.CreateScalarKeyFrameAnimation();
        animation.InsertKeyFrame(0f, 0f);
        animation.InsertKeyFrame(
            1f,
            1f,
            compositor.CreateCubicBezierEasingFunction(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f)));
        animation.Duration = TimeSpan.FromMilliseconds(durationMs);
        animation.DelayTime = TimeSpan.FromMilliseconds(delayMs);
        visual.StartAnimation("Opacity", animation);
    }

    private static void FadeOut(SpriteVisual? visual, int durationMs)
    {
        if (visual == null)
        {
            return;
        }

        var compositor = visual.Compositor;
        var animation = compositor.CreateScalarKeyFrameAnimation();
        animation.InsertKeyFrame(
            1f,
            0f,
            compositor.CreateCubicBezierEasingFunction(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f)));
        animation.Duration = TimeSpan.FromMilliseconds(durationMs);
        visual.StartAnimation("Opacity", animation);
    }
}
