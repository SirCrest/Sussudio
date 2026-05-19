using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackDecoder
{
    private void CopyFramePlanesToBuffer(byte* dest, int destSize)
    {
        if (_isHdr)
        {
            var yLinesize = _videoWidth * 2;
            var yPlaneSize = yLinesize * _videoHeight;
            var uvLinesize = _videoWidth * 2;

            CopyPlane(_videoFrame->data[0], _videoFrame->linesize[0],
                      dest, yLinesize, _videoHeight);
            CopyPlane(_videoFrame->data[1], _videoFrame->linesize[1],
                      dest + yPlaneSize, uvLinesize, _videoHeight / 2);
        }
        else
        {
            var yPlaneSize = _videoWidth * _videoHeight;

            CopyPlane(_videoFrame->data[0], _videoFrame->linesize[0],
                      dest, _videoWidth, _videoHeight);
            CopyPlane(_videoFrame->data[1], _videoFrame->linesize[1],
                      dest + yPlaneSize, _videoWidth, _videoHeight / 2);
        }
    }

    private static void CopyPlane(byte* src, int srcLinesize, byte* dst, int dstLinesize, int height)
    {
        if (srcLinesize == dstLinesize)
        {
            Buffer.MemoryCopy(src, dst, (long)dstLinesize * height, (long)srcLinesize * height);
            return;
        }

        var copyWidth = Math.Min(srcLinesize, dstLinesize);
        for (var y = 0; y < height; y++)
        {
            Buffer.MemoryCopy(
                src + y * srcLinesize,
                dst + y * dstLinesize,
                dstLinesize, copyWidth);
        }
    }

    private void ConvertYuv420pToNv12(byte* dest)
    {
        var w = _videoWidth;
        var h = _videoHeight;

        CopyPlane(_videoFrame->data[0], _videoFrame->linesize[0], dest, w, h);

        var uvDest = dest + w * h;
        var halfW = w / 2;
        var uStride = _videoFrame->linesize[1];
        var vStride = _videoFrame->linesize[2];

        for (var row = 0; row < h / 2; row++)
        {
            var uRow = _videoFrame->data[1] + row * uStride;
            var vRow = _videoFrame->data[2] + row * vStride;
            var destRow = uvDest + row * w;

            InterleaveUvRow(uRow, vRow, destRow, halfW);
        }
    }

    private void ConvertYuv420p10leToP010(byte* dest)
    {
        var w = _videoWidth;
        var h = _videoHeight;

        CopyPlane(_videoFrame->data[0], _videoFrame->linesize[0], dest, w * 2, h);

        var uvDest = (ushort*)(dest + w * h * 2);
        var halfW = w / 2;
        var uStride = _videoFrame->linesize[1];
        var vStride = _videoFrame->linesize[2];

        for (var row = 0; row < h / 2; row++)
        {
            var uRow = (ushort*)(_videoFrame->data[1] + row * uStride);
            var vRow = (ushort*)(_videoFrame->data[2] + row * vStride);
            var destRow = uvDest + row * w;

            for (var col = 0; col < halfW; col++)
            {
                destRow[col * 2] = uRow[col];
                destRow[col * 2 + 1] = vRow[col];
            }
        }
    }

    private static void InterleaveUvRow(byte* uRow, byte* vRow, byte* dest, int halfW)
    {
        var col = 0;

        if (Avx2.IsSupported)
        {
            for (; col + 32 <= halfW; col += 32)
            {
                var u = Avx.LoadVector256(uRow + col);
                var v = Avx.LoadVector256(vRow + col);

                var lo = Avx2.UnpackLow(u, v);
                var hi = Avx2.UnpackHigh(u, v);

                var out0 = Avx2.Permute2x128(lo, hi, 0x20);
                var out1 = Avx2.Permute2x128(lo, hi, 0x31);

                Avx.Store(dest + col * 2, out0);
                Avx.Store(dest + col * 2 + 32, out1);
            }
        }
        else if (Sse2.IsSupported)
        {
            for (; col + 16 <= halfW; col += 16)
            {
                var u = Sse2.LoadVector128(uRow + col);
                var v = Sse2.LoadVector128(vRow + col);

                var lo = Sse2.UnpackLow(u, v);
                var hi = Sse2.UnpackHigh(u, v);

                Sse2.Store(dest + col * 2, lo);
                Sse2.Store(dest + col * 2 + 16, hi);
            }
        }

        for (; col < halfW; col++)
        {
            dest[col * 2] = uRow[col];
            dest[col * 2 + 1] = vRow[col];
        }
    }
}
