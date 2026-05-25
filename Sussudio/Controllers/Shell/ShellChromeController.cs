using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

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
