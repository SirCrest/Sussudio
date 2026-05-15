using System;
using System.Reflection;
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
        AssertContains(propertyChangedText, "HandleFlashbackRangeChanged();");
        AssertContains(flashbackPropertyChangedText, "Flashback-specific ViewModel property projections");
        AssertContains(flashbackPropertyChangedText, "UpdateFlashbackMarkers();");
        AssertDoesNotContain(flashbackText, "private void UpdateFlashbackMarkers()");
        AssertDoesNotContain(flashbackText, "private static string FormatFlashbackDuration(TimeSpan ts)");
        AssertDoesNotContain(adapterText, "private static string FormatFlashbackDuration(TimeSpan ts)");
        AssertDoesNotContain(adapterText, "Canvas.SetLeft(");
        AssertDoesNotContain(adapterText, "FlashbackInPointMarker.Visibility = Visibility.Visible;");
        AssertDoesNotContain(adapterText, "FlashbackSelectionRegion.Visibility = Visibility.Visible;");

        return Task.CompletedTask;
    }

    private static Task FlashbackPlaybackPresentation_LivesInController()
    {
        var flashbackText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.FlashbackPlaybackPresentation.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/FlashbackPlaybackPresentationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private FlashbackPlaybackPresentationController _flashbackPlaybackPresentationController = null!;");
        AssertContains(adapterText, "private void InitializeFlashbackPlaybackPresentationController()");
        AssertContains(adapterText, "PlayPauseIcon = FlashbackPlayPauseIcon,");
        AssertContains(adapterText, "GoLiveButton = FlashbackGoLiveButton,");
        AssertContains(adapterText, "BufferDurationText = FlashbackBufferDurationText,");
        AssertContains(adapterText, "PlayheadTimeText = FlashbackPlayheadTimeText,");
        AssertContains(mainWindowText, "InitializeFlashbackPlaybackPresentationController();");
        AssertContains(controllerText, "internal sealed class FlashbackPlaybackPresentationController");
        AssertContains(controllerText, "public static string GetPlayPauseGlyph(FlashbackPlaybackState state)");
        AssertContains(controllerText, "public static bool IsGoLiveEnabled(FlashbackPlaybackState state)");
        AssertContains(controllerText, "public static string FormatPositionLabel(");
        AssertContains(controllerText, "\"\\uE769\"");
        AssertContains(controllerText, "\"\\uE768\"");
        AssertContains(controllerText, "return \"LIVE\";");
        AssertContains(controllerText, "return $\"-{FlashbackMarkerPresentationController.FormatDuration(gapFromLive)} / {totalText}\";");
        AssertContains(flashbackText, "_flashbackPlaybackPresentationController.UpdateState(state);");
        AssertContains(flashbackText, "StartFlashbackPlaybackPolling();");
        AssertContains(flashbackText, "StopFlashbackPlaybackPolling();");
        AssertContains(flashbackText, "RefreshFlashbackCtiMotion(\"state_change\");");
        AssertContains(flashbackText, "_flashbackPlaybackPresentationController.UpdateBufferFill(duration);");
        AssertContains(flashbackText, "_flashbackPlaybackPresentationController.UpdatePosition(");
        AssertContains(flashbackText, "RefreshFlashbackCtiMotion(\"position_change\");");
        AssertDoesNotContain(flashbackText, "FlashbackPlayPauseIcon.Glyph =");
        AssertDoesNotContain(flashbackText, "FlashbackGoLiveButton.IsEnabled =");
        AssertDoesNotContain(flashbackText, "FlashbackBufferDurationText.Text =");
        AssertDoesNotContain(flashbackText, "FlashbackPlayheadTimeText.Text =");

        var controllerType = RequireType("Sussudio.Controllers.FlashbackPlaybackPresentationController");
        var stateType = RequireType("Sussudio.Models.FlashbackPlaybackState");
        var getPlayPauseGlyph = controllerType.GetMethod("GetPlayPauseGlyph", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackPlaybackPresentationController.GetPlayPauseGlyph was not found.");
        var isGoLiveEnabled = controllerType.GetMethod("IsGoLiveEnabled", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackPlaybackPresentationController.IsGoLiveEnabled was not found.");
        var formatPositionLabel = controllerType.GetMethod("FormatPositionLabel", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("FlashbackPlaybackPresentationController.FormatPositionLabel was not found.");

        object State(string name) => Enum.Parse(stateType, name);

        AssertEqual("\uE769", getPlayPauseGlyph.Invoke(null, new[] { State("Playing") })?.ToString(), "playing glyph");
        AssertEqual("\uE769", getPlayPauseGlyph.Invoke(null, new[] { State("Live") })?.ToString(), "live glyph");
        AssertEqual("\uE768", getPlayPauseGlyph.Invoke(null, new[] { State("Paused") })?.ToString(), "paused glyph");
        AssertEqual("\uE768", getPlayPauseGlyph.Invoke(null, new[] { State("Scrubbing") })?.ToString(), "scrubbing glyph");
        AssertEqual(false, (bool)isGoLiveEnabled.Invoke(null, new[] { State("Live") })!, "live disables go-live button");
        AssertEqual(false, (bool)isGoLiveEnabled.Invoke(null, new[] { State("Disabled") })!, "disabled disables go-live button");
        AssertEqual(true, (bool)isGoLiveEnabled.Invoke(null, new[] { State("Paused") })!, "paused enables go-live button");
        AssertEqual(
            "LIVE",
            formatPositionLabel.Invoke(null, new object[] { State("Live"), TimeSpan.FromSeconds(125), TimeSpan.FromSeconds(5) })?.ToString(),
            "live position label");
        AssertEqual(
            "-0:05 / 2:05",
            formatPositionLabel.Invoke(null, new object[] { State("Paused"), TimeSpan.FromSeconds(125), TimeSpan.FromSeconds(5) })?.ToString(),
            "buffered position label");

        return Task.CompletedTask;
    }

    private static Task FlashbackExportProgressPresentation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedFlashback.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.FlashbackExportProgressPresentation.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/FlashbackExportProgressPresentationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private FlashbackExportProgressPresentationController _flashbackExportProgressPresentationController = null!;");
        AssertContains(adapterText, "private void InitializeFlashbackExportProgressPresentationController()");
        AssertContains(adapterText, "FlashbackExportProgressBar = FlashbackExportProgressBar,");
        AssertContains(adapterText, "=> _flashbackExportProgressPresentationController.UpdateProgress(progress);");
        AssertContains(adapterText, "=> _flashbackExportProgressPresentationController.UpdateExporting(isExporting);");
        AssertContains(mainWindowText, "InitializeFlashbackExportProgressPresentationController();");
        AssertContains(propertyChangedText, "HandleFlashbackExportProgressChanged();");
        AssertContains(propertyChangedText, "HandleFlashbackExportingChanged();");
        AssertContains(flashbackPropertyChangedText, "UpdateFlashbackExportProgress(ViewModel.FlashbackExportProgress);");
        AssertContains(flashbackPropertyChangedText, "UpdateFlashbackExportingPresentation(ViewModel.IsFlashbackExporting);");
        AssertContains(controllerText, "internal sealed class FlashbackExportProgressPresentationController");
        AssertContains(controllerText, "public void UpdateProgress(double progress)");
        AssertContains(controllerText, "_context.FlashbackExportProgressBar.Value = progress;");
        AssertContains(controllerText, "public void UpdateExporting(bool isExporting)");
        AssertContains(controllerText, "_context.FlashbackExportProgressBar.Visibility = isExporting");
        AssertContains(controllerText, "? Visibility.Visible");
        AssertContains(controllerText, ": Visibility.Collapsed;");
        AssertContains(controllerText, "if (!isExporting)");
        AssertContains(controllerText, "_context.FlashbackExportProgressBar.Value = 0;");
        AssertDoesNotContain(flashbackPropertyChangedText, "FlashbackExportProgressBar.Value = ViewModel.FlashbackExportProgress;");
        AssertDoesNotContain(flashbackPropertyChangedText, "FlashbackExportProgressBar.Visibility = ViewModel.IsFlashbackExporting");

        return Task.CompletedTask;
    }
}
