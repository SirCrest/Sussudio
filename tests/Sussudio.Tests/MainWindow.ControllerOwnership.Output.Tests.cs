using System.Threading.Tasks;
using System.Reflection;

static partial class Program
{
    private static Task OutputPathDisplay_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.Bindings.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChanged.cs").Replace("\r\n", "\n");
        var outputPropertyChangedText = ReadRepoFile("Sussudio/MainWindow.PropertyChangedOutput.cs").Replace("\r\n", "\n");
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
        AssertContains(propertyChangedText, "TryHandleOutputPropertyChanged(propertyName)");
        AssertContains(outputPropertyChangedText, "UpdateOutputPathDisplay();");
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
        AssertContains(adapterText, "GetWindowHandle = () => _hwnd,");
        AssertContains(adapterText, "GetOutputPath = () => ViewModel.OutputPath,");
        AssertContains(adapterText, "SetOutputPath = path => ViewModel.OutputPath = path,");
        AssertContains(adapterText, "SetStatusText = text => ViewModel.StatusText = text,");
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
        AssertContains(controllerText, "public async Task BrowseAsync()");
        AssertContains(controllerText, "var picker = new FolderPicker();");
        AssertContains(controllerText, "picker.SuggestedStartLocation = PickerLocationId.VideosLibrary;");
        AssertContains(controllerText, "picker.FileTypeFilter.Add(\"*\");");
        AssertContains(controllerText, "WinRT.Interop.InitializeWithWindow.Initialize(picker, _context.GetWindowHandle());");
        AssertContains(controllerText, "await picker.PickSingleFolderAsync();");
        AssertContains(controllerText, "_context.SetOutputPath(folder.Path);");
        AssertContains(controllerText, "_context.SetStatusText($\"Error selecting folder: {ex.Message}\");");
        AssertContains(controllerText, "public Task OpenRecordingsFolderIfAvailableAsync()");
        AssertContains(controllerText, "var path = _context.GetOutputPath();");
        AssertContains(controllerText, "string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)");
        AssertContains(controllerText, "return _context.OpenRecordingsFolderAsync();");
        AssertDoesNotContain(adapterText, "ViewModel.BrowseOutputPathAsync()");
        AssertDoesNotContain(adapterText, "System.IO.Directory.Exists(path)");
        AssertDoesNotContain(controllerText, "Sussudio.ViewModels");
        AssertDoesNotContain(controllerText, "MainViewModel");

