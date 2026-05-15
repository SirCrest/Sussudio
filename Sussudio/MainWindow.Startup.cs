using System;
using System.Threading;
using Microsoft.UI.Xaml;

namespace Sussudio;

// First-load startup and automation hosting for the shell. Close/finalize
// handling stays in MainWindow.CloseLifecycle.cs.
public sealed partial class MainWindow
{
    private int _automationServicesStarted;

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ((FrameworkElement)this.Content).Loaded -= MainWindow_Loaded;

        // Defer uncloak until the first frame is actually composed. Loaded fires
        // after layout but before the first paint, so uncloaking here would expose
        // an unrendered (black) frame before the splash background paints.
        EventHandler<object>? uncloakOnFirstFrame = null;
        uncloakOnFirstFrame = (_, _) =>
        {
            Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= uncloakOnFirstFrame;
            UncloakNativeShellWindow();
        };
        Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += uncloakOnFirstFrame;

        // Start device init immediately; it runs behind the splash.
        _ = RunUiEventHandlerAsync(async () =>
        {
            Logger.Log("=== MainWindow_Loaded - Starting device enumeration ===");
            try
            {
                await ViewModel.InitializeAsync();
                // LoadSettings just pushed saved volume to CaptureService; re-prime it
                // so WASAPI playback starts silent and fades in only after live frames render.
                PrimePreviewAudioFadeIn();
                await ViewModel.RefreshDevicesAsync();
                if (!ViewModel.IsPreviewing && !IsPreviewFirstVisualConfirmed)
                {
                    RevealPreviewUnavailablePlaceholder();
                }
            }
            finally
            {
                StartAutomationServices();
            }
        }, nameof(MainWindow_Loaded));

        // Start the splash-to-entrance sequence.
        PlaySplashAndEntrance();
    }

    private void StartAutomationServices()
    {
        if (Interlocked.Exchange(ref _automationServicesStarted, 1) != 0)
        {
            return;
        }

        if (_automationPipeServer.Start())
        {
            _automationDiagnosticsHub.Start();
            Logger.Log(
                $"Automation control ready on pipe '{_automationPipeName}' (token required={_automationTokenRequired}).");
        }
        else
        {
            Logger.Log(
                $"Automation control disabled on pipe '{_automationPipeName}' (token required={_automationTokenRequired}).");
        }
    }
}
