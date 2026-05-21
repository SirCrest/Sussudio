using Microsoft.UI.Xaml;
using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private LaunchStartupController _launchStartupController = null!;

    private void InitializeLaunchStartupController()
    {
        _launchStartupController = new LaunchStartupController(new LaunchStartupControllerContext
        {
            MainContent = (FrameworkElement)Content,
            LoadedHandler = MainWindow_Loaded,
            ScheduleNativeShellRevealAfterFirstFrame = ScheduleNativeShellRevealAfterFirstFrame,
            RunUiEventHandlerAsync = RunUiEventHandlerAsync,
            InitializeViewModelAsync = ViewModel.InitializeAsync,
            PrimePreviewAudioFadeIn = PrimePreviewAudioFadeIn,
            RefreshDevicesAsync = () => ViewModel.RefreshDevicesAsync(),
            IsPreviewing = () => ViewModel.IsPreviewing,
            IsPreviewFirstVisualConfirmed = () => IsPreviewFirstVisualConfirmed,
            RevealPreviewUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,
            StartAutomationHost = _automationHostLifecycleController.Start,
            PlaySplashAndEntrance = PlaySplashAndEntrance,
            Log = message => Logger.Log(message),
        });
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        => _launchStartupController.HandleLoaded(nameof(MainWindow_Loaded));
}
