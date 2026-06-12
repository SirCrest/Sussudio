using System;
using System.Runtime.InteropServices;
using System.Threading;
using SharpGen.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Sussudio.Services.Preview;

// Shared D3D11 device plus MF DXGI device-manager handle used by the source
// reader and preview renderer. The manager owns reset/disposal ordering so GPU
// surfaces can be shared without each feature creating its own device.
internal interface ILiveVideoSource
{
    void SuppressPreviewSubmission();
    void ResumePreviewSubmission();
    SharedD3DDeviceManager? D3DManager { get; }
}

internal sealed class SharedD3DDeviceManager : IDisposable
{
    private readonly object _sync = new();
    private ID3D11Device? _device;
    private ID3D11Multithread? _multithread;
    private IntPtr _dxgiDeviceManagerPtr;
    private int _disposed;

    public SharedD3DDeviceManager()
    {
        try
        {
            Initialize();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public ID3D11Device Device
    {
        get
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                return _device ?? throw new ObjectDisposedException(nameof(SharedD3DDeviceManager));
            }
        }
    }

    public IntPtr DxgiDeviceManagerPtr
    {
        get
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                return _dxgiDeviceManagerPtr;
            }
        }
    }

    public ID3D11DeviceContext ImmediateContext
    {
        get
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                return _device?.ImmediateContext
                    ?? throw new ObjectDisposedException(nameof(SharedD3DDeviceManager));
            }
        }
    }

    public uint ResetToken { get; private set; }

    public bool TryCreateDeviceReference(out ID3D11Device? device, out string reason)
    {
        lock (_sync)
        {
            if (Volatile.Read(ref _disposed) != 0)
            {
                device = null;
                reason = "disposed";
                return false;
            }

            var currentDevice = _device;
            if (currentDevice == null)
            {
                device = null;
                reason = "missing_device";
                return false;
            }

            var nativePointer = currentDevice.NativePointer;
            if (nativePointer == IntPtr.Zero)
            {
                device = null;
                reason = "null_device_pointer";
                return false;
            }

            Marshal.AddRef(nativePointer);
            device = new ID3D11Device(nativePointer);
            reason = "ok";
            return true;
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        lock (_sync)
        {
            if (_dxgiDeviceManagerPtr != IntPtr.Zero)
            {
                Marshal.Release(_dxgiDeviceManagerPtr);
                _dxgiDeviceManagerPtr = IntPtr.Zero;
            }

            _multithread?.Dispose();
            _multithread = null;
            _device?.Dispose();
            _device = null;
        }
    }

    private void Initialize()
    {
        var featureLevels = new[] { FeatureLevel.Level_11_0 };
        var flags = DeviceCreationFlags.BgraSupport | DeviceCreationFlags.VideoSupport;

        var result = D3D11.D3D11CreateDevice(
            adapter: null,
            DriverType.Hardware,
            flags,
            featureLevels,
            out var device,
            out var featureLevel,
            out var context);

        context?.Dispose();

        if (result.Failure)
        {
            Logger.Log($"SHARED_D3D_DEVICE_CREATE_WARN mode=hardware hr=0x{result.Code:X8} fallback=warp");
            result = D3D11.D3D11CreateDevice(
                adapter: null,
                DriverType.Warp,
                flags,
                featureLevels,
                out device,
                out featureLevel,
                out context);
            context?.Dispose();
        }

        if (result.Failure || device == null)
        {
            throw new InvalidOperationException($"Shared D3D11 device creation failed (hr=0x{result.Code:X8}).");
        }

        _device = device;
        _multithread = _device.QueryInterfaceOrNull<ID3D11Multithread>();
        _multithread?.SetMultithreadProtected(true);

        var hr = MfInterop.MFCreateDXGIDeviceManager(out var resetToken, out var deviceManagerPtr);
        if (hr < 0 || deviceManagerPtr == IntPtr.Zero)
        {
            throw new InvalidOperationException($"MFCreateDXGIDeviceManager failed (hr=0x{hr:X8}).");
        }

        _dxgiDeviceManagerPtr = deviceManagerPtr;
        ResetToken = resetToken;

        hr = MfInterop.ResetDxgiDeviceManager(_dxgiDeviceManagerPtr, _device.NativePointer, resetToken);
        if (hr < 0)
        {
            throw new InvalidOperationException($"IMFDXGIDeviceManager.ResetDevice failed (hr=0x{hr:X8}).");
        }

        Logger.Log(
            "SHARED_D3D_DEVICE_CREATE " +
            $"feature_level={featureLevel} reset_token={ResetToken} manager=0x{_dxgiDeviceManagerPtr.ToInt64():X16}");
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(SharedD3DDeviceManager));
        }
    }

    private static class MfInterop
    {
        [DllImport("mfplat.dll", ExactSpelling = true)]
        internal static extern int MFCreateDXGIDeviceManager(out uint pResetToken, out IntPtr ppDeviceManager);

        internal static unsafe int ResetDxgiDeviceManager(IntPtr deviceManagerPtr, IntPtr devicePtr, uint resetToken)
        {
            if (deviceManagerPtr == IntPtr.Zero)
            {
                throw new ArgumentException("DXGI device manager pointer is null.", nameof(deviceManagerPtr));
            }

            if (devicePtr == IntPtr.Zero)
            {
                throw new ArgumentException("D3D11 device pointer is null.", nameof(devicePtr));
            }

            var vtable = *(IntPtr**)deviceManagerPtr;
            // IMFDXGIDeviceManager vtable: 0-2 IUnknown, 3 CloseDeviceHandle, 4 GetVideoService,
            // 5 LockDevice, 6 OpenDeviceHandle, 7 ResetDevice, 8 TestDevice, 9 UnlockDevice
            var resetDevice = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, uint, int>)vtable[7];
            return resetDevice(deviceManagerPtr, devicePtr, resetToken);
        }
    }
}

