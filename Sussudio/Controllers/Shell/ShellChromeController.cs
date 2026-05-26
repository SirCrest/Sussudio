using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class ControlBarAnimationControllerContext
{
    public required IReadOnlyList<FrameworkElement> ControlBarButtons { get; init; }
}

internal sealed class ControlBarAnimationController
{
    private readonly ControlBarAnimationControllerContext _context;

    public ControlBarAnimationController(ControlBarAnimationControllerContext context)
    {
        _context = context;
    }

    public IReadOnlyList<FrameworkElement> EntranceButtons => _context.ControlBarButtons;

    public void AttachHoverAnimations()
    {
        foreach (var button in _context.ControlBarButtons)
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
}

internal sealed class ShellElevationControllerContext
{
    public required UIElement ControlBarBorder { get; init; }
    public required UIElement SettingsOverlayPanel { get; init; }
    public required UIElement RecordButton { get; init; }
}

internal sealed class ShellElevationController
{
    private readonly ShellElevationControllerContext _context;

    public ShellElevationController(ShellElevationControllerContext context)
    {
        _context = context;
    }

    public void Apply()
    {
        var controlBarShadow = new ThemeShadow();
        controlBarShadow.Receivers.Add(_context.SettingsOverlayPanel);
        _context.ControlBarBorder.Shadow = controlBarShadow;
        _context.ControlBarBorder.Translation = new Vector3(0, 0, 32);

        var recordButtonShadow = new ThemeShadow();
        _context.RecordButton.Shadow = recordButtonShadow;
        _context.RecordButton.Translation = new Vector3(0, 0, 16);
    }
}

internal sealed class ShellPropertyChangedControllerContext
{
    public required StatsOverlayCompositionController StatsOverlayComposition { get; init; }
    public required SettingsShelfController SettingsShelf { get; init; }
    public required Func<bool> IsStatsVisible { get; init; }
    public required Func<bool> IsSettingsVisible { get; init; }
}

internal sealed class ShellPropertyChangedController
{
    private readonly ShellPropertyChangedControllerContext _context;

    public ShellPropertyChangedController(ShellPropertyChangedControllerContext context)
    {
        _context = context;
    }

    public bool TryHandlePropertyChanged(string propertyName)
    {
        if (_context.StatsOverlayComposition.TryHandlePropertyChanged(propertyName, _context.IsStatsVisible()))
        {
            return true;
        }

        if (_context.SettingsShelf.TryHandlePropertyChanged(propertyName, _context.IsSettingsVisible()))
        {
            return true;
        }

        return false;
    }
}

internal sealed class SettingsShelfControllerContext
{
    public required FrameworkElement SettingsOverlayPanel { get; init; }
}

internal sealed class SettingsShelfController
{
    private readonly SettingsShelfControllerContext _context;
    private bool _isAnimating;

    public SettingsShelfController(SettingsShelfControllerContext context)
    {
        _context = context;
    }

    public bool IsAnimating => _isAnimating;

    public bool IsVisible => _context.SettingsOverlayPanel.Visibility == Visibility.Visible;

    public void Toggle()
    {
        if (_isAnimating)
        {
            return;
        }

        if (IsVisible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public void ApplyVisibility(bool visible)
    {
        if (_isAnimating)
        {
            return;
        }

        if (visible == IsVisible)
        {
            return;
        }

        if (visible)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    public bool TryHandlePropertyChanged(string propertyName, bool isSettingsVisible)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.IsSettingsVisible):
                ApplyVisibility(isSettingsVisible);
                return true;

            default:
                return false;
        }
    }

    public void Show()
        => Animate(show: true);

    public void Hide()
        => Animate(show: false);

    public void ResetAnimationState()
        => _isAnimating = false;

    private void Animate(bool show)
    {
        _isAnimating = true;
        var durationMs = show ? 400 : 300;
        var easing = new CubicEase { EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn };
        var duration = TimeSpan.FromMilliseconds(durationMs);

        double targetHeight;
        if (show)
        {
            _context.SettingsOverlayPanel.Opacity = 0;
            _context.SettingsOverlayPanel.Height = double.NaN;
            _context.SettingsOverlayPanel.Visibility = Visibility.Visible;
            _context.SettingsOverlayPanel.UpdateLayout();
            targetHeight = _context.SettingsOverlayPanel.ActualHeight;
            _context.SettingsOverlayPanel.Height = 0;
        }
        else
        {
            targetHeight = _context.SettingsOverlayPanel.ActualHeight;
            _context.SettingsOverlayPanel.Height = targetHeight;
        }

        var heightAnim = new DoubleAnimation
        {
            To = show ? targetHeight : 0,
            Duration = duration,
            EasingFunction = easing,
            EnableDependentAnimation = true
        };
        Storyboard.SetTarget(heightAnim, _context.SettingsOverlayPanel);
        Storyboard.SetTargetProperty(heightAnim, "Height");

        var fade = new DoubleAnimation
        {
            From = show ? 0 : 1,
            To = show ? 1 : 0,
            Duration = duration,
            EasingFunction = easing
        };
        Storyboard.SetTarget(fade, _context.SettingsOverlayPanel);
        Storyboard.SetTargetProperty(fade, "Opacity");

        var storyboard = new Storyboard();
        storyboard.Children.Add(heightAnim);
        storyboard.Children.Add(fade);
        storyboard.Completed += (_, _) =>
        {
            if (show)
            {
                _context.SettingsOverlayPanel.Height = double.NaN;
                _context.SettingsOverlayPanel.Opacity = 1;
            }
            else
            {
                _context.SettingsOverlayPanel.Visibility = Visibility.Collapsed;
                _context.SettingsOverlayPanel.Height = double.NaN;
                _context.SettingsOverlayPanel.Opacity = 1;
            }

            _isAnimating = false;
        };
        storyboard.Begin();
    }
}

internal sealed class StatusStripPresentationControllerContext
{
    public required InfoBar DiskWarningInfoBar { get; init; }
    public required TextBlock StatusTextBlock { get; init; }
    public required TextBlock RecordingTimeTextBlock { get; init; }
    public required TextBlock DiskSpaceTextBlock { get; init; }
    public required TextBlock RecordingSizeTextBlock { get; init; }
    public required TextBlock RecordingBitrateTextBlock { get; init; }
}

internal readonly record struct StatusStripPresentationSnapshot(
    string StatusText,
    string RecordingTime,
    string DiskSpaceInfo,
    string RecordingSizeInfo,
    string RecordingBitrateInfo,
    string FlashbackBitrateInfo,
    bool IsDiskWarningActive,
    bool IsRecording,
    bool IsFlashbackEnabled);

internal sealed class StatusStripPresentationController
{
    private readonly StatusStripPresentationControllerContext _context;

