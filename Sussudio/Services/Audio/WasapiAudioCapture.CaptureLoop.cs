using System;
using System.Threading;

namespace Sussudio.Services.Audio;

internal sealed partial class WasapiAudioCapture
{
    private void CaptureThreadMain()
    {
        var captureEvent = _captureEvent;
        if (captureEvent == null)
        {
            return;
        }

        var waitHandle = captureEvent.SafeWaitHandle.DangerousGetHandle();
        while (Volatile.Read(ref _stopRequested) == 0)
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

            if (Volatile.Read(ref _stopRequested) != 0)
            {
                return;
            }

            try
            {
                TrackCaptureCallback(Environment.TickCount64);
                DrainCapturePackets();
            }
            catch (Exception ex)
            {
                Logger.Log($"WASAPI capture loop error: {ex.Message}");
                OnCaptureFailed(ex);
            }
        }
    }

    private void DrainCapturePackets()
    {
        if (_audioCaptureClient == null)
        {
            return;
        }

        while (Volatile.Read(ref _stopRequested) == 0)
        {
            WasapiComInterop.ThrowIfFailed(
                _audioCaptureClient.GetNextPacketSize(out var packetFrames),
                "IAudioCaptureClient.GetNextPacketSize");

            if (packetFrames == 0)
            {
                return;
            }

            WasapiComInterop.ThrowIfFailed(
                _audioCaptureClient.GetBuffer(
                    out var data,
                    out var availableFrames,
                    out var flags,
                    out _,
                    out _),
                "IAudioCaptureClient.GetBuffer");

            var converted = default(ConvertedAudioPacket);
            var handoffToPlayback = false;
            try
            {
                if (availableFrames == 0)
                {
                    Interlocked.Increment(ref _captureCallbackSilenceCount);
                    continue;
                }

                TrackCapturePacketFlags(flags);
                converted = ConvertToOutputFormat(
                    data,
                    (int)availableFrames,
                    (flags & WasapiComInterop.AUDCLNT_BUFFERFLAGS_SILENT) != 0);
                if (converted.Length <= 0 || converted.Frames <= 0 || converted.Buffer == null)
                {
                    continue;
                }

                Interlocked.Add(ref _audioFramesArrived, converted.Frames);
                var convertedBuffer = converted.Buffer;
                RaiseAudioLevelIfDue(convertedBuffer.AsSpan(0, converted.Length));

                var audioWriter = Volatile.Read(ref _audioWriter);
                if (audioWriter != null)
                {
                    try
                    {
                        InvokeHotAudioWriter(
                            audioWriter,
                            new ReadOnlyMemory<byte>(convertedBuffer, 0, converted.Length),
                            "delegate");
                        Interlocked.Add(ref _audioFramesWrittenToSink, converted.Frames);
                    }
                    catch (Exception ex)
                    {
                        Volatile.Write(ref _audioWriter, null);
                        Interlocked.Exchange(ref _stopRequested, 1);
                        _captureEvent?.Set();
                        throw new InvalidOperationException("WASAPI audio delegate write failed.", ex);
                    }
                }
                else
                {
                    var sink = Volatile.Read(ref _recordingSink);
                    if (sink != null)
                    {
                        try
                        {
                            WriteAudioToSinkOnCaptureThread(
                                sink,
                                new ReadOnlyMemory<byte>(convertedBuffer, 0, converted.Length),
                                "recording");
                            Interlocked.Add(ref _audioFramesWrittenToSink, converted.Frames);
                        }
                        catch (Exception ex)
                        {
                            Volatile.Write(ref _recordingSink, null);
                            Interlocked.Exchange(ref _stopRequested, 1);
                            _captureEvent?.Set();
                            throw new InvalidOperationException("WASAPI audio sink write failed.", ex);
                        }
                    }
                }

                var flashbackSink = Volatile.Read(ref _flashbackSink);
                if (flashbackSink != null)
                {
                    try
                    {
                        WriteAudioToSinkOnCaptureThread(
                            flashbackSink,
                            new ReadOnlyMemory<byte>(convertedBuffer, 0, converted.Length),
                            "flashback");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"WASAPI_FLASHBACK_AUDIO_FAIL type={ex.GetType().Name} msg={ex.Message}");
                    }
                }

                var playback = Volatile.Read(ref _playback);
                if (playback != null)
                {
                    playback.EnqueuePooledSamples(convertedBuffer, converted.Length);
                    handoffToPlayback = true;
                }
            }
            finally
            {
                if (!handoffToPlayback)
                {
                    ReturnPacketBuffer(converted);
                }

                WasapiComInterop.ThrowIfFailed(
                    _audioCaptureClient.ReleaseBuffer(availableFrames),
                    "IAudioCaptureClient.ReleaseBuffer");
            }
        }
    }

    private void OnCaptureFailed(Exception ex)
    {
        var handler = CaptureFailed;
        if (handler == null)
        {
            return;
        }

        try
        {
            handler.Invoke(this, ex);
        }
        catch (Exception fanOutEx)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in WasapiAudioCapture event fan-out: {fanOutEx.Message}");
        }
    }
}