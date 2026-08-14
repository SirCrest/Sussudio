using System;

namespace Sussudio.Services.Preview;

/// <summary>
/// Chooses stable compositor targets from the visible panel, without changing
/// the source-sized input path.  The small buckets prevent a stream of one-pixel
/// WinUI layout changes from forcing DXGI resource churn.
/// </summary>
internal static class PreviewOutputSizePolicy
{
    internal const int BucketPixels = 64;
    internal const int MinimumResizeDeltaPixels = 96;
    internal const int ResizeDebounceMilliseconds = 250;

    internal static PreviewOutputSize Resolve(int panelWidth, int panelHeight, int fallbackWidth, int fallbackHeight)
    {
        var width = panelWidth > 0 ? panelWidth : fallbackWidth;
        var height = panelHeight > 0 ? panelHeight : fallbackHeight;
        width = Math.Max(2, width);
        height = Math.Max(2, height);

        // A preview target larger than the source only moves the compositor's
        // upscale into our render pass and increases GPU work. Cap the target
        // to the natural frame while retaining the panel aspect ratio.
        var maxWidth = fallbackWidth > 0 ? fallbackWidth : width;
        var maxHeight = fallbackHeight > 0 ? fallbackHeight : height;
        var sourceScale = Math.Min(
            1.0,
            Math.Min((double)maxWidth / width, (double)maxHeight / height));
        width = Math.Max(2, (int)Math.Round(width * sourceScale, MidpointRounding.AwayFromZero));
        height = Math.Max(2, (int)Math.Round(height * sourceScale, MidpointRounding.AwayFromZero));

        // Bucket the longer display axis and derive the other axis from the
        // panel aspect ratio.  This preserves the visual aspect ratio while
        // keeping resize targets stable across fractional-DPI layout churn.
        if (width >= height)
        {
            var bucketedWidth = Math.Min(RoundToBucket(width), RoundDownToEven(maxWidth));
            return new PreviewOutputSize(bucketedWidth, RoundToEven((double)bucketedWidth * height / width));
        }

        var bucketedHeight = Math.Min(RoundToBucket(height), RoundDownToEven(maxHeight));
        return new PreviewOutputSize(RoundToEven((double)bucketedHeight * width / height), bucketedHeight);
    }

    internal static bool ShouldResize(PreviewOutputSize current, PreviewOutputSize target)
    {
        if (current == target)
        {
            return false;
        }

        return Math.Abs(current.Width - target.Width) >= MinimumResizeDeltaPixels ||
               Math.Abs(current.Height - target.Height) >= MinimumResizeDeltaPixels;
    }

    private static int RoundToBucket(int value)
        => Math.Max(2, ((Math.Max(2, value) + (BucketPixels / 2)) / BucketPixels) * BucketPixels);

    private static int RoundToEven(double value)
        => Math.Max(2, ((int)Math.Round(value, MidpointRounding.AwayFromZero) + 1) & ~1);

    private static int RoundDownToEven(int value)
        => Math.Max(2, value & ~1);
}

internal readonly record struct PreviewOutputSize(int Width, int Height);
