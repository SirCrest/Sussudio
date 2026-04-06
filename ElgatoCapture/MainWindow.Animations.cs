using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using ElgatoCapture.Services;
using ElgatoCapture.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using System.Numerics;
using WinRT.Interop;

namespace ElgatoCapture;

public sealed partial class MainWindow
{
    private FrameworkElement[] GetControlBarButtons() => new FrameworkElement[]
    {
        SettingsToggleButton,
        OpenRecordingsButton,
        ScreenshotButton,
        RecordButton,
        PreviewButton,
        HdrToggle,
        AudioRecordToggle,
        TrueHdrPreviewToggle,
        AudioPreviewToggle,
        StatsToggle
    };
    private void SetupButtonHoverAnimations()
    {
        foreach (var button in GetControlBarButtons())
        {
            var isHovered = false;
            button.RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
            button.RenderTransform = new ScaleTransform { ScaleX = 1, ScaleY = 1 };

            button.PointerEntered += (_, _) =>
            {
                isHovered = true;
                if (button.RenderTransform is ScaleTransform transform)
                {
                    AnimateScale(transform, 1.08, TimeSpan.FromMilliseconds(100));
                }
            };

            button.PointerExited += (_, _) =>
            {
                isHovered = false;
                if (button.RenderTransform is ScaleTransform transform)
                {
                    AnimateScale(transform, 1.0, TimeSpan.FromMilliseconds(100));
                }
            };

            button.PointerPressed += (_, _) =>
            {
                if (button.RenderTransform is ScaleTransform transform)
                {
                    AnimateScale(transform, 0.95, TimeSpan.FromMilliseconds(60));
                }
            };

            button.PointerReleased += (_, _) =>
            {
                if (button.RenderTransform is ScaleTransform transform)
                {
                    AnimateScale(transform, isHovered ? 1.08 : 1.0, TimeSpan.FromMilliseconds(60));
                }
            };
        }
    }
    private FrameworkElement[] GetEntranceButtons() => GetControlBarButtons();
    private void PlaySplashAndEntrance()
    {
        if (_entranceAnimationPlayed) return;
        _entranceAnimationPlayed = true;

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var easingIn = new CubicEase { EasingMode = EasingMode.EaseIn };

        // Phase 1: Splash holds for 600ms, then scales down + fades out over 400ms
        var splashFade = new DoubleAnimation
        {
            From = 1, To = 0,
            BeginTime = TimeSpan.FromMilliseconds(600),
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = easingIn
        };
        Storyboard.SetTarget(splashFade, SplashOverlay);
        Storyboard.SetTargetProperty(splashFade, "Opacity");

        var splashScaleX = new DoubleAnimation
        {
            From = 1.0, To = 0.95,
            BeginTime = TimeSpan.FromMilliseconds(600),
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = easingIn,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(splashScaleX, SplashScale);
        Storyboard.SetTargetProperty(splashScaleX, "ScaleX");

        var splashScaleY = new DoubleAnimation
        {
            From = 1.0, To = 0.95,
            BeginTime = TimeSpan.FromMilliseconds(600),
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = easingIn,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(splashScaleY, SplashScale);
        Storyboard.SetTargetProperty(splashScaleY, "ScaleY");

        var splashSb = new Storyboard();
        splashSb.Children.Add(splashFade);
        splashSb.Children.Add(splashScaleX);
        splashSb.Children.Add(splashScaleY);
        splashSb.Completed += (_, _) =>
        {
            SplashOverlay.Visibility = Visibility.Collapsed;
            PlayEntranceAnimation();
        };
        splashSb.Begin();
    }
    private void PlayEntranceAnimation()
    {
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var storyboard = new Storyboard();

        // 1. Control bar: slide up 20px + fade in (0ms, 350ms)
        var barFade = new DoubleAnimation
        {
            From = 0, To = 1,
            Duration = TimeSpan.FromMilliseconds(350),
            EasingFunction = easing
        };
        Storyboard.SetTarget(barFade, ControlBarBorder);
        Storyboard.SetTargetProperty(barFade, "Opacity");
        storyboard.Children.Add(barFade);

        var barSlide = new DoubleAnimation
        {
            From = 20, To = 0,
            Duration = TimeSpan.FromMilliseconds(350),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(barSlide, (TranslateTransform)ControlBarBorder.RenderTransform);
        Storyboard.SetTargetProperty(barSlide, "Y");
        storyboard.Children.Add(barSlide);

        // 2. Buttons stagger: 50ms offset, 200ms each (starting at 150ms)
        var buttons = GetEntranceButtons();
        for (var i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            var beginTime = TimeSpan.FromMilliseconds(150 + (i * 50));
            var duration = TimeSpan.FromMilliseconds(200);

            var buttonFade = new DoubleAnimation
            {
                From = 0, To = 1,
                BeginTime = beginTime, Duration = duration,
                EasingFunction = easing
            };
            Storyboard.SetTarget(buttonFade, button);
            Storyboard.SetTargetProperty(buttonFade, "Opacity");
            storyboard.Children.Add(buttonFade);

            if (button.RenderTransform is ScaleTransform transform)
            {
                var scaleX = new DoubleAnimation
                {
                    From = 0.85, To = 1.0,
                    BeginTime = beginTime, Duration = duration,
                    EasingFunction = easing, EnableDependentAnimation = true
                };
                Storyboard.SetTarget(scaleX, transform);
                Storyboard.SetTargetProperty(scaleX, "ScaleX");
                storyboard.Children.Add(scaleX);

                var scaleY = new DoubleAnimation
                {
                    From = 0.85, To = 1.0,
                    BeginTime = beginTime, Duration = duration,
                    EasingFunction = easing, EnableDependentAnimation = true
                };
                Storyboard.SetTarget(scaleY, transform);
                Storyboard.SetTargetProperty(scaleY, "ScaleY");
                storyboard.Children.Add(scaleY);
            }
        }

        // 3. Stats row: slide down 10px + fade in (600ms begin, 300ms duration)
        var statsFade = new DoubleAnimation
        {
            From = 0, To = 1,
            BeginTime = TimeSpan.FromMilliseconds(600),
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = easing
        };
        Storyboard.SetTarget(statsFade, StatsRow);
        Storyboard.SetTargetProperty(statsFade, "Opacity");
        storyboard.Children.Add(statsFade);

        var statsSlide = new DoubleAnimation
        {
            From = -10, To = 0,
            BeginTime = TimeSpan.FromMilliseconds(600),
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = easing, EnableDependentAnimation = true
        };
        Storyboard.SetTarget(statsSlide, (TranslateTransform)StatsRow.RenderTransform);
        Storyboard.SetTargetProperty(statsSlide, "Y");
        storyboard.Children.Add(statsSlide);

        // 4. Preview area: scale 0.97→1.0 + fade in (900ms begin, 400ms duration)
        var previewFade = new DoubleAnimation
        {
            From = 0, To = 1,
            BeginTime = TimeSpan.FromMilliseconds(900),
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = easing
        };
        Storyboard.SetTarget(previewFade, PreviewBorder);
        Storyboard.SetTargetProperty(previewFade, "Opacity");
        storyboard.Children.Add(previewFade);

        var previewScaleX = new DoubleAnimation
        {
            From = 0.97, To = 1.0,
            BeginTime = TimeSpan.FromMilliseconds(900),
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = easing, EnableDependentAnimation = true
        };
        Storyboard.SetTarget(previewScaleX, PreviewBorderScale);
        Storyboard.SetTargetProperty(previewScaleX, "ScaleX");
        storyboard.Children.Add(previewScaleX);

        var previewScaleY = new DoubleAnimation
        {
            From = 0.97, To = 1.0,
            BeginTime = TimeSpan.FromMilliseconds(900),
            Duration = TimeSpan.FromMilliseconds(400),
            EasingFunction = easing, EnableDependentAnimation = true
        };
        Storyboard.SetTarget(previewScaleY, PreviewBorderScale);
        Storyboard.SetTargetProperty(previewScaleY, "ScaleY");
        storyboard.Children.Add(previewScaleY);

        // 5. Volume slider: fade in audio after everything else has settled (1500ms begin, 800ms ramp)
        if (_savedPreviewVolume > 0 || ViewModel.PreviewVolume > 0)
        {
            var volumeTarget = ViewModel.PreviewVolume > 0 ? ViewModel.PreviewVolume : _savedPreviewVolume;
            _savedPreviewVolume = volumeTarget;
            var volumeAnim = new DoubleAnimation
            {
                From = 0,
                To = volumeTarget * 100,
                BeginTime = TimeSpan.FromMilliseconds(1500),
                Duration = TimeSpan.FromMilliseconds(800),
                EasingFunction = easing,
                EnableDependentAnimation = true
            };
            Storyboard.SetTarget(volumeAnim, PreviewVolumeSlider);
            Storyboard.SetTargetProperty(volumeAnim, "Value");
            storyboard.Children.Add(volumeAnim);
            ViewModel.SuppressVolumeSave = true;
        }

        storyboard.Completed += (_, _) =>
        {
            _entranceStoryboard = null;
            if (!_isVolumeFadingIn)
            {
                // User already cancelled the fade-in via slider interaction.
                // Just clean up suppression flags; don't overwrite their choice.
                ViewModel.SuppressVolumeSave = false;
                ViewModel.VolumeSaveOverride = null;
                return;
            }
            _isVolumeFadingIn = false;
            ViewModel.SuppressVolumeSave = false;
            ViewModel.VolumeSaveOverride = null;
            if (_savedPreviewVolume > 0)
            {
                ViewModel.PreviewVolume = _savedPreviewVolume;
            }
        };

        _entranceStoryboard = storyboard;
        storyboard.Begin();

        // 6. Control bar shadow depth fade-in (Composition animation, compositor thread)
        // Delayed so the bar appears first, then gains depth.
        FadeInShadow(_controlBarShadowVisual, delayMs: 400, durationMs: 500);
    }
    private static void FadeInShadow(SpriteVisual? visual, int delayMs, int durationMs)
    {
        if (visual == null) return;
        var compositor = visual.Compositor;
        var anim = compositor.CreateScalarKeyFrameAnimation();
        anim.InsertKeyFrame(0f, 0f);
        anim.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f)));
        anim.Duration = TimeSpan.FromMilliseconds(durationMs);
        anim.DelayTime = TimeSpan.FromMilliseconds(delayMs);
        visual.StartAnimation("Opacity", anim);
    }
    private static void FadeOutShadow(SpriteVisual? visual, int durationMs)
    {
        if (visual == null) return;
        var compositor = visual.Compositor;
        var anim = compositor.CreateScalarKeyFrameAnimation();
        anim.InsertKeyFrame(1f, 0f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f)));
        anim.Duration = TimeSpan.FromMilliseconds(durationMs);
        visual.StartAnimation("Opacity", anim);
    }
    private void AnimateRecordButtonWidth(double from, double to, Action? onCompleted = null)
    {
        var anim = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(anim, RecordButton);
        Storyboard.SetTargetProperty(anim, "Width");

        var sb = new Storyboard();
        sb.Children.Add(anim);
        sb.Completed += (_, _) =>
        {
            // Set final width explicitly (NaN for pill, 36 for circle)
            RecordButton.Width = to == 36 ? 36 : double.NaN;
            onCompleted?.Invoke();
        };
        sb.Begin();
    }
    private static void AnimateScale(ScaleTransform target, double to, TimeSpan duration)
    {
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

        var scaleX = new DoubleAnimation
        {
            To = to,
            Duration = new Duration(duration),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(scaleX, target);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");

        var scaleY = new DoubleAnimation
        {
            To = to,
            Duration = new Duration(duration),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(scaleY, target);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");

        var storyboard = new Storyboard();
        storyboard.Children.Add(scaleX);
        storyboard.Children.Add(scaleY);
        storyboard.Begin();
    }
    private void ResetPreviewContentTransform()
    {
        PreviewContentGrid.Opacity = 1.0;
        PreviewContentScale.ScaleX = 1.0;
        PreviewContentScale.ScaleY = 1.0;
    }
    private static Task BeginStoryboardAsync(Storyboard storyboard)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        storyboard.Completed += (_, _) => tcs.TrySetResult(true);
        storyboard.Begin();
        return tcs.Task;
    }
    private Task AnimatePreviewTransitionAsync(double opacityTarget, double scaleTarget, int durationMs, EasingMode easingMode)
    {
        var duration = TimeSpan.FromMilliseconds(durationMs);
        var easing = new CubicEase { EasingMode = easingMode };

        var fade = new DoubleAnimation { To = opacityTarget, Duration = new Duration(duration), EasingFunction = easing };
        Storyboard.SetTarget(fade, PreviewContentGrid);
        Storyboard.SetTargetProperty(fade, "Opacity");

        var scaleX = new DoubleAnimation { To = scaleTarget, Duration = new Duration(duration), EasingFunction = easing };
        Storyboard.SetTarget(scaleX, PreviewContentScale);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");

        var scaleY = new DoubleAnimation { To = scaleTarget, Duration = new Duration(duration), EasingFunction = easing };
        Storyboard.SetTarget(scaleY, PreviewContentScale);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");

        var storyboard = new Storyboard();
        storyboard.Children.Add(fade);
        storyboard.Children.Add(scaleX);
        storyboard.Children.Add(scaleY);
        return BeginStoryboardAsync(storyboard);
    }
    private Task AnimatePreviewOutAsync()
    {
        FadeOutShadow(_videoShadowVisual, durationMs: 150);
        return AnimatePreviewTransitionAsync(0.0, 0.97, 200, EasingMode.EaseIn);
    }
    private Task AnimatePreviewInAsync()
    {
        FadeInShadow(_videoShadowVisual, delayMs: 0, durationMs: 400);
        return AnimatePreviewTransitionAsync(1.0, 1.0, 250, EasingMode.EaseOut);
    }
    private void UpdateLiveSignalInfoVisibility()
    {
        const string emDash = "\u2014";
        bool allReal =
            ViewModel.LiveResolution != emDash &&
            ViewModel.LiveFrameRate != emDash &&
            ViewModel.LivePixelFormat != emDash;

        if (allReal && !_liveSignalInfoVisible)
        {
            // Debounce: wait 500ms for values to stabilize before animating in.
            // During startup the pipeline cascades through Requested → Negotiated → Actual,
            // and each level can change the text width. Animating mid-cascade looks jerky.
            if (_liveSignalDebounceTimer == null)
            {
                _liveSignalDebounceTimer = DispatcherQueue.CreateTimer();
                _liveSignalDebounceTimer.Interval = TimeSpan.FromMilliseconds(500);
                _liveSignalDebounceTimer.IsRepeating = false;
                _liveSignalDebounceTimer.Tick += (_, _) =>
                {
                    _liveSignalDebounceTimer = null;
                    // Re-check: values might have reverted during the wait
                    bool stillReal =
                        ViewModel.LiveResolution != emDash &&
                        ViewModel.LiveFrameRate != emDash &&
                        ViewModel.LivePixelFormat != emDash;
                    if (stillReal && !_liveSignalInfoVisible)
                    {
                        _liveSignalInfoVisible = true;
                        AnimateLiveSignalInfoIn();
                    }
                };
            }
            _liveSignalDebounceTimer.Start();
        }
        else if (!allReal)
        {
            // Cancel any pending show debounce
            if (_liveSignalDebounceTimer != null)
            {
                _liveSignalDebounceTimer.Stop();
                _liveSignalDebounceTimer = null;
            }

            // Debounce hide: during Hz transitions the source bounces through
            // unstable states briefly. Wait 800ms before hiding to avoid flicker.
            if (_liveSignalInfoVisible)
            {
                if (_liveSignalHideDebounceTimer == null)
                {
                    _liveSignalHideDebounceTimer = DispatcherQueue.CreateTimer();
                    _liveSignalHideDebounceTimer.Interval = TimeSpan.FromMilliseconds(800);
                    _liveSignalHideDebounceTimer.IsRepeating = false;
                    _liveSignalHideDebounceTimer.Tick += (_, _) =>
                    {
                        _liveSignalHideDebounceTimer = null;
                        bool stillGone =
                            ViewModel.LiveResolution == emDash ||
                            ViewModel.LiveFrameRate == emDash ||
                            ViewModel.LivePixelFormat == emDash;
                        if (stillGone && _liveSignalInfoVisible)
                        {
                            _liveSignalInfoVisible = false;
                            AnimateLiveSignalInfoOut();
                        }
                    };
                }
                _liveSignalHideDebounceTimer.Start();
            }
        }
        else if (allReal && _liveSignalHideDebounceTimer != null)
        {
            // Signal recovered before hide debounce fired — cancel the hide
            _liveSignalHideDebounceTimer.Stop();
            _liveSignalHideDebounceTimer = null;
        }
    }
    private void AnimateLiveSignalInfoIn()
    {
        LiveSignalInfoPanel.Opacity = 0;
        LiveSignalInfoPanel.Visibility = Visibility.Visible;

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var storyboard = new Storyboard();

        var fade = new DoubleAnimation
        {
            From = 0, To = 1,
            Duration = TimeSpan.FromMilliseconds(450),
            EasingFunction = easing
        };
        Storyboard.SetTarget(fade, LiveSignalInfoPanel);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);

        var scaleX = new DoubleAnimation
        {
            From = 0.92, To = 1.0,
            Duration = TimeSpan.FromMilliseconds(450),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(scaleX, LiveSignalInfoScale);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");
        storyboard.Children.Add(scaleX);

        var scaleY = new DoubleAnimation
        {
            From = 0.92, To = 1.0,
            Duration = TimeSpan.FromMilliseconds(450),
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(scaleY, LiveSignalInfoScale);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");
        storyboard.Children.Add(scaleY);

        storyboard.Begin();
    }
    private void AnimateLiveSignalInfoOut()
    {
        var easing = new CubicEase { EasingMode = EasingMode.EaseIn };
        var storyboard = new Storyboard();

        var fade = new DoubleAnimation
        {
            From = 1, To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = easing
        };
        Storyboard.SetTarget(fade, LiveSignalInfoPanel);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);

        storyboard.Completed += (_, _) =>
        {
            LiveSignalInfoPanel.Visibility = Visibility.Collapsed;
        };

        storyboard.Begin();
    }
    private static void FadeOutElement(UIElement element)
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
    private static void FadeInElement(UIElement element)
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
    private void ApplySettingsVisibility(bool visible)
    {
        if (_isSettingsShelfAnimating)
        {
            return;
        }

        var isCurrentlyVisible = SettingsOverlayPanel.Visibility == Visibility.Visible;
        if (visible == isCurrentlyVisible)
        {
            return;
        }

        if (visible)
        {
            ShowSettingsShelf();
        }
        else
        {
            HideSettingsShelf();
        }
    }
    private void AnimateSettingsShelf(bool show)
    {
        _isSettingsShelfAnimating = true;
        var durationMs = show ? 400 : 300;
        var easing = new CubicEase { EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn };
        var duration = TimeSpan.FromMilliseconds(durationMs);

        double targetHeight;
        if (show)
        {
            // Measure natural height without rendering (opacity 0, same sync block)
            SettingsOverlayPanel.Opacity = 0;
            SettingsOverlayPanel.Height = double.NaN;
            SettingsOverlayPanel.Visibility = Visibility.Visible;
            SettingsOverlayPanel.UpdateLayout();
            targetHeight = SettingsOverlayPanel.ActualHeight;
            SettingsOverlayPanel.Height = 0;
        }
        else
        {
            targetHeight = SettingsOverlayPanel.ActualHeight;
            SettingsOverlayPanel.Height = targetHeight; // Pin to numeric value so animation can interpolate
        }

        var heightAnim = new DoubleAnimation
        {
            To = show ? targetHeight : 0,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(heightAnim, SettingsOverlayPanel);
        Storyboard.SetTargetProperty(heightAnim, "Height");

        var fade = new DoubleAnimation
        {
            From = show ? 0 : 1,
            To = show ? 1 : 0,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(fade, SettingsOverlayPanel);
        Storyboard.SetTargetProperty(fade, "Opacity");

        var storyboard = new Storyboard();
        storyboard.Children.Add(heightAnim);
        storyboard.Children.Add(fade);
        storyboard.Completed += (_, _) =>
        {
            if (show)
            {
                SettingsOverlayPanel.Height = double.NaN; // Return to Auto
                SettingsOverlayPanel.Opacity = 1;
            }
            else
            {
                SettingsOverlayPanel.Visibility = Visibility.Collapsed;
                SettingsOverlayPanel.Height = double.NaN;
                SettingsOverlayPanel.Opacity = 1;
            }
            _isSettingsShelfAnimating = false;
        };
        storyboard.Begin();
    }
    private void ShowSettingsShelf() => AnimateSettingsShelf(show: true);
    private void HideSettingsShelf() => AnimateSettingsShelf(show: false);
}
