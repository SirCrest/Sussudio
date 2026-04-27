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
using ElgatoCapture.Models;
using ElgatoCapture.Services.Capture;
using ElgatoCapture.Services.Runtime;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace ElgatoCapture.Services.Preview;

internal sealed partial class D3D11PreviewRenderer : IPreviewFrameSink, IPreviewDisplayClock, IDisposable
{
    private const int FrameCaptureTimeoutMs = 5000;
    private const string RendererModeNone = "None";
    private const string RendererModeVideoProcessor = "D3D11VideoProcessor";
    private const string RendererModeNv12Shader = "Nv12Shader";
    private const string RendererModeHdrShader = "HdrShader";
    private const string RendererModeHdrPassthrough = "HdrPassthrough";
    private static readonly uint[] PngCrc32Table = InitPngCrc32Table();

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

    private const string FullscreenVertexShaderSource = """
        struct VSOutput {
            float4 position : SV_POSITION;
            float2 texcoord : TEXCOORD0;
        };

        VSOutput main(uint vertexId : SV_VertexID) {
            VSOutput output;
            output.texcoord = float2((vertexId << 1) & 2, vertexId & 2);
            output.position = float4(output.texcoord * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0);
            return output;
        }
        """;

    private const string HdrTonemapPixelShaderSource = """
        cbuffer ViewportInfo : register(b0) {
            float2 vpOrigin;
            float2 vpSize;
        };

        Texture2D<float> yPlane : register(t0);
        Texture2D<float2> uvPlane : register(t1);
        SamplerState bilinearSampler : register(s0);

        static const float PQ_m1 = 0.1593017578125;
        static const float PQ_m2 = 78.84375;
        static const float PQ_c1 = 0.8359375;
        static const float PQ_c2 = 18.8515625;
        static const float PQ_c3 = 18.6875;

        float3 PQ_EOTF(float3 N) {
            float3 Np = pow(max(N, 0.0), 1.0 / PQ_m2);
            float3 numerator = max(Np - PQ_c1, 0.0);
            float3 denominator = PQ_c2 - PQ_c3 * Np;
            return pow(numerator / denominator, 1.0 / PQ_m1);
        }

        float3 BT2020_to_BT709(float3 c) {
            return float3(
                 1.6605 * c.r - 0.5877 * c.g - 0.0728 * c.b,
                -0.1246 * c.r + 1.1329 * c.g - 0.0083 * c.b,
                -0.0182 * c.r - 0.1006 * c.g + 1.1187 * c.b
            );
        }

        float3 LinearToSRGB(float3 c) {
            float3 lo = 12.92 * c;
            float3 hi = 1.055 * pow(max(c, 1e-6), 1.0 / 2.4) - 0.055;
            return float3(
                c.r <= 0.0031308 ? lo.r : hi.r,
                c.g <= 0.0031308 ? lo.g : hi.g,
                c.b <= 0.0031308 ? lo.b : hi.b
            );
        }

        float4 main(float4 pos : SV_Position) : SV_Target {
            float2 uv = (pos.xy - vpOrigin) / vpSize;

            float y_raw = yPlane.Sample(bilinearSampler, uv);
            float2 uv_raw = uvPlane.Sample(bilinearSampler, uv);

            float Y = saturate((y_raw - 64.0 / 1023.0) * 1023.0 / (940.0 - 64.0));
            float Cb = (uv_raw.x - 512.0 / 1023.0) * 1023.0 / (960.0 - 64.0);
            float Cr = (uv_raw.y - 512.0 / 1023.0) * 1023.0 / (960.0 - 64.0);

            float3 rgb;
            rgb.r = Y + 1.4746 * Cr;
            rgb.g = Y - 0.16455 * Cb - 0.57135 * Cr;
            rgb.b = Y + 1.8814 * Cb;
            rgb = saturate(rgb);

            float3 linearScene = PQ_EOTF(rgb) * 10000.0;
            linearScene /= 100.0;

            float3 bt709 = BT2020_to_BT709(linearScene);
            bt709 = max(bt709, 0.0);

            float3 tonemapped = bt709 / (1.0 + bt709);
            float3 srgb = LinearToSRGB(tonemapped);
            return float4(saturate(srgb), 1.0);
        }
        """;

    private const string HdrPassthroughPixelShaderSource = """
        cbuffer ViewportInfo : register(b0) {
            float2 vpOrigin;
            float2 vpSize;
        };

        Texture2D<float> yPlane : register(t0);
        Texture2D<float2> uvPlane : register(t1);
        SamplerState bilinearSampler : register(s0);

        float4 main(float4 pos : SV_Position) : SV_Target {
            float2 uv = (pos.xy - vpOrigin) / vpSize;

            float y_raw = yPlane.Sample(bilinearSampler, uv);
            float2 uv_raw = uvPlane.Sample(bilinearSampler, uv);

            // Narrow-range P010 to normalized YCbCr (same as tonemap shader)
            float Y = saturate((y_raw - 64.0 / 1023.0) * 1023.0 / (940.0 - 64.0));
            float Cb = (uv_raw.x - 512.0 / 1023.0) * 1023.0 / (960.0 - 64.0);
            float Cr = (uv_raw.y - 512.0 / 1023.0) * 1023.0 / (960.0 - 64.0);

            // BT.2020 YCbCr to RGB (preserve PQ encoding, no EOTF/tonemap/OETF)
            float3 rgb;
            rgb.r = Y + 1.4746 * Cr;
            rgb.g = Y - 0.16455 * Cb - 0.57135 * Cr;
            rgb.b = Y + 1.8814 * Cb;
            return float4(saturate(rgb), 1.0);
        }
        """;

