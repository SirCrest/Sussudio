using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Contracts;
using Sussudio.Services.Runtime;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer : IPreviewFrameSink, IPreviewFrameQueueControl, IPreviewDisplayClock, IDisposable
{
    private const string RendererModeNone = "None";
    private const string RendererModeVideoProcessor = "D3D11VideoProcessor";
    private const string RendererModeHdrPassthrough = "HdrPassthrough";

    [ComImport]
    [Guid("63aad0b8-7c24-40ff-85a8-640d944cc325")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISwapChainPanelNative
    {
        void SetSwapChain(IntPtr swapChain);
    }

    [ComImport]
    [Guid("8BA5FB08-5195-40e2-AC58-0D989C3A0102")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ID3DBlob
    {
        [PreserveSig]
        IntPtr GetBufferPointer();
        [PreserveSig]
        IntPtr GetBufferSize();
    }

    [DllImport("d3dcompiler_47.dll", EntryPoint = "D3DCompile", CallingConvention = CallingConvention.StdCall)]
    private static extern int D3DCompileNative(
        byte[] srcData,
        IntPtr srcDataSize,
        [MarshalAs(UnmanagedType.LPStr)] string? sourceName,
        IntPtr defines,
        IntPtr include,
        [MarshalAs(UnmanagedType.LPStr)] string entryPoint,
        [MarshalAs(UnmanagedType.LPStr)] string target,
        uint flags1,
        uint flags2,
        out IntPtr code,
        out IntPtr errorMsgs);

    [DllImport("dwmapi.dll", ExactSpelling = true)]
    private static extern int DwmFlush();

    private readonly SwapChainPanel _panel;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ManualResetEventSlim _frameReadyEvent = new(false);
    private readonly object _lifecycleLock = new();
    // Best measured 4K120 MJPG cadence on the SwapChainPanel path uses DWM-paced
    // Present(1) with a shallow compositor queue. The env overrides remain for A/B
    // runs on other machines or display modes.
    private readonly int _presentSyncInterval = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_PRESENT_SYNC_INTERVAL", 1, 0, 1);
    private readonly int _dxgiMaxFrameLatency = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_DXGI_MAX_FRAME_LATENCY", 1, 1, 3);
    private readonly int _swapChainBufferCount = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_SWAPCHAIN_BUFFER_COUNT", 2, 2, 4);
    private readonly int _maxPendingFrames = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_RENDER_QUEUE_DEPTH", 4, 1, 8);
    private readonly bool _waitableSwapChainEnabled = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_WAITABLE_SWAPCHAIN", 0, 0, 1) != 0;
    private readonly bool _dxgiFrameStatisticsEnabled = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_DXGI_FRAME_STATS", 1, 0, 1) != 0;
    private readonly int _dxgiFrameStatisticsSampleIntervalFrames = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_DXGI_FRAME_STATS_SAMPLE_INTERVAL", 2, 1, 120);
    private readonly bool _dxgiFrameStatisticsDwmFlushEnabled = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_DXGI_FRAME_STATS_DWM_FLUSH", 0, 0, 1) != 0;
    private readonly double _slowFrameDiagnosticThresholdMs = EnvironmentHelpers.GetDoubleFromEnv("SUSSUDIO_PREVIEW_SLOW_FRAME_THRESHOLD_MS", 0, 0, 1000);
    private readonly bool _mediaPresentDurationEnabled = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_MEDIA_PRESENT_DURATION", 0, 0, 1) != 0;
    private readonly string _renderMmcssTask = Environment.GetEnvironmentVariable("SUSSUDIO_PREVIEW_RENDER_MMCSS_TASK") ?? "Playback";
    private readonly int _renderMmcssPriority = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_RENDER_MMCSS_PRIORITY", 1, -2, 2);
    private readonly int _nativeStopFenceTimeoutMs = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_NATIVE_STOP_FENCE_TIMEOUT_MS", 1000, 100, 10000);
    private readonly int _renderThreadStopTimeoutMs = EnvironmentHelpers.GetIntFromEnv("SUSSUDIO_PREVIEW_RENDER_THREAD_STOP_TIMEOUT_MS", 3000, 500, 30000);

    private Thread? _renderThread;
    private readonly ConcurrentQueue<PendingFrame> _pendingFrames = new();
    private int _pendingFrameCount;
    private int _swapChainBound; // 0=unbound, 1=bound; use Interlocked.CompareExchange to claim unbind

    private int _disposed;
    private int _stopRequested;
    private int _isRendering;
    private int _inNativeCall; // 1 while render thread is between guard-check and Present return
    private int _compositionTransformDirty;
    private int _firstFrameRaised;

    private int _panelPixelWidth;
    private int _panelPixelHeight;
    private double _panelLogicalWidth = 1.0;
    private double _panelLogicalHeight = 1.0;
    private double _rasterizationScale = 1.0;
    private int _startupWidth;
    private int _startupHeight;
    private double _startupFps = 60.0;

    private int _naturalWidth;
    private int _naturalHeight;

    private long _framesSubmitted;
    private long _framesRendered;
    private long _framesDropped;
    private const int CadenceWindowSeconds = 20;
    private readonly object _presentCadenceLock = new();
    private double[] _presentIntervalWindowMs = new double[1200];
    private int _presentIntervalCount;
    private int _presentIntervalIndex;
    private long _lastPresentTick;
    private int _presentCadenceBaselinePending;
    private readonly object _pipelineLatencyLock = new();
    private double[] _pipelineLatencyWindowMs = new double[1200];
    private int _pipelineLatencyCount;
    private int _pipelineLatencyIndex;
    private readonly object _renderCpuTimingLock = new();
    private double[] _inputUploadCpuTimingWindowMs = new double[1200];
    private double[] _renderSubmitCpuTimingWindowMs = new double[1200];
    private double[] _presentCallTimingWindowMs = new double[1200];
    private double[] _renderTotalCpuTimingWindowMs = new double[1200];
    private int _renderCpuTimingCount;
    private int _renderCpuTimingIndex;
    private readonly object _frameLatencyWaitTimingLock = new();
    private double[] _frameLatencyWaitTimingWindowMs = new double[1200];
    private int _frameLatencyWaitTimingCount;
    private int _frameLatencyWaitTimingIndex;
    private long _frameLatencyWaitCallCount;
    private long _frameLatencyWaitSignaledCount;
    private long _frameLatencyWaitTimeoutCount;
    private long _frameLatencyWaitUnexpectedResultCount;
    private long _frameLatencyWaitLastResult;
    private long _frameLatencyWaitLastTicks;
    private readonly object _slowFrameDiagnosticsLock = new();
    private readonly PreviewSlowFrameDiagnostic[] _slowFrameDiagnostics = new PreviewSlowFrameDiagnostic[64];
    private int _slowFrameDiagnosticsCount;
    private int _slowFrameDiagnosticsIndex;
    private readonly object _dxgiFrameStatisticsLock = new();
    private long _dxgiFrameStatisticsSampleCount;
    private long _dxgiFrameStatisticsSuccessCount;
    private long _dxgiFrameStatisticsFailureCount;
    private string _dxgiFrameStatisticsLastError = string.Empty;
    private long _dxgiFrameStatisticsPresentCount = -1;
    private long _dxgiFrameStatisticsPresentRefreshCount = -1;
    private long _dxgiFrameStatisticsSyncRefreshCount = -1;
    private long _dxgiFrameStatisticsSyncQpcTime;
    private long _dxgiFrameStatisticsLastPresentDelta;
    private long _dxgiFrameStatisticsLastPresentRefreshDelta;
    private long _dxgiFrameStatisticsLastSyncRefreshDelta;
    private long _dxgiFrameStatisticsMissedRefreshCount;
    private long _dxgiFrameStatisticsFrameCounter;
    private long _dxgiFrameStatisticsLastSampleFrameCounter;
    private bool _dxgiFrameStatisticsHasBaseline;
    private long _lastSubmittedPreviewPresentId;
    private long _lastSubmittedSourceSequenceNumber = -1;
    private long _lastSubmittedSourcePtsTicks;
    private long _lastSubmittedQpc;
    private long _lastSubmittedUtcUnixMs;
    private long _lastRenderedPreviewPresentId;
    private long _lastRenderedSourceSequenceNumber = -1;
    private long _lastRenderedSourcePtsTicks;
    private long _lastRenderedQpc;
    private long _lastRenderedUtcUnixMs;
    private long _lastRenderedSchedulerToPresentTicks;
    private long _lastRenderedPipelineLatencyTicks;
    private long _lastDroppedPreviewPresentId;
    private long _lastDroppedSourceSequenceNumber = -1;
    private long _lastDroppedSourcePtsTicks;
    private long _lastDroppedQpc;
    private long _lastDroppedUtcUnixMs;
    private long _submissionGeneration;
    private string _lastDropReason = string.Empty;
    private string _submissionGenerationDropReason = "transition";


    private string _inputColorSpaceLabel = "Unknown";
    private string _outputColorSpaceLabel = "Unknown";
    private string _rendererMode = RendererModeNone;
    private string _lastRenderThreadFailureType = string.Empty;
    private string _lastRenderThreadFailureMessage = string.Empty;
    private int _lastRenderThreadFailureHResult;
    private long _renderThreadFailureCount;

    private ID3D11Device? _device;
    private ID3D11DeviceContext? _deviceContext;
    private ID3D11Device3? _device3;
    private ID3D11Multithread? _multithread;
    private ID3D11VideoDevice? _videoDevice;
    private ID3D11VideoContext? _videoContext;
    private ID3D11VideoContext1? _videoContext1;
    private IDXGIFactory2? _factory;
    private IDXGISwapChain1? _swapChain;
    private IDXGISwapChain2? _swapChain2;
    private IDXGISwapChain3? _swapChain3;
    private IntPtr _frameLatencyWaitHandle;
    private long _swapChainAddress;
    private ID3D11Texture2D? _swapChainBackBuffer;
    private ID3D11RenderTargetView? _swapChainRTV;
    private ID3D11Texture2D? _inputTexture;
    private ID3D11Texture2D? _stagingTexture;
    private ID3D11VideoProcessorEnumerator? _videoProcessorEnumerator;
    private ID3D11VideoProcessor? _videoProcessor;
    private ID3D11VideoProcessorInputView? _inputView;
    private ID3D11VideoProcessorOutputView? _outputView;
    private ID3D11Texture2D? _hdrInputTexture;
    private ID3D11Texture2D? _hdrStagingTexture;
    private ID3D11ShaderResourceView? _hdrYPlaneSRV;
    private ID3D11ShaderResourceView? _hdrUVPlaneSRV;
    private ID3D11VertexShader? _fullscreenVS;
    private ID3D11PixelShader? _nv12PS;
    private ID3D11ShaderResourceView? _nv12YSRV;
    private ID3D11ShaderResourceView? _nv12UVSRV;
    private IntPtr _nv12LastYPtr;
    private IntPtr _nv12LastUVPtr;
    private ID3D11PixelShader? _hdrTonemapPS;
    private ID3D11PixelShader? _hdrPassthroughPS;
    private ID3D11SamplerState? _linearSampler;
    private ID3D11Buffer? _viewportCB;
    private int _hdrInputConfiguredWidth;
    private int _hdrInputConfiguredHeight;
    private bool _hdrPlaneViewsUnavailable;
    private ID3D11Device? _sharedDevice;

    // Pre-allocated arrays to avoid per-frame GC pressure (720+ allocs/s at 120fps)
    private readonly VideoProcessorStream[] _vpStreamArray = new VideoProcessorStream[1];
    private readonly ID3D11RenderTargetView[] _rtvArray = new ID3D11RenderTargetView[1];
    private readonly Viewport[] _viewportArray = new Viewport[1];
    private readonly ID3D11SamplerState[] _samplerArray = new ID3D11SamplerState[1];
    private readonly ID3D11ShaderResourceView[] _srvArray2 = new ID3D11ShaderResourceView[2];
    private readonly ID3D11ShaderResourceView[] _srvNullArray2 = { null!, null! };
    private readonly ID3D11Buffer[] _cbArray = new ID3D11Buffer[1];


    private int _configuredInputWidth;
    private int _configuredInputHeight;
    private int _configuredOutputWidth;
    private int _configuredOutputHeight;
    private Format _configuredInputFormat = Format.Unknown;
    private bool _configuredHdr;
    private bool _fullRangeInput;
    private bool _hdrCapableSwapChain;
    private uint _outputFrameIndex;
    private int _hdrPassthroughEnabled;
    private int _swapChainColorSpaceDirty;
    private int _sharedDeviceResetPending;
    private int _sharedDeviceActive;
    private bool _loggedNv12ShaderMissing;
    private bool _loggedDirectUploadFallback;
    private bool _loggedHdrShaderFallback;
    private int _lastNv12IsHdr = -1; // tri-state: -1 = unset, 0 = SDR, 1 = HDR

    public D3D11PreviewRenderer(SwapChainPanel panel, DispatcherQueue dispatcherQueue)
    {
        _panel = panel ?? throw new ArgumentNullException(nameof(panel));
        _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
        _panelPixelWidth = 1;
        _panelPixelHeight = 1;
    }

    public event Action? FirstFrameRendered;
    public event Action<string>? RenderThreadFailed;

    public long FramesSubmitted => Interlocked.Read(ref _framesSubmitted);
    public long FramesRendered => Interlocked.Read(ref _framesRendered);
    public long FramesDropped => Interlocked.Read(ref _framesDropped);
    public long RenderThreadFailureCount => Interlocked.Read(ref _renderThreadFailureCount);
    public int PendingFrameCount => Math.Max(0, Volatile.Read(ref _pendingFrameCount));
    public bool IsRendering => Volatile.Read(ref _isRendering) != 0;
    public bool IsHdrCapableSwapChain => _hdrCapableSwapChain;
    public string RendererMode => Volatile.Read(ref _rendererMode);
    public string LastRenderThreadFailureType => Volatile.Read(ref _lastRenderThreadFailureType);
    public string LastRenderThreadFailureMessage => Volatile.Read(ref _lastRenderThreadFailureMessage);
    public int LastRenderThreadFailureHResult => Volatile.Read(ref _lastRenderThreadFailureHResult);
    public string InputColorSpaceLabel => _inputColorSpaceLabel;
    public string OutputColorSpaceLabel => _outputColorSpaceLabel;
    public int PresentSyncInterval => _presentSyncInterval;
    public int DxgiMaxFrameLatency => _dxgiMaxFrameLatency;
    public int SwapChainBufferCount => _swapChainBufferCount;
    public bool WaitableSwapChainEnabled => _waitableSwapChainEnabled;
    public int NaturalWidth => Volatile.Read(ref _naturalWidth);
    public int NaturalHeight => Volatile.Read(ref _naturalHeight);
    public string SwapChainAddress
    {
        get
        {
            var address = Interlocked.Read(ref _swapChainAddress);
            return address == 0 ? string.Empty : $"0x{address:X}";
        }
    }

    public void SetHdrPassthroughEnabled(bool enabled)
    {
        Interlocked.Exchange(ref _hdrPassthroughEnabled, enabled ? 1 : 0);
        Interlocked.Exchange(ref _swapChainColorSpaceDirty, 1);
        SignalFrameReady("hdr_passthrough");
    }

    public void SetSharedDevice(ID3D11Device sharedDevice)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(sharedDevice);
        if (sharedDevice.NativePointer == IntPtr.Zero)
        {
            throw new ArgumentException("Shared D3D11 device pointer is null.", nameof(sharedDevice));
        }

        ID3D11Device? previous;
        lock (_lifecycleLock)
        {
            Marshal.AddRef(sharedDevice.NativePointer);
            previous = _sharedDevice;
            _sharedDevice = new ID3D11Device(sharedDevice.NativePointer);
        }

        previous?.Dispose();
        Interlocked.Exchange(ref _sharedDeviceActive, 0);

        // The render thread flips _isRendering before its first InitializeD3D().
        // If the capture service applies the shared device in that startup
        // window, the initial InitializeD3D() will already consume _sharedDevice;
        // queuing a reset would immediately unbind/dispose/recreate the freshly
        // bound swap chain. Only reset once D3D resources actually exist.
        if (Volatile.Read(ref _isRendering) != 0 &&
            (_device != null || _swapChain != null))
        {
            Interlocked.Exchange(ref _sharedDeviceResetPending, 1);
            SignalFrameReady("shared_device_reset");
        }
    }

    public void RetireSharedDeviceReferenceForReinit()
    {
        // Mode reinit retires this renderer after Stop() has already released
        // the render-thread resources. The remaining shared-device wrapper is a
        // duplicate COM reference obtained from the capture backend's
        // SharedD3DDeviceManager. Disposing that wrapper while the old capture
        // pipeline is also disposing its manager has produced corrupted-state
        // AccessViolationException crashes in SharpGen/Vortice. Abandon the
        // duplicate reference for this rare mode-switch path; the active
        // renderer gets a fresh shared device from the new capture pipeline.
        _sharedDevice = null;
        Interlocked.Exchange(ref _sharedDeviceActive, 0);
        Interlocked.Exchange(ref _sharedDeviceResetPending, 0);
    }

    public void Start(int width, int height, double fps, bool isHdr)
    {
        ThrowIfDisposed();
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (fps <= 0) throw new ArgumentOutOfRangeException(nameof(fps));

        Stop();

        lock (_lifecycleLock)
        {
            _startupWidth = width;
            _startupHeight = height;
            _startupFps = fps;

            Volatile.Write(ref _naturalWidth, width);
            Volatile.Write(ref _naturalHeight, height);
            if (Volatile.Read(ref _panelPixelWidth) <= 0) Volatile.Write(ref _panelPixelWidth, width);
            if (Volatile.Read(ref _panelPixelHeight) <= 0) Volatile.Write(ref _panelPixelHeight, height);

            _configuredInputWidth = 0;
            _configuredInputHeight = 0;
            _configuredOutputWidth = 0;
            _configuredOutputHeight = 0;
            _configuredInputFormat = Format.Unknown;
            _configuredHdr = isHdr;
            _outputFrameIndex = 0;
            Volatile.Write(ref _rendererMode, RendererModeNone);

            Interlocked.Exchange(ref _stopRequested, 0);
            Interlocked.Exchange(ref _compositionTransformDirty, 1);
            Interlocked.Exchange(ref _firstFrameRaised, 0);
            Interlocked.Exchange(ref _sharedDeviceResetPending, 0);
            ResetFrameReady("start");

            _renderThread = new Thread(RenderThreadMain)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
                Name = "D3D11PreviewRenderer"
            };
            _renderThread.Start();
        }

        Logger.Log($"D3D11 preview renderer start width={width} height={height} fps={fps:0.###} hdr={isHdr}.");
    }

    /// <summary>
    /// Stops the render thread before capture pipeline teardown.
    /// </summary>
    public void StopRenderThread()
    {
        // Reinit has two native lifetime constraints that have to be ordered:
        // stop rendering before UnifiedVideoCapture tears down the shared D3D
        // device, and detach SwapChainPanel while the old swap chain is still
        // alive. HDR<->SDR or resolution changes can alter swap-chain format
        // and size; replacing a still-attached stale chain later lets WinUI call
        // through a released native reference during SetSwapChain(newPtr).
        Stop();
        Logger.Log("D3D11 preview renderer render thread stopped for reinit.");
    }

    public void Stop()
    {
        Thread? renderThread;
        lock (_lifecycleLock)
        {
            renderThread = _renderThread;
            if (renderThread == null)
            {
                FailPendingFrameCapture("Preview renderer is not running.");
                Volatile.Write(ref _rendererMode, RendererModeNone);
                ResetPresentCadence();
                return;
            }
            Interlocked.Exchange(ref _stopRequested, 1);
        }

        // Wait for any in-flight native render call (VideoProcessorBlt / Present)
        // to complete before we unbind the swap chain. The render thread sets
        // _inNativeCall=1 before entering the native call block and clears it after.
        // Without this gate, the CAS unbind below can yank the swap chain while
        // the render thread is inside a native D3D call, causing an unrecoverable
        // AccessViolationException (.NET 8 cannot catch corrupted-state exceptions).
        WaitForNativeCallToDrainOrThrow("stop");

        // Unbind swap chain from panel BEFORE joining the render thread.
        // The render thread releases the swap chain and D3D device during cleanup.
        // If we unbind after that, the panel holds a stale DXGI reference and
        // SetSwapChain (either null or new chain) hits an AccessViolationException
        // — a corrupted-state exception .NET Core cannot catch.
        // Unbinding first, while D3D resources are still alive, avoids this.
        //
        // CAS(1->0) ensures exactly one thread performs the unbind. The render
        // thread's CleanupD3DResources also CAS's this flag before disposing the
        // swap chain — whoever loses the race skips the native call entirely.
        if (Interlocked.CompareExchange(ref _swapChainBound, 0, 1) == 1)
        {
            Interlocked.Exchange(ref _swapChainAddress, 0);
            try
            {
                if (_dispatcherQueue.HasThreadAccess)
                {
                    if (_panel?.XamlRoot != null)
                    {
                        var panelNative = WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel);
                        panelNative.SetSwapChain(IntPtr.Zero);
                    }
                }
                else
                {
                    UnbindSwapChainFromPanel();
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"D3D11 preview swap chain unbind failed: {ex.GetType().Name} msg={ex.Message}");
            }
        }

        // Wake the render thread AFTER the swap chain is safely unbound so it
        // sees _stopRequested and exits without attempting to Present.
        SignalFrameReady("stop");
        if (Thread.CurrentThread != renderThread)
        {
            if (!renderThread.Join(TimeSpan.FromMilliseconds(_renderThreadStopTimeoutMs)))
            {
                Logger.Log($"D3D11 preview renderer stop timed out after {_renderThreadStopTimeoutMs}ms; leaving renderer owned by the stop path to avoid blocking UI indefinitely.");
                throw new TimeoutException("D3D11 preview render thread did not stop before timeout.");
            }
        }

        lock (_lifecycleLock)
        {
            if (ReferenceEquals(_renderThread, renderThread))
            {
                _renderThread = null;
            }
        }

        while (TryDequeuePendingFrame(out var stale))
        {
            TrackFrameDropped(stale, "renderer-stop");
            stale.Dispose();
        }

        FailPendingFrameCapture("Preview renderer stopped before frame capture completed.");
        Volatile.Write(ref _rendererMode, RendererModeNone);
        ResetPresentCadence();
        Logger.Log("D3D11 preview renderer stop completed.");
    }

    private void WaitForNativeCallToDrainOrThrow(string operation)
    {
        if (Volatile.Read(ref _inNativeCall) == 0)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var spinner = new SpinWait();
        while (Volatile.Read(ref _inNativeCall) != 0)
        {
            if (stopwatch.ElapsedMilliseconds >= _nativeStopFenceTimeoutMs)
            {
                Logger.Log($"D3D11 preview renderer {operation} timed out waiting for native render call to return after {_nativeStopFenceTimeoutMs}ms.");
                throw new TimeoutException("D3D11 preview native render call did not return before timeout.");
            }

            spinner.SpinOnce();
            if (spinner.NextSpinWillYield)
            {
                Thread.Sleep(1);
            }
        }
    }

    public void OnPanelSizeChanged(double logicalWidth, double logicalHeight, double rasterizationScale)
    {
        if (logicalWidth <= 0 || logicalHeight <= 0 || rasterizationScale <= 0) return;

        Volatile.Write(ref _panelLogicalWidth, logicalWidth);
        Volatile.Write(ref _panelLogicalHeight, logicalHeight);
        var pixelWidth = Math.Max(1, (int)(logicalWidth * rasterizationScale));
        var pixelHeight = Math.Max(1, (int)(logicalHeight * rasterizationScale));

        Volatile.Write(ref _panelPixelWidth, pixelWidth);
        Volatile.Write(ref _panelPixelHeight, pixelHeight);
        Volatile.Write(ref _rasterizationScale, rasterizationScale);
        Interlocked.Exchange(ref _compositionTransformDirty, 1);
        SignalFrameReady("panel_size_changed");
        Logger.Log($"D3D11 preview resize requested width={pixelWidth} height={pixelHeight} scale={rasterizationScale}.");
    }

    /// <summary>
    /// Set to true when the input NV12 data uses full range (0-255), e.g. from MJPEG/NVDEC decode.
    /// Must be set before frames are submitted. Affects VP input color space.
    /// </summary>
    public bool FullRangeInput
    {
        get => Volatile.Read(ref _fullRangeInput);
        set => Volatile.Write(ref _fullRangeInput, value);
    }

    public void SubmitRawFrame(
        IntPtr data,
        int dataLength,
        int width,
        int height,
        bool isHdr,
        PreviewFrameTracking tracking)
    {
        if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _stopRequested) != 0)
        {
            return;
        }

        if (data == IntPtr.Zero || dataLength <= 0 || width <= 0 || height <= 0)
        {
            return;
        }

        var copied = ArrayPool<byte>.Shared.Rent(dataLength);
        try
        {
            Marshal.Copy(data, copied, 0, dataLength);
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(copied);
            throw;
        }

        var frame = new PendingFrame(
            null,
            0,
            copied,
            dataLength,
            width,
            height,
            isHdr,
            tracking.ArrivalTick,
            tracking.SourceSequenceNumber,
            tracking.PreviewPresentId,
            tracking.SchedulerSubmitTick,
            tracking.SourcePtsTicks,
            countForPresentCadence: tracking.CountForPresentCadence);
        EnqueuePendingFrame(frame);
    }

    public void SubmitRawFrameLease(
        PooledVideoFrameLease frame,
        bool isHdr,
        PreviewFrameTracking tracking)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _stopRequested) != 0)
        {
            frame.Dispose();
            return;
        }

        if (frame.Length <= 0 || frame.Width <= 0 || frame.Height <= 0)
        {
            frame.Dispose();
            return;
        }

        EnqueuePendingFrame(new PendingFrame(
            null,
            0,
            null,
            frame.Length,
            frame.Width,
            frame.Height,
            isHdr,
            frame.ArrivalTick,
            frame.SequenceNumber,
            tracking.PreviewPresentId,
            tracking.SchedulerSubmitTick,
            sourcePtsTicks: 0,
            frameLease: frame,
            countForPresentCadence: tracking.CountForPresentCadence));
    }

    public void SubmitTexture(
        IntPtr d3dTexture,
        int subresourceIndex,
        int width,
        int height,
        bool isHdr,
        PreviewFrameTracking tracking)
    {
        if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _stopRequested) != 0)
        {
            return;
        }

        if (d3dTexture == IntPtr.Zero || width <= 0 || height <= 0)
        {
            return;
        }

        if (Volatile.Read(ref _sharedDeviceActive) == 0)
        {
            throw new InvalidOperationException("Shared D3D11 device is not active for texture submission.");
        }

        IntPtr ownedTexturePtr = IntPtr.Zero;
        ID3D11Texture2D? texture = null;
        try
        {
            Marshal.AddRef(d3dTexture);
            ownedTexturePtr = d3dTexture;
            texture = new ID3D11Texture2D(ownedTexturePtr);
        }
        catch
        {
            texture?.Dispose();
            if (texture == null && ownedTexturePtr != IntPtr.Zero)
            {
                Marshal.Release(ownedTexturePtr);
            }

            throw;
        }

        var frame = new PendingFrame(
            texture,
            subresourceIndex,
            null,
            0,
            width,
            height,
            isHdr,
            tracking.ArrivalTick,
            tracking.SourceSequenceNumber,
            tracking.PreviewPresentId,
            tracking.SchedulerSubmitTick,
            tracking.SourcePtsTicks,
            countForPresentCadence: tracking.CountForPresentCadence);
        EnqueuePendingFrame(frame);
    }

    public void SubmitNv12PlaneTextures(
        IntPtr yTexturePtr,
        IntPtr uvTexturePtr,
        int width,
        int height,
        bool isHdr,
        PreviewFrameTracking tracking)
    {
        if (Volatile.Read(ref _disposed) != 0 || Volatile.Read(ref _stopRequested) != 0)
        {
            return;
        }

        if (yTexturePtr == IntPtr.Zero || uvTexturePtr == IntPtr.Zero || width <= 0 || height <= 0)
        {
            return;
        }

        if (_nv12PS == null)
        {
            if (!_loggedNv12ShaderMissing)
            {
                Logger.Log("D3D11_RENDERER_WARN NV12 pixel shader not available — frames will be dropped via this path");
                _loggedNv12ShaderMissing = true;
            }
            return;
        }

        if (Volatile.Read(ref _sharedDeviceActive) == 0)
        {
            throw new InvalidOperationException("Shared D3D11 device is not active for NV12 texture submission.");
        }

        // Log the first frame and any HDR/SDR transition through the NV12 plane path.
        var hdrInt = isHdr ? 1 : 0;
        var prev = Interlocked.Exchange(ref _lastNv12IsHdr, hdrInt);
        if (prev != hdrInt)
        {
            var prevLabel = prev == -1 ? "unset" : (prev == 1 ? "HDR" : "SDR");
            var curLabel = isHdr ? "HDR" : "SDR";
            Logger.Log($"D3D11_PREVIEW_NV12_HDR_TRANSITION from={prevLabel} to={curLabel} pathTag=PlaneTextures");
        }

        IntPtr ownedYTexturePtr = IntPtr.Zero;
        IntPtr ownedUvTexturePtr = IntPtr.Zero;
        ID3D11Texture2D? yTexture = null;
        ID3D11Texture2D? uvTexture = null;
        try
        {
            Marshal.AddRef(yTexturePtr);
            ownedYTexturePtr = yTexturePtr;
            yTexture = new ID3D11Texture2D(ownedYTexturePtr);

            Marshal.AddRef(uvTexturePtr);
            ownedUvTexturePtr = uvTexturePtr;
            uvTexture = new ID3D11Texture2D(ownedUvTexturePtr);

            EnqueueNv12Frame(
                ownedYTexturePtr,
                yTexture,
                ownedUvTexturePtr,
                uvTexture,
                width,
                height,
                isHdr,
                tracking);
        }
        catch
        {
            uvTexture?.Dispose();
            if (uvTexture == null && ownedUvTexturePtr != IntPtr.Zero)
            {
                Marshal.Release(ownedUvTexturePtr);
            }

            yTexture?.Dispose();
            if (yTexture == null && ownedYTexturePtr != IntPtr.Zero)
            {
                Marshal.Release(ownedYTexturePtr);
            }

            throw;
        }
    }

    private void EnqueueNv12Frame(
        IntPtr yTexturePtr,
        ID3D11Texture2D yTexture,
        IntPtr uvTexturePtr,
        ID3D11Texture2D uvTexture,
        int width,
        int height,
        bool isHdr,
        PreviewFrameTracking tracking)
    {
        var frame = new PendingFrame(
            d3dTexture: null,
            d3dSubresourceIndex: 0,
            rawData: null,
            rawDataLength: 0,
            width: width,
            height: height,
            isHdr: isHdr,
            arrivalTick: tracking.ArrivalTick,
            sourceSequenceNumber: tracking.SourceSequenceNumber,
            previewPresentId: tracking.PreviewPresentId,
            schedulerSubmitTick: tracking.SchedulerSubmitTick,
            sourcePtsTicks: tracking.SourcePtsTicks,
            d3dTextureY: yTexturePtr,
            d3dTextureUV: uvTexturePtr,
            d3dTextureYObject: yTexture,
            d3dTextureUVObject: uvTexture,
            countForPresentCadence: tracking.CountForPresentCadence);
        EnqueuePendingFrame(frame);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        Stop();
        _sharedDevice?.Dispose();
        _sharedDevice = null;
        _frameReadyEvent.Dispose();
    }


}
