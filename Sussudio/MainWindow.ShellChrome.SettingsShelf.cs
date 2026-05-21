using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private SettingsShelfController _settingsShelfController = null!;

    private void InitializeSettingsShelfController()
    {
        _settingsShelfController = new SettingsShelfController(new SettingsShelfControllerContext
        {
            SettingsOverlayPanel = SettingsOverlayPanel,
        });
    }

    private void SettingsToggleButton_Click(object sender, RoutedEventArgs e)
        => _settingsShelfController.Toggle();

    private void ApplySettingsVisibility(bool visible)
        => _settingsShelfController.ApplyVisibility(visible);

    private void ShowSettingsShelf()
        => _settingsShelfController.Show();

    private void HideSettingsShelf()
        => _settingsShelfController.Hide();
}
