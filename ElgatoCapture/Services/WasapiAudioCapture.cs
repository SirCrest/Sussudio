using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;

namespace ElgatoCapture.Services;

internal sealed class WasapiAudioCapture : IAsyncDisposable
{
    private const int OutputSampleRate = 48_000;
    private const int OutputChannels = 2;
    private const int BytesPerFloatSample = 4;
    private const int OutputBlockAlign = OutputChannels * BytesPerFloatSample;
    private const int AudioLevelFireIntervalMs = 66;
    private const uint WaitTimeoutMs = 100;

    private IMMDeviceEnumerator? _deviceEnumerator;
    private IMMDevice? _device;
    private IAudioClient? _audioClient;
    private IAudioClient3? _audioClient3;
    private IAudioCaptureClient? _audioCaptureClient;
    private AutoResetEvent? _captureEvent;
    private Thread? _captureThread;
    private WasapiAudioPlayback? _playback;
    private WasapiAudioFormat _captureFormat;
    private IRecordingSink? _recordingSink;
    private long _audioFramesArrived;
    private long _audioFramesWrittenToSink;
    private long _audioLevelLastFireTick;
    private long _audioLevelEventsFired;
    private long _audioLevelEventsLastFireTickMs;
    private long _resampleRemainderNumerator;
    private long _captureCallbackCount;
    private long _lastCaptureCallbackTickMs;
    private int _captureCallbackSilenceCount;
    private readonly object _captureCallbackIntervalLock = new();
    private readonly double[] _captureCallbackIntervalWindowMs = new double[100];
    private int _captureCallbackIntervalCount;
    private int _captureCallbackIntervalIndex;
    private int _initialized;
    private int _capturing;
    private int _stopRequested;
    private int _disposed;
    private bool _fastPathCopy;

    public event EventHandler<AudioLevelEventArgs>? AudioLevelUpdated;
    public event EventHandler<Exception>? CaptureFailed;

    public bool IsCapturing => Volatile.Read(ref _capturing) != 0;

    public long AudioFramesArrived => Interlocked.Read(ref _audioFramesArrived);

    public long AudioFramesWrittenToSink => Interlocked.Read(ref _audioFramesWrittenToSink);

    public long CaptureCallbackCount => Interlocked.Read(ref _captureCallbackCount);

    public double CaptureCallbackAvgIntervalMs => GetCaptureCallbackIntervalMetrics().AverageIntervalMs;

    public double CaptureCallbackMaxIntervalMs => GetCaptureCallbackIntervalMetrics().MaxIntervalMs;

    public int CaptureCallbackSilenceCount => Volatile.Read(ref _captureCallbackSilenceCount);

    public long LastCaptureCallbackTickMs => Interlocked.Read(ref _lastCaptureCallbackTickMs);

    public long AudioLevelEventsFired => Interlocked.Read(ref _audioLevelEventsFired);

    public long AudioLevelEventsLastFireTickMs => Interlocked.Read(ref _audioLevelEventsLastFireTickMs);

    public Task InitializeAsync(string audioDeviceId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Volatile.Read(ref _initialized) != 0)
        {
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(audioDeviceId))
        {
            throw new ArgumentException("Audio device id is required.", nameof(audioDeviceId));
        }

        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;
        IAudioClient? audioClient = null;
        IAudioClient3? audioClient3 = null;
        IAudioCaptureClient? audioCaptureClient = null;
        AutoResetEvent? captureEvent = null;
        IntPtr mixFormat = IntPtr.Zero;
        IntPtr desiredFormat = IntPtr.Zero;
        IntPtr selectedFormat = IntPtr.Zero;
        var useDesiredFormat = false;