internal static class PreviewShaderSources
{
    internal const string RendererModeNv12 = "Nv12Shader";
    internal const string RendererModeHdr = "HdrShader";

    internal const string FullscreenVertex = """
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

    internal const string HdrTonemapPixel = """
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

    internal const string HdrPassthroughPixel = """
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

    internal const string Nv12Pixel = """
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
}

internal sealed partial class D3D11PreviewRenderer
{
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
    private long _swapChainAddress;
    private ID3D11Texture2D? _swapChainBackBuffer;
    private ID3D11RenderTargetView? _swapChainRTV;
    private ID3D11VideoProcessorEnumerator? _videoProcessorEnumerator;
    private ID3D11VideoProcessor? _videoProcessor;
    private ID3D11VideoProcessorOutputView? _outputView;
    // Raw-frame uploads cycle through a small texture ring instead of a single
    // input texture: writing into a texture the previous frame's
    // VideoProcessorBlt may still be reading forces the driver to stall or
    // rename a 4K NV12 resource every frame.
    private ID3D11Texture2D?[] _inputTextures = Array.Empty<ID3D11Texture2D?>();
    private ID3D11Texture2D?[] _stagingTextures = Array.Empty<ID3D11Texture2D?>();
    private ID3D11VideoProcessorInputView?[] _inputViews = Array.Empty<ID3D11VideoProcessorInputView?>();
    private int _inputTextureRingIndex;
    private ID3D11Texture2D? _hdrInputTexture;
    private ID3D11Texture2D? _hdrStagingTexture;
    private ID3D11ShaderResourceView? _hdrYPlaneSRV;
    private ID3D11ShaderResourceView? _hdrUVPlaneSRV;
    private int _hdrInputConfiguredWidth;
    private int _hdrInputConfiguredHeight;
    private bool _hdrPlaneViewsUnavailable;
    private ID3D11Device? _sharedDevice;
    private int _sharedDeviceResetPending;
    private int _sharedDeviceActive;

    // Shader resources live beside device, swap-chain, and top-level D3D cleanup state.
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

    // Pre-allocated arrays to avoid per-frame GC pressure (720+ allocs/s at 120fps)
    private readonly VideoProcessorStream[] _vpStreamArray = new VideoProcessorStream[1];
    private readonly ID3D11RenderTargetView[] _rtvArray = new ID3D11RenderTargetView[1];
    private readonly Viewport[] _viewportArray = new Viewport[1];
    private readonly ID3D11SamplerState[] _samplerArray = new ID3D11SamplerState[1];
    private readonly ID3D11ShaderResourceView[] _srvArray2 = new ID3D11ShaderResourceView[2];
    private readonly ID3D11ShaderResourceView[] _srvNullArray2 = { null!, null! };
    private readonly ID3D11Buffer[] _cbArray = new ID3D11Buffer[1];

    // Reused at every shader bind in the per-frame render path; the LINQ-friendly
    // overloads on Vortice's device context allocate an IReadOnlyList wrapper from
    // Array.Empty<T>() each call, which adds up at 60-120 fps.
    private static readonly ID3D11ClassInstance[] EmptyClassInstances = System.Array.Empty<ID3D11ClassInstance>();

