using System;
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

}
