using Microsoft.UI.Xaml;
using System;

namespace Sussudio;

// Post-close shutdown cleanup. Recording finalization side effects live in
// WindowCloseRecordingFinalizationController; close state/completion lives in
// WindowCloseLifecycleController.
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

        await _windowCloseRecordingFinalizationController.StopAfterClosedBestEffortAsync(
            ViewModel,
            Content as FrameworkElement);

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
