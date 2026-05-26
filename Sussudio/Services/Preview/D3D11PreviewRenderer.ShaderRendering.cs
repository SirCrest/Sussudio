using System;
using System.Runtime.InteropServices;
using Sussudio.Services.Runtime;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Sussudio.Services.Preview;

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
}
