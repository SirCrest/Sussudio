using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private ShellPropertyChangedController _shellPropertyChangedController = null!;

    private void InitializeShellPropertyChangedController()
    {
        _shellPropertyChangedController = new ShellPropertyChangedController(new ShellPropertyChangedControllerContext
        {
            StatsOverlayComposition = _statsOverlayCompositionController,
            SettingsShelf = _settingsShelfController,
            IsStatsVisible = () => ViewModel.IsStatsVisible,
            IsSettingsVisible = () => ViewModel.IsSettingsVisible,
        });
    }

    private bool TryHandleShellPropertyChanged(string propertyName)
        => _shellPropertyChangedController.TryHandlePropertyChanged(propertyName);
}
