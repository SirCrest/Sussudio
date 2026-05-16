using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing responsive-layout adapter. Controllers apply
// ResponsiveShellLayoutPolicy decisions to the WinUI elements.
public sealed partial class MainWindow
{
    private ControlBarLabelVisibilityController _controlBarLabelVisibilityController = null!;
    private ResponsiveShellLayoutController _responsiveShellLayoutController = null!;

    private void InitializeResponsiveShellLayoutController()
    {
        var controlBarLabels = new UIElement[]
        {
            HdrToggleLabel,
            AudioRecordToggleLabel,
            PreviewButtonLabel,
            HdrPreviewToggleLabel,
            AudioPreviewToggleLabel,
            StatsToggleLabel,
            FrameTimeOverlayToggleLabel,
            FlashbackToggleLabel,
        };

        _controlBarLabelVisibilityController = new ControlBarLabelVisibilityController(new ControlBarLabelVisibilityControllerContext
        {
            ControlBarBorder = ControlBarBorder,
            ControlBarLabels = controlBarLabels,
        });

        _responsiveShellLayoutController = new ResponsiveShellLayoutController(new ResponsiveShellLayoutControllerContext
        {
            CaptureSettingsGrid = CaptureSettingsGrid,
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
    {
        _controlBarLabelVisibilityController.Attach();
        _responsiveShellLayoutController.Attach();
    }
}
