using System.Threading.Tasks;

static partial class Program
{
    internal static Task ControlBarHoverAnimations_LiveInController()
    {
        var launchEntranceShellText = ReadRepoFile("Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadMainWindowShellChromeAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/ControlBarAnimationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private ControlBarAnimationController _controlBarAnimationController = null!;");
        AssertContains(adapterText, "private void InitializeControlBarAnimationController()");
        AssertContains(adapterText, "SettingsToggleButton,");
        AssertContains(adapterText, "FrameTimeOverlayToggle,");
        AssertContains(adapterText, "=> _controlBarAnimationController.AttachHoverAnimations();");
        AssertContains(adapterText, "=> _controlBarAnimationController.EntranceButtons;");
        AssertContains(mainWindowText, "InitializeControlBarAnimationController();");
        AssertContains(mainWindowText, "SetupButtonHoverAnimations();");
        AssertContains(launchEntranceShellText, "var buttons = _context.GetEntranceButtons();");
        AssertContains(controllerText, "internal sealed class ControlBarAnimationController");
        AssertContains(controllerText, "public IReadOnlyList<FrameworkElement> EntranceButtons");
        AssertContains(controllerText, "public void AttachHoverAnimations()");
        AssertContains(controllerText, "private static void AnimateScale(");
        AssertDoesNotContain(adapterText, "private FrameworkElement[] GetControlBarButtons()");

        return Task.CompletedTask;
    }

