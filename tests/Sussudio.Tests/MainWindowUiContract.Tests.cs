using System.Text.RegularExpressions;

static partial class Program
{
    internal static Task MainWindowAutomationIds_CoverAgentCriticalSurface()
    {
        var xaml = ReadRepoFile("Sussudio/MainWindow.xaml").Replace("\r\n", "\n");
        var requiredIds = new[]
        {
            "PreviewBorder",
            "PreviewPlayerElement",
            "PreviewImage",
            "PreviewLoadingOverlay",
            "NoDevicePlaceholder",
            "DiskWarningInfoBar",
            "SettingsOverlayPanel",
            "DeviceComboBox",
            "ApplyDeviceButton",
            "RefreshButton",
            "DeviceAudioModeComboBox",
            "AnalogAudioGainSlider",
            "AudioInputComboBox",
            "MicrophoneComboBox",
            "VideoFormatComboBox",
            "ResolutionComboBox",
            "FrameRateComboBox",
            "FormatComboBox",
            "QualityComboBox",
            "PresetComboBox",
            "CustomBitrateNumberBox",
            "OutputPathTextBox",
            "BrowseButton",
            "FlashbackEnabledToggle",
            "FlashbackBufferDurationCombo",
            "FlashbackApplyButton",
            "FlashbackGpuDecodeToggle",
            "FlashbackTimelinePanel",
            "FlashbackScrubArea",
            "FlashbackInButton",
            "FlashbackOutButton",
            "FlashbackClearButton",
            "FlashbackPlayPauseButton",
            "FlashbackGoLiveButton",
            "FlashbackExportButton",
            "FlashbackSaveLast5mButton",
            "FlashbackExportProgressBar",
            "ControlBarBorder",
            "SettingsToggleButton",
            "OpenRecordingsButton",
            "ScreenshotButton",
            "RecordButton",
            "PreviewButton",
            "HdrToggle",
            "AudioRecordToggle",
            "TrueHdrPreviewToggle",
            "AudioPreviewToggle",
            "StatsToggle",
            "FrameTimeOverlayToggle",
            "FlashbackToggle",
            "FullScreenButton",
            "FullScreenControlsOverlay",
            "SplashOverlay",
            "StatsDockPanel",
            "Stats_SessionStateValue",
            "Stats_SummaryCaptureValue",
            "Stats_SummaryPreviewValue",
            "Stats_SummaryRendererFpsValue",
            "Stats_SummaryVisualFpsValue",
            "Stats_SummaryLatencyValue",
            "Stats_SourceFormatValue",
            "Stats_PreviewFpsValue",
            "Stats_PipelineLatencyValue",
            "FrameTimeOverlay",
            "FrameTime_SourceValue",
            "FrameTime_VisualValue",
            "FrameTime_PreviewValue",
            "FrameTime_LatencyValue",
            "FrameTime_StatusValue",
            "StatusTextBlock",
            "RecordingTimeTextBlock",
            "LiveResolutionTextBlock",
            "LiveFrameRateTextBlock",
            "LivePixelFormatTextBlock",
            "PreviewVolumeSlider",
            "MicVolumeSlider"
        };

        var matches = Regex.Matches(
                xaml,
                "AutomationProperties\\.AutomationId=\"(?<id>[^\"]+)\"",
                RegexOptions.CultureInvariant)
            .Select(match => match.Groups["id"].Value)
            .ToArray();

        foreach (var id in requiredIds)
        {
            AssertEqual(1, matches.Count(candidate => string.Equals(candidate, id, StringComparison.Ordinal)), id);
        }

        var duplicates = matches
            .GroupBy(id => id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => $"{group.Key} x{group.Count()}")
            .ToArray();
        if (duplicates.Length > 0)
        {
            throw new InvalidOperationException(
                $"MainWindow automation IDs must be unique. Duplicates: {string.Join(", ", duplicates)}");
        }

        return Task.CompletedTask;
    }

