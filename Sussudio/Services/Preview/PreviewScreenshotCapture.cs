using System;
using System.Runtime.InteropServices;
using Sussudio.Models;
using Vortice.Direct3D11;

namespace Sussudio.Services.Preview;

internal static partial class PreviewScreenshotCapture
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
