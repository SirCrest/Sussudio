using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing launch entrance adapter. LaunchEntranceAnimationController owns
// splash-to-shell choreography and SplashLoadingPhraseController owns phrase
// loading, timer state, and two-line text animation.
public sealed partial class MainWindow
{
    private LaunchEntranceAnimationController _launchEntranceAnimationController = null!;
    private SplashLoadingPhraseController _splashLoadingPhraseController = null!;

    private void InitializeLaunchEntranceAnimationController()
    {
        _launchEntranceAnimationController = new LaunchEntranceAnimationController(new LaunchEntranceAnimationControllerContext
        {
            SplashContent = SplashContent,
            SplashOverlay = SplashOverlay,
            SplashScale = SplashScale,
            ControlBarBorder = ControlBarBorder,
            StatsRow = StatsRow,
            PreviewBorder = PreviewBorder,
            PreviewBorderScale = PreviewBorderScale,
            GetEntranceButtons = GetEntranceButtons,
            IsPreviewFirstVisualConfirmed = () => IsPreviewFirstVisualConfirmed,
            StartSplashLoadingPhrases = StartSplashLoadingPhrases,
            StopSplashLoadingPhrases = StopSplashLoadingPhrases,
            AddPreviewShellEntranceAnimations = AddPreviewShellEntranceAnimations,
            FadeInControlBarShadow = () => FadeInControlBarShadow(delayMs: 400, durationMs: 500),
        });
    }

    private void InitializeSplashLoadingPhraseController()
    {
        _splashLoadingPhraseController = new SplashLoadingPhraseController(new SplashLoadingPhraseControllerContext
        {
            SplashLoadingTextA = SplashLoadingTextA,
            SplashLoadingTextB = SplashLoadingTextB,
            SplashLoadingTransformA = SplashLoadingTransformA,
            SplashLoadingTransformB = SplashLoadingTransformB,
        });
    }

    private void PrepareLaunchEntranceInitialState()
        => _launchEntranceAnimationController.PrepareInitialState();

    private void PlaySplashAndEntrance()
        => _launchEntranceAnimationController.PlaySplashAndEntrance();

    private void StartSplashLoadingPhrases()
        => _splashLoadingPhraseController.Start();

    private void StopSplashLoadingPhrases()
        => _splashLoadingPhraseController.Stop();
}
