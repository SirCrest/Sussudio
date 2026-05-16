using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Capture;
using Sussudio.Services.Contracts;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer : IPreviewFrameSink, IPreviewFrameQueueControl, IPreviewDisplayClock, IDisposable
{
    private const string RendererModeNone = "None";
    private const string RendererModeVideoProcessor = "D3D11VideoProcessor";
    private const string RendererModeHdrPassthrough = "HdrPassthrough";

    private readonly SwapChainPanel _panel;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ManualResetEventSlim _frameReadyEvent = new(false);
    private readonly object _lifecycleLock = new();

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

}
