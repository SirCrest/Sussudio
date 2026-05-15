using System.Threading.Tasks;
using System.Reflection;

static partial class Program
{
    private static Task OutputPathDisplay_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.OutputPathDisplay.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/OutputPathDisplayController.cs").Replace("\r\n", "\n");
        var formatterText = ReadRepoFile("Sussudio/Controllers/OutputPathDisplayTextFormatter.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private OutputPathDisplayController _outputPathDisplayController = null!;");
        AssertContains(adapterText, "private void InitializeOutputPathDisplayController()");
        AssertContains(adapterText, "OutputPathTextBox = OutputPathTextBox,");
        AssertContains(adapterText, "GetOutputPath = () => ViewModel.OutputPath,");
        AssertContains(adapterText, "private void AttachOutputPathDisplay()");
        AssertContains(adapterText, "=> _outputPathDisplayController.Attach();");
        AssertContains(adapterText, "private void UpdateOutputPathDisplay()");
        AssertContains(adapterText, "=> _outputPathDisplayController.Update();");
        AssertContains(mainWindowText, "InitializeOutputPathDisplayController();");
        AssertContains(bindingsText, "AttachOutputPathDisplay();");
        AssertContains(propertyChangedText, "UpdateOutputPathDisplay();");
        AssertContains(controllerText, "internal sealed class OutputPathDisplayController");
        AssertContains(controllerText, "public void Attach()");
        AssertContains(controllerText, "public void Update()");
        AssertContains(controllerText, "ToolTipService.SetToolTip(_context.OutputPathTextBox, path);");
        AssertContains(controllerText, "OutputPathDisplayTextFormatter.Format(path, availableWidth);");
        AssertContains(formatterText, "internal static class OutputPathDisplayTextFormatter");
        AssertContains(formatterText, "public static string Format(string path, double availableWidth)");
        AssertContains(formatterText, "var maxChars = (int)((availableWidth - 20) / 7);");
        AssertContains(formatterText, "var parts = path.Split('\\\\', '/');");
        AssertContains(formatterText, "var candidate = $\"{root}\\\\...\\\\{tail}\";");
        AssertDoesNotContain(controllerText, "var maxChars = (int)((availableWidth - 20) / 7);");
        AssertDoesNotContain(controllerText, "var parts = path.Split('\\\\', '/');");
        AssertDoesNotContain(controllerText, "var candidate = $\"{root}\\\\...\\\\{tail}\";");
        AssertDoesNotContain(bindingsText, "OutputPathTextBox.SizeChanged += (s, e) => UpdateOutputPathDisplay();");
        AssertDoesNotContain(bindingsText, "private void UpdateOutputPathDisplay()");
        AssertDoesNotContain(bindingsText, "ToolTipService.SetToolTip(OutputPathTextBox, path);");

