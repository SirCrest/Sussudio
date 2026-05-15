using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    private static Task LibAvEncoder_GetHdrBitstreamFilterName_MapsCodecs()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("GetHdrBitstreamFilterName",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetHdrBitstreamFilterName not found.");

        // HEVC variants → "hevc_metadata"
        var hevc1 = method.Invoke(null, new object[] { "hevc_nvenc" })?.ToString();
        AssertEqual("hevc_metadata", hevc1!, "hevc_nvenc → hevc_metadata");

        var hevc2 = method.Invoke(null, new object[] { "libx265" })?.ToString();
        // libx265 doesn't contain "hevc" so should return null
        AssertEqual(true, hevc2 == null, "libx265 → null (no hevc substring)");

        // AV1 → "av1_metadata"
        var av1 = method.Invoke(null, new object[] { "av1_nvenc" })?.ToString();
        AssertEqual("av1_metadata", av1!, "av1_nvenc → av1_metadata");

        // H264 → null (no HDR bitstream filter)
        var h264 = method.Invoke(null, new object?[] { "h264_nvenc" });
        AssertEqual(true, h264 == null, "h264 → null");

        return Task.CompletedTask;
    }

    private static Task LibAvEncoder_Invert_SwapsNumeratorDenominator()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("Invert",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Invert not found.");

        // The method takes AVRational which is a struct from FFmpeg.AutoGen
        // AVRational has fields: int num, int den
        var avRationalType = method.GetParameters()[0].ParameterType;
        var input = Activator.CreateInstance(avRationalType)!;
        avRationalType.GetField("num")!.SetValue(input, 60);
        avRationalType.GetField("den")!.SetValue(input, 1);

        var result = method.Invoke(null, new[] { input })!;
        var resultNum = (int)avRationalType.GetField("num")!.GetValue(result)!;
        var resultDen = (int)avRationalType.GetField("den")!.GetValue(result)!;

        AssertEqual(1, resultNum, "Inverted numerator");
        AssertEqual(60, resultDen, "Inverted denominator");

        return Task.CompletedTask;
    }

    private static Task LibAvEncoder_ChromaticityAndLuminanceRationals_ParseCorrectly()
    {
        var hdrType = RequireType("Sussudio.Services.Recording.HdrMasterDisplayMetadata");

        var chromaMethod = hdrType.GetMethod("ToChromaticityRational",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ToChromaticityRational not found.");
        var lumaMethod = hdrType.GetMethod("ToLuminanceRational",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ToLuminanceRational not found.");

        var avRationalType = chromaMethod.ReturnType;

        // ToChromaticityRational: int.Parse(value) / 50000
        var chromaResult = chromaMethod.Invoke(null, new object[] { "13250" })!;
        var chromaNum = (int)avRationalType.GetField("num")!.GetValue(chromaResult)!;
        var chromaDen = (int)avRationalType.GetField("den")!.GetValue(chromaResult)!;
        AssertEqual(13250, chromaNum, "Chromaticity numerator");
        AssertEqual(50000, chromaDen, "Chromaticity denominator");

        // ToLuminanceRational: int.Parse(value) / 10000
        var lumaResult = lumaMethod.Invoke(null, new object[] { "10000" })!;
        var lumaNum = (int)avRationalType.GetField("num")!.GetValue(lumaResult)!;
        var lumaDen = (int)avRationalType.GetField("den")!.GetValue(lumaResult)!;
        AssertEqual(10000, lumaNum, "Luminance numerator");
        AssertEqual(10000, lumaDen, "Luminance denominator");

        return Task.CompletedTask;
    }
}
