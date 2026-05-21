using Microsoft.UI.Xaml.Input;

namespace Sussudio;

public sealed partial class MainWindow
{
    private void OnFullScreenPointerActivity(object sender, PointerRoutedEventArgs e)
        => _fullScreenController.OnPointerActivity(e);

    private void OnFullScreenControlsPointerEntered(object sender, PointerRoutedEventArgs e)
        => _fullScreenController.OnControlsPointerEntered();

    private void OnFullScreenControlsPointerExited(object sender, PointerRoutedEventArgs e)
        => _fullScreenController.OnControlsPointerExited(e);

    private void StopFullScreenAutoHideTimer()
        => _fullScreenController.StopAutoHideTimer();
}