        return Task.CompletedTask;
    }

    private static Task PreviewScreenshotButtonWorkflow_LivesInController()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.PreviewScreenshot.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/PreviewScreenshotController.cs").Replace("\r\n", "\n");
        var policyText = ReadRepoFile("Sussudio/Controllers/PreviewScreenshotPlanPolicy.cs").Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md").Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md").Replace("\r\n", "\n");

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
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.PreviewRequiredStatusText");
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.Create(");
        AssertContains(controllerText, "Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)");
        AssertContains(controllerText, "DateTime.Now");
        AssertContains(controllerText, "Directory.CreateDirectory(plan.OutputDirectory);");
        AssertContains(controllerText, "CapturePreviewFrameAsync(plan.FilePath)");
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.FormatSavedStatus(plan.FilePath)");
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.FormatSavedLog(plan.FilePath, result.CapturedWidth, result.CapturedHeight)");
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.FormatFailedStatus(result.Message)");
        AssertContains(controllerText, "PreviewScreenshotPlanPolicy.FormatFailedLog(result.Message)");
        AssertContains(policyText, "internal static class PreviewScreenshotPlanPolicy");
        AssertContains(policyText, "PreviewRequiredStatusText = \"Start preview before capturing a screenshot\"");
        AssertContains(policyText, "Path.Combine(picturesFolder, DefaultOutputFolderName)");
        AssertContains(policyText, "$\"Screenshot_{timestamp.ToString(TimestampFormat)}.png\"");
        AssertContains(policyText, "=> $\"Screenshot saved: {Path.GetFileName(filePath)}\";");
        AssertContains(policyText, "=> $\"Screenshot failed: {message}\";");
        AssertContains(policyText, "=> $\"SCREENSHOT_SAVED path={filePath} width={capturedWidth} height={capturedHeight}\";");
        AssertContains(policyText, "=> $\"SCREENSHOT_FAILED reason={message}\";");
        AssertContains(policyText, "internal readonly record struct PreviewScreenshotPlan(string OutputDirectory, string FilePath);");
        AssertContains(controllerText, "_context.ScreenshotButton.IsEnabled = false;");
        AssertContains(controllerText, "_context.ScreenshotButton.IsEnabled = true;");
        AssertContains(agentMapText, "PreviewScreenshotPlanPolicy.cs");
        AssertContains(cleanupPlanText, "PreviewScreenshotPlanPolicy.cs");
        AssertDoesNotContain(adapterText, "Directory.CreateDirectory(outputDir);");
        AssertDoesNotContain(adapterText, "CapturePreviewFrameAsync(");
        AssertDoesNotContain(controllerText, "Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), \"Sussudio\")");
        AssertDoesNotContain(controllerText, "Screenshot saved: {Path.GetFileName(filePath)}");
        AssertDoesNotContain(policyText, "Button");
        AssertDoesNotContain(policyText, "CapturePreviewFrameAsync");
        AssertDoesNotContain(policyText, "Directory.CreateDirectory");
        AssertDoesNotContain(policyText, "Logger.Log");

        return Task.CompletedTask;
    }

    private static Task PreviewScreenshotPlanPolicy_PreservesPathAndTextContracts()
    {
        var policyType = RequireType("Sussudio.Controllers.PreviewScreenshotPlanPolicy");
        var create = policyType.GetMethod("Create", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.Create was not found.");
        var savedStatus = policyType.GetMethod("FormatSavedStatus", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.FormatSavedStatus was not found.");
        var failedStatus = policyType.GetMethod("FormatFailedStatus", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.FormatFailedStatus was not found.");
        var savedLog = policyType.GetMethod("FormatSavedLog", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.FormatSavedLog was not found.");
        var failedLog = policyType.GetMethod("FormatFailedLog", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.FormatFailedLog was not found.");
        var previewRequired = policyType.GetField("PreviewRequiredStatusText", BindingFlags.Static | BindingFlags.Public)
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.PreviewRequiredStatusText was not found.");

        var timestamp = new System.DateTime(2026, 5, 16, 14, 3, 4);
        var fallbackPlan = create.Invoke(null, new object?[] { "   ", "C:\\Users\\crest\\Pictures", timestamp })
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.Create returned null.");
        var configuredPlan = create.Invoke(null, new object?[] { "D:\\Captures", "C:\\Users\\crest\\Pictures", timestamp })
            ?? throw new InvalidOperationException("PreviewScreenshotPlanPolicy.Create returned null.");
        var fallbackPath = GetStringProperty(fallbackPlan, "FilePath");
        var configuredPath = GetStringProperty(configuredPlan, "FilePath");

        AssertEqual(
            "Start preview before capturing a screenshot",
            previewRequired.GetValue(null)?.ToString(),
            "preview screenshot not-previewing status");
        AssertEqual(
            "C:\\Users\\crest\\Pictures\\Sussudio",
            GetStringProperty(fallbackPlan, "OutputDirectory"),
            "preview screenshot fallback output directory");
        AssertEqual(
            "C:\\Users\\crest\\Pictures\\Sussudio\\Screenshot_2026-05-16_14-03-04.png",
            fallbackPath,
            "preview screenshot fallback path");
        AssertEqual(
            "D:\\Captures",
            GetStringProperty(configuredPlan, "OutputDirectory"),
            "preview screenshot configured output directory");
        AssertEqual(
            "D:\\Captures\\Screenshot_2026-05-16_14-03-04.png",
            configuredPath,
            "preview screenshot configured path");
        AssertEqual(
            "Screenshot saved: Screenshot_2026-05-16_14-03-04.png",
            savedStatus.Invoke(null, new object[] { configuredPath })?.ToString(),
            "preview screenshot saved status");
        AssertEqual(
            "SCREENSHOT_SAVED path=D:\\Captures\\Screenshot_2026-05-16_14-03-04.png width=1280 height=720",
            savedLog.Invoke(null, new object[] { configuredPath, 1280, 720 })?.ToString(),
            "preview screenshot saved log");
        AssertEqual(
            "Screenshot failed: renderer unavailable",
            failedStatus.Invoke(null, new object[] { "renderer unavailable" })?.ToString(),
            "preview screenshot failed status");
        AssertEqual(
            "SCREENSHOT_FAILED reason=renderer unavailable",
            failedLog.Invoke(null, new object[] { "renderer unavailable" })?.ToString(),
            "preview screenshot failed log");

        return Task.CompletedTask;
    }
}
