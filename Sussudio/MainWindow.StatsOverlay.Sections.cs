using Microsoft.UI.Xaml.Input;

namespace Sussudio;

public sealed partial class MainWindow
{
    private void StatsSectionHeader_Tapped(object sender, TappedRoutedEventArgs e)
        => _statsOverlayCompositionController.ToggleSectionFromHeader(sender);

    private void SetStatsSectionVisible(string section, bool visible)
        => _statsOverlayCompositionController.SetSectionVisible(section, visible);
}
