using System;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task OutputPathDisplay_LivesInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var bindingsText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var propertyChangedText = ReadRepoFile("Sussudio/MainWindow.xaml.cs").Replace("\r\n", "\n");
        var adapterText = ReadRepoFile("Sussudio/MainWindow.ButtonActions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Recording/Output/OutputPathController.cs").Replace("\r\n", "\n");
        const string formatterMarker = "internal static class OutputPathDisplayTextFormatter";
        var formatterStart = controllerText.IndexOf(formatterMarker, System.StringComparison.Ordinal);
        if (formatterStart < 0)
        {
            throw new System.InvalidOperationException("OutputPathDisplayTextFormatter was not found in OutputPathController.cs.");
        }

        var formatterText = controllerText[formatterStart..];

        AssertContains(adapterText, "private OutputPathController _outputPathController = null!;");
        AssertContains(adapterText, "private void InitializeOutputPathController()");
        AssertContains(adapterText, "OutputPathTextBox = OutputPathTextBox,");
        AssertContains(adapterText, "GetOutputPath = () => ViewModel.OutputPath,");
        AssertContains(adapterText, "private void AttachOutputPathDisplay()");
        AssertContains(adapterText, "=> _outputPathController.AttachDisplay();");
        AssertContains(adapterText, "private void UpdateOutputPathDisplay()");
        AssertContains(adapterText, "=> _outputPathController.UpdateDisplay();");
        AssertContains(mainWindowText, "InitializeOutputPathController();");
        AssertContains(bindingsText, "AttachOutputPathDisplay();");
        AssertContains(propertyChangedText, "TryHandleOutput = TryHandleOutputPropertyChanged,");
        AssertContains(adapterText, "private bool TryHandleOutputPropertyChanged(string propertyName)");
        AssertContains(adapterText, "=> _outputPathController.TryHandlePropertyChanged(propertyName);");
        AssertDoesNotContain(adapterText, "case nameof(MainViewModel.OutputPath):");
        AssertContains(controllerText, "internal sealed class OutputPathController");
        AssertContains(controllerText, "public void AttachDisplay()");
        AssertContains(controllerText, "public void UpdateDisplay()");
        AssertContains(controllerText, "public bool TryHandlePropertyChanged(string propertyName)");
        AssertContains(controllerText, "case nameof(MainViewModel.OutputPath):");
        AssertContains(controllerText, "UpdateDisplay();");
        AssertContains(controllerText, "ToolTipService.SetToolTip(_context.OutputPathTextBox, path);");
        AssertContains(controllerText, "OutputPathDisplayTextFormatter.Format(path, availableWidth);");
        AssertContains(formatterText, "internal static class OutputPathDisplayTextFormatter");
        AssertContains(formatterText, "public static string Format(string path, double availableWidth)");
        AssertContains(formatterText, "var maxChars = (int)((availableWidth - 20) / 7);");
        AssertContains(formatterText, "var parts = path.Split('\\\\', '/');");
        AssertContains(formatterText, "var candidate = $\"{root}\\\\...\\\\{tail}\";");
        AssertDoesNotContain(adapterText, "var maxChars = (int)((availableWidth - 20) / 7);");
        AssertDoesNotContain(adapterText, "var parts = path.Split('\\\\', '/');");
        AssertDoesNotContain(adapterText, "var candidate = $\"{root}\\\\...\\\\{tail}\";");
        AssertDoesNotContain(bindingsText, "OutputPathTextBox.SizeChanged += (s, e) => UpdateOutputPathDisplay();");
        AssertDoesNotContain(bindingsText, "private void UpdateOutputPathDisplay()");
        AssertDoesNotContain(bindingsText, "ToolTipService.SetToolTip(OutputPathTextBox, path);");

        return Task.CompletedTask;
    }

    internal static Task OutputPathDisplayTextFormatter_PreservesTruncationPolicy()
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

    internal static Task OutputPathButtonActions_LiveInController()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var adapterText = ReadRepoFile("Sussudio/MainWindow.ButtonActions.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Recording/Output/OutputPathController.cs").Replace("\r\n", "\n");

        AssertContains(adapterText, "private OutputPathController _outputPathController = null!;");
        AssertContains(adapterText, "private void InitializeOutputPathController()");
        AssertContains(adapterText, "GetWindowHandle = () => _hwnd,");
        AssertContains(adapterText, "GetOutputPath = () => ViewModel.OutputPath,");
        AssertContains(adapterText, "SetOutputPath = path => ViewModel.OutputPath = path,");
        AssertContains(adapterText, "SetStatusText = text => ViewModel.StatusText = text,");
        AssertContains(adapterText, "OpenRecordingsFolderAsync = () => OpenRecordingsFolderAsync()");
        AssertContains(adapterText, "private Task BrowseOutputPathFromButtonAsync()");
        AssertContains(adapterText, "=> _outputPathController.BrowseAsync();");
        AssertContains(adapterText, "private Task OpenRecordingsFolderFromButtonAsync()");
        AssertContains(adapterText, "=> _outputPathController.OpenRecordingsFolderIfAvailableAsync();");
        AssertContains(adapterText, "private void BrowseButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(adapterText, "_ = RunUiEventHandlerAsync(() => BrowseOutputPathFromButtonAsync(), nameof(BrowseButton_Click));");
        AssertContains(adapterText, "private void OpenRecordingsButton_Click(object sender, RoutedEventArgs e)");
        AssertContains(adapterText, "_ = RunUiEventHandlerAsync(() => OpenRecordingsFolderFromButtonAsync(), nameof(OpenRecordingsButton_Click));");
        AssertContains(mainWindowText, "InitializeOutputPathController();");
        AssertContains(controllerText, "internal sealed class OutputPathController");
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
        AssertContains(controllerText, "case nameof(MainViewModel.OutputPath):");

        return Task.CompletedTask;
    }


    internal static Task MainViewModelOutputPathSelection_LivesInFocusedPartial()
    {
        var mainViewModelFiles = ReadMainViewModelCodeFiles();
        var mainViewModelText = string.Join("\n", mainViewModelFiles.Values);
        var outputPathSelectionPath = Path.Combine(
            GetRepoRoot(),
            "Sussudio",
            "ViewModels",
            "MainViewModel.OutputPathSelection.cs");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertEqual(false, File.Exists(outputPathSelectionPath), "MainViewModel output picker partial retired");
        AssertDoesNotContain(mainViewModelText, "BrowseOutputPathAsync");
        AssertDoesNotContain(mainViewModelText, "FolderPicker");
        AssertDoesNotContain(mainViewModelText, "FileTypeFilter");
        AssertContains(agentMapText, "`Sussudio/Controllers/Recording/Output/OutputPathController.cs` owns recording output-");
        AssertContains(agentMapText, "`MainViewModel.RecordingState.cs` owns the stable recording facade:");
        AssertContains(agentMapText, "bridge, recording option selections, output path, counters, and observable");
        AssertContains(cleanupPlanText, "Recording output-path textbox, tooltip, resize-event updates, browse, and");
        AssertDoesNotContain(agentMapText, "`MainViewModel.OutputPathSelection.cs` owns output folder picker and path assignment.");
        AssertDoesNotContain(cleanupPlanText, "`MainViewModel.OutputPathSelection.cs`");

        return Task.CompletedTask;
    }

    internal static Task OutputDriveSpacePresentationBuilder_InvalidPathReturnsEmpty()
    {
        var builderType = RequireType("Sussudio.ViewModels.OutputDriveSpacePresentationBuilder");
        var buildMethod = builderType.GetMethod(
            "Build",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("OutputDriveSpacePresentationBuilder.Build was not found.");

        AssertEqual(
            "",
            buildMethod.Invoke(null, new object?[] { "\0" }),
            "Output drive space invalid path fallback");

        return Task.CompletedTask;
    }

    internal static Task OutputDriveSpacePresentationBuilder_LivesInFocusedHelper()
    {
        var bridgeText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.RecordingState.cs")
            .Replace("\r\n", "\n");
        var builderText = ReadRepoFile("Sussudio/ViewModels/ViewModelBuilders.cs")
            .Replace("\r\n", "\n");
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md");

        AssertContains(bridgeText, "private void UpdateDiskSpace()");
        AssertContains(bridgeText, "DiskSpaceInfo = OutputDriveSpacePresentationBuilder.Build(OutputPath);");
        AssertDoesNotContain(bridgeText, "new DriveInfo(");
        AssertDoesNotContain(bridgeText, "Path.GetPathRoot(");
        AssertDoesNotContain(bridgeText, "Trace.TraceWarning(");
        AssertDoesNotContain(bridgeText, "Free: {freeGb:F1} GB");

        AssertContains(builderText, "internal static class OutputDriveSpacePresentationBuilder");
        AssertContains(builderText, "internal static string Build(string outputPath)");
        AssertContains(builderText, "new DriveInfo(Path.GetPathRoot(outputPath) ?? \"C:\");");
        AssertContains(builderText, "return $\"Free: {freeGb:F1} GB\";");
        AssertContains(builderText, "Trace.TraceWarning($\"Suppressed exception in MainViewModel.RefreshDiskSpace: {ex.Message}\");");
        AssertContains(builderText, "return \"\";");
        AssertDoesNotContain(builderText, "DiskSpaceInfo =");

        AssertContains(agentMapText, "`MainViewModel.RecordingState.cs` owns recording-runtime counters and the DiskSpaceInfo assignment bridge");
        AssertContains(agentMapText, "`Sussudio/ViewModels/ViewModelBuilders.cs` owns output drive probing");
        AssertContains(cleanupPlanText, "`ViewModelBuilders.cs`");

        return Task.CompletedTask;
    }
}