    internal static Task MainWindowFullScreenAutomation_AwaitsTransitionTask()
    {
        var fullScreenSource = ReadMainWindowShellChromeAdapterSource();
        var fullScreenControllerRootSource = ReadRepoFile("Sussudio/Controllers/FullScreen/FullScreenController.cs")
            .Replace("\r\n", "\n");
        var dispatchingSource = ReadRepoFile("Sussudio/MainWindow.ShellChrome.Composition.cs")
            .Replace("\r\n", "\n");
        var dispatchControllerSource = ReadRepoFile("Sussudio/Controllers/Window/WindowUiDispatchController.cs")
            .Replace("\r\n", "\n");

        AssertContains(fullScreenSource, "public Task SetFullScreenEnabledAsync(bool enabled, CancellationToken cancellationToken = default)\n        => InvokeOnUiThreadAsync(\n            () => _fullScreenController.SetEnabledAsync(enabled),");
        AssertContains(fullScreenSource, "private void OnContentKeyDown(object sender, KeyRoutedEventArgs e)\n        => _fullScreenController.OnKeyDown(e);");
        AssertContains(fullScreenSource, "private Task EnterFullScreenAsync()\n        => _fullScreenController.EnterAsync();");
        AssertContains(fullScreenSource, "private Task ExitFullScreenAsync()\n        => _fullScreenController.ExitAsync();");
        AssertContains(fullScreenControllerRootSource, "internal sealed class FullScreenController");
        AssertContains(fullScreenControllerRootSource, "public async Task EnterAsync()");
        AssertContains(fullScreenControllerRootSource, "public async Task ExitAsync()");
        AssertContains(fullScreenControllerRootSource, "await AnimateFullScreenRectAsync(");
        AssertContains(fullScreenControllerRootSource, "private Task AnimateFullScreenRectAsync(");
        AssertContains(fullScreenControllerRootSource, "return completion.Task;");
        AssertContains(fullScreenControllerRootSource, "private void PrepareChromeForOverlay()");
        AssertContains(fullScreenControllerRootSource, "public void OnKeyDown(KeyRoutedEventArgs e)");
        AssertContains(fullScreenControllerRootSource, "if (e.Key == Windows.System.VirtualKey.Escape && _isFullScreen)");
        AssertContains(fullScreenControllerRootSource, "Exit();");
        AssertContains(fullScreenControllerRootSource, "public void OnPointerActivity(PointerRoutedEventArgs e)");
        AssertContains(fullScreenControllerRootSource, "private void ShowControls()");
        AssertDoesNotContain(fullScreenControllerRootSource, "partial class FullScreenController");
        AssertDoesNotContain(fullScreenSource, "private async void EnterFullScreen");
        AssertDoesNotContain(fullScreenSource, "private async void ExitFullScreen");
        AssertDoesNotContain(fullScreenSource, "Windows.System.VirtualKey.Escape");
        AssertDoesNotContain(fullScreenSource, "HandleFlashbackFullScreenKeyDown");
        AssertDoesNotContain(fullScreenControllerRootSource, "async void");
        AssertContains(dispatchingSource, "private Task InvokeOnUiThreadAsync(Func<Task> action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchingSource, "=> WindowUiDispatchController.InvokeAsync(action, cancellationToken);");
        AssertContains(dispatchControllerSource, "await action().ConfigureAwait(true);");
        AssertDoesNotContain(dispatchControllerSource, "registration.Dispose();\n                registration = default;\n\n                if (cancellationToken.IsCancellationRequested)");
        return Task.CompletedTask;
    }

    internal static Task MainWindowWindowAutomationCommands_LiveInController()
    {
        var mainWindowSource = ReadMainWindowCompositionSource();
        var dispatchingSource = ReadRepoFile("Sussudio/MainWindow.ShellChrome.Composition.cs")
            .Replace("\r\n", "\n");
        var dispatchControllerSource = ReadRepoFile("Sussudio/Controllers/Window/WindowUiDispatchController.cs")
            .Replace("\r\n", "\n");
        var adapterSource = ReadRepoFile("Sussudio/MainWindow.ShellChrome.Composition.cs")
            .Replace("\r\n", "\n");
        var controllerSource = ReadRepoFile("Sussudio/Controllers/Window/WindowAutomationController.cs")
            .Replace("\r\n", "\n");

        AssertContains(adapterSource, "private WindowAutomationController _windowAutomationController = null!;");
        AssertContains(adapterSource, "private void InitializeWindowAutomationController()");
        AssertContains(adapterSource, "GetAppWindow = GetAppWindow,");
        AssertContains(adapterSource, "GetWindowHandle = () => _hwnd,");
        AssertContains(adapterSource, "InvokeOnUiThreadAsync = InvokeOnUiThreadAsync");
        AssertContains(dispatchingSource, "private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchingSource, "=> WindowUiDispatchController.InvokeAsync(action, cancellationToken);");
        AssertContains(dispatchControllerSource, "public Task InvokeAsync(Action action, CancellationToken cancellationToken = default)");
        AssertContains(adapterSource, "=> _windowAutomationController.MinimizeAsync(cancellationToken);");
        AssertContains(adapterSource, "=> _windowAutomationController.OpenRecordingsFolderAsync(cancellationToken);");
        AssertContains(adapterSource, "=> _windowAutomationController.SnapToRegionAsync(region, cancellationToken);");
        AssertContains(mainWindowSource, "InitializeWindowAutomationController();");
        AssertContains(controllerSource, "internal sealed class WindowAutomationController");
        AssertContains(controllerSource, "public Task MoveToAsync(int x, int y, CancellationToken cancellationToken = default)");
        AssertContains(controllerSource, "public Task ResizeToAsync(int width, int height, CancellationToken cancellationToken = default)");
        AssertContains(controllerSource, "public Task SnapToRegionAsync(AutomationWindowAction region, CancellationToken cancellationToken = default)");
        AssertContains(controllerSource, "DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary)");
        AssertContains(controllerSource, "var currentSize = region == AutomationWindowAction.Center");
        AssertContains(controllerSource, "WindowSnapRegionLayoutPolicy.ResolveTargetBounds(region, work, currentSize)");
        AssertContains(controllerSource, "appWindow.MoveAndResize(bounds);");
        AssertContains(controllerSource, "Process.Start(\"explorer.exe\", path);");
        AssertContains(controllerSource, "internal static class WindowSnapRegionLayoutPolicy");
        AssertContains(controllerSource, "public static RectInt32? ResolveTargetBounds(");
        AssertContains(controllerSource, "AutomationWindowAction.Center => new RectInt32(");
        AssertContains(adapterSource, "public Task CloseAsync(CancellationToken cancellationToken = default)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Window", "WindowSnapRegionLayoutPolicy.cs")),
            "snap-region rectangle math lives with window automation controller concerns");
        return Task.CompletedTask;
    }

