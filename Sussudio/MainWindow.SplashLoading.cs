using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing splash phrase adapter. SplashLoadingPhraseController owns phrase
// loading, pacing, timer state, and two-line text animation.
public sealed partial class MainWindow
{
    private SplashLoadingPhraseController _splashLoadingPhraseController = null!;

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

    private void StartSplashLoadingPhrases()
        => _splashLoadingPhraseController.Start();

    private void StopSplashLoadingPhrases()
        => _splashLoadingPhraseController.Stop();
}
