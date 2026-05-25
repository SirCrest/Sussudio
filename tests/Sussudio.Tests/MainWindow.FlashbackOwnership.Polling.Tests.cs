using System.Threading.Tasks;

static partial class Program
{
    internal static Task FlashbackPollingTimers_LiveInController()
    {
        var flashbackText = ReadMainWindowFlashbackAdapterSource();
        var mainWindowText = ReadMainWindowCompositionSource();
        var pollingAdapterText = ReadMainWindowFlashbackAdapterSource();
        var timelineAdapterText = ReadMainWindowFlashbackAdapterSource();
        var shutdownCleanupText = ReadMainWindowCompositionSource();
        var shutdownCleanupControllerText = ReadRepoFile("Sussudio/Controllers/Window/WindowCloseLifecycleController.cs").Replace("\r\n", "\n");
        var controllerText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackUiControllers.cs").Replace("\r\n", "\n");
        var playbackCoordinatorText = ReadRepoFile("Sussudio/Controllers/Flashback/FlashbackPlaybackUiCoordinator.cs").Replace("\r\n", "\n");

        AssertContains(pollingAdapterText, "private FlashbackPollingController _flashbackPollingController = null!;");
        AssertContains(pollingAdapterText, "private void InitializeFlashbackPollingController()");
        AssertContains(pollingAdapterText, "IsWindowClosing = () => _isWindowClosing,");
        AssertContains(pollingAdapterText, "=> _flashbackPollingController.StartStatusPolling();");
        AssertContains(pollingAdapterText, "_flashbackPollingController.StopStatusPolling();");
        AssertContains(pollingAdapterText, "StopFlashbackCtiAnchorTimer();");
        AssertContains(pollingAdapterText, "=> _flashbackPollingController.StartPlaybackPolling();");
        AssertContains(pollingAdapterText, "=> _flashbackPollingController.StopPlaybackPolling();");
        AssertContains(mainWindowText, "InitializeFlashbackPollingController();");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Flashback.Interactions.cs")),
            "Flashback polling adapter lives in the consolidated Flashback interaction adapter");
        AssertContains(timelineAdapterText, "StartStatusPolling = StartFlashbackStatusPolling,");
        AssertContains(shutdownCleanupText, "StopFlashbackStatusPolling();");
        AssertContains(shutdownCleanupControllerText, "_context.StopTimers();");
        AssertContains(flashbackText, "StartPlaybackPolling = StartFlashbackPlaybackPolling,");
        AssertContains(flashbackText, "StopPlaybackPolling = StopFlashbackPlaybackPolling,");
        AssertContains(playbackCoordinatorText, "_context.StartPlaybackPolling();");
        AssertContains(playbackCoordinatorText, "_context.StopPlaybackPolling();");
        AssertContains(controllerText, "internal sealed class FlashbackPollingController");
        AssertContains(controllerText, "private DispatcherQueueTimer? _statusTimer;");
        AssertContains(controllerText, "private DispatcherQueueTimer? _playbackTimer;");
        AssertContains(controllerText, "public void StartStatusPolling()");
        AssertContains(controllerText, "public void StopStatusPolling()");
        AssertContains(controllerText, "public void StartPlaybackPolling()");
        AssertContains(controllerText, "public void StopPlaybackPolling()");
        AssertContains(controllerText, "_context.ViewModel.UpdateFlashbackBufferStatus();");
        AssertContains(controllerText, "_context.ViewModel.FlashbackPlaybackPosition = playback.PlaybackPosition;");
        AssertContains(controllerText, "FLASHBACK_STATUS_TIMER_FAIL");
        AssertContains(controllerText, "FLASHBACK_PLAYBACK_TIMER_FAIL");
        AssertDoesNotContain(flashbackText, "private DispatcherQueueTimer? _flashbackStatusTimer;");
        AssertDoesNotContain(flashbackText, "private void FlashbackStatusTimer_Tick(");
        AssertDoesNotContain(flashbackText, "private void FlashbackPlaybackTimer_Tick(");

        return Task.CompletedTask;
    }
}
