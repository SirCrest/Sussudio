using Sussudio.Services.Preview;
using Xunit;

namespace Sussudio.Tests;

/// <summary>
/// Executes the linked production <see cref="PreviewOutputSizePolicy"/> source directly.
/// The policy is pure arithmetic, so these are behavioral tests of the real compositor
/// sizing rules — not source-text or reflection assertions.
/// </summary>
public sealed class PreviewOutputSizePolicyTests
{
    [Theory]
    // Panel already matches the source: the target is the source size untouched.
    [InlineData(1920, 1080, 1920, 1080, 1920, 1080)]
    // Panel smaller than the source stays at the panel size (bucket-aligned).
    [InlineData(1280, 720, 1920, 1080, 1280, 720)]
    // Panel larger than the source is capped to the source, never upscaled.
    [InlineData(3840, 2160, 1920, 1080, 1920, 1080)]
    // Portrait panels bucket the long axis (height) and derive the short axis.
    [InlineData(720, 1280, 1080, 1920, 720, 1280)]
    public void Resolve_ProducesExpectedTarget(
        int panelWidth,
        int panelHeight,
        int fallbackWidth,
        int fallbackHeight,
        int expectedWidth,
        int expectedHeight)
    {
        var size = PreviewOutputSizePolicy.Resolve(panelWidth, panelHeight, fallbackWidth, fallbackHeight);

        Assert.Equal(new PreviewOutputSize(expectedWidth, expectedHeight), size);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, -1)]
    [InlineData(0, 1080)]
    [InlineData(1920, 0)]
    public void Resolve_FallsBackToSourceSizeWhenPanelIsUnmeasured(int panelWidth, int panelHeight)
    {
        // WinUI reports a zero-size panel before first layout; the policy must not
        // collapse the swap chain, it must fall back to the natural frame size.
        var size = PreviewOutputSizePolicy.Resolve(panelWidth, panelHeight, 1920, 1080);

        Assert.Equal(new PreviewOutputSize(1920, 1080), size);
    }

    [Theory]
    [InlineData(3840, 2160)]
    [InlineData(2560, 1440)]
    [InlineData(1921, 1081)]
    public void Resolve_NeverExceedsTheSourceFrame(int panelWidth, int panelHeight)
    {
        // Rendering above the source only moves the compositor's upscale into our
        // render pass, so the target must stay within the source frame.
        var size = PreviewOutputSizePolicy.Resolve(panelWidth, panelHeight, 1920, 1080);

        Assert.True(size.Width <= 1920, $"width {size.Width} exceeded source 1920");
        Assert.True(size.Height <= 1080, $"height {size.Height} exceeded source 1080");
    }

    [Theory]
    [InlineData(1, 1, 0, 0)]
    [InlineData(2, 2, 2, 2)]
    [InlineData(1, 1, 1, 1)]
    public void Resolve_ClampsToTheMinimumTwoPixelTarget(
        int panelWidth,
        int panelHeight,
        int fallbackWidth,
        int fallbackHeight)
    {
        // A zero or one-pixel swap chain is invalid for DXGI; two pixels is the floor.
        var size = PreviewOutputSizePolicy.Resolve(panelWidth, panelHeight, fallbackWidth, fallbackHeight);

        Assert.True(size.Width >= 2, $"width {size.Width} below the 2px floor");
        Assert.True(size.Height >= 2, $"height {size.Height} below the 2px floor");
    }

    [Fact]
    public void Resolve_AlwaysProducesEvenDimensions()
    {
        // Even dimensions keep 4:2:0 chroma subsampling valid downstream. Sweep a
        // wide span of awkward panel sizes rather than asserting a single case.
        for (var panelWidth = 3; panelWidth <= 2000; panelWidth += 7)
        {
            for (var panelHeight = 3; panelHeight <= 1200; panelHeight += 53)
            {
                var size = PreviewOutputSizePolicy.Resolve(panelWidth, panelHeight, 1920, 1080);

                Assert.True(
                    size.Width % 2 == 0 && size.Height % 2 == 0,
                    $"panel {panelWidth}x{panelHeight} produced odd target {size.Width}x{size.Height}");
            }
        }
    }

    [Fact]
    public void Resolve_KeepsTheBucketedAxisFixedAcrossSubBucketPanelJitter()
    {
        // Fractional-DPI layout churn moves the panel by a few pixels every frame.
        // The bucketed (long) axis must not move within a bucket.
        var baseline = PreviewOutputSizePolicy.Resolve(1280, 720, 1920, 1080);

        for (var jitter = -20; jitter <= 20; jitter++)
        {
            var jittered = PreviewOutputSizePolicy.Resolve(1280 + jitter, 720, 1920, 1080);

            Assert.Equal(baseline.Width, jittered.Width);
        }
    }

    [Fact]
    public void SubBucketPanelJitterNeverTriggersAResize()
    {
        // The derived axis still tracks the panel aspect ratio exactly, so it drifts
        // a little under jitter. What matters to the renderer is that the pair
        // Resolve + ShouldResize never asks for a swap-chain rebuild for that drift.
        var current = PreviewOutputSizePolicy.Resolve(1280, 720, 1920, 1080);

        for (var jitter = -20; jitter <= 20; jitter++)
        {
            var target = PreviewOutputSizePolicy.Resolve(1280 + jitter, 720, 1920, 1080);

            Assert.False(
                PreviewOutputSizePolicy.ShouldResize(current, target),
                $"jitter {jitter} produced {target.Width}x{target.Height}, forcing a resize from {current.Width}x{current.Height}");
        }
    }

    [Fact]
    public void Resolve_PreservesPanelAspectRatioWithinOneBucket()
    {
        var size = PreviewOutputSizePolicy.Resolve(1600, 900, 1920, 1080);

        var panelAspect = 1600.0 / 900.0;
        var targetAspect = (double)size.Width / size.Height;

        Assert.True(
            System.Math.Abs(panelAspect - targetAspect) < 0.02,
            $"aspect drifted: panel {panelAspect:F4} vs target {targetAspect:F4}");
    }

    [Fact]
    public void ShouldResize_IsFalseForAnIdenticalTarget()
    {
        var current = new PreviewOutputSize(1920, 1080);

        Assert.False(PreviewOutputSizePolicy.ShouldResize(current, current));
    }

    [Theory]
    // Below the hysteresis threshold on both axes — not worth a resize.
    [InlineData(1920, 1080, 1856, 1080, false)]
    [InlineData(1920, 1080, 1920, 1000, false)]
    [InlineData(1920, 1080, 1888, 1048, false)]
    // At or beyond the threshold on either axis — resize.
    [InlineData(1920, 1080, 1824, 1080, true)]
    [InlineData(1920, 1080, 1920, 984, true)]
    [InlineData(1920, 1080, 1280, 720, true)]
    public void ShouldResize_AppliesHysteresisPerAxis(
        int currentWidth,
        int currentHeight,
        int targetWidth,
        int targetHeight,
        bool expected)
    {
        var current = new PreviewOutputSize(currentWidth, currentHeight);
        var target = new PreviewOutputSize(targetWidth, targetHeight);

        Assert.Equal(expected, PreviewOutputSizePolicy.ShouldResize(current, target));
    }

    [Fact]
    public void ShouldResize_TreatsTheThresholdAsInclusive()
    {
        var current = new PreviewOutputSize(1920, 1080);
        var atThreshold = new PreviewOutputSize(
            1920 - PreviewOutputSizePolicy.MinimumResizeDeltaPixels,
            1080);
        var justUnder = new PreviewOutputSize(
            1920 - PreviewOutputSizePolicy.MinimumResizeDeltaPixels + 1,
            1080);

        Assert.True(PreviewOutputSizePolicy.ShouldResize(current, atThreshold));
        Assert.False(PreviewOutputSizePolicy.ShouldResize(current, justUnder));
    }

    [Fact]
    public void ShouldResize_IsSymmetricBetweenGrowAndShrink()
    {
        var small = new PreviewOutputSize(1280, 720);
        var large = new PreviewOutputSize(1920, 1080);

        Assert.Equal(
            PreviewOutputSizePolicy.ShouldResize(small, large),
            PreviewOutputSizePolicy.ShouldResize(large, small));
    }
}
