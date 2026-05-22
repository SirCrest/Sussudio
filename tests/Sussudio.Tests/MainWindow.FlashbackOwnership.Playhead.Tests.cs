using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackPlayheadMotion_LivesInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var mainWindowText = ReadMainWindowCompositionSource();
        var scrubText = ReadMainWindowFlashbackAdapterSource();
        var scrubControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackScrubInteractionController.cs").Replace("\r\n", "\n");
        var playheadText = ReadMainWindowFlashbackAdapterSource();
        var pollingAdapterText = ReadMainWindowFlashbackAdapterSource();
        var controllerRootText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.cs").Replace("\r\n", "\n");
        var controllerText = controllerRootText;
        var playbackCoordinatorText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPlaybackUiCoordinator.cs").Replace("\r\n", "\n");

        AssertContains(playheadText, "XAML-facing Flashback playhead motion adapter");
        AssertContains(playheadText, "private FlashbackPlayheadMotionController _flashbackPlayheadMotionController = null!;");
        AssertContains(playheadText, "private void InitializeFlashbackPlayheadMotionController()");
        AssertContains(playheadText, "IsScrubbing = () => _flashbackScrubInteractionController.IsScrubbing,");
        AssertContains(playheadText, "private void RequestFlashbackPlayheadSnapOnNextUpdate()");
        AssertContains(playheadText, "private void PositionFlashbackMagneticPlayhead(double x, double trackWidth)");
        AssertContains(playheadText, "private void RefreshFlashbackCtiMotion(string reason)");
        AssertContains(playheadText, "=> _flashbackPlayheadMotionController.RefreshCtiMotion(reason);");
        AssertContains(playheadText, "private void StopFlashbackCtiAnchorTimer()");
        AssertContains(playheadText, "=> _flashbackPlayheadMotionController.StopCtiAnchorTimer();");
        AssertContains(mainWindowText, "InitializeFlashbackPlayheadMotionController();");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Interactions.cs")),
            "Flashback playhead adapter lives in the consolidated Flashback interaction adapter");
        AssertOccursBefore(mainWindowText, "InitializeFlashbackScrubInteractionController();", "InitializeFlashbackPlayheadMotionController();");
        AssertOccursBefore(mainWindowText, "InitializeFlashbackPlayheadMotionController();", "InitializeFlashbackTimelineController();");
        AssertContains(controllerRootText, "internal sealed class FlashbackPlayheadMotionControllerContext");
        AssertContains(controllerRootText, "internal sealed class FlashbackPlayheadMotionController");
        AssertContains(controllerRootText, "private enum FlashbackPlayheadMotion");
        AssertContains(controllerRootText, "private Visual? _flashbackPlayheadVisual;");
        AssertContains(controllerRootText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertContains(controllerRootText, "private CompositionEasingFunction? _flashbackPlayheadEaseLinear;");
        AssertContains(controllerRootText, "private bool _snapFlashbackPlayheadOnNextUpdate;");
        AssertContains(controllerRootText, "public void RequestSnapOnNextUpdate()");
        AssertContains(controllerRootText, "public void PositionMagneticPlayhead(double x, double trackWidth)");
        AssertContains(controllerText, "public void RefreshCtiMotion(string reason)");
        AssertContains(controllerText, "public void StopCtiAnchorTimer()");
        AssertContains(controllerText, "private void StartFlashbackCtiAnchorTimer()");
        AssertContains(controllerText, "private void FlashbackCtiAnchorTimer_Tick(DispatcherQueueTimer sender, object args)");
        AssertContains(controllerText, "FlashbackTimelineGeometry.IsUsableTrackDimension(trackW)");
        AssertContains(controllerText, "state == FlashbackPlaybackState.Live");
        AssertContains(controllerText, "SnapPlayheadVisualsToFraction(1.0, trackW);");
        AssertContains(controllerText, "StartLinearPlayheadExtrapolation(");
        AssertContains(controllerText, "RefreshCtiMotion(\"anchor_tick\");");
        AssertContains(controllerText, "FLASHBACK_CTI_ANCHOR_TICK_FAIL");
        AssertContains(controllerText, "private void EnsureFlashbackPlayheadVisuals()");
        AssertContains(controllerText, "private void PositionFlashbackPlayhead(double x, double trackWidth, FlashbackPlayheadMotion motion)");
        AssertContains(controllerText, "private void StartLinearPlayheadExtrapolation(");
        AssertContains(controllerText, "private static void StartLinearKeyframe(");
        AssertContains(controllerText, "private void SnapPlayheadVisualsToFraction(");
        AssertContains(controllerText, "private void AnimateFlashbackPlayheadX(");
        AssertContains(controllerText, "private static void SnapFlashbackPlayheadX(");
        AssertContains(controllerText, "ElementCompositionPreview.SetIsTranslationEnabled(_context.Playhead, true);");
        AssertContains(controllerText, "Canvas.SetLeft(_context.Playhead, 0);");
        AssertContains(controllerText, "var labelX = Math.Clamp(x - labelW / 2, 0, Math.Max(0, trackWidth - labelW));");
        AssertContains(controllerText, "var lineX = (float)(x - 1);");
        AssertContains(controllerText, "var handleX = (float)(x - 5);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackPlayheadMotionController.Cti.cs")),
            "Flashback playhead CTI partial is consolidated into the motion controller root");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Flashback", "FlashbackPlayheadMotionController.Visuals.cs")),
            "Flashback playhead visuals partial is consolidated into the motion controller root");
        AssertContains(scrubText, "PositionMagneticPlayhead = PositionFlashbackMagneticPlayhead,");
        AssertContains(scrubControllerText, "_context.PositionMagneticPlayhead(x, width);");
        AssertContains(playbackCoordinatorText, "_context.RefreshCtiMotion(\"state_change\");");
        AssertContains(pollingAdapterText, "StopFlashbackCtiAnchorTimer();");
        AssertContains(playbackCoordinatorText, "_context.RequestPlayheadSnapOnNextUpdate();");
        AssertDoesNotContain(playheadText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertDoesNotContain(playheadText, "private void StartLinearPlayheadExtrapolation(");
        AssertDoesNotContain(playheadText, "FLASHBACK_CTI_ANCHOR_TICK_FAIL");
        AssertDoesNotContain(flashbackText, "private enum FlashbackPlayheadMotion");
        AssertDoesNotContain(flashbackText, "private Visual? _flashbackPlayheadVisual;");
        AssertDoesNotContain(flashbackText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertDoesNotContain(flashbackText, "private void StartLinearPlayheadExtrapolation(");

        return Task.CompletedTask;
    }
}
