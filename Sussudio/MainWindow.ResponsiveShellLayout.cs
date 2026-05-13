using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing responsive-layout adapter. ResponsiveShellLayoutController owns
// control-bar label visibility and capture-settings narrow/wide placement.
public sealed partial class MainWindow
{
    private ResponsiveShellLayoutController _responsiveShellLayoutController = null!;

    private void InitializeResponsiveShellLayoutController()
    {
        _responsiveShellLayoutController = new ResponsiveShellLayoutController(new ResponsiveShellLayoutControllerContext
        {
            ControlBarBorder = ControlBarBorder,
            CaptureSettingsGrid = CaptureSettingsGrid,
            HdrToggleLabel = HdrToggleLabel,
            AudioRecordToggleLabel = AudioRecordToggleLabel,
            PreviewButtonLabel = PreviewButtonLabel,
            HdrPreviewToggleLabel = HdrPreviewToggleLabel,
            AudioPreviewToggleLabel = AudioPreviewToggleLabel,
            StatsToggleLabel = StatsToggleLabel,
            FrameTimeOverlayToggleLabel = FrameTimeOverlayToggleLabel,
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