        try
        {
            enumerator = WasapiComInterop.CreateDeviceEnumerator();
            var hrGetDevice = enumerator.GetDevice(audioDeviceId, out device);
            if (hrGetDevice < 0)
            {
                throw new InvalidOperationException(
                    $"WASAPI audio capture device '{audioDeviceId}' was not found (hr=0x{hrGetDevice:X8}).");
            }

            audioClient = WasapiComInterop.ActivateAudioClient(device, out audioClient3);
            WasapiComInterop.ThrowIfFailed(
                audioClient.GetMixFormat(out mixFormat),
                "IAudioClient.GetMixFormat(capture)");

            desiredFormat = WasapiComInterop.AllocFloatStereo48kFormat();
            var hrFormat = audioClient.IsFormatSupported(
                WasapiComInterop.AUDCLNT_SHAREMODE_SHARED,
                desiredFormat,
                out var closestMatch);
            if (closestMatch != IntPtr.Zero)
            {
                WasapiComInterop.CoTaskMemFree(closestMatch);
            }

            useDesiredFormat = hrFormat == WasapiComInterop.S_OK;
            selectedFormat = useDesiredFormat ? desiredFormat : mixFormat;
            _captureFormat = WasapiComInterop.ReadAudioFormat(selectedFormat);

            if (!WasapiComInterop.TryInitializeSharedStreamWithAudioClient3(audioClient3, selectedFormat))
            {
                WasapiComInterop.ThrowIfFailed(
                    audioClient.Initialize(
                        WasapiComInterop.AUDCLNT_SHAREMODE_SHARED,
                        WasapiComInterop.AUDCLNT_STREAMFLAGS_EVENTCALLBACK,
                        0,
                        0,
                        selectedFormat,
                        IntPtr.Zero),
                    "IAudioClient.Initialize(capture)");
            }

            captureEvent = new AutoResetEvent(false);
            WasapiComInterop.ThrowIfFailed(
                audioClient.SetEventHandle(captureEvent.SafeWaitHandle.DangerousGetHandle()),
                "IAudioClient.SetEventHandle(capture)");

            var iidCaptureClient = WasapiComInterop.IID_IAudioCaptureClient;
            WasapiComInterop.ThrowIfFailed(
                audioClient.GetService(ref iidCaptureClient, out var captureClientObject),
                "IAudioClient.GetService(IAudioCaptureClient)");
            audioCaptureClient = (IAudioCaptureClient)captureClientObject;

            _fastPathCopy = _captureFormat.SampleRate == OutputSampleRate &&
                            _captureFormat.Channels == OutputChannels &&
                            _captureFormat.SampleType == WasapiSampleType.Float32;
            _resampleRemainderNumerator = 0;
            _deviceEnumerator = enumerator;
            _device = device;
            _audioClient = audioClient;
            _audioClient3 = audioClient3;
            _audioCaptureClient = audioCaptureClient;
            _captureEvent = captureEvent;
            Interlocked.Exchange(ref _audioFramesArrived, 0);
            Interlocked.Exchange(ref _audioFramesWrittenToSink, 0);
            Interlocked.Exchange(ref _audioLevelLastFireTick, 0);
            Interlocked.Exchange(ref _audioLevelEventsFired, 0);
            Interlocked.Exchange(ref _audioLevelEventsLastFireTickMs, 0);
            Interlocked.Exchange(ref _captureCallbackCount, 0);
            Interlocked.Exchange(ref _lastCaptureCallbackTickMs, 0);
            Volatile.Write(ref _captureCallbackSilenceCount, 0);
            lock (_captureCallbackIntervalLock)
            {
                Array.Clear(_captureCallbackIntervalWindowMs, 0, _captureCallbackIntervalWindowMs.Length);
                _captureCallbackIntervalCount = 0;
                _captureCallbackIntervalIndex = 0;
            }
            Interlocked.Exchange(ref _initialized, 1);

            Logger.Log(
                "WASAPI capture initialized: " +
                $"device={audioDeviceId} " +
                $"selected={(useDesiredFormat ? "f32-48k-stereo" : "mix-format")} " +
                $"sample_rate={_captureFormat.SampleRate} " +
                $"channels={_captureFormat.Channels} " +
                $"bits={_captureFormat.BitsPerSample} " +
                $"type={_captureFormat.SampleType}");

            return Task.CompletedTask;
        }
        catch
        {
            captureEvent?.Dispose();
            WasapiComInterop.ReleaseComObject(ref audioCaptureClient);
            WasapiComInterop.ReleaseComObject(ref audioClient3);
            WasapiComInterop.ReleaseComObject(ref audioClient);
            WasapiComInterop.ReleaseComObject(ref device);
            WasapiComInterop.ReleaseComObject(ref enumerator);
            throw;
        }
        finally
        {
            if (desiredFormat != IntPtr.Zero)
            {
                WasapiComInterop.CoTaskMemFree(desiredFormat);
            }

            if (mixFormat != IntPtr.Zero)
            {
                WasapiComInterop.CoTaskMemFree(mixFormat);
            }
        }
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        if (Volatile.Read(ref _initialized) == 0)
        {
            throw new InvalidOperationException("WASAPI capture must be initialized before start.");
        }

        if (Interlocked.CompareExchange(ref _capturing, 1, 0) != 0)
        {
            return;
        }

        Interlocked.Exchange(ref _stopRequested, 0);
        _captureThread = new Thread(CaptureThreadMain)
        {
            IsBackground = true,
            Name = "WASAPI Capture",
            Priority = ThreadPriority.AboveNormal
        };

