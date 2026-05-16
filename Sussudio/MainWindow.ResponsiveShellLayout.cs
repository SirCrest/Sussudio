using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing responsive-layout adapter. ResponsiveShellLayoutController applies
// ResponsiveShellLayoutPolicy decisions to the WinUI elements.
public sealed partial class MainWindow
{
    private ResponsiveShellLayoutController _responsiveShellLayoutController = null!;

    private void InitializeResponsiveShellLayoutController()
    {
        _responsiveShellLayoutController = new ResponsiveShellLayoutController(new ResponsiveShellLayoutControllerContext
        {
            ControlBarBorder = ControlBarBorder,
            CaptureSettingsGrid = CaptureSettingsGrid,
            ControlBarLabels = new UIElement[]
            {
                HdrToggleLabel,
                AudioRecordToggleLabel,
                PreviewButtonLabel,
                HdrPreviewToggleLabel,
                AudioPreviewToggleLabel,
                StatsToggleLabel,
                FrameTimeOverlayToggleLabel,
                FlashbackToggleLabel,
            },
            VideoFormatColumn = VideoFormatColumn,
            PresetColumn = PresetColumn,
            SplitColumn = SplitColumn,
            VideoFormatPanel = VideoFormatPanel,
            PresetPanel = PresetPanel,
            SplitPanel = SplitPanel,
            CustomBitratePanel = CustomBitratePanel,
        });
    }

    private void SetupResponsiveShellLayoutBindings()
        => _responsiveShellLayoutController.Attach();
}
