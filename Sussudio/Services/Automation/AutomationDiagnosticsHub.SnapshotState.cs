using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationDiagnosticsHub
{
    private AudioSignalState UpdateAudioSignalState(ViewModelRuntimeSnapshot viewModelSnapshot, long nowTick)
    {
        var audioSignalPresent = viewModelSnapshot.AudioPeak >= AudioSignalThreshold;
        var audioContextActive = viewModelSnapshot.IsAudioEnabled &&
                                 (viewModelSnapshot.IsAudioPreviewEnabled || viewModelSnapshot.IsRecording);
        if (audioContextActive && !audioSignalPresent)
        {
            if (_muteLowSignalStartTick == 0)
            {
                _muteLowSignalStartTick = nowTick;
            }
        }
        else
        {
            _muteLowSignalStartTick = 0;
        }

        var audioMutedSuspected = audioContextActive &&
                                  _muteLowSignalStartTick > 0 &&
                                  nowTick - _muteLowSignalStartTick >= LowSignalMuteThresholdMs;

        return new AudioSignalState(audioSignalPresent, audioMutedSuspected);
    }

    private bool UpdateRecordingFileGrowthState(
        ViewModelRuntimeSnapshot viewModelSnapshot,
        RecordingStats recordingStats,
        bool recordingStarted,
        long nowTick)
    {
        if (recordingStarted)
        {
            _lastRecordedBytes = recordingStats.TotalBytes;
            _recordingNoGrowthStartTick = 0;
        }

        var totalBytes = recordingStats.TotalBytes;
        if (!viewModelSnapshot.IsRecording)
        {
            _lastRecordedBytes = totalBytes;
            _recordingNoGrowthStartTick = 0;
            return false;
        }

        var recordingFileGrowing = true;
        if (totalBytes > _lastRecordedBytes)
        {
            _recordingNoGrowthStartTick = 0;
        }
        else
        {
            if (_recordingNoGrowthStartTick == 0)
            {
                _recordingNoGrowthStartTick = nowTick;
            }

            recordingFileGrowing = nowTick - _recordingNoGrowthStartTick < RecordingNoGrowthThresholdMs;
        }

        _lastRecordedBytes = totalBytes;
        return recordingFileGrowing;
    }

    private readonly record struct AudioSignalState(bool SignalPresent, bool MutedSuspected);
}
