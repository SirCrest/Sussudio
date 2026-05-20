using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    private static Task MainWindowShutdownCleanup_OwnsPostCloseCleanupOrder()
    {
        var shutdownCleanupText = ReadRepoFile("Sussudio/MainWindow.ShutdownCleanup.cs").Replace("\r\n", "\n");
        var automationHostControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowAutomationHostLifecycleController.cs").Replace("\r\n", "\n");
        var shutdownCleanupControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowShutdownCleanupController.cs").Replace("\r\n", "\n");

        AssertContains(shutdownCleanupText, "Post-close shutdown cleanup");
        AssertContains(shutdownCleanupText, "private WindowShutdownCleanupController _windowShutdownCleanupController = null!;");
        AssertContains(shutdownCleanupText, "private void InitializeWindowShutdownCleanupController()");
        AssertContains(shutdownCleanupText, "private async void MainWindow_Closed(object sender, WindowEventArgs args)");
        AssertContains(shutdownCleanupText, "=> await _windowShutdownCleanupController.RunAsync();");
        AssertContains(shutdownCleanupText, "StopRecordingAfterClosedBestEffortAsync = () => _windowCloseRecordingFinalizationController.StopAfterClosedBestEffortAsync(");
        AssertContains(shutdownCleanupText, "DisposeAutomationHostAsync = () => _automationHostLifecycleController.DisposeAsync(),");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.AutomationHost.cs")),
            "MainWindow automation host adapter partial");

        AssertContains(automationHostControllerText, "public async ValueTask DisposeAsync()");
        AssertContains(automationHostControllerText, "await _pipeServer.DisposeAsync();");
        AssertContains(automationHostControllerText, "await _diagnosticsHub.DisposeAsync();");
        AssertContains(automationHostControllerText, "Logger.Log($\"Automation shutdown cleanup failed: {ex.Message}\");");
        AssertContains(automationHostControllerText, "Logger.Log($\"Automation diagnostics shutdown cleanup failed: {ex.Message}\");");
        AssertOccursBefore(automationHostControllerText, "await _pipeServer.DisposeAsync();", "await _diagnosticsHub.DisposeAsync();");
        AssertOccursBefore(automationHostControllerText, "Logger.Log($\"Automation shutdown cleanup failed: {ex.Message}\");", "await _diagnosticsHub.DisposeAsync();");

        AssertContains(shutdownCleanupControllerText, "internal sealed class WindowShutdownCleanupControllerContext");
        AssertContains(shutdownCleanupControllerText, "internal sealed class WindowShutdownCleanupController");
        AssertContains(shutdownCleanupControllerText, "public async Task RunAsync()");
        AssertContains(shutdownCleanupControllerText, "_context.CancelNativeShellRevealAfterFirstFrame();");
        AssertContains(shutdownCleanupControllerText, "if (!_context.LifecycleController.TryBeginCleanup())");
        AssertContains(shutdownCleanupControllerText, "LogWindowClosedTrigger();");
        AssertContains(shutdownCleanupControllerText, "_context.CompleteWindowCloseRequest();");
        AssertContains(shutdownCleanupControllerText, "_context.LifecycleController.MarkClosing();");
        AssertContains(shutdownCleanupControllerText, "_context.DetachMeterActivationHandlers();");
        AssertContains(shutdownCleanupControllerText, "_context.StopTimers();");
        AssertContains(shutdownCleanupControllerText, "_context.StopStatsOverlay();");
        AssertContains(shutdownCleanupControllerText, "_context.StopRecordingVisuals();");
        AssertContains(shutdownCleanupControllerText, "_context.DetachMainContentSizeChanged();");
        AssertContains(shutdownCleanupControllerText, "_context.DetachViewModelEventHandlers();");
        AssertContains(shutdownCleanupControllerText, "_context.StopPreviewForShutdown();");
        AssertContains(shutdownCleanupControllerText, "_context.ResetPreviewStartupTracking();");
        AssertContains(shutdownCleanupControllerText, "await _context.StopRecordingAfterClosedBestEffortAsync().ConfigureAwait(false);");
        AssertContains(shutdownCleanupControllerText, "await _context.DisposeAutomationHostAsync().ConfigureAwait(false);");
        AssertContains(shutdownCleanupControllerText, "_context.DisposeNvmlMonitor();");
        AssertContains(shutdownCleanupControllerText, "await _context.DisposeViewModelAsync().ConfigureAwait(false);");
        AssertOccursBefore(shutdownCleanupControllerText, "_context.CancelNativeShellRevealAfterFirstFrame();", "if (!_context.LifecycleController.TryBeginCleanup())");
        AssertOccursBefore(shutdownCleanupControllerText, "_context.CompleteWindowCloseRequest();", "_context.LifecycleController.MarkClosing();");
        AssertOccursBefore(shutdownCleanupControllerText, "_context.LifecycleController.MarkClosing();", "_context.DetachMeterActivationHandlers();");
        AssertOccursBefore(shutdownCleanupControllerText, "_context.DetachViewModelEventHandlers();", "_context.StopPreviewForShutdown();");
        AssertOccursBefore(shutdownCleanupControllerText, "_context.ResetPreviewStartupTracking();", "await _context.StopRecordingAfterClosedBestEffortAsync().ConfigureAwait(false);");
        AssertOccursBefore(shutdownCleanupControllerText, "await _context.StopRecordingAfterClosedBestEffortAsync().ConfigureAwait(false);", "await _context.DisposeAutomationHostAsync().ConfigureAwait(false);");
        AssertOccursBefore(shutdownCleanupControllerText, "await _context.DisposeAutomationHostAsync().ConfigureAwait(false);", "_context.DisposeNvmlMonitor();");

        AssertContains(shutdownCleanupText, "StopLiveSignalInfoTimers();");
        AssertContains(shutdownCleanupText, "StopMicMeterRowAnimation();");
        AssertContains(shutdownCleanupText, "StopFlashbackStatusPolling();");
        AssertContains(shutdownCleanupText, "CancelNativeShellRevealAfterFirstFrame = CancelNativeShellRevealAfterFirstFrame,");
        AssertDoesNotContain(shutdownCleanupText, "WINDOW_CLOSED_TRIGGER ");
        AssertDoesNotContain(shutdownCleanupText, "_automationPipeServer.DisposeAsync()");
        AssertDoesNotContain(shutdownCleanupText, "_automationDiagnosticsHub.DisposeAsync()");

        return Task.CompletedTask;
    }
}
