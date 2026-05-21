using System;
using Vortice.Direct3D11;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
    private bool _loggedDirectUploadFallback;

    private unsafe bool UploadRawFrameToTexture(
        byte[] data, int dataLength, int width, int height, bool isHdr,
        ID3D11Texture2D stagingTexture, ID3D11Texture2D inputTexture)
        => UploadRawFrameToTexture(data.AsSpan(0, Math.Min(dataLength, data.Length)), width, height, isHdr, stagingTexture, inputTexture);

    private unsafe bool UploadRawFrameToTexture(
        ReadOnlySpan<byte> data, int width, int height, bool isHdr,
        ID3D11Texture2D stagingTexture, ID3D11Texture2D inputTexture)
    {
        if (_deviceContext == null)
        {
            return false;
        }

        var rowBytes = isHdr ? width * 2 : width;
        var uvRows = height / 2;
        var expectedBytes = (rowBytes * height) + (rowBytes * uvRows);
        if (data.Length < expectedBytes)
        {
            Logger.Log(
                $"D3D11 preview raw frame too small: expected={expectedBytes} actual={data.Length} hdr={isHdr}.");
            return false;
        }

        if (TryUpdateRawFrameTexture(data, inputTexture, rowBytes, expectedBytes))
        {
            return true;
        }

        return UploadRawFrameViaStaging(data, width, height, rowBytes, uvRows, stagingTexture, inputTexture);
    }

    private unsafe bool TryUpdateRawFrameTexture(
        ReadOnlySpan<byte> data,
        ID3D11Texture2D inputTexture,
        int rowBytes,
        int expectedBytes)
    {
        if (_deviceContext == null)
        {
            return false;
        }

        try
        {
            fixed (byte* srcStart = data)
            {
                _deviceContext.UpdateSubresource(
                    inputTexture,
                    0,
                    null,
                    (IntPtr)srcStart,
                    (uint)rowBytes,
                    (uint)expectedBytes);
            }

            return true;
        }
        catch (Exception ex)
        {
            if (!_loggedDirectUploadFallback)
            {
                _loggedDirectUploadFallback = true;
                Logger.Log($"D3D11 preview direct texture update failed; falling back to staging upload. type={ex.GetType().Name} hr=0x{ex.HResult:X8} msg={ex.Message}");
            }

            return false;
        }
    }

    private unsafe bool UploadRawFrameViaStaging(
        ReadOnlySpan<byte> data,
        int width,
        int height,
        int rowBytes,
        int uvRows,
        ID3D11Texture2D stagingTexture,
        ID3D11Texture2D inputTexture)
    {
        if (_deviceContext == null)
        {
            return false;
        }

        fixed (byte* srcStart = data)
        {
            var srcY = srcStart;
            var srcUv = srcStart + (rowBytes * height);

            _deviceContext.Map(stagingTexture, 0, MapMode.Write, Vortice.Direct3D11.MapFlags.None, out var mapped);
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
                _deviceContext.Unmap(stagingTexture, 0);
            }
        }

        _deviceContext.CopyResource(inputTexture, stagingTexture);
        return true;
    }
}
