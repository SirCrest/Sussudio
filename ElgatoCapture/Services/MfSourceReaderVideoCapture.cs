using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ElgatoCapture.Services;

public sealed class MfSourceReaderVideoCapture : IAsyncDisposable
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
    private readonly object _cadenceLock = new();
    private readonly double[] _sourceIntervalWindowMs = new double[300];
    private int _sourceIntervalCount;
    private int _sourceIntervalIndex;
    private long _prevMfTimestamp100ns = -1;
    private double _expectedIntervalMs;

    public long FramesDelivered => Interlocked.Read(ref _framesDelivered);
    public long FramesDropped => Interlocked.Read(ref _framesDropped);
    public string NegotiatedFormat => Volatile.Read(ref _negotiatedFormat);
    public string NativeInputFormat => Volatile.Read(ref _nativeInputFormat);
    public bool IsP010 => Volatile.Read(ref _isP010);
    public bool IsD3DOutputEnabled => Volatile.Read(ref _sourceReaderD3DEnabled);
    public bool IsHighFrameRateMjpegMode => Volatile.Read(ref _strictD3DOutputRequired);
    public int Width => Volatile.Read(ref _width);
    public int Height => Volatile.Read(ref _height);
    public double Fps => Volatile.Read(ref _fps);
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
        double MaxIntervalMs,
        double JitterStdDevMs,
        long SevereGapCount,
        long EstimatedDroppedFrames,
        double EstimatedDropPercent);

    public Task InitializeAsync(
        string deviceSymbolicLink,
        int width,
        int height,
        double fps,
        bool requireP010,
        string? requestedPixelFormat = null,
        bool useMjpegHighFrameRateMode = false,
        IntPtr dxgiDeviceManager = default)
    {
        if (string.IsNullOrWhiteSpace(deviceSymbolicLink))
        {
            throw new ArgumentException("Video device symbolic link is required.", nameof(deviceSymbolicLink));
        }

        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Video width/height must be positive.");
        }

        if (fps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fps), "Video frame rate must be positive.");
        }

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
                                    string.Equals(requestedPixelFormat, "MJPG", StringComparison.OrdinalIgnoreCase);

        try
        {
            MfInterop.AddStartupReference();
            startupHeld = true;

            mediaSource = CreateMediaSource(deviceSymbolicLink);

            disableConverters = !useConvertedMjpegNv12;
            ThrowIfFailed(
                MfInterop.MFCreateAttributes(out readerAttributes, useConvertedMjpegNv12 ? 3 : 2),
                "MFCreateAttributes(reader)");
            if (useConvertedMjpegNv12)
            {
                ThrowIfFailed(
                    readerAttributes.SetUINT32(ref MfGuids.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, 1),
                    "IMFAttributes.SetUINT32(MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS)");
            }
            if (disableConverters)
            {
                ThrowIfFailed(
                    readerAttributes.SetUINT32(ref MfGuids.MF_READWRITE_DISABLE_CONVERTERS, 1),
                    "IMFAttributes.SetUINT32(MF_READWRITE_DISABLE_CONVERTERS)");
            }

            if (dxgiDeviceManager != IntPtr.Zero &&
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
                    ThrowIfFailed(createSourceReaderHr, "MFCreateSourceReaderFromMediaSource(hfr_mjpeg_d3d)");
                }

                Log(
                    "MF_SOURCE_READER_D3D_INIT_WARN " +
                    $"stage=CreateSourceReader hr=0x{createSourceReaderHr:X8} " +
                    "fallback=cpu_only");

                ReleaseComObject(ref sourceReader);
                ReleaseComObject(ref readerAttributes);

                ThrowIfFailed(
                    MfInterop.MFCreateAttributes(out readerAttributes, 1),
                    "MFCreateAttributes(reader_cpu_fallback)");
                if (disableConverters)
                {
                    ThrowIfFailed(
                        readerAttributes.SetUINT32(ref MfGuids.MF_READWRITE_DISABLE_CONVERTERS, 1),
                        "IMFAttributes.SetUINT32(MF_READWRITE_DISABLE_CONVERTERS)");
                }

                sourceReaderD3DEnabled = false;
                createSourceReaderHr = MfInterop.MFCreateSourceReaderFromMediaSource(mediaSource, readerAttributes, out sourceReader);
            }

            ThrowIfFailed(createSourceReaderHr, "MFCreateSourceReaderFromMediaSource");

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

            ThrowIfFailed(
                sourceReader.SetCurrentMediaType(
                    MfConstants.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                    IntPtr.Zero,
                    selectedMediaType),
                $"IMFSourceReader.SetCurrentMediaType({SubtypeGuidToName(negotiatedSubtype)})");

            ThrowIfFailed(
                sourceReader.GetCurrentMediaType(
                    MfConstants.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                    out actualMediaType),
                "IMFSourceReader.GetCurrentMediaType");

            if (actualMediaType != null)
            {
                if (TryGetGuid(actualMediaType, ref MfGuids.MF_MT_SUBTYPE, out var actualSubtype))
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

            _deviceSymbolicLink = deviceSymbolicLink;
            _width = negotiatedWidth;
            _height = negotiatedHeight;
            _fps = negotiatedFps;
            SetExpectedFrameRate(_fps);
            _isP010 = negotiatedSubtype == MfGuids.MFVideoFormat_P010;
            _strictD3DOutputRequired = useConvertedMjpegNv12;
            _strictTextureOutputRequired = useConvertedMjpegNv12;
            Volatile.Write(ref _nativeInputFormat, useConvertedMjpegNv12 ? "MJPG" : SubtypeGuidToName(negotiatedSubtype));
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
            ReleaseComObject(ref actualMediaType);
            ReleaseComObject(ref selectedMediaType);
            ReleaseComObject(ref readerAttributes);
            ReleaseComObject(ref sourceReader);
            ReleaseComObject(ref mediaSource);

            if (startupHeld)
            {
                MfInterop.ReleaseStartupReference();
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
            MfInterop.ReleaseStartupReference();
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

                if (hr == MfHResults.MF_E_SHUTDOWN || hr == MfHResults.MF_E_INVALIDREQUEST)
                {
                    if (ct.IsCancellationRequested)
                    {
                        break;
                    }

                    ThrowIfFailed(hr, "IMFSourceReader.ReadSample");
                }
                else
                {
                    ThrowIfFailed(hr, "IMFSourceReader.ReadSample");
                }

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
                ReleaseComObject(ref sample);
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
                    MaxIntervalMs: 0,
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
        var p95IntervalMs = sorted[Math.Clamp(p95Index, 0, sorted.Length - 1)];
        var estimatedDropPercent = estimatedDroppedFrames * 100.0 / Math.Max(1, sampleCount + estimatedDroppedFrames);

        return new SourceCadenceMetrics(
            SampleCount: sampleCount,
            ObservedFps: observedFps,
            ExpectedIntervalMs: targetIntervalMs,
            AverageIntervalMs: average,
            P95IntervalMs: p95IntervalMs,
            MaxIntervalMs: max,
            JitterStdDevMs: jitterStdDevMs,
            SevereGapCount: severeGapCount,
            EstimatedDroppedFrames: estimatedDroppedFrames,
            EstimatedDropPercent: estimatedDropPercent);
    }

    private void TrackSourceCadence(long mfTimestamp100ns)
    {
        var previousTimestamp = Interlocked.Read(ref _prevMfTimestamp100ns);
        if (previousTimestamp < 0)
        {
            Interlocked.Exchange(ref _prevMfTimestamp100ns, mfTimestamp100ns);
            return;
        }

        var intervalMs = (mfTimestamp100ns - previousTimestamp) / 10_000.0;
        Interlocked.Exchange(ref _prevMfTimestamp100ns, mfTimestamp100ns);
        if (intervalMs <= 0 || intervalMs > 5000)
        {
            return;
        }

        lock (_cadenceLock)
        {
            _sourceIntervalWindowMs[_sourceIntervalIndex] = intervalMs;
            _sourceIntervalIndex = (_sourceIntervalIndex + 1) % _sourceIntervalWindowMs.Length;
            if (_sourceIntervalCount < _sourceIntervalWindowMs.Length)
            {
                _sourceIntervalCount++;
            }
        }
    }

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

    private unsafe void DeliverFrame(
        IMFSample sample,
        RawFrameCallback? onFrame,
        DualFrameCallback? onDualFrame,
        long arrivalTick)
    {
        // One-shot vtable diagnostic — runs on the very first sample to compare
        // raw vtable dispatch vs managed COM interop dispatch. This definitively
        // reveals whether .NET's vtable slot calculation for IMFSample is correct.
        if (Interlocked.CompareExchange(ref _vtableDiagDone, 1, 0) == 0)
        {
            DiagnoseVtable(sample);
        }

        IMFMediaBuffer? buffer = null;
        try
        {
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

            if (buffer == null)
            {
                Interlocked.Increment(ref _framesDropped);
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
            ReleaseComObject(ref buffer);
        }
    }

    private unsafe void DeliverRawFrameFromBuffer(IMFMediaBuffer buffer, RawFrameCallback onFrame, long arrivalTick)
    {
        if (TryDeliverFrameFrom2DBuffer(buffer, onFrame, arrivalTick))
        {
            return;
        }

        ThrowIfFailed(
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
                    if (_isP010)
                    {
                        CopyP010WithStride((byte*)dataPtr, inferredStride, packedSpan, _width, _height);
                    }
                    else
                    {
                        CopyNV12WithStride((byte*)dataPtr, inferredStride, packedSpan, _width, _height);
                    }

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
            if (TryDeliverDualFrameFrom2DBuffer(buffer, gpuTexture, gpuSubresource, onDualFrame, arrivalTick))
            {
                return;
            }

            ThrowIfFailed(
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
                        if (_isP010)
                        {
                            CopyP010WithStride((byte*)dataPtr, inferredStride, packedSpan, _width, _height);
                        }
                        else
                        {
                            CopyNV12WithStride((byte*)dataPtr, inferredStride, packedSpan, _width, _height);
                        }

                        InvokeDualFrameCallback(
                            onDualFrame,
                            gpuTexture,
                            gpuSubresource,
                            packedSpan,
                            _width,
                            _height,
                            arrivalTick);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(packed);
                    }
                }
                else
                {
                    InvokeDualFrameCallback(
                        onDualFrame,
                        gpuTexture,
                        gpuSubresource,
                        new ReadOnlySpan<byte>((void*)dataPtr, packedFrameBytes),
                        _width,
                        _height,
                        arrivalTick);
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

        ThrowIfFailed(
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
                if (_isP010)
                {
                    CopyP010WithStride((byte*)scanlinePtr, pitch, packedSpan, _width, _height);
                }
                else
                {
                    CopyNV12WithStride((byte*)scanlinePtr, pitch, packedSpan, _width, _height);
                }

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

        ThrowIfFailed(
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
                InvokeDualFrameCallback(
                    onFrame,
                    gpuTexture,
                    gpuSubresource,
                    new ReadOnlySpan<byte>((void*)scanlinePtr, packedFrameBytes),
                    _width,
                    _height,
                    arrivalTick);
                return true;
            }

            var packed = ArrayPool<byte>.Shared.Rent(packedFrameBytes);
            try
            {
                var packedSpan = packed.AsSpan(0, packedFrameBytes);
                if (_isP010)
                {
                    CopyP010WithStride((byte*)scanlinePtr, pitch, packedSpan, _width, _height);
                }
                else
                {
                    CopyNV12WithStride((byte*)scanlinePtr, pitch, packedSpan, _width, _height);
                }

                InvokeDualFrameCallback(onFrame, gpuTexture, gpuSubresource, packedSpan, _width, _height, arrivalTick);
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

    private static void InvokeDualFrameCallback(
        DualFrameCallback callback,
        IntPtr gpuTexture,
        int gpuSubresource,
        ReadOnlySpan<byte> cpuData,
        int width,
        int height,
        long arrivalTick)
    {
        callback(gpuTexture, gpuSubresource, cpuData, width, height, arrivalTick);
    }

    private bool TrySetSourceReaderD3DManager(IMFAttributes attributes, IntPtr dxgiDeviceManager)
    {
        object? managerAsUnknown = null;
        try
        {
            managerAsUnknown = Marshal.GetObjectForIUnknown(dxgiDeviceManager);
            ThrowIfFailed(
                attributes.SetUnknown(ref MfGuids.MF_SOURCE_READER_D3D_MANAGER, managerAsUnknown),
                "IMFAttributes.SetUnknown(MF_SOURCE_READER_D3D_MANAGER)");
            return true;
        }
        catch (Exception ex)
        {
            Log(
                "MF_SOURCE_READER_D3D_INIT_WARN " +
                $"stage=SetUnknown type={ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message} " +
                "fallback=cpu_only");
            return false;
        }
        finally
        {
            if (managerAsUnknown != null && Marshal.IsComObject(managerAsUnknown))
            {
                _ = Marshal.ReleaseComObject(managerAsUnknown);
            }
        }
    }

    private IMFMediaSource CreateMediaSource(string deviceSymbolicLink)
    {
        IMFAttributes? attrs = null;
        try
        {
            ThrowIfFailed(MfInterop.MFCreateAttributes(out attrs, 2), "MFCreateAttributes(device)");
            ThrowIfFailed(
                attrs.SetGUID(
                    ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE,
                    ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID),
                "IMFAttributes.SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE)");
            ThrowIfFailed(
                attrs.SetString(
                    ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK,
                    deviceSymbolicLink),
                "IMFAttributes.SetString(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK)");

            var directHr = MfInterop.MFCreateDeviceSource(attrs, out var mediaSource);
            if (directHr >= 0 && mediaSource != null)
            {
                return mediaSource;
            }

            Log(
                "MF_SOURCE_READER_DEVICE_OPEN_DIRECT_FAIL " +
                $"device='{deviceSymbolicLink}' hr=0x{directHr:X8}");
            return CreateMediaSourceByEnumeration(deviceSymbolicLink, directHr);
        }
        finally
        {
            ReleaseComObject(ref attrs);
        }
    }

    private IMFMediaSource CreateMediaSourceByEnumeration(string targetSymbolicLink, int directHr)
    {
        IMFAttributes? attrs = null;
        IntPtr activateArrayPtr = IntPtr.Zero;
        var candidates = new List<string>();

        try
        {
            ThrowIfFailed(MfInterop.MFCreateAttributes(out attrs, 1), "MFCreateAttributes(enum)");
            ThrowIfFailed(
                attrs.SetGUID(
                    ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE,
                    ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID),
                "IMFAttributes.SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE)");

            ThrowIfFailed(
                MfInterop.MFEnumDeviceSources(attrs, out activateArrayPtr, out var activateCount),
                "MFEnumDeviceSources");

            if (activateCount <= 0 || activateArrayPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"No video capture devices were reported while opening '{targetSymbolicLink}'.");
            }

            for (var i = 0; i < activateCount; i++)
            {
                var activatePtr = Marshal.ReadIntPtr(activateArrayPtr, i * IntPtr.Size);
                if (activatePtr == IntPtr.Zero)
                {
                    continue;
                }

                IMFActivate? activate = null;
                try
                {
                    activate = (IMFActivate)Marshal.GetObjectForIUnknown(activatePtr);
                    _ = Marshal.Release(activatePtr);

                    var link = TryReadAllocatedString(
                        activate,
                        ref MfGuids.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK);
                    if (!string.IsNullOrWhiteSpace(link))
                    {
                        candidates.Add(link);
                    }

                    if (!SymbolicLinksMatch(targetSymbolicLink, link))
                    {
                        continue;
                    }

                    var mediaSourceIid = typeof(IMFMediaSource).GUID;
                    ThrowIfFailed(
                        activate.ActivateObject(ref mediaSourceIid, out var activated),
                        "IMFActivate.ActivateObject(IMFMediaSource)");

                    if (activated is IMFMediaSource source)
                    {
                        return source;
                    }

                    throw new InvalidOperationException(
                        $"Activated object for '{link}' does not implement IMFMediaSource.");
                }
                finally
                {
                    ReleaseComObject(ref activate);
                }
            }

            var candidateSummary = candidates.Count > 0
                ? string.Join(" | ", candidates)
                : "none";
            throw new InvalidOperationException(
                "Unable to open capture device by symbolic link. " +
                $"requested='{targetSymbolicLink}' direct_hr=0x{directHr:X8} candidates='{candidateSummary}'. " +
                "If this device cannot be shared, close other capture apps and retry with Windows Frame Server enabled.");
        }
        finally
        {
            if (activateArrayPtr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(activateArrayPtr);
            }

            ReleaseComObject(ref attrs);
        }
    }

    private IMFMediaType SelectMediaType(
        IMFSourceReader reader,
        int requestedWidth,
        int requestedHeight,
        double requestedFps,
        Guid requestedSubtype,
        out Guid selectedSubtype,
        out int selectedWidth,
        out int selectedHeight,
        out double selectedFps,
        out string negotiatedDescription)
    {
        IMFMediaType? bestType = null;
        var bestFpsDelta = double.MaxValue;
        selectedSubtype = requestedSubtype;
        selectedWidth = requestedWidth;
        selectedHeight = requestedHeight;
        selectedFps = requestedFps;
        negotiatedDescription = "unknown";

        var totalNativeTypes = 0;
        var requestedSubtypeCount = 0;
        var subtypeSummary = new Dictionary<string, int>();
        var requestedSubtypeName = SubtypeGuidToName(requestedSubtype);

        for (var index = 0; ; index++)
        {
            IMFMediaType? nativeType = null;
            try
            {
                var hr = reader.GetNativeMediaType(
                    MfConstants.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                    index,
                    out nativeType);
                if (hr == MfHResults.MF_E_NO_MORE_TYPES)
                {
                    break;
                }

                ThrowIfFailed(hr, $"IMFSourceReader.GetNativeMediaType(index={index})");
                if (nativeType == null)
                {
                    continue;
                }

                totalNativeTypes++;
                var hasSubtype = TryGetGuid(nativeType, ref MfGuids.MF_MT_SUBTYPE, out var subtype);
                var subtypeName = hasSubtype ? SubtypeGuidToName(subtype) : "unknown";

                if (!subtypeSummary.ContainsKey(subtypeName))
                    subtypeSummary[subtypeName] = 0;
                subtypeSummary[subtypeName]++;

                TryGetFrameSize(nativeType, out var nWidth, out var nHeight);
                var nFps = TryGetFrameRate(nativeType, out var nNum, out var nDen) && nDen > 0
                    ? (double)nNum / nDen : 0;

                if (hasSubtype && subtype == requestedSubtype)
                {
                    requestedSubtypeCount++;
                    Log($"MF_SOURCE_READER_NATIVE_{requestedSubtypeName} index={index} {nWidth}x{nHeight}@{nFps:0.###}");
                }

                if (!hasSubtype || subtype != requestedSubtype)
                {
                    continue;
                }

                var width = nWidth;
                var height = nHeight;
                if (width != requestedWidth || height != requestedHeight)
                {
                    continue;
                }

                var frameRate = TryGetFrameRate(nativeType, out var fpsNumerator, out var fpsDenominator)
                    ? (double)fpsNumerator / fpsDenominator
                    : 0;
                var delta = Math.Abs(frameRate - requestedFps);

                if (delta < bestFpsDelta)
                {
                    ReleaseComObject(ref bestType);
                    bestType = nativeType;
                    nativeType = null;
                    bestFpsDelta = delta;
                    selectedWidth = width;
                    selectedHeight = height;
                    selectedFps = frameRate > 0 ? frameRate : requestedFps;
                    selectedSubtype = subtype;
                    negotiatedDescription = frameRate > 0
                        ? $"{requestedSubtypeName} {width}x{height}@{frameRate:0.###}"
                        : $"{requestedSubtypeName} {width}x{height}";
                }
            }
            finally
            {
                ReleaseComObject(ref nativeType);
            }
        }

        var subtypeList = string.Join(", ", subtypeSummary.Select(kv => $"{kv.Key}={kv.Value}"));
        Log(
            "MF_SOURCE_READER_NATIVE_TYPES " +
            $"total={totalNativeTypes} requested_subtype={requestedSubtypeName} " +
            $"requested_count={requestedSubtypeCount} subtypes=[{subtypeList}]");

        if (bestType == null)
        {
            throw new InvalidOperationException(
                $"No {requestedSubtypeName} media type was found for {requestedWidth}x{requestedHeight}@{requestedFps:0.###}. " +
                $"Source reader has {totalNativeTypes} native types ({requestedSubtypeCount} {requestedSubtypeName}). Subtypes: [{subtypeList}]");
        }

        if (bestFpsDelta > 0.5)
        {
            ReleaseComObject(ref bestType);
            throw new InvalidOperationException(
                $"No {requestedSubtypeName} media type matched requested frame rate {requestedFps:0.###}fps " +
                $"for {requestedWidth}x{requestedHeight}.");
        }

        return bestType;
    }

    private IMFMediaType SelectConvertedMediaType(
        IMFSourceReader reader,
        int requestedWidth,
        int requestedHeight,
        double requestedFps,
        Guid requestedSourceSubtype,
        Guid requestedOutputSubtype,
        out Guid selectedSubtype,
        out int selectedWidth,
        out int selectedHeight,
        out double selectedFps,
        out string negotiatedDescription)
    {
        var nativeType = SelectMediaType(
            reader,
            requestedWidth,
            requestedHeight,
            requestedFps,
            requestedSourceSubtype,
            out var nativeSubtype,
            out selectedWidth,
            out selectedHeight,
            out selectedFps,
            out _);

        IMFMediaType? convertedType = null;
        try
        {
            ThrowIfFailed(MfInterop.MFCreateMediaType(out convertedType), "MFCreateMediaType");

            ThrowIfFailed(
                convertedType.SetGUID(ref MfGuids.MF_MT_MAJOR_TYPE, ref MfGuids.MFMediaType_Video),
                "IMFMediaType.SetGUID(MF_MT_MAJOR_TYPE)");
            ThrowIfFailed(
                convertedType.SetGUID(ref MfGuids.MF_MT_SUBTYPE, ref requestedOutputSubtype),
                $"IMFMediaType.SetGUID(MF_MT_SUBTYPE,{SubtypeGuidToName(requestedOutputSubtype)})");

            if (TryGetUInt64(nativeType, ref MfGuids.MF_MT_FRAME_SIZE, out var frameSize))
            {
                ThrowIfFailed(
                    convertedType.SetUINT64(ref MfGuids.MF_MT_FRAME_SIZE, frameSize),
                    "IMFMediaType.SetUINT64(MF_MT_FRAME_SIZE)");
            }

            if (TryGetUInt64(nativeType, ref MfGuids.MF_MT_FRAME_RATE, out var frameRate))
            {
                ThrowIfFailed(
                    convertedType.SetUINT64(ref MfGuids.MF_MT_FRAME_RATE, frameRate),
                    "IMFMediaType.SetUINT64(MF_MT_FRAME_RATE)");
            }

            CopyOptionalUInt64(nativeType, convertedType, ref MfGuids.MF_MT_FRAME_RATE_RANGE_MIN);
            CopyOptionalUInt64(nativeType, convertedType, ref MfGuids.MF_MT_FRAME_RATE_RANGE_MAX);
            CopyOptionalUInt64(nativeType, convertedType, ref MfGuids.MF_MT_PIXEL_ASPECT_RATIO);
            CopyOptionalUInt32(nativeType, convertedType, ref MfGuids.MF_MT_INTERLACE_MODE);
            CopyOptionalUInt32(nativeType, convertedType, ref MfGuids.MF_MT_ALL_SAMPLES_INDEPENDENT);

            selectedSubtype = requestedOutputSubtype;
            negotiatedDescription =
                $"{SubtypeGuidToName(requestedOutputSubtype)} <= {SubtypeGuidToName(nativeSubtype)} {selectedWidth}x{selectedHeight}@{selectedFps:0.###}";

            var result = convertedType;
            convertedType = null;
            return result;
        }
        finally
        {
            ReleaseComObject(ref nativeType);
            ReleaseComObject(ref convertedType);
        }
    }

    private static bool TryGetFrameSize(IMFAttributes attributes, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (!TryGetUInt64(attributes, ref MfGuids.MF_MT_FRAME_SIZE, out var packed))
        {
            return false;
        }

        width = (int)(packed >> 32);
        height = (int)(packed & 0xFFFFFFFFu);
        return width > 0 && height > 0;
    }

    private static bool TryGetFrameRate(
        IMFAttributes attributes,
        out uint numerator,
        out uint denominator)
    {
        numerator = 0;
        denominator = 0;
        if (!TryGetUInt64(attributes, ref MfGuids.MF_MT_FRAME_RATE, out var packed))
        {
            return false;
        }

        numerator = (uint)(packed >> 32);
        denominator = (uint)(packed & 0xFFFFFFFFu);
        return numerator > 0 && denominator > 0;
    }

    private static bool TryGetGuid(IMFAttributes attributes, ref Guid key, out Guid value)
    {
        var hr = attributes.GetGUID(ref key, out value);
        if (hr == MfHResults.MF_E_ATTRIBUTENOTFOUND)
        {
            value = Guid.Empty;
            return false;
        }

        ThrowIfFailed(hr, $"IMFAttributes.GetGUID({key})");
        return true;
    }

    private static bool TryGetUInt64(IMFAttributes attributes, ref Guid key, out ulong value)
    {
        var hr = attributes.GetUINT64(ref key, out value);
        if (hr == MfHResults.MF_E_ATTRIBUTENOTFOUND)
        {
            value = 0;
            return false;
        }

        ThrowIfFailed(hr, $"IMFAttributes.GetUINT64({key})");
        return true;
    }

    private static bool TryGetUInt32(IMFAttributes attributes, ref Guid key, out int value)
    {
        var hr = attributes.GetUINT32(ref key, out value);
        if (hr == MfHResults.MF_E_ATTRIBUTENOTFOUND)
        {
            value = 0;
            return false;
        }

        ThrowIfFailed(hr, $"IMFAttributes.GetUINT32({key})");
        return true;
    }

    private static void CopyOptionalUInt64(IMFAttributes source, IMFAttributes destination, ref Guid key)
    {
        if (!TryGetUInt64(source, ref key, out var value))
        {
            return;
        }

        ThrowIfFailed(destination.SetUINT64(ref key, value), $"IMFAttributes.SetUINT64({key})");
    }

    private static void CopyOptionalUInt32(IMFAttributes source, IMFAttributes destination, ref Guid key)
    {
        if (!TryGetUInt32(source, ref key, out var value))
        {
            return;
        }

        ThrowIfFailed(destination.SetUINT32(ref key, value), $"IMFAttributes.SetUINT32({key})");
    }

    private static string TryReadAllocatedString(IMFAttributes attributes, ref Guid key)
    {
        IntPtr textPtr = IntPtr.Zero;
        try
        {
            var hr = attributes.GetAllocatedString(ref key, out textPtr, out var length);
            if (hr == MfHResults.MF_E_ATTRIBUTENOTFOUND || textPtr == IntPtr.Zero)
            {
                return string.Empty;
            }

            ThrowIfFailed(hr, $"IMFAttributes.GetAllocatedString({key})");
            return length > 0
                ? Marshal.PtrToStringUni(textPtr, length) ?? string.Empty
                : Marshal.PtrToStringUni(textPtr) ?? string.Empty;
        }
        finally
        {
            if (textPtr != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(textPtr);
            }
        }
    }

    private static bool SymbolicLinksMatch(string target, string candidate)
    {
        if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        return string.Equals(target, candidate, StringComparison.OrdinalIgnoreCase) ||
               candidate.Contains(target, StringComparison.OrdinalIgnoreCase) ||
               target.Contains(candidate, StringComparison.OrdinalIgnoreCase);
    }

    public static int GetFrameSizeBytes(int width, int height, bool isP010)
        => isP010 ? width * height * 3 : (width * height * 3) / 2;

    private static int GetRowBytes(int width, bool isP010)
        => isP010 ? width * 2 : width;

    private unsafe static void CopyP010WithStride(
        byte* sourceStart,
        int stride,
        Span<byte> destination,
        int width,
        int height)
    {
        var rowBytes = width * 2;
        var uvHeight = height / 2;
        var yBytes = rowBytes * height;
        var uvBytes = rowBytes * uvHeight;
        if (destination.Length < yBytes + uvBytes)
        {
            throw new ArgumentException("Destination span is too small for packed P010 frame.");
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

    private unsafe static void CopyNV12WithStride(
        byte* sourceStart,
        int stride,
        Span<byte> destination,
        int width,
        int height)
    {
        var rowBytes = width;
        var uvHeight = height / 2;
        var yBytes = rowBytes * height;
        var uvBytes = rowBytes * uvHeight;
        if (destination.Length < yBytes + uvBytes)
        {
            throw new ArgumentException("Destination span is too small for packed NV12 frame.");
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

        ReleaseComObject(ref sourceReader);
        ReleaseComObject(ref mediaSource);
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

    private static void ThrowIfFailed(int hr, string operation)
    {
        if (hr >= 0)
        {
            return;
        }

        throw new InvalidOperationException($"{operation} failed (hr=0x{hr:X8}).");
    }

    private static void ReleaseComObject<T>(ref T? comObject)
        where T : class
    {
        if (comObject == null)
        {
            return;
        }

        try
        {
            if (Marshal.IsComObject(comObject))
            {
                _ = Marshal.ReleaseComObject(comObject);
            }
        }
        catch
        {
            // Best effort during cleanup.
        }
        finally
        {
            comObject = null;
        }
    }

    private static class MfInterop
    {
        private static readonly object StartupSync = new();
        private static int _startupRefCount;

        [DllImport("mfplat.dll", ExactSpelling = true)]
        private static extern int MFStartup(int version, int dwFlags);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        private static extern int MFShutdown();

        [DllImport("mfplat.dll", ExactSpelling = true)]
        internal static extern int MFCreateAttributes(
            [MarshalAs(UnmanagedType.Interface)] out IMFAttributes ppMFAttributes,
            int cInitialSize);

        [DllImport("mf.dll", ExactSpelling = true)]
        internal static extern int MFEnumDeviceSources(
            [MarshalAs(UnmanagedType.Interface)] IMFAttributes pAttributes,
            out IntPtr pppSourceActivate,
            out int pcSourceActivate);

        [DllImport("mf.dll", ExactSpelling = true)]
        internal static extern int MFCreateDeviceSource(
            [MarshalAs(UnmanagedType.Interface)] IMFAttributes pAttributes,
            [MarshalAs(UnmanagedType.Interface)] out IMFMediaSource ppSource);

        [DllImport("mfreadwrite.dll", ExactSpelling = true)]
        internal static extern int MFCreateSourceReaderFromMediaSource(
            [MarshalAs(UnmanagedType.Interface)] IMFMediaSource pMediaSource,
            [MarshalAs(UnmanagedType.Interface)] IMFAttributes? pAttributes,
            [MarshalAs(UnmanagedType.Interface)] out IMFSourceReader ppSourceReader);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        internal static extern int MFCreateMediaType(
            [MarshalAs(UnmanagedType.Interface)] out IMFMediaType ppMFType);

        [DllImport("mfplat.dll", ExactSpelling = true)]
        internal static extern int MFCreateDXGIDeviceManager(out uint pResetToken, out IntPtr ppDeviceManager);

        internal static void AddStartupReference()
        {
            lock (StartupSync)
            {
                if (_startupRefCount == 0)
                {
                    ThrowIfFailed(MFStartup(MfConstants.MF_VERSION, 0), "MFStartup");
                }

                _startupRefCount++;
            }
        }

        internal static void ReleaseStartupReference()
        {
            lock (StartupSync)
            {
                if (_startupRefCount <= 0)
                {
                    return;
                }

                _startupRefCount--;
                if (_startupRefCount == 0)
                {
                    _ = MFShutdown();
                }
            }
        }
    }

    private static class MfConstants
    {
        internal const int MF_VERSION = 0x00020070;
        internal const int MF_SOURCE_READER_FIRST_VIDEO_STREAM = unchecked((int)0xFFFFFFFC);
        internal const int MF_SOURCE_READERF_ENDOFSTREAM = 0x00000002;
    }

    private static class MfHResults
    {
        internal const int MF_E_NO_MORE_TYPES = unchecked((int)0xC00D36B9);
        internal const int MF_E_ATTRIBUTENOTFOUND = unchecked((int)0xC00D36E6);
        internal const int MF_E_INVALIDREQUEST = unchecked((int)0xC00D36B2);
        internal const int MF_E_SHUTDOWN = unchecked((int)0xC00D3E85);
    }

    private static class MfGuids
    {
        internal static Guid MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE = new(
            0xC60AC5FE, 0x252A, 0x478F, 0xA0, 0xEF, 0xBC, 0x8F, 0xA5, 0xF7, 0xCA, 0xD3);
        internal static Guid MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID = new(
            0x8AC3587A, 0x4AE7, 0x42D8, 0x99, 0xE0, 0x0A, 0x60, 0x13, 0xEE, 0xF9, 0x0F);
        internal static Guid MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK = new(
            0x58F0AAD8, 0x22BF, 0x4F8A, 0xBB, 0x3D, 0xD2, 0xC4, 0x97, 0x8C, 0x6E, 0x2F);
        internal static Guid MF_READWRITE_DISABLE_CONVERTERS = new(
            0x98D5B065, 0x1374, 0x4847, 0x8D, 0x5D, 0x31, 0x52, 0x0F, 0xEE, 0x71, 0x56);
        internal static Guid MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS = new(
            0xA634A91C, 0x822B, 0x41B9, 0xA4, 0x94, 0x4D, 0xE4, 0x64, 0x36, 0x12, 0xB0);
        internal static Guid MF_SOURCE_READER_D3D_MANAGER = new(
            0xEC822DA2, 0xE1E9, 0x4B29, 0xA0, 0xD8, 0x56, 0x3C, 0x71, 0x9F, 0x52, 0x69);
        internal static Guid MF_MT_MAJOR_TYPE = new(
            0x48EBA18E, 0xF8C9, 0x4687, 0xBF, 0x11, 0x0A, 0x74, 0xC9, 0xF9, 0x6A, 0x8F);
        internal static Guid MF_MT_SUBTYPE = new(
            0xF7E34C9A, 0x42E8, 0x4714, 0xB7, 0x4B, 0xCB, 0x29, 0xD7, 0x2C, 0x35, 0xE5);
        internal static Guid MF_MT_ALL_SAMPLES_INDEPENDENT = new(
            0xC9173739, 0x5E56, 0x461C, 0xB7, 0x13, 0x46, 0xFB, 0x99, 0x5C, 0xB9, 0x5F);
        internal static Guid MF_MT_FRAME_SIZE = new(
            0x1652C33D, 0xD6B2, 0x4012, 0xB8, 0x34, 0x72, 0x03, 0x08, 0x49, 0xA3, 0x7D);
        internal static Guid MF_MT_FRAME_RATE = new(
            0xC459A2E8, 0x3D2C, 0x4E44, 0xB1, 0x32, 0xFE, 0xE5, 0x15, 0x6C, 0x7B, 0xB0);
        internal static Guid MF_MT_FRAME_RATE_RANGE_MIN = new(
            0xD2E7558C, 0xDC1F, 0x403F, 0x9A, 0x72, 0xD2, 0x8B, 0xB1, 0xEB, 0x3B, 0x5E);
        internal static Guid MF_MT_FRAME_RATE_RANGE_MAX = new(
            0xE3371D41, 0xB4CF, 0x4A05, 0xBD, 0x4E, 0x20, 0xB8, 0x8B, 0xB2, 0xC4, 0xD6);
        internal static Guid MF_MT_PIXEL_ASPECT_RATIO = new(
            0xC6376A1E, 0x8D0A, 0x4027, 0xBE, 0x45, 0x6D, 0x9A, 0x0A, 0xD3, 0x9B, 0xB6);
        internal static Guid MF_MT_INTERLACE_MODE = new(
            0xE2724BB8, 0xE676, 0x4806, 0xB4, 0xB2, 0xA8, 0xD6, 0xEF, 0xB4, 0x4C, 0xCD);
        internal static Guid MFMediaType_Video = new(
            0x73646976, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
        internal static Guid MFVideoFormat_P010 = new(
            0x30313050, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
        internal static Guid MFVideoFormat_NV12 = new(
            0x3231564E, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
        internal static Guid MFVideoFormat_MJPG = new(
            0x47504A4D, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
    }
}

[ComImport]
[Guid("2cd2d921-c447-44a7-a13c-4adabfc247e3")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFAttributes
{
    [PreserveSig]
    int GetItem(ref Guid guidKey, IntPtr pValue);

    [PreserveSig]
    int GetItemType(ref Guid guidKey, out int pType);

    [PreserveSig]
    int CompareItem(ref Guid guidKey, IntPtr value, [MarshalAs(UnmanagedType.Bool)] out bool pbResult);

    [PreserveSig]
    int Compare(IMFAttributes pTheirs, int matchType, [MarshalAs(UnmanagedType.Bool)] out bool pbResult);

    [PreserveSig]
    int GetUINT32(ref Guid guidKey, out int punValue);

    [PreserveSig]
    int GetUINT64(ref Guid guidKey, out ulong punValue);

    [PreserveSig]
    int GetDouble(ref Guid guidKey, out double pfValue);

    [PreserveSig]
    int GetGUID(ref Guid guidKey, out Guid pguidValue);

    [PreserveSig]
    int GetStringLength(ref Guid guidKey, out int pcchLength);

    [PreserveSig]
    int GetString(ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszValue, int cchBufSize, out int pcchLength);

    [PreserveSig]
    int GetAllocatedString(ref Guid guidKey, out IntPtr ppwszValue, out int pcchLength);

    [PreserveSig]
    int GetBlobSize(ref Guid guidKey, out int pcbBlobSize);

    [PreserveSig]
    int GetBlob(ref Guid guidKey, IntPtr pBuf, int cbBufSize, out int pcbBlobSize);

    [PreserveSig]
    int GetAllocatedBlob(ref Guid guidKey, out IntPtr ppBuf, out int pcbSize);

    [PreserveSig]
    int GetUnknown(ref Guid guidKey, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

    [PreserveSig]
    int SetItem(ref Guid guidKey, IntPtr value);

    [PreserveSig]
    int DeleteItem(ref Guid guidKey);

    [PreserveSig]
    int DeleteAllItems();

    [PreserveSig]
    int SetUINT32(ref Guid guidKey, int unValue);

    [PreserveSig]
    int SetUINT64(ref Guid guidKey, ulong unValue);

    [PreserveSig]
    int SetDouble(ref Guid guidKey, double fValue);

    [PreserveSig]
    int SetGUID(ref Guid guidKey, ref Guid guidValue);

    [PreserveSig]
    int SetString(ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] string wszValue);

    [PreserveSig]
    int SetBlob(ref Guid guidKey, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pBuf, int cbBufSize);

    [PreserveSig]
    int SetUnknown(ref Guid guidKey, [MarshalAs(UnmanagedType.IUnknown)] object? pUnknown);

    [PreserveSig]
    int LockStore();

    [PreserveSig]
    int UnlockStore();

    [PreserveSig]
    int GetCount(out int pcItems);

    [PreserveSig]
    int GetItemByIndex(int unIndex, out Guid pguidKey, IntPtr pValue);

    [PreserveSig]
    int CopyAllItems(IMFAttributes pDest);
}

[ComImport]
[Guid("44ae0fa8-ea31-4109-8d2e-4cae4997c555")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFMediaType : IMFAttributes
{
    [PreserveSig]
    int GetMajorType(out Guid pguidMajorType);

    [PreserveSig]
    int IsCompressedFormat([MarshalAs(UnmanagedType.Bool)] out bool pfCompressed);

    [PreserveSig]
    int IsEqual(IMFMediaType pIMediaType, out int pdwFlags);

    [PreserveSig]
    int GetRepresentation(Guid guidRepresentation, out IntPtr ppvRepresentation);

    [PreserveSig]
    int FreeRepresentation(Guid guidRepresentation, IntPtr pvRepresentation);
}

[ComImport]
[Guid("7FEE9E9A-4A89-47A6-899C-B6A53A70FB67")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFActivate : IMFAttributes
{
    [PreserveSig]
    int ActivateObject(ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);

    [PreserveSig]
    int ShutdownObject();

    [PreserveSig]
    int DetachObject();
}

[ComImport]
[Guid("279a808d-aec7-40c8-9c6b-a6b492c78a66")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFMediaSource
{
}

[ComImport]
[Guid("70ae66f2-c809-4e4f-8915-bdcb406b7993")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFSourceReader
{
    [PreserveSig]
    int GetStreamSelection(int dwStreamIndex, [MarshalAs(UnmanagedType.Bool)] out bool pfSelected);

    [PreserveSig]
    int SetStreamSelection(int dwStreamIndex, [MarshalAs(UnmanagedType.Bool)] bool fSelected);

    [PreserveSig]
    int GetNativeMediaType(int dwStreamIndex, int dwMediaTypeIndex, out IMFMediaType? ppMediaType);

    [PreserveSig]
    int GetCurrentMediaType(int dwStreamIndex, out IMFMediaType? ppMediaType);

    [PreserveSig]
    int SetCurrentMediaType(int dwStreamIndex, IntPtr pdwReserved, IMFMediaType pMediaType);

    [PreserveSig]
    int SetCurrentPosition(ref Guid guidTimeFormat, IntPtr varPosition);

    [PreserveSig]
    int ReadSample(
        int dwStreamIndex,
        int dwControlFlags,
        out int pdwActualStreamIndex,
        out int pdwStreamFlags,
        out long pllTimestamp,
        out IMFSample? ppSample);

    [PreserveSig]
    int Flush(int dwStreamIndex);

    [PreserveSig]
    int GetServiceForStream(int dwStreamIndex, ref Guid guidService, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppvObject);

    [PreserveSig]
    int GetPresentationAttribute(int dwStreamIndex, ref Guid guidAttribute, IntPtr pvarAttribute);
}

/// <summary>
/// Flattened IMFSample COM interface — does NOT use C# interface inheritance.
/// .NET COM interop miscalculates vtable slot offsets when using
/// <c>IMFSample : IMFAttributes</c>, causing derived methods to dispatch to
/// wrong vtable entries. This flattened layout explicitly reserves slots 3-32
/// for the 30 inherited IMFAttributes methods, then places the 14 IMFSample
/// methods at the correct slots 33-46.
/// </summary>
[ComImport]
[Guid("c40a00f2-b93a-4d80-ae8c-5a1c634f58e4")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFSample
{
    // ── IMFAttributes vtable slots 3–32 (30 methods) ──
    // These placeholders reserve the correct vtable positions.
    // Never called through this interface — use IMFAttributes directly for attribute access.
    [PreserveSig] int _Attr_GetItem(ref Guid guidKey, IntPtr pValue);
    [PreserveSig] int _Attr_GetItemType(ref Guid guidKey, out int pType);
    [PreserveSig] int _Attr_CompareItem(ref Guid guidKey, IntPtr value, [MarshalAs(UnmanagedType.Bool)] out bool pbResult);
    [PreserveSig] int _Attr_Compare(IMFAttributes pTheirs, int matchType, [MarshalAs(UnmanagedType.Bool)] out bool pbResult);
    [PreserveSig] int _Attr_GetUINT32(ref Guid guidKey, out int punValue);
    [PreserveSig] int _Attr_GetUINT64(ref Guid guidKey, out ulong punValue);
    [PreserveSig] int _Attr_GetDouble(ref Guid guidKey, out double pfValue);
    [PreserveSig] int _Attr_GetGUID(ref Guid guidKey, out Guid pguidValue);
    [PreserveSig] int _Attr_GetStringLength(ref Guid guidKey, out int pcchLength);
    [PreserveSig] int _Attr_GetString(ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszValue, int cchBufSize, out int pcchLength);
    [PreserveSig] int _Attr_GetAllocatedString(ref Guid guidKey, out IntPtr ppwszValue, out int pcchLength);
    [PreserveSig] int _Attr_GetBlobSize(ref Guid guidKey, out int pcbBlobSize);
    [PreserveSig] int _Attr_GetBlob(ref Guid guidKey, IntPtr pBuf, int cbBufSize, out int pcbBlobSize);
    [PreserveSig] int _Attr_GetAllocatedBlob(ref Guid guidKey, out IntPtr ppBuf, out int pcbSize);
    [PreserveSig] int _Attr_GetUnknown(ref Guid guidKey, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
    [PreserveSig] int _Attr_SetItem(ref Guid guidKey, IntPtr value);
    [PreserveSig] int _Attr_DeleteItem(ref Guid guidKey);
    [PreserveSig] int _Attr_DeleteAllItems();
    [PreserveSig] int _Attr_SetUINT32(ref Guid guidKey, int unValue);
    [PreserveSig] int _Attr_SetUINT64(ref Guid guidKey, ulong unValue);
    [PreserveSig] int _Attr_SetDouble(ref Guid guidKey, double fValue);
    [PreserveSig] int _Attr_SetGUID(ref Guid guidKey, ref Guid guidValue);
    [PreserveSig] int _Attr_SetString(ref Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] string wszValue);
    [PreserveSig] int _Attr_SetBlob(ref Guid guidKey, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] byte[] pBuf, int cbBufSize);
    [PreserveSig] int _Attr_SetUnknown(ref Guid guidKey, [MarshalAs(UnmanagedType.IUnknown)] object? pUnknown);
    [PreserveSig] int _Attr_LockStore();
    [PreserveSig] int _Attr_UnlockStore();
    [PreserveSig] int _Attr_GetCount(out int pcItems);
    [PreserveSig] int _Attr_GetItemByIndex(int unIndex, out Guid pguidKey, IntPtr pValue);
    [PreserveSig] int _Attr_CopyAllItems(IMFAttributes pDest);

    // ── IMFSample vtable slots 33–46 (14 methods) ──
    [PreserveSig]
    int GetSampleFlags(out int pdwSampleFlags);

    [PreserveSig]
    int SetSampleFlags(int dwSampleFlags);

    [PreserveSig]
    int GetSampleTime(out long phnsSampleTime);

    [PreserveSig]
    int SetSampleTime(long hnsSampleTime);

    [PreserveSig]
    int GetSampleDuration(out long phnsSampleDuration);

    [PreserveSig]
    int SetSampleDuration(long hnsSampleDuration);

    [PreserveSig]
    int GetBufferCount(out int pdwBufferCount);

    [PreserveSig]
    int GetBufferByIndex(int dwIndex, out IMFMediaBuffer ppBuffer);

    [PreserveSig]
    int ConvertToContiguousBuffer(out IMFMediaBuffer ppBuffer);

    [PreserveSig]
    int AddBuffer(IMFMediaBuffer pBuffer);

    [PreserveSig]
    int RemoveBufferByIndex(int dwIndex);

    [PreserveSig]
    int RemoveAllBuffers();

    [PreserveSig]
    int GetTotalLength(out int pcbTotalLength);

    [PreserveSig]
    int CopyToBuffer(IMFMediaBuffer pBuffer);
}

[ComImport]
[Guid("045FA593-8799-42b8-BC8D-8968C6453507")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFMediaBuffer
{
    [PreserveSig]
    int Lock(out IntPtr ppbBuffer, out int pcbMaxLength, out int pcbCurrentLength);

    [PreserveSig]
    int Unlock();

    [PreserveSig]
    int GetCurrentLength(out int pcbCurrentLength);

    [PreserveSig]
    int SetCurrentLength(int cbCurrentLength);

    [PreserveSig]
    int GetMaxLength(out int pcbMaxLength);
}

[ComImport]
[Guid("7DC9D5F9-9ED9-44EC-9BBF-0600BB589FBB")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMF2DBuffer
{
    [PreserveSig]
    int Lock2D(out IntPtr ppbScanline0, out int plPitch);

    [PreserveSig]
    int Unlock2D();

    [PreserveSig]
    int GetScanline0AndPitch(out IntPtr pbScanline0, out int plPitch);

    [PreserveSig]
    int IsContiguousFormat([MarshalAs(UnmanagedType.Bool)] out bool pfIsContiguous);

    [PreserveSig]
    int GetContiguousLength(out int pcbLength);

    [PreserveSig]
    int ContiguousCopyTo(IntPtr pbDestBuffer, int cbDestBuffer);

    [PreserveSig]
    int ContiguousCopyFrom(IntPtr pbSrcBuffer, int cbSrcBuffer);
}

[ComImport]
[Guid("e7174cfa-1c9e-48b1-8866-626226bfc258")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMFDXGIBuffer
{
    [PreserveSig]
    int GetResource(ref Guid riid, out IntPtr ppvObject);

    [PreserveSig]
    int GetSubresourceIndex(out uint puSubresource);

    [PreserveSig]
    int GetUnknown(ref Guid guid, ref Guid riid, out IntPtr ppvObject);
}