    internal static Task ShellElevationSetup_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadMainWindowShellChromeAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Shell/ShellChromeController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private ShellElevationController _shellElevationController = null!;");
        AssertContains(adapterText, "private void InitializeShellElevationController()");
        AssertContains(adapterText, "ControlBarBorder = ControlBarBorder,");
        AssertContains(adapterText, "SettingsOverlayPanel = SettingsOverlayPanel,");
        AssertContains(adapterText, "RecordButton = RecordButton,");
        AssertContains(adapterText, "private void ApplyShellElevation()");
        AssertContains(adapterText, "=> _shellElevationController.Apply();");
        AssertContains(mainWindowText, "InitializeShellElevationController();");
        AssertContains(mainWindowText, "ApplyShellElevation();");
        AssertContains(controllerText, "internal sealed class ShellElevationController");
        AssertContains(controllerText, "var controlBarShadow = new ThemeShadow();");
        AssertContains(controllerText, "controlBarShadow.Receivers.Add(_context.SettingsOverlayPanel);");
        AssertContains(controllerText, "_context.ControlBarBorder.Translation = new Vector3(0, 0, 32);");
        AssertContains(controllerText, "var recordButtonShadow = new ThemeShadow();");
        AssertContains(controllerText, "_context.RecordButton.Translation = new Vector3(0, 0, 16);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Shell", "ShellElevationController.cs")),
            "shell elevation setup lives with shell chrome controller concerns");
        AssertDoesNotContain(mainWindowText, "new Microsoft.UI.Xaml.Media.ThemeShadow()");
        AssertDoesNotContain(mainWindowText, "ControlBarBorder.Translation = new System.Numerics.Vector3(0, 0, 32);");
        AssertDoesNotContain(mainWindowText, "RecordButton.Translation = new System.Numerics.Vector3(0, 0, 16);");

        return Task.CompletedTask;
    }

    internal static Task PreviewTransitionAnimations_LiveInController()
    {
        var launchEntranceShellText = ReadRepoFile("Sussudio/Controllers/Launch/Entrance/LaunchEntranceAnimationController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadMainWindowPreviewTransitionsAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs").Replace("\r\n", "\n");
        var shadowControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewSurfaceShadowController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private PreviewTransitionAnimationController _previewTransitionAnimationController = null!;");
        AssertContains(adapterText, "private void InitializePreviewTransitionAnimationController()");
        AssertContains(adapterText, "PreviewBorder = PreviewBorder,");
        AssertContains(adapterText, "PreviewContentGrid = PreviewContentGrid,");
        AssertContains(adapterText, "StopPreviewFadeInTimer = StopPreviewFadeInTimer,");
        AssertContains(adapterText, "FadeOutVideoFrameShadow = FadeOutVideoFrameShadow,");
        AssertContains(adapterText, "FadeInVideoFrameShadow = FadeInVideoFrameShadow,");
        AssertContains(adapterText, "=> _previewTransitionAnimationController.AddPreviewShellEntranceAnimations(storyboard, easing, beginMs, durationMs);");
        AssertContains(adapterText, "=> _previewTransitionAnimationController.ResetPreviewContentTransform();");
        AssertContains(adapterText, "=> _previewTransitionAnimationController.AnimatePreviewOutAsync();");
        AssertContains(adapterText, "=> _previewTransitionAnimationController.AnimatePreviewInAsync();");
        AssertContains(adapterText, "=> _previewTransitionAnimationController.PrepareStartupPresentation();");
        AssertContains(mainWindowText, "InitializePreviewTransitionAnimationController();");
        AssertContains(launchEntranceShellText, "_context.AddPreviewShellEntranceAnimations(storyboard, easing, 900, 400);");
        AssertContains(controllerText, "internal sealed class PreviewTransitionAnimationController");
        AssertContains(controllerText, "public void AddPreviewShellEntranceAnimations(Storyboard storyboard, EasingFunctionBase easing, int beginMs, int durationMs)");
        AssertContains(controllerText, "public Task AnimatePreviewOutAsync()");
        AssertContains(controllerText, "public Task AnimatePreviewInAsync()");
        AssertContains(controllerText, "_context.FadeOutVideoFrameShadow(150);");
        AssertContains(controllerText, "_context.FadeInVideoFrameShadow(0, 400);");
        AssertContains(controllerText, "public void PrepareStartupPresentation()");
        AssertContains(controllerText, "public void RevealUnavailablePlaceholder()");
        AssertContains(controllerText, "public static void FadeOutElement(UIElement element)");
        AssertContains(controllerText, "private Task AnimatePreviewTransitionAsync(");
        AssertContains(controllerText, "private static Task BeginStoryboardAsync(");
        AssertContains(shadowControllerText, "internal sealed class PreviewSurfaceShadowController");
        AssertContains(shadowControllerText, "=> FadeIn(_videoShadowVisual, delayMs, durationMs);");
        AssertContains(shadowControllerText, "=> FadeOut(_videoShadowVisual, durationMs);");
        AssertContains(shadowControllerText, "=> FadeIn(_controlBarShadowVisual, delayMs, durationMs);");
        AssertContains(shadowControllerText, "private static void FadeIn(SpriteVisual? visual, int delayMs, int durationMs)");
        AssertContains(shadowControllerText, "private static void FadeOut(SpriteVisual? visual, int durationMs)");
        AssertContains(shadowControllerText, "if (visual == null) return;");
        AssertContains(shadowControllerText, "CreateScalarKeyFrameAnimation()");
        AssertContains(shadowControllerText, "animation.InsertKeyFrame(0f, 0f);");
        AssertContains(shadowControllerText, "animation.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f)));");
        AssertContains(shadowControllerText, "animation.DelayTime = TimeSpan.FromMilliseconds(delayMs);");
        AssertContains(shadowControllerText, "visual.StartAnimation(\"Opacity\", animation);");
        AssertDoesNotContain(controllerText, "PreviewShadowFadeAnimator.");
        AssertDoesNotContain(shadowControllerText, "PreviewShadowFadeAnimator.");
        AssertDoesNotContain(adapterText, "FadeOutVideoFrameShadow(durationMs: 150);");
        AssertDoesNotContain(adapterText, "FadeInVideoFrameShadow(delayMs: 0, durationMs: 400);");
        AssertDoesNotContain(adapterText, "private Task AnimatePreviewTransitionAsync(");
        AssertDoesNotContain(adapterText, "private static Task BeginStoryboardAsync(");

        return Task.CompletedTask;
    }

    internal static Task PreviewStartupOverlay_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadMainWindowPreviewTransitionsAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private PreviewStartupOverlayController _previewStartupOverlayController = null!;");
        AssertContains(adapterText, "private void InitializePreviewStartupOverlayController()");
        AssertContains(adapterText, "PreviewLoadingOverlay = PreviewLoadingOverlay,");
        AssertContains(adapterText, "=> _previewStartupOverlayController.Start();");
        AssertContains(adapterText, "=> _previewStartupOverlayController.Stop(IsPreviewReinitAnimating);");
        AssertContains(mainWindowText, "InitializePreviewStartupOverlayController();");
        AssertContains(controllerText, "internal sealed class PreviewStartupOverlayController");
        AssertContains(controllerText, "public void Start()");
        AssertContains(controllerText, "public void Stop(bool isPreviewReinitAnimating)");
        AssertContains(controllerText, "var ring = (ProgressRing)_context.PreviewLoadingOverlay.Children[0];");
        AssertContains(controllerText, "ring.IsActive = true;");
        AssertContains(controllerText, "ring.IsActive = false;");
        AssertContains(controllerText, "_context.PreviewLoadingOverlay.Visibility = Visibility.Collapsed;");
        AssertContains(controllerText, "_context.PreviewLoadingOverlay.Opacity = 1.0;");
        AssertContains(controllerText, "PreviewTransitionAnimationController.FadeInElement(_context.PreviewLoadingOverlay);");
        AssertContains(controllerText, "PreviewTransitionAnimationController.FadeOutElement(_context.PreviewLoadingOverlay);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "Startup", "PreviewStartupOverlayController.cs")),
            "preview startup overlay lives with preview transition presentation concerns");
        AssertDoesNotContain(adapterText, "FadeInElement = FadeInElement,");
        AssertDoesNotContain(adapterText, "FadeOutElement = FadeOutElement,");
        AssertDoesNotContain(adapterText, "var ring = (ProgressRing)");
        AssertDoesNotContain(adapterText, "PreviewLoadingOverlay.Visibility = Visibility.Collapsed;");

        return Task.CompletedTask;
    }

    internal static Task PreviewFadeInReveal_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewTransitionText = ReadMainWindowPreviewTransitionsAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewFadeInController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private PreviewFadeInController _previewFadeInController = null!;");
        AssertContains(adapterText, "private void InitializePreviewFadeInController()");
        AssertContains(adapterText, "DispatcherQueue = _dispatcherQueue,");
        AssertContains(adapterText, "GetRenderer = () => _previewRendererHostController.Renderer,");
        AssertContains(adapterText, "AnimatePreviewInAsync = AnimatePreviewInAsync,");
        AssertContains(adapterText, "StartPreviewAudioFadeIn = () => StartPreviewAudioFadeIn(),");
        AssertContains(adapterText, "=> _previewFadeInController.Schedule();");
        AssertContains(adapterText, "=> _previewFadeInController.Stop();");
        AssertContains(mainWindowText, "InitializePreviewFadeInController();");
        AssertContains(previewTransitionText, "StopPreviewFadeInTimer = StopPreviewFadeInTimer,");
        AssertContains(controllerText, "internal sealed class PreviewFadeInController");
        AssertContains(controllerText, "private const int PreviewFadeInFrameThreshold = 3;");
        AssertContains(controllerText, "private DispatcherQueueTimer? _timer;");
        AssertContains(controllerText, "_timer.Interval = TimeSpan.FromMilliseconds(50);");
        AssertContains(controllerText, "_timer.Interval = TimeSpan.FromMilliseconds(16);");
        AssertContains(controllerText, "var baselineFrames = renderer.FramesRendered;");
        AssertContains(controllerText, "current == null || current != renderer");
        AssertContains(controllerText, "PREVIEW_FADE_IN_READY framesRendered={rendered} baseline={baselineFrames}");
        AssertOccursBefore(controllerText, "_ = _context.AnimatePreviewInAsync();", "_context.StartPreviewAudioFadeIn();");
        AssertDoesNotContain(adapterText, "private const int PreviewFadeInFrameThreshold = 3;");
        AssertDoesNotContain(adapterText, "private DispatcherQueueTimer? _previewFadeInTimer;");
        AssertDoesNotContain(adapterText, "PREVIEW_FADE_IN_READY");

        return Task.CompletedTask;
    }
}
