using System;
using Microsoft.UI.Xaml.Media;
using Sussudio.Controllers;

namespace Sussudio;

// Adapter for audio-meter rendering. AudioMeterController owns smoothing,
// timers, peak/range state, and meter clips; this partial keeps old call sites.
public sealed partial class MainWindow
{
    private AudioMeterController _audioMeterController = null!;

    private void InitializeAudioMeterBrushes()
    {
        _audioMeterController = new AudioMeterController(new AudioMeterControllerContext
        {
            DispatcherQueue = _dispatcherQueue,
            ViewModel = ViewModel,
            AudioMeterTrack = AudioMeterTrack,
            AudioMeterContent = AudioMeterContent,
            AudioMeterRawFill = AudioMeterRawFill,
            AudioMeterFill = AudioMeterFill,
            AudioMeterRawClip = AudioMeterRawClip,
            AudioMeterColorClip = AudioMeterColorClip,
            AudioPeakHoldIndicator = AudioPeakHoldIndicator,
            AudioPeakHoldTranslate = AudioPeakHoldTranslate,
            AudioRangeMinMarker = AudioRangeMinMarker,
            AudioRangeMinTranslate = AudioRangeMinTranslate,
            AudioRangeMaxMarker = AudioRangeMaxMarker,
            AudioRangeMaxTranslate = AudioRangeMaxTranslate,
            MicMeterTrack = MicMeterTrack,
            MicMeterContent = MicMeterContent,
            MicMeterClip = MicMeterClip,
        });
        _audioMeterController.Initialize();
    }

    private void AnimateAudioMeterTick()
        => _audioMeterController.AnimateTick();

    private void ResetAudioMeterVisuals()
        => _audioMeterController.ResetVisuals();

    private void ResetMicrophoneMeterVisuals()
        => _audioMeterController.ResetMicrophoneVisuals();

    private void SetAudioMeterTargetLevel(double targetLevel)
        => _audioMeterController.SetAudioMeterTargetLevel(targetLevel);

    private void EnsureAudioMeterTimerRunning()
        => _audioMeterController.EnsureTimerRunning();

    private void StopAudioMeterTimer()
        => _audioMeterController.StopTimer();

    private void SetAudioMeterMonitoringState(bool isMonitoring)
        => _audioMeterController.SetMonitoringState(isMonitoring);

    private void AnimateAudioMeterDisabled(bool isDisabled)
        => _audioMeterController.AnimateDisabled(isDisabled);

    private static double TranslateMarker(double trackWidth, double level, double markerWidth)
        => AudioMeterController.TranslateMarker(trackWidth, level, markerWidth);
}
