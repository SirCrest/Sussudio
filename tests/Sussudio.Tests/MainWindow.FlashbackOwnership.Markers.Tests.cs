using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackMarkerPresentation_LivesInController()
    {
        var flashbackText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.FlashbackMarkers.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackMarkerPresentationController.cs").Replace("\r\n", "\n");
        var playbackCoordinatorText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPlaybackUiCoordinator.cs").Replace("\r\n", "\n");
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
        AssertContains(flashbackText, "UpdateMarkers = UpdateFlashbackMarkers,");
        AssertContains(playbackCoordinatorText, "_context.UpdateMarkers();");
        AssertContains(propertyChangedText, "TryHandleFlashbackPropertyChanged(propertyName)");
        AssertContains(flashbackPropertyChangedText, "HandleFlashbackRangeChanged();");
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
}