    public StatusStripPresentationController(StatusStripPresentationControllerContext context)
    {
        _context = context;
    }

    public void ApplyInitial(StatusStripPresentationSnapshot snapshot)
    {
        UpdateStatusText(snapshot.StatusText);
        UpdateRecordingTime(snapshot.RecordingTime);
        UpdateDiskSpace(snapshot.DiskSpaceInfo);
        UpdateRecordingSize(snapshot.RecordingSizeInfo);
        UpdateRecordingBitrate(snapshot.RecordingBitrateInfo);
        UpdateDiskWarning(snapshot.IsDiskWarningActive);
    }

    public bool TryHandlePropertyChanged(
        string? propertyName,
        StatusStripPresentationSnapshot snapshot,
        Action applyWindowTitle)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.StatusText):
                UpdateStatusText(snapshot.StatusText);
                return true;

            case nameof(MainViewModel.RecordingTime):
                UpdateRecordingTime(snapshot.RecordingTime);
                if (snapshot.IsRecording)
                {
                    applyWindowTitle();
                }

                return true;

            case nameof(MainViewModel.DiskSpaceInfo):
                UpdateDiskSpace(snapshot.DiskSpaceInfo);
                return true;

            case nameof(MainViewModel.RecordingSizeInfo):
                UpdateRecordingSize(snapshot.RecordingSizeInfo);
                return true;

            case nameof(MainViewModel.RecordingBitrateInfo):
                UpdateRecordingBitrate(snapshot.RecordingBitrateInfo);
                return true;

            case nameof(MainViewModel.FlashbackBitrateInfo):
                UpdateFlashbackBitrate(snapshot.FlashbackBitrateInfo, snapshot.IsRecording, snapshot.IsFlashbackEnabled);
                return true;

            case nameof(MainViewModel.IsDiskWarningActive):
                UpdateDiskWarning(snapshot.IsDiskWarningActive);
                return true;

            default:
                return false;
        }
    }

    public void UpdateStatusText(string statusText)
    {
        _context.StatusTextBlock.Text = statusText;
    }

    public void UpdateRecordingTime(string recordingTime)
    {
        _context.RecordingTimeTextBlock.Text = recordingTime;
    }

    public void UpdateDiskSpace(string diskSpaceInfo)
    {
        _context.DiskSpaceTextBlock.Text = diskSpaceInfo;
    }

    public void UpdateRecordingSize(string recordingSizeInfo)
    {
        _context.RecordingSizeTextBlock.Text = recordingSizeInfo;
    }

    public void UpdateRecordingBitrate(string recordingBitrateInfo)
    {
        _context.RecordingBitrateTextBlock.Text = recordingBitrateInfo;
    }

    public void UpdateFlashbackBitrate(string flashbackBitrateInfo, bool isRecording, bool isFlashbackEnabled)
    {
        if (!isRecording && isFlashbackEnabled)
        {
            _context.RecordingBitrateTextBlock.Text = flashbackBitrateInfo;
        }
    }

    public void UpdateDiskWarning(bool isDiskWarningActive)
    {
        _context.DiskWarningInfoBar.IsOpen = isDiskWarningActive;
    }
}

internal sealed class WindowTitleController
{
    private const string DefaultTitle = "Simple Sussudio";

    private readonly string _baseTitle;

    public WindowTitleController()
        : this(BuildWindowTitleBase())
    {
    }

    internal WindowTitleController(string baseTitle)
    {
        _baseTitle = string.IsNullOrWhiteSpace(baseTitle) ? DefaultTitle : baseTitle;
    }

    public string BuildTitle(bool isRecording, string recordingTime)
        => FormatTitle(_baseTitle, isRecording, recordingTime);

    internal static string BuildWindowTitleBase()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            return DefaultTitle;
        }

        return FormatBuildTitle(File.GetLastWriteTime(exePath));
    }

    internal static string FormatBuildTitle(DateTime buildTime)
    {
        if (buildTime == DateTime.MinValue)
        {
            return DefaultTitle;
        }

        return $"{DefaultTitle} (build {buildTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)})";
    }

    internal static string FormatTitle(string baseTitle, bool isRecording, string recordingTime)
        => isRecording ? $"{baseTitle} - REC {recordingTime}" : baseTitle;
}
