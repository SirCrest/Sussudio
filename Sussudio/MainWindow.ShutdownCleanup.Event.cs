using Microsoft.UI.Xaml;

namespace Sussudio;

public sealed partial class MainWindow
{
    private async void MainWindow_Closed(object sender, WindowEventArgs args)
        => await _windowShutdownCleanupController.RunAsync();
}
