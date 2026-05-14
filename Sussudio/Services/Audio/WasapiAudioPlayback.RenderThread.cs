using System;
using System.Diagnostics;
using System.Threading;

namespace Sussudio.Services.Audio;

internal sealed partial class WasapiAudioPlayback
{
    private void RenderThreadMain()
    {
        var renderEvent = _renderEvent;
        if (renderEvent == null)
        {
            return;
        }

        var waitHandle = renderEvent.SafeWaitHandle.DangerousGetHandle();
        while (Volatile.Read(ref _started) != 0)
        {
            var waitResult = WasapiComInterop.WaitForSingleObject(waitHandle, WaitTimeoutMs);
            if (waitResult == WasapiComInterop.WaitTimeout)
            {
                continue;
            }

            if (waitResult != WasapiComInterop.WaitObject0)
            {
                continue;
            }

            if (Volatile.Read(ref _started) == 0)
            {
                return;
            }

            // Handle pause request on the render thread to avoid cross-thread WASAPI calls
            if (_pauseRequested)
            {
                _pauseRequested = false;
                try
                {
                    _audioClient?.Stop();
                    _audioClient?.Reset();
                }
                catch (Exception ex)
                {
                    Logger.Log($"WASAPI_PAUSE_RENDER_WARN: {ex.Message}");
                }
                Flush();
                Interlocked.Exchange(ref _renderingPaused, 1);
                Logger.Log("WASAPI_PLAYBACK_RENDER_PAUSED");
                if (!_resumeRequested)
                {
                    continue;
                }
            }

            // Handle resume request on the render thread to avoid cross-thread WASAPI calls
            if (_resumeRequested)
            {
                _resumeRequested = false;
                if (Volatile.Read(ref _renderingPaused) == 0)
                {
                    Logger.Log("WASAPI_PLAYBACK_RENDER_RESUME_CANCELED_PENDING_PAUSE");
                    continue;
                }

                WaitForResumePrebuffer();
                try { _audioClient?.Start(); }
                catch (Exception ex) { Logger.Log($"WASAPI_RESUME_RENDER_WARN: {ex.Message}"); }
                Interlocked.Exchange(ref _renderingPaused, 0);
                Logger.Log("WASAPI_PLAYBACK_RENDER_RESUMED");
                continue;
            }

            try
            {
                RenderAvailableFrames();
            }
            catch (Exception ex)
            {
                Logger.Log($"WASAPI playback render error: {ex.Message}");
            }
        }
    }

    private void WaitForResumePrebuffer()
    {
        var targetFrames = Volatile.Read(ref _resumePrebufferFrames);
        var timeoutMs = Volatile.Read(ref _resumePrebufferTimeoutMs);
        Volatile.Write(ref _resumePrebufferFrames, 0);
        Volatile.Write(ref _resumePrebufferTimeoutMs, 0);
        if (targetFrames <= 0)
        {
            return;
        }

        var start = Stopwatch.GetTimestamp();
        var timedOut = false;
        var bufferedFrames = PlaybackBufferedFramesForResume();
        while (bufferedFrames < targetFrames &&
               Volatile.Read(ref _started) != 0 &&
               Volatile.Read(ref _disposed) == 0)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            if (timeoutMs <= 0 || elapsedMs >= timeoutMs)
            {
                timedOut = true;
                break;
            }

            Thread.Sleep(Math.Min(5, Math.Max(1, timeoutMs - (int)elapsedMs)));
            bufferedFrames = PlaybackBufferedFramesForResume();
        }

