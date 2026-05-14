using System;
using System.Diagnostics;
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

    private void OnFrameArrived(ReadOnlySpan<byte> frameData, int width, int height, long arrivalTick)
    {
        var sourceSequence = Interlocked.Increment(ref _videoFramesArrived) - 1;
        Interlocked.Exchange(ref _lastVideoFrameArrivedTick, Environment.TickCount64);
        RecordCaptureArrived(sourceSequence, arrivalTick, width, height, frameData.Length);

        var pipeline = Volatile.Read(ref _mjpegPipeline);
        if (pipeline != null)
        {
            var accepted = pipeline.EnqueueFrame(frameData, width, height, arrivalTick);
            _frameLedger.RecordEvent(
                sourceSequence,
                FrameLedgerStage.CompressedQueued,
                subsystem: "mjpeg",
                byteDepth: frameData.Length,
                accepted: accepted,
                reason: accepted ? null : "mjpeg_queue_rejected");
            return;
        }

        var isP010 = Volatile.Read(ref _isP010);
        FirePixelFormatObserverOnce(isP010 ? "P010" : "NV12");

        EnqueueRecordingFrame(frameData, width, height, isP010, sourceSequence);
        EnqueueFlashbackFrame(frameData, width, height, isP010, sourceSequence);

        var previewSink = Volatile.Read(ref _previewSink);
        if (!_previewSuppressed && previewSink != null && !frameData.IsEmpty)
        {
            SubmitPreviewRawFrame(previewSink, frameData, width, height, isP010, arrivalTick, sourceSequence);
        }
    }

    private void OnMjpegPipelineFrameEmitted(PooledVideoFrame frame)
    {
        FirePixelFormatObserverOnce("NV12");
        _frameLedger.RecordEvent(
            frame.SequenceNumber,
            FrameLedgerStage.StrictOrderReleased,
            subsystem: "mjpeg");

        EnqueueRecordingFrame(frame);
        EnqueueFlashbackFrame(frame);
    }

    private void OnDualFrameArrived(
        IntPtr gpuTexture,
        int gpuSubresource,
        ReadOnlySpan<byte> frameData,
        int width,
        int height,
        long arrivalTick)
    {
        var sourceSequence = Interlocked.Increment(ref _videoFramesArrived) - 1;
        Interlocked.Exchange(ref _lastVideoFrameArrivedTick, Environment.TickCount64);
        RecordCaptureArrived(sourceSequence, arrivalTick, width, height, frameData.Length);

        var isP010 = Volatile.Read(ref _isP010);
        FirePixelFormatObserverOnce(isP010 ? "P010" : "NV12");

        var gpuEncoder = Volatile.Read(ref _gpuRecordingEncoder);
        if (gpuEncoder != null && gpuTexture != IntPtr.Zero)
        {
            EnqueueGpuRecordingFrame(gpuEncoder, gpuTexture, gpuSubresource, sourceSequence);
        }
        else
        {
            EnqueueRecordingFrame(frameData, width, height, isP010, sourceSequence);
        }

        if (gpuTexture != IntPtr.Zero)
        {
            EnqueueFlashbackGpuFrame(gpuTexture, gpuSubresource, sourceSequence);
        }
        else
        {
            EnqueueFlashbackFrame(frameData, width, height, isP010, sourceSequence);
        }

        var previewSink = Volatile.Read(ref _previewSink);
        if (!_previewSuppressed && previewSink != null)
        {
            var textureSubmitted = false;
            if (gpuTexture != IntPtr.Zero)
            {
                try
                {
                    var previewPresentId = Interlocked.Increment(ref _livePreviewPresentId);
                    var submitTick = Stopwatch.GetTimestamp();
                    previewSink.SubmitTexture(
                        gpuTexture,
                        gpuSubresource,
                        width,
                        height,
                        isP010,
                        PreviewFrameTracking.Default with
                        {
                            ArrivalTick = arrivalTick,
                            SourceSequenceNumber = sourceSequence,
                            PreviewPresentId = previewPresentId,
                            SchedulerSubmitTick = submitTick,
                        });
                    _frameLedger.RecordEvent(
                        sourceSequence,
                        FrameLedgerStage.PreviewEnqueued,
                        subsystem: "preview",
                        accepted: true);
                    textureSubmitted = true;
                    Interlocked.Exchange(ref _consecutiveTextureFailures, 0);
                    if (!frameData.IsEmpty)
                    {
                        TrackPreviewVisualFrame(
                            frameData,
                            width,
                            height,
                            isP010 ? PooledVideoPixelFormat.P010 : PooledVideoPixelFormat.Nv12,
                            arrivalTick,
                            sequenceNumber: sourceSequence);
                    }
                    else
                    {
                        MarkPreviewVisualCadenceUnavailable("d3d_texture_only");
                    }
                }
                catch (Exception ex)
                {
                    _frameLedger.RecordEvent(
                        sourceSequence,
                        FrameLedgerStage.PreviewEnqueued,
                        subsystem: "preview",
                        accepted: false,
                        reason: "texture_submit_exception");
                    Logger.Log($"UNIFIED_VIDEO_PREVIEW_TEXTURE_FAIL type={ex.GetType().Name} msg={ex.Message}");
                }
            }

            if (!textureSubmitted &&
                Volatile.Read(ref _strictPreviewTextureRequired))
            {
                var failures = Interlocked.Increment(ref _consecutiveTextureFailures);
                Interlocked.Increment(ref _videoFramesDropped);

                if (failures >= MaxConsecutiveTextureFailures)
                {
                    SignalFatalError(
                        new InvalidOperationException(
                            $"4K120 MJPG mode requires D3D preview textures, but texture delivery failed {failures} consecutive times for native_input='{_nativeInputFormat}' negotiated='{_negotiatedFormat}'."),
                        $"UNIFIED_VIDEO_PREVIEW_TEXTURE_REQUIRED consecutive={failures} " +
                        $"native_input='{_nativeInputFormat}' negotiated='{_negotiatedFormat}'");
                }
                else
                {
                    Logger.Log($"UNIFIED_VIDEO_PREVIEW_TEXTURE_GRACE consecutive={failures}/{MaxConsecutiveTextureFailures}");
                }
            }
            else if (!textureSubmitted && !frameData.IsEmpty)
            {
                SubmitPreviewRawFrame(previewSink, frameData, width, height, isP010, arrivalTick, sourceSequence);
            }
        }
    }

    private void RecordCaptureArrived(long sourceSequence, long arrivalTick, int width, int height, int compressedByteLength)
    {
        _frameLedger.RecordCaptureArrived(new FrameIdentity(
            SourceSequence: sourceSequence,
            CaptureArrivalQpc: arrivalTick,
            DeviceTimestamp100ns: null,
            InputFormat: Volatile.Read(ref _nativeInputFormat),
            Width: width,
            Height: height,
            FrameRateNominal: Volatile.Read(ref _fps),
            CompressedByteLength: compressedByteLength));
    }

    private void FirePixelFormatObserverOnce(string format)
    {
        if (Interlocked.CompareExchange(ref _pixelFormatObserverFired, 1, 0) != 0)
        {
            return;
        }

        Volatile.Read(ref _pixelFormatDetectedCallback)?.Invoke(format);
    }

    private void SignalFatalError(Exception ex, string logMessage)
    {
        Logger.Log(logMessage);

        if (Interlocked.Exchange(ref _fatalErrorSignaled, 1) != 0)
        {
            return;
        }

        try
        {
            FatalErrorOccurred?.Invoke(this, ex);
        }
        catch (Exception callbackEx)
        {
            Logger.Log($"UNIFIED_VIDEO_FATAL_CALLBACK_FAIL type={callbackEx.GetType().Name} msg={callbackEx.Message}");
        }
    }
}
