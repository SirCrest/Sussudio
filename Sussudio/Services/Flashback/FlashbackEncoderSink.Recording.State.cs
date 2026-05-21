using System;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    public TimeSpan LastRecordingStartPts { get; private set; }
    public TimeSpan LastRecordingEndPts { get; private set; }
    public bool IsRecordingActive => Volatile.Read(ref _recordingActive) != 0;

    public bool CanBeginRecording
    {
        get
        {
            lock (_sync)
            {
                return !_disposed &&
                       _started &&
                       _encodingFailure == null &&
                       Volatile.Read(ref _recordingActive) == 0 &&
                       !_bufferManager.IsSessionPreservedForRecovery &&
                       !IsForceRotateActive &&
                       _encodingTask?.IsCompleted != true;
            }
        }
    }
}
