static partial class Program
{
    private static Task MainWindowFullScreenAutomation_AwaitsTransitionTask()
    {
        var fullScreenSource = ReadRepoFile("Sussudio/MainWindow.FullScreen.cs")
            .Replace("\r\n", "\n");
        var fullScreenControllerRootSource = ReadRepoFile("Sussudio/Controllers/FullScreenController.cs")
            .Replace("\r\n", "\n");
        var fullScreenControllerTransitionSource = ReadRepoFile("Sussudio/Controllers/FullScreenController.Transitions.cs")
            .Replace("\r\n", "\n");
        var fullScreenControllerAnimationSource = ReadRepoFile("Sussudio/Controllers/FullScreenController.Animation.cs")
            .Replace("\r\n", "\n");
        var fullScreenControllerChromeSource = ReadRepoFile("Sussudio/Controllers/FullScreenController.Chrome.cs")
            .Replace("\r\n", "\n");
        var fullScreenControllerControlsSource = ReadRepoFile("Sussudio/Controllers/FullScreenController.Controls.cs")
            .Replace("\r\n", "\n");
        var closeLifecycleSource = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs")
            .Replace("\r\n", "\n");
        var dispatchingSource = ReadRepoFile("Sussudio/MainWindow.Dispatching.cs")
            .Replace("\r\n", "\n");

        AssertContains(fullScreenSource, "public Task SetFullScreenEnabledAsync(bool enabled, CancellationToken cancellationToken = default)\n        => InvokeOnUiThreadAsync(\n            () => _fullScreenController.SetEnabledAsync(enabled),");
        AssertContains(fullScreenSource, "private Task EnterFullScreenAsync()\n        => _fullScreenController.EnterAsync();");
        AssertContains(fullScreenSource, "private Task ExitFullScreenAsync()\n        => _fullScreenController.ExitAsync();");
        AssertContains(fullScreenControllerRootSource, "internal sealed partial class FullScreenController");
        AssertContains(fullScreenControllerTransitionSource, "public async Task EnterAsync()");
        AssertContains(fullScreenControllerTransitionSource, "public async Task ExitAsync()");
        AssertContains(fullScreenControllerTransitionSource, "await AnimateFullScreenRectAsync(");
        AssertContains(fullScreenControllerAnimationSource, "private Task AnimateFullScreenRectAsync(");
        AssertContains(fullScreenControllerAnimationSource, "return completion.Task;");
        AssertContains(fullScreenControllerChromeSource, "private void PrepareChromeForOverlay()");
        AssertContains(fullScreenControllerControlsSource, "public void OnPointerActivity(PointerRoutedEventArgs e)");
        AssertContains(fullScreenControllerControlsSource, "private void ShowControls()");
        AssertDoesNotContain(fullScreenControllerRootSource, "public async Task EnterAsync()");
        AssertDoesNotContain(fullScreenControllerRootSource, "private Task AnimateFullScreenRectAsync(");
        AssertDoesNotContain(fullScreenControllerRootSource, "private void ShowControls()");
        AssertDoesNotContain(fullScreenSource, "private async void EnterFullScreen");
        AssertDoesNotContain(fullScreenSource, "private async void ExitFullScreen");
        AssertDoesNotContain(fullScreenControllerRootSource, "async void");
        AssertDoesNotContain(fullScreenControllerTransitionSource, "async void");
        AssertDoesNotContain(fullScreenControllerAnimationSource, "async void");
        AssertDoesNotContain(fullScreenControllerControlsSource, "async void");
        AssertContains(dispatchingSource, "private Task InvokeOnUiThreadAsync(Func<Task> action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchingSource, "await action().ConfigureAwait(true);");
        AssertDoesNotContain(dispatchingSource, "registration.Dispose();\n                registration = default;\n\n                if (cancellationToken.IsCancellationRequested)");
        AssertDoesNotContain(closeLifecycleSource, "private Task InvokeOnUiThreadAsync(Func<Task> action, CancellationToken cancellationToken = default)");
        return Task.CompletedTask;
    }

    private static Task MainWindowWindowAutomationCommands_LiveInController()
    {
        var mainWindowSource = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var closeLifecycleSource = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs")
            .Replace("\r\n", "\n");
        var dispatchingSource = ReadRepoFile("Sussudio/MainWindow.Dispatching.cs")
            .Replace("\r\n", "\n");
        var adapterSource = ReadRepoFile("Sussudio/MainWindow.WindowAutomation.cs")
            .Replace("\r\n", "\n");
        var controllerSource = ReadRepoFile("Sussudio/Controllers/WindowAutomationController.cs")
            .Replace("\r\n", "\n");

        AssertContains(adapterSource, "private WindowAutomationController _windowAutomationController = null!;");
        AssertContains(adapterSource, "private void InitializeWindowAutomationController()");
        AssertContains(adapterSource, "GetAppWindow = GetAppWindow,");
        AssertContains(adapterSource, "GetWindowHandle = () => _hwnd,");
        AssertContains(adapterSource, "InvokeOnUiThreadAsync = InvokeOnUiThreadAsync");
        AssertContains(dispatchingSource, "private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)");
        AssertContains(adapterSource, "=> _windowAutomationController.MinimizeAsync(cancellationToken);");
        AssertContains(adapterSource, "=> _windowAutomationController.OpenRecordingsFolderAsync(cancellationToken);");
        AssertContains(adapterSource, "=> _windowAutomationController.SnapToRegionAsync(region, cancellationToken);");
        AssertContains(mainWindowSource, "InitializeWindowAutomationController();");
        AssertContains(controllerSource, "internal sealed class WindowAutomationController");
        AssertContains(controllerSource, "public Task MoveToAsync(int x, int y, CancellationToken cancellationToken = default)");
        AssertContains(controllerSource, "public Task ResizeToAsync(int width, int height, CancellationToken cancellationToken = default)");
        AssertContains(controllerSource, "public Task SnapToRegionAsync(AutomationWindowAction region, CancellationToken cancellationToken = default)");
        AssertContains(controllerSource, "DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Primary)");
        AssertContains(controllerSource, "Process.Start(\"explorer.exe\", path);");
        AssertContains(closeLifecycleSource, "public Task CloseAsync(CancellationToken cancellationToken = default)");
        AssertDoesNotContain(closeLifecycleSource, "public Task MinimizeAsync(");
        AssertDoesNotContain(closeLifecycleSource, "public Task OpenRecordingsFolderAsync(");
        AssertDoesNotContain(closeLifecycleSource, "public Task SnapToRegionAsync(");
        return Task.CompletedTask;
    }
}
