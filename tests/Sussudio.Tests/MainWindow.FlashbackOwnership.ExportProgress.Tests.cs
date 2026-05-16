using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackExportProgressPresentation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedFlashback.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.FlashbackExportProgressPresentation.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackExportProgressPresentationController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private FlashbackExportProgressPresentationController _flashbackExportProgressPresentationController = null!;");
        AssertContains(adapterText, "private void InitializeFlashbackExportProgressPresentationController()");
        AssertContains(adapterText, "FlashbackExportProgressBar = FlashbackExportProgressBar,");
        AssertContains(adapterText, "=> _flashbackExportProgressPresentationController.UpdateProgress(progress);");
        AssertContains(adapterText, "=> _flashbackExportProgressPresentationController.UpdateExporting(isExporting);");
        AssertContains(mainWindowText, "InitializeFlashbackExportProgressPresentationController();");
        AssertContains(propertyChangedText, "TryHandleFlashbackPropertyChanged(propertyName)");
        AssertContains(flashbackPropertyChangedText, "HandleFlashbackExportProgressChanged();");
        AssertContains(flashbackPropertyChangedText, "HandleFlashbackExportingChanged();");
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
