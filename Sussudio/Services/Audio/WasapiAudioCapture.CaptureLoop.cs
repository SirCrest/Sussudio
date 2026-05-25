using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Recording;

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

                handoffToPlayback = DispatchConvertedAudioPacket(converted);
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

    public void AttachRecordingSink(IRecordingSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        Volatile.Write(ref _recordingSink, sink);
    }

    public void DetachRecordingSink()
    {
        Volatile.Write(ref _recordingSink, null);
    }

    public void AttachFlashbackSink(IRecordingSink sink)
    {
        ArgumentNullException.ThrowIfNull(sink);
        Volatile.Write(ref _flashbackSink, sink);
    }

    public void DetachFlashbackSink()
    {
        Volatile.Write(ref _flashbackSink, null);
    }

    public void SetAudioWriter(Func<ReadOnlyMemory<byte>, Task>? writer)
    {
        // Runs on the WASAPI capture thread. Writers must copy/enqueue
        // synchronously and return a completed task; incomplete tasks are
        // rejected instead of being waited on in the callback path.
        Volatile.Write(ref _audioWriter, writer);
    }

    internal void SetPlayback(WasapiAudioPlayback? playback)
    {
        Volatile.Write(ref _playback, playback);
    }

    private static void InvokeHotAudioWriter(
        Func<ReadOnlyMemory<byte>, Task> writer,
        ReadOnlyMemory<byte> samples,
        string target)
        => CompleteHotAudioWrite(writer(samples), target);

    private bool DispatchConvertedAudioPacket(ConvertedAudioPacket converted)
    {
        var convertedBuffer = converted.Buffer;
        if (convertedBuffer == null || converted.Length <= 0 || converted.Frames <= 0)
        {
            return false;
        }

        var samples = new ReadOnlyMemory<byte>(convertedBuffer, 0, converted.Length);
        var audioWriter = Volatile.Read(ref _audioWriter);
        if (audioWriter != null)
        {
            try
            {
                InvokeHotAudioWriter(audioWriter, samples, "delegate");
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
                    WriteAudioToSinkOnCaptureThread(sink, samples, "recording");
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
                WriteAudioToSinkOnCaptureThread(flashbackSink, samples, "flashback");
            }
            catch (Exception ex)
            {
                Logger.Log($"WASAPI_FLASHBACK_AUDIO_FAIL type={ex.GetType().Name} msg={ex.Message}");
            }
        }

        var playback = Volatile.Read(ref _playback);
        if (playback == null)
        {
            return false;
        }

        playback.EnqueuePooledSamples(convertedBuffer, converted.Length);
        return true;
    }

    private static void WriteAudioToSinkOnCaptureThread(
        IRecordingSink sink,
        ReadOnlyMemory<byte> samples,
        string target)
        => CompleteHotAudioWrite(sink.WriteAudioAsync(samples), target);

    private static void CompleteHotAudioWrite(Task task, string target)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (!task.IsCompleted)
        {
            throw new InvalidOperationException(
                $"{target} audio writer returned an incomplete Task on the WASAPI capture thread. " +
                "Audio writers must copy/enqueue synchronously and return Task.CompletedTask.");
        }

        task.GetAwaiter().GetResult();
    }
}