        var waitedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        Logger.Log(
            $"WASAPI_PLAYBACK_RENDER_PREBUFFER target_ms={FramesToMilliseconds(targetFrames):F1} actual_ms={FramesToMilliseconds(bufferedFrames):F1} waited_ms={waitedMs:F1} timed_out={timedOut}");
    }

    private int PlaybackBufferedFramesForResume()
    {
        var queuedFrames = Volatile.Read(ref _playbackQueueFrames);
        var activeFrames = Volatile.Read(ref _activeChunkRemainingFrames);
        return Math.Max(0, queuedFrames) + Math.Max(0, activeFrames);
    }

    private unsafe void RenderAvailableFrames()
    {
        if (_audioClient == null || _audioRenderClient == null || _bufferFrameCount == 0)
        {
            return;
        }

        if (Volatile.Read(ref _renderingPaused) != 0) return;

        Interlocked.Increment(ref _renderCallbackCount);
        Interlocked.Exchange(ref _lastRenderCallbackTickMs, Environment.TickCount64);

        WasapiComInterop.ThrowIfFailed(
            _audioClient.GetCurrentPadding(out var paddingFrames),
            "IAudioClient.GetCurrentPadding(render)");

        if (paddingFrames >= _bufferFrameCount)
        {
            return;
        }

        var framesToWrite = Math.Min(_bufferFrameCount - paddingFrames, MaxRenderWriteFrames);
        if (framesToWrite == 0)
        {
            return;
        }

        WasapiComInterop.ThrowIfFailed(
            _audioRenderClient.GetBuffer(framesToWrite, out var destination),
            "IAudioRenderClient.GetBuffer");

        try
        {
            var bytesToWrite = checked((int)framesToWrite * OutputBlockAlign);
            var destinationSpan = new Span<byte>((void*)destination, bytesToWrite);
            lock (_chunkLock)
            {
                FillRenderBuffer(destinationSpan);
            }
            ApplyVolume(destinationSpan);
            UpdateOutputLevel(destinationSpan);
            Volatile.Write(ref _endpointQueuedFrames, checked((int)Math.Min(int.MaxValue, paddingFrames + framesToWrite)));
        }
        finally
        {
            WasapiComInterop.ThrowIfFailed(
                _audioRenderClient.ReleaseBuffer(framesToWrite, 0),
                "IAudioRenderClient.ReleaseBuffer");
        }
    }

    private void FillRenderBuffer(Span<byte> destination)
    {
        var written = 0;
        while (written < destination.Length)
        {
            if (!_hasActiveChunk || _activeChunkOffset >= _activeChunk.Length)
            {
                ReturnActiveChunk();
                if (!TryDequeueChunk(out _activeChunk))
                {
                    Interlocked.Increment(ref _renderSilenceCount);
                    Volatile.Write(ref _activeChunkRemainingFrames, 0);
                    destination[written..].Clear();
                    return;
                }

                _activeChunkOffset = 0;
                _hasActiveChunk = true;
                if (_activeChunk.PtsTicks != 0)
                    Interlocked.Exchange(ref _renderingPtsTicks, _activeChunk.PtsTicks);
            }

            var activeBuffer = _activeChunk.Buffer;
            if (activeBuffer == null)
            {
                destination[written..].Clear();
                ReturnActiveChunk();
                Volatile.Write(ref _activeChunkRemainingFrames, 0);
                return;
            }

            var available = _activeChunk.Length - _activeChunkOffset;
            var copyLength = Math.Min(destination.Length - written, available);
            activeBuffer.AsSpan(_activeChunkOffset, copyLength).CopyTo(destination[written..]);
            _activeChunkOffset += copyLength;
            UpdateRenderingPtsForActiveChunk();
            UpdateActiveChunkRemainingFrames();
            written += copyLength;
        }
    }

    private void UpdateRenderingPtsForActiveChunk()
    {
        if (_activeChunk.PtsTicks == 0) return;

        var frameOffset = Math.Max(0, _activeChunkOffset) / OutputBlockAlign;
        var offsetTicks = frameOffset * TimeSpan.TicksPerSecond / OutputSampleRate;
        Interlocked.Exchange(ref _renderingPtsTicks, _activeChunk.PtsTicks + offsetTicks);
    }
}
