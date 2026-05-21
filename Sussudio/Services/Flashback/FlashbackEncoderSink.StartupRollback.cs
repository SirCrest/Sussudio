using System;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private void RollBackStartFailure(Exception ex, string? startupGeneratedSegmentPath)
    {
        Logger.Log($"FLASHBACK_SINK_START_FAIL type={ex.GetType().Name} msg='{ex.Message}'");
        CompleteWriter(_videoQueue);
        CompleteWriter(_audioQueue);
        CompleteWriter(_microphoneQueue);
        CompleteWriter(_gpuQueue);
        _videoQueue = null;
        _audioQueue = null;
        _microphoneQueue = null;
        _gpuQueue = null;
        _gpuEncodingEnabled = false;
        lock (_sync)
        {
            _started = false;
        }
        DisposeCtsBestEffort(_cts, "start_fail");
        _cts = null;
        _encodingTask = null;
        _sessionContext = null;
        _width = 0;
        _height = 0;
        _audioEnabled = false;
        _microphoneEnabled = false;
        _tsFilePath = null;
        _recordingOutputPath = string.Empty;
        _segmentStartPts = TimeSpan.Zero;
        _segmentDuration = TimeSpan.Zero;
        _ptsBaseOffset = TimeSpan.Zero;
        Interlocked.Exchange(ref _segmentStartBytes, 0);

        DisposeEncoderBestEffort("start_fail");
        if (_ownsBufferManager)
        {
            _bufferManager.PurgeAllSegments();
        }
        else if (startupGeneratedSegmentPath != null)
        {
            _bufferManager.AbandonGeneratedSegmentPath(startupGeneratedSegmentPath, restoreActivePath: null);
        }
    }
}
