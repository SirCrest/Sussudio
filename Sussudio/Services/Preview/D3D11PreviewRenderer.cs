using System;
using System.Threading;
using Sussudio.Services.Capture;
using Sussudio.Services.Contracts;
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
