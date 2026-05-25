static partial class Program
{
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
}
