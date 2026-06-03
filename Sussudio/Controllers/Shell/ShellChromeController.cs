using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using Microsoft.UI.Dispatching;
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

internal enum ResponsiveCaptureSettingsLayoutKind
{
    Wide,
    Narrow,
}

internal readonly record struct ResponsiveGridSlot(int Row, int Column);

internal readonly record struct ResponsiveCaptureSettingsPlacement(
    bool CollapseCaptureOptionColumns,
    ResponsiveGridSlot VideoFormat,
    ResponsiveGridSlot Preset,
    ResponsiveGridSlot Split,
    ResponsiveGridSlot CustomBitrate);

internal static class ResponsiveShellLayoutPolicy
{
    public const double ControlBarLabelThreshold = 900.0;
    public const double CaptureSettingsNarrowWidth = 700.0;

    private static readonly ResponsiveCaptureSettingsPlacement NarrowPlacement = new(
        true,
        new ResponsiveGridSlot(1, 1),
        new ResponsiveGridSlot(1, 2),
        new ResponsiveGridSlot(1, 3),
        new ResponsiveGridSlot(1, 2));

    private static readonly ResponsiveCaptureSettingsPlacement WidePlacement = new(
        false,
        new ResponsiveGridSlot(0, 0),
        new ResponsiveGridSlot(0, 5),
        new ResponsiveGridSlot(0, 6),
        new ResponsiveGridSlot(0, 5));

    public static bool ShouldShowControlBarLabels(double controlBarWidth)
        => controlBarWidth >= ControlBarLabelThreshold;

    public static ResponsiveCaptureSettingsLayoutKind GetCaptureSettingsLayoutKind(double width)
        => width < CaptureSettingsNarrowWidth
            ? ResponsiveCaptureSettingsLayoutKind.Narrow
            : ResponsiveCaptureSettingsLayoutKind.Wide;

    public static ResponsiveCaptureSettingsPlacement GetCaptureSettingsPlacement(
        ResponsiveCaptureSettingsLayoutKind layoutKind)
        => layoutKind == ResponsiveCaptureSettingsLayoutKind.Narrow
            ? NarrowPlacement
            : WidePlacement;
}

internal sealed class ControlBarLabelVisibilityControllerContext
{
    public required Border ControlBarBorder { get; init; }
    public required UIElement[] ControlBarLabels { get; init; }
}

internal sealed class ControlBarLabelVisibilityController
{
    private readonly ControlBarLabelVisibilityControllerContext _context;
    private bool _toggleLabelsVisible;

    public ControlBarLabelVisibilityController(ControlBarLabelVisibilityControllerContext context)
    {
        _context = context;
    }

    public void Attach()
    {
        _context.ControlBarBorder.SizeChanged += (_, e) => ApplyControlBarWidth(e.NewSize.Width);
    }

    private void ApplyControlBarWidth(double controlBarWidth)
    {
        var showLabels = ResponsiveShellLayoutPolicy.ShouldShowControlBarLabels(controlBarWidth);
        if (showLabels == _toggleLabelsVisible)
        {
            return;
        }

        _toggleLabelsVisible = showLabels;
        var visibility = showLabels ? Visibility.Visible : Visibility.Collapsed;
        foreach (var label in _context.ControlBarLabels)
        {
            label.Visibility = visibility;
        }
    }
}

internal sealed class ResponsiveShellLayoutControllerContext
{
    public required Grid CaptureSettingsGrid { get; init; }
    public required ColumnDefinition VideoFormatColumn { get; init; }
    public required ColumnDefinition PresetColumn { get; init; }
    public required ColumnDefinition SplitColumn { get; init; }
    public required FrameworkElement VideoFormatPanel { get; init; }
    public required FrameworkElement PresetPanel { get; init; }
    public required FrameworkElement SplitPanel { get; init; }
    public required FrameworkElement CustomBitratePanel { get; init; }
}

internal sealed class ResponsiveShellLayoutController
{
    private readonly ResponsiveShellLayoutControllerContext _context;
    private bool _captureSettingsNarrow;

