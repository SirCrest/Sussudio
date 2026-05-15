using Microsoft.UI.Xaml.Input;
using Sussudio.Controllers;

namespace Sussudio;

// XAML and automation adapter for stats section chrome.
public sealed partial class MainWindow
{
    private StatsSectionChromeController _statsSectionChromeController = null!;

    private void InitializeStatsSectionChromeController()
    {
        _statsSectionChromeController = new StatsSectionChromeController(new StatsSectionChromeControllerContext
        {
            StatsDockPanel = StatsDockPanel,
            DiagnosticsContent = Diagnostics_Content,
            RefreshDiagnosticsSection = _statsDockRefreshController.RefreshDiagnosticsSection
        });
    }

    private void StatsSectionHeader_Tapped(object sender, TappedRoutedEventArgs e)
        => _statsSectionChromeController.ToggleFromHeader(sender);

    private void SetStatsSectionVisible(string section, bool visible)
        => _statsSectionChromeController.SetVisible(section, visible);
}
