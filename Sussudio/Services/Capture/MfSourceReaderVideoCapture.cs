using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

// Negotiated capture mode for one MfSourceReaderVideoCapture session. Bundling
// these is mostly about not having a 9-positional-parameter call site — the
// device link stays separate because it identifies the device, not the mode.
public sealed record VideoCaptureNegotiationOptions(
    int Width,
    int Height,
    double Fps,
    bool RequireP010,
    string? RequestedPixelFormat = null,
    bool UseMjpegHighFrameRateMode = false,
    IntPtr DxgiDeviceManager = default,
    bool UseExternalMjpegDecode = false);

public sealed partial class MfSourceReaderVideoCapture : IAsyncDisposable
{
    public delegate void RawFrameCallback(ReadOnlySpan<byte> frameData, int width, int height, long arrivalTick);
    public delegate void DualFrameCallback(IntPtr gpuTexture, int gpuSubresource, ReadOnlySpan<byte> cpuData, int width, int height, long arrivalTick);

    private readonly object _sync = new();
    private IMFSourceReader? _sourceReader;
    private IMFMediaSource? _mediaSource;
    private CancellationTokenSource? _readCts;
    private Task? _readTask;
    private bool _isInitialized;
    private bool _startupHeld;
    private bool _sourceReaderD3DEnabled;
    private IntPtr _dxgiDeviceManagerPtr;
    private int _width;
    private int _height;
    private double _fps;
    private bool _isP010;
    private bool _isCompressedMjpgOutput;
    private bool _isHighFrameRateMjpegMode;
    private bool _strictD3DOutputRequired;
    private bool _strictTextureOutputRequired;
    private string _deviceSymbolicLink = string.Empty;
    private string _nativeInputFormat = "unknown";
    private string _negotiatedFormat = "unknown";
    private int _fatalErrorSignaled;
    private long _framesDelivered;
    private long _framesDropped;
    private int _isReadSampleOutstanding;
    private long _readSampleOutstandingStartTickMs;
    private long _lastFrameDeliveredTickMs;
    private int _vtableDiagDone;
    private int _dxgiBufferProbeDone;
    private int _dxgiResourceFailureCount;
    private bool _skipCpuReadback;
    public long FramesDelivered => Interlocked.Read(ref _framesDelivered);
    public long FramesDropped => Interlocked.Read(ref _framesDropped);
    public string NegotiatedFormat => Volatile.Read(ref _negotiatedFormat);
    public string NativeInputFormat => Volatile.Read(ref _nativeInputFormat);
    public bool IsP010 => Volatile.Read(ref _isP010);
    public bool IsCompressedMjpgOutput => Volatile.Read(ref _isCompressedMjpgOutput);
    public bool IsD3DOutputEnabled => Volatile.Read(ref _sourceReaderD3DEnabled);
    public bool IsHighFrameRateMjpegMode => Volatile.Read(ref _isHighFrameRateMjpegMode);
    public int Width => Volatile.Read(ref _width);
    public int Height => Volatile.Read(ref _height);
    public double Fps => Volatile.Read(ref _fps);
    public bool SkipCpuReadback
    {
        get => Volatile.Read(ref _skipCpuReadback);
        set => Volatile.Write(ref _skipCpuReadback, value);
    }
    public event EventHandler<Exception>? FatalErrorOccurred;
    public bool IsReadSampleOutstanding => Volatile.Read(ref _isReadSampleOutstanding) != 0;
    public long ReadSampleOutstandingMs
    {
        get
        {
            if (Volatile.Read(ref _isReadSampleOutstanding) == 0)
            {
                return 0;
            }

            var startedTickMs = Interlocked.Read(ref _readSampleOutstandingStartTickMs);
            return startedTickMs <= 0
                ? 0
                : Math.Max(0, Environment.TickCount64 - startedTickMs);
        }
    }
    public long LastFrameDeliveredTickMs => Interlocked.Read(ref _lastFrameDeliveredTickMs);
    public Task InitializeAsync(string deviceSymbolicLink, VideoCaptureNegotiationOptions options)
    {
        if (string.IsNullOrWhiteSpace(deviceSymbolicLink))
        {
            throw new ArgumentException("Video device symbolic link is required.", nameof(deviceSymbolicLink));
        }

        if (options.Width <= 0 || options.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Video width/height must be positive.");
        }

        if (options.Fps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Video frame rate must be positive.");
        }

        var width = options.Width;
        var height = options.Height;
        var fps = options.Fps;
        var requireP010 = options.RequireP010;
        var requestedPixelFormat = options.RequestedPixelFormat;
        var useMjpegHighFrameRateMode = options.UseMjpegHighFrameRateMode;
        var dxgiDeviceManager = options.DxgiDeviceManager;
        var useExternalMjpegDecode = options.UseExternalMjpegDecode;

        lock (_sync)
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("IMF source reader capture is already initialized.");
            }
        }

        IMFMediaSource? mediaSource = null;
        IMFSourceReader? sourceReader = null;
        IMFAttributes? readerAttributes = null;
        IMFMediaType? selectedMediaType = null;
        IMFMediaType? actualMediaType = null;
        var startupHeld = false;
        var sourceReaderD3DEnabled = false;
        var disableConverters = true;
        var requestedSourceSubtypeName = requestedPixelFormat;
        var useConvertedMjpegNv12 = useMjpegHighFrameRateMode &&
                                    !requireP010 &&
                                    !useExternalMjpegDecode &&
                                    string.Equals(requestedPixelFormat, "MJPG", StringComparison.OrdinalIgnoreCase);
        var useRawMjpgOutput = useMjpegHighFrameRateMode &&
                               !requireP010 &&
                               useExternalMjpegDecode &&
                                    string.Equals(requestedPixelFormat, "MJPG", StringComparison.OrdinalIgnoreCase);

        try
        {
            MfInteropHelpers.AddStartupReference();
            startupHeld = true;

            mediaSource = CreateMediaSource(deviceSymbolicLink);

            disableConverters = !useConvertedMjpegNv12 && !useRawMjpgOutput;
            MfInteropHelpers.ThrowIfFailed(
                MfInterop.MFCreateAttributes(out readerAttributes, useConvertedMjpegNv12 ? 3 : 2),
                "MFCreateAttributes(reader)");
            if (useConvertedMjpegNv12)
            {
                MfInteropHelpers.ThrowIfFailed(
                    readerAttributes.SetUINT32(ref MfGuids.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1),
                    "IMFAttributes.SetUINT32(MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS)");
            }
            if (disableConverters)
            {
                MfInteropHelpers.ThrowIfFailed(
                    readerAttributes.SetUINT32(ref MfGuids.MF_READWRITE_DISABLE_CONVERTERS, 1),
                    "IMFAttributes.SetUINT32(MF_READWRITE_DISABLE_CONVERTERS)");
            }

            if (!useRawMjpgOutput &&
                dxgiDeviceManager != IntPtr.Zero &&
                TrySetSourceReaderD3DManager(readerAttributes, dxgiDeviceManager))
            {
                sourceReaderD3DEnabled = true;
            }
            else if (useConvertedMjpegNv12)
            {
                throw new InvalidOperationException("4K120 MJPG mode requires D3D11-backed SourceReader output.");
            }

            var createSourceReaderHr = MfInterop.MFCreateSourceReaderFromMediaSource(mediaSource, readerAttributes, out sourceReader);
            if (createSourceReaderHr < 0 && sourceReaderD3DEnabled)
            {
                if (useConvertedMjpegNv12)
                {
                    MfInteropHelpers.ThrowIfFailed(createSourceReaderHr, "MFCreateSourceReaderFromMediaSource(hfr_mjpeg_d3d)");
                }

                Log(
                    "MF_SOURCE_READER_D3D_INIT_WARN " +
                    $"stage=CreateSourceReader hr=0x{createSourceReaderHr:X8} " +
                    "fallback=cpu_only");

                WasapiComInterop.ReleaseComObject(ref sourceReader);
                WasapiComInterop.ReleaseComObject(ref readerAttributes);

                MfInteropHelpers.ThrowIfFailed(
                    MfInterop.MFCreateAttributes(out readerAttributes, 1),
                    "MFCreateAttributes(reader_cpu_fallback)");
                if (disableConverters)
                {
                    MfInteropHelpers.ThrowIfFailed(
                        readerAttributes.SetUINT32(ref MfGuids.MF_READWRITE_DISABLE_CONVERTERS, 1),
                        "IMFAttributes.SetUINT32(MF_READWRITE_DISABLE_CONVERTERS)");
                }

                sourceReaderD3DEnabled = false;
                createSourceReaderHr = MfInterop.MFCreateSourceReaderFromMediaSource(mediaSource, readerAttributes, out sourceReader);
            }

            MfInteropHelpers.ThrowIfFailed(createSourceReaderHr, "MFCreateSourceReaderFromMediaSource");

            var requestedSubtype = requireP010
                ? MfGuids.MFVideoFormat_P010
                : MfGuids.MFVideoFormat_NV12;

            Guid negotiatedSubtype;
            int negotiatedWidth;
            int negotiatedHeight;
            double negotiatedFps;
            string negotiatedDescription;
            if (useConvertedMjpegNv12)
            {
                requestedSourceSubtypeName = "MJPG";
                selectedMediaType = SelectConvertedMediaType(
                    sourceReader,
                    width,
                    height,
                    fps,
                    MfGuids.MFVideoFormat_MJPG,
                    requestedSubtype,
                    out negotiatedSubtype,
                    out negotiatedWidth,
                    out negotiatedHeight,
                    out negotiatedFps,
                    out negotiatedDescription);
            }
            else if (useRawMjpgOutput)
            {
                requestedSourceSubtypeName = "MJPG";
                selectedMediaType = SelectMediaType(
                    sourceReader,
                    width,
                    height,
                    fps,
                    MfGuids.MFVideoFormat_MJPG,
                    out negotiatedSubtype,
                    out negotiatedWidth,
                    out negotiatedHeight,
                    out negotiatedFps,
                    out negotiatedDescription);
            }
            else
            {
                selectedMediaType = SelectMediaType(
                    sourceReader,
                    width,
                    height,
                    fps,
                    requestedSubtype,
                    out negotiatedSubtype,
                    out negotiatedWidth,
                    out negotiatedHeight,
                    out negotiatedFps,
                    out negotiatedDescription);
            }

            MfInteropHelpers.ThrowIfFailed(
                sourceReader.SetCurrentMediaType(
                    MfConstants.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                    IntPtr.Zero,
                    selectedMediaType),
                $"IMFSourceReader.SetCurrentMediaType({SubtypeGuidToName(negotiatedSubtype)})");

            MfInteropHelpers.ThrowIfFailed(
                sourceReader.GetCurrentMediaType(
                    MfConstants.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                    out actualMediaType),
                "IMFSourceReader.GetCurrentMediaType");

            if (actualMediaType != null)
            {
                if (MfInteropHelpers.TryGetGuid(actualMediaType, ref MfGuids.MF_MT_SUBTYPE, out var actualSubtype))
                {
                    negotiatedSubtype = actualSubtype;
                }

                if (TryGetFrameSize(actualMediaType, out var actualWidth, out var actualHeight))
                {
                    negotiatedWidth = actualWidth;
                    negotiatedHeight = actualHeight;
                }

                if (TryGetFrameRate(actualMediaType, out var actualFpsNumerator, out var actualFpsDenominator) &&
                    actualFpsDenominator > 0)
                {
                    negotiatedFps = (double)actualFpsNumerator / actualFpsDenominator;
                }

                if (useConvertedMjpegNv12)
                {
                    negotiatedDescription =
                        $"{SubtypeGuidToName(negotiatedSubtype)} <= MJPG {negotiatedWidth}x{negotiatedHeight}@{negotiatedFps:0.###}";
                }
            }

            if (useConvertedMjpegNv12)
            {
                if (!sourceReaderD3DEnabled)
                {
                    throw new InvalidOperationException("4K120 MJPG mode requires D3D11-backed decoded output.");
                }

                if (negotiatedSubtype != MfGuids.MFVideoFormat_NV12)
                {
                    throw new InvalidOperationException(
                        $"4K120 MJPG mode requires decoded NV12 output, but negotiated {SubtypeGuidToName(negotiatedSubtype)}.");
                }
            }
            else if (useRawMjpgOutput && negotiatedSubtype != MfGuids.MFVideoFormat_MJPG)
            {
                throw new InvalidOperationException(
                    $"External MJPG decode requires native MJPG output, but negotiated {SubtypeGuidToName(negotiatedSubtype)}.");
            }

            _deviceSymbolicLink = deviceSymbolicLink;
            _width = negotiatedWidth;
            _height = negotiatedHeight;
            _fps = negotiatedFps;
            SetExpectedFrameRate(_fps);
            _isP010 = useRawMjpgOutput ? false : negotiatedSubtype == MfGuids.MFVideoFormat_P010;
            _isCompressedMjpgOutput = useRawMjpgOutput;
            _isHighFrameRateMjpegMode = useConvertedMjpegNv12 || useRawMjpgOutput;
            _strictD3DOutputRequired = useConvertedMjpegNv12;
            _strictTextureOutputRequired = useConvertedMjpegNv12;
            var nativeFormatName = SubtypeGuidToName(negotiatedSubtype);
            if (!useConvertedMjpegNv12 && !useRawMjpgOutput)
            {
                // Bandwidth heuristic: raw uncompressed NV12/YUY2 at this res+fps may exceed USB 3.0 (~5 Gbps).
                // If so, the device must be delivering compressed (MJPG) regardless of what MF negotiated.
                var rawBitsPerSecond = (double)negotiatedWidth * negotiatedHeight * 1.5 * negotiatedFps * 8;
                const double usb30BandwidthBits = 5e9;
                if (rawBitsPerSecond > usb30BandwidthBits &&
                    !string.Equals(nativeFormatName, "MJPG", StringComparison.OrdinalIgnoreCase))
                {
                    Log($"MF_NATIVE_FORMAT_OVERRIDE negotiated={nativeFormatName} raw_bps={rawBitsPerSecond:0} usb_bps={usb30BandwidthBits:0} => MJPG");
                    nativeFormatName = "MJPG";
                }
            }
            else
            {
                nativeFormatName = "MJPG";
            }
            Volatile.Write(ref _nativeInputFormat, nativeFormatName);
            Volatile.Write(ref _negotiatedFormat, negotiatedDescription);
            Interlocked.Exchange(ref _framesDelivered, 0);
            Interlocked.Exchange(ref _framesDropped, 0);
            Volatile.Write(ref _isReadSampleOutstanding, 0);
            Interlocked.Exchange(ref _readSampleOutstandingStartTickMs, 0);
            Interlocked.Exchange(ref _lastFrameDeliveredTickMs, 0);
            Interlocked.Exchange(ref _dxgiBufferProbeDone, 0);
            Interlocked.Exchange(ref _dxgiResourceFailureCount, 0);
            Interlocked.Exchange(ref _fatalErrorSignaled, 0);

            lock (_sync)
            {
                _mediaSource = mediaSource;
                _sourceReader = sourceReader;
                _startupHeld = startupHeld;
                _sourceReaderD3DEnabled = sourceReaderD3DEnabled;
                _dxgiDeviceManagerPtr = sourceReaderD3DEnabled ? dxgiDeviceManager : IntPtr.Zero;
                _isInitialized = true;
                mediaSource = null;
                sourceReader = null;
                startupHeld = false;
            }

            Log(
                "MF_SOURCE_READER_INIT " +
                $"device='{deviceSymbolicLink}' " +
                $"requested={width}x{height}@{fps:0.###} " +
                $"requested_source_subtype='{requestedSourceSubtypeName ?? (requireP010 ? "P010" : "NV12")}' " +
                $"native_input='{_nativeInputFormat}' " +
                $"negotiated='{_negotiatedFormat}' " +
                $"d3d_manager_enabled={sourceReaderD3DEnabled} " +
                $"mf_readwrite_disable_converters={disableConverters.ToString().ToLowerInvariant()} " +
                $"mf_readwrite_enable_hardware_transforms={useConvertedMjpegNv12.ToString().ToLowerInvariant()}");
        }
        catch (Exception ex)
        {
            Log(
                "MF_SOURCE_READER_INIT_FAIL " +
                $"device='{deviceSymbolicLink}' " +
                $"requested={width}x{height}@{fps:0.###} " +
                $"requested_source_subtype='{requestedSourceSubtypeName ?? (requireP010 ? "P010" : "NV12")}' " +
                $"d3d_manager_requested={(dxgiDeviceManager != IntPtr.Zero)} " +
                $"type={ex.GetType().Name} msg={ex.Message}");
            throw;
        }
        finally
        {
            WasapiComInterop.ReleaseComObject(ref actualMediaType);
            WasapiComInterop.ReleaseComObject(ref selectedMediaType);
            WasapiComInterop.ReleaseComObject(ref readerAttributes);
            WasapiComInterop.ReleaseComObject(ref sourceReader);
            WasapiComInterop.ReleaseComObject(ref mediaSource);

            if (startupHeld)
            {
                MfInteropHelpers.ReleaseStartupReference();
            }
        }

        return Task.CompletedTask;
    }

    public void StartReading(RawFrameCallback onFrame, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(onFrame);
        StartReading(onFrame, null, ct);
    }

    public void StartReading(DualFrameCallback onFrame, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(onFrame);
        StartReading(null, onFrame, ct);
    }

    public void StartReading(RawFrameCallback? onFrame, DualFrameCallback? onDualFrame, CancellationToken ct)
    {
        if (onFrame == null && onDualFrame == null)
        {
            throw new ArgumentNullException(nameof(onFrame), "At least one frame callback must be provided.");
        }

        lock (_sync)
        {
            if (!_isInitialized || _sourceReader == null)
            {
                throw new InvalidOperationException("InitializeAsync must succeed before StartReading.");
            }

            if (_readTask != null)
            {
                throw new InvalidOperationException("Read loop is already running.");
            }

            _readCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var readToken = _readCts.Token;
            _readTask = Task.Run(() => ReadLoop(onFrame, onDualFrame, readToken), CancellationToken.None);
        }

        Log(
            "MF_SOURCE_READER_START " +
            $"device='{_deviceSymbolicLink}' negotiated='{_negotiatedFormat}' d3d_manager_enabled={_sourceReaderD3DEnabled}");
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? readCts;
        Task? readTask;

        lock (_sync)
        {
            readCts = _readCts;
            readTask = _readTask;
            _readCts = null;
            _readTask = null;
        }

        readCts?.Cancel();
        ResetSourceCadence();

        if (readTask != null)
        {
            try
            {
                await readTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected during stop.
            }
            catch (Exception ex)
            {
                Log($"MF_SOURCE_READER_STOP_WAIT_ERROR type={ex.GetType().Name} msg={ex.Message}");
            }
        }

        readCts?.Dispose();
        ReleaseReaderAndSource();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);

        lock (_sync)
        {
            _isInitialized = false;
            _deviceSymbolicLink = string.Empty;
            _isP010 = false;
            _isCompressedMjpgOutput = false;
            _isHighFrameRateMjpegMode = false;
            _strictD3DOutputRequired = false;
            _strictTextureOutputRequired = false;
            _sourceReaderD3DEnabled = false;
            _dxgiDeviceManagerPtr = IntPtr.Zero;
            _nativeInputFormat = "unknown";
            _negotiatedFormat = "unknown";
            Interlocked.Exchange(ref _fatalErrorSignaled, 0);
        }

        if (_startupHeld)
        {
            MfInteropHelpers.ReleaseStartupReference();
            _startupHeld = false;
        }
    }

    private void ReadLoop(RawFrameCallback? onFrame, DualFrameCallback? onDualFrame, CancellationToken ct)
    {
        Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

        while (!ct.IsCancellationRequested)
        {
            IMFSourceReader? reader;
            lock (_sync)
            {
                reader = _sourceReader;
            }

            if (reader == null)
            {
                break;
            }

            IMFSample? sample = null;
            try
            {
                var readStartedTickMs = Environment.TickCount64;
                Volatile.Write(ref _isReadSampleOutstanding, 1);
                Interlocked.Exchange(ref _readSampleOutstandingStartTickMs, readStartedTickMs);

                int hr;
                int flags;
                long mfTimestamp100ns;
                try
                {
                    hr = reader.ReadSample(
                        MfConstants.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                        0,
                        out _,
                        out flags,
                        out mfTimestamp100ns,
                        out sample);
                }
                finally
                {
                    Interlocked.Exchange(ref _readSampleOutstandingStartTickMs, 0);
                    Volatile.Write(ref _isReadSampleOutstanding, 0);
                }

                if (ct.IsCancellationRequested)
                {
                    break;
                }

                if ((hr == MfHResults.MF_E_SHUTDOWN || hr == MfHResults.MF_E_INVALIDREQUEST)
                    && ct.IsCancellationRequested)
                {
                    break;
                }

                MfInteropHelpers.ThrowIfFailed(hr, "IMFSourceReader.ReadSample");

                if ((flags & MfConstants.MF_SOURCE_READERF_ENDOFSTREAM) != 0)
                {
                    Log("MF_SOURCE_READER_EOS reached end-of-stream.");
                    break;
                }

                if (sample == null)
                {
                    Interlocked.Increment(ref _framesDropped);
                    continue;
                }

                var arrivalTick = Stopwatch.GetTimestamp();
                if (mfTimestamp100ns > 0)
                {
                    TrackSourceCadence(mfTimestamp100ns);
                }

                DeliverFrame(sample, onFrame, onDualFrame, arrivalTick);
                Interlocked.Exchange(ref _lastFrameDeliveredTickMs, Environment.TickCount64);
                Interlocked.Increment(ref _framesDelivered);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _framesDropped);
                Log(
                    "MF_SOURCE_READER_FRAME_ERROR " +
                    $"type={ex.GetType().Name} " +
                    $"hr=0x{ex.HResult:X8} " +
                    $"msg={ex.Message}");

                if (Volatile.Read(ref _strictD3DOutputRequired))
                {
                    SignalFatalError(ex);
                    break;
                }
            }
            finally
            {
                WasapiComInterop.ReleaseComObject(ref sample);
            }
        }
    }

    private unsafe void DeliverFrame(
        IMFSample sample,
        RawFrameCallback? onFrame,
        DualFrameCallback? onDualFrame,
        long arrivalTick)
    {
#if DEBUG
        // One-shot vtable diagnostic — runs on the very first sample to compare
        // raw vtable dispatch vs managed COM interop dispatch. This definitively
        // reveals whether .NET's vtable slot calculation for IMFSample is correct.
        if (Interlocked.CompareExchange(ref _vtableDiagDone, 1, 0) == 0)
        {
            DiagnoseVtable(sample);
        }
#endif

        IMFMediaBuffer? buffer = null;
        try
        {
            if (Volatile.Read(ref _isCompressedMjpgOutput) && onDualFrame == null && onFrame != null)
            {
                var getBufferCountHr = sample.GetBufferCount(out var bufferCount);
                if (getBufferCountHr >= 0 && bufferCount == 1)
                {
                    var getBufferHr = sample.GetBufferByIndex(0, out buffer);
                    if (getBufferHr >= 0 && buffer != null)
                    {
                        DeliverRawFrameFromBuffer(buffer, onFrame!, arrivalTick);
                        return;
                    }
                }
            }

            var ctcbHr = sample.ConvertToContiguousBuffer(out buffer);
            if (ctcbHr < 0 || buffer == null)
            {
                var probeCount = Interlocked.Increment(ref _framesDropped);
                if (probeCount <= 3)
                {
                    Log($"MF_SOURCE_READER_BUFFER_PROBE ctcb_hr=0x{ctcbHr:X8} sample_type={sample.GetType().Name}");
                }
                return;
            }

            if (onDualFrame != null)
            {
                DeliverDualFrameFromBuffer(buffer, onDualFrame, onFrame, arrivalTick);
                return;
            }

            if (onFrame != null)
            {
                DeliverRawFrameFromBuffer(buffer, onFrame, arrivalTick);
            }
        }
        finally
        {
            WasapiComInterop.ReleaseComObject(ref buffer);
        }
    }

    private unsafe void DeliverRawFrameFromBuffer(IMFMediaBuffer buffer, RawFrameCallback onFrame, long arrivalTick)
    {
        if (Volatile.Read(ref _isCompressedMjpgOutput))
        {
            MfInteropHelpers.ThrowIfFailed(
                buffer.Lock(out var compressedDataPtr, out _, out var compressedLength),
                "IMFMediaBuffer.Lock");

            byte[]? rentedBuffer = null;
            int jpegLength = 0;
            try
            {
                if (compressedDataPtr == IntPtr.Zero || compressedLength <= 0)
                {
                    Interlocked.Increment(ref _framesDropped);
                    return;
                }

                // Copy MJPG bytes out so we release the source reader's USB buffer slot
                // before running the decode + preview + recording pipeline.
                rentedBuffer = ArrayPool<byte>.Shared.Rent(compressedLength);
                jpegLength = compressedLength;
                new ReadOnlySpan<byte>((void*)compressedDataPtr, compressedLength)
                    .CopyTo(rentedBuffer);
            }
            finally
            {
                _ = buffer.Unlock();
            }

            try
            {
                onFrame(new ReadOnlySpan<byte>(rentedBuffer, 0, jpegLength), _width, _height, arrivalTick);
            }
            finally
            {
                if (rentedBuffer != null)
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
            }

            return;
        }

        if (TryDeliverFrameFrom2DBuffer(buffer, onFrame, arrivalTick))
        {
            return;
        }

        MfInteropHelpers.ThrowIfFailed(
            buffer.Lock(out var dataPtr, out _, out var curLen),
            "IMFMediaBuffer.Lock");
        try
        {
            if (dataPtr == IntPtr.Zero || curLen <= 0)
            {
                Interlocked.Increment(ref _framesDropped);
                return;
            }

            var packedFrameBytes = GetFrameSizeBytes(_width, _height, _isP010);
            if (packedFrameBytes <= 0)
            {
                throw new InvalidOperationException("Invalid frame dimensions.");
            }

            if (curLen < packedFrameBytes)
            {
                throw new InvalidOperationException(
                    $"Media buffer length ({curLen}) is smaller than expected frame size ({packedFrameBytes}).");
            }

            var expectedStride = GetRowBytes(_width, _isP010);
            var inferredStride = InferPackedStride(curLen, _height);
            if (inferredStride > expectedStride)
            {
                var packed = ArrayPool<byte>.Shared.Rent(packedFrameBytes);
                try
                {
                    var packedSpan = packed.AsSpan(0, packedFrameBytes);
                    CopyYuvWithStride((byte*)dataPtr, inferredStride, packedSpan, _width, _height, _isP010);

                    onFrame(packedSpan, _width, _height, arrivalTick);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(packed);
                }
            }
            else
            {
                onFrame(new ReadOnlySpan<byte>((void*)dataPtr, packedFrameBytes), _width, _height, arrivalTick);
            }
        }
        finally
        {
            _ = buffer.Unlock();
        }
    }

    private unsafe void DeliverDualFrameFromBuffer(
        IMFMediaBuffer buffer,
        DualFrameCallback onDualFrame,
        RawFrameCallback? fallbackRawFrame,
        long arrivalTick)
    {
        var hasTexture = TryGetDxgiTexture(buffer, out var gpuTexture, out var gpuSubresource);
        if (!hasTexture && Volatile.Read(ref _strictTextureOutputRequired))
        {
            throw new InvalidOperationException("4K120 MJPG mode requires D3D11 texture delivery for preview.");
        }

        if (!hasTexture && fallbackRawFrame != null)
        {
            DeliverRawFrameFromBuffer(buffer, fallbackRawFrame, arrivalTick);
            return;
        }

        try
        {
            if (hasTexture && Volatile.Read(ref _skipCpuReadback))
            {
                try
                {
                    onDualFrame(gpuTexture, gpuSubresource, ReadOnlySpan<byte>.Empty, _width, _height, arrivalTick);
                    return;
                }
                catch (Exception ex)
                {
                    Logger.Log($"MF_SOURCE_READER_GPU_ONLY_FAIL type={ex.GetType().Name} msg={ex.Message}");
                }
            }

            if (TryDeliverDualFrameFrom2DBuffer(buffer, gpuTexture, gpuSubresource, onDualFrame, arrivalTick))
            {
                return;
            }

            MfInteropHelpers.ThrowIfFailed(
                buffer.Lock(out var dataPtr, out _, out var curLen),
                "IMFMediaBuffer.Lock");
            try
            {
                if (dataPtr == IntPtr.Zero || curLen <= 0)
                {
                    Interlocked.Increment(ref _framesDropped);
                    return;
                }

                var packedFrameBytes = GetFrameSizeBytes(_width, _height, _isP010);
                if (packedFrameBytes <= 0)
                {
                    throw new InvalidOperationException("Invalid frame dimensions.");
                }

                if (curLen < packedFrameBytes)
                {
                    throw new InvalidOperationException(
                        $"Media buffer length ({curLen}) is smaller than expected frame size ({packedFrameBytes}).");
                }

                var expectedStride = GetRowBytes(_width, _isP010);
                var inferredStride = InferPackedStride(curLen, _height);
                if (inferredStride > expectedStride)
                {
                    var packed = ArrayPool<byte>.Shared.Rent(packedFrameBytes);
                    try
                    {
                        var packedSpan = packed.AsSpan(0, packedFrameBytes);
                        CopyYuvWithStride((byte*)dataPtr, inferredStride, packedSpan, _width, _height, _isP010);

                        onDualFrame(gpuTexture, gpuSubresource, packedSpan, _width, _height, arrivalTick);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(packed);
                    }
                }
                else
                {
                    onDualFrame(gpuTexture, gpuSubresource, new ReadOnlySpan<byte>((void*)dataPtr, packedFrameBytes), _width, _height, arrivalTick);
                }
            }
            finally
            {
                _ = buffer.Unlock();
            }
        }
        finally
        {
            if (gpuTexture != IntPtr.Zero)
            {
                Marshal.Release(gpuTexture);
            }
        }
    }

    private unsafe bool TryDeliverFrameFrom2DBuffer(IMFMediaBuffer buffer, RawFrameCallback onFrame, long arrivalTick)
    {
        if (buffer is not IMF2DBuffer buffer2D)
        {
            return false;
        }

        MfInteropHelpers.ThrowIfFailed(
            buffer2D.Lock2D(out var scanlinePtr, out var pitch),
            "IMF2DBuffer.Lock2D");
        try
        {
            if (scanlinePtr == IntPtr.Zero)
            {
                Interlocked.Increment(ref _framesDropped);
                return true;
            }

            var packedFrameBytes = GetFrameSizeBytes(_width, _height, _isP010);
            if (packedFrameBytes <= 0)
            {
                throw new InvalidOperationException("Invalid frame dimensions.");
            }

            var expectedStride = GetRowBytes(_width, _isP010);
            if (pitch == expectedStride)
            {
                onFrame(new ReadOnlySpan<byte>((void*)scanlinePtr, packedFrameBytes), _width, _height, arrivalTick);
                return true;
            }

            var packed = ArrayPool<byte>.Shared.Rent(packedFrameBytes);
            try
            {
                var packedSpan = packed.AsSpan(0, packedFrameBytes);
                CopyYuvWithStride((byte*)scanlinePtr, pitch, packedSpan, _width, _height, _isP010);

                onFrame(packedSpan, _width, _height, arrivalTick);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(packed);
            }

            return true;
        }
        finally
        {
            _ = buffer2D.Unlock2D();
        }
    }

    private unsafe bool TryDeliverDualFrameFrom2DBuffer(
        IMFMediaBuffer buffer,
        IntPtr gpuTexture,
        int gpuSubresource,
        DualFrameCallback onFrame,
        long arrivalTick)
    {
        if (buffer is not IMF2DBuffer buffer2D)
        {
            return false;
        }

        MfInteropHelpers.ThrowIfFailed(
            buffer2D.Lock2D(out var scanlinePtr, out var pitch),
            "IMF2DBuffer.Lock2D");
        try
        {
            if (scanlinePtr == IntPtr.Zero)
            {
                Interlocked.Increment(ref _framesDropped);
                return true;
            }

            var packedFrameBytes = GetFrameSizeBytes(_width, _height, _isP010);
            if (packedFrameBytes <= 0)
            {
                throw new InvalidOperationException("Invalid frame dimensions.");
            }

            var expectedStride = GetRowBytes(_width, _isP010);
            if (pitch == expectedStride)
            {
                onFrame(gpuTexture, gpuSubresource, new ReadOnlySpan<byte>((void*)scanlinePtr, packedFrameBytes), _width, _height, arrivalTick);
                return true;
            }

            var packed = ArrayPool<byte>.Shared.Rent(packedFrameBytes);
            try
            {
                var packedSpan = packed.AsSpan(0, packedFrameBytes);
                CopyYuvWithStride((byte*)scanlinePtr, pitch, packedSpan, _width, _height, _isP010);

                onFrame(gpuTexture, gpuSubresource, packedSpan, _width, _height, arrivalTick);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(packed);
            }

            return true;
        }
        finally
        {
            _ = buffer2D.Unlock2D();
        }
    }

    private void ReleaseReaderAndSource()
    {
        IMFSourceReader? sourceReader;
        IMFMediaSource? mediaSource;

        lock (_sync)
        {
            sourceReader = _sourceReader;
            mediaSource = _mediaSource;
            _sourceReader = null;
            _mediaSource = null;
            _isInitialized = false;
            _sourceReaderD3DEnabled = false;
            _dxgiDeviceManagerPtr = IntPtr.Zero;
        }

        WasapiComInterop.ReleaseComObject(ref sourceReader);
        WasapiComInterop.ReleaseComObject(ref mediaSource);
    }

    private static void Log(string message)
    {
        Debug.WriteLine(message);
        Logger.Log(message);
    }

    private void SignalFatalError(Exception ex)
    {
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
            Log($"MF_SOURCE_READER_FATAL_ERROR_CALLBACK_FAIL type={callbackEx.GetType().Name} msg={callbackEx.Message}");
        }
    }
}
