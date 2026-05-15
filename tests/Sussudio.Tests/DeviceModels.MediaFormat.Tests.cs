using System.Threading.Tasks;

static partial class Program
{
    private static Task MediaFormat_Equality_WithMatchingRationalFrameRates()
    {
        var a = CreateInstance("Sussudio.Models.MediaFormat");
        SetPropertyOrBackingField(a, "Width", 1920u);
        SetPropertyOrBackingField(a, "Height", 1080u);
        SetPropertyOrBackingField(a, "FrameRateNumerator", 60000u);
        SetPropertyOrBackingField(a, "FrameRateDenominator", 1001u);
        SetPropertyOrBackingField(a, "PixelFormat", "NV12");
        SetPropertyOrBackingField(a, "IsHdr", false);

        var b = CreateInstance("Sussudio.Models.MediaFormat");
        SetPropertyOrBackingField(b, "Width", 1920u);
        SetPropertyOrBackingField(b, "Height", 1080u);
        SetPropertyOrBackingField(b, "FrameRateNumerator", 60000u);
        SetPropertyOrBackingField(b, "FrameRateDenominator", 1001u);
        SetPropertyOrBackingField(b, "PixelFormat", "NV12");
        SetPropertyOrBackingField(b, "IsHdr", false);

        AssertEqual(true, a.Equals(b), "MediaFormat rational equality");
        return Task.CompletedTask;
    }

    private static Task MediaFormat_Inequality_WhenDimensionsDiffer()
    {
        var a = CreateInstance("Sussudio.Models.MediaFormat");
        SetPropertyOrBackingField(a, "Width", 1920u);
        SetPropertyOrBackingField(a, "Height", 1080u);
        SetPropertyOrBackingField(a, "FrameRate", 60.0);
        SetPropertyOrBackingField(a, "PixelFormat", "NV12");
        SetPropertyOrBackingField(a, "IsHdr", false);

        var b = CreateInstance("Sussudio.Models.MediaFormat");
        SetPropertyOrBackingField(b, "Width", 3840u);
        SetPropertyOrBackingField(b, "Height", 2160u);
        SetPropertyOrBackingField(b, "FrameRate", 60.0);
        SetPropertyOrBackingField(b, "PixelFormat", "NV12");
        SetPropertyOrBackingField(b, "IsHdr", false);

        AssertEqual(false, a.Equals(b), "MediaFormat dimension inequality");
        return Task.CompletedTask;
    }

    private static Task MediaFormat_GetHashCode_ConsistencyForEqualObjects()
    {
        var a = CreateInstance("Sussudio.Models.MediaFormat");
        SetPropertyOrBackingField(a, "Width", 3840u);
        SetPropertyOrBackingField(a, "Height", 2160u);
        SetPropertyOrBackingField(a, "FrameRateNumerator", 120000u);
        SetPropertyOrBackingField(a, "FrameRateDenominator", 1001u);
        SetPropertyOrBackingField(a, "PixelFormat", "P010");
        SetPropertyOrBackingField(a, "IsHdr", true);

        var b = CreateInstance("Sussudio.Models.MediaFormat");
        SetPropertyOrBackingField(b, "Width", 3840u);
        SetPropertyOrBackingField(b, "Height", 2160u);
        SetPropertyOrBackingField(b, "FrameRateNumerator", 120000u);
        SetPropertyOrBackingField(b, "FrameRateDenominator", 1001u);
        SetPropertyOrBackingField(b, "PixelFormat", "P010");
        SetPropertyOrBackingField(b, "IsHdr", true);

        AssertEqual(a.GetHashCode(), b.GetHashCode(), "MediaFormat hash consistency");
        return Task.CompletedTask;
    }
}