    private bool TryEnsureNv12ShaderResources(PendingFrame frame)
    {
        if (_device == null)
        {
            return false;
        }

        if (frame.D3DTextureY == _nv12LastYPtr &&
            frame.D3DTextureUV == _nv12LastUVPtr &&
            _nv12YSRV != null &&
            _nv12UVSRV != null)
        {
            return true;
        }

        _nv12YSRV?.Dispose();
        _nv12YSRV = null;
        _nv12UVSRV?.Dispose();
        _nv12UVSRV = null;
        _nv12LastYPtr = IntPtr.Zero;
        _nv12LastUVPtr = IntPtr.Zero;

        try
        {
            var yTexture = frame.D3DTextureYObject;
            var uvTexture = frame.D3DTextureUVObject;
            if (yTexture == null || uvTexture == null)
            {
                return false;
            }

            _nv12YSRV = _device.CreateShaderResourceView(
                yTexture,
                new ShaderResourceViewDescription(yTexture, ShaderResourceViewDimension.Texture2D, Format.R8_UNorm, 0, 1));
            _nv12UVSRV = _device.CreateShaderResourceView(
                uvTexture,
                new ShaderResourceViewDescription(uvTexture, ShaderResourceViewDimension.Texture2D, Format.R8G8_UNorm, 0, 1));

            _nv12LastYPtr = frame.D3DTextureY;
            _nv12LastUVPtr = frame.D3DTextureUV;
            return true;
        }
        catch (Exception ex)
        {
            _nv12YSRV?.Dispose();
            _nv12YSRV = null;
            _nv12UVSRV?.Dispose();
            _nv12UVSRV = null;
            _nv12LastYPtr = IntPtr.Zero;
            _nv12LastUVPtr = IntPtr.Zero;
            Logger.Log($"D3D11 preview NV12 SRV creation failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
            return false;
        }
    }

    private void DisposeNv12ShaderResourceViews()
    {
        _nv12YSRV?.Dispose();
        _nv12YSRV = null;
        _nv12UVSRV?.Dispose();
        _nv12UVSRV = null;
        _nv12LastYPtr = IntPtr.Zero;
        _nv12LastUVPtr = IntPtr.Zero;
    }

    private void DisposeShaderPipelineResources()
    {
        _linearSampler?.Dispose();
        _linearSampler = null;
        _viewportCB?.Dispose();
        _viewportCB = null;
        _nv12PS?.Dispose();
        _nv12PS = null;
        _hdrTonemapPS?.Dispose();
        _hdrTonemapPS = null;
        _hdrPassthroughPS?.Dispose();
        _hdrPassthroughPS = null;
        _fullscreenVS?.Dispose();
        _fullscreenVS = null;
    }

    private unsafe void CompileTonemapShaders()
    {
        _fullscreenVS?.Dispose();
        _fullscreenVS = null;
        _nv12PS?.Dispose();
        _nv12PS = null;
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
            var vertexShaderBytecode = CompileShader(PreviewShaderSources.FullscreenVertex, "main", "vs_5_0");
            var pixelShaderBytecode = CompileShader(PreviewShaderSources.HdrTonemapPixel, "main", "ps_5_0");
            var passthroughBytecode = CompileShader(PreviewShaderSources.HdrPassthroughPixel, "main", "ps_5_0");

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

            try
            {
                var nv12Bytecode = CompileShader(PreviewShaderSources.Nv12Pixel, "main", "ps_5_0");
                fixed (byte* nv12Ptr = nv12Bytecode)
                {
                    _nv12PS = _device.CreatePixelShader(nv12Ptr, (nuint)nv12Bytecode.Length, null);
                }
            }
            catch (Exception ex)
            {
                _nv12PS?.Dispose();
                _nv12PS = null;
                Logger.Log($"D3D11 preview NV12 shader compile failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
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

            _viewportCB = _device.CreateBuffer(new BufferDescription(
                16, BindFlags.ConstantBuffer, ResourceUsage.Dynamic, CpuAccessFlags.Write));

            Logger.Log(
                $"D3D11 HDR shaders compiled (VS={vertexShaderBytecode.Length}b TonemapPS={pixelShaderBytecode.Length}b PassthroughPS={passthroughBytecode.Length}b Nv12PS={(_nv12PS != null ? "ok" : "unavailable")}).");
        }
        catch (Exception ex)
        {
            _fullscreenVS?.Dispose();
            _fullscreenVS = null;
            _nv12PS?.Dispose();
            _nv12PS = null;
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

        // Keep the compositor queue shallow. This defaults to 2 for latency,
        // but is env-tunable while we measure DWM pacing behavior.
        using var dxgiDevice1 = device.QueryInterfaceOrNull<IDXGIDevice1>();
        dxgiDevice1?.SetMaximumFrameLatency((uint)_dxgiMaxFrameLatency);

        _videoDevice = device.QueryInterfaceOrNull<ID3D11VideoDevice>();
        _videoContext = deviceContext.QueryInterfaceOrNull<ID3D11VideoContext>();
        _videoContext1 = deviceContext.QueryInterfaceOrNull<ID3D11VideoContext1>();
        if (_videoDevice == null || _videoContext == null || _videoContext1 == null)
        {
            throw new InvalidOperationException("D3D11 video interfaces are unavailable.");
        }

        var (swapChain, pixelWidth, pixelHeight) = InitializeCompositionSwapChain(device);
        ConfigureMediaPresentDuration();
        ApplyCompositionScaleTransform(swapChain);
        BindSwapChainToPanel(swapChain);
        CompileTonemapShaders();

        Logger.Log($"D3D11 preview device created featureLevel={featureLevel} shared={sharedDeviceActive}.");
        Logger.Log($"D3D11 preview swap chain created width={pixelWidth} height={pixelHeight} buffers={_swapChainBufferCount} renderQueue={_maxPendingFrames} sync={_presentSyncInterval} latency={_dxgiMaxFrameLatency} waitable={_waitableSwapChainEnabled}.");
    }

    private (IDXGISwapChain1 SwapChain, int PixelWidth, int PixelHeight) InitializeCompositionSwapChain(ID3D11Device device)
    {
        var factoryResult = DXGI.CreateDXGIFactory2(false, out _factory);
        if (factoryResult.Failure || _factory == null)
        {
            throw new InvalidOperationException($"CreateDXGIFactory2 failed: 0x{factoryResult.Code:X8}.");
        }

        var pixelWidth = Math.Max(1, Volatile.Read(ref _startupWidth));
        var pixelHeight = Math.Max(1, Volatile.Read(ref _startupHeight));

        var swapChainFormat = Format.B8G8R8A8_UNorm;
        _hdrCapableSwapChain = false;
        _swapChain3?.Dispose();
        _swapChain3 = null;

        if (_configuredHdr)
        {
            swapChainFormat = Format.R10G10B10A2_UNorm;
        }

        var swapChainFlags = _waitableSwapChainEnabled
            ? SwapChainFlags.FrameLatencyWaitableObject
            : SwapChainFlags.None;

        var swapChainDescription = CreateCompositionSwapChainDescription(
            pixelWidth,
            pixelHeight,
            swapChainFormat,
            swapChainFlags);

        _swapChain = _factory.CreateSwapChainForComposition(device, swapChainDescription, null);
        ConfigureFrameLatencyWaitableObject();

        if (_configuredHdr)
        {
            EnsureHdrCapableSwapChainOrFallbackToSdr(device, pixelWidth, pixelHeight, swapChainFlags);
        }

        _configuredOutputWidth = pixelWidth;
        _configuredOutputHeight = pixelHeight;
        return (_swapChain, pixelWidth, pixelHeight);
    }

    private void EnsureHdrCapableSwapChainOrFallbackToSdr(
        ID3D11Device device,
        int pixelWidth,
        int pixelHeight,
        SwapChainFlags swapChainFlags)
    {
        _swapChain3 = _swapChain!.QueryInterfaceOrNull<IDXGISwapChain3>();
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
                _outputColorSpaceLabel = wantHdr ? "HDR10-PQ (BT.2020)" : "sRGB (BT.709)";
                Interlocked.Exchange(ref _swapChainColorSpaceDirty, 0);
                Logger.Log($"D3D11 preview HDR-capable swap chain: srgb={srgbOk} hdr10={hdr10Ok} initial={initialColorSpace}.");
                return;
            }

            Logger.Log($"D3D11 preview HDR color space check: srgb={srgbOk}({srgbSupport}) hdr10={hdr10Ok}({hdr10Support}). Falling back to B8G8R8A8.");
            RecreateSdrCompositionSwapChain(device, pixelWidth, pixelHeight, swapChainFlags);
            return;
        }

        Logger.Log("D3D11 preview IDXGISwapChain3 unavailable - HDR passthrough not supported.");
        RecreateSdrCompositionSwapChain(device, pixelWidth, pixelHeight, swapChainFlags);
    }

    private void RecreateSdrCompositionSwapChain(
        ID3D11Device device,
        int pixelWidth,
        int pixelHeight,
        SwapChainFlags swapChainFlags)
    {
        _swapChain3?.Dispose();
        _swapChain3 = null;
        _swapChain2?.Dispose();
        _swapChain2 = null;
        _frameLatencyWaitHandle = IntPtr.Zero;
        _swapChain!.Dispose();

        var fallbackDescription = CreateCompositionSwapChainDescription(
            pixelWidth,
            pixelHeight,
            Format.B8G8R8A8_UNorm,
            swapChainFlags);
        _swapChain = _factory!.CreateSwapChainForComposition(device, fallbackDescription, null);
        ConfigureFrameLatencyWaitableObject();
    }

    private SwapChainDescription1 CreateCompositionSwapChainDescription(
        int pixelWidth,
        int pixelHeight,
        Format format,
        SwapChainFlags swapChainFlags)
        => new(
            (uint)pixelWidth,
            (uint)pixelHeight,
            format,
            false,
            Usage.RenderTargetOutput,
            (uint)_swapChainBufferCount,
            Scaling.Stretch,
            SwapEffect.FlipSequential,
            AlphaMode.Ignore,
            swapChainFlags);

    private void ConfigureMediaPresentDuration()
    {
        if (!_mediaPresentDurationEnabled || _swapChain == null)
        {
            return;
        }

        using var mediaSwapChain = _swapChain.QueryInterfaceOrNull<IDXGISwapChainMedia>();
        if (mediaSwapChain == null)
        {
            Logger.Log("D3D11 preview media present duration unavailable: IDXGISwapChainMedia not supported.");
            return;
        }

        var fps = Math.Max(1.0, _startupFps);
        var desiredDuration = (uint)Math.Max(1, (int)Math.Round(10_000_000.0 / fps));
        try
        {
            mediaSwapChain.CheckPresentDurationSupport(
                desiredDuration,
                out var closestSmaller,
                out var closestLarger);
            Logger.Log(
                $"D3D11 preview media present duration support desired={desiredDuration} " +
                $"smaller={closestSmaller} larger={closestLarger}");

            mediaSwapChain.SetPresentDuration(desiredDuration);
            Logger.Log($"D3D11 preview media present duration set desired={desiredDuration} fps={fps:0.###}");
        }
        catch (Exception ex)
        {
            Logger.Log($"D3D11 preview media present duration failed: {ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
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

    private void HandleDeviceLost(Exception ex)
    {
        Logger.Log($"D3D11 preview device lost ({ex.GetType().Name}); recreating device.");

        // If Stop() is pending, bail. Stop() will unbind the swap chain from
        // the panel while D3D resources are still alive, then the finally block
        // will clean up. Proceeding here would dispose the swap chain while
        // Stop() may be concurrently calling SetSwapChain(null) on the panel -
        // the native call would hit freed memory and trigger an
        // AccessViolationException that .NET 8 cannot catch.
        if (Volatile.Read(ref _stopRequested) != 0) return;

        CleanupD3DResources();
        while (TryDequeuePendingFrame(out var stalePending))
        {
            TrackFrameDropped(stalePending, "device-lost");
            stalePending.Dispose();
        }

        // Re-check: Stop() may have been called during cleanup. Proceeding
        // into InitializeD3D->BindSwapChainToPanel would dispatch to the UI
        // thread, which may be blocked on Join - a 5-second deadlock.
        if (Volatile.Read(ref _stopRequested) != 0) return;

        InitializeD3D();
        Interlocked.Exchange(ref _compositionTransformDirty, 1);
    }

    private static bool IsDeviceLostException(Exception ex)
    {
        if (ex is SharpGenException sharpGenException)
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

    private void EnsureInputResources(int width, int height, bool isHdr)
    {
        if (_device == null || _videoDevice == null || _videoProcessorEnumerator == null)
        {
            throw new InvalidOperationException("D3D11 device state is incomplete for input texture creation.");
        }

        var targetFormat = isHdr ? Format.P010 : Format.NV12;
        if (_inputTextures.Length == _inputTextureRingSize &&
            _inputViews.Length == _inputTextureRingSize &&
            _inputTextures[0] != null &&
            _stagingTextures[0] != null &&
            _inputViews[0] != null &&
            _configuredInputWidth == width &&
            _configuredInputHeight == height &&
            _configuredInputFormat == targetFormat)
        {
            return;
        }

        DisposeProcessorInputResources();
        DisposeInputTextureResources();

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

        var inputViewDescription = new VideoProcessorInputViewDescription
        {
            ViewDimension = VideoProcessorInputViewDimension.Texture2D,
            Texture2D = new Texture2DVideoProcessorInputView { MipSlice = 0, ArraySlice = 0 }
        };

        _inputTextures = new ID3D11Texture2D?[_inputTextureRingSize];
        _stagingTextures = new ID3D11Texture2D?[_inputTextureRingSize];
        _inputViews = new ID3D11VideoProcessorInputView?[_inputTextureRingSize];
        for (var i = 0; i < _inputTextureRingSize; i++)
        {
            _inputTextures[i] = _device.CreateTexture2D(inputDescription);
            _stagingTextures[i] = _device.CreateTexture2D(stagingDescription);
            _inputViews[i] = _videoDevice.CreateVideoProcessorInputView(_inputTextures[i]!, _videoProcessorEnumerator, inputViewDescription);
        }

        _inputTextureRingIndex = 0;
        _configuredInputFormat = targetFormat;
    }

    private void EnsureHdrInputResources(int width, int height)
    {
        if (_device == null)
        {
            throw new InvalidOperationException("D3D11 device state is incomplete for HDR shader input texture creation.");
        }

        if (_hdrPlaneViewsUnavailable)
        {
            return;
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

        if (_hdrYPlaneSRV == null && _hdrUVPlaneSRV == null)
        {
            _hdrInputTexture.Dispose();
            _hdrInputTexture = null;
            _hdrStagingTexture.Dispose();
            _hdrStagingTexture = null;
            _hdrPlaneViewsUnavailable = true;
            return;
        }

        _hdrInputConfiguredWidth = width;
        _hdrInputConfiguredHeight = height;
    }

    private ID3D11ShaderResourceView? CreateHdrPlaneView(Format format, uint planeSlice)
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

        Logger.Log("D3D11_RENDERER_WARN Device3 not available for P010 plane views â€” HDR shader path disabled, falling back to VideoProcessor");
        return null;
    }

    private void DisposeProcessorInputResources()
    {
        foreach (var inputView in _inputViews)
        {
            inputView?.Dispose();
        }

        _inputViews = Array.Empty<ID3D11VideoProcessorInputView?>();
    }

    private void DisposeInputTextureResources()
    {
        foreach (var stagingTexture in _stagingTextures)
        {
            stagingTexture?.Dispose();
        }

        _stagingTextures = Array.Empty<ID3D11Texture2D?>();

        foreach (var inputTexture in _inputTextures)
        {
            inputTexture?.Dispose();
        }

        _inputTextures = Array.Empty<ID3D11Texture2D?>();
        _inputTextureRingIndex = 0;
    }

    private void DisposeHdrInputResources()
    {
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
        _hdrPlaneViewsUnavailable = false;
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

    private void DisposeProcessorResources()
    {
        DisposeProcessorInputResources();
        DisposeHdrInputResources();
        DisposeNv12ShaderResourceViews();
        _outputView?.Dispose();
        _outputView = null;
        _swapChainRTV?.Dispose();
        _swapChainRTV = null;
        _swapChainBackBuffer?.Dispose();
        _swapChainBackBuffer = null;
        _videoProcessor?.Dispose();
        _videoProcessor = null;
        _videoProcessorEnumerator?.Dispose();
        _videoProcessorEnumerator = null;
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

    private void CleanupD3DResources()
    {
        DisposeProcessorResources();
        DisposeFrameCaptureStagingResources();
        DisposeInputTextureResources();

        // Stop() unbinds the panel before waking the render thread, while the
        // swap chain is still alive. Cleanup can then release the DXGI objects
        // without leaving SwapChainPanel holding a stale native reference.
        Interlocked.CompareExchange(ref _swapChainBound, 0, 1);
        Interlocked.Exchange(ref _swapChainAddress, 0);

        _swapChain3?.Dispose();
        _swapChain3 = null;
        _swapChain2?.Dispose();
        _swapChain2 = null;
        _frameLatencyWaitHandle = IntPtr.Zero;
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
        DisposeShaderPipelineResources();
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
        Interlocked.Exchange(ref _sharedDeviceActive, 0);
    }

}
