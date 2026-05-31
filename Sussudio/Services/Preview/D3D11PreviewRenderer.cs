using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using Sussudio.Services.Capture;
using Sussudio.Services.Contracts;
using Sussudio.Services.Runtime;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal interface IPreviewFrameQueueControl
{
    int DropPendingFrames(string reason);
}

internal sealed partial class D3D11PreviewRenderer : IPreviewFrameSink, IPreviewFrameQueueControl, IPreviewDisplayClock, IDisposable
{
    [ComImport]
    [Guid("63aad0b8-7c24-40ff-85a8-640d944cc325")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISwapChainPanelNative
    {
        void SetSwapChain(IntPtr swapChain);
    }

    private const string RendererModeNone = "None";
    private const string RendererModeVideoProcessor = "D3D11VideoProcessor";
    private const string RendererModeHdrPassthrough = "HdrPassthrough";

    private readonly SwapChainPanel _panel;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly object _lifecycleLock = new();
    private readonly ManualResetEventSlim _frameReadyEvent = new(false);
    private readonly ConcurrentQueue<PendingFrame> _pendingFrames = new();

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
    private Thread? _renderThread;
    private int _disposed;
    private int _isRendering;
    private int _startupWidth;
    private int _startupHeight;
    private double _startupFps = 60.0;

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
    private int _compositionTransformDirty;
    private int _panelPixelWidth = 1;
    private int _panelPixelHeight = 1;
    private double _panelLogicalWidth = 1.0;
    private double _panelLogicalHeight = 1.0;
    private double _rasterizationScale = 1.0;
    private int _swapChainBound; // 0=unbound, 1=bound; use Interlocked.CompareExchange to claim unbind
    private const uint WaitObject0 = 0;
    private const uint WaitTimeout = 258;
    private IntPtr _frameLatencyWaitHandle;
    private int _stopRequested;
    private int _inNativeCall; // 1 while render thread is between guard-check and Present return
    private int _pendingFrameCount;
    private bool _loggedNv12ShaderMissing;
    private int _lastNv12IsHdr = -1; // tri-state: -1 = unset, 0 = SDR, 1 = HDR

    private sealed class PendingFrame : IDisposable
    {
        public PendingFrame(
            ID3D11Texture2D? d3dTexture,
            int d3dSubresourceIndex,
            byte[]? rawData,
            int rawDataLength,
            int width,
            int height,
            bool isHdr,
            long arrivalTick,
            long sourceSequenceNumber = -1,
            long previewPresentId = 0,
            long schedulerSubmitTick = 0,
            long sourcePtsTicks = 0,
            PooledVideoFrameLease? frameLease = null,
            IntPtr d3dTextureY = default,
            IntPtr d3dTextureUV = default,
            ID3D11Texture2D? d3dTextureYObject = null,
            ID3D11Texture2D? d3dTextureUVObject = null,
            bool countForPresentCadence = true)
        {
            D3DTexture = d3dTexture;
            D3DSubresourceIndex = Math.Max(0, d3dSubresourceIndex);
            RawData = rawData;
            RawDataLength = rawDataLength;
            Width = width;
            Height = height;
            IsHdr = isHdr;
            ArrivalTick = arrivalTick;
            SourceSequenceNumber = sourceSequenceNumber;
            PreviewPresentId = previewPresentId;
            SourcePtsTicks = sourcePtsTicks;
            SchedulerSubmitTick = schedulerSubmitTick;
            FrameLease = frameLease;
            D3DTextureY = d3dTextureY;
            D3DTextureUV = d3dTextureUV;
            D3DTextureYObject = d3dTextureYObject;
            D3DTextureUVObject = d3dTextureUVObject;
            CountForPresentCadence = countForPresentCadence;
        }

        public ID3D11Texture2D? D3DTexture { get; private set; }
        public int D3DSubresourceIndex { get; }
        public IntPtr D3DTextureY { get; private set; }
        public IntPtr D3DTextureUV { get; private set; }
        public ID3D11Texture2D? D3DTextureYObject { get; private set; }
        public ID3D11Texture2D? D3DTextureUVObject { get; private set; }
        public byte[]? RawData { get; private set; }
        public int RawDataLength { get; private set; }
        public PooledVideoFrameLease? FrameLease { get; private set; }
        public int Width { get; }
        public int Height { get; }
        public bool IsHdr { get; }
        public long ArrivalTick { get; }
        public long SourceSequenceNumber { get; }
        public long PreviewPresentId { get; }
        public long SourcePtsTicks { get; }
        public long SchedulerSubmitTick { get; }
        public bool CountForPresentCadence { get; }
        public long SubmissionGeneration { get; set; }

        public void Dispose()
        {
            D3DTexture?.Dispose();
            D3DTexture = null;
            if (D3DTextureYObject != null)
            {
                D3DTextureYObject.Dispose();
                D3DTextureYObject = null;
                D3DTextureY = IntPtr.Zero;
            }
            else if (D3DTextureY != IntPtr.Zero)
            {
                Marshal.Release(D3DTextureY);
                D3DTextureY = IntPtr.Zero;
            }

            if (D3DTextureUVObject != null)
            {
                D3DTextureUVObject.Dispose();
                D3DTextureUVObject = null;
                D3DTextureUV = IntPtr.Zero;
            }
            else if (D3DTextureUV != IntPtr.Zero)
            {
                Marshal.Release(D3DTextureUV);
                D3DTextureUV = IntPtr.Zero;
            }

            if (RawData != null)
            {
                ArrayPool<byte>.Shared.Return(RawData);
                RawData = null;
                RawDataLength = 0;
            }

            FrameLease?.Dispose();
            FrameLease = null;
        }
    }

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
                Logger.Log("D3D11_RENDERER_WARN NV12 pixel shader not available - frames will be dropped via this path");
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

    private void EnqueuePendingFrame(PendingFrame frame)
    {
        lock (_lifecycleLock)
        {
            if (Volatile.Read(ref _disposed) != 0 ||
                Volatile.Read(ref _stopRequested) != 0 ||
                _renderThread == null)
            {
                TrackFrameDropped(frame, "renderer-stopped");
                frame.Dispose();
                return;
            }

            frame.SubmissionGeneration = Interlocked.Read(ref _submissionGeneration);
            var pendingFrameCount = Interlocked.Increment(ref _pendingFrameCount);
            _pendingFrames.Enqueue(frame);
            TrackFrameSubmitted(frame);

            // Trim oldest frames if the queue exceeds the elastic limit.
            // Under normal operation the render thread keeps up and the queue
            // stays at 0-1 (no added latency). The extra slots only absorb
            // brief render hiccups instead of dropping frames.
            while (pendingFrameCount > _maxPendingFrames)
            {
                if (TryDequeuePendingFrame(out var oldest))
                {
                    TrackFrameDropped(oldest, "renderer-backlog");
                    oldest.Dispose();
                    pendingFrameCount = PendingFrameCount;
                }
                else
                {
                    Interlocked.Exchange(ref _pendingFrameCount, 0);
                    break;
                }
            }

            Volatile.Write(ref _naturalWidth, frame.Width);
            Volatile.Write(ref _naturalHeight, frame.Height);
            Interlocked.Increment(ref _framesSubmitted);
        }

        SignalFrameReady("pending_frame");
    }

    private void SignalFrameReady(string operation)
    {
        try
        {
            _frameReadyEvent.Set();
        }
        catch (ObjectDisposedException)
        {
            Logger.Log($"D3D11_PREVIEW_FRAME_SIGNAL_SKIPPED op={operation} reason=disposed");
        }
    }

    private void ResetFrameReady(string operation)
    {
        try
        {
            _frameReadyEvent.Reset();
        }
        catch (ObjectDisposedException)
        {
            Logger.Log($"D3D11_PREVIEW_FRAME_RESET_SKIPPED op={operation} reason=disposed");
        }
    }

    private bool TryDequeuePendingFrame(out PendingFrame frame)
    {
        if (_pendingFrames.TryDequeue(out var dequeued))
        {
            frame = dequeued;
            DecrementPendingFrameCount();
            return true;
        }

        frame = null!;
        return false;
    }

    public int DropPendingFrames(string reason)
    {
        var normalizedReason = string.IsNullOrWhiteSpace(reason)
            ? "explicit-drain"
            : reason.Trim();
        var dropped = 0;
        Volatile.Write(ref _submissionGenerationDropReason, normalizedReason);
        Interlocked.Increment(ref _submissionGeneration);
        lock (_lifecycleLock)
        {
            while (TryDequeuePendingFrame(out var stale))
            {
                TrackFrameDropped(stale, normalizedReason);
                stale.Dispose();
                dropped++;
            }
        }

        if (dropped > 0)
        {
            Logger.Log($"D3D11_PREVIEW_PENDING_DRAIN reason={normalizedReason} dropped={dropped}");
            if (_pendingFrames.IsEmpty &&
                Volatile.Read(ref _compositionTransformDirty) == 0 &&
                Volatile.Read(ref _sharedDeviceResetPending) == 0)
            {
                ResetFrameReady("pending_drain");
            }
        }

        return dropped;
    }

    private void DecrementPendingFrameCount()
    {
        while (true)
        {
            var current = Volatile.Read(ref _pendingFrameCount);
            if (current <= 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref _pendingFrameCount, current - 1, current) == current)
            {
                return;
            }
        }
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
            ResetFirstFrameNotification();
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

    private void RenderThreadMain()
    {
        Interlocked.Exchange(ref _isRendering, 1);
        using var mmcss = MmcssThreadRegistration.TryRegister(_renderMmcssTask, _renderMmcssPriority, message => Logger.Log(message));
        try
        {
            InitializeD3D();
            while (Volatile.Read(ref _stopRequested) == 0)
            {
                _frameReadyEvent.Wait(TimeSpan.FromMilliseconds(200));
                if (Volatile.Read(ref _stopRequested) != 0) break;

                if (Interlocked.CompareExchange(ref _sharedDeviceResetPending, 0, 1) == 1)
                {
                    HandlePendingSharedDeviceResetOnRenderThread();
                }

                if (Interlocked.CompareExchange(ref _compositionTransformDirty, 0, 1) == 1)
                {
                    if (!TryApplyPendingCompositionTransformOnRenderThread(out var skipFrameDispatch))
                    {
                        break;
                    }

                    if (skipFrameDispatch)
                    {
                        continue;
                    }
                }

                if (!ProcessRenderThreadFrameOrIdle())
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"D3D11 preview renderer thread failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
            NotifyRenderThreadFailed(ex);
        }
        finally
        {
            CleanupRenderThreadExit();
        }
    }

    private void HandlePendingSharedDeviceResetOnRenderThread()
    {
        while (TryDequeuePendingFrame(out var stale))
        {
            TrackFrameDropped(stale, "shared-device-reset");
            stale.Dispose();
        }

        try
        {
            // The capture backend can hand us its shared D3D device after the
            // render thread has already created a startup swap chain. Rebuilding
            // directly would leave the first chain attached to SwapChainPanel
            // while the fields point at the second chain, corrupting WinUI's
            // native panel state. Unbind before rebuilding the shared-device chain.
            if (Interlocked.CompareExchange(ref _swapChainBound, 0, 1) == 1)
            {
                Interlocked.Exchange(ref _swapChainAddress, 0);
                UnbindSwapChainFromPanel();
            }

            CleanupD3DResources();
            InitializeD3D();
            Interlocked.Exchange(ref _compositionTransformDirty, 1);
        }
        catch (Exception ex)
        {
            Logger.Log($"D3D11 preview shared device rebind failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
            CleanupD3DResources();
        }
    }

    private bool TryApplyPendingCompositionTransformOnRenderThread(out bool skipFrameDispatch)
    {
        skipFrameDispatch = false;

        // Re-check stop flag: Stop() may have unbound the swap chain between
        // the top-of-loop check and here. Accessing an unbound chain causes
        // native stack corruption (BEX64 / 0xc0000409).
        if (Volatile.Read(ref _stopRequested) != 0)
        {
            return false;
        }

        try
        {
            var swapChain = _swapChain;
            if (swapChain != null && Volatile.Read(ref _swapChainBound) == 1)
            {
                ApplyCompositionScaleTransform(swapChain);
            }
        }
        catch (Exception ex)
        {
            if (IsDeviceLostException(ex))
            {
                HandleDeviceLost(ex);
                skipFrameDispatch = true;
                return true;
            }

            Logger.Log($"D3D11 preview composition transform update failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
        }

        return true;
    }

    private bool ProcessRenderThreadFrameOrIdle()
    {
        if (!TryDequeuePendingFrame(out var frame))
        {
            ResetFrameReady("render_loop_idle");
            if (!_pendingFrames.IsEmpty ||
                Volatile.Read(ref _compositionTransformDirty) != 0 ||
                Volatile.Read(ref _sharedDeviceResetPending) != 0)
            {
                SignalFrameReady("render_loop_race");
            }

            return true;
        }

        if (Volatile.Read(ref _stopRequested) != 0)
        {
            TrackFrameDropped(frame, "renderer-stopped");
            frame.Dispose();
            return false;
        }

        try
        {
            if (frame.SubmissionGeneration != Interlocked.Read(ref _submissionGeneration))
            {
                var reason = Volatile.Read(ref _submissionGenerationDropReason);
                TrackFrameDropped(frame, string.IsNullOrWhiteSpace(reason) ? "stale-generation" : $"{reason}:stale");
                return true;
            }

            WaitForFrameLatencySignal();
            var framesRenderedBefore = Interlocked.Read(ref _framesRendered);
            RenderFrame(frame);
            if (Interlocked.Read(ref _framesRendered) == framesRenderedBefore)
            {
                TrackFrameDropped(frame, "render-skipped");
            }

            // Keep the event set while more frames are queued so the
            // render thread drains the elastic buffer without waiting.
            if (!_pendingFrames.IsEmpty)
            {
                SignalFrameReady("render_loop_drain");
            }
        }
        catch (Exception ex)
        {
            if (IsDeviceLostException(ex))
            {
                HandleDeviceLost(ex);
            }
            else
            {
                Logger.Log($"D3D11 preview render failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
            }

            TrackFrameDropped(frame, "render-failed");
        }
        finally
        {
            frame.Dispose();
        }

        if (_pendingFrames.IsEmpty &&
            Volatile.Read(ref _compositionTransformDirty) == 0 &&
            Volatile.Read(ref _sharedDeviceResetPending) == 0)
        {
            ResetFrameReady("render_loop_empty_after_failure");
        }

        return true;
    }

    private void ConfigureFrameLatencyWaitableObject()
    {
        _frameLatencyWaitHandle = IntPtr.Zero;
        _swapChain2?.Dispose();
        _swapChain2 = null;

        if (!_waitableSwapChainEnabled || _swapChain == null)
        {
            return;
        }

        _swapChain2 = _swapChain.QueryInterfaceOrNull<IDXGISwapChain2>();
        if (_swapChain2 == null)
        {
            Logger.Log("D3D11 preview waitable swap chain unavailable: IDXGISwapChain2 not supported.");
            return;
        }

        _swapChain2.MaximumFrameLatency = (uint)_dxgiMaxFrameLatency;
        _frameLatencyWaitHandle = _swapChain2.FrameLatencyWaitableObject;
        Logger.Log($"D3D11 preview waitable swap chain configured handle=0x{_frameLatencyWaitHandle.ToInt64():X} latency={_dxgiMaxFrameLatency}.");
    }

    private void WaitForFrameLatencySignal()
    {
        if (!_waitableSwapChainEnabled || _frameLatencyWaitHandle == IntPtr.Zero)
        {
            return;
        }

        var waitStart = Stopwatch.GetTimestamp();
        var result = WaitForSingleObject(_frameLatencyWaitHandle, 8);
        TrackFrameLatencyWait(result, Stopwatch.GetTimestamp() - waitStart);
        if (result != WaitObject0 && result != WaitTimeout)
        {
            Logger.Log($"D3D11 preview waitable swap chain wait returned {result}.");
        }
    }

    private void CleanupRenderThreadExit()
    {
        while (TryDequeuePendingFrame(out var stale))
        {
            TrackFrameDropped(stale, "renderer-exit");
            stale.Dispose();
        }

        FailPendingFrameCapture("Render thread exited before frame capture completed.");
        CleanupD3DResources();
        Interlocked.Exchange(ref _isRendering, 0);
        Volatile.Write(ref _rendererMode, RendererModeNone);
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
        // SetSwapChain (either null or new chain) hits an AccessViolationException,
        // a corrupted-state exception .NET Core cannot catch.
        // Unbinding first, while D3D resources are still alive, avoids this.
        //
        // CAS(1->0) ensures exactly one thread performs the unbind. The render
        // thread's CleanupD3DResources also CAS's this flag before disposing the
        // swap chain; whoever loses the race skips the native call entirely.
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

    private bool TryEnterNativeRenderCall()
    {
        if (Volatile.Read(ref _stopRequested) != 0 || Volatile.Read(ref _swapChainBound) == 0)
        {
            return false;
        }

        Interlocked.Exchange(ref _inNativeCall, 1);
        if (Volatile.Read(ref _stopRequested) == 0 && Volatile.Read(ref _swapChainBound) != 0)
        {
            return true;
        }

        ExitNativeRenderCall();
        return false;
    }

    private void ExitNativeRenderCall()
        => Interlocked.Exchange(ref _inNativeCall, 0);

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(D3D11PreviewRenderer));
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        Stop();
        _sharedDevice?.Dispose();
        _sharedDevice = null;
        _frameReadyEvent.Dispose();
    }

    private void ApplyCompositionScaleTransform(IDXGISwapChain1 swapChain)
    {
        using var swapChain2 = swapChain.QueryInterfaceOrNull<IDXGISwapChain2>();
        if (swapChain2 == null)
        {
            return;
        }

        var panelLogicalW = Volatile.Read(ref _panelLogicalWidth);
        var panelLogicalH = Volatile.Read(ref _panelLogicalHeight);
        var swapW = (double)Math.Max(1, _configuredOutputWidth);
        var swapH = (double)Math.Max(1, _configuredOutputHeight);

        if (panelLogicalW <= 0 || panelLogicalH <= 0)
        {
            swapChain2.MatrixTransform = Matrix3x2.Identity;
            return;
        }

        var uniformScale = (float)Math.Min(panelLogicalW / swapW, panelLogicalH / swapH);
        var offsetX = (float)((panelLogicalW - swapW * uniformScale) * 0.5);
        var offsetY = (float)((panelLogicalH - swapH * uniformScale) * 0.5);

        swapChain2.MatrixTransform = new Matrix3x2(
            uniformScale, 0,
            0, uniformScale,
            offsetX, offsetY);

        Logger.Log($"D3D11 preview composition transform set scale={uniformScale:F4} offset=({offsetX:F1},{offsetY:F1}) panel={panelLogicalW:F0}x{panelLogicalH:F0} swap={swapW}x{swapH}.");
    }

    private void BindSwapChainToPanel(IDXGISwapChain1 swapChain)
    {
        // ISwapChainPanelNative.SetSwapChain must be called on the UI thread
        // because _panel is a XAML element. Marshal from the render thread.
        //
        // Reinit deadlock guard: if the UI thread is blocked in StopRenderThread().Join()
        // waiting for this render thread to exit, dispatching here would deadlock until
        // the Join times out. Two safeguards: (a) the wait below polls _stopRequested in
        // short chunks so it can bail early, (b) the queued lambda re-checks both
        // _stopRequested and that _swapChain still equals the chain we are trying to
        // bind. If either has changed, the renderer has been stopped or the chain
        // superseded, and SetSwapChain on a stale chain would AV.
        var done = new ManualResetEventSlim(false);
        Exception? uiError = null;
        var aborted = false;

        var enqueued = _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                if (Volatile.Read(ref _stopRequested) != 0 ||
                    !ReferenceEquals(_swapChain, swapChain))
                {
                    uiError = new OperationCanceledException(
                        "Swap chain binding superseded before reaching the UI thread.");
                    return;
                }

                if (_panel?.XamlRoot == null)
                {
                    uiError = new InvalidOperationException(
                        "Panel is no longer in the visual tree; swap chain binding skipped.");
                    return;
                }

                var panelNative = WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel);
                panelNative.SetSwapChain(swapChain.NativePointer);
                Interlocked.Exchange(ref _swapChainAddress, swapChain.NativePointer.ToInt64());
                Interlocked.Exchange(ref _swapChainBound, 1);
            }
            catch (Exception ex)
            {
                uiError = ex;
            }
            finally
            {
                try { done.Set(); }
                catch { /* race with dispose if we aborted; safe to ignore */ }
            }
        });

        if (!enqueued)
        {
            done.Dispose();
            throw new InvalidOperationException("Failed to enqueue swap chain binding to UI thread.");
        }

        const int waitChunkMs = 50;
        const int maxWaitMs = 5000;
        var elapsedMs = 0;
        var completed = false;
        while (elapsedMs < maxWaitMs)
        {
            if (done.Wait(waitChunkMs))
            {
                completed = true;
                break;
            }

            elapsedMs += waitChunkMs;
            if (Volatile.Read(ref _stopRequested) != 0)
            {
                aborted = true;
                Logger.Log($"D3D11 preview swap-chain binding aborted at {elapsedMs}ms: stop requested during UI dispatcher wait.");
                break;
            }
        }

        if (!completed)
        {
            // Leave done undisposed: the queued lambda may still run later and
            // call done.Set(). Disposing now would race with that and risk an
            // ObjectDisposedException on the UI thread. The lambda's stale-chain
            // guard above prevents it from binding a disposed swap chain.
            if (aborted)
            {
                return;
            }

            throw new TimeoutException("Swap chain binding to UI thread timed out.");
        }

        done.Dispose();
        if (uiError != null)
        {
            if (uiError is OperationCanceledException)
            {
                Logger.Log("D3D11 preview swap-chain binding cancelled on UI thread; renderer shutting down.");
                return;
            }

            throw new InvalidOperationException("Swap chain binding failed on UI thread.", uiError);
        }
    }

