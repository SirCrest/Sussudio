using System.Collections.Generic;
using Sussudio.Controllers;
using Microsoft.UI.Xaml;

namespace Sussudio;

// XAML-facing control-bar animation adapter. ControlBarAnimationController owns
// the shared button list plus hover press/release scale behavior; shared shadow
// fade composition details live in CompositionShadowFadeAnimator.
public sealed partial class MainWindow
{
    private ControlBarAnimationController _controlBarAnimationController = null!;

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

    private void SetupButtonHoverAnimations()
        => _controlBarAnimationController.AttachHoverAnimations();

    private IReadOnlyList<FrameworkElement> GetEntranceButtons()
        => _controlBarAnimationController.EntranceButtons;
}
