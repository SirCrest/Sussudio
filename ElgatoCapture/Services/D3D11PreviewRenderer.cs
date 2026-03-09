using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using WinRT;

namespace ElgatoCapture.Services;

internal sealed class D3D11PreviewRenderer : IPreviewFrameSink, IDisposable
{
    private const int FrameCaptureTimeoutMs = 5000;
    private const string RendererModeNone = "None";
    private const string RendererModeVideoProcessor = "D3D11VideoProcessor";
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
            long arrivalTick)
        {
            D3DTexture = d3dTexture;
            D3DSubresourceIndex = Math.Max(0, d3dSubresourceIndex);
            RawData = rawData;
            RawDataLength = rawDataLength;
            Width = width;
            Height = height;
            IsHdr = isHdr;
            ArrivalTick = arrivalTick;
        }

        public ID3D11Texture2D? D3DTexture { get; private set; }
        public int D3DSubresourceIndex { get; }
        public byte[]? RawData { get; private set; }
        public int RawDataLength { get; private set; }
        public int Width { get; }
        public int Height { get; }
        public bool IsHdr { get; }
        public long ArrivalTick { get; }

        public void Dispose()
        {
            D3DTexture?.Dispose();
            D3DTexture = null;
            if (RawData != null)
            {
                ArrayPool<byte>.Shared.Return(RawData);
                RawData = null;
                RawDataLength = 0;
            }
        }
    }

    public readonly record struct PresentCadenceMetrics(
        int SampleCount,
        double ObservedFps,
        double ExpectedIntervalMs,
        double AverageIntervalMs,
        double P95IntervalMs,
        double MaxIntervalMs,
        double JitterStdDevMs,
        long SlowFrameCount,
        double SlowFramePercent);

    private readonly SwapChainPanel _panel;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly ManualResetEventSlim _frameReadyEvent = new(false);
    private readonly object _lifecycleLock = new();

    private Thread? _renderThread;
    private PendingFrame? _pendingFrame;

    private int _disposed;
    private int _stopRequested;
    private int _isRendering;
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
    private readonly object _presentCadenceLock = new();
    private readonly double[] _presentIntervalWindowMs = new double[300];
    private int _presentIntervalCount;
    private int _presentIntervalIndex;
    private long _lastPresentTick;
    private readonly object _pipelineLatencyLock = new();
    private readonly double[] _pipelineLatencyWindowMs = new double[300];
    private int _pipelineLatencyCount;
    private int _pipelineLatencyIndex;

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
    private IDXGISwapChain3? _swapChain3;
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
    private ID3D11PixelShader? _hdrTonemapPS;
    private ID3D11PixelShader? _hdrPassthroughPS;
    private ID3D11SamplerState? _linearSampler;
    private ID3D11Buffer? _viewportCB;
    private int _hdrInputConfiguredWidth;
    private int _hdrInputConfiguredHeight;
    private ID3D11Device? _sharedDevice;

    private int _configuredInputWidth;
    private int _configuredInputHeight;
    private int _configuredOutputWidth;
    private int _configuredOutputHeight;
    private Format _configuredInputFormat = Format.Unknown;
    private bool _configuredHdr;
    private bool _fullRangeInput;
    private bool _hdrCapableSwapChain;
    private bool _swapChainIsHdr10;
    private uint _outputFrameIndex;
    private int _hdrPassthroughEnabled;
    private int _swapChainColorSpaceDirty;
    private int _sharedDeviceResetPending;
    private int _sharedDeviceActive;

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
    public bool IsRendering => Volatile.Read(ref _isRendering) != 0;
    public bool IsHdrCapableSwapChain => _hdrCapableSwapChain;
    public string RendererMode => Volatile.Read(ref _rendererMode);
    public string InputColorSpaceLabel => _inputColorSpaceLabel;
    public string OutputColorSpaceLabel => _outputColorSpaceLabel;
    public int NaturalWidth => Volatile.Read(ref _naturalWidth);
    public int NaturalHeight => Volatile.Read(ref _naturalHeight);

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

        _frameReadyEvent.Set();
        if (Thread.CurrentThread != renderThread)
        {
            if (!renderThread.Join(TimeSpan.FromSeconds(3)))
            {
                Logger.Log("D3D11 preview renderer stop wait exceeded 3s; waiting until render thread exits.");
                renderThread.Join();
            }
        }

        var pending = Interlocked.Exchange(ref _pendingFrame, null);
        pending?.Dispose();
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

    public void SubmitRawFrame(IntPtr data, int dataLength, int width, int height, bool isHdr, long arrivalTick = 0)
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

        var frame = new PendingFrame(null, 0, copied, dataLength, width, height, isHdr, arrivalTick);
        EnqueuePendingFrame(frame);
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

    private void EnqueuePendingFrame(PendingFrame frame)
    {
        var previous = Interlocked.Exchange(ref _pendingFrame, frame);
        if (previous != null)
        {
            previous.Dispose();
            Interlocked.Increment(ref _framesDropped);
        }

        Volatile.Write(ref _naturalWidth, frame.Width);
        Volatile.Write(ref _naturalHeight, frame.Height);
        Interlocked.Increment(ref _framesSubmitted);
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
        var slowPercent = slowFrameCount <= 0
            ? 0
            : (double)slowFrameCount / Math.Max(1, sampleCount) * 100.0;

        return new PresentCadenceMetrics(
            SampleCount: sampleCount,
            ObservedFps: observedFps,
            ExpectedIntervalMs: targetIntervalMs,
            AverageIntervalMs: average,
            P95IntervalMs: p95IntervalMs,
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
    }

    private void RenderThreadMain()
    {
        Interlocked.Exchange(ref _isRendering, 1);
        try
        {
            InitializeD3D();
            while (Volatile.Read(ref _stopRequested) == 0)
            {
                _frameReadyEvent.Wait(TimeSpan.FromMilliseconds(200));
                if (Volatile.Read(ref _stopRequested) != 0) break;

                if (Interlocked.CompareExchange(ref _sharedDeviceResetPending, 0, 1) == 1)
                {
                    var stalePending = Interlocked.Exchange(ref _pendingFrame, null);
                    stalePending?.Dispose();
                    try
                    {
                        InitializeD3D();
                        Interlocked.Exchange(ref _compositionTransformDirty, 1);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"D3D11 preview shared device rebind failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
                    }
                }

                if (Interlocked.CompareExchange(ref _compositionTransformDirty, 0, 1) == 1)
                {
                    try
                    {
                        if (_swapChain != null)
                        {
                            ApplyCompositionScaleTransform(_swapChain);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (IsDeviceLostException(ex))
                        {
                            HandleDeviceLost(ex);
                            continue;
                        }

                        Logger.Log($"D3D11 preview composition transform update failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
                    }
                }

                var frame = Interlocked.Exchange(ref _pendingFrame, null);
                if (frame == null)
                {
                    _frameReadyEvent.Reset();
                    if (Volatile.Read(ref _pendingFrame) != null ||
                        Volatile.Read(ref _compositionTransformDirty) != 0 ||
                        Volatile.Read(ref _sharedDeviceResetPending) != 0)
                    {
                        _frameReadyEvent.Set();
                    }
                    continue;
                }

                try
                {
                    RenderFrame(frame);
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
                }
                finally
                {
                    frame.Dispose();
                }

                if (Volatile.Read(ref _pendingFrame) == null &&
                    Volatile.Read(ref _compositionTransformDirty) == 0 &&
                    Volatile.Read(ref _sharedDeviceResetPending) == 0)
                {
                    _frameReadyEvent.Reset();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"D3D11 preview renderer thread failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
        }
        finally
        {
            var pending = Interlocked.Exchange(ref _pendingFrame, null);
            pending?.Dispose();
            FailPendingFrameCapture("Render thread exited before frame capture completed.");
            CleanupD3DResources();
            Interlocked.Exchange(ref _isRendering, 0);
            Volatile.Write(ref _rendererMode, RendererModeNone);
        }
    }

    private void RenderFrame(PendingFrame frame)
    {
        ApplySwapChainColorSpaceIfDirty();

        if (frame.IsHdr && _fullscreenVS != null)
        {
            var usePassthrough = Volatile.Read(ref _hdrPassthroughEnabled) != 0 &&
                                 _hdrCapableSwapChain &&
                                 _hdrPassthroughPS != null;

            if (usePassthrough)
            {
                Volatile.Write(ref _rendererMode, RendererModeHdrPassthrough);
                RenderHdrFrameWithShader(frame, _hdrPassthroughPS!);
                return;
            }

            if (_hdrTonemapPS != null)
            {
                Volatile.Write(ref _rendererMode, RendererModeHdrShader);
                RenderHdrFrameWithShader(frame, _hdrTonemapPS);
                return;
            }
        }

        Volatile.Write(ref _rendererMode, RendererModeVideoProcessor);
        RenderFrameWithVideoProcessor(frame);
    }

    private void ApplySwapChainColorSpaceIfDirty()
    {
        if (Interlocked.CompareExchange(ref _swapChainColorSpaceDirty, 0, 1) != 1)
        {
            return;
        }

        if (_swapChain3 == null || !_hdrCapableSwapChain)
        {
            return;
        }

        var wantHdr = Volatile.Read(ref _hdrPassthroughEnabled) != 0;
        var targetColorSpace = wantHdr
            ? ColorSpaceType.RgbFullG2084NoneP2020
            : ColorSpaceType.RgbFullG22NoneP709;

        _swapChain3.SetColorSpace1(targetColorSpace);
        _swapChainIsHdr10 = wantHdr;

        var label = wantHdr ? "HDR10-PQ (BT.2020)" : "sRGB (BT.709)";
        _outputColorSpaceLabel = label;
        Logger.Log($"D3D11 preview swap chain color space set to {targetColorSpace} ({label}).");
    }

    private void RenderFrameWithVideoProcessor(PendingFrame frame)
    {
        var useExternalTexture = frame.D3DTexture != null;
        EnsurePipeline(frame.Width, frame.Height, frame.IsHdr, useExternalTexture);

        if (!TryResolveInputView(frame, out var inputView, out var disposeInputView))
        {
            return;
        }

        try
        {
            if (_videoContext == null || _videoProcessor == null || _outputView == null || inputView == null || _swapChain == null)
            {
                return;
            }

            var stream = new VideoProcessorStream { Enable = true, InputSurface = inputView };
            var bltResult = _videoContext.VideoProcessorBlt(_videoProcessor, _outputView, _outputFrameIndex++, 1, new[] { stream });
            if (bltResult.Failure)
            {
                throw new InvalidOperationException($"VideoProcessorBlt failed: 0x{bltResult.Code:X8}.");
            }

            TryCaptureFrameBeforePresent("VideoProcessor");
            var presentResult = _swapChain.Present(1, PresentFlags.None);
            if (presentResult.Failure)
            {
                throw new InvalidOperationException($"SwapChain.Present failed: 0x{presentResult.Code:X8}.");
            }

            if (Interlocked.Exchange(ref _firstFrameRaised, 1) == 0)
            {
                Logger.Log("D3D11 preview first frame rendered.");
                _dispatcherQueue.TryEnqueue(() => FirstFrameRendered?.Invoke());
            }

            Interlocked.Increment(ref _framesRendered);
            TrackPresentCadence();
            TrackPipelineLatency(frame.ArrivalTick);
        }
        finally
        {
            if (disposeInputView)
            {
                inputView?.Dispose();
            }
        }
    }

    private void RenderHdrFrameWithShader(PendingFrame frame, ID3D11PixelShader pixelShader)
    {
        if (_device == null || _deviceContext == null || _swapChain == null)
        {
            return;
        }

        if (_fullscreenVS == null || pixelShader == null || _linearSampler == null)
        {
            return;
        }

        EnsureHdrInputResources(frame.Width, frame.Height);
        if (_hdrInputTexture == null ||
            _hdrStagingTexture == null ||
            _hdrYPlaneSRV == null ||
            _hdrUVPlaneSRV == null)
        {
            return;
        }

        if (frame.D3DTexture != null)
        {
            // P010 is planar: Y plane (subresource 0) and UV plane (subresource 1).
            // Source reader returns a texture array where each frame occupies one array slice.
            // Planar subresource layout: plane 1 offset = arraySize * mipLevels.
            var srcDesc = frame.D3DTexture.Description;
            var planeOffset = (int)(srcDesc.ArraySize * Math.Max(1, srcDesc.MipLevels));

            // Copy Y plane: src array[i] plane 0 → dst plane 0
            _deviceContext.CopySubresourceRegion(_hdrInputTexture, 0, 0, 0, 0,
                frame.D3DTexture, (uint)frame.D3DSubresourceIndex);

            // Copy UV plane: src array[i] plane 1 → dst plane 1
            _deviceContext.CopySubresourceRegion(_hdrInputTexture, 1, 0, 0, 0,
                frame.D3DTexture, (uint)(frame.D3DSubresourceIndex + planeOffset));
        }
        else if (frame.RawData != null)
        {
            if (!UploadRawFrameToHdrTexture(frame.RawData, frame.RawDataLength, frame.Width, frame.Height))
            {
                return;
            }
        }
        else
        {
            return;
        }

        EnsureSwapChainRTV();
        if (_swapChainRTV == null)
        {
            return;
        }

        var outputWidth = _configuredOutputWidth > 0
            ? _configuredOutputWidth
            : Math.Max(1, Volatile.Read(ref _startupWidth));
        var outputHeight = _configuredOutputHeight > 0
            ? _configuredOutputHeight
            : Math.Max(1, Volatile.Read(ref _startupHeight));
        var destinationRect = ComputeLetterboxRect(frame.Width, frame.Height, outputWidth, outputHeight);
        var viewport = new Viewport(
            destinationRect.Left,
            destinationRect.Top,
            Math.Max(1, destinationRect.Right - destinationRect.Left),
            Math.Max(1, destinationRect.Bottom - destinationRect.Top),
            0.0f,
            1.0f);

        _deviceContext.OMSetRenderTargets(1, new[] { _swapChainRTV }, null);
        _deviceContext.ClearRenderTargetView(_swapChainRTV, new Color4(0.0f, 0.0f, 0.0f, 1.0f));
        _deviceContext.RSSetViewports(1, new[] { viewport });
        _deviceContext.IASetInputLayout(null);
        _deviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        _deviceContext.VSSetShader(_fullscreenVS, Array.Empty<ID3D11ClassInstance>(), 0);
        _deviceContext.PSSetShader(pixelShader, Array.Empty<ID3D11ClassInstance>(), 0);
        _deviceContext.PSSetSamplers(0, 1, new[] { _linearSampler });
        _deviceContext.PSSetShaderResources(0, 2, new[] { _hdrYPlaneSRV!, _hdrUVPlaneSRV! });

        if (_viewportCB != null)
        {
            var mapped = _deviceContext.Map(_viewportCB, 0, MapMode.WriteDiscard);
            unsafe
            {
                var data = (float*)mapped.DataPointer;
                data[0] = viewport.X;
                data[1] = viewport.Y;
                data[2] = viewport.Width;
                data[3] = viewport.Height;
            }
            _deviceContext.Unmap(_viewportCB, 0);
            _deviceContext.PSSetConstantBuffers(0, 1, new[] { _viewportCB });
        }

        _deviceContext.Draw(3, 0);
        _deviceContext.PSSetShaderResources(0, 2, new ID3D11ShaderResourceView[] { null!, null! });

        var rendererMode = ReferenceEquals(pixelShader, _hdrPassthroughPS)
            ? RendererModeHdrPassthrough
            : RendererModeHdrShader;
        TryCaptureFrameBeforePresent(rendererMode);
        var presentResult = _swapChain.Present(1, PresentFlags.None);
        if (presentResult.Failure)
        {
            throw new InvalidOperationException($"SwapChain.Present failed: 0x{presentResult.Code:X8}.");
        }

        if (Interlocked.Exchange(ref _firstFrameRaised, 1) == 0)
        {
            Logger.Log(
                rendererMode == RendererModeHdrPassthrough
                    ? "D3D11 preview first HDR frame rendered via passthrough shader."
                    : "D3D11 preview first HDR frame rendered via tonemapping shader.");
            _dispatcherQueue.TryEnqueue(() => FirstFrameRendered?.Invoke());
        }

        Interlocked.Increment(ref _framesRendered);
        TrackPresentCadence();
        TrackPipelineLatency(frame.ArrivalTick);
    }

    private void TryCaptureFrameBeforePresent(string rendererMode)
    {
        var request = Interlocked.Exchange(ref _frameCaptureRequest, null);
        if (request == null)
        {
            return;
        }

        var requestedOutputPath = request.Task.AsyncState as string;
        Interlocked.Exchange(ref _frameCaptureOutputPath, null);
        var outputPath = string.IsNullOrWhiteSpace(requestedOutputPath)
            ? Path.Combine(Path.GetTempPath(), $"preview_capture_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss_fff}.bmp")
            : requestedOutputPath;

        try
        {
            if (_device == null || _deviceContext == null || _swapChain == null)
            {
                request.TrySetResult(CreateFrameCaptureError("Renderer device state is unavailable.", rendererMode));
                return;
            }

            var fullOutputPath = Path.GetFullPath(outputPath);
            var isPng = fullOutputPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
            if (isPng && Interlocked.CompareExchange(ref _frameCaptureEncodeInProgress, 1, 0) != 0)
            {
                request.TrySetResult(CreateFrameCaptureError("A preview frame capture is already pending.", rendererMode));
                return;
            }

            ID3D11Texture2D? backBuffer = _swapChainBackBuffer;
            var disposeBackBuffer = false;
            if (backBuffer == null)
            {
                backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
                disposeBackBuffer = true;
            }

            try
            {
                if (backBuffer == null)
                {
                    throw new InvalidOperationException("Swap chain back buffer is unavailable.");
                }

                var backBufferDescription = backBuffer.Description;
                var width = checked((int)backBufferDescription.Width);
                var height = checked((int)backBufferDescription.Height);
                if (width <= 0 || height <= 0)
                {
                    throw new InvalidOperationException("Swap chain back buffer has invalid dimensions.");
                }

                var stagingDescription = new Texture2DDescription(
                    backBufferDescription.Format,
                    (uint)width,
                    (uint)height,
                    1,
                    1,
                    BindFlags.None,
                    ResourceUsage.Staging,
                    CpuAccessFlags.Read,
                    1,
                    0,
                    ResourceOptionFlags.None);

                using var stagingTexture = _device.CreateTexture2D(stagingDescription);
                _deviceContext.CopyResource(stagingTexture, backBuffer);

                _deviceContext.Map(stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None, out var mapped);
                PreviewFrameCaptureResult captureResult;
                byte[]? pngFrameBuffer = null;
                var pngSourceRowBytes = checked(width * 4);
                try
                {
                    if (isPng)
                    {
                        pngFrameBuffer = CopyMappedFrameToBuffer(mapped, height, pngSourceRowBytes);
                        captureResult = default!;
                    }
                    else
                    {
                        captureResult = CaptureMappedFrameToBmp(
                            mapped,
                            width,
                            height,
                            fullOutputPath,
                            rendererMode,
                            backBufferDescription.Format);
                    }
                }
                finally
                {
                    _deviceContext.Unmap(stagingTexture, 0);
                }

                if (isPng)
                {
                    var pngBuffer = pngFrameBuffer!;
                    _ = Task.Run(
                        () =>
                        {
                            try
                            {
                                var pngCaptureResult = CaptureFrameBufferTo16BitPng(
                                    pngBuffer,
                                    pngSourceRowBytes,
                                    width,
                                    height,
                                    fullOutputPath,
                                    rendererMode,
                                    backBufferDescription.Format);
                                request.TrySetResult(pngCaptureResult);
                                Logger.Log(
                                    $"PREVIEW_FRAME_CAPTURE_RESULT ok={pngCaptureResult.Succeeded} renderer={pngCaptureResult.RendererMode} path={pngCaptureResult.FilePath ?? "n/a"} width={pngCaptureResult.CapturedWidth} height={pngCaptureResult.CapturedHeight} avgLum={pngCaptureResult.AverageLuminance:0.00} pureBlackPct={pngCaptureResult.PureBlackPercent:0.00}");
                            }
                            catch (Exception ex)
                            {
                                request.TrySetResult(CreateFrameCaptureError($"Preview frame capture failed: {ex.Message}", rendererMode));
                                Logger.Log($"PREVIEW_FRAME_CAPTURE_RESULT ok=false renderer={rendererMode} type={ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
                            }
                            finally
                            {
                                Interlocked.Exchange(ref _frameCaptureEncodeInProgress, 0);
                            }
                        });
                    return;
                }

                request.TrySetResult(captureResult);
                Logger.Log(
                    $"PREVIEW_FRAME_CAPTURE_RESULT ok={captureResult.Succeeded} renderer={captureResult.RendererMode} path={captureResult.FilePath ?? "n/a"} width={captureResult.CapturedWidth} height={captureResult.CapturedHeight} avgLum={captureResult.AverageLuminance:0.00} pureBlackPct={captureResult.PureBlackPercent:0.00}");
            }
            finally
            {
                if (disposeBackBuffer)
                {
                    backBuffer?.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            Interlocked.Exchange(ref _frameCaptureEncodeInProgress, 0);
            request.TrySetResult(CreateFrameCaptureError($"Preview frame capture failed: {ex.Message}", rendererMode));
            Logger.Log($"PREVIEW_FRAME_CAPTURE_RESULT ok=false renderer={rendererMode} type={ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
        }
    }

    private static PreviewFrameCaptureResult CaptureMappedFrameToBmp(
        MappedSubresource mapped,
        int width,
        int height,
        string outputPath,
        string rendererMode,
        Format backBufferFormat = Format.B8G8R8A8_UNorm)
    {
        const int bitmapFileHeaderSize = 14;
        const int bitmapInfoHeaderSize = 40;
        const int bitmapColorMaskSize = 12;
        const int bytesPerPixel = 4;

        var rowBytes = checked(width * bytesPerPixel);
        var imageSize = checked(rowBytes * height);
        var pixelDataOffset = bitmapFileHeaderSize + bitmapInfoHeaderSize + bitmapColorMaskSize;
        var fileSize = checked(pixelDataOffset + imageSize);

        var histogram = new int[16];
        var rowAllBlack = new bool[height];
        var columnAllBlack = new bool[width];
        Array.Fill(rowAllBlack, true);
        Array.Fill(columnAllBlack, true);

        long sumR = 0;
        long sumG = 0;
        long sumB = 0;
        double sumLuminance = 0;
        double minLuminance = 255;
        double maxLuminance = 0;
        long nearBlackCount = 0;
        long nearWhiteCount = 0;
        long pureBlackCount = 0;
        var totalPixels = (long)width * height;

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var rowBuffer = ArrayPool<byte>.Shared.Rent(rowBytes);
        try
        {
            using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new BinaryWriter(fileStream, System.Text.Encoding.ASCII, leaveOpen: false);
            WriteBitmapHeaders(writer, fileSize, pixelDataOffset, width, height, imageSize);

            for (var y = 0; y < height; y++)
            {
                var sourceRow = IntPtr.Add(mapped.DataPointer, checked(y * (int)mapped.RowPitch));
                Marshal.Copy(sourceRow, rowBuffer, 0, rowBytes);

                if (backBufferFormat == Format.R10G10B10A2_UNorm)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var offset = x * bytesPerPixel;
                        var pixel = (uint)(rowBuffer[offset] |
                                           (rowBuffer[offset + 1] << 8) |
                                           (rowBuffer[offset + 2] << 16) |
                                           (rowBuffer[offset + 3] << 24));
                        rowBuffer[offset] = (byte)(((pixel >> 20) & 0x3FFu) >> 2);
                        rowBuffer[offset + 1] = (byte)(((pixel >> 10) & 0x3FFu) >> 2);
                        rowBuffer[offset + 2] = (byte)((pixel & 0x3FFu) >> 2);
                        rowBuffer[offset + 3] = 255;
                    }
                }

                writer.Write(rowBuffer, 0, rowBytes);

                var isRowPureBlack = true;
                for (var x = 0; x < width; x++)
                {
                    var offset = x * bytesPerPixel;
                    var b = rowBuffer[offset];
                    var g = rowBuffer[offset + 1];
                    var r = rowBuffer[offset + 2];

                    sumR += r;
                    sumG += g;
                    sumB += b;

                    var luminance = (0.299 * r) + (0.587 * g) + (0.114 * b);
                    sumLuminance += luminance;
                    if (luminance < minLuminance)
                    {
                        minLuminance = luminance;
                    }
                    if (luminance > maxLuminance)
                    {
                        maxLuminance = luminance;
                    }

                    if (luminance < 16.0)
                    {
                        nearBlackCount++;
                    }
                    if (luminance > 240.0)
                    {
                        nearWhiteCount++;
                    }

                    var isPureBlack = r == 0 && g == 0 && b == 0;
                    if (isPureBlack)
                    {
                        pureBlackCount++;
                    }
                    else
                    {
                        isRowPureBlack = false;
                        columnAllBlack[x] = false;
                    }

                    var histogramIndex = (int)(luminance / 16.0);
                    if (histogramIndex > 15)
                    {
                        histogramIndex = 15;
                    }
                    histogram[histogramIndex]++;
                }

                rowAllBlack[y] = isRowPureBlack;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rowBuffer);
        }

        var letterboxTopRows = CountLeadingBlackEdges(rowAllBlack);
        var letterboxBottomRows = letterboxTopRows == height ? 0 : CountTrailingBlackEdges(rowAllBlack);
        var pillarboxLeftCols = CountLeadingBlackEdges(columnAllBlack);
        var pillarboxRightCols = pillarboxLeftCols == width ? 0 : CountTrailingBlackEdges(columnAllBlack);

        var contentWidth = Math.Max(0, width - pillarboxLeftCols - pillarboxRightCols);
        var contentHeight = Math.Max(0, height - letterboxTopRows - letterboxBottomRows);
        var contentAspectRatio = contentHeight > 0
            ? (double)contentWidth / contentHeight
            : 0.0;

        var averageR = totalPixels > 0 ? (double)sumR / totalPixels : 0.0;
        var averageG = totalPixels > 0 ? (double)sumG / totalPixels : 0.0;
        var averageB = totalPixels > 0 ? (double)sumB / totalPixels : 0.0;
        var averageLuminance = totalPixels > 0 ? sumLuminance / totalPixels : 0.0;
        var nearBlackPercent = totalPixels > 0 ? (nearBlackCount * 100.0) / totalPixels : 0.0;
        var nearWhitePercent = totalPixels > 0 ? (nearWhiteCount * 100.0) / totalPixels : 0.0;
        var pureBlackPercent = totalPixels > 0 ? (pureBlackCount * 100.0) / totalPixels : 0.0;

        return new PreviewFrameCaptureResult
        {
            Succeeded = true,
            Message = "Preview frame captured.",
            FilePath = outputPath,
            CapturedWidth = width,
            CapturedHeight = height,
            RendererMode = rendererMode,
            AverageR = averageR,
            AverageG = averageG,
            AverageB = averageB,
            AverageLuminance = averageLuminance,
            MinLuminance = minLuminance,
            MaxLuminance = maxLuminance,
            NearBlackPercent = nearBlackPercent,
            NearWhitePercent = nearWhitePercent,
            PureBlackPercent = pureBlackPercent,
            LetterboxTopRows = letterboxTopRows,
            LetterboxBottomRows = letterboxBottomRows,
            PillarboxLeftCols = pillarboxLeftCols,
            PillarboxRightCols = pillarboxRightCols,
            ContentWidth = contentWidth,
            ContentHeight = contentHeight,
            ContentAspectRatio = contentAspectRatio,
            LuminanceHistogram = histogram,
            TotalPixels = totalPixels
        };
    }

    private static byte[] CopyMappedFrameToBuffer(MappedSubresource mapped, int height, int sourceRowBytes)
    {
        var sourceBuffer = new byte[checked(sourceRowBytes * height)];
        for (var y = 0; y < height; y++)
        {
            var sourceRow = IntPtr.Add(mapped.DataPointer, checked(y * (int)mapped.RowPitch));
            Marshal.Copy(sourceRow, sourceBuffer, checked(y * sourceRowBytes), sourceRowBytes);
        }

        return sourceBuffer;
    }

    private static PreviewFrameCaptureResult CaptureFrameBufferTo16BitPng(
        byte[] sourceBuffer,
        int sourceRowBytes,
        int width,
        int height,
        string outputPath,
        string rendererMode,
        Format backBufferFormat)
    {
        const int sourceBytesPerPixel = 4;
        const int pngBytesPerPixel = 6;
        var pngRowBytes = checked(1 + (width * pngBytesPerPixel));

        var histogram = new int[16];
        var rowAllBlack = new bool[height];
        var columnAllBlack = new bool[width];
        Array.Fill(rowAllBlack, true);
        Array.Fill(columnAllBlack, true);

        long sumR = 0;
        long sumG = 0;
        long sumB = 0;
        double sumLuminance = 0;
        double minLuminance = 255;
        double maxLuminance = 0;
        long nearBlackCount = 0;
        long nearWhiteCount = 0;
        long pureBlackCount = 0;
        var totalPixels = (long)width * height;

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var sourceRowBuffer = ArrayPool<byte>.Shared.Rent(sourceRowBytes);
        var pngRowBuffer = ArrayPool<byte>.Shared.Rent(pngRowBytes);
        try
        {
            using (var compressedDataStream = new MemoryStream())
            {
                using (var zlibStream = new ZLibStream(compressedDataStream, CompressionLevel.Fastest, leaveOpen: true))
                {
                    for (var y = 0; y < height; y++)
                    {
                        var sourceRowOffset = checked(y * sourceRowBytes);
                        Buffer.BlockCopy(sourceBuffer, sourceRowOffset, sourceRowBuffer, 0, sourceRowBytes);

                        pngRowBuffer[0] = 0;
                        var pngOffset = 1;
                        var isRowPureBlack = true;

                        for (var x = 0; x < width; x++)
                        {
                            var offset = x * sourceBytesPerPixel;
                            byte r8;
                            byte g8;
                            byte b8;
                            ushort r16;
                            ushort g16;
                            ushort b16;

                            if (backBufferFormat == Format.R10G10B10A2_UNorm)
                            {
                                var pixel = (uint)(sourceRowBuffer[offset] |
                                                   (sourceRowBuffer[offset + 1] << 8) |
                                                   (sourceRowBuffer[offset + 2] << 16) |
                                                   (sourceRowBuffer[offset + 3] << 24));
                                var r10 = pixel & 0x3FFu;
                                var g10 = (pixel >> 10) & 0x3FFu;
                                var b10 = (pixel >> 20) & 0x3FFu;

                                r8 = (byte)(r10 >> 2);
                                g8 = (byte)(g10 >> 2);
                                b8 = (byte)(b10 >> 2);
                                r16 = (ushort)((r10 << 6) | (r10 >> 4));
                                g16 = (ushort)((g10 << 6) | (g10 >> 4));
                                b16 = (ushort)((b10 << 6) | (b10 >> 4));
                            }
                            else if (backBufferFormat == Format.B8G8R8A8_UNorm)
                            {
                                b8 = sourceRowBuffer[offset];
                                g8 = sourceRowBuffer[offset + 1];
                                r8 = sourceRowBuffer[offset + 2];
                                b16 = (ushort)((b8 << 8) | b8);
                                g16 = (ushort)((g8 << 8) | g8);
                                r16 = (ushort)((r8 << 8) | r8);
                            }
                            else
                            {
                                throw new InvalidOperationException($"Preview PNG capture does not support back buffer format {backBufferFormat}.");
                            }

                            pngRowBuffer[pngOffset++] = (byte)(r16 >> 8);
                            pngRowBuffer[pngOffset++] = (byte)r16;
                            pngRowBuffer[pngOffset++] = (byte)(g16 >> 8);
                            pngRowBuffer[pngOffset++] = (byte)g16;
                            pngRowBuffer[pngOffset++] = (byte)(b16 >> 8);
                            pngRowBuffer[pngOffset++] = (byte)b16;

                            sumR += r8;
                            sumG += g8;
                            sumB += b8;

                            var luminance = (0.299 * r8) + (0.587 * g8) + (0.114 * b8);
                            sumLuminance += luminance;
                            if (luminance < minLuminance)
                            {
                                minLuminance = luminance;
                            }
                            if (luminance > maxLuminance)
                            {
                                maxLuminance = luminance;
                            }

                            if (luminance < 16.0)
                            {
                                nearBlackCount++;
                            }
                            if (luminance > 240.0)
                            {
                                nearWhiteCount++;
                            }

                            var isPureBlack = r8 == 0 && g8 == 0 && b8 == 0;
                            if (isPureBlack)
                            {
                                pureBlackCount++;
                            }
                            else
                            {
                                isRowPureBlack = false;
                                columnAllBlack[x] = false;
                            }

                            var histogramIndex = (int)(luminance / 16.0);
                            if (histogramIndex > 15)
                            {
                                histogramIndex = 15;
                            }

                            histogram[histogramIndex]++;
                        }

                        rowAllBlack[y] = isRowPureBlack;
                        zlibStream.Write(pngRowBuffer, 0, pngRowBytes);
                    }
                }

                using var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                using var writer = new BinaryWriter(fileStream, System.Text.Encoding.ASCII, leaveOpen: false);

                writer.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

                var ihdrData = new byte[13];
                BinaryPrimitives.WriteUInt32BigEndian(ihdrData.AsSpan(0, 4), checked((uint)width));
                BinaryPrimitives.WriteUInt32BigEndian(ihdrData.AsSpan(4, 4), checked((uint)height));
                ihdrData[8] = 16;
                ihdrData[9] = 2;
                ihdrData[10] = 0;
                ihdrData[11] = 0;
                ihdrData[12] = 0;

                WritePngChunk(writer, new byte[] { (byte)'I', (byte)'H', (byte)'D', (byte)'R' }, ihdrData);
                if (compressedDataStream.TryGetBuffer(out var compressedData))
                {
                    WritePngChunk(
                        writer,
                        new byte[] { (byte)'I', (byte)'D', (byte)'A', (byte)'T' },
                        compressedData.Array!,
                        compressedData.Offset,
                        checked((int)compressedDataStream.Length));
                }
                else
                {
                    WritePngChunk(writer, new byte[] { (byte)'I', (byte)'D', (byte)'A', (byte)'T' }, compressedDataStream.ToArray());
                }

                WritePngChunk(writer, new byte[] { (byte)'I', (byte)'E', (byte)'N', (byte)'D' }, Array.Empty<byte>());
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pngRowBuffer);
            ArrayPool<byte>.Shared.Return(sourceRowBuffer);
        }

        var letterboxTopRows = CountLeadingBlackEdges(rowAllBlack);
        var letterboxBottomRows = letterboxTopRows == height ? 0 : CountTrailingBlackEdges(rowAllBlack);
        var pillarboxLeftCols = CountLeadingBlackEdges(columnAllBlack);
        var pillarboxRightCols = pillarboxLeftCols == width ? 0 : CountTrailingBlackEdges(columnAllBlack);

        var contentWidth = Math.Max(0, width - pillarboxLeftCols - pillarboxRightCols);
        var contentHeight = Math.Max(0, height - letterboxTopRows - letterboxBottomRows);
        var contentAspectRatio = contentHeight > 0
            ? (double)contentWidth / contentHeight
            : 0.0;

        var averageR = totalPixels > 0 ? (double)sumR / totalPixels : 0.0;
        var averageG = totalPixels > 0 ? (double)sumG / totalPixels : 0.0;
        var averageB = totalPixels > 0 ? (double)sumB / totalPixels : 0.0;
        var averageLuminance = totalPixels > 0 ? sumLuminance / totalPixels : 0.0;
        var nearBlackPercent = totalPixels > 0 ? (nearBlackCount * 100.0) / totalPixels : 0.0;
        var nearWhitePercent = totalPixels > 0 ? (nearWhiteCount * 100.0) / totalPixels : 0.0;
        var pureBlackPercent = totalPixels > 0 ? (pureBlackCount * 100.0) / totalPixels : 0.0;

        return new PreviewFrameCaptureResult
        {
            Succeeded = true,
            Message = "Preview frame captured.",
            FilePath = outputPath,
            CapturedWidth = width,
            CapturedHeight = height,
            RendererMode = rendererMode,
            AverageR = averageR,
            AverageG = averageG,
            AverageB = averageB,
            AverageLuminance = averageLuminance,
            MinLuminance = minLuminance,
            MaxLuminance = maxLuminance,
            NearBlackPercent = nearBlackPercent,
            NearWhitePercent = nearWhitePercent,
            PureBlackPercent = pureBlackPercent,
            LetterboxTopRows = letterboxTopRows,
            LetterboxBottomRows = letterboxBottomRows,
            PillarboxLeftCols = pillarboxLeftCols,
            PillarboxRightCols = pillarboxRightCols,
            ContentWidth = contentWidth,
            ContentHeight = contentHeight,
            ContentAspectRatio = contentAspectRatio,
            LuminanceHistogram = histogram,
            TotalPixels = totalPixels
        };
    }

    private static void WriteBitmapHeaders(
        BinaryWriter writer,
        int fileSize,
        int pixelDataOffset,
        int width,
        int height,
        int imageSize)
    {
        writer.Write((byte)'B');
        writer.Write((byte)'M');
        writer.Write(fileSize);
        writer.Write((short)0);
        writer.Write((short)0);
        writer.Write(pixelDataOffset);

        writer.Write(40);
        writer.Write(width);
        writer.Write(-height);
        writer.Write((short)1);
        writer.Write((short)32);
        writer.Write(3);
        writer.Write(imageSize);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);
        writer.Write(0);

        writer.Write(unchecked((int)0x00FF0000));
        writer.Write(unchecked((int)0x0000FF00));
        writer.Write(unchecked((int)0x000000FF));
    }

    private static uint[] InitPngCrc32Table()
    {
        var table = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            var c = n;
            for (var k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }

    private static uint ComputePngCrc32(byte[] buffer, int offset, int length)
    {
        return UpdatePngCrc32(0xFFFFFFFFu, buffer, offset, length) ^ 0xFFFFFFFFu;
    }

    private static uint UpdatePngCrc32(uint crc, byte[] buffer, int offset, int length)
    {
        for (var i = offset; i < offset + length; i++)
        {
            crc = PngCrc32Table[(crc ^ buffer[i]) & 0xFF] ^ (crc >> 8);
        }

        return crc;
    }

    private static void WritePngChunk(BinaryWriter writer, byte[] chunkType, byte[] data)
    {
        WritePngChunk(writer, chunkType, data, 0, data.Length);
    }

    private static void WritePngChunk(BinaryWriter writer, byte[] chunkType, byte[] data, int dataOffset, int dataLength)
    {
        writer.Write(BinaryPrimitives.ReverseEndianness(checked((uint)dataLength)));
        writer.Write(chunkType);
        if (dataLength > 0)
        {
            writer.Write(data, dataOffset, dataLength);
        }

        var crc = 0xFFFFFFFFu;
        crc = UpdatePngCrc32(crc, chunkType, 0, chunkType.Length);
        if (dataLength > 0)
        {
            crc = UpdatePngCrc32(crc, data, dataOffset, dataLength);
        }

        writer.Write(BinaryPrimitives.ReverseEndianness(crc ^ 0xFFFFFFFFu));
    }

    private static int CountLeadingBlackEdges(bool[] values)
    {
        var count = 0;
        while (count < values.Length && values[count])
        {
            count++;
        }

        return count;
    }

    private static int CountTrailingBlackEdges(bool[] values)
    {
        var count = 0;
        while (count < values.Length && values[values.Length - 1 - count])
        {
            count++;
        }

        return count;
    }

    private static PreviewFrameCaptureResult CreateFrameCaptureError(string message, string rendererMode = "Unknown")
    {
        return new PreviewFrameCaptureResult
        {
            Succeeded = false,
            Message = message,
            RendererMode = rendererMode,
            LuminanceHistogram = new int[16]
        };
    }

    private void FailPendingFrameCapture(string message)
    {
        var request = Interlocked.Exchange(ref _frameCaptureRequest, null);
        Interlocked.Exchange(ref _frameCaptureOutputPath, null);
        if (request == null)
        {
            return;
        }

        request.TrySetResult(CreateFrameCaptureError(message));
        Logger.Log($"PREVIEW_FRAME_CAPTURE_ABORTED reason={message}");
    }

    private bool UploadFrameToInputTexture(PendingFrame frame)
    {
        if (_deviceContext == null || _inputTexture == null || _stagingTexture == null)
        {
            return false;
        }

        if (frame.RawData != null)
        {
            return UploadRawFrame(frame.RawData, frame.RawDataLength, frame.Width, frame.Height, frame.IsHdr);
        }

        return false;
    }

    private bool TryResolveInputView(PendingFrame frame, out ID3D11VideoProcessorInputView? inputView, out bool disposeInputView)
    {
        inputView = null;
        disposeInputView = false;

        if (frame.D3DTexture != null)
        {
            inputView = CreateInputViewFromTexture(frame.D3DTexture, frame.D3DSubresourceIndex);
            disposeInputView = true;
            return true;
        }

        if (!UploadFrameToInputTexture(frame))
        {
            return false;
        }

        inputView = _inputView;
        return inputView != null;
    }

    private ID3D11VideoProcessorInputView CreateInputViewFromTexture(ID3D11Texture2D texture, int subresourceIndex)
    {
        if (_videoDevice == null || _videoProcessorEnumerator == null)
        {
            throw new InvalidOperationException("D3D11 preview pipeline is not ready for external texture input.");
        }

        var textureDescription = texture.Description;
        var mipLevels = Math.Max(1, (int)textureDescription.MipLevels);
        var arraySize = Math.Max(1, (int)textureDescription.ArraySize);
        var safeSubresource = Math.Max(0, subresourceIndex);
        var arraySlice = Math.Clamp(safeSubresource / mipLevels, 0, arraySize - 1);
        var mipSlice = Math.Clamp(safeSubresource % mipLevels, 0, mipLevels - 1);
        var inputViewDescription = new VideoProcessorInputViewDescription
        {
            ViewDimension = VideoProcessorInputViewDimension.Texture2D,
            Texture2D = new Texture2DVideoProcessorInputView
            {
                MipSlice = (uint)mipSlice,
                ArraySlice = (uint)arraySlice
            }
        };

        return _videoDevice.CreateVideoProcessorInputView(texture, _videoProcessorEnumerator, inputViewDescription);
    }

    private unsafe bool UploadRawFrame(byte[] data, int dataLength, int width, int height, bool isHdr)
    {
        if (_deviceContext == null || _stagingTexture == null || _inputTexture == null)
        {
            return false;
        }

        var expectedBytes = isHdr ? width * height * 3 : (width * height * 3) / 2;
        if (dataLength < expectedBytes)
        {
            Logger.Log(
                $"D3D11 preview raw frame too small: expected={expectedBytes} actual={dataLength} hdr={isHdr}.");
            return false;
        }

        var rowBytes = isHdr ? width * 2 : width;
        var uvRows = height / 2;

        fixed (byte* srcStart = data)
        {
            var srcY = srcStart;
            var srcUv = srcStart + (rowBytes * height);

            _deviceContext.Map(_stagingTexture, 0, MapMode.Write, Vortice.Direct3D11.MapFlags.None, out var mapped);
            try
            {
                var dstY = (byte*)mapped.DataPointer;
                var dstUv = dstY + (mapped.RowPitch * height);

                for (var row = 0; row < height; row++)
                {
                    Buffer.MemoryCopy(
                        srcY + (row * rowBytes),
                        dstY + (row * mapped.RowPitch),
                        mapped.RowPitch,
                        rowBytes);
                }

                for (var row = 0; row < uvRows; row++)
                {
                    Buffer.MemoryCopy(
                        srcUv + (row * rowBytes),
                        dstUv + (row * mapped.RowPitch),
                        mapped.RowPitch,
                        rowBytes);
                }
            }
            finally
            {
                _deviceContext.Unmap(_stagingTexture, 0);
            }
        }

        _deviceContext.CopyResource(_inputTexture, _stagingTexture);
        return true;
    }

    private void InitializeD3D()
    {
        CleanupD3DResources();

        var sharedDeviceActive = TryInitializeWithSharedDevice(out var featureLevel);
        if (!sharedDeviceActive)
        {
            CreateRendererOwnedDevice(out featureLevel);
        }

        if (_device == null || _deviceContext == null)
        {
            throw new InvalidOperationException("D3D11 device initialization did not produce a valid device/context.");
        }

        var device = _device;
        var deviceContext = _deviceContext;
        _device3?.Dispose();
        _device3 = device.QueryInterfaceOrNull<ID3D11Device3>();
        Interlocked.Exchange(ref _sharedDeviceActive, sharedDeviceActive ? 1 : 0);

        _multithread = device.QueryInterfaceOrNull<ID3D11Multithread>();
        _multithread?.SetMultithreadProtected(true);

        _videoDevice = device.QueryInterfaceOrNull<ID3D11VideoDevice>();
        _videoContext = deviceContext.QueryInterfaceOrNull<ID3D11VideoContext>();
        _videoContext1 = deviceContext.QueryInterfaceOrNull<ID3D11VideoContext1>();
        if (_videoDevice == null || _videoContext == null || _videoContext1 == null)
        {
            throw new InvalidOperationException("D3D11 video interfaces are unavailable.");
        }

        var factoryResult = DXGI.CreateDXGIFactory2(false, out _factory);
        if (factoryResult.Failure || _factory == null)
        {
            throw new InvalidOperationException($"CreateDXGIFactory2 failed: 0x{factoryResult.Code:X8}.");
        }

        var pixelWidth = Math.Max(1, Volatile.Read(ref _startupWidth));
        var pixelHeight = Math.Max(1, Volatile.Read(ref _startupHeight));

        var swapChainFormat = Format.B8G8R8A8_UNorm;
        _hdrCapableSwapChain = false;
        _swapChainIsHdr10 = false;
        _swapChain3?.Dispose();
        _swapChain3 = null;

        if (_configuredHdr)
        {
            swapChainFormat = Format.R10G10B10A2_UNorm;
        }

        var swapChainDescription = new SwapChainDescription1(
            (uint)pixelWidth,
            (uint)pixelHeight,
            swapChainFormat,
            false,
            Usage.RenderTargetOutput,
            2,
            Scaling.Stretch,
            SwapEffect.FlipSequential,
            AlphaMode.Ignore,
            SwapChainFlags.None);

        _swapChain = _factory.CreateSwapChainForComposition(device, swapChainDescription, null);
        if (_configuredHdr)
        {
            _swapChain3 = _swapChain.QueryInterfaceOrNull<IDXGISwapChain3>();
            if (_swapChain3 != null)
            {
                var srgbSupport = _swapChain3.CheckColorSpaceSupport(ColorSpaceType.RgbFullG22NoneP709);
                var hdr10Support = _swapChain3.CheckColorSpaceSupport(ColorSpaceType.RgbFullG2084NoneP2020);

                var srgbOk = (srgbSupport & SwapChainColorSpaceSupportFlags.Present) != 0;
                var hdr10Ok = (hdr10Support & SwapChainColorSpaceSupportFlags.Present) != 0;

                if (srgbOk && hdr10Ok)
                {
                    _hdrCapableSwapChain = true;
                    var wantHdr = Volatile.Read(ref _hdrPassthroughEnabled) != 0;
                    var initialColorSpace = wantHdr
                        ? ColorSpaceType.RgbFullG2084NoneP2020
                        : ColorSpaceType.RgbFullG22NoneP709;
                    _swapChain3.SetColorSpace1(initialColorSpace);
                    _swapChainIsHdr10 = wantHdr;
                    _outputColorSpaceLabel = wantHdr ? "HDR10-PQ (BT.2020)" : "sRGB (BT.709)";
                    Interlocked.Exchange(ref _swapChainColorSpaceDirty, 0);
                    Logger.Log($"D3D11 preview HDR-capable swap chain: srgb={srgbOk} hdr10={hdr10Ok} initial={initialColorSpace}.");
                }
                else
                {
                    Logger.Log($"D3D11 preview HDR color space check: srgb={srgbOk}({srgbSupport}) hdr10={hdr10Ok}({hdr10Support}). Falling back to B8G8R8A8.");
                    _swapChain3.Dispose();
                    _swapChain3 = null;
                    _swapChain.Dispose();
                    swapChainDescription = new SwapChainDescription1(
                        (uint)pixelWidth,
                        (uint)pixelHeight,
                        Format.B8G8R8A8_UNorm,
                        false,
                        Usage.RenderTargetOutput,
                        2,
                        Scaling.Stretch,
                        SwapEffect.FlipSequential,
                        AlphaMode.Ignore,
                        SwapChainFlags.None);
                    _swapChain = _factory.CreateSwapChainForComposition(device, swapChainDescription, null);
                }
            }
            else
            {
                Logger.Log("D3D11 preview IDXGISwapChain3 unavailable — HDR passthrough not supported.");
                _swapChain.Dispose();
                swapChainDescription = new SwapChainDescription1(
                    (uint)pixelWidth,
                    (uint)pixelHeight,
                    Format.B8G8R8A8_UNorm,
                    false,
                    Usage.RenderTargetOutput,
                    2,
                    Scaling.Stretch,
                    SwapEffect.FlipSequential,
                    AlphaMode.Ignore,
                    SwapChainFlags.None);
                _swapChain = _factory.CreateSwapChainForComposition(device, swapChainDescription, null);
            }
        }

        _configuredOutputWidth = pixelWidth;
        _configuredOutputHeight = pixelHeight;
        ApplyCompositionScaleTransform(_swapChain);
        BindSwapChainToPanel(_swapChain);
        CompileTonemapShaders();

        Logger.Log($"D3D11 preview device created featureLevel={featureLevel} shared={sharedDeviceActive}.");
        Logger.Log($"D3D11 preview swap chain created width={pixelWidth} height={pixelHeight}.");
    }

    private bool TryInitializeWithSharedDevice(out FeatureLevel featureLevel)
    {
        featureLevel = FeatureLevel.Level_11_0;

        ID3D11Device? sharedDevice = null;
        lock (_lifecycleLock)
        {
            if (_sharedDevice == null || _sharedDevice.NativePointer == IntPtr.Zero)
            {
                return false;
            }

            Marshal.AddRef(_sharedDevice.NativePointer);
            sharedDevice = new ID3D11Device(_sharedDevice.NativePointer);
        }

        try
        {
            _device = sharedDevice;
            sharedDevice = null;
            _deviceContext = _device.ImmediateContext;
            if (_deviceContext == null)
            {
                throw new InvalidOperationException("Shared D3D11 device returned a null immediate context.");
            }

            featureLevel = _device.FeatureLevel;
            return true;
        }
        catch (Exception ex)
        {
            sharedDevice?.Dispose();
            _deviceContext?.Dispose();
            _deviceContext = null;
            _device?.Dispose();
            _device = null;
            Logger.Log($"D3D11 shared device init failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}; falling back to renderer-owned device.");
            return false;
        }
    }

    private void CreateRendererOwnedDevice(out FeatureLevel featureLevel)
    {
        var featureLevels = new[] { FeatureLevel.Level_11_0 };
        var flags = DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport;

        var result = D3D11.D3D11CreateDevice(
            adapter: null,
            DriverType.Hardware,
            flags,
            featureLevels,
            out _device,
            out featureLevel,
            out _deviceContext);

        if (result.Failure)
        {
            Logger.Log($"D3D11 hardware device creation failed: 0x{result.Code:X8}. Falling back to WARP.");
            result = D3D11.D3D11CreateDevice(
                adapter: null,
                DriverType.Warp,
                flags,
                featureLevels,
                out _device,
                out featureLevel,
                out _deviceContext);
        }

        if (result.Failure || _device == null || _deviceContext == null)
        {
            throw new InvalidOperationException($"D3D11CreateDevice failed: 0x{result.Code:X8}.");
        }
    }

    private unsafe void CompileTonemapShaders()
    {
        _fullscreenVS?.Dispose();
        _fullscreenVS = null;
        _hdrTonemapPS?.Dispose();
        _hdrTonemapPS = null;
        _hdrPassthroughPS?.Dispose();
        _hdrPassthroughPS = null;
        _linearSampler?.Dispose();
        _linearSampler = null;
        _viewportCB?.Dispose();
        _viewportCB = null;

        if (_device == null)
        {
            return;
        }

        try
        {
            var vertexShaderBytecode = CompileShader(FullscreenVertexShaderSource, "main", "vs_5_0");
            var pixelShaderBytecode = CompileShader(HdrTonemapPixelShaderSource, "main", "ps_5_0");
            var passthroughBytecode = CompileShader(HdrPassthroughPixelShaderSource, "main", "ps_5_0");

            fixed (byte* vertexShaderPtr = vertexShaderBytecode)
            {
                _fullscreenVS = _device.CreateVertexShader(vertexShaderPtr, (nuint)vertexShaderBytecode.Length, null);
            }

            fixed (byte* pixelShaderPtr = pixelShaderBytecode)
            {
                _hdrTonemapPS = _device.CreatePixelShader(pixelShaderPtr, (nuint)pixelShaderBytecode.Length, null);
            }

            fixed (byte* passthroughPtr = passthroughBytecode)
            {
                _hdrPassthroughPS = _device.CreatePixelShader(passthroughPtr, (nuint)passthroughBytecode.Length, null);
            }

            var samplerDescription = new SamplerDescription
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                MipLODBias = 0.0f,
                MaxAnisotropy = 1,
                ComparisonFunc = ComparisonFunction.Never,
                BorderColor = default,
                MinLOD = 0.0f,
                MaxLOD = float.MaxValue
            };

            _linearSampler = _device.CreateSamplerState(samplerDescription);

            _viewportCB?.Dispose();
            _viewportCB = _device.CreateBuffer(new BufferDescription(
                16, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));

            Logger.Log($"D3D11 HDR shaders compiled (VS={vertexShaderBytecode.Length}b TonemapPS={pixelShaderBytecode.Length}b PassthroughPS={passthroughBytecode.Length}b).");
        }
        catch (Exception ex)
        {
            _fullscreenVS?.Dispose();
            _fullscreenVS = null;
            _hdrTonemapPS?.Dispose();
            _hdrTonemapPS = null;
            _hdrPassthroughPS?.Dispose();
            _hdrPassthroughPS = null;
            _linearSampler?.Dispose();
            _linearSampler = null;
            _viewportCB?.Dispose();
            _viewportCB = null;
            Logger.Log($"D3D11 HDR tonemap shader compile failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
        }
    }

    private static byte[] CompileShader(string hlslSource, string entryPoint, string profile)
    {
        var sourceBytes = System.Text.Encoding.UTF8.GetBytes(hlslSource);
        var shaderBlob = IntPtr.Zero;
        var errorBlob = IntPtr.Zero;
        try
        {
            var hr = D3DCompileNative(
                sourceBytes,
                (IntPtr)sourceBytes.Length,
                null,
                IntPtr.Zero,
                IntPtr.Zero,
                entryPoint,
                profile,
                0,
                0,
                out shaderBlob,
                out errorBlob);

            if (hr < 0)
            {
                var errors = ReadBlobString(errorBlob);
                throw new InvalidOperationException(
                    $"D3DCompile failed entry={entryPoint} target={profile} hr=0x{hr:X8} errors={errors}");
            }

            if (shaderBlob == IntPtr.Zero)
            {
                throw new InvalidOperationException($"D3DCompile returned an empty blob for entry={entryPoint} target={profile}.");
            }

            return ReadBlobBytes(shaderBlob);
        }
        finally
        {
            if (shaderBlob != IntPtr.Zero)
            {
                Marshal.Release(shaderBlob);
            }

            if (errorBlob != IntPtr.Zero)
            {
                Marshal.Release(errorBlob);
            }
        }
    }

    private static byte[] ReadBlobBytes(IntPtr blobPtr)
    {
        if (blobPtr == IntPtr.Zero)
        {
            return Array.Empty<byte>();
        }

        ID3DBlob? blob = null;
        try
        {
            blob = (ID3DBlob)Marshal.GetObjectForIUnknown(blobPtr);
            var length = checked((int)blob.GetBufferSize().ToInt64());
            if (length <= 0)
            {
                return Array.Empty<byte>();
            }

            var bytes = new byte[length];
            Marshal.Copy(blob.GetBufferPointer(), bytes, 0, length);
            return bytes;
        }
        finally
        {
            if (blob != null)
            {
                Marshal.ReleaseComObject(blob);
            }
        }
    }

    private static string ReadBlobString(IntPtr blobPtr)
    {
        if (blobPtr == IntPtr.Zero)
        {
            return string.Empty;
        }

        ID3DBlob? blob = null;
        try
        {
            blob = (ID3DBlob)Marshal.GetObjectForIUnknown(blobPtr);
            var length = checked((int)blob.GetBufferSize().ToInt64());
            if (length <= 0)
            {
                return string.Empty;
            }

            var bytes = new byte[length];
            Marshal.Copy(blob.GetBufferPointer(), bytes, 0, length);
            return System.Text.Encoding.ASCII.GetString(bytes).TrimEnd('\0', '\r', '\n');
        }
        finally
        {
            if (blob != null)
            {
                Marshal.ReleaseComObject(blob);
            }
        }
    }

    private void EnsurePipeline(int width, int height, bool isHdr, bool useExternalTexture)
    {
        if (_swapChain == null || _videoDevice == null || _videoContext == null || _videoContext1 == null)
        {
            throw new InvalidOperationException("D3D11 preview pipeline is not initialized.");
        }

        // Keep the internal render target aligned with the source-sized swap chain.
        var outputWidth = _configuredOutputWidth > 0
            ? _configuredOutputWidth
            : Math.Max(1, Volatile.Read(ref _startupWidth));
        var outputHeight = _configuredOutputHeight > 0
            ? _configuredOutputHeight
            : Math.Max(1, Volatile.Read(ref _startupHeight));
        var needRecreate = _videoProcessor == null ||
                           _videoProcessorEnumerator == null ||
                           _configuredInputWidth != width ||
                           _configuredInputHeight != height ||
                           _configuredHdr != isHdr;

        if (needRecreate)
        {
            DisposeProcessorResources();

            var fps = Math.Max(1.0, _startupFps);
            var fpsNum = (uint)Math.Max(1, (int)Math.Round(fps * 1000.0));
            var frameRate = new Rational(fpsNum, 1000u);
            var contentDescription = new VideoProcessorContentDescription
            {
                InputFrameFormat = VideoFrameFormat.Progressive,
                InputFrameRate = frameRate,
                InputWidth = (uint)width,
                InputHeight = (uint)height,
                OutputFrameRate = frameRate,
                OutputWidth = (uint)outputWidth,
                OutputHeight = (uint)outputHeight,
                Usage = VideoUsage.PlaybackNormal
            };

            _videoProcessorEnumerator = _videoDevice.CreateVideoProcessorEnumerator(contentDescription);
            _videoProcessor = _videoDevice.CreateVideoProcessor(_videoProcessorEnumerator, 0);
            RecreateOutputView();

            var sourceRect = new Vortice.RawRect(0, 0, width, height);
            var destinationRect = ComputeLetterboxRect(width, height, outputWidth, outputHeight);
            var outputTargetRect = new Vortice.RawRect(0, 0, outputWidth, outputHeight);
            _videoContext.VideoProcessorSetStreamFrameFormat(_videoProcessor, 0, VideoFrameFormat.Progressive);
            _videoContext.VideoProcessorSetStreamAutoProcessingMode(_videoProcessor, 0, false);
            _videoContext.VideoProcessorSetStreamSourceRect(_videoProcessor, 0, true, sourceRect);
            _videoContext.VideoProcessorSetStreamDestRect(_videoProcessor, 0, true, destinationRect);
            _videoContext.VideoProcessorSetOutputTargetRect(_videoProcessor, true, outputTargetRect);
            _videoContext.VideoProcessorSetOutputBackgroundColor(_videoProcessor, false, new VideoColor());
            ApplyColorSpaces(isHdr);

            _configuredInputWidth = width;
            _configuredInputHeight = height;
            _configuredHdr = isHdr;

            Logger.Log($"D3D11 video processor created input={width}x{height} output={outputWidth}x{outputHeight} hdr={isHdr}.");
        }

        if (!useExternalTexture)
        {
            EnsureInputResources(width, height, isHdr);
        }
    }

    private void EnsureInputResources(int width, int height, bool isHdr)
    {
        if (_device == null || _videoDevice == null || _videoProcessorEnumerator == null)
        {
            throw new InvalidOperationException("D3D11 device state is incomplete for input texture creation.");
        }

        var targetFormat = isHdr ? Format.P010 : Format.NV12;
        if (_inputTexture != null &&
            _stagingTexture != null &&
            _inputView != null &&
            _configuredInputWidth == width &&
            _configuredInputHeight == height &&
            _configuredInputFormat == targetFormat)
        {
            return;
        }

        _inputView?.Dispose();
        _inputView = null;
        _inputTexture?.Dispose();
        _inputTexture = null;
        _stagingTexture?.Dispose();
        _stagingTexture = null;

        var inputDescription = new Texture2DDescription(
            targetFormat,
            (uint)width,
            (uint)height,
            1,
            1,
            BindFlags.None,
            ResourceUsage.Default,
            CpuAccessFlags.None,
            1,
            0,
            ResourceOptionFlags.None);

        var stagingDescription = new Texture2DDescription(
            targetFormat,
            (uint)width,
            (uint)height,
            1,
            1,
            BindFlags.None,
            ResourceUsage.Staging,
            CpuAccessFlags.Write,
            1,
            0,
            ResourceOptionFlags.None);

        _inputTexture = _device.CreateTexture2D(inputDescription);
        _stagingTexture = _device.CreateTexture2D(stagingDescription);

        var inputViewDescription = new VideoProcessorInputViewDescription
        {
            ViewDimension = VideoProcessorInputViewDimension.Texture2D,
            Texture2D = new Texture2DVideoProcessorInputView { MipSlice = 0, ArraySlice = 0 }
        };

        _inputView = _videoDevice.CreateVideoProcessorInputView(_inputTexture, _videoProcessorEnumerator, inputViewDescription);
        _configuredInputFormat = targetFormat;
    }

    private void EnsureHdrInputResources(int width, int height)
    {
        if (_device == null)
        {
            throw new InvalidOperationException("D3D11 device state is incomplete for HDR shader input texture creation.");
        }

        if (_hdrInputTexture != null &&
            _hdrStagingTexture != null &&
            _hdrYPlaneSRV != null &&
            _hdrUVPlaneSRV != null &&
            _hdrInputConfiguredWidth == width &&
            _hdrInputConfiguredHeight == height)
        {
            return;
        }

        _hdrYPlaneSRV?.Dispose();
        _hdrYPlaneSRV = null;
        _hdrUVPlaneSRV?.Dispose();
        _hdrUVPlaneSRV = null;
        _hdrInputTexture?.Dispose();
        _hdrInputTexture = null;
        _hdrStagingTexture?.Dispose();
        _hdrStagingTexture = null;

        var inputDescription = new Texture2DDescription(
            Format.P010,
            (uint)width,
            (uint)height,
            1,
            1,
            BindFlags.ShaderResource,
            ResourceUsage.Default,
            CpuAccessFlags.None,
            1,
            0,
            ResourceOptionFlags.None);

        var stagingDescription = new Texture2DDescription(
            Format.P010,
            (uint)width,
            (uint)height,
            1,
            1,
            BindFlags.None,
            ResourceUsage.Staging,
            CpuAccessFlags.Write,
            1,
            0,
            ResourceOptionFlags.None);

        _hdrInputTexture = _device.CreateTexture2D(inputDescription);
        _hdrStagingTexture = _device.CreateTexture2D(stagingDescription);

        _hdrYPlaneSRV = CreateHdrPlaneView(Format.R16_UNorm, planeSlice: 0);
        _hdrUVPlaneSRV = CreateHdrPlaneView(Format.R16G16_UNorm, planeSlice: 1);
        _hdrInputConfiguredWidth = width;
        _hdrInputConfiguredHeight = height;
    }

    private ID3D11ShaderResourceView CreateHdrPlaneView(Format format, uint planeSlice)
    {
        if (_device == null || _hdrInputTexture == null)
        {
            throw new InvalidOperationException("HDR shader input texture has not been created.");
        }

        if (_device3 != null)
        {
            var srvDesc = new ShaderResourceViewDescription1(
                _hdrInputTexture,
                ShaderResourceViewDimension.Texture2D,
                format,
                0,
                1,
                0,
                1,
                planeSlice);

            return _device3.CreateShaderResourceView1(_hdrInputTexture, srvDesc);
        }

        var fallbackDesc = new ShaderResourceViewDescription(
            _hdrInputTexture,
            ShaderResourceViewDimension.Texture2D,
            format,
            0,
            1,
            0,
            1);

        return _device.CreateShaderResourceView(_hdrInputTexture, fallbackDesc);
    }

    private unsafe bool UploadRawFrameToHdrTexture(byte[] data, int dataLength, int width, int height)
    {
        if (_deviceContext == null || _hdrStagingTexture == null || _hdrInputTexture == null)
        {
            return false;
        }

        var rowBytes = width * 2;
        var uvRows = height / 2;
        var expectedBytes = (rowBytes * height) + (rowBytes * uvRows);
        if (dataLength < expectedBytes)
        {
            Logger.Log($"D3D11 preview HDR raw frame too small: expected={expectedBytes} actual={dataLength}.");
            return false;
        }

        fixed (byte* srcStart = data)
        {
            var srcY = srcStart;
            var srcUv = srcStart + (rowBytes * height);

            _deviceContext.Map(_hdrStagingTexture, 0, MapMode.Write, Vortice.Direct3D11.MapFlags.None, out var mapped);
            try
            {
                var dstY = (byte*)mapped.DataPointer;
                var dstUv = dstY + (mapped.RowPitch * height);

                for (var row = 0; row < height; row++)
                {
                    Buffer.MemoryCopy(
                        srcY + (row * rowBytes),
                        dstY + (row * mapped.RowPitch),
                        mapped.RowPitch,
                        rowBytes);
                }

                for (var row = 0; row < uvRows; row++)
                {
                    Buffer.MemoryCopy(
                        srcUv + (row * rowBytes),
                        dstUv + (row * mapped.RowPitch),
                        mapped.RowPitch,
                        rowBytes);
                }
            }
            finally
            {
                _deviceContext.Unmap(_hdrStagingTexture, 0);
            }
        }

        _deviceContext.CopyResource(_hdrInputTexture, _hdrStagingTexture);
        return true;
    }

    private void EnsureSwapChainRTV()
    {
        if (_device == null || _swapChain == null)
        {
            throw new InvalidOperationException("D3D11 preview swap chain render target state is unavailable.");
        }

        if (_swapChainBackBuffer == null)
        {
            _swapChainBackBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        }

        if (_swapChainRTV == null)
        {
            _swapChainRTV = _device.CreateRenderTargetView(_swapChainBackBuffer, null);
        }
    }

    private void RecreateOutputView()
    {
        if (_swapChain == null || _videoDevice == null || _videoProcessorEnumerator == null || _device == null)
        {
            throw new InvalidOperationException("D3D11 output view recreation requires swap chain and video enumerator.");
        }

        _outputView?.Dispose();
        _outputView = null;
        _swapChainRTV?.Dispose();
        _swapChainRTV = null;
        _swapChainBackBuffer?.Dispose();
        _swapChainBackBuffer = null;

        _swapChainBackBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        EnsureSwapChainRTV();
        var outputViewDescription = new VideoProcessorOutputViewDescription
        {
            ViewDimension = VideoProcessorOutputViewDimension.Texture2D,
            Texture2D = new Texture2DVideoProcessorOutputView { MipSlice = 0 }
        };

        _outputView = _videoDevice.CreateVideoProcessorOutputView(_swapChainBackBuffer, _videoProcessorEnumerator, outputViewDescription);
    }

    private void ApplyColorSpaces(bool isHdr)
    {
        if (_videoContext1 == null || _videoProcessor == null) return;

        var fullRange = Volatile.Read(ref _fullRangeInput);
        var inputColorSpace = isHdr
            ? ColorSpaceType.YcbcrStudioG2084LeftP2020
            : fullRange
                ? ColorSpaceType.YcbcrFullG22LeftP709
                : ColorSpaceType.YcbcrStudioG22LeftP709;
        var outputColorSpace = ColorSpaceType.RgbFullG22NoneP709;

        _videoContext1.VideoProcessorSetStreamColorSpace1(_videoProcessor, 0, inputColorSpace);
        _videoContext1.VideoProcessorSetOutputColorSpace1(_videoProcessor, outputColorSpace);

        _inputColorSpaceLabel = inputColorSpace.ToString();
        _outputColorSpaceLabel = outputColorSpace.ToString();
        Logger.Log($"D3D11 preview color space input={_inputColorSpaceLabel} output={_outputColorSpaceLabel} mode=VideoProcessor.");
    }

    private void BindSwapChainToPanel(IDXGISwapChain1 swapChain)
    {
        // ISwapChainPanelNative.SetSwapChain must be called on the UI thread
        // because _panel is a XAML element. Marshal from the render thread.
        using var done = new ManualResetEventSlim(false);
        Exception? uiError = null;

        var enqueued = _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                var panelNative = CastExtensions.As<ISwapChainPanelNative>(_panel);
                panelNative.SetSwapChain(swapChain.NativePointer);
            }
            catch (Exception ex)
            {
                uiError = ex;
            }
            finally
            {
                done.Set();
            }
        });

        if (!enqueued)
        {
            throw new InvalidOperationException("Failed to enqueue swap chain binding to UI thread.");
        }

        if (!done.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("Swap chain binding to UI thread timed out.");
        }

        if (uiError != null)
        {
            throw new InvalidOperationException("Swap chain binding failed on UI thread.", uiError);
        }
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
            swapChain2.MatrixTransform = System.Numerics.Matrix3x2.Identity;
            return;
        }

        var uniformScale = (float)Math.Min(panelLogicalW / swapW, panelLogicalH / swapH);
        var offsetX = (float)((panelLogicalW - swapW * uniformScale) * 0.5);
        var offsetY = (float)((panelLogicalH - swapH * uniformScale) * 0.5);

        swapChain2.MatrixTransform = new System.Numerics.Matrix3x2(
            uniformScale, 0,
            0, uniformScale,
            offsetX, offsetY);

        Logger.Log($"D3D11 preview composition transform set scale={uniformScale:F4} offset=({offsetX:F1},{offsetY:F1}) panel={panelLogicalW:F0}x{panelLogicalH:F0} swap={swapW}x{swapH}.");
    }

    private void HandleDeviceLost(Exception ex)
    {
        Logger.Log($"D3D11 preview device lost ({ex.GetType().Name}); recreating device.");
        CleanupD3DResources();
        var stalePending = Interlocked.Exchange(ref _pendingFrame, null);
        stalePending?.Dispose();
        InitializeD3D();
        Interlocked.Exchange(ref _compositionTransformDirty, 1);
    }

    private void DisposeProcessorResources()
    {
        _inputView?.Dispose();
        _inputView = null;
        _outputView?.Dispose();
        _outputView = null;
        _swapChainRTV?.Dispose();
        _swapChainRTV = null;
        _swapChainBackBuffer?.Dispose();
        _swapChainBackBuffer = null;
        _hdrYPlaneSRV?.Dispose();
        _hdrYPlaneSRV = null;
        _hdrUVPlaneSRV?.Dispose();
        _hdrUVPlaneSRV = null;
        _hdrStagingTexture?.Dispose();
        _hdrStagingTexture = null;
        _hdrInputTexture?.Dispose();
        _hdrInputTexture = null;
        _hdrInputConfiguredWidth = 0;
        _hdrInputConfiguredHeight = 0;
        _videoProcessor?.Dispose();
        _videoProcessor = null;
        _videoProcessorEnumerator?.Dispose();
        _videoProcessorEnumerator = null;
    }

    private void CleanupD3DResources()
    {
        DisposeProcessorResources();

        _stagingTexture?.Dispose();
        _stagingTexture = null;
        _inputTexture?.Dispose();
        _inputTexture = null;
        _swapChainRTV?.Dispose();
        _swapChainRTV = null;
        _swapChainBackBuffer?.Dispose();
        _swapChainBackBuffer = null;
        _swapChain3?.Dispose();
        _swapChain3 = null;
        _swapChain?.Dispose();
        _swapChain = null;
        _factory?.Dispose();
        _factory = null;
        _videoContext1?.Dispose();
        _videoContext1 = null;
        _videoContext?.Dispose();
        _videoContext = null;
        _videoDevice?.Dispose();
        _videoDevice = null;
        _linearSampler?.Dispose();
        _linearSampler = null;
        _viewportCB?.Dispose();
        _viewportCB = null;
        _hdrTonemapPS?.Dispose();
        _hdrTonemapPS = null;
        _hdrPassthroughPS?.Dispose();
        _hdrPassthroughPS = null;
        _fullscreenVS?.Dispose();
        _fullscreenVS = null;
        _multithread?.Dispose();
        _multithread = null;
        _device3?.Dispose();
        _device3 = null;
        _deviceContext?.Dispose();
        _deviceContext = null;
        _device?.Dispose();
        _device = null;

        _configuredInputWidth = 0;
        _configuredInputHeight = 0;
        _configuredOutputWidth = 0;
        _configuredOutputHeight = 0;
        _configuredInputFormat = Format.Unknown;
        _hdrCapableSwapChain = false;
        _swapChainIsHdr10 = false;
        Interlocked.Exchange(ref _sharedDeviceActive, 0);
    }

    private static Vortice.RawRect ComputeLetterboxRect(int srcWidth, int srcHeight, int dstWidth, int dstHeight)
    {
        if (srcWidth <= 0 || srcHeight <= 0 || dstWidth <= 0 || dstHeight <= 0)
        {
            return new Vortice.RawRect(0, 0, dstWidth, dstHeight);
        }

        var srcAspect = (double)srcWidth / srcHeight;
        var dstAspect = (double)dstWidth / dstHeight;

        int fitWidth, fitHeight;
        if (srcAspect > dstAspect)
        {
            // Source is wider â€” letterbox (bars top/bottom)
            fitWidth = dstWidth;
            fitHeight = (int)(dstWidth / srcAspect);
        }
        else
        {
            // Source is taller â€” pillarbox (bars left/right)
            fitHeight = dstHeight;
            fitWidth = (int)(dstHeight * srcAspect);
        }

        var x = (dstWidth - fitWidth) / 2;
        var y = (dstHeight - fitHeight) / 2;
        return new Vortice.RawRect(x, y, x + fitWidth, y + fitHeight);
    }

    private static bool IsDeviceLostException(Exception ex)
    {
        if (ex is SharpGen.Runtime.SharpGenException sharpGenException)
        {
            return sharpGenException.ResultCode == Vortice.DXGI.ResultCode.DeviceRemoved ||
                   sharpGenException.ResultCode == Vortice.DXGI.ResultCode.DeviceReset;
        }

        if (ex is COMException comException)
        {
            return comException.HResult == unchecked((int)0x887A0005) ||
                   comException.HResult == unchecked((int)0x887A0007);
        }

        return false;
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(D3D11PreviewRenderer));
        }
    }
}
