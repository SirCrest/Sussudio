using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Recording;
using Sussudio.Services.Telemetry;

namespace Sussudio.Services.Audio;

// Event-driven WASAPI capture for HDMI, custom audio input, and microphone
// sources. It normalizes capture into f32le 48 kHz stereo blocks, fans samples
// out to monitoring/recording/Flashback, and keeps low-volume glitch counters
// for automation diagnostics.
internal sealed partial class WasapiAudioCapture : IAsyncDisposable
{
    private const int OutputSampleRate = 48_000;
    private const int OutputChannels = 2;
    private const int BytesPerFloatSample = 4;
    private const int OutputBlockAlign = OutputChannels * BytesPerFloatSample;
    private const int AudioLevelFireIntervalMs = 66;
    private const double SevereCallbackGapMultiplier = 4.0;
    private const uint WaitTimeoutMs = 100;
    private static readonly TimeSpan CaptureThreadJoinTimeout = TimeSpan.FromSeconds(3);

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
    private IRecordingSink? _flashbackSink;
    private Func<ReadOnlyMemory<byte>, Task>? _audioWriter;
    private long _audioFramesArrived;
    private long _audioFramesWrittenToSink;
    private long _audioLevelLastFireTick;
    private long _audioLevelEventsFired;
    private long _audioLevelEventsLastFireTickMs;

    // Integer remainder carried between callbacks when the endpoint mix format
    // does not divide evenly into the 48 kHz output rate. This avoids slow
    // sample-count drift without bringing a full resampler into the hot path.
    private long _resampleRemainderNumerator;
    private long _captureCallbackCount;
    private long _lastCaptureCallbackTickMs;
    private long _captureCallbackSevereGapCount;
    private long _audioDataDiscontinuityCount;
    private long _audioTimestampErrorCount;
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
            Interlocked.Exchange(ref _captureCallbackSevereGapCount, 0);
            Interlocked.Exchange(ref _audioDataDiscontinuityCount, 0);
            Interlocked.Exchange(ref _audioTimestampErrorCount, 0);
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
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in WasapiAudioCapture.InitializeAsync: {ex.Message}");
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
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in WasapiAudioCapture.StartCapture: {ex.Message}");
            Interlocked.Exchange(ref _stopRequested, 1);
            _captureEvent?.Set();
            if (_captureThread?.IsAlive == true)
            {
                JoinCaptureThread(_captureThread, "WASAPI_CAPTURE_THREAD_JOIN_TIMEOUT_START_FAILURE");
            }

            _captureThread = null;
            Interlocked.Exchange(ref _capturing, 0);
            throw;
        }
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
            JoinCaptureThread(thread, "WASAPI_CAPTURE_THREAD_JOIN_TIMEOUT_STOP");
        }

        Logger.Log("WASAPI capture stopped.");
        return Task.CompletedTask;
    }

    private static bool JoinCaptureThread(Thread thread, string timeoutEvent)
    {
        if (thread.Join(CaptureThreadJoinTimeout))
        {
            return true;
        }

        Logger.Log(timeoutEvent);
        return false;
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        await StopAsync().ConfigureAwait(false);
        Volatile.Write(ref _recordingSink, null);
        Volatile.Write(ref _flashbackSink, null);
        Volatile.Write(ref _audioWriter, null);
        Volatile.Write(ref _playback, null);

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
