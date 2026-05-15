static partial class Program
{
    private static Task MainWindowUiDispatching_LivesInDispatchingPartial()
    {
        var dispatchingSource = ReadRepoFile("Sussudio/MainWindow.Dispatching.cs")
            .Replace("\r\n", "\n");
        var closeLifecycleSource = ReadRepoFile("Sussudio/MainWindow.CloseLifecycle.cs")
            .Replace("\r\n", "\n");
        var eventHandlersSource = ReadRepoFile("Sussudio/MainWindow.EventHandlers.cs")
            .Replace("\r\n", "\n");
        var flashbackSource = ReadRepoFile("Sussudio/MainWindow.FlashbackCommands.cs")
            .Replace("\r\n", "\n");
        var flashbackControllerSource = ReadRepoFile("Sussudio/Controllers/FlashbackCommandController.cs")
            .Replace("\r\n", "\n");

        AssertContains(dispatchingSource, "private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchingSource, "private Task InvokeOnUiThreadAsync(Func<Task> action, CancellationToken cancellationToken = default)");
        AssertContains(dispatchingSource, "private async Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)");
        AssertContains(dispatchingSource, "CompleteWindowCloseRequest(new OperationCanceledException(cancellationToken));");
        AssertContains(dispatchingSource, "ViewModel.StatusText = $\"{operationName} failed: {ex.Message}\";");
        AssertContains(eventHandlersSource, "RunUiEventHandlerAsync(async () =>");
        AssertContains(flashbackSource, "=> _flashbackCommandController.Export(nameof(FlashbackExportButton_Click));");
        AssertContains(flashbackSource, "=> _flashbackCommandController.SaveLast5m(nameof(FlashbackSaveLast5mButton_Click));");
        AssertContains(flashbackControllerSource, "=> _ = _context.RunUiEventHandlerAsync(() => _context.ViewModel.ExportFlashbackAsync(), operationName);");
        AssertContains(flashbackControllerSource, "=> _ = _context.RunUiEventHandlerAsync(() => _context.ViewModel.SaveFlashbackLast5mAsync(), operationName);");
        AssertDoesNotContain(closeLifecycleSource, "private Task InvokeOnUiThreadAsync(Action action, CancellationToken cancellationToken = default)");
        AssertDoesNotContain(closeLifecycleSource, "private async Task RunUiEventHandlerAsync(Func<Task> operation, string operationName)");

        return Task.CompletedTask;
    }
}
