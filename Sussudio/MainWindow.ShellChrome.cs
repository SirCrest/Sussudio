using System.Collections.Generic;
using Sussudio.Controllers;
using Microsoft.UI.Xaml;
using Sussudio.ViewModels;

namespace Sussudio;

// XAML-facing shell chrome adapter. ControlBarAnimationController owns the
// shared button list plus hover press/release scale behavior; ShellElevationController
// owns static ThemeShadow/elevation setup; shared shadow fade composition details
// live in PreviewShadowFadeAnimator.
public sealed partial class MainWindow
{
    private ControlBarAnimationController _controlBarAnimationController = null!;
    private ShellElevationController _shellElevationController = null!;

    private void InitializeControlBarAnimationController()
    {
        _controlBarAnimationController = new ControlBarAnimationController(new ControlBarAnimationControllerContext
        {
            ControlBarButtons = new FrameworkElement[]
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
                StatsToggle,
                FrameTimeOverlayToggle,
            },
        });
    }

    private void InitializeShellElevationController()
    {
        _shellElevationController = new ShellElevationController(new ShellElevationControllerContext
        {
            ControlBarBorder = ControlBarBorder,
            SettingsOverlayPanel = SettingsOverlayPanel,
            RecordButton = RecordButton,
        });
    }

    private void SetupButtonHoverAnimations()
        => _controlBarAnimationController.AttachHoverAnimations();

    private IReadOnlyList<FrameworkElement> GetEntranceButtons()
        => _controlBarAnimationController.EntranceButtons;

    private void ApplyShellElevation()
        => _shellElevationController.Apply();

    private bool TryHandleShellPropertyChanged(string propertyName)
    {
        switch (propertyName)
        {
            case nameof(MainViewModel.IsStatsVisible):
                HandleStatsVisibleChanged();
                return true;

            case nameof(MainViewModel.IsSettingsVisible):
                ApplySettingsVisibility(ViewModel.IsSettingsVisible);
                return true;

            default:
                return false;
        }
    }

    private void HandleStatsVisibleChanged()
        => ApplyStatsVisibility(ViewModel.IsStatsVisible);
}
