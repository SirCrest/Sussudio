using Microsoft.UI.Xaml;

namespace Sussudio;

public sealed partial class MainWindow
{
    private void DetachMeterActivationHandlers()
    {
        ViewModel.AudioMeterActivated -= EnsureAudioMeterTimerRunning;
        ViewModel.MicrophoneMeterActivated -= EnsureAudioMeterTimerRunning;
    }

    private void StopShutdownTimers()
    {
        StopAudioMeterTimer();
        StopLiveSignalInfoTimers();
        StopFullScreenAutoHideTimer();
        StopFlashbackStatusPolling();
    }

    private void StopStatsOverlayForShutdown()
    {
        DetachStatsOverlayToggleBindings();
        StopStatsDockPolling();
        HideStatsDockPanel(immediate: true);
    }

    private void StopRecordingVisualsForShutdown()
    {
        StopMicMeterRowAnimation();
        RecordingGlowPulseStoryboard.Stop();
        RecordingGlowBorder.Opacity = 0;
        RecPulseStoryboard.Stop();
    }

    private void DetachMainContentSizeChanged()
    {
        if (Content is FrameworkElement mainContent)
        {
            mainContent.SizeChanged -= MainWindow_SizeChanged;
        }
    }

    private void DetachViewModelEventHandlers()
    {
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        ViewModel.PreviewStartRequested -= ViewModel_PreviewStartRequested;
        ViewModel.PreviewStopRequested -= ViewModel_PreviewStopRequested;
        ViewModel.PreviewReinitRequested -= ViewModel_PreviewReinitRequested;
        ViewModel.PreviewRendererStopRequested -= ViewModel_PreviewRendererStopRequested;
    }
}