        return Task.CompletedTask;
    }

    private static Task OutputPathDisplayTextFormatter_PreservesTruncationPolicy()
    {
        var formatterType = RequireType("Sussudio.Controllers.OutputPathDisplayTextFormatter");
        var format = formatterType.GetMethod("Format", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("OutputPathDisplayTextFormatter.Format was not found.");

        string Format(string path, double availableWidth)
        {
            return format.Invoke(null, new object[] { path, availableWidth })?.ToString()
                ?? throw new InvalidOperationException("OutputPathDisplayTextFormatter.Format returned null.");
        }

        AssertEqual(
            "C:\\captures\\clip.mp4",
            Format("C:\\captures\\clip.mp4", 240),
            "Full output path fits when width has enough characters");
        AssertEqual(
            "C:\\captures\\clip.mp4",
            Format("C:\\captures\\clip.mp4", 0),
            "Zero output path width preserves full path");
        AssertEqual(
            "C:\\captures\\clip.mp4",
            Format("C:\\captures\\clip.mp4", -10),
            "Negative output path width preserves full path");
        AssertEqual(
            "clip-with-a-very-long-name.mp4",
            Format("clip-with-a-very-long-name.mp4", 40),
            "Simple path without folder segments stays unchanged");
        AssertEqual(
            "C:\\...\\session\\captures\\clip.mp4",
            Format("C:\\users\\crest\\videos\\session\\captures\\clip.mp4", 250),
            "Deep output path keeps root and fitting tail segments");
        AssertEqual(
            "C:\\...\\clip.mp4",
            Format("C:\\users\\crest\\videos\\session\\captures\\clip.mp4", 80),
            "Deep output path falls back to root and filename");

        return Task.CompletedTask;
    }

    private static Task OutputPathButtonActions_LiveInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.OutputPathActions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/OutputPathActionController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private OutputPathActionController _outputPathActionController = null!;");
        AssertContains(adapterText, "private void InitializeOutputPathActionController()");
        AssertContains(adapterText, "ViewModel = ViewModel,");
        AssertContains(adapterText, "OpenRecordingsFolderAsync = () => OpenRecordingsFolderAsync()");
        AssertContains(adapterText, "private Task BrowseOutputPathFromButtonAsync()");
        AssertContains(adapterText, "=> _outputPathActionController.BrowseAsync();");
        AssertContains(adapterText, "private Task OpenRecordingsFolderFromButtonAsync()");
        AssertContains(adapterText, "=> _outputPathActionController.OpenRecordingsFolderIfAvailableAsync();");
        AssertContains(adapterText, "private void BrowseButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(adapterText, "_ = RunUiEventHandlerAsync(() => BrowseOutputPathFromButtonAsync(), nameof(BrowseButton_Click));");
        AssertContains(adapterText, "private void OpenRecordingsButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(adapterText, "_ = RunUiEventHandlerAsync(() => OpenRecordingsFolderFromButtonAsync(), nameof(OpenRecordingsButton_Click));");
        AssertContains(mainWindowText, "InitializeOutputPathActionController();");
        AssertContains(controllerText, "internal sealed class OutputPathActionController");
        AssertContains(controllerText, "public Task BrowseAsync()");
        AssertContains(controllerText, "=> _context.ViewModel.BrowseOutputPathAsync();");
        AssertContains(controllerText, "public Task OpenRecordingsFolderIfAvailableAsync()");
        AssertContains(controllerText, "string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)");
        AssertContains(controllerText, "return _context.OpenRecordingsFolderAsync();");
        AssertDoesNotContain(adapterText, "ViewModel.BrowseOutputPathAsync()");
        AssertDoesNotContain(adapterText, "System.IO.Directory.Exists(path)");

        return Task.CompletedTask;
    }

    private static Task PreviewScreenshotButtonWorkflow_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.PreviewScreenshot.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/PreviewScreenshotController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private PreviewScreenshotController _previewScreenshotController = null!;");
        AssertContains(adapterText, "private void InitializePreviewScreenshotController()");
        AssertContains(adapterText, "ViewModel = ViewModel,");
        AssertContains(adapterText, "ScreenshotButton = ScreenshotButton,");
        AssertContains(adapterText, "private Task CapturePreviewScreenshotAsync()");
        AssertContains(adapterText, "=> _previewScreenshotController.CaptureAsync();");
        AssertContains(adapterText, "private void ScreenshotButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(adapterText, "_ = RunUiEventHandlerAsync(() => CapturePreviewScreenshotAsync(), nameof(ScreenshotButton_Click));");
        AssertContains(mainWindowText, "InitializePreviewScreenshotController();");
        AssertContains(controllerText, "internal sealed class PreviewScreenshotController");
        AssertContains(controllerText, "public async Task CaptureAsync()");
        AssertContains(controllerText, "Start preview before capturing a screenshot");
        AssertContains(controllerText, "Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), \"Sussudio\")");
        AssertContains(controllerText, "Directory.CreateDirectory(outputDir);");
        AssertContains(controllerText, "CapturePreviewFrameAsync(filePath)");
        AssertContains(controllerText, "Screenshot saved: {Path.GetFileName(filePath)}");
        AssertContains(controllerText, "SCREENSHOT_SAVED");
        AssertContains(controllerText, "SCREENSHOT_FAILED");
        AssertContains(controllerText, "_context.ScreenshotButton.IsEnabled = false;");
        AssertContains(controllerText, "_context.ScreenshotButton.IsEnabled = true;");
        AssertDoesNotContain(adapterText, "Directory.CreateDirectory(outputDir);");
        AssertDoesNotContain(adapterText, "CapturePreviewFrameAsync(filePath)");

        return Task.CompletedTask;
    }
}
