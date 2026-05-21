using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Sussudio;

public sealed partial class MainWindow
{
    private void PreviewBorder_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        ToggleFullScreen();
    }

    private void FullScreenMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ToggleFullScreen();
    }

    private void FullScreenButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFullScreen();
    }

    private void ToggleFullScreen()
        => _fullScreenController.Toggle();

    public Task SetFullScreenEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => InvokeOnUiThreadAsync(
            () => _fullScreenController.SetEnabledAsync(enabled),
            cancellationToken);

    private void EnterFullScreen()
        => _fullScreenController.Enter();

    private void ExitFullScreen()
        => _fullScreenController.Exit();

    private Task EnterFullScreenAsync()
        => _fullScreenController.EnterAsync();

    private Task ExitFullScreenAsync()
        => _fullScreenController.ExitAsync();
}
