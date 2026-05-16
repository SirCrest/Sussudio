using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackTimelineTrackLayout_LivesInController()
    {
        var flashbackText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs").Replace("\r\n", "\n");
        var timelineAdapterText = ReadRepoFile("Sussudio/MainWindow.FlashbackTimeline.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackTimelineController.cs").Replace("\r\n", "\n");
        var playbackCoordinatorText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPlaybackUiCoordinator.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");

        AssertContains(timelineAdapterText, "FlashbackTrackBackground = FlashbackTrackBackground,");
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
        AssertContains(flashbackText, "private void FlashbackTrack_SizeChanged(object sender, SizeChangedEventArgs e)");
        AssertContains(flashbackText, "=> _flashbackPlaybackUiCoordinator.HandleTrackSizeChanged(e.NewSize.Width, e.NewSize.Height);");
        AssertContains(playbackCoordinatorText, "public void HandleTrackSizeChanged(double width, double height)");
        AssertContains(playbackCoordinatorText, "_context.ApplyTrackSize(width, height);");
        AssertOccursBefore(playbackCoordinatorText, "_context.ApplyTrackSize(width, height);", "_context.RequestPlayheadSnapOnNextUpdate();");
        AssertOccursBefore(playbackCoordinatorText, "_context.RequestPlayheadSnapOnNextUpdate();", "UpdatePosition();");
        AssertOccursBefore(playbackCoordinatorText, "UpdatePosition();", "_context.UpdateMarkers();");
        AssertOccursBefore(playbackCoordinatorText, "_context.UpdateMarkers();", "_context.RefreshCtiMotion(\"size_changed\");");
        AssertContains(agentMapText, "timeline track layout sizing");
        AssertDoesNotContain(flashbackText, "FlashbackTrackBackground.Width =");
        AssertDoesNotContain(flashbackText, "FlashbackTrackBackground.Height =");
        AssertDoesNotContain(flashbackText, "FlashbackScrubArea.Width =");
        AssertDoesNotContain(flashbackText, "FlashbackScrubArea.Height =");
        AssertDoesNotContain(flashbackText, "FlashbackPlayhead.Height =");
        AssertDoesNotContain(flashbackText, "FlashbackLiveEdge.Height =");
        AssertDoesNotContain(flashbackText, "Canvas.SetLeft(FlashbackLiveEdge");

        return Task.CompletedTask;
    }
}
