using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Sussudio.Controllers;

internal sealed class RecordingButtonChromeControllerContext
{
    public required Border RecordingGlowBorder { get; init; }
    public required Storyboard RecordingGlowPulseStoryboard { get; init; }
    public required Storyboard RecPulseStoryboard { get; init; }
    public required Button RecordButton { get; init; }
    public required UIElement RecordButtonNormalContent { get; init; }
    public required ProgressRing RecordButtonStartingContent { get; init; }
    public required UIElement RecordButtonRecordingContent { get; init; }
}

internal sealed class RecordingButtonChromeController
{
    private const double CollapsedRecordButtonWidth = 36;
    private readonly RecordingButtonChromeControllerContext _context;

    public RecordingButtonChromeController(RecordingButtonChromeControllerContext context)
    {
        _context = context;
    }

    public void ApplyRecordingGlow(bool isRecording)
    {
        if (isRecording)
        {
            _context.RecordingGlowBorder.Opacity = 1.0;
            _context.RecordingGlowPulseStoryboard.Begin();
        }
        else
        {
            _context.RecordingGlowPulseStoryboard.Stop();
            _context.RecordingGlowBorder.Opacity = 0;
        }
    }

    public void ApplyRecordingButtonState(bool isRecording)
    {
        _context.RecordButtonStartingContent.IsActive = false;
        _context.RecordButtonStartingContent.Visibility = Visibility.Collapsed;
        if (isRecording)
        {
            _context.RecordButtonNormalContent.Visibility = Visibility.Collapsed;
            _context.RecordButtonRecordingContent.Visibility = Visibility.Visible;
            _context.RecordButton.Padding = new Thickness(12, 0, 12, 0);
            _context.RecordButton.Width = double.NaN;
            _context.RecordButton.UpdateLayout();
            var targetWidth = _context.RecordButton.ActualWidth;
            _context.RecordButton.Width = CollapsedRecordButtonWidth;
            AnimateWidth(CollapsedRecordButtonWidth, targetWidth, null);
        }
        else
        {
            var currentWidth = _context.RecordButton.ActualWidth;
            _context.RecordButton.Width = currentWidth;
            AnimateWidth(currentWidth, CollapsedRecordButtonWidth, () =>
            {
                _context.RecordButtonRecordingContent.Visibility = Visibility.Collapsed;
                _context.RecordButtonNormalContent.Visibility = Visibility.Visible;
                _context.RecordButton.Padding = new Thickness(0);
            });
        }
    }

    public void ApplyRecordingPulse(bool isRecording)
    {
        if (isRecording)
        {
            _context.RecPulseStoryboard.Begin();
        }
        else
        {
            _context.RecPulseStoryboard.Stop();
        }
    }

    public void ApplyTransitioningState(bool isRecording, RecordingStatePresentationState state)
    {
        _context.RecordButton.IsEnabled = state.TransitionRecordButtonEnabled;
        if (state.TransitionStartingContentActive)
        {
            if (isRecording)
            {
                _context.RecordButton.Width = _context.RecordButton.ActualWidth;
                _context.RecordButtonRecordingContent.Visibility = Visibility.Collapsed;
            }
            else
            {
                _context.RecordButtonNormalContent.Visibility = Visibility.Collapsed;
            }

            _context.RecordButtonStartingContent.IsActive = state.TransitionStartingContentActive;
            _context.RecordButtonStartingContent.Visibility = Visibility.Visible;
        }
        else
        {
            _context.RecordButtonStartingContent.IsActive = false;
            _context.RecordButtonStartingContent.Visibility = Visibility.Collapsed;
            _context.RecordButtonNormalContent.Visibility = ToVisibility(state.SettledNormalContentVisible);
            _context.RecordButtonRecordingContent.Visibility = ToVisibility(state.SettledRecordingContentVisible);
        }
    }

    public void ApplyFfmpegMissingState(RecordingStatePresentationState state)
    {
        _context.RecordButton.IsEnabled = state.FfmpegRecordButtonEnabled;
    }

    private void AnimateWidth(double from, double to, Action? onCompleted = null)
    {
        var anim = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(anim, _context.RecordButton);
        Storyboard.SetTargetProperty(anim, "Width");

        var storyboard = new Storyboard();
        storyboard.Children.Add(anim);
        storyboard.Completed += (_, _) =>
        {
            _context.RecordButton.Width = to == CollapsedRecordButtonWidth ? CollapsedRecordButtonWidth : double.NaN;
            onCompleted?.Invoke();
        };
        storyboard.Begin();
    }

    private static Visibility ToVisibility(bool isVisible)
        => isVisible ? Visibility.Visible : Visibility.Collapsed;
}
