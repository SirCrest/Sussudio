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
    public delegate void RawFrameCallback(ReadOnlySpan<byte> frameData, int width, int height);

    private readonly object _sync = new();
    private IMFSourceReader? _sourceReader;
    private IMFMediaSource? _mediaSource;
    private CancellationTokenSource? _readCts;
    private Task? _readTask;
    private bool _isInitialized;
    private bool _startupHeld;
    private int _width;
    private int _height;
    private double _fps;
    private string _deviceSymbolicLink = string.Empty;
    private string _negotiatedFormat = "unknown";
    private long _framesDelivered;
    private long _framesDropped;
    private int _vtableDiagDone;

    public long FramesDelivered => Interlocked.Read(ref _framesDelivered);
    public long FramesDropped => Interlocked.Read(ref _framesDropped);
    public string NegotiatedFormat => Volatile.Read(ref _negotiatedFormat);

    public Task InitializeAsync(string deviceSymbolicLink, int width, int height, double fps)
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
        var startupHeld = false;

        try
        {
            MfInterop.AddStartupReference();
            startupHeld = true;

            mediaSource = CreateMediaSource(deviceSymbolicLink);

            ThrowIfFailed(
                MfInterop.MFCreateAttributes(out readerAttributes, 1),
                "MFCreateAttributes(reader)");
            ThrowIfFailed(
                readerAttributes.SetUINT32(ref MfGuids.MF_READWRITE_DISABLE_CONVERTERS, 1),
                "IMFAttributes.SetUINT32(MF_READWRITE_DISABLE_CONVERTERS)");

            ThrowIfFailed(
                MfInterop.MFCreateSourceReaderFromMediaSource(mediaSource, readerAttributes, out sourceReader),
                "MFCreateSourceReaderFromMediaSource");

            selectedMediaType = SelectP010MediaType(
                sourceReader,
                width,
                height,
                fps,
                out var negotiatedWidth,
                out var negotiatedHeight,
                out var negotiatedFps,
                out var negotiatedDescription);

            ThrowIfFailed(
                sourceReader.SetCurrentMediaType(
                    MfConstants.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                    IntPtr.Zero,
                    selectedMediaType),
                "IMFSourceReader.SetCurrentMediaType(P010)");

            _deviceSymbolicLink = deviceSymbolicLink;
            _width = negotiatedWidth;
            _height = negotiatedHeight;
            _fps = negotiatedFps;
            Volatile.Write(ref _negotiatedFormat, negotiatedDescription);
            Interlocked.Exchange(ref _framesDelivered, 0);
            Interlocked.Exchange(ref _framesDropped, 0);

            lock (_sync)
            {
                _mediaSource = mediaSource;
                _sourceReader = sourceReader;
                _startupHeld = startupHeld;
                _isInitialized = true;
                mediaSource = null;
                sourceReader = null;
                startupHeld = false;
            }

            Log(
                "MF_SOURCE_READER_INIT " +
                $"device='{deviceSymbolicLink}' " +
                $"requested={width}x{height}@{fps:0.###} " +
                $"negotiated='{_negotiatedFormat}' " +
                "mf_readwrite_disable_converters=true");
        }
        catch (Exception ex)
        {
            Log(
                "MF_SOURCE_READER_INIT_FAIL " +
                $"device='{deviceSymbolicLink}' " +
                $"requested={width}x{height}@{fps:0.###} " +
                $"type={ex.GetType().Name} msg={ex.Message}");
            throw;
        }
        finally
        {
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
            _readTask = Task.Run(() => ReadLoop(onFrame, readToken), CancellationToken.None);
        }

        Log(
            "MF_SOURCE_READER_START " +
            $"device='{_deviceSymbolicLink}' negotiated='{_negotiatedFormat}'");
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
        }

        if (_startupHeld)
        {
            MfInterop.ReleaseStartupReference();
            _startupHeld = false;
        }
    }

    private void ReadLoop(RawFrameCallback onFrame, CancellationToken ct)
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
                var hr = reader.ReadSample(
                    MfConstants.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                    0,
                    out _,
                    out var flags,
                    out _,
                    out sample);

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

                DeliverFrame(sample, onFrame);
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
            }
            finally
            {
                ReleaseComObject(ref sample);
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

    private unsafe void DeliverFrame(IMFSample sample, RawFrameCallback onFrame)
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

            if (TryDeliverFrameFrom2DBuffer(buffer, onFrame))
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

                var packedFrameBytes = FFmpegEncoderService.GetP010FrameSizeBytes(_width, _height);
                if (packedFrameBytes <= 0)
                {
                    throw new InvalidOperationException("Invalid P010 frame dimensions.");
                }

                if (curLen < packedFrameBytes)
                {
                    throw new InvalidOperationException(
                        $"Media buffer length ({curLen}) is smaller than expected P010 frame size ({packedFrameBytes}).");
                }

                var expectedStride = _width * 2;
                var inferredStride = InferPackedStride(curLen, _height);
                if (inferredStride > expectedStride)
                {
                    var packed = ArrayPool<byte>.Shared.Rent(packedFrameBytes);
                    try
                    {
                        var packedSpan = packed.AsSpan(0, packedFrameBytes);
                        CopyP010WithStride((byte*)dataPtr, inferredStride, packedSpan, _width, _height);
                        onFrame(packedSpan, _width, _height);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(packed);
                    }
                }
                else
                {
                    onFrame(new ReadOnlySpan<byte>((void*)dataPtr, packedFrameBytes), _width, _height);
                }
            }
            finally
            {
                _ = buffer.Unlock();
            }
        }
        finally
        {
            ReleaseComObject(ref buffer);
        }
    }

    private unsafe bool TryDeliverFrameFrom2DBuffer(IMFMediaBuffer buffer, RawFrameCallback onFrame)
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

            var packedFrameBytes = FFmpegEncoderService.GetP010FrameSizeBytes(_width, _height);
            if (packedFrameBytes <= 0)
            {
                throw new InvalidOperationException("Invalid P010 frame dimensions.");
            }

            var expectedStride = _width * 2;
            if (pitch == expectedStride)
            {
                onFrame(new ReadOnlySpan<byte>((void*)scanlinePtr, packedFrameBytes), _width, _height);
                return true;
            }

            var packed = ArrayPool<byte>.Shared.Rent(packedFrameBytes);
            try
            {
                var packedSpan = packed.AsSpan(0, packedFrameBytes);
                CopyP010WithStride((byte*)scanlinePtr, pitch, packedSpan, _width, _height);
                onFrame(packedSpan, _width, _height);
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

    private IMFMediaType SelectP010MediaType(
        IMFSourceReader reader,
        int requestedWidth,
        int requestedHeight,
        double requestedFps,
        out int selectedWidth,
        out int selectedHeight,
        out double selectedFps,
        out string negotiatedDescription)
    {
        IMFMediaType? bestType = null;
        var bestFpsDelta = double.MaxValue;
        selectedWidth = requestedWidth;
        selectedHeight = requestedHeight;
        selectedFps = requestedFps;
        negotiatedDescription = "unknown";

        var totalNativeTypes = 0;
        var p010Count = 0;
        var subtypeSummary = new Dictionary<string, int>();

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

                if (hasSubtype && subtype == MfGuids.MFVideoFormat_P010)
                {
                    p010Count++;
                    Log($"MF_SOURCE_READER_NATIVE_P010 index={index} {nWidth}x{nHeight}@{nFps:0.###}");
                }

                if (!hasSubtype || subtype != MfGuids.MFVideoFormat_P010)
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
                    negotiatedDescription = frameRate > 0
                        ? $"P010 {width}x{height}@{frameRate:0.###}"
                        : $"P010 {width}x{height}";
                }
            }
            finally
            {
                ReleaseComObject(ref nativeType);
            }
        }

        var subtypeList = string.Join(", ", subtypeSummary.Select(kv => $"{kv.Key}={kv.Value}"));
        Log($"MF_SOURCE_READER_NATIVE_TYPES total={totalNativeTypes} p010={p010Count} subtypes=[{subtypeList}]");

        if (bestType == null)
        {
            throw new InvalidOperationException(
                $"No P010 media type was found for {requestedWidth}x{requestedHeight}@{requestedFps:0.###}. " +
                $"Source reader has {totalNativeTypes} native types ({p010Count} P010). Subtypes: [{subtypeList}]");
        }

        if (bestFpsDelta > 0.5)
        {
            ReleaseComObject(ref bestType);
            throw new InvalidOperationException(
                $"No P010 media type matched requested frame rate {requestedFps:0.###}fps " +
                $"for {requestedWidth}x{requestedHeight}.");
        }

        return bestType;
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
        }

        ReleaseComObject(ref sourceReader);
        ReleaseComObject(ref mediaSource);
    }

    private static string SubtypeGuidToName(Guid subtype)
    {
        if (subtype == MfGuids.MFVideoFormat_P010) return "P010";
        if (subtype == new Guid(0x3231564E, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71)) return "NV12";
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
        internal static Guid MF_MT_SUBTYPE = new(
            0xF7E34C9A, 0x42E8, 0x4714, 0xB7, 0x4B, 0xCB, 0x29, 0xD7, 0x2C, 0x35, 0xE5);
        internal static Guid MF_MT_FRAME_SIZE = new(
            0x1652C33D, 0xD6B2, 0x4012, 0xB8, 0x34, 0x72, 0x03, 0x08, 0x49, 0xA3, 0x7D);
        internal static Guid MF_MT_FRAME_RATE = new(
            0xC459A2E8, 0x3D2C, 0x4E44, 0xB1, 0x32, 0xFE, 0xE5, 0x15, 0x6C, 0x7B, 0xB0);
        internal static Guid MFVideoFormat_P010 = new(
            0x30313050, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71);
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
