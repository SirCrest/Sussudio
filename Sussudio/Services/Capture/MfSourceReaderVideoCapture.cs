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
    private static readonly Guid ID3D11Texture2DIid = new(
        0x6F15AAF2, 0xD208, 0x4E89, 0x9A, 0xB4, 0x48, 0x95, 0x35, 0xD3, 0x4F, 0x9C);
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
    private const int CadenceWindowSeconds = 20;
    private readonly object _cadenceLock = new();
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
        Interlocked.Exchange(ref _prevMfTimestamp100ns, -1);
        lock (_cadenceLock)
        {
            Array.Clear(_sourceIntervalWindowMs, 0, _sourceIntervalWindowMs.Length);
            _sourceIntervalCount = 0;
            _sourceIntervalIndex = 0;
        }

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

#if DEBUG
    /// <summary>
    /// One-shot diagnostic: compares raw COM vtable dispatch with managed interface dispatch
    /// to detect vtable misalignment in the IMFSample COM interop definition.
    /// IMFSample inherits IMFAttributes (30 methods). If .NET miscalculates the derived
    /// method offsets, managed calls will hit wrong vtable slots.
    /// Expected vtable layout: IUnknown(3) + IMFAttributes(30) + IMFSample(14) = 47 slots.
    /// </summary>
    private unsafe void DiagnoseVtable(IMFSample sample)
    {
        try
        {
            // --- Raw vtable dispatch (ground truth) ---
            var punk = Marshal.GetIUnknownForObject(sample);
            try
            {
                var iidSample = new Guid("c40a00f2-b93a-4d80-ae8c-5a1c634f58e4");
                var qiHr = Marshal.QueryInterface(punk, ref iidSample, out var pSample);
                Log($"VTABLE_DIAG QI_for_IMFSample hr=0x{qiHr:X8} pUnk=0x{punk:X16} pSample=0x{pSample:X16} same={punk == pSample}");

                if (qiHr < 0 || pSample == IntPtr.Zero)
                {
                    Log("VTABLE_DIAG QI FAILED — cannot diagnose vtable");
                    return;
                }

                try
                {
                    var vtable = *(IntPtr*)pSample;

                    // GetSampleTime = slot 35 (3 IUnknown + 30 IMFAttributes + 2 IMFSample)
                    // HRESULT GetSampleTime(IMFSample* this, LONGLONG* phnsSampleTime)
                    {
                        var fn = *(IntPtr*)((byte*)vtable + 35 * sizeof(IntPtr));
                        long time = -1;
                        var hr = ((delegate* unmanaged[Stdcall]<IntPtr, long*, int>)fn)(pSample, &time);
                        Log($"VTABLE_DIAG RAW slot35_GetSampleTime hr=0x{hr:X8} time={time}");
                    }

                    // GetBufferCount = slot 39
                    // HRESULT GetBufferCount(IMFSample* this, DWORD* pdwBufferCount)
                    {
                        var fn = *(IntPtr*)((byte*)vtable + 39 * sizeof(IntPtr));
                        int count = -1;
                        var hr = ((delegate* unmanaged[Stdcall]<IntPtr, int*, int>)fn)(pSample, &count);
                        Log($"VTABLE_DIAG RAW slot39_GetBufferCount hr=0x{hr:X8} count={count}");
                    }

                    // ConvertToContiguousBuffer = slot 41
                    // HRESULT ConvertToContiguousBuffer(IMFSample* this, IMFMediaBuffer** ppBuffer)
                    {
                        var fn = *(IntPtr*)((byte*)vtable + 41 * sizeof(IntPtr));
                        IntPtr buf = IntPtr.Zero;
                        var hr = ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)fn)(pSample, &buf);
                        Log($"VTABLE_DIAG RAW slot41_ConvertToContiguousBuffer hr=0x{hr:X8} buffer=0x{buf:X16}");
                        if (buf != IntPtr.Zero)
                        {
                            // Probe the buffer: Lock it to see actual frame data
                            // IMFMediaBuffer::Lock = slot 3 (IUnknown + first method)
                            var bufVtable = *(IntPtr*)buf;
                            var lockFn = *(IntPtr*)((byte*)bufVtable + 3 * sizeof(IntPtr));
                            IntPtr dataPtr = IntPtr.Zero;
                            int maxLen = 0, curLen = 0;
                            var lockHr = ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int*, int*, int>)lockFn)(
                                buf, &dataPtr, &maxLen, &curLen);
                            Log($"VTABLE_DIAG RAW buffer_Lock hr=0x{lockHr:X8} data=0x{dataPtr:X16} maxLen={maxLen} curLen={curLen}");

                            if (lockHr >= 0)
                            {
                                // Unlock: slot 4
                                var unlockFn = *(IntPtr*)((byte*)bufVtable + 4 * sizeof(IntPtr));
                                ((delegate* unmanaged[Stdcall]<IntPtr, int>)unlockFn)(buf);
                            }

                            Marshal.Release(buf);
                        }
                    }

                    // --- Managed interface dispatch (what .NET thinks the slots are) ---
                    {
                        var hr = sample.GetSampleTime(out var time);
                        Log($"VTABLE_DIAG MANAGED GetSampleTime hr=0x{hr:X8} time={time}");
                    }
                    {
                        var hr = sample.GetBufferCount(out var count);
                        Log($"VTABLE_DIAG MANAGED GetBufferCount hr=0x{hr:X8} count={count}");
                    }
                    {
                        var hr = sample.ConvertToContiguousBuffer(out var buf);
                        Log($"VTABLE_DIAG MANAGED ConvertToContiguousBuffer hr=0x{hr:X8} buffer={(buf != null ? "non-null" : "null")}");
                        if (buf != null) Marshal.ReleaseComObject(buf);
                    }
                }
                finally
                {
                    Marshal.Release(pSample);
                }
            }
            finally
            {
                Marshal.Release(punk);
            }
        }
        catch (Exception ex)
        {
            Log($"VTABLE_DIAG EXCEPTION type={ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
        }
    }
