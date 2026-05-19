using System;
using System.Diagnostics;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Capture;

internal sealed partial class VisualCadenceTracker
{
    private readonly record struct LumaSample(int Length, double ChangedPixels);

    private LumaSample SampleLumaAndCompare(
        ReadOnlySpan<byte> frame,
        int width,
        int height,
        int bytesPerLuma,
        byte[] destination,
        byte[]? previous)
    {
        var cropX = Math.Clamp((int)Math.Round(width * _cropLeft), 0, Math.Max(0, width - 1));
        var cropY = Math.Clamp((int)Math.Round(height * _cropTop), 0, Math.Max(0, height - 1));
        var cropWidth = Math.Clamp((int)Math.Round(width * _cropWidth), 1, width - cropX);
        var cropHeight = Math.Clamp((int)Math.Round(height * _cropHeight), 1, height - cropY);
        var sampleWidth = Math.Min(_sampleColumns, cropWidth);
        var sampleHeight = Math.Min(_sampleRows, cropHeight);
        var sampleX = cropX + Math.Max(0, (cropWidth - sampleWidth) / 2);
        var sampleY = cropY + Math.Max(0, (cropHeight - sampleHeight) / 2);
        var index = 0;
        var changed = 0;
        for (var row = 0; row < sampleHeight; row++)
        {
            var y = sampleY + row;
            var rowOffset = y * width * bytesPerLuma;
            for (var col = 0; col < sampleWidth; col++)
            {
                var x = sampleX + col;
                var lumaOffset = rowOffset + x * bytesPerLuma;
                var changedPixel = false;
                var luma = frame[lumaOffset];
                destination[index] = luma;
                if (previous != null && previous[index] != luma)
                {
                    changedPixel = true;
                }

                index++;
                if (bytesPerLuma == 2)
                {
                    var secondLuma = lumaOffset + 1 < frame.Length
                        ? frame[lumaOffset + 1]
                        : (byte)0;
                    destination[index] = secondLuma;
                    if (previous != null && previous[index] != secondLuma)
                    {
                        changedPixel = true;
                    }

                    index++;
                }

                if (changedPixel)
                {
                    changed++;
                }
            }
        }

        return new LumaSample(index, changed);
    }

    private void PromoteCurrentSample(int sampleLength, int bytesPerLuma)
    {
        var oldLast = _lastSample;
        _lastSample = _currentSample;
        _currentSample = oldLast;
        _lastSampleLength = sampleLength;
        _lastBytesPerLuma = bytesPerLuma;
    }

    private static void AddTimingSample(double[] window, ref int count, ref int index, double value)
    {
        if (!double.IsFinite(value) || value < 0 || value > 5000)
        {
            return;
        }

        RingBufferHelpers.Add(window, ref count, ref index, value);
    }

    private static void AddValueSample(double[] window, ref int count, ref int index, double value)
    {
        if (!double.IsFinite(value) || value < 0)
        {
            return;
        }

        RingBufferHelpers.Add(window, ref count, ref index, value);
    }

    private static double ElapsedMs(long startTick, long endTick)
        => (endTick - startTick) * 1000.0 / Stopwatch.Frequency;
}
