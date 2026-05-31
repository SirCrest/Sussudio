using System;
using System.Diagnostics;
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
    private const int CadenceWindowSeconds = 20;
    public delegate void RawFrameCallback(ReadOnlySpan<byte> frameData, int width, int height, long arrivalTick);
    public delegate void DualFrameCallback(IntPtr gpuTexture, int gpuSubresource, ReadOnlySpan<byte> cpuData, int width, int height, long arrivalTick);

    private readonly object _sync = new();
    private readonly object _cadenceLock = new();
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
    private double[] _sourceIntervalWindowMs = new double[1200];
    private int _sourceIntervalCount;
    private int _sourceIntervalIndex;
    private long _prevMfTimestamp100ns = -1;
    private double _expectedIntervalMs;
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

    public static int GetFrameSizeBytes(int width, int height, bool isP010)
        => isP010 ? width * height * 3 : (width * height * 3) / 2;

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

            var negotiatedMode = ApplyCurrentMediaTypeAndReconcileActualOutput(
                sourceReader,
                selectedMediaType,
                new SourceReaderNegotiatedMode(
                    negotiatedSubtype,
                    negotiatedWidth,
                    negotiatedHeight,
                    negotiatedFps,
                    negotiatedDescription),
                useConvertedMjpegNv12);
            ValidateNegotiatedOutputMode(
                negotiatedMode,
                useConvertedMjpegNv12,
                useRawMjpgOutput,
                sourceReaderD3DEnabled);
            CommitInitializedRuntimeState(
                deviceSymbolicLink,
                negotiatedMode,
                ref mediaSource,
                ref sourceReader,
                ref startupHeld,
                sourceReaderD3DEnabled,
                dxgiDeviceManager,
                useConvertedMjpegNv12,
                useRawMjpgOutput);

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

    private readonly record struct SourceReaderNegotiatedMode(
        Guid Subtype,
        int Width,
        int Height,
        double Fps,
        string Description);

    private SourceReaderNegotiatedMode ApplyCurrentMediaTypeAndReconcileActualOutput(
        IMFSourceReader sourceReader,
        IMFMediaType selectedMediaType,
        SourceReaderNegotiatedMode selectedMode,
        bool useConvertedMjpegNv12)
    {
        IMFMediaType? actualMediaType = null;
        try
        {
            MfInteropHelpers.ThrowIfFailed(
                sourceReader.SetCurrentMediaType(
                    MfConstants.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                    IntPtr.Zero,
                    selectedMediaType),
                $"IMFSourceReader.SetCurrentMediaType({SubtypeGuidToName(selectedMode.Subtype)})");

            MfInteropHelpers.ThrowIfFailed(
                sourceReader.GetCurrentMediaType(
                    MfConstants.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                    out actualMediaType),
                "IMFSourceReader.GetCurrentMediaType");

            return actualMediaType != null
                ? ReconcileActualMediaType(actualMediaType, selectedMode, useConvertedMjpegNv12)
                : selectedMode;
        }
        finally
        {
            WasapiComInterop.ReleaseComObject(ref actualMediaType);
        }
    }

    private static SourceReaderNegotiatedMode ReconcileActualMediaType(
        IMFMediaType actualMediaType,
        SourceReaderNegotiatedMode selectedMode,
        bool useConvertedMjpegNv12)
    {
        var subtype = selectedMode.Subtype;
        var width = selectedMode.Width;
        var height = selectedMode.Height;
        var fps = selectedMode.Fps;
        var description = selectedMode.Description;

        if (MfInteropHelpers.TryGetGuid(actualMediaType, ref MfGuids.MF_MT_SUBTYPE, out var actualSubtype))
        {
            subtype = actualSubtype;
        }

        if (TryGetFrameSize(actualMediaType, out var actualWidth, out var actualHeight))
        {
            width = actualWidth;
            height = actualHeight;
        }

        if (TryGetFrameRate(actualMediaType, out var actualFpsNumerator, out var actualFpsDenominator) &&
            actualFpsDenominator > 0)
        {
            fps = (double)actualFpsNumerator / actualFpsDenominator;
        }

        if (useConvertedMjpegNv12)
        {
            description = $"{SubtypeGuidToName(subtype)} <= MJPG {width}x{height}@{fps:0.###}";
        }

        return new SourceReaderNegotiatedMode(subtype, width, height, fps, description);
    }

    private void ValidateNegotiatedOutputMode(
        SourceReaderNegotiatedMode mode,
        bool useConvertedMjpegNv12,
        bool useRawMjpgOutput,
        bool sourceReaderD3DEnabled)
    {
        if (useConvertedMjpegNv12)
        {
            if (!sourceReaderD3DEnabled)
            {
                throw new InvalidOperationException("4K120 MJPG mode requires D3D11-backed decoded output.");
            }

            if (mode.Subtype != MfGuids.MFVideoFormat_NV12)
            {
                throw new InvalidOperationException(
                    $"4K120 MJPG mode requires decoded NV12 output, but negotiated {SubtypeGuidToName(mode.Subtype)}.");
            }
        }
        else if (useRawMjpgOutput && mode.Subtype != MfGuids.MFVideoFormat_MJPG)
        {
            throw new InvalidOperationException(
                $"External MJPG decode requires native MJPG output, but negotiated {SubtypeGuidToName(mode.Subtype)}.");
        }
    }

    private string ResolveNativeInputFormatName(
        SourceReaderNegotiatedMode mode,
        bool useConvertedMjpegNv12,
        bool useRawMjpgOutput)
    {
        var nativeFormatName = SubtypeGuidToName(mode.Subtype);
        if (!useConvertedMjpegNv12 && !useRawMjpgOutput)
        {
            // Bandwidth heuristic: raw uncompressed NV12/YUY2 at this res+fps may exceed USB 3.0 (~5 Gbps).
            // If so, the device must be delivering compressed (MJPG) regardless of what MF negotiated.
            var rawBitsPerSecond = (double)mode.Width * mode.Height * 1.5 * mode.Fps * 8;
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

        return nativeFormatName;
    }

    private void CommitInitializedRuntimeState(
        string deviceSymbolicLink,
        SourceReaderNegotiatedMode mode,
        ref IMFMediaSource? mediaSource,
        ref IMFSourceReader? sourceReader,
        ref bool startupHeld,
        bool sourceReaderD3DEnabled,
        IntPtr dxgiDeviceManager,
        bool useConvertedMjpegNv12,
        bool useRawMjpgOutput)
    {
        _deviceSymbolicLink = deviceSymbolicLink;
        _width = mode.Width;
        _height = mode.Height;
        _fps = mode.Fps;
        SetExpectedFrameRate(_fps);
        _isP010 = useRawMjpgOutput ? false : mode.Subtype == MfGuids.MFVideoFormat_P010;
        _isCompressedMjpgOutput = useRawMjpgOutput;
        _isHighFrameRateMjpegMode = useConvertedMjpegNv12 || useRawMjpgOutput;
        _strictD3DOutputRequired = useConvertedMjpegNv12;
        _strictTextureOutputRequired = useConvertedMjpegNv12;

        Volatile.Write(ref _nativeInputFormat, ResolveNativeInputFormatName(mode, useConvertedMjpegNv12, useRawMjpgOutput));
        Volatile.Write(ref _negotiatedFormat, mode.Description);
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

    public readonly record struct SourceCadenceMetrics(
        int SampleCount,
        double ObservedFps,
        double ExpectedIntervalMs,
        double AverageIntervalMs,
        double P95IntervalMs,
        double P99IntervalMs,
        double MaxIntervalMs,
        double OnePercentLowFps,
        double FivePercentLowFps,
        double SampleDurationMs,
        double[] RecentIntervalsMs,
        double JitterStdDevMs,
        long SevereGapCount,
        long EstimatedDroppedFrames,
        double EstimatedDropPercent);

    public void SetExpectedFrameRate(double fps)
    {
        if (fps > 0)
        {
            _expectedIntervalMs = 1000.0 / fps;
            var targetSize = Math.Max(600, (int)Math.Ceiling(fps * CadenceWindowSeconds));
            lock (_cadenceLock)
            {
                if (_sourceIntervalWindowMs.Length != targetSize)
                {
                    _sourceIntervalWindowMs = new double[targetSize];
                    _sourceIntervalCount = 0;
                    _sourceIntervalIndex = 0;
                }
            }
        }
    }

    private void ResetSourceCadence()
    {
        Interlocked.Exchange(ref _prevMfTimestamp100ns, -1);
        lock (_cadenceLock)
        {
            Array.Clear(_sourceIntervalWindowMs, 0, _sourceIntervalWindowMs.Length);
            _sourceIntervalCount = 0;
            _sourceIntervalIndex = 0;
        }
    }

    public SourceCadenceMetrics GetSourceCadenceMetrics()
    {
        double[] samples;
        double expectedIntervalMs;
        lock (_cadenceLock)
        {
            expectedIntervalMs = _expectedIntervalMs;
            if (_sourceIntervalCount <= 0)
            {
                return new SourceCadenceMetrics(
                    SampleCount: 0,
                    ObservedFps: 0,
                    ExpectedIntervalMs: expectedIntervalMs,
                    AverageIntervalMs: 0,
                    P95IntervalMs: 0,
                    P99IntervalMs: 0,
                    MaxIntervalMs: 0,
                    OnePercentLowFps: 0,
                    FivePercentLowFps: 0,
                    SampleDurationMs: 0,
                    RecentIntervalsMs: Array.Empty<double>(),
                    JitterStdDevMs: 0,
                    SevereGapCount: 0,
                    EstimatedDroppedFrames: 0,
                    EstimatedDropPercent: 0);
            }

            samples = new double[_sourceIntervalCount];
            for (var i = 0; i < _sourceIntervalCount; i++)
            {
                var ringIndex = (_sourceIntervalIndex - _sourceIntervalCount + i + _sourceIntervalWindowMs.Length)
                    % _sourceIntervalWindowMs.Length;
                samples[i] = _sourceIntervalWindowMs[ringIndex];
            }
        }

        var sampleCount = samples.Length;
        var sum = 0.0;
        var max = 0.0;
        for (var i = 0; i < sampleCount; i++)
        {
            sum += samples[i];
            if (samples[i] > max)
            {
                max = samples[i];
            }
        }

        var average = sum / sampleCount;
        var observedFps = average > double.Epsilon ? 1000.0 / average : 0;
        var targetIntervalMs = expectedIntervalMs > 0 ? expectedIntervalMs : average;
        var severeGapThresholdMs = targetIntervalMs * 1.6;

        long severeGapCount = 0;
        long estimatedDroppedFrames = 0;
        var varianceSum = 0.0;
        for (var i = 0; i < sampleCount; i++)
        {
            var interval = samples[i];
            var delta = interval - average;
            varianceSum += delta * delta;
            if (interval >= severeGapThresholdMs)
            {
                severeGapCount++;
            }

            if (targetIntervalMs > double.Epsilon)
            {
                estimatedDroppedFrames += Math.Max(0, (int)Math.Round(interval / targetIntervalMs) - 1);
            }
        }

        var jitterStdDevMs = Math.Sqrt(varianceSum / sampleCount);
        var sorted = (double[])samples.Clone();
        Array.Sort(sorted);
        var p95Index = (int)Math.Ceiling((sorted.Length - 1) * 0.95);
        var p99Index = (int)Math.Ceiling((sorted.Length - 1) * 0.99);
        var p95IntervalMs = sorted[Math.Clamp(p95Index, 0, sorted.Length - 1)];
        var p99IntervalMs = sorted[Math.Clamp(p99Index, 0, sorted.Length - 1)];
        var onePercentLowFps = p99IntervalMs > double.Epsilon ? 1000.0 / p99IntervalMs : 0;
        var fivePercentLowFps = p95IntervalMs > double.Epsilon ? 1000.0 / p95IntervalMs : 0;
        var estimatedDropPercent = estimatedDroppedFrames * 100.0 / Math.Max(1, sampleCount + estimatedDroppedFrames);

        return new SourceCadenceMetrics(
            SampleCount: sampleCount,
            ObservedFps: observedFps,
            ExpectedIntervalMs: targetIntervalMs,
            AverageIntervalMs: average,
            P95IntervalMs: p95IntervalMs,
            P99IntervalMs: p99IntervalMs,
            MaxIntervalMs: max,
            OnePercentLowFps: onePercentLowFps,
            FivePercentLowFps: fivePercentLowFps,
            SampleDurationMs: sum,
            RecentIntervalsMs: samples,
            JitterStdDevMs: jitterStdDevMs,
            SevereGapCount: severeGapCount,
            EstimatedDroppedFrames: estimatedDroppedFrames,
            EstimatedDropPercent: estimatedDropPercent);
    }

    private void TrackSourceCadence(long mfTimestamp100ns)
    {
        var previousTimestamp = Volatile.Read(ref _prevMfTimestamp100ns);
        if (previousTimestamp < 0)
        {
            Volatile.Write(ref _prevMfTimestamp100ns, mfTimestamp100ns);
            return;
        }

        var intervalMs = (mfTimestamp100ns - previousTimestamp) / 10_000.0;
        Volatile.Write(ref _prevMfTimestamp100ns, mfTimestamp100ns);
        if (intervalMs <= 0 || intervalMs > 5000)
        {
            return;
        }

        // Keep source cadence state coherent with diagnostics snapshots and frame-rate changes.
        lock (_cadenceLock)
        {
            var window = _sourceIntervalWindowMs;
            if (window.Length == 0)
            {
                return;
            }

            var idx = _sourceIntervalIndex;
            if (idx < 0 || idx >= window.Length)
            {
                idx = 0;
            }

            window[idx] = intervalMs;
            _sourceIntervalIndex = (idx + 1) % window.Length;
            if (_sourceIntervalCount < window.Length)
            {
                _sourceIntervalCount++;
            }
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

    private static int GetRowBytes(int width, bool isP010)
        => isP010 ? width * 2 : width;

    private unsafe static void CopyYuvWithStride(
        byte* sourceStart,
        int stride,
        Span<byte> destination,
        int width,
        int height,
        bool isP010)
    {
        var rowBytes = GetRowBytes(width, isP010);
        var uvHeight = height / 2;
        var yBytes = rowBytes * height;
        var uvBytes = rowBytes * uvHeight;
        if (destination.Length < yBytes + uvBytes)
        {
            throw new ArgumentException("Destination span is too small for packed frame.");
        }

        var strideAbs = Math.Abs(stride);
        if (strideAbs < rowBytes)
        {
            throw new InvalidOperationException(
                $"Source stride ({stride}) is smaller than packed row width ({rowBytes}).");
        }

        var yDest = destination[..yBytes];
        var uvDest = destination.Slice(yBytes, uvBytes);
        var yStart = sourceStart;
        var uvStart = sourceStart + (stride * height);

        if (stride < 0)
        {
            yStart = sourceStart + (stride * (height - 1));
            uvStart = sourceStart + (stride * (height + uvHeight - 1));
        }

        for (var row = 0; row < height; row++)
        {
            var src = stride >= 0
                ? yStart + (row * stride)
                : yStart - (row * strideAbs);
            var dst = yDest.Slice(row * rowBytes, rowBytes);
            new ReadOnlySpan<byte>(src, rowBytes).CopyTo(dst);
        }

        for (var row = 0; row < uvHeight; row++)
        {
            var src = stride >= 0
                ? uvStart + (row * stride)
                : uvStart - (row * strideAbs);
            var dst = uvDest.Slice(row * rowBytes, rowBytes);
            new ReadOnlySpan<byte>(src, rowBytes).CopyTo(dst);
        }
    }

    private static int InferPackedStride(int currentLength, int height)
    {
        if (currentLength <= 0 || height <= 0)
        {
            return 0;
        }

        var totalRows = height + (height / 2);
        return totalRows > 0 ? currentLength / totalRows : 0;
    }

    private static string SubtypeGuidToName(Guid subtype)
    {
        if (subtype == MfGuids.MFVideoFormat_P010) return "P010";
        if (subtype == MfGuids.MFVideoFormat_NV12) return "NV12";
        if (subtype == new Guid(0x32595559, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71)) return "YUY2";
        if (subtype == new Guid(0x47504A4D, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71)) return "MJPG";
        if (subtype == new Guid(0x00000014, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71)) return "RGB24";
        // FourCC-style: first 4 bytes of GUID are the FourCC.
        var bytes = subtype.ToByteArray();
        if (bytes[4] == 0 && bytes[5] == 0 && bytes[6] == 0x10 && bytes[7] == 0)
        {
            var fourcc = new char[4];
            for (var i = 0; i < 4; i++)
            {
                fourcc[i] = bytes[i] >= 0x20 && bytes[i] <= 0x7E ? (char)bytes[i] : '?';
            }

            return new string(fourcc);
        }

        return subtype.ToString("B");
    }
}
