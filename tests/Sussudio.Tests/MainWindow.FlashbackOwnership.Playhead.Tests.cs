using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackPlayheadMotion_LivesInController()
    {
        var flashbackText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var scrubText = ReadRepoFile("Sussudio/MainWindow.FlashbackScrub.cs").Replace("\r\n", "\n");
        var scrubControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackScrubInteractionController.cs").Replace("\r\n", "\n");
        var playheadText = ReadRepoFile("Sussudio/MainWindow.FlashbackPlayhead.cs").Replace("\r\n", "\n");
        var ctiMotionText = ReadRepoFile("Sussudio/MainWindow.FlashbackPlayhead.CtiMotion.cs").Replace("\r\n", "\n");
        var pollingAdapterText = ReadRepoFile("Sussudio/MainWindow.FlashbackPolling.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPlayheadMotionController.cs").Replace("\r\n", "\n");
        var playbackCoordinatorText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPlaybackUiCoordinator.cs").Replace("\r\n", "\n");

        AssertContains(playheadText, "XAML-facing Flashback playhead motion adapter");
        AssertContains(playheadText, "private FlashbackPlayheadMotionController _flashbackPlayheadMotionController = null!;");
        AssertContains(playheadText, "private void InitializeFlashbackPlayheadMotionController()");
        AssertContains(playheadText, "IsScrubbing = () => _flashbackScrubInteractionController.IsScrubbing,");
        AssertContains(playheadText, "private void RequestFlashbackPlayheadSnapOnNextUpdate()");
        AssertContains(playheadText, "private void PositionFlashbackMagneticPlayhead(double x, double trackWidth)");
        AssertContains(ctiMotionText, "XAML-facing Flashback CTI motion adapter");
        AssertContains(ctiMotionText, "=> _flashbackPlayheadMotionController.RefreshCtiMotion(reason);");
        AssertContains(ctiMotionText, "=> _flashbackPlayheadMotionController.StopCtiAnchorTimer();");
        AssertContains(mainWindowText, "InitializeFlashbackPlayheadMotionController();");
        AssertOccursBefore(mainWindowText, "InitializeFlashbackScrubInteractionController();", "InitializeFlashbackPlayheadMotionController();");
        AssertOccursBefore(mainWindowText, "InitializeFlashbackPlayheadMotionController();", "InitializeFlashbackTimelineController();");
        AssertContains(controllerText, "internal sealed class FlashbackPlayheadMotionControllerContext");
        AssertContains(controllerText, "internal sealed class FlashbackPlayheadMotionController");
        AssertContains(controllerText, "private enum FlashbackPlayheadMotion");
        AssertContains(controllerText, "private Visual? _flashbackPlayheadVisual;");
        AssertContains(controllerText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertContains(controllerText, "private CompositionEasingFunction? _flashbackPlayheadEaseLinear;");
        AssertContains(controllerText, "private bool _snapFlashbackPlayheadOnNextUpdate;");
        AssertContains(controllerText, "public void RequestSnapOnNextUpdate()");
        AssertContains(controllerText, "public void PositionMagneticPlayhead(double x, double trackWidth)");
        AssertContains(controllerText, "public void RefreshCtiMotion(string reason)");
        AssertContains(controllerText, "public void StopCtiAnchorTimer()");
        AssertContains(controllerText, "private void PositionFlashbackPlayhead(double x, double trackWidth, FlashbackPlayheadMotion motion)");
        AssertContains(controllerText, "StartLinearPlayheadExtrapolation(");
        AssertContains(controllerText, "private static void StartLinearKeyframe(");
        AssertContains(controllerText, "private void SnapPlayheadVisualsToFraction(");
        AssertContains(controllerText, "FLASHBACK_CTI_ANCHOR_TICK_FAIL");
        AssertContains(scrubText, "PositionMagneticPlayhead = PositionFlashbackMagneticPlayhead,");
        AssertContains(scrubControllerText, "_context.PositionMagneticPlayhead(x, width);");
        AssertContains(playbackCoordinatorText, "_context.RefreshCtiMotion(\"state_change\");");
        AssertContains(pollingAdapterText, "StopFlashbackCtiAnchorTimer();");
        AssertContains(playbackCoordinatorText, "_context.RequestPlayheadSnapOnNextUpdate();");
        AssertDoesNotContain(playheadText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertDoesNotContain(playheadText, "private void RefreshFlashbackCtiMotion(string reason)");
        AssertDoesNotContain(playheadText, "private void StartLinearPlayheadExtrapolation(");
        AssertDoesNotContain(playheadText, "FLASHBACK_CTI_ANCHOR_TICK_FAIL");
        AssertDoesNotContain(ctiMotionText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertDoesNotContain(ctiMotionText, "private void StartLinearPlayheadExtrapolation(");
        AssertDoesNotContain(ctiMotionText, "FLASHBACK_CTI_ANCHOR_TICK_FAIL");
        AssertDoesNotContain(flashbackText, "private enum FlashbackPlayheadMotion");
        AssertDoesNotContain(flashbackText, "private Visual? _flashbackPlayheadVisual;");
        AssertDoesNotContain(flashbackText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertDoesNotContain(flashbackText, "private void RefreshFlashbackCtiMotion(string reason)");

        return Task.CompletedTask;
    }
}
