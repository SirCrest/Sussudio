using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackMarkerPresentation_LivesInController()
    {
        var flashbackText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs").Replace("\r\n", "\n");
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
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
        AssertContains(propertyChangedText, "TryHandleFlashbackPropertyChanged(propertyName)");
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
}