    private const string Nv12PixelShaderSource = """
        cbuffer ViewportInfo : register(b0)
        {
            float2 vpOrigin;
            float2 vpSize;
        };

        Texture2D<float> yPlane : register(t0);
        Texture2D<float2> uvPlane : register(t1);
        SamplerState bilinear : register(s0);

        float4 main(float4 pos : SV_Position) : SV_Target
        {
            float2 uv = (pos.xy - vpOrigin) / vpSize;

            float y = yPlane.Sample(bilinear, uv).r;
            float2 uv2 = uvPlane.Sample(bilinear, uv);
            float cb = uv2.r - 0.501960784f;
            float cr = uv2.g - 0.501960784f;

            float r = saturate(y + 1.57480f * cr);
            float g = saturate(y - 0.18732f * cb - 0.46812f * cr);
            float b = saturate(y + 1.85560f * cb);
            return float4(r, g, b, 1.0f);
        }
        """;

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
            PooledVideoFrameLease? frameLease = null,
            IntPtr d3dTextureY = default,
            IntPtr d3dTextureUV = default,
            ID3D11Texture2D? d3dTextureYObject = null,
            ID3D11Texture2D? d3dTextureUVObject = null)
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
            SchedulerSubmitTick = schedulerSubmitTick;
            FrameLease = frameLease;
            D3DTextureY = d3dTextureY;
            D3DTextureUV = d3dTextureUV;
            D3DTextureYObject = d3dTextureYObject;
            D3DTextureUVObject = d3dTextureUVObject;
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
        public long SchedulerSubmitTick { get; }

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

    public readonly record struct PresentCadenceMetrics(
        int SampleCount,
        double ObservedFps,
        double ExpectedIntervalMs,
        double AverageIntervalMs,
        double P95IntervalMs,
        double P99IntervalMs,
        double MaxIntervalMs,
        double JitterStdDevMs,
        long SlowFrameCount,
        double SlowFramePercent);

    public readonly record struct CpuStageTimingMetrics(
        int SampleCount,
        double AverageMs,
        double P95Ms,
        double P99Ms,
        double MaxMs);

    public readonly record struct RenderCpuTimingMetrics(
        CpuStageTimingMetrics InputUpload,
        CpuStageTimingMetrics RenderSubmit,
        CpuStageTimingMetrics PresentCall,
        CpuStageTimingMetrics TotalFrame);

    public readonly record struct FrameOwnershipMetrics(
        long LastSubmittedPreviewPresentId,
        long LastSubmittedSourceSequenceNumber,
        long LastSubmittedQpc,
        long LastSubmittedUtcUnixMs,
        long LastRenderedPreviewPresentId,
        long LastRenderedSourceSequenceNumber,
        long LastRenderedQpc,
        long LastRenderedUtcUnixMs,
        double LastRenderedSchedulerToPresentMs,
        long LastDroppedPreviewPresentId,
        long LastDroppedSourceSequenceNumber,
        long LastDroppedQpc,
        long LastDroppedUtcUnixMs,
        string LastDropReason);

    public readonly record struct DxgiFrameStatisticsMetrics(
        long SampleCount,
        long SuccessCount,
        long FailureCount,
        string LastError,
        long PresentCount,
        long PresentRefreshCount,
        long SyncRefreshCount,
        long SyncQpcTime,
        long LastPresentDelta,
        long LastPresentRefreshDelta,
        long LastSyncRefreshDelta,
        long MissedRefreshCount);

    private readonly SwapChainPanel _panel;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ManualResetEventSlim _frameReadyEvent = new(false);
    private readonly object _lifecycleLock = new();
    private readonly int _presentSyncInterval = EnvironmentHelpers.GetIntFromEnv("ELGATOCAPTURE_PREVIEW_PRESENT_SYNC_INTERVAL", 1, 0, 1);
    private readonly int _dxgiMaxFrameLatency = EnvironmentHelpers.GetIntFromEnv("ELGATOCAPTURE_PREVIEW_DXGI_MAX_FRAME_LATENCY", 2, 1, 3);
    private readonly int _swapChainBufferCount = EnvironmentHelpers.GetIntFromEnv("ELGATOCAPTURE_PREVIEW_SWAPCHAIN_BUFFER_COUNT", 3, 2, 4);
    private readonly int _maxPendingFrames = EnvironmentHelpers.GetIntFromEnv("ELGATOCAPTURE_PREVIEW_RENDER_QUEUE_DEPTH", 4, 1, 8);
    private readonly bool _waitableSwapChainEnabled = EnvironmentHelpers.GetIntFromEnv("ELGATOCAPTURE_PREVIEW_WAITABLE_SWAPCHAIN", 0, 0, 1) != 0;
    private readonly bool _dxgiFrameStatisticsEnabled = EnvironmentHelpers.GetIntFromEnv("ELGATOCAPTURE_PREVIEW_DXGI_FRAME_STATS", 1, 0, 1) != 0;
    private readonly bool _dxgiFrameStatisticsDwmFlushEnabled = EnvironmentHelpers.GetIntFromEnv("ELGATOCAPTURE_PREVIEW_DXGI_FRAME_STATS_DWM_FLUSH", 0, 0, 1) != 0;
    private readonly bool _mediaPresentDurationEnabled = EnvironmentHelpers.GetIntFromEnv("ELGATOCAPTURE_PREVIEW_MEDIA_PRESENT_DURATION", 0, 0, 1) != 0;
    private readonly string _renderMmcssTask = Environment.GetEnvironmentVariable("ELGATOCAPTURE_PREVIEW_RENDER_MMCSS_TASK") ?? string.Empty;
    private readonly int _renderMmcssPriority = EnvironmentHelpers.GetIntFromEnv("ELGATOCAPTURE_PREVIEW_RENDER_MMCSS_PRIORITY", 1, -2, 2);

    private Thread? _renderThread;
    private readonly ConcurrentQueue<PendingFrame> _pendingFrames = new();
    private int _swapChainBound; // 0=unbound, 1=bound; use Interlocked.CompareExchange to claim unbind

    private int _disposed;
    private int _stopRequested;
    private int _isRendering;
    private int _inNativeCall; // 1 while render thread is between guard-check and Present return
    private int _compositionTransformDirty;
    private int _firstFrameRaised;
    private int _skipSwapChainDisposal; // 1 = CleanupD3DResources should not dispose the swap chain

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
    private bool _dxgiFrameStatisticsHasBaseline;
    private long _lastSubmittedPreviewPresentId;
    private long _lastSubmittedSourceSequenceNumber = -1;
    private long _lastSubmittedQpc;
    private long _lastSubmittedUtcUnixMs;
    private long _lastRenderedPreviewPresentId;
    private long _lastRenderedSourceSequenceNumber = -1;
    private long _lastRenderedQpc;
    private long _lastRenderedUtcUnixMs;
    private long _lastRenderedSchedulerToPresentTicks;
    private long _lastDroppedPreviewPresentId;
    private long _lastDroppedSourceSequenceNumber = -1;
    private long _lastDroppedQpc;
    private long _lastDroppedUtcUnixMs;
    private string _lastDropReason = string.Empty;

    private TaskCompletionSource<PreviewFrameCaptureResult>? _frameCaptureRequest;
    private int _frameCaptureEncodeInProgress;
    private string? _frameCaptureOutputPath;

    private string _inputColorSpaceLabel = "Unknown";
    private string _outputColorSpaceLabel = "Unknown";
    private string _rendererMode = RendererModeNone;

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
    private readonly ID3D11RenderTargetView?[] _rtvArray = new ID3D11RenderTargetView?[1];
    private readonly Viewport[] _viewportArray = new Viewport[1];
    private readonly ID3D11SamplerState[] _samplerArray = new ID3D11SamplerState[1];
    private readonly ID3D11ShaderResourceView?[] _srvArray2 = new ID3D11ShaderResourceView?[2];
    private readonly ID3D11ShaderResourceView?[] _srvNullArray2 = new ID3D11ShaderResourceView?[2];
    private readonly ID3D11Buffer[] _cbArray = new ID3D11Buffer[1];

    // Persistent staging texture for frame capture (avoids GPU resource churn)
    private ID3D11Texture2D? _captureStagingTexture;
    private int _captureStagingWidth;
    private int _captureStagingHeight;

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

    public D3D11PreviewRenderer(SwapChainPanel panel, DispatcherQueue dispatcherQueue)
    {
        _panel = panel ?? throw new ArgumentNullException(nameof(panel));
        _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
        _panelPixelWidth = 1;
        _panelPixelHeight = 1;
    }

    public event Action? FirstFrameRendered;

    public long FramesSubmitted => Interlocked.Read(ref _framesSubmitted);
    public long FramesRendered => Interlocked.Read(ref _framesRendered);
    public long FramesDropped => Interlocked.Read(ref _framesDropped);
    public int PendingFrameCount => _pendingFrames.Count;
    public bool IsRendering => Volatile.Read(ref _isRendering) != 0;
    public bool IsHdrCapableSwapChain => _hdrCapableSwapChain;
    public string RendererMode => Volatile.Read(ref _rendererMode);
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
        _frameReadyEvent.Set();
    }

    public Task<PreviewFrameCaptureResult> CaptureNextFrameAsync(string outputPath)
    {
        if (!IsRendering || _device == null || _swapChain == null || Volatile.Read(ref _stopRequested) != 0)
        {
            return Task.FromResult(CreateFrameCaptureError("No active preview renderer."));
        }

        if (Volatile.Read(ref _frameCaptureEncodeInProgress) != 0)
        {
            return Task.FromResult(CreateFrameCaptureError("A preview frame capture is already pending."));
        }

        var resolvedOutputPath = string.IsNullOrWhiteSpace(outputPath)
            ? Path.Combine(Path.GetTempPath(), $"preview_capture_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss_fff}.bmp")
            : outputPath;

        var request = new TaskCompletionSource<PreviewFrameCaptureResult>(
            state: resolvedOutputPath,
            creationOptions: TaskCreationOptions.RunContinuationsAsynchronously);
        if (Interlocked.CompareExchange(ref _frameCaptureRequest, request, null) != null)
        {
            return Task.FromResult(CreateFrameCaptureError("A preview frame capture is already pending."));
        }

        Volatile.Write(ref _frameCaptureOutputPath, resolvedOutputPath);
        _ = Task.Delay(FrameCaptureTimeoutMs).ContinueWith(
            _ =>
            {
                var pending = Interlocked.CompareExchange(ref _frameCaptureRequest, null, request);
                if (!ReferenceEquals(pending, request))
                {
                    return;
                }

                Interlocked.Exchange(ref _frameCaptureOutputPath, null);
                request.TrySetResult(CreateFrameCaptureError("Timed out waiting for the next rendered preview frame."));
                Logger.Log("PREVIEW_FRAME_CAPTURE_TIMEOUT missing=RenderedFrame");
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return request.Task;
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

        if (Volatile.Read(ref _isRendering) != 0)
        {
            Interlocked.Exchange(ref _sharedDeviceResetPending, 1);
            _frameReadyEvent.Set();
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
            Interlocked.Exchange(ref _skipSwapChainDisposal, 0);
            Interlocked.Exchange(ref _compositionTransformDirty, 1);
            Interlocked.Exchange(ref _firstFrameRaised, 0);
            Interlocked.Exchange(ref _sharedDeviceResetPending, 0);
            _frameReadyEvent.Reset();

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
    /// Stops the render thread and waits for it to exit, but does NOT unbind
    /// the swap chain from the XAML panel. Use this when the renderer will be
    /// replaced by a new instance on the same SwapChainPanel during reinit —
    /// the new renderer's BindSwapChainToPanel will overwrite the binding.
    /// Calling SetSwapChain(null) then SetSwapChain(newPtr) in quick succession
    /// on the same panel triggers an AccessViolationException in WinUI 3.
    /// </summary>
    public void StopRenderThread()
    {
        Thread? renderThread;
        lock (_lifecycleLock)
        {
            renderThread = _renderThread;
            _renderThread = null;
            if (renderThread == null)
            {
                FailPendingFrameCapture("Preview renderer is not running.");
                Volatile.Write(ref _rendererMode, RendererModeNone);
                ResetPresentCadence();
                return;
            }
            Interlocked.Exchange(ref _stopRequested, 1);
        }

        // Wait for any in-flight native render call to complete.
        {
            var sw = new SpinWait();
            while (Volatile.Read(ref _inNativeCall) != 0)
            {
                sw.SpinOnce();
            }
        }

        // Do NOT unbind or dispose the swap chain — the new renderer will
        // overwrite the panel binding with its own chain. Disposing the old
        // chain while the panel still holds a native reference triggers an
        // AccessViolationException in WinUI 3's ISwapChainPanelNative.
        Interlocked.Exchange(ref _skipSwapChainDisposal, 1);

        _frameReadyEvent.Set();
        if (Thread.CurrentThread != renderThread)
        {
            if (!renderThread.Join(TimeSpan.FromSeconds(3)))
            {
                Logger.Log("D3D11 preview renderer stop (render-thread-only) wait exceeded 3s; waiting until render thread exits.");
                renderThread.Join();
            }
        }

        while (_pendingFrames.TryDequeue(out var stale))
        {
            TrackFrameDropped(stale, "renderer-stop");
            stale.Dispose();
        }

        FailPendingFrameCapture("Preview renderer stopped before frame capture completed.");
        Volatile.Write(ref _rendererMode, RendererModeNone);
        ResetPresentCadence();
        Logger.Log("D3D11 preview renderer render thread stopped (swap chain still bound).");
    }

    public void Stop()
    {
        Thread? renderThread;
        lock (_lifecycleLock)
        {
            renderThread = _renderThread;
            _renderThread = null;
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
        {
            var sw = new SpinWait();
            while (Volatile.Read(ref _inNativeCall) != 0)
            {
                sw.SpinOnce();
            }
        }

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
        _frameReadyEvent.Set();
        if (Thread.CurrentThread != renderThread)
        {
            if (!renderThread.Join(TimeSpan.FromSeconds(3)))
            {
                Logger.Log("D3D11 preview renderer stop wait exceeded 3s; waiting until render thread exits.");
                renderThread.Join();
            }
        }

        while (_pendingFrames.TryDequeue(out var stale))
        {
            TrackFrameDropped(stale, "renderer-stop");
            stale.Dispose();
        }

        FailPendingFrameCapture("Preview renderer stopped before frame capture completed.");
        Volatile.Write(ref _rendererMode, RendererModeNone);
        ResetPresentCadence();
        Logger.Log("D3D11 preview renderer stop completed.");
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
        _frameReadyEvent.Set();
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
        long arrivalTick = 0,
        long sourceSequenceNumber = -1,
        long previewPresentId = 0,
        long schedulerSubmitTick = 0)
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
            arrivalTick,
            sourceSequenceNumber,
            previewPresentId,
            schedulerSubmitTick);
        EnqueuePendingFrame(frame);
    }

    public void SubmitRawFrameLease(
        PooledVideoFrameLease frame,
        bool isHdr,
        long previewPresentId = 0,
        long schedulerSubmitTick = 0)
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
            previewPresentId,
            schedulerSubmitTick,
            frameLease: frame));
    }

    public void SubmitTexture(IntPtr d3dTexture, int subresourceIndex, int width, int height, bool isHdr, long arrivalTick = 0)
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

        var frame = new PendingFrame(texture, subresourceIndex, null, 0, width, height, isHdr, arrivalTick);
        EnqueuePendingFrame(frame);
    }

    public void SubmitNv12PlaneTextures(IntPtr yTexturePtr, IntPtr uvTexturePtr, int width, int height, long arrivalTick = 0)
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

            EnqueueNv12Frame(ownedYTexturePtr, yTexture, ownedUvTexturePtr, uvTexture, width, height, arrivalTick);
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
        long arrivalTick)
    {
        var frame = new PendingFrame(
            d3dTexture: null,
            d3dSubresourceIndex: 0,
            rawData: null,
            rawDataLength: 0,
            width: width,
            height: height,
            isHdr: false,
            arrivalTick: arrivalTick,
            d3dTextureY: yTexturePtr,
            d3dTextureUV: uvTexturePtr,
            d3dTextureYObject: yTexture,
            d3dTextureUVObject: uvTexture);
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
                Interlocked.Increment(ref _framesDropped);
                return;
            }

            _pendingFrames.Enqueue(frame);
            TrackFrameSubmitted(frame);

            // Trim oldest frames if the queue exceeds the elastic limit.
            // Under normal operation the render thread keeps up and the queue
            // stays at 0-1 (no added latency). The extra slots only absorb
            // brief render hiccups instead of dropping frames.
            while (_pendingFrames.Count > _maxPendingFrames)
            {
                if (_pendingFrames.TryDequeue(out var oldest))
                {
                    TrackFrameDropped(oldest, "renderer-backlog");
                    oldest.Dispose();
                    Interlocked.Increment(ref _framesDropped);
                }
            }

            Volatile.Write(ref _naturalWidth, frame.Width);
            Volatile.Write(ref _naturalHeight, frame.Height);
            Interlocked.Increment(ref _framesSubmitted);
        }

        _frameReadyEvent.Set();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        Stop();
        _sharedDevice?.Dispose();
        _sharedDevice = null;
        _frameReadyEvent.Dispose();
    }

    public PresentCadenceMetrics GetPresentCadenceMetrics(double expectedIntervalMs)
    {
        double[] samples;
        lock (_presentCadenceLock)
        {
            if (_presentIntervalCount <= 0)
            {
                return new PresentCadenceMetrics(
                    SampleCount: 0,
                    ObservedFps: 0,
                    ExpectedIntervalMs: expectedIntervalMs,
                    AverageIntervalMs: 0,
                    P95IntervalMs: 0,
                    P99IntervalMs: 0,
                    MaxIntervalMs: 0,
                    JitterStdDevMs: 0,
                    SlowFrameCount: 0,
                    SlowFramePercent: 0);
            }

            samples = new double[_presentIntervalCount];
            for (var i = 0; i < _presentIntervalCount; i++)
            {
                var ringIndex = (_presentIntervalIndex - _presentIntervalCount + i + _presentIntervalWindowMs.Length)
                    % _presentIntervalWindowMs.Length;
                samples[i] = _presentIntervalWindowMs[ringIndex];
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
        var slowThresholdMs = targetIntervalMs * 1.6;

        long slowFrameCount = 0;
        var varianceSum = 0.0;
        for (var i = 0; i < sampleCount; i++)
        {
            var delta = samples[i] - average;
            varianceSum += delta * delta;
            if (samples[i] >= slowThresholdMs)
            {
                slowFrameCount++;
            }
        }

        var jitterStdDevMs = Math.Sqrt(varianceSum / sampleCount);
        var sorted = (double[])samples.Clone();
        Array.Sort(sorted);
        var p95Index = (int)Math.Ceiling((sorted.Length - 1) * 0.95);
        var p95IntervalMs = sorted[Math.Clamp(p95Index, 0, sorted.Length - 1)];
        var p99Index = (int)Math.Ceiling((sorted.Length - 1) * 0.99);
        var p99IntervalMs = sorted[Math.Clamp(p99Index, 0, sorted.Length - 1)];
        var slowPercent = slowFrameCount <= 0
            ? 0
            : (double)slowFrameCount / Math.Max(1, sampleCount) * 100.0;

        return new PresentCadenceMetrics(
            SampleCount: sampleCount,
            ObservedFps: observedFps,
            ExpectedIntervalMs: targetIntervalMs,
            AverageIntervalMs: average,
            P95IntervalMs: p95IntervalMs,
            P99IntervalMs: p99IntervalMs,
            MaxIntervalMs: max,
            JitterStdDevMs: jitterStdDevMs,
            SlowFrameCount: slowFrameCount,
            SlowFramePercent: slowPercent);
    }

    public double GetEstimatedPipelineLatencyMs()
    {
        lock (_pipelineLatencyLock)
        {
            if (_pipelineLatencyCount <= 0)
            {
                return 0;
            }

            var sum = 0.0;
            for (var i = 0; i < _pipelineLatencyCount; i++)
            {
                var idx = (_pipelineLatencyIndex - _pipelineLatencyCount + i + _pipelineLatencyWindowMs.Length)
                    % _pipelineLatencyWindowMs.Length;
                sum += _pipelineLatencyWindowMs[idx];
            }

            return sum / _pipelineLatencyCount;
        }
    }

    public double[] GetRecentPresentIntervalsMs(int maxSamples)
    {
        lock (_presentCadenceLock)
        {
            return CopyRecentRing(_presentIntervalWindowMs, _presentIntervalCount, _presentIntervalIndex, maxSamples);
        }
    }

    public double[] GetRecentPipelineLatencyMs(int maxSamples)
    {
        lock (_pipelineLatencyLock)
        {
            return CopyRecentRing(_pipelineLatencyWindowMs, _pipelineLatencyCount, _pipelineLatencyIndex, maxSamples);
        }
    }

    public RenderCpuTimingMetrics GetRenderCpuTimingMetrics()
    {
        double[] uploadSamples;
        double[] renderSamples;
        double[] presentSamples;
        double[] totalSamples;
        lock (_renderCpuTimingLock)
        {
            uploadSamples = CopyRecentRing(_inputUploadCpuTimingWindowMs, _renderCpuTimingCount, _renderCpuTimingIndex, _renderCpuTimingCount);
            renderSamples = CopyRecentRing(_renderSubmitCpuTimingWindowMs, _renderCpuTimingCount, _renderCpuTimingIndex, _renderCpuTimingCount);
            presentSamples = CopyRecentRing(_presentCallTimingWindowMs, _renderCpuTimingCount, _renderCpuTimingIndex, _renderCpuTimingCount);
            totalSamples = CopyRecentRing(_renderTotalCpuTimingWindowMs, _renderCpuTimingCount, _renderCpuTimingIndex, _renderCpuTimingCount);
        }

        return new RenderCpuTimingMetrics(
            SummarizeCpuStageTiming(uploadSamples),
            SummarizeCpuStageTiming(renderSamples),
            SummarizeCpuStageTiming(presentSamples),
            SummarizeCpuStageTiming(totalSamples));
    }

    public FrameOwnershipMetrics GetFrameOwnershipMetrics()
    {
        var schedulerToPresentTicks = Interlocked.Read(ref _lastRenderedSchedulerToPresentTicks);
        return new FrameOwnershipMetrics(
            LastSubmittedPreviewPresentId: Interlocked.Read(ref _lastSubmittedPreviewPresentId),
            LastSubmittedSourceSequenceNumber: Interlocked.Read(ref _lastSubmittedSourceSequenceNumber),
            LastSubmittedQpc: Interlocked.Read(ref _lastSubmittedQpc),
            LastSubmittedUtcUnixMs: Interlocked.Read(ref _lastSubmittedUtcUnixMs),
            LastRenderedPreviewPresentId: Interlocked.Read(ref _lastRenderedPreviewPresentId),
            LastRenderedSourceSequenceNumber: Interlocked.Read(ref _lastRenderedSourceSequenceNumber),
            LastRenderedQpc: Interlocked.Read(ref _lastRenderedQpc),
            LastRenderedUtcUnixMs: Interlocked.Read(ref _lastRenderedUtcUnixMs),
            LastRenderedSchedulerToPresentMs: schedulerToPresentTicks > 0 ? TicksToMs(schedulerToPresentTicks) : 0,
            LastDroppedPreviewPresentId: Interlocked.Read(ref _lastDroppedPreviewPresentId),
            LastDroppedSourceSequenceNumber: Interlocked.Read(ref _lastDroppedSourceSequenceNumber),
            LastDroppedQpc: Interlocked.Read(ref _lastDroppedQpc),
            LastDroppedUtcUnixMs: Interlocked.Read(ref _lastDroppedUtcUnixMs),
            LastDropReason: Volatile.Read(ref _lastDropReason));
    }

    public DxgiFrameStatisticsMetrics GetDxgiFrameStatisticsMetrics()
    {
        lock (_dxgiFrameStatisticsLock)
        {
            return new DxgiFrameStatisticsMetrics(
                SampleCount: _dxgiFrameStatisticsSampleCount,
                SuccessCount: _dxgiFrameStatisticsSuccessCount,
                FailureCount: _dxgiFrameStatisticsFailureCount,
                LastError: _dxgiFrameStatisticsLastError,
                PresentCount: _dxgiFrameStatisticsPresentCount,
                PresentRefreshCount: _dxgiFrameStatisticsPresentRefreshCount,
                SyncRefreshCount: _dxgiFrameStatisticsSyncRefreshCount,
                SyncQpcTime: _dxgiFrameStatisticsSyncQpcTime,
                LastPresentDelta: _dxgiFrameStatisticsLastPresentDelta,
                LastPresentRefreshDelta: _dxgiFrameStatisticsLastPresentRefreshDelta,
                LastSyncRefreshDelta: _dxgiFrameStatisticsLastSyncRefreshDelta,
                MissedRefreshCount: _dxgiFrameStatisticsMissedRefreshCount);
        }
    }

    private void TrackFrameSubmitted(PendingFrame frame)
    {
        Interlocked.Exchange(ref _lastSubmittedPreviewPresentId, frame.PreviewPresentId);
        Interlocked.Exchange(ref _lastSubmittedSourceSequenceNumber, frame.SourceSequenceNumber);
        Interlocked.Exchange(ref _lastSubmittedQpc, Stopwatch.GetTimestamp());
        Interlocked.Exchange(ref _lastSubmittedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }

    private void TrackFramePresented(PendingFrame frame, long presentReturnTick)
    {
        Interlocked.Exchange(ref _lastRenderedPreviewPresentId, frame.PreviewPresentId);
        Interlocked.Exchange(ref _lastRenderedSourceSequenceNumber, frame.SourceSequenceNumber);
        Interlocked.Exchange(ref _lastRenderedQpc, presentReturnTick);
        Interlocked.Exchange(ref _lastRenderedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        var schedulerToPresentTicks = frame.SchedulerSubmitTick > 0 && presentReturnTick > frame.SchedulerSubmitTick
            ? presentReturnTick - frame.SchedulerSubmitTick
            : 0;
        Interlocked.Exchange(ref _lastRenderedSchedulerToPresentTicks, schedulerToPresentTicks);
    }

    private void TrackFrameDropped(PendingFrame frame, string reason)
    {
        Interlocked.Exchange(ref _lastDroppedPreviewPresentId, frame.PreviewPresentId);
        Interlocked.Exchange(ref _lastDroppedSourceSequenceNumber, frame.SourceSequenceNumber);
        Interlocked.Exchange(ref _lastDroppedQpc, Stopwatch.GetTimestamp());
        Interlocked.Exchange(ref _lastDroppedUtcUnixMs, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Volatile.Write(ref _lastDropReason, reason);
    }

    private static double[] CopyRecentRing(double[] window, int count, int index, int maxSamples)
    {
        var take = Math.Min(Math.Max(0, maxSamples), count);
        if (take <= 0)
        {
            return Array.Empty<double>();
        }

        var result = new double[take];
        var start = (index - take + window.Length) % window.Length;
        for (var i = 0; i < take; i++)
        {
            result[i] = window[(start + i) % window.Length];
        }

        return result;
    }

    private void TrackPresentCadence()
    {
        var nowTick = Stopwatch.GetTimestamp();
        var previousTick = Interlocked.Exchange(ref _lastPresentTick, nowTick);
        if (previousTick <= 0)
        {
            return;
        }

        var intervalMs = (nowTick - previousTick) * 1000.0 / Stopwatch.Frequency;
        if (intervalMs <= 0 || intervalMs > 5000)
        {
            return;
        }

        lock (_presentCadenceLock)
        {
            _presentIntervalWindowMs[_presentIntervalIndex] = intervalMs;
            _presentIntervalIndex = (_presentIntervalIndex + 1) % _presentIntervalWindowMs.Length;
            if (_presentIntervalCount < _presentIntervalWindowMs.Length)
            {
                _presentIntervalCount++;
            }
        }
    }

    private void TrackDxgiFrameStatistics()
    {
        if (!_dxgiFrameStatisticsEnabled || _swapChain == null)
        {
            return;
        }

        try
        {
            if (_dxgiFrameStatisticsDwmFlushEnabled)
            {
                _ = DwmFlush();
            }

            var result = _swapChain.GetFrameStatistics(out var stats);
            lock (_dxgiFrameStatisticsLock)
            {
                _dxgiFrameStatisticsSampleCount++;
                if (result.Failure)
                {
                    _dxgiFrameStatisticsFailureCount++;
                    _dxgiFrameStatisticsLastError = $"0x{result.Code:X8}";
                    return;
                }

                _dxgiFrameStatisticsSuccessCount++;
                _dxgiFrameStatisticsLastError = string.Empty;

                var presentCount = (long)stats.PresentCount;
                var presentRefreshCount = (long)stats.PresentRefreshCount;
                var syncRefreshCount = (long)stats.SyncRefreshCount;
                _dxgiFrameStatisticsSyncQpcTime = stats.SyncQPCTime;

                if (_dxgiFrameStatisticsHasBaseline &&
                    _dxgiFrameStatisticsPresentCount > 0 &&
                    _dxgiFrameStatisticsPresentRefreshCount > 0 &&
                    _dxgiFrameStatisticsSyncRefreshCount > 0)
                {
                    _dxgiFrameStatisticsLastPresentDelta = presentCount - _dxgiFrameStatisticsPresentCount;
                    _dxgiFrameStatisticsLastPresentRefreshDelta = presentRefreshCount - _dxgiFrameStatisticsPresentRefreshCount;
                    _dxgiFrameStatisticsLastSyncRefreshDelta = syncRefreshCount - _dxgiFrameStatisticsSyncRefreshCount;
                    if (_dxgiFrameStatisticsLastPresentDelta < 0 ||
                        _dxgiFrameStatisticsLastPresentRefreshDelta < 0 ||
                        _dxgiFrameStatisticsLastSyncRefreshDelta < 0 ||
                        _dxgiFrameStatisticsLastPresentDelta > 100 ||
                        _dxgiFrameStatisticsLastPresentRefreshDelta > 100 ||
                        _dxgiFrameStatisticsLastSyncRefreshDelta > 100)
                    {
                        _dxgiFrameStatisticsLastPresentDelta = 0;
                        _dxgiFrameStatisticsLastPresentRefreshDelta = 0;
                        _dxgiFrameStatisticsLastSyncRefreshDelta = 0;
                    }
                    else if (_dxgiFrameStatisticsLastPresentDelta > 0 &&
                             _dxgiFrameStatisticsLastPresentRefreshDelta > _dxgiFrameStatisticsLastPresentDelta)
                    {
                        _dxgiFrameStatisticsMissedRefreshCount +=
                            _dxgiFrameStatisticsLastPresentRefreshDelta - _dxgiFrameStatisticsLastPresentDelta;
                    }
                }
                else
                {
                    _dxgiFrameStatisticsLastPresentDelta = 0;
                    _dxgiFrameStatisticsLastPresentRefreshDelta = 0;
                    _dxgiFrameStatisticsLastSyncRefreshDelta = 0;
                }

                _dxgiFrameStatisticsPresentCount = presentCount;
                _dxgiFrameStatisticsPresentRefreshCount = presentRefreshCount;
                _dxgiFrameStatisticsSyncRefreshCount = syncRefreshCount;
                _dxgiFrameStatisticsHasBaseline =
                    presentCount > 0 &&
                    presentRefreshCount > 0 &&
                    syncRefreshCount > 0;
            }
        }
        catch (Exception ex)
        {
            lock (_dxgiFrameStatisticsLock)
            {
                _dxgiFrameStatisticsSampleCount++;
                _dxgiFrameStatisticsFailureCount++;
                _dxgiFrameStatisticsLastError = $"{ex.GetType().Name}:0x{ex.HResult:X8}";
            }
        }
    }

    private void TrackPipelineLatency(long arrivalTick)
    {
        if (arrivalTick <= 0)
        {
            return;
        }

        var latencyMs = (Stopwatch.GetTimestamp() - arrivalTick) * 1000.0 / Stopwatch.Frequency;
        if (latencyMs < 0 || latencyMs > 10000)
        {
            return;
        }

        lock (_pipelineLatencyLock)
        {
            _pipelineLatencyWindowMs[_pipelineLatencyIndex] = latencyMs;
            _pipelineLatencyIndex = (_pipelineLatencyIndex + 1) % _pipelineLatencyWindowMs.Length;
            if (_pipelineLatencyCount < _pipelineLatencyWindowMs.Length)
            {
                _pipelineLatencyCount++;
            }
        }
    }

    private void TrackRenderCpuTiming(long inputUploadTicks, long renderSubmitTicks, long presentCallTicks, long totalTicks)
    {
        if (totalTicks <= 0)
        {
            return;
        }

        var inputUploadMs = TicksToMs(inputUploadTicks);
        var renderSubmitMs = TicksToMs(renderSubmitTicks);
        var presentCallMs = TicksToMs(presentCallTicks);
        var totalMs = TicksToMs(totalTicks);
        if (!IsValidRenderCpuStageMs(totalMs))
        {
            return;
        }

        lock (_renderCpuTimingLock)
        {
            _inputUploadCpuTimingWindowMs[_renderCpuTimingIndex] = IsValidRenderCpuStageMs(inputUploadMs) ? inputUploadMs : 0;
            _renderSubmitCpuTimingWindowMs[_renderCpuTimingIndex] = IsValidRenderCpuStageMs(renderSubmitMs) ? renderSubmitMs : 0;
            _presentCallTimingWindowMs[_renderCpuTimingIndex] = IsValidRenderCpuStageMs(presentCallMs) ? presentCallMs : 0;
            _renderTotalCpuTimingWindowMs[_renderCpuTimingIndex] = totalMs;
            _renderCpuTimingIndex = (_renderCpuTimingIndex + 1) % _renderTotalCpuTimingWindowMs.Length;
            if (_renderCpuTimingCount < _renderTotalCpuTimingWindowMs.Length)
            {
                _renderCpuTimingCount++;
            }
        }
    }

    private static double TicksToMs(long ticks)
        => ticks <= 0 ? 0 : ticks * 1000.0 / Stopwatch.Frequency;

    private static bool IsValidRenderCpuStageMs(double value)
        => value >= 0 && value <= 5000 && !double.IsNaN(value) && !double.IsInfinity(value);

    public void SetExpectedFrameRate(double fps)
    {
        if (fps <= 0) return;
        _startupFps = fps;
        var targetSize = Math.Max(600, (int)Math.Ceiling(fps * CadenceWindowSeconds));
        lock (_presentCadenceLock)
        {
            if (_presentIntervalWindowMs.Length != targetSize)
            {
                _presentIntervalWindowMs = new double[targetSize];
                _presentIntervalCount = 0;
                _presentIntervalIndex = 0;
            }
        }

        lock (_pipelineLatencyLock)
        {
            if (_pipelineLatencyWindowMs.Length != targetSize)
            {
                _pipelineLatencyWindowMs = new double[targetSize];
                _pipelineLatencyCount = 0;
                _pipelineLatencyIndex = 0;
            }
        }

        lock (_renderCpuTimingLock)
        {
            if (_renderTotalCpuTimingWindowMs.Length != targetSize)
            {
                _inputUploadCpuTimingWindowMs = new double[targetSize];
                _renderSubmitCpuTimingWindowMs = new double[targetSize];
                _presentCallTimingWindowMs = new double[targetSize];
                _renderTotalCpuTimingWindowMs = new double[targetSize];
                _renderCpuTimingCount = 0;
                _renderCpuTimingIndex = 0;
            }
        }
    }

    public bool TryGetDisplayClock(out PreviewDisplayClockSnapshot snapshot)
    {
        var fps = Math.Max(1.0, _startupFps);
        var intervalTicks = Math.Max(1, (long)Math.Round(Stopwatch.Frequency / fps));
        long lastPresentTick;
        int sampleCount;
        lock (_dxgiFrameStatisticsLock)
        {
            lastPresentTick = _dxgiFrameStatisticsSyncQpcTime > 0
                ? _dxgiFrameStatisticsSyncQpcTime
                : Interlocked.Read(ref _lastPresentTick);
            sampleCount = _dxgiFrameStatisticsSuccessCount > 0
                ? (int)Math.Min(int.MaxValue, _dxgiFrameStatisticsSuccessCount)
                : Volatile.Read(ref _presentIntervalCount);
        }

        snapshot = new PreviewDisplayClockSnapshot(
            LastPresentTick: lastPresentTick,
            FrameIntervalTicks: intervalTicks,
            ExpectedFrameIntervalMs: 1000.0 / fps,
            SampleCount: sampleCount);
        return lastPresentTick > 0;
    }

    private void ResetPresentCadence()
    {
        Interlocked.Exchange(ref _lastPresentTick, 0);
        lock (_presentCadenceLock)
        {
            Array.Clear(_presentIntervalWindowMs, 0, _presentIntervalWindowMs.Length);
            _presentIntervalCount = 0;
            _presentIntervalIndex = 0;
        }

        lock (_pipelineLatencyLock)
        {
            Array.Clear(_pipelineLatencyWindowMs, 0, _pipelineLatencyWindowMs.Length);
            _pipelineLatencyCount = 0;
            _pipelineLatencyIndex = 0;
        }

        lock (_renderCpuTimingLock)
        {
            Array.Clear(_inputUploadCpuTimingWindowMs, 0, _inputUploadCpuTimingWindowMs.Length);
            Array.Clear(_renderSubmitCpuTimingWindowMs, 0, _renderSubmitCpuTimingWindowMs.Length);
            Array.Clear(_presentCallTimingWindowMs, 0, _presentCallTimingWindowMs.Length);
            Array.Clear(_renderTotalCpuTimingWindowMs, 0, _renderTotalCpuTimingWindowMs.Length);
            _renderCpuTimingCount = 0;
            _renderCpuTimingIndex = 0;
        }
    }

    private static CpuStageTimingMetrics SummarizeCpuStageTiming(double[] samples)
    {
        if (samples.Length == 0)
        {
            return new CpuStageTimingMetrics(0, 0, 0, 0, 0);
        }

        var sorted = (double[])samples.Clone();
        Array.Sort(sorted);
        var sum = 0.0;
        var max = 0.0;
        for (var i = 0; i < sorted.Length; i++)
        {
            sum += sorted[i];
            if (sorted[i] > max)
            {
                max = sorted[i];
            }
        }

        var p95Index = (int)Math.Ceiling((sorted.Length - 1) * 0.95);
        var p99Index = (int)Math.Ceiling((sorted.Length - 1) * 0.99);
        return new CpuStageTimingMetrics(
            sorted.Length,
            sum / sorted.Length,
            sorted[Math.Clamp(p95Index, 0, sorted.Length - 1)],
            sorted[Math.Clamp(p99Index, 0, sorted.Length - 1)],
            max);
    }

}
