using System.Threading.Tasks;

static partial class Program
{
    private static Task ControlBarHoverAnimations_LiveInController()
    {
        var launchEntranceControllerText = ReadRepoFile("Sussudio/Controllers/LaunchEntranceAnimationController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.ControlBarAnimations.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/ControlBarAnimationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private ControlBarAnimationController _controlBarAnimationController = null!;");
        AssertContains(adapterText, "private void InitializeControlBarAnimationController()");
        AssertContains(adapterText, "SettingsToggleButton,");
        AssertContains(adapterText, "FrameTimeOverlayToggle,");
        AssertContains(adapterText, "=> _controlBarAnimationController.AttachHoverAnimations();");
        AssertContains(adapterText, "=> _controlBarAnimationController.EntranceButtons;");
        AssertContains(mainWindowText, "InitializeControlBarAnimationController();");
        AssertContains(mainWindowText, "SetupButtonHoverAnimations();");
        AssertContains(launchEntranceControllerText, "var buttons = _context.GetEntranceButtons();");
        AssertContains(controllerText, "internal sealed class ControlBarAnimationController");
        AssertContains(controllerText, "public IReadOnlyList<FrameworkElement> EntranceButtons");
        AssertContains(controllerText, "public void AttachHoverAnimations()");
        AssertContains(controllerText, "private static void AnimateScale(");
        AssertDoesNotContain(adapterText, "private FrameworkElement[] GetControlBarButtons()");

        return Task.CompletedTask;
    }

    private static Task ShellElevationSetup_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.ShellElevation.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/ShellElevationController.cs").Replace("\r\n", "\n");

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
        AssertDoesNotContain(mainWindowText, "new Microsoft.UI.Xaml.Media.ThemeShadow()");
        AssertDoesNotContain(mainWindowText, "ControlBarBorder.Translation = new System.Numerics.Vector3(0, 0, 32);");
        AssertDoesNotContain(mainWindowText, "RecordButton.Translation = new System.Numerics.Vector3(0, 0, 16);");