        try
        {
            _captureThread.Start();
            WasapiComInterop.ThrowIfFailed(_audioClient!.Start(), "IAudioClient.Start(capture)");
            Logger.Log("WASAPI capture started.");
        }
        catch
        {
            Interlocked.Exchange(ref _stopRequested, 1);
            _captureEvent?.Set();
            if (_captureThread?.IsAlive == true)
            {
                _captureThread.Join();
            }

            _captureThread = null;
            Interlocked.Exchange(ref _capturing, 0);
            throw;
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

    internal void SetPlayback(WasapiAudioPlayback? playback)
    {
        _playback = playback;
    }

    public Task StopAsync()
    {
        if (Interlocked.CompareExchange(ref _capturing, 0, 1) != 1)
        {
            return Task.CompletedTask;
        }

        Interlocked.Exchange(ref _stopRequested, 1);
        _captureEvent?.Set();
        try
        {
            _audioClient?.Stop();
        }
        catch (Exception ex)
        {
            Logger.Log($"WASAPI capture stop warning: {ex.Message}");
        }

        var thread = _captureThread;
        _captureThread = null;
        if (thread != null && thread.IsAlive)
        {
            thread.Join();
        }

        Logger.Log("WASAPI capture stopped.");
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        await StopAsync().ConfigureAwait(false);
        Volatile.Write(ref _recordingSink, null);
        _playback = null;

        _captureEvent?.Dispose();
        _captureEvent = null;
        WasapiComInterop.ReleaseComObject(ref _audioCaptureClient);
        WasapiComInterop.ReleaseComObject(ref _audioClient3);
        WasapiComInterop.ReleaseComObject(ref _audioClient);
        WasapiComInterop.ReleaseComObject(ref _device);
        WasapiComInterop.ReleaseComObject(ref _deviceEnumerator);
    }

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

                var sink = Volatile.Read(ref _recordingSink);
                if (sink != null)
                {
                    try
                    {
                        sink.WriteAudioAsync(new ReadOnlyMemory<byte>(convertedBuffer, 0, converted.Length))
                            .GetAwaiter()
                            .GetResult();
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

                var playback = _playback;
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

    private void RaiseAudioLevelIfDue(ReadOnlySpan<byte> f32leBytes)
    {
        var handler = AudioLevelUpdated;
        if (handler == null || f32leBytes.Length == 0)
        {
            return;
        }

        var nowTick = Environment.TickCount64;
        var lastTick = Interlocked.Read(ref _audioLevelLastFireTick);
        if (nowTick - lastTick < AudioLevelFireIntervalMs)
        {
            return;
        }

        if (Interlocked.CompareExchange(ref _audioLevelLastFireTick, nowTick, lastTick) != lastTick)
        {
            return;
        }

        var samples = MemoryMarshal.Cast<byte, float>(f32leBytes);
        float peak = 0f;
        foreach (var sample in samples)
        {
            var abs = MathF.Abs(sample);
            if (abs > peak)
            {
                peak = abs;
            }
        }

        Interlocked.Increment(ref _audioLevelEventsFired);
        Interlocked.Exchange(ref _audioLevelEventsLastFireTickMs, nowTick);
        handler.Invoke(this, new AudioLevelEventArgs(peak, 0, peak >= 1.0f));
    }

    private void TrackCaptureCallback(long callbackTickMs)
    {
        Interlocked.Increment(ref _captureCallbackCount);
        var previousTickMs = Interlocked.Exchange(ref _lastCaptureCallbackTickMs, callbackTickMs);
        if (previousTickMs <= 0 || callbackTickMs <= previousTickMs)
        {
            return;
        }

        var intervalMs = callbackTickMs - previousTickMs;
        lock (_captureCallbackIntervalLock)
        {
            _captureCallbackIntervalWindowMs[_captureCallbackIntervalIndex] = intervalMs;
            _captureCallbackIntervalIndex = (_captureCallbackIntervalIndex + 1) % _captureCallbackIntervalWindowMs.Length;
            if (_captureCallbackIntervalCount < _captureCallbackIntervalWindowMs.Length)
            {
                _captureCallbackIntervalCount++;
            }
        }
    }

    private CallbackIntervalMetrics GetCaptureCallbackIntervalMetrics()
    {
        double[] intervals;
        lock (_captureCallbackIntervalLock)
        {
            if (_captureCallbackIntervalCount <= 0)
            {
                return new CallbackIntervalMetrics(0, 0, 0);
            }

            intervals = new double[_captureCallbackIntervalCount];
            for (var i = 0; i < _captureCallbackIntervalCount; i++)
            {
                var ringIndex = (_captureCallbackIntervalIndex - _captureCallbackIntervalCount + i + _captureCallbackIntervalWindowMs.Length)
                    % _captureCallbackIntervalWindowMs.Length;
                intervals[i] = _captureCallbackIntervalWindowMs[ringIndex];
            }
        }

        var sum = 0.0;
        var max = 0.0;
        for (var i = 0; i < intervals.Length; i++)
        {
            sum += intervals[i];
            if (intervals[i] > max)
            {
                max = intervals[i];
            }
        }

        return new CallbackIntervalMetrics(intervals.Length, sum / intervals.Length, max);
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
        catch
        {
            // Never throw from capture thread event fan-out.
        }
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

    private readonly record struct CallbackIntervalMetrics(int SampleCount, double AverageIntervalMs, double MaxIntervalMs);
}
