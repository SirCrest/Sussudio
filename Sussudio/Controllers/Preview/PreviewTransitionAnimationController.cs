using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Sussudio.Controllers;

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
