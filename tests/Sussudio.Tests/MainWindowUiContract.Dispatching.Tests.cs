static partial class Program
{
    private static Task MainWindowUiDispatching_LivesInDispatchingPartial()
    {
        var dispatchingSource = ReadRepoFile("Sussudio/MainWindow.Dispatching.cs")
            .Replace("\r\n", "\n");
        var dispatchControllerSource = ReadRepoFile("Sussudio/Controllers/WindowUiDispatchController.cs")
            .Replace("\r\n", "\n");
        var closeLifecycleSource = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs")
            .Replace("\r\n", "\n");
        var previewActionsSource = ReadRepoFile("Sussudio/MainWindow.PreviewActions.cs")
            .Replace("\r\n", "\n");
        var flashbackSource = ReadRepoFile("Sussudio/MainWindow.FlashbackCommands.cs")
            .Replace("\r\n", "\n");
        var flashbackControllerSource = ReadRepoFile("Sussudio/Controllers/FlashbackCommandController.cs")
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
        AssertContains(dispatchControllerSource, "public async Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)");
        AssertContains(dispatchControllerSource, "if (_context.DispatcherQueue.HasThreadAccess)\n        {\n            action();\n            return Task.CompletedTask;\n        }");
        AssertContains(dispatchControllerSource, "if (_context.DispatcherQueue.HasThreadAccess)\n        {\n            return action();\n        }");
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
        AssertDoesNotContain(closeLifecycleSource, "private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(closeLifecycleSource, "private Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)");

        return Task.CompletedTask;
    }
}
