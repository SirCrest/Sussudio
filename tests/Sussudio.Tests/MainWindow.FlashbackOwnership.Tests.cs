using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackPollingTimers_LiveInController()
    {
        var flashbackText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var pollingAdapterText = ReadRepoFile("Sussudio/MainWindow.FlashbackPolling.cs").Replace("\r\n", "\n");
        var timelineAdapterText = ReadRepoFile("Sussudio/MainWindow.FlashbackTimeline.cs").Replace("\r\n", "\n");
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/FlashbackPollingController.cs").Replace("\r\n", "\n");

        AssertContains(pollingAdapterText, "private FlashbackPollingController _flashbackPollingController = null!;");
        AssertContains(pollingAdapterText, "private void InitializeFlashbackPollingController()");
        AssertContains(pollingAdapterText, "IsWindowClosing = () => _isWindowClosing,");
        AssertContains(pollingAdapterText, "=> _flashbackPollingController.StartStatusPolling();");
        AssertContains(pollingAdapterText, "_flashbackPollingController.StopStatusPolling();");
        AssertContains(pollingAdapterText, "StopFlashbackCtiAnchorTimer();");
        AssertContains(pollingAdapterText, "=> _flashbackPollingController.StartPlaybackPolling();");
        AssertContains(pollingAdapterText, "=> _flashbackPollingController.StopPlaybackPolling();");
        AssertContains(mainWindowText, "InitializeFlashbackPollingController();");
        AssertContains(timelineAdapterText, "StartStatusPolling = StartFlashbackStatusPolling,");
        AssertContains(shutdownCleanupText, "StopFlashbackStatusPolling();");
        AssertContains(flashbackText, "StartFlashbackPlaybackPolling();");
        AssertContains(flashbackText, "StopFlashbackPlaybackPolling();");
        AssertContains(controllerText, "internal sealed class FlashbackPollingController");
        AssertContains(controllerText, "private DispatcherQueueTimer? _statusTimer;");
        AssertContains(controllerText, "private DispatcherQueueTimer? _playbackTimer;");
        AssertContains(controllerText, "public void StartStatusPolling()");
        AssertContains(controllerText, "public void StopStatusPolling()");
        AssertContains(controllerText, "public void StartPlaybackPolling()");
        AssertContains(controllerText, "public void StopPlaybackPolling()");
        AssertContains(controllerText, "_context.ViewModel.UpdateFlashbackBufferStatus();");
        AssertContains(controllerText, "_context.ViewModel.FlashbackPlaybackPosition = playback.PlaybackPosition;");
        AssertContains(controllerText, "FLASHBACK_STATUS_TIMER_FAIL");
        AssertContains(controllerText, "FLASHBACK_PLAYBACK_TIMER_FAIL");
        AssertDoesNotContain(flashbackText, "private DispatcherQueueTimer? _flashbackStatusTimer;");
        AssertDoesNotContain(flashbackText, "private void FlashbackStatusTimer_Tick(");
        AssertDoesNotContain(flashbackText, "private void FlashbackPlaybackTimer_Tick(");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlayheadMotion_LivesInFocusedPartial()
    {
        var flashbackText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs").Replace("\r\n", "\n");
        var scrubText = ReadRepoFile("Sussudio/MainWindow.FlashbackScrub.cs").Replace("\r\n", "\n");
        var playheadText = ReadRepoFile("Sussudio/MainWindow.FlashbackPlayhead.cs").Replace("\r\n", "\n");
        var pollingAdapterText = ReadRepoFile("Sussudio/MainWindow.FlashbackPolling.cs").Replace("\r\n", "\n");

        AssertContains(playheadText, "Flashback current-time-indicator visuals");
        AssertContains(playheadText, "private enum FlashbackPlayheadMotion");
        AssertContains(playheadText, "private Visual? _flashbackPlayheadVisual;");
        AssertContains(playheadText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertContains(playheadText, "private void RefreshFlashbackCtiMotion(string reason)");
        AssertContains(playheadText, "private void PositionFlashbackPlayhead(double x, double trackWidth, FlashbackPlayheadMotion motion)");
        AssertContains(playheadText, "StartLinearPlayheadExtrapolation(");
        AssertContains(playheadText, "FLASHBACK_CTI_ANCHOR_TICK_FAIL");
        AssertContains(scrubText, "PositionFlashbackPlayhead(x, width, FlashbackPlayheadMotion.Magnetic);");
        AssertContains(flashbackText, "RefreshFlashbackCtiMotion(\"state_change\");");
        AssertContains(pollingAdapterText, "StopFlashbackCtiAnchorTimer();");
        AssertDoesNotContain(flashbackText, "private enum FlashbackPlayheadMotion");
        AssertDoesNotContain(flashbackText, "private Visual? _flashbackPlayheadVisual;");
        AssertDoesNotContain(flashbackText, "private DispatcherQueueTimer? _flashbackCtiAnchorTimer;");
        AssertDoesNotContain(flashbackText, "private void RefreshFlashbackCtiMotion(string reason)");

        return Task.CompletedTask;
    }

    private static Task FlashbackMarkerPresentation_LivesInController()
    {
        var flashbackText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.FlashbackMarkers.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/FlashbackMarkerPresentationController.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedFlashback.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private FlashbackMarkerPresentationController _flashbackMarkerPresentationController = null!;");
        AssertContains(adapterText, "private void InitializeFlashbackMarkerPresentationController()");
        AssertContains(adapterText, "ScrubArea = FlashbackScrubArea,");
        AssertContains(adapterText, "InPointMarker = FlashbackInPointMarker,");
        AssertContains(adapterText, "OutPointMarker = FlashbackOutPointMarker,");
        AssertContains(adapterText, "SelectionRegion = FlashbackSelectionRegion,");
        AssertContains(adapterText, "=> FlashbackMarkerPresentationController.FormatDuration(ts);");
        AssertContains(adapterText, "=> _flashbackMarkerPresentationController.UpdateMarkers(");
        AssertContains(adapterText, "ViewModel.FlashbackBufferFilledDuration,");
        AssertContains(adapterText, "ViewModel.FlashbackInPoint,");
        AssertContains(adapterText, "ViewModel.FlashbackOutPoint);");
        AssertContains(mainWindowText, "InitializeFlashbackMarkerPresentationController();");
        AssertContains(controllerText, "internal sealed class FlashbackMarkerPresentationController");
        AssertContains(controllerText, "public static string FormatDuration(TimeSpan value)");
        AssertContains(controllerText, "public void UpdateMarkers(TimeSpan bufferDuration, TimeSpan? inPoint, TimeSpan? outPoint)");
        AssertContains(controllerText, "_context.InPointMarker.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "_context.OutPointMarker.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "_context.SelectionRegion.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "Canvas.SetLeft(_context.SelectionRegion, selLeft);");
        AssertContains(flashbackText, "UpdateFlashbackMarkers();");
        AssertContains(flashbackText, "FormatFlashbackDuration(bufferDuration)");
        AssertContains(propertyChangedText, "HandleFlashbackRangeChanged();");
        AssertContains(flashbackPropertyChangedText, "Flashback-specific ViewModel property projections");
        AssertContains(flashbackPropertyChangedText, "UpdateFlashbackMarkers();");
        AssertDoesNotContain(flashbackText, "private void UpdateFlashbackMarkers()");
        AssertDoesNotContain(flashbackText, "private static string FormatFlashbackDuration(TimeSpan ts)");
        AssertDoesNotContain(adapterText, "Canvas.SetLeft(");
        AssertDoesNotContain(adapterText, "FlashbackInPointMarker.Visibility = Visibility.Visible;");
        AssertDoesNotContain(adapterText, "FlashbackSelectionRegion.Visibility = Visibility.Visible;");

        return Task.CompletedTask;
    }
}
