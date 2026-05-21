using System;
using System.Threading;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    private void OnVideoFrameEncoded()
    {
        if (_disposed)
        {
            return;
        }

        Interlocked.Exchange(ref _lastVideoWriteTick, Environment.TickCount64);
        var encoded = Interlocked.Increment(ref _encodedVideoFrames);

        var pts = ResolveEncoderPts();
        if (pts > TimeSpan.Zero)
        {
            _bufferManager.UpdateLatestPts(pts);

            // Segment rotation happens on the encoding thread, so no extra lock is needed here.
            if (_segmentDuration > TimeSpan.Zero && pts - _segmentStartPts >= _segmentDuration)
            {
                _ = RotateSegment(pts);
            }
        }

        // Refresh disk bytes ~4 Hz so the monotonic counter stays current for UI
        // bitrate sampling; the prior frame-count gate plateaued for ~5 s at 60 fps.
        var nowMs = Environment.TickCount64;
        if (nowMs - _lastDiskBytesUpdateMs >= 250)
        {
            _lastDiskBytesUpdateMs = nowMs;
            _bufferManager.UpdateDiskBytes(_encoder.TotalBytesWritten);
        }

        // NOTE: This event fires on the encoding background thread, NOT the UI thread.
        // Handlers must marshal to DispatcherQueue if they need to update UI state.
        if (!_disposed && Volatile.Read(ref _recordingActive) == 1)
        {
            try
            {
                FrameEncoded?.Invoke(this, encoded);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_SINK_FRAME_EVENT_FAIL type={ex.GetType().Name} msg={ex.Message}");
            }
        }
    }

    private TimeSpan ResolveEncoderPts()
    {
        var frameRate = ResolveSessionFrameRate(_sessionContext?.FrameRate ?? 30.0);
        var seconds = _encoder.NextVideoPts / frameRate;
        if (!double.IsFinite(seconds) || seconds <= 0)
        {
            return _ptsBaseOffset;
        }

        if (seconds >= TimeSpan.MaxValue.TotalSeconds)
        {
            return TimeSpan.MaxValue;
        }

        var delta = TimeSpan.FromSeconds(seconds);
        return _ptsBaseOffset > TimeSpan.MaxValue - delta
            ? TimeSpan.MaxValue
            : _ptsBaseOffset + delta;
    }
}