    private void UnbindSwapChainFromPanel()
    {
        // Must run on UI thread since _panel is a XAML element.
        // Called from render thread during cleanup, so marshal via dispatcher.
        try
        {
            using var done = new ManualResetEventSlim(false);
            var enqueued = _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    // Guard: if the panel is no longer in the visual tree, its native
                    // COM backing may be released. AccessViolationException from a stale
                    // vtable pointer is a corrupted-state exception that .NET Core cannot
                    // catch; it terminates the process. Skip the call entirely.
                    if (_panel?.XamlRoot == null)
                    {
                        done.Set();
                        return;
                    }

                    var panelNative = WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel);
                    panelNative.SetSwapChain(IntPtr.Zero);
                }
                catch
                {
                    // Best-effort: panel may already be torn down during app shutdown.
                    Logger.Log("D3D11 preview swap chain unbind skipped: UI callback failed during cleanup.");
                }
                finally
                {
                    done.Set();
                }
            });

            if (enqueued)
            {
                if (!done.Wait(TimeSpan.FromSeconds(2)))
                {
                    Logger.Log("D3D11 preview swap chain unbind timed out on UI thread during cleanup.");
                }
            }
            else
            {
                Logger.Log("D3D11 preview swap chain unbind enqueue failed during cleanup.");
            }
        }
        catch (Exception ex)
        {
            // Dispatcher may be shut down; safe to ignore during cleanup.
            Logger.Log($"D3D11 preview swap chain unbind ignored during cleanup: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);
}
