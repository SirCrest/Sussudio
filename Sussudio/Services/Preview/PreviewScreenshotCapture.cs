using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using Sussudio.Models;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal static class PreviewScreenshotCapture
{
    private static readonly uint[] PngCrc32Table = InitPngCrc32Table();

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

    internal static uint[] InitPngCrc32Table()
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
}
