using System;
using System.Buffers;
using System.Runtime.InteropServices;
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

    private ConvertedAudioPacket ConvertToOutputFormat(IntPtr dataPtr, int inputFrames, bool silent)
    {
        if (inputFrames <= 0)
        {
            return default;
        }

        if (silent)
        {
            var outputFrames = ComputeResampledFrameCount(inputFrames);
            if (outputFrames <= 0)
            {
                return default;
            }

            var outputLength = outputFrames * OutputBlockAlign;
            var buffer = ArrayPool<byte>.Shared.Rent(outputLength);
            buffer.AsSpan(0, outputLength).Clear();
            return new ConvertedAudioPacket(buffer, outputLength, outputFrames, isPooled: true);
        }

        if (_fastPathCopy)
        {
            var outputLength = inputFrames * OutputBlockAlign;
            var buffer = ArrayPool<byte>.Shared.Rent(outputLength);
            Marshal.Copy(dataPtr, buffer, 0, outputLength);
            return new ConvertedAudioPacket(buffer, outputLength, inputFrames, isPooled: true);
        }

        var decodedSampleCount = inputFrames * OutputChannels;
        var decodedStereo = ArrayPool<float>.Shared.Rent(decodedSampleCount);
        try
        {
            DecodeToStereo(dataPtr, inputFrames, _captureFormat, decodedStereo);

            if (_captureFormat.SampleRate == OutputSampleRate)
            {
                var outputLength = decodedSampleCount * BytesPerFloatSample;
                var buffer = ArrayPool<byte>.Shared.Rent(outputLength);
                var outputSamples = MemoryMarshal.Cast<byte, float>(buffer.AsSpan(0, outputLength));
                decodedStereo.AsSpan(0, decodedSampleCount).CopyTo(outputSamples);
                return new ConvertedAudioPacket(buffer, outputLength, inputFrames, isPooled: true);
            }

            var outputFrames = ComputeResampledFrameCount(inputFrames);
            if (outputFrames <= 0)
            {
                return default;
            }

            var outputSampleCount = outputFrames * OutputChannels;
            var resampledStereo = ArrayPool<float>.Shared.Rent(outputSampleCount);
            try
            {
                ResampleStereoLinear(decodedStereo, inputFrames, resampledStereo, outputFrames);
                var outputLength = outputSampleCount * BytesPerFloatSample;
                var buffer = ArrayPool<byte>.Shared.Rent(outputLength);
                var outputSamples = MemoryMarshal.Cast<byte, float>(buffer.AsSpan(0, outputLength));
                resampledStereo.AsSpan(0, outputSampleCount).CopyTo(outputSamples);
                return new ConvertedAudioPacket(buffer, outputLength, outputFrames, isPooled: true);
            }
            finally
            {
                ArrayPool<float>.Shared.Return(resampledStereo);
            }
        }
        finally
        {
            ArrayPool<float>.Shared.Return(decodedStereo);
        }
    }

    private int ComputeResampledFrameCount(int inputFrames)
    {
        if (inputFrames <= 0)
        {
            return 0;
        }

        if (_captureFormat.SampleRate == OutputSampleRate)
        {
            return inputFrames;
        }

        var sampleRate = _captureFormat.SampleRate;
        var scaledFrames = ((long)inputFrames * OutputSampleRate) + _resampleRemainderNumerator;
        if (scaledFrames <= 0)
        {
            return 0;
        }

        var outputFrames = (int)(scaledFrames / sampleRate);
        _resampleRemainderNumerator = scaledFrames % sampleRate;
        return outputFrames;
    }

    private static void ResampleStereoLinear(
        float[] sourceStereo,
        int sourceFrames,
        float[] destinationStereo,
        int outputFrames)
    {
        if (outputFrames <= 0)
        {
            return;
        }

        if (sourceFrames <= 1 || outputFrames == 1)
        {
            var sampleL = sourceStereo.Length >= 1 ? sourceStereo[0] : 0f;
            var sampleR = sourceStereo.Length >= 2 ? sourceStereo[1] : sampleL;
            for (var i = 0; i < outputFrames; i++)
            {
                destinationStereo[i * 2] = sampleL;
                destinationStereo[i * 2 + 1] = sampleR;
            }

            return;
        }

        var step = (sourceFrames - 1d) / (outputFrames - 1d);
        for (var i = 0; i < outputFrames; i++)
        {
            var srcPosition = i * step;
            var srcIndex = (int)srcPosition;
            var srcNext = Math.Min(srcIndex + 1, sourceFrames - 1);
            var frac = (float)(srcPosition - srcIndex);

            var leftA = sourceStereo[srcIndex * 2];
            var rightA = sourceStereo[srcIndex * 2 + 1];
            var leftB = sourceStereo[srcNext * 2];
            var rightB = sourceStereo[srcNext * 2 + 1];

            destinationStereo[i * 2] = leftA + ((leftB - leftA) * frac);
            destinationStereo[i * 2 + 1] = rightA + ((rightB - rightA) * frac);
        }
    }

    private static unsafe void DecodeToStereo(IntPtr dataPtr, int frameCount, WasapiAudioFormat format, float[] destinationStereo)
    {
        var frameStride = format.BlockAlign > 0
            ? format.BlockAlign
            : format.Channels * format.BytesPerSample;

        var origin = (byte*)dataPtr.ToPointer();
        for (var frame = 0; frame < frameCount; frame++)
        {
            var framePtr = origin + (frame * frameStride);
            if (format.Channels <= 1)
            {
                var mono = ReadSample(framePtr, 0, format);
                destinationStereo[frame * 2] = mono;
                destinationStereo[frame * 2 + 1] = mono;
                continue;
            }

            destinationStereo[frame * 2] = ReadSample(framePtr, 0, format);
            destinationStereo[frame * 2 + 1] = ReadSample(framePtr, 1, format);
        }
    }

    private static unsafe float ReadSample(byte* framePtr, int channelIndex, WasapiAudioFormat format)
    {
        var samplePtr = framePtr + (channelIndex * format.BytesPerSample);
        return format.SampleType switch
        {
            WasapiSampleType.Float32 => *(float*)samplePtr,
            WasapiSampleType.Float64 => (float)(*(double*)samplePtr),
            WasapiSampleType.Pcm16 => *(short*)samplePtr / 32768f,
            WasapiSampleType.Pcm24 => ReadPcm24(samplePtr),
            WasapiSampleType.Pcm32 => *(int*)samplePtr / 2147483648f,
            _ => 0f
        };
    }

    private static unsafe float ReadPcm24(byte* samplePtr)
    {
        var value = samplePtr[0] | (samplePtr[1] << 8) | (samplePtr[2] << 16);
        if ((value & 0x00800000) != 0)
        {
            value |= unchecked((int)0xFF000000);
        }

        return (value << 8) / 2147483648f;
    }

    private static void ReturnPacketBuffer(ConvertedAudioPacket packet)
    {
        if (!packet.IsPooled || packet.Buffer == null)
        {
            return;
        }

        ArrayPool<byte>.Shared.Return(packet.Buffer);
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

    private readonly struct ConvertedAudioPacket
    {
        public ConvertedAudioPacket(byte[] buffer, int length, int frames, bool isPooled)
        {
            Buffer = buffer;
            Length = length;
            Frames = frames;
            IsPooled = isPooled;
        }

        public byte[]? Buffer { get; }

        public int Length { get; }

        public int Frames { get; }

        public bool IsPooled { get; }
    }
}
