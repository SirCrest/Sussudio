using System.Threading.Tasks;

static partial class Program
{
    private static Task FlashbackExportProgressPresentation_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedFlashback.cs").Replace("\r\n", "\n");
        var flashbackPropertyChangedControllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPropertyChangedController.cs").Replace("\r\n", "\n");
        var flashbackText = ReadRepoFile("Sussudio/MainWindow.Flashback.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackExportProgressPresentationController.cs").Replace("\r\n", "\n");

        AssertContains(flashbackText, "private FlashbackExportProgressPresentationController _flashbackExportProgressPresentationController = null!;");
        AssertContains(flashbackText, "private void InitializeFlashbackExportProgressPresentationController()");
        AssertContains(flashbackText, "FlashbackExportProgressBar = FlashbackExportProgressBar,");
        AssertContains(flashbackText, "=> _flashbackExportProgressPresentationController.UpdateProgress(progress);");
        AssertContains(flashbackText, "=> _flashbackExportProgressPresentationController.UpdateExporting(isExporting);");
        AssertContains(mainWindowText, "InitializeFlashbackExportProgressPresentationController();");
        AssertContains(mainWindowText, "InitializeFlashbackPropertyChangedController();");
        AssertContains(propertyChangedText, "TryHandleFlashbackPropertyChanged(propertyName)");
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
