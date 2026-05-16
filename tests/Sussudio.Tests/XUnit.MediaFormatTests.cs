using System;
using Xunit;

namespace Sussudio.Tests;

public class MediaFormatTests
{
    [Fact]
    public void MediaFormat_Equality_WithMatchingRationalFrameRates()
    {
        var a = CreateMediaFormat(
            width: 1920u,
            height: 1080u,
            frameRateNumerator: 60000u,
            frameRateDenominator: 1001u,
            pixelFormat: "NV12",
            isHdr: false);
        var b = CreateMediaFormat(
            width: 1920u,
            height: 1080u,
            frameRateNumerator: 60000u,
            frameRateDenominator: 1001u,
            pixelFormat: "NV12",
            isHdr: false);

        Assert.True(a.Equals(b));
    }

    [Fact]
    public void MediaFormat_Inequality_WhenDimensionsDiffer()
    {
        var a = CreateMediaFormat(
            width: 1920u,
            height: 1080u,
            frameRate: 60.0,
            pixelFormat: "NV12",
            isHdr: false);
        var b = CreateMediaFormat(
            width: 3840u,
            height: 2160u,
            frameRate: 60.0,
            pixelFormat: "NV12",
            isHdr: false);

        Assert.False(a.Equals(b));
    }

    [Fact]
    public void MediaFormat_GetHashCode_ConsistencyForEqualObjects()
    {
        var a = CreateMediaFormat(
            width: 3840u,
            height: 2160u,
            frameRateNumerator: 120000u,
            frameRateDenominator: 1001u,
            pixelFormat: "P010",
            isHdr: true);
        var b = CreateMediaFormat(
            width: 3840u,
            height: 2160u,
            frameRateNumerator: 120000u,
            frameRateDenominator: 1001u,
            pixelFormat: "P010",
            isHdr: true);

        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    private static object CreateMediaFormat(
        uint width,
        uint height,
        string pixelFormat,
        bool isHdr,
        double? frameRate = null,
        uint? frameRateNumerator = null,
        uint? frameRateDenominator = null)
    {
        var mediaFormatType = SussudioAssembly.Load().GetType("Sussudio.Models.MediaFormat", throwOnError: true)!;
        var format = Activator.CreateInstance(mediaFormatType)
            ?? throw new InvalidOperationException("Failed to create MediaFormat.");

        SetProperty(format, "Width", width);
        SetProperty(format, "Height", height);
        if (frameRate.HasValue)
        {
            SetProperty(format, "FrameRate", frameRate.Value);
        }

        if (frameRateNumerator.HasValue)
        {
            SetProperty(format, "FrameRateNumerator", frameRateNumerator.Value);
        }

        if (frameRateDenominator.HasValue)
        {
            SetProperty(format, "FrameRateDenominator", frameRateDenominator.Value);
        }

        SetProperty(format, "PixelFormat", pixelFormat);
        SetProperty(format, "IsHdr", isHdr);
        return format;
    }

    private static void SetProperty(object instance, string propertyName, object value)
    {
        var property = instance.GetType().GetProperty(propertyName)
            ?? throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} was not found.");
        property.SetValue(instance, value);
    }
}
