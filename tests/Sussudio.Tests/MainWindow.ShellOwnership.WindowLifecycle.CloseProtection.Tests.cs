using System.Threading.Tasks;

static partial class Program
{
    internal static Task MainWindowClose_CancelsCloseUntilRecordingStopCompletes()
    {
        var windowCtorText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var closeLifecycleText = ReadRepoFile("Sussudio/MainWindow.WindowShell.cs")
            .Replace("\r\n", "\n");
        var closeLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseLifecycleController.cs")
            .Replace("\r\n", "\n");
        var appClosingControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowAppClosingController.cs")
            .Replace("\r\n", "\n");
        var closeRequestControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseRequestController.cs")
            .Replace("\r\n", "\n");
        var closeRecordingFinalizationControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseRecordingFinalizationController.cs")
            .Replace("\r\n", "\n");

        AssertContains(windowCtorText, "RegisterCloseLifecycle(appWindow);");
        AssertContains(closeLifecycleText, "appWindow.Closing += MainWindow_Closing;");
        AssertContains(closeLifecycleText, "_windowAppClosingController.HandleClosingAsync(args)");
        AssertContains(appClosingControllerText, "args.Cancel = true;");
        AssertContains(closeLifecycleText, "TryStopRecordingBeforeCloseAsync");
        AssertContains(appClosingControllerText, "if (!_context.IsRecording() && !_context.IsRecordingTransitioning())");
        AssertContains(closeLifecycleText, "=> _windowCloseRecordingFinalizationController.StopBeforeCloseAsync(");
        AssertContains(appClosingControllerText, "_context.RequestWindowClose();");
        AssertContains(closeLifecycleText, "_windowCloseLifecycleController.CloseAsync(_dispatcherQueue, RequestWindowClose, cancellationToken)");
        AssertContains(closeLifecycleText, "=> _windowCloseRequestController.RequestClose();");
        AssertContains(closeRequestControllerText, "_context.CloseWindow();");
        AssertContains(closeRequestControllerText, "_context.LifecycleController.CompleteRequest();");
        AssertContains(closeRequestControllerText, "_context.ExitApplication();");
        AssertContains(appClosingControllerText, "_context.LifecycleController.CompleteRequest(new InvalidOperationException(_context.GetStatusText()))");
        AssertContains(appClosingControllerText, "_context.LifecycleController.CompleteRequest();");
        AssertContains(closeLifecycleControllerText, "private Task GetCompletionTask(CancellationToken cancellationToken)");
        AssertContains(closeLifecycleControllerText, "var enqueueFailure = new InvalidOperationException(\"Failed to enqueue window close action on the UI thread.\");");
        AssertContains(closeRecordingFinalizationControllerText, "private const int StopBudgetMs = 120_000;");
        AssertContains(closeRecordingFinalizationControllerText, "var stopTask = viewModel.StopRecordingAndWaitAsync();");
        AssertContains(closeRecordingFinalizationControllerText, "var completed = await Task.WhenAny(stopTask, Task.Delay(StopBudgetMs));");
        AssertContains(closeRecordingFinalizationControllerText, "close cancelled to protect recording");
        AssertContains(closeRecordingFinalizationControllerText, "Still saving recording. Close cancelled.");
        AssertContains(closeRecordingFinalizationControllerText, "RECORDING_FINALIZE_FAILED_AFTER_CLOSE ");
        AssertDoesNotContain(closeLifecycleText, "Task.WhenAny(");
        AssertDoesNotContain(closeLifecycleText, "StopRecordingAndWaitAsync");
        AssertDoesNotContain(closeLifecycleText, "args.Cancel = true;");
        AssertDoesNotContain(closeLifecycleText, "MP4 may be truncated.");

        return Task.CompletedTask;
    }
}
