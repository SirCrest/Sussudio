using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackTimelineTrackLayout_LivesInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var timelineAdapterText = ReadMainWindowFlashbackAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackTimelineController.cs").Replace("\r\n", "\n");
        var animationControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackTimelineAnimationController.cs").Replace("\r\n", "\n");
        var playbackCoordinatorText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPlaybackUiCoordinator.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(timelineAdapterText, "FlashbackTrackBackground = FlashbackTrackBackground,");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Timeline.cs")),
            "Flashback timeline adapter lives in the focused Flashback timeline partial");
        AssertContains(timelineAdapterText, "FlashbackScrubArea = FlashbackScrubArea,");
        AssertContains(timelineAdapterText, "FlashbackPlayhead = FlashbackPlayhead,");
        AssertContains(timelineAdapterText, "FlashbackLiveEdge = FlashbackLiveEdge,");
        AssertContains(controllerText, "public required FrameworkElement FlashbackTrackBackground { get; init; }");
        AssertContains(controllerText, "public required FrameworkElement FlashbackScrubArea { get; init; }");
        AssertContains(controllerText, "public required FrameworkElement FlashbackPlayhead { get; init; }");
        AssertContains(controllerText, "public required FrameworkElement FlashbackLiveEdge { get; init; }");
        AssertContains(controllerText, "public void ApplyTrackSize(double width, double height)");
        AssertContains(controllerText, "_context.FlashbackTrackBackground.Width = width;");
        AssertContains(controllerText, "_context.FlashbackTrackBackground.Height = height;");
        AssertContains(controllerText, "_context.FlashbackScrubArea.Width = width;");
        AssertContains(controllerText, "_context.FlashbackScrubArea.Height = height;");
        AssertContains(controllerText, "_context.FlashbackPlayhead.Height = height;");
        AssertContains(controllerText, "_context.FlashbackLiveEdge.Height = height;");
        AssertContains(controllerText, "Canvas.SetLeft(_context.FlashbackLiveEdge, width - 2);");
        AssertContains(controllerText, "private readonly FlashbackTimelineAnimationController _animationController;");
        AssertContains(controllerText, "_animationController.Animate(show: true);");
        AssertContains(controllerText, "_animationController.Animate(show: false);");
        AssertContains(animationControllerText, "internal sealed class FlashbackTimelineAnimationController");
        AssertContains(animationControllerText, "private Storyboard? _timelineStoryboard;");
        AssertContains(animationControllerText, "public bool IsAnimating { get; private set; }");
        AssertContains(animationControllerText, "public void CollapseImmediately()");
        AssertContains(animationControllerText, "public void ResetForFullScreen()");
        AssertContains(animationControllerText, "private void CompleteAnimation(Storyboard storyboard)");
        AssertDoesNotContain(controllerText, "private Storyboard? _timelineStoryboard;");
        AssertDoesNotContain(controllerText, "new DoubleAnimation");
        AssertContains(flashbackText, "private void FlashbackTrack_SizeChanged(object sender, SizeChangedEventArgs e)");
        AssertContains(flashbackText, "=> _flashbackPlaybackUiCoordinator.HandleTrackSizeChanged(e.NewSize.Width, e.NewSize.Height);");
        AssertContains(playbackCoordinatorText, "public void HandleTrackSizeChanged(double width, double height)");
        AssertContains(playbackCoordinatorText, "_context.ApplyTrackSize(width, height);");
        AssertOccursBefore(playbackCoordinatorText, "_context.ApplyTrackSize(width, height);", "_context.RequestPlayheadSnapOnNextUpdate();");
        AssertOccursBefore(playbackCoordinatorText, "_context.RequestPlayheadSnapOnNextUpdate();", "UpdatePosition();");
        AssertOccursBefore(playbackCoordinatorText, "UpdatePosition();", "_context.UpdateMarkers();");
        AssertOccursBefore(playbackCoordinatorText, "_context.UpdateMarkers();", "_context.RefreshCtiMotion(\"size_changed\");");
        AssertContains(agentMapText, "timeline track layout sizing");
        AssertContains(agentMapText, "FlashbackTimelineAnimationController.cs");
        AssertDoesNotContain(flashbackText, "FlashbackTrackBackground.Width =");
        AssertDoesNotContain(flashbackText, "FlashbackTrackBackground.Height =");
        AssertDoesNotContain(flashbackText, "FlashbackScrubArea.Width =");
        AssertDoesNotContain(flashbackText, "FlashbackScrubArea.Height =");
        AssertDoesNotContain(flashbackText, "FlashbackPlayhead.Height =");
        AssertDoesNotContain(flashbackText, "FlashbackLiveEdge.Height =");
        AssertDoesNotContain(flashbackText, "Canvas.SetLeft(FlashbackLiveEdge");

        return Task.CompletedTask;
    }

    internal static Task FlashbackMarkerPresentation_LivesInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var mainWindowText = ReadMainWindowCompositionSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackMarkerPresentationController.cs").Replace("\r\n", "\n");
        var playbackCoordinatorText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPlaybackUiCoordinator.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedFlashback.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPropertyChangedController.cs").Replace("\r\n", "\n");

        AssertContains(flashbackText, "private FlashbackMarkerPresentationController _flashbackMarkerPresentationController = null!;");
        AssertContains(flashbackText, "private void InitializeFlashbackMarkerPresentationController()");
        AssertContains(flashbackText, "ScrubArea = FlashbackScrubArea,");
        AssertContains(flashbackText, "InPointMarker = FlashbackInPointMarker,");
        AssertContains(flashbackText, "OutPointMarker = FlashbackOutPointMarker,");
        AssertContains(flashbackText, "SelectionRegion = FlashbackSelectionRegion,");
        AssertContains(flashbackText, "=> _flashbackMarkerPresentationController.UpdateMarkers(");
        AssertContains(flashbackText, "ViewModel.FlashbackBufferFilledDuration,");
        AssertContains(flashbackText, "ViewModel.FlashbackInPoint,");
        AssertContains(flashbackText, "ViewModel.FlashbackOutPoint);");
        AssertContains(mainWindowText, "InitializeFlashbackMarkerPresentationController();");
        AssertContains(controllerText, "internal sealed class FlashbackMarkerPresentationController");
        AssertContains(controllerText, "public static string FormatDuration(TimeSpan value)");
        AssertContains(controllerText, "public void UpdateMarkers(TimeSpan bufferDuration, TimeSpan? inPoint, TimeSpan? outPoint)");
        AssertContains(controllerText, "_context.InPointMarker.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "_context.OutPointMarker.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "_context.SelectionRegion.Visibility = Visibility.Visible;");
        AssertContains(controllerText, "Canvas.SetLeft(_context.SelectionRegion, selLeft);");
        AssertContains(flashbackText, "UpdateMarkers = UpdateFlashbackMarkers,");
        AssertContains(playbackCoordinatorText, "_context.UpdateMarkers();");
        AssertContains(propertyChangedText, "TryHandleFlashback = TryHandleFlashbackPropertyChanged");
        AssertContains(flashbackPropertyChangedText, "UpdateRangeMarkers = UpdateFlashbackMarkers,");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackInPoint):");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackOutPoint):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.UpdateRangeMarkers();");
        AssertDoesNotContain(flashbackText, "private static string FormatFlashbackDuration(TimeSpan ts)");
        AssertDoesNotContain(flashbackText, "Canvas.SetLeft(");
        AssertDoesNotContain(flashbackText, "FlashbackInPointMarker.Visibility = Visibility.Visible;");
        AssertDoesNotContain(flashbackText, "FlashbackSelectionRegion.Visibility = Visibility.Visible;");

        return Task.CompletedTask;
    }

    internal static Task FlashbackExportProgressPresentation_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedFlashback.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPropertyChangedController.cs").Replace("\r\n", "\n");
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackExportProgressPresentationController.cs").Replace("\r\n", "\n");

        AssertContains(flashbackText, "private FlashbackExportProgressPresentationController _flashbackExportProgressPresentationController = null!;");
        AssertContains(flashbackText, "private void InitializeFlashbackExportProgressPresentationController()");
        AssertContains(flashbackText, "FlashbackExportProgressBar = FlashbackExportProgressBar,");
        AssertContains(flashbackText, "=> _flashbackExportProgressPresentationController.UpdateProgress(progress);");
        AssertContains(flashbackText, "=> _flashbackExportProgressPresentationController.UpdateExporting(isExporting);");
        AssertContains(mainWindowText, "InitializeFlashbackExportProgressPresentationController();");
        AssertContains(mainWindowText, "InitializeFlashbackPropertyChangedController();");
        AssertContains(propertyChangedText, "TryHandleFlashback = TryHandleFlashbackPropertyChanged");
        AssertContains(flashbackPropertyChangedText, "UpdateExportProgress = UpdateFlashbackExportProgress,");
        AssertContains(flashbackPropertyChangedText, "UpdateExportingPresentation = UpdateFlashbackExportingPresentation,");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.FlashbackExportProgress):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.UpdateExportProgress(_context.GetExportProgress());");
        AssertContains(flashbackPropertyChangedControllerText, "case nameof(MainViewModel.IsFlashbackExporting):");
        AssertContains(flashbackPropertyChangedControllerText, "_context.UpdateExportingPresentation(_context.IsExporting());");
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