        return Task.CompletedTask;
    }

    private static Task PreviewTransitionAnimations_LiveInController()
    {
        var launchEntranceControllerText = ReadRepoFile("Sussudio/Controllers/LaunchEntranceAnimationController.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.PreviewTransitions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/PreviewTransitionAnimationController.cs").Replace("\r\n", "\n");
        var shadowAnimatorText = ReadRepoFile("Sussudio/Controllers/CompositionShadowFadeAnimator.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private PreviewTransitionAnimationController _previewTransitionAnimationController = null!;");
        AssertContains(adapterText, "private void InitializePreviewTransitionAnimationController()");
        AssertContains(adapterText, "PreviewBorder = PreviewBorder,");
        AssertContains(adapterText, "PreviewContentGrid = PreviewContentGrid,");
        AssertContains(adapterText, "StopPreviewFadeInTimer = StopPreviewFadeInTimer,");
        AssertContains(adapterText, "=> _previewTransitionAnimationController.AddPreviewShellEntranceAnimations(storyboard, easing, beginMs, durationMs);");
        AssertContains(adapterText, "=> _previewTransitionAnimationController.ResetPreviewContentTransform();");
        AssertContains(adapterText, "FadeOutVideoFrameShadow(durationMs: 150);");
        AssertContains(adapterText, "FadeInVideoFrameShadow(delayMs: 0, durationMs: 400);");
        AssertContains(adapterText, "=> _previewTransitionAnimationController.PrepareStartupPresentation();");
        AssertContains(adapterText, "=> PreviewTransitionAnimationController.FadeInElement(element);");
        AssertContains(mainWindowText, "InitializePreviewTransitionAnimationController();");
        AssertContains(launchEntranceControllerText, "_context.AddPreviewShellEntranceAnimations(storyboard, easing, 900, 400);");
        AssertContains(controllerText, "internal sealed class PreviewTransitionAnimationController");
        AssertContains(controllerText, "public void AddPreviewShellEntranceAnimations(Storyboard storyboard, EasingFunctionBase easing, int beginMs, int durationMs)");
        AssertContains(controllerText, "public Task AnimatePreviewOutAsync()");
        AssertContains(controllerText, "public Task AnimatePreviewInAsync()");
        AssertContains(controllerText, "public void PrepareStartupPresentation()");
        AssertContains(controllerText, "public void RevealUnavailablePlaceholder()");
        AssertContains(controllerText, "public static void FadeOutElement(UIElement element)");
        AssertContains(controllerText, "private Task AnimatePreviewTransitionAsync(");
        AssertContains(controllerText, "private static Task BeginStoryboardAsync(");
        AssertContains(shadowAnimatorText, "internal static class CompositionShadowFadeAnimator");
        AssertContains(shadowAnimatorText, "public static void FadeIn(SpriteVisual? visual, int delayMs, int durationMs)");
        AssertContains(shadowAnimatorText, "public static void FadeOut(SpriteVisual? visual, int durationMs)");
        AssertContains(shadowAnimatorText, "if (visual == null) return;");
        AssertContains(shadowAnimatorText, "CreateScalarKeyFrameAnimation()");
        AssertContains(shadowAnimatorText, "animation.InsertKeyFrame(0f, 0f);");
        AssertContains(shadowAnimatorText, "animation.InsertKeyFrame(1f, 1f, compositor.CreateCubicBezierEasingFunction(new Vector2(0.25f, 0.1f), new Vector2(0.25f, 1f)));");
        AssertContains(shadowAnimatorText, "animation.DelayTime = TimeSpan.FromMilliseconds(delayMs);");
        AssertContains(shadowAnimatorText, "visual.StartAnimation(\"Opacity\", animation);");
        AssertDoesNotContain(adapterText, "private Task AnimatePreviewTransitionAsync(");
        AssertDoesNotContain(adapterText, "private static Task BeginStoryboardAsync(");

        return Task.CompletedTask;
    }

    private static Task PreviewStartupOverlay_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.PreviewStartupOverlay.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/PreviewStartupOverlayController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private PreviewStartupOverlayController _previewStartupOverlayController = null!;");
        AssertContains(adapterText, "private void InitializePreviewStartupOverlayController()");
        AssertContains(adapterText, "PreviewLoadingOverlay = PreviewLoadingOverlay,");
        AssertContains(adapterText, "FadeInElement = FadeInElement,");
        AssertContains(adapterText, "FadeOutElement = FadeOutElement,");
        AssertContains(adapterText, "=> _previewStartupOverlayController.Start();");
        AssertContains(adapterText, "=> _previewStartupOverlayController.Stop(_isPreviewReinitAnimating);");
        AssertContains(mainWindowText, "InitializePreviewStartupOverlayController();");
        AssertContains(controllerText, "internal sealed class PreviewStartupOverlayController");
        AssertContains(controllerText, "public void Start()");
        AssertContains(controllerText, "public void Stop(bool isPreviewReinitAnimating)");
        AssertContains(controllerText, "var ring = (ProgressRing)_context.PreviewLoadingOverlay.Children[0];");
        AssertContains(controllerText, "ring.IsActive = true;");
        AssertContains(controllerText, "ring.IsActive = false;");
        AssertContains(controllerText, "_context.PreviewLoadingOverlay.Visibility = Visibility.Collapsed;");
        AssertContains(controllerText, "_context.PreviewLoadingOverlay.Opacity = 1.0;");
        AssertContains(controllerText, "_context.FadeOutElement(_context.PreviewLoadingOverlay);");
        AssertDoesNotContain(adapterText, "var ring = (ProgressRing)");
        AssertDoesNotContain(adapterText, "PreviewLoadingOverlay.Visibility = Visibility.Collapsed;");

        return Task.CompletedTask;
    }

    private static Task PreviewFadeInReveal_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.PreviewFadeIn.cs").Replace("\r\n", "\n");
        var previewTransitionText = ReadRepoFile("Sussudio/MainWindow.PreviewTransitions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/PreviewFadeInController.cs").Replace("\r\n", "\n");

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

    private static Task RecordButtonWidthAnimation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var recordingPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedRecording.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.RecordButtonAnimations.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/RecordButtonAnimationController.cs").Replace("\r\n", "\n");
        var recordingPresentationText = ReadRepoFile("Sussudio/Controllers/RecordingStatePresentationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private RecordButtonAnimationController _recordButtonAnimationController = null!;");
        AssertContains(adapterText, "private void InitializeRecordButtonAnimationController()");
        AssertContains(adapterText, "RecordButton = RecordButton,");
        AssertContains(adapterText, "=> _recordButtonAnimationController.AnimateWidth(from, to, onCompleted);");
        AssertContains(mainWindowText, "InitializeRecordButtonAnimationController();");
        AssertContains(propertyChangedText, "TryHandleRecordingPropertyChanged(propertyName)");
        AssertContains(recordingPropertyChangedText, "HandleRecordingChanged();");
        AssertContains(recordingPropertyChangedText, "_recordingStatePresentationController.HandleRecordingChanged();");
        AssertContains(recordingPresentationText, "public required Action<double, double, Action?> AnimateRecordButtonWidth { get; init; }");
        AssertContains(recordingPresentationText, "_context.AnimateRecordButtonWidth(36, targetWidth, null);");
        AssertContains(recordingPresentationText, "_context.AnimateRecordButtonWidth(currentWidth, 36, () =>");
        AssertContains(controllerText, "internal sealed class RecordButtonAnimationController");
        AssertContains(controllerText, "public void AnimateWidth(double from, double to, Action? onCompleted = null)");
        AssertContains(controllerText, "Storyboard.SetTarget(anim, _context.RecordButton);");
        AssertContains(controllerText, "_context.RecordButton.Width = to == 36 ? 36 : double.NaN;");
        AssertDoesNotContain(adapterText, "Storyboard.SetTarget(anim, RecordButton);");

        return Task.CompletedTask;
    }

    private static Task RecordingStatePresentation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedRecording.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/RecordingStatePresentationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private RecordingStatePresentationController _recordingStatePresentationController = null!;");
        AssertContains(adapterText, "private void InitializeRecordingStatePresentationController()");
        AssertContains(adapterText, "RecordingGlowBorder = RecordingGlowBorder,");
        AssertContains(adapterText, "RecordButtonStartingContent = RecordButtonStartingContent,");
        AssertContains(adapterText, "AudioRecordToggle = AudioRecordToggle,");
        AssertContains(adapterText, "AnalogAudioGainSlider = AnalogAudioGainSlider,");
        AssertContains(adapterText, "AnimateRecordButtonWidth = AnimateRecordButtonWidth,");
        AssertContains(adapterText, "ApplyWindowTitle = ApplyWindowTitle,");
        AssertContains(adapterText, "=> _recordingStatePresentationController.HandleRecordingChanged();");
        AssertContains(adapterText, "=> _recordingStatePresentationController.HandleRecordingTransitioningChanged();");
        AssertContains(adapterText, "=> _recordingStatePresentationController.HandleFfmpegMissingChanged();");
        AssertContains(mainWindowText, "InitializeRecordingStatePresentationController();");
        AssertContains(controllerText, "internal sealed class RecordingStatePresentationController");
        AssertContains(controllerText, "public void HandleRecordingChanged()");
        AssertContains(controllerText, "_context.RecordingGlowBorder.Opacity = 1.0;");
        AssertContains(controllerText, "_context.RecordingGlowPulseStoryboard.Begin();");
        AssertContains(controllerText, "_context.ResetAudioMeterVisuals();");
        AssertContains(controllerText, "_context.RecordButtonStartingContent.IsActive = false;");
        AssertContains(controllerText, "_context.RecordButton.Padding = new Thickness(12, 0, 12, 0);");
        AssertContains(controllerText, "_context.RecordButton.Width = 36;");
        AssertContains(controllerText, "_context.AudioInputComboBox.IsEnabled = viewModel.IsCustomAudioInputEnabled && !isRecording;");
        AssertContains(controllerText, "string.Equals(viewModel.SelectedDeviceAudioMode, DeviceAudioMode.Analog, StringComparison.OrdinalIgnoreCase)");
        AssertContains(controllerText, "_context.RecPulseStoryboard.Begin();");
        AssertContains(controllerText, "_context.ApplyWindowTitle();");
        AssertContains(controllerText, "public void HandleRecordingTransitioningChanged()");
        AssertContains(controllerText, "_context.RecordButton.IsEnabled = !viewModel.IsRecordingTransitioning;");
        AssertContains(controllerText, "_context.RecordButtonRecordingContent.Visibility = Visibility.Collapsed;");
        AssertContains(controllerText, "public void HandleFfmpegMissingChanged()");
        AssertContains(controllerText, "!_context.ViewModel.IsFfmpegMissing &&");
        AssertDoesNotContain(adapterText, "RecordingGlowPulseStoryboard.Begin();");
        AssertDoesNotContain(adapterText, "RecordButtonStartingContent.IsActive = false;");
        AssertDoesNotContain(adapterText, "AudioInputComboBox.IsEnabled = ViewModel.IsCustomAudioInputEnabled");
        AssertDoesNotContain(adapterText, "RecordButton.IsEnabled = !ViewModel.IsFfmpegMissing");

        return Task.CompletedTask;
    }

}
