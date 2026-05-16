using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Audio;
using Sussudio.Services.Contracts;
using Sussudio.Services.Flashback;
using Sussudio.Services.Gpu;
using Sussudio.Services.Preview;
using Sussudio.Services.Recording;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio.Services.Capture;

// Owns the single source-reader session used by both preview and recording.
// The important contract is fan-out: capture frames arrive once, then this
// class routes them to the live preview sink, optional Flashback sink, and
// optional user recording sink without starting a second device session.
internal sealed partial class UnifiedVideoCapture : IAsyncDisposable, ILiveVideoSource
{
    private readonly object _sync = new();
    private MfSourceReaderVideoCapture? _capture;
    private SharedD3DDeviceManager? _d3dManager;
    private CancellationTokenSource? _readCts;
    private IPreviewFrameSink? _previewSink;
    private IRecordingSink? _recordingSink;
    private IRawVideoFrameEncoder? _recordingEncoder;
    private IGpuVideoFrameEncoder? _gpuRecordingEncoder;
    private FlashbackEncoderSink? _flashbackSink;
    private ParallelMjpegDecodePipeline? _mjpegPipeline;
    private MjpegPreviewJitterBuffer? _mjpegPreviewJitterBuffer;
    private readonly FrameLedger _frameLedger = new();
    private readonly VisualCadenceTracker _visualCadenceTracker = new(cropLeft: 0.25, cropTop: 0.25, cropWidth: 0.5, cropHeight: 0.5);
    private readonly VisualCadenceTracker _visualCenterCadenceTracker = new(
        sampleColumns: 320,
        sampleRows: 180,
        cropLeft: 0.375,
        cropTop: 0.375,
        cropWidth: 0.25,
        cropHeight: 0.25);
    private bool _started;
    private bool _recordingActive;
    private bool _flashbackRecordingAccountingActive;
    private long _flashbackRecordingLastAcceptedSequence = -1;
    private long _flashbackRecordingSequenceGaps;
    private bool _disposed;
    private int _disposeStarted;
    private bool _isP010;
    private bool _isHighFrameRateMjpegMode;
    private bool _strictPreviewTextureRequired;
    private int _fatalErrorSignaled;
    private int _consecutiveTextureFailures;
    private int _visualCadenceCpuDataUnavailable;
    private const int MaxConsecutiveTextureFailures = 5;
    private int _width;
    private int _height;
    private double _fps;
    private string _nativeInputFormat = "unknown";
    private string _negotiatedFormat = "unknown";
    private long _videoFramesArrived;
    private long _videoFramesDropped;
    private long _livePreviewPresentId;
    private long _videoFramesWrittenToSink;
    private long _recordingFramesDelivered;
    private long _lastVideoFrameArrivedTick;
    private Action<string>? _pixelFormatDetectedCallback;
    private int _pixelFormatObserverFired;
    private volatile bool _previewSuppressed;

    public bool IsP010 => Volatile.Read(ref _isP010);
    public int Width => Volatile.Read(ref _width);
    public int Height => Volatile.Read(ref _height);
    public double Fps => Volatile.Read(ref _fps);
    public bool IsHighFrameRateMjpegMode => Volatile.Read(ref _isHighFrameRateMjpegMode);
    public bool IsSoftwareMjpegPipelineActive => Volatile.Read(ref _mjpegPipeline) != null;
    public string NativeInputFormat => Volatile.Read(ref _nativeInputFormat);
    public string NegotiatedFormat => Volatile.Read(ref _negotiatedFormat);
    public long VideoFramesArrived => Interlocked.Read(ref _videoFramesArrived);
    public long VideoFramesDropped
    {
        get
        {
            var captureDrops = _capture?.FramesDropped ?? 0;
            return Math.Max(captureDrops, Interlocked.Read(ref _videoFramesDropped));
        }
    }
    public long VideoFramesWrittenToSink => Interlocked.Read(ref _videoFramesWrittenToSink);
    public long RecordingFramesDelivered => Interlocked.Read(ref _recordingFramesDelivered);
    public long FlashbackRecordingSequenceGaps => Interlocked.Read(ref _flashbackRecordingSequenceGaps);
    public long LastVideoFrameArrivedTick => Interlocked.Read(ref _lastVideoFrameArrivedTick);
    public event EventHandler<Exception>? FatalErrorOccurred;
    public bool SourceReaderReadOutstanding => _capture?.IsReadSampleOutstanding ?? false;
    public long SourceReaderReadOutstandingMs => _capture?.ReadSampleOutstandingMs ?? 0;
    public long SourceReaderLastFrameTickMs => _capture?.LastFrameDeliveredTickMs ?? 0;
    public SharedD3DDeviceManager? D3DManager => Volatile.Read(ref _d3dManager);

    public void SetFlashbackSink(FlashbackEncoderSink? sink)
    {
        Volatile.Write(ref _flashbackSink, sink);
    }

    public void SetPixelFormatDetectedCallback(Action<string>? observer)
    {
        Volatile.Write(ref _pixelFormatDetectedCallback, observer);
    }

    public Task StartRecordingAsync(
        IRecordingSink sink,
        IRawVideoFrameEncoder encoder,
        IGpuVideoFrameEncoder? gpuEncoder = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(sink);
        ArgumentNullException.ThrowIfNull(encoder);

        lock (_sync)
        {
            if (_capture == null)
            {
                throw new InvalidOperationException("Cannot start recording before capture is initialized.");
            }

            _recordingSink = sink;
            Volatile.Write(ref _recordingEncoder, encoder);
            Volatile.Write(ref _gpuRecordingEncoder, gpuEncoder);
            Interlocked.Exchange(ref _recordingFramesDelivered, 0);
            Volatile.Write(ref _recordingActive, true);
        }

        return Task.CompletedTask;
    }

    public void BeginFlashbackRecordingAccounting()
    {
        Interlocked.Exchange(ref _videoFramesWrittenToSink, 0);
        Interlocked.Exchange(ref _recordingFramesDelivered, 0);
        Interlocked.Exchange(ref _flashbackRecordingLastAcceptedSequence, -1);
        Interlocked.Exchange(ref _flashbackRecordingSequenceGaps, 0);
        Volatile.Write(ref _flashbackRecordingAccountingActive, true);
    }

    public void EndFlashbackRecordingAccounting()
    {
        Volatile.Write(ref _flashbackRecordingAccountingActive, false);
    }

    public Task StopRecordingAsync()
    {
        lock (_sync)
        {
            Volatile.Write(ref _recordingActive, false);
            Volatile.Write(ref _flashbackRecordingAccountingActive, false);
            _recordingSink = null;
            Volatile.Write(ref _recordingEncoder, null);
            Volatile.Write(ref _gpuRecordingEncoder, null);
        }

        return Task.CompletedTask;
    }

    public void SetSkipCpuReadback(bool skip)
    {
        var capture = _capture;
        if (capture != null)
        {
            capture.SkipCpuReadback = skip;
        }
    }
}