#endif

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

    private bool TryGetDxgiTexture(IMFMediaBuffer buffer, out IntPtr gpuTexture, out int gpuSubresource)
    {
        gpuTexture = IntPtr.Zero;
        gpuSubresource = 0;
        if (!Volatile.Read(ref _sourceReaderD3DEnabled) || _dxgiDeviceManagerPtr == IntPtr.Zero)
        {
            return false;
        }

        if (buffer is not IMFDXGIBuffer dxgiBuffer)
        {
            if (Interlocked.CompareExchange(ref _dxgiBufferProbeDone, 1, 0) == 0)
            {
                Log(
                    "MF_SOURCE_READER_D3D_BUFFER_MISS " +
                    $"buffer_type={buffer.GetType().Name} fallback=cpu");
            }

            return false;
        }

        var textureIid = ID3D11Texture2DIid;
        var getResourceHr = dxgiBuffer.GetResource(ref textureIid, out gpuTexture);
        if (getResourceHr < 0 || gpuTexture == IntPtr.Zero)
        {
            var failureCount = Interlocked.Increment(ref _dxgiResourceFailureCount);
            if (failureCount <= 3)
            {
                Log(
                    "MF_SOURCE_READER_D3D_RESOURCE_FAIL " +
                    $"stage=GetResource hr=0x{getResourceHr:X8} fallback=cpu");
            }

            gpuTexture = IntPtr.Zero;
            return false;
        }

        var subresourceHr = dxgiBuffer.GetSubresourceIndex(out var subresource);
        if (subresourceHr < 0)
        {
            var failureCount = Interlocked.Increment(ref _dxgiResourceFailureCount);
            if (failureCount <= 3)
            {
                Log(
                    "MF_SOURCE_READER_D3D_RESOURCE_FAIL " +
                    $"stage=GetSubresourceIndex hr=0x{subresourceHr:X8} fallback=cpu");
            }

            Marshal.Release(gpuTexture);
            gpuTexture = IntPtr.Zero;
            return false;
        }

        gpuSubresource = unchecked((int)subresource);
        return true;
    }

    public static int GetFrameSizeBytes(int width, int height, bool isP010)
        => isP010 ? width * height * 3 : (width * height * 3) / 2;

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

    private static string SubtypeGuidToName(Guid subtype)
    {
        if (subtype == MfGuids.MFVideoFormat_P010) return "P010";
        if (subtype == MfGuids.MFVideoFormat_NV12) return "NV12";
        if (subtype == new Guid(0x32595559, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71)) return "YUY2";
        if (subtype == new Guid(0x47504A4D, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71)) return "MJPG";
        if (subtype == new Guid(0x00000014, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71)) return "RGB24";
        // FourCC-style: first 4 bytes of GUID are the FourCC
        var bytes = subtype.ToByteArray();
        if (bytes[4] == 0 && bytes[5] == 0 && bytes[6] == 0x10 && bytes[7] == 0)
        {
            var fourcc = new char[4];
            for (var i = 0; i < 4; i++)
                fourcc[i] = bytes[i] >= 0x20 && bytes[i] <= 0x7E ? (char)bytes[i] : '?';
            return new string(fourcc);
        }
        return subtype.ToString("B");
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
