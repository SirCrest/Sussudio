using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Sussudio.Models;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal static class PreviewScreenshotCapture
{
    internal static byte[] CopyMappedFrameToBuffer(MappedSubresource mapped, int height, int sourceRowBytes)
    {
        var sourceBuffer = new byte[checked(sourceRowBytes * height)];
        for (var y = 0; y < height; y++)
        {
            var sourceRow = IntPtr.Add(mapped.DataPointer, checked(y * (int)mapped.RowPitch));
            Marshal.Copy(sourceRow, sourceBuffer, checked(y * sourceRowBytes), sourceRowBytes);
        }

        return sourceBuffer;
    }

    internal static int CountLeadingBlackEdges(bool[] values)
    {
        var count = 0;
        while (count < values.Length && values[count])
        {
            count++;
        }

        return count;
    }

    internal static int CountTrailingBlackEdges(bool[] values)
    {
        var count = 0;
        while (count < values.Length && values[values.Length - 1 - count])
        {
            count++;
        }

        return count;
    }

    internal static PreviewFrameCaptureResult CaptureMappedFrameToBmp(
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
        var analysis = new PreviewScreenshotPixelAnalysis(width, height);

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

                    analysis.AnalyzePixel(x, r, g, b, ref isRowPureBlack);
                }

                analysis.CompleteRow(y, isRowPureBlack);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rowBuffer);
        }

        return analysis.CreateResult(outputPath, rendererMode);
    }

    internal static PreviewFrameCaptureResult CaptureFrameBufferTo16BitPng(
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
        var analysis = new PreviewScreenshotPixelAnalysis(width, height);

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

                            analysis.AnalyzePixel(x, r8, g8, b8, ref isRowPureBlack);
                        }

                        analysis.CompleteRow(y, isRowPureBlack);
                        zlibStream.Write(pngRowBuffer, 0, pngRowBytes);
                    }
                }

                PreviewPng16Encoder.WriteCompressedRgb16Png(outputPath, width, height, compressedDataStream);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pngRowBuffer);
            ArrayPool<byte>.Shared.Return(sourceRowBuffer);
        }

        return analysis.CreateResult(outputPath, rendererMode);
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

    private sealed class PreviewScreenshotPixelAnalysis
    {
        private readonly int _width;
        private readonly int _height;
        private readonly int[] _histogram = new int[16];
        private readonly bool[] _rowAllBlack;
        private readonly bool[] _columnAllBlack;
        private long _sumR;
        private long _sumG;
        private long _sumB;
        private double _sumLuminance;
        private double _minLuminance = 255;
        private double _maxLuminance;
        private long _nearBlackCount;
        private long _nearWhiteCount;
        private long _pureBlackCount;

        internal PreviewScreenshotPixelAnalysis(int width, int height)
        {
            _width = width;
            _height = height;
            _rowAllBlack = new bool[height];
            _columnAllBlack = new bool[width];
            Array.Fill(_rowAllBlack, true);
            Array.Fill(_columnAllBlack, true);
        }

        internal void AnalyzePixel(int x, byte r, byte g, byte b, ref bool isRowPureBlack)
        {
            _sumR += r;
            _sumG += g;
            _sumB += b;

            var luminance = (0.299 * r) + (0.587 * g) + (0.114 * b);
            _sumLuminance += luminance;
            if (luminance < _minLuminance)
            {
                _minLuminance = luminance;
            }
            if (luminance > _maxLuminance)
            {
                _maxLuminance = luminance;
            }

            if (luminance < 16.0)
            {
                _nearBlackCount++;
            }
            if (luminance > 240.0)
            {
                _nearWhiteCount++;
            }

            var isPureBlack = r == 0 && g == 0 && b == 0;
            if (isPureBlack)
            {
                _pureBlackCount++;
            }
            else
            {
                isRowPureBlack = false;
                _columnAllBlack[x] = false;
            }

            var histogramIndex = (int)(luminance / 16.0);
            if (histogramIndex > 15)
            {
                histogramIndex = 15;
            }

            _histogram[histogramIndex]++;
        }

        internal void CompleteRow(int y, bool isRowPureBlack)
        {
            _rowAllBlack[y] = isRowPureBlack;
        }

        internal PreviewFrameCaptureResult CreateResult(string outputPath, string rendererMode)
        {
            var totalPixels = (long)_width * _height;
            var letterboxTopRows = CountLeadingBlackEdges(_rowAllBlack);
            var letterboxBottomRows = letterboxTopRows == _height ? 0 : CountTrailingBlackEdges(_rowAllBlack);
            var pillarboxLeftCols = CountLeadingBlackEdges(_columnAllBlack);
            var pillarboxRightCols = pillarboxLeftCols == _width ? 0 : CountTrailingBlackEdges(_columnAllBlack);

            var contentWidth = Math.Max(0, _width - pillarboxLeftCols - pillarboxRightCols);
            var contentHeight = Math.Max(0, _height - letterboxTopRows - letterboxBottomRows);
            var contentAspectRatio = contentHeight > 0
                ? (double)contentWidth / contentHeight
                : 0.0;

            return new PreviewFrameCaptureResult
            {
                Succeeded = true,
                Message = "Preview frame captured.",
                FilePath = outputPath,
                CapturedWidth = _width,
                CapturedHeight = _height,
                RendererMode = rendererMode,
                AverageR = totalPixels > 0 ? (double)_sumR / totalPixels : 0.0,
                AverageG = totalPixels > 0 ? (double)_sumG / totalPixels : 0.0,
                AverageB = totalPixels > 0 ? (double)_sumB / totalPixels : 0.0,
                AverageLuminance = totalPixels > 0 ? _sumLuminance / totalPixels : 0.0,
                MinLuminance = _minLuminance,
                MaxLuminance = _maxLuminance,
                NearBlackPercent = totalPixels > 0 ? (_nearBlackCount * 100.0) / totalPixels : 0.0,
                NearWhitePercent = totalPixels > 0 ? (_nearWhiteCount * 100.0) / totalPixels : 0.0,
                PureBlackPercent = totalPixels > 0 ? (_pureBlackCount * 100.0) / totalPixels : 0.0,
                LetterboxTopRows = letterboxTopRows,
                LetterboxBottomRows = letterboxBottomRows,
                PillarboxLeftCols = pillarboxLeftCols,
                PillarboxRightCols = pillarboxRightCols,
                ContentWidth = contentWidth,
                ContentHeight = contentHeight,
                ContentAspectRatio = contentAspectRatio,
                LuminanceHistogram = _histogram,
                TotalPixels = totalPixels
            };
        }
    }
}
