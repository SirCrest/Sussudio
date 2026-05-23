using System;
using System.Threading;
using Sussudio.Services.Capture;
using Sussudio.Services.Contracts;
using Sussudio.Services.Runtime;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer : IPreviewFrameSink, IPreviewFrameQueueControl, IPreviewDisplayClock, IDisposable
{
    private const string RendererModeNone = "None";
    private const string RendererModeVideoProcessor = "D3D11VideoProcessor";
    private const string RendererModeHdrPassthrough = "HdrPassthrough";

    private readonly SwapChainPanel _panel;
    private readonly DispatcherQueue _dispatcherQueue;

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

    private int _naturalWidth;
    private int _naturalHeight;

    private const int CadenceWindowSeconds = 20;


    private string _inputColorSpaceLabel = "Unknown";
    private string _outputColorSpaceLabel = "Unknown";
    private string _rendererMode = RendererModeNone;

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

    public D3D11PreviewRenderer(SwapChainPanel panel, DispatcherQueue dispatcherQueue)
    {
        _panel = panel ?? throw new ArgumentNullException(nameof(panel));
        _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
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
