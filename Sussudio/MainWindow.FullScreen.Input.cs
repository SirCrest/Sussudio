using Microsoft.UI.Xaml.Input;

namespace Sussudio;

public sealed partial class MainWindow
{
    private void OnContentKeyDown(object sender, KeyRoutedEventArgs e)
        => _fullScreenController.OnKeyDown(e);
}
