using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackPlayheadMotion_LivesInController()
    {
        var flashbackText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var scrubText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs").Replace("\r\n", "\n");
        var scrubControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackScrubInteractionController.cs").Replace("\r\n", "\n");
        var playheadText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs").Replace("\r\n", "\n");
        var pollingAdapterText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs").Replace("\r\n", "\n");
        var controllerRootText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.cs").Replace("\r\n", "\n");
        var controllerCtiText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.Cti.cs").Replace("\r\n", "\n");
        var controllerVisualsText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.Visuals.cs").Replace("\r\n", "\n");
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
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.FlashbackPlayhead.cs")),
            "Flashback playhead adapter is consolidated into MainWindow.Flashback.cs");
        AssertOccursBefore(mainWindowText, "InitializeFlashbackScrubInteractionController();", "InitializeFlashbackPlayheadMotionController();");
        AssertOccursBefore(mainWindowText, "InitializeFlashbackPlayheadMotionController();", "InitializeFlashbackTimelineController();");
        AssertContains(controllerRootText, "internal sealed class FlashbackPlayheadMotionControllerContext");
        AssertContains(controllerRootText, "internal sealed partial class FlashbackPlayheadMotionController");
        AssertContains(controllerRootText, "private enum FlashbackPlayheadMotion");
        AssertContains(controllerRootText, "private Visual? _flashbackPlayheadVisual;");
        AssertContains(controllerRootText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertContains(controllerRootText, "private CompositionEasingFunction? _flashbackPlayheadEaseLinear;");
        AssertContains(controllerRootText, "private bool _snapFlashbackPlayheadOnNextUpdate;");
        AssertContains(controllerRootText, "public void RequestSnapOnNextUpdate()");
        AssertContains(controllerRootText, "public void PositionMagneticPlayhead(double x, double trackWidth)");
        AssertContains(controllerCtiText, "public void RefreshCtiMotion(string reason)");
        AssertContains(controllerCtiText, "public void StopCtiAnchorTimer()");
        AssertContains(controllerCtiText, "private void StartFlashbackCtiAnchorTimer()");
        AssertContains(controllerCtiText, "private void FlashbackCtiAnchorTimer_Tick(DispatcherQueueTimer sender, object args)");
        AssertContains(controllerCtiText, "FlashbackTimelineGeometry.IsUsableTrackDimension(trackW)");
        AssertContains(controllerCtiText, "state == FlashbackPlaybackState.Live");
        AssertContains(controllerCtiText, "SnapPlayheadVisualsToFraction(1.0, trackW);");
        AssertContains(controllerCtiText, "StartLinearPlayheadExtrapolation(");
        AssertContains(controllerCtiText, "RefreshCtiMotion(\"anchor_tick\");");
        AssertContains(controllerCtiText, "FLASHBACK_CTI_ANCHOR_TICK_FAIL");
        AssertContains(controllerVisualsText, "private void EnsureFlashbackPlayheadVisuals()");
        AssertContains(controllerVisualsText, "private void PositionFlashbackPlayhead(double x, double trackWidth, FlashbackPlayheadMotion motion)");
        AssertContains(controllerVisualsText, "private void StartLinearPlayheadExtrapolation(");
        AssertContains(controllerVisualsText, "private static void StartLinearKeyframe(");
        AssertContains(controllerVisualsText, "private void SnapPlayheadVisualsToFraction(");
        AssertContains(controllerVisualsText, "private void AnimateFlashbackPlayheadX(");
        AssertContains(controllerVisualsText, "private static void SnapFlashbackPlayheadX(");
        AssertContains(controllerVisualsText, "ElementCompositionPreview.SetIsTranslationEnabled(_context.Playhead, true);");
        AssertContains(controllerVisualsText, "Canvas.SetLeft(_context.Playhead, 0);");
        AssertContains(controllerVisualsText, "var labelX = Math.Clamp(x - labelW / 2, 0, Math.Max(0, trackWidth - labelW));");
        AssertContains(controllerVisualsText, "var lineX = (float)(x - 1);");
        AssertContains(controllerVisualsText, "var handleX = (float)(x - 5);");
        AssertDoesNotContain(controllerRootText, "public void RefreshCtiMotion(string reason)");
        AssertDoesNotContain(controllerRootText, "private void EnsureFlashbackPlayheadVisuals()");
        AssertDoesNotContain(controllerRootText, "FLASHBACK_CTI_ANCHOR_TICK_FAIL");
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