    internal static Task MainWindowUiDispatching_LivesInShellChromeAdapter()
    {
        var dispatchingSource = ReadRepoFile("Sussudio/MainWindow.ShellChrome.Composition.cs")
            .Replace("\r\n", "\n");
        var dispatchControllerSource = ReadRepoFile("Sussudio/Controllers/Window/WindowUiDispatchController.cs")
            .Replace("\r\n", "\n");
        var previewActionsSource = ReadMainWindowPreviewTransitionsAdapterSource();
        var flashbackSource = ReadMainWindowFlashbackAdapterSource();
        var flashbackControllerSource = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackCommandController.cs")
            .Replace("\r\n", "\n");

        AssertContains(dispatchingSource, "private WindowUiDispatchController? _windowUiDispatchController;");
        AssertContains(dispatchingSource, "private WindowUiDispatchController WindowUiDispatchController =>");
        AssertContains(dispatchingSource, "CompleteWindowCloseRequest = CompleteWindowCloseRequest");
        AssertContains(dispatchingSource, "private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchingSource, "=> WindowUiDispatchController.InvokeAsync(action, cancellationToken);");
        AssertContains(dispatchingSource, "private Task InvokeOnUiThreadAsync(Func<Task> action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchingSource, "private Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)");
        AssertContains(dispatchingSource, "=> WindowUiDispatchController.RunUiEventHandlerAsync(operation, operationName);");
        AssertContains(dispatchControllerSource, "internal sealed class WindowUiDispatchController");
        AssertContains(dispatchControllerSource, "public Task InvokeAsync(Action action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchControllerSource, "public Task InvokeAsync(Func<Task> action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchControllerSource, "public async Task<TResult> InvokeWithRetryAsync<TResult>(");
        AssertContains(dispatchControllerSource, "public async Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)");
        AssertContains(dispatchControllerSource, "if (_context.DispatcherQueue.HasThreadAccess)\n        {\n            action();\n            return Task.CompletedTask;\n        }");
        AssertContains(dispatchControllerSource, "if (_context.DispatcherQueue.HasThreadAccess)\n        {\n            return action();\n        }");
        AssertContains(dispatchControllerSource, "const int maxAttempts = 3;");
        AssertContains(dispatchControllerSource, "completion.TrySetResult(action());");
        AssertContains(dispatchControllerSource, "await Task.Delay(50, cancellationToken).ConfigureAwait(false);");
        AssertContains(dispatchControllerSource, "throw new InvalidOperationException(enqueueFailureMessage);");
        AssertContains(dispatchControllerSource, "_context.CompleteWindowCloseRequest(new OperationCanceledException(cancellationToken));");
        AssertContains(dispatchControllerSource, "await action().ConfigureAwait(true);");
        AssertContains(dispatchControllerSource, "completion.TrySetException(new InvalidOperationException(\"Failed to enqueue window action on the UI thread.\"));");
        AssertContains(dispatchControllerSource, "_context.ViewModel.StatusText = $\"{operationName} failed: {ex.Message}\";");
        AssertContains(previewActionsSource, "_ = RunUiEventHandlerAsync(() => TogglePreviewFromButtonAsync(), nameof(PreviewButton_Click));");
        AssertContains(flashbackSource, "=> _flashbackCommandController.Export(nameof(FlashbackExportButton_Click));");
        AssertContains(flashbackSource, "=> _flashbackCommandController.SaveLast5m(nameof(FlashbackSaveLast5mButton_Click));");
        AssertContains(flashbackControllerSource, "=> _ = _context.RunUiEventHandlerAsync(() => _context.ViewModel.ExportFlashbackAsync(), operationName);");
        AssertContains(flashbackControllerSource, "=> _ = _context.RunUiEventHandlerAsync(() => _context.ViewModel.SaveFlashbackLast5mAsync(), operationName);");
        AssertDoesNotContain(dispatchingSource, "CompleteWindowCloseRequest(new OperationCanceledException(cancellationToken));");
        AssertDoesNotContain(dispatchingSource, "ViewModel.StatusText = $\"{operationName} failed: {ex.Message}\";");

        return Task.CompletedTask;
    }
}
