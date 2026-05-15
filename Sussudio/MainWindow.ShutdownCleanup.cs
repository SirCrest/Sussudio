using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;

namespace Sussudio;

// Post-close shutdown cleanup. The pre-close recording guard stays in
// MainWindow.CloseLifecycle.cs; close state/completion lives in the controller.
public sealed partial class MainWindow
{
    private async void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (!_windowCloseLifecycleController.TryBeginCleanup())
        {
            return;
        }

        try
        {
            var snapshot = _windowCloseLifecycleController.Snapshot;
            Logger.Log(
                "WINDOW_CLOSED_TRIGGER " +
                $"requested={snapshot.Requested} " +
                $"recordingStopInProgress={snapshot.RecordingStopInProgress} " +
                $"allowedAfterRecordingStop={snapshot.AllowedAfterRecordingStop} " +
                $"isRecording={ViewModel.IsRecording} " +
                $"isPreviewing={ViewModel.IsPreviewing} " +
                $"stack=\n{new System.Diagnostics.StackTrace(true)}");
        }
        catch (Exception logEx)
        {
            System.Diagnostics.Trace.TraceWarning($"WINDOW_CLOSED_TRIGGER log failed: {logEx.Message}");
        }

        CompleteWindowCloseRequest();
        _windowCloseLifecycleController.MarkClosing();
        ViewModel.AudioMeterActivated -= EnsureAudioMeterTimerRunning;
        ViewModel.MicrophoneMeterActivated -= EnsureAudioMeterTimerRunning;
        StopAudioMeterTimer();
        StopLiveSignalInfoTimers();
        StopFullScreenAutoHideTimer();
        StopFlashbackStatusPolling();
        StopStatsDockPolling();
        HideStatsDockPanel(immediate: true);
        StopMicMeterRowAnimation();
        RecordingGlowPulseStoryboard.Stop();
        RecordingGlowBorder.Opacity = 0;
        RecPulseStoryboard.Stop();

        if (this.Content is FrameworkElement mainContent)
        {
            mainContent.SizeChanged -= MainWindow_SizeChanged;
        }

        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.PreviewStartRequested -= ViewModel_PreviewStartRequested;
        ViewModel.PreviewStopRequested -= ViewModel_PreviewStopRequested;
        ViewModel.PreviewReinitRequested -= ViewModel_PreviewReinitRequested;
        ViewModel.PreviewRendererStopRequested -= ViewModel_PreviewRendererStopRequested;

        try
        {
            StopPreviewForShutdown();
            ResetPreviewStartupTracking();
        }
        catch (Exception ex)
        {
            Logger.Log($"Preview shutdown cleanup failed: {ex.Message}");
        }

        // Graceful recording stop: the mux finalize (esp. 4K HDR with large buffered
        // NVENC frames) routinely exceeds the prior 5s timeout, producing a truncated
        // MP4 with no moov atom. Block the close on the real stop up to a generous
        // cap; surface "Stopping recording..." and disable input so the user sees the
        // app is working rather than appearing frozen.
        if (ViewModel.IsRecording)
        {
            const int StopBudgetMs = 120_000;
            Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording active, awaiting graceful stop...");
            ViewModel.StatusText = "Stopping recording — please wait…";
            if (this.Content is FrameworkElement shutdownContent)
            {
                shutdownContent.IsHitTestVisible = false;
                shutdownContent.Opacity = 0.5;
            }
            try
            {
                var stopTask = ViewModel.StopRecordingAndWaitAsync();
                var completed = await Task.WhenAny(stopTask, Task.Delay(StopBudgetMs));
                if (completed == stopTask)
                {
                    await stopTask; // propagate any exception
                    Logger.Log("WINDOW_CLOSE_RECORDING_STOP: recording stopped cleanly.");
                }
                else
                {
                    Logger.LogFatalBreadcrumb("RECORDING_FINALIZE_TIMEOUT "
                        + $"budget_ms={StopBudgetMs}; window already closed; continuing shutdown cleanup.");
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"WINDOW_CLOSE_RECORDING_STOP: stop failed: {ex.Message}");
                Logger.LogFatalBreadcrumb("RECORDING_FINALIZE_FAILED_AFTER_CLOSE "
                    + $"window already closed; continuing shutdown cleanup. error='{ex.Message}'");
            }
        }

        try
        {
            await _automationPipeServer.DisposeAsync();
            await _automationDiagnosticsHub.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"Automation shutdown cleanup failed: {ex.Message}");
        }

        _nvmlMonitor?.Dispose();

        try
        {
            await ViewModel.DisposeAsync();
        }
        catch (Exception ex)
        {
            Logger.Log($"ViewModel dispose during window close failed: {ex.Message}");
        }
    }
}