    public ResponsiveShellLayoutController(ResponsiveShellLayoutControllerContext context)
    {
        _context = context;
    }

    public void Attach()
    {
        _context.CaptureSettingsGrid.SizeChanged += (_, e) => ApplyCaptureSettingsWidth(e.NewSize.Width);
    }

    private void ApplyCaptureSettingsWidth(double width)
    {
        var layoutKind = ResponsiveShellLayoutPolicy.GetCaptureSettingsLayoutKind(width);
        var narrow = layoutKind == ResponsiveCaptureSettingsLayoutKind.Narrow;
        if (narrow == _captureSettingsNarrow)
        {
            return;
        }

        _captureSettingsNarrow = narrow;
        ApplyCaptureSettingsLayout(ResponsiveShellLayoutPolicy.GetCaptureSettingsPlacement(layoutKind));
    }

    private void ApplyCaptureSettingsLayout(ResponsiveCaptureSettingsPlacement placement)
    {
        var responsiveColumnWidth = placement.CollapseCaptureOptionColumns
            ? new GridLength(0)
            : new GridLength(1, GridUnitType.Star);
        _context.VideoFormatColumn.Width = responsiveColumnWidth;
        _context.PresetColumn.Width = responsiveColumnWidth;
        _context.SplitColumn.Width = responsiveColumnWidth;
        ApplyGridSlot(_context.VideoFormatPanel, placement.VideoFormat);
        ApplyGridSlot(_context.PresetPanel, placement.Preset);
        ApplyGridSlot(_context.SplitPanel, placement.Split);
        ApplyGridSlot(_context.CustomBitratePanel, placement.CustomBitrate);
    }

    private static void ApplyGridSlot(FrameworkElement element, ResponsiveGridSlot slot)
    {
        Grid.SetRow(element, slot.Row);
        Grid.SetColumn(element, slot.Column);
    }
}

internal sealed class LiveSignalInfoControllerContext
{
    public required DispatcherQueue DispatcherQueue { get; init; }
    public required StackPanel LiveSignalInfoPanel { get; init; }
    public required ScaleTransform LiveSignalInfoScale { get; init; }
    public required TextBlock LiveResolutionTextBlock { get; init; }
    public required TextBlock LiveFrameRateTextBlock { get; init; }
    public required TextBlock LivePixelFormatTextBlock { get; init; }
}

internal sealed class LiveSignalInfoController
{
    private const string LiveInfoUnavailable = "\u2014";
    private static readonly TimeSpan ShowDebounce = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan HideDebounce = TimeSpan.FromMilliseconds(800);

    private readonly LiveSignalInfoControllerContext _context;
    private bool _visible;
    private DispatcherQueueTimer? _showDebounceTimer;
    private DispatcherQueueTimer? _hideDebounceTimer;
    private string _liveResolution = LiveInfoUnavailable;
    private string _liveFrameRate = LiveInfoUnavailable;
    private string _livePixelFormat = LiveInfoUnavailable;

    public LiveSignalInfoController(LiveSignalInfoControllerContext context)
    {
        _context = context;
    }

