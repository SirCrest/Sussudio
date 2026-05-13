using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for static shell elevation and ThemeShadow setup.
public sealed partial class MainWindow
{
    private ShellElevationController _shellElevationController = null!;

    private void InitializeShellElevationController()
    {
        _shellElevationController = new ShellElevationController(new ShellElevationControllerContext
        {
            ControlBarBorder = ControlBarBorder,
            SettingsOverlayPanel = SettingsOverlayPanel,
            RecordButton = RecordButton,
        });
    }

    private void ApplyShellElevation()
        => _shellElevationController.Apply();
}
