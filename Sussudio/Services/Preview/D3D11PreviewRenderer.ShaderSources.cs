using System;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
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