    public void Update(string liveResolution, string liveFrameRate, string livePixelFormat)
    {
        _liveResolution = liveResolution;
        _liveFrameRate = liveFrameRate;
        _livePixelFormat = livePixelFormat;
        _context.LiveResolutionTextBlock.Text = liveResolution;
        _context.LiveFrameRateTextBlock.Text = liveFrameRate;
        _context.LivePixelFormatTextBlock.Text = livePixelFormat;

        if (HasCompleteLiveSignal() && !_visible)
        {
            if (_showDebounceTimer is null)
            {
                _showDebounceTimer = _context.DispatcherQueue.CreateTimer();
                _showDebounceTimer.Interval = ShowDebounce;
                _showDebounceTimer.IsRepeating = false;
                _showDebounceTimer.Tick += (_, _) =>
                {
                    _showDebounceTimer = null;
                    if (HasCompleteLiveSignal() && !_visible)
                    {
                        _visible = true;
                        AnimateIn();
                    }
                };
            }

            _showDebounceTimer.Start();
        }
        else if (!HasCompleteLiveSignal())
        {
            StopShowDebounce();

            if (_visible)
            {
                if (_hideDebounceTimer is null)
                {
                    _hideDebounceTimer = _context.DispatcherQueue.CreateTimer();
                    _hideDebounceTimer.Interval = HideDebounce;
                    _hideDebounceTimer.IsRepeating = false;
                    _hideDebounceTimer.Tick += (_, _) =>
                    {
                        _hideDebounceTimer = null;
                        if (HasMissingLiveSignal() && _visible)
                        {
                            _visible = false;
                            AnimateOut();
                        }
                    };
                }

                _hideDebounceTimer.Start();
            }
        }
        else if (HasCompleteLiveSignal() && _hideDebounceTimer is not null)
        {
            StopHideDebounce();
        }
    }

    public bool TryHandlePropertyChanged(string propertyName, string liveResolution, string liveFrameRate, string livePixelFormat)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.LiveResolution):
            case nameof(MainViewModel.LiveFrameRate):
            case nameof(MainViewModel.LivePixelFormat):
                Update(liveResolution, liveFrameRate, livePixelFormat);
                return true;

            default:
                return false;
        }
    }

    public void StopTimers()
    {
        StopShowDebounce();
        StopHideDebounce();
    }

    private bool HasCompleteLiveSignal()
        => _liveResolution != LiveInfoUnavailable &&
           _liveFrameRate != LiveInfoUnavailable &&
           _livePixelFormat != LiveInfoUnavailable;

    private bool HasMissingLiveSignal()
        => _liveResolution == LiveInfoUnavailable ||
           _liveFrameRate == LiveInfoUnavailable ||
           _livePixelFormat == LiveInfoUnavailable;

    private void StopShowDebounce()
    {
        _showDebounceTimer?.Stop();
        _showDebounceTimer = null;
    }

    private void StopHideDebounce()
    {
        _hideDebounceTimer?.Stop();
        _hideDebounceTimer = null;
    }

    private void AnimateIn()
    {
        _context.LiveSignalInfoPanel.Opacity = 0;
        _context.LiveSignalInfoPanel.Visibility = Visibility.Visible;

        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var storyboard = new Storyboard();

        var fade = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(450),
            EasingFunction = easing,
        };
        Storyboard.SetTarget(fade, _context.LiveSignalInfoPanel);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);

        var scaleX = new DoubleAnimation
        {
            From = 0.92,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(450),
            EasingFunction = easing,
            EnableDependentAnimation = true,
        };
        Storyboard.SetTarget(scaleX, _context.LiveSignalInfoScale);
        Storyboard.SetTargetProperty(scaleX, "ScaleX");
        storyboard.Children.Add(scaleX);

        var scaleY = new DoubleAnimation
        {
            From = 0.92,
            To = 1.0,
            Duration = TimeSpan.FromMilliseconds(450),
            EasingFunction = easing,
            EnableDependentAnimation = true,
        };
        Storyboard.SetTarget(scaleY, _context.LiveSignalInfoScale);
        Storyboard.SetTargetProperty(scaleY, "ScaleY");
        storyboard.Children.Add(scaleY);

        storyboard.Begin();
    }

    private void AnimateOut()
    {
        var easing = new CubicEase { EasingMode = EasingMode.EaseIn };
        var storyboard = new Storyboard();

        var fade = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = easing,
        };
        Storyboard.SetTarget(fade, _context.LiveSignalInfoPanel);
        Storyboard.SetTargetProperty(fade, "Opacity");
        storyboard.Children.Add(fade);

        storyboard.Completed += (_, _) =>
        {
            _context.LiveSignalInfoPanel.Visibility = Visibility.Collapsed;
        };

        storyboard.Begin();
    }
}
