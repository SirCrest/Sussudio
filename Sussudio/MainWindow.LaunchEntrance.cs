using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing launch entrance adapter. LaunchEntranceAnimationController owns
// splash-to-shell choreography and its one-shot playback state.
public sealed partial class MainWindow
{
    private LaunchEntranceAnimationController _launchEntranceAnimationController = null!;

    private void InitializeLaunchEntranceAnimationController()
    {
        _launchEntranceAnimationController = new LaunchEntranceAnimationController(new LaunchEntranceAnimationControllerContext
        {
            SplashContent = SplashContent,
            SplashOverlay = SplashOverlay,
            SplashScale = SplashScale,
            ControlBarBorder = ControlBarBorder,
            StatsRow = StatsRow,
            GetEntranceButtons = GetEntranceButtons,
            IsPreviewFirstVisualConfirmed = () => _previewFirstVisualConfirmed,
            StartSplashLoadingPhrases = StartSplashLoadingPhrases,
            StopSplashLoadingPhrases = StopSplashLoadingPhrases,
            AddPreviewShellEntranceAnimations = AddPreviewShellEntranceAnimations,
            FadeInControlBarShadow = () => FadeInShadow(_controlBarShadowVisual, delayMs: 400, durationMs: 500),
        });
    }

    private void PlaySplashAndEntrance()
        => _launchEntranceAnimationController.PlaySplashAndEntrance();
}
