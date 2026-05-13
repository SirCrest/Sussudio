using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Sussudio;

// Loading overlay presentation used while preview startup waits for visual
// confirmation. Timeout and watchdog behavior stays in MainWindow.PreviewStartup.cs.
public sealed partial class MainWindow
{
    private void StartPreviewStartupOverlay()
    {
        var ring = (ProgressRing)PreviewLoadingOverlay.Children[0];
        ring.IsActive = true;
        FadeInElement(PreviewLoadingOverlay);
    }

    private void StopPreviewStartupOverlay()
    {
        if (PreviewLoadingOverlay.Visibility == Visibility.Collapsed)
        {
            return;
        }

        var ring = (ProgressRing)PreviewLoadingOverlay.Children[0];
        ring.IsActive = false;
        if (_isPreviewReinitAnimating)
        {
            PreviewLoadingOverlay.Visibility = Visibility.Collapsed;
            PreviewLoadingOverlay.Opacity = 1.0;
            return;
        }

        FadeOutElement(PreviewLoadingOverlay);
    }
}
