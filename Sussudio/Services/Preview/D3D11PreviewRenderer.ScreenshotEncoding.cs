using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using Sussudio.Models;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Sussudio.Services.Preview;

internal sealed partial class D3D11PreviewRenderer
{
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

        var letterboxTopRows = PreviewScreenshotCapture.CountLeadingBlackEdges(rowAllBlack);
        var letterboxBottomRows = letterboxTopRows == height ? 0 : PreviewScreenshotCapture.CountTrailingBlackEdges(rowAllBlack);
        var pillarboxLeftCols = PreviewScreenshotCapture.CountLeadingBlackEdges(columnAllBlack);
        var pillarboxRightCols = pillarboxLeftCols == width ? 0 : PreviewScreenshotCapture.CountTrailingBlackEdges(columnAllBlack);

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
}
