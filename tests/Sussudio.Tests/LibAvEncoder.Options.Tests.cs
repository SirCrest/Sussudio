using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    // LibAvEncoder: ValidateOptions

    private static Task LibAvEncoder_ValidateOptions_AcceptsValidOptions()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ValidateOptions not found.");
        var options = CreateValidEncoderOptions();
        method.Invoke(null, new[] { options });
        return Task.CompletedTask;
    }

    private static Task LibAvEncoder_ValidateOptions_RejectsEmptyOutputPath()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions", BindingFlags.Static | BindingFlags.NonPublic)!;
        var options = CreateValidEncoderOptions();
        SetPropertyBackingField(options, "OutputPath", "");
        var threw = false;
        try { method.Invoke(null, new[] { options }); }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException) { threw = true; }
        AssertEqual(true, threw, "Empty OutputPath throws ArgumentException");
        return Task.CompletedTask;
    }

    private static Task LibAvEncoder_ValidateOptions_RejectsZeroDimensions()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions", BindingFlags.Static | BindingFlags.NonPublic)!;
        var options = CreateValidEncoderOptions();
        SetPropertyBackingField(options, "Width", 0);
        var threw = false;
        try { method.Invoke(null, new[] { options }); }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException) { threw = true; }
        AssertEqual(true, threw, "Width=0 throws ArgumentOutOfRangeException");
        return Task.CompletedTask;
    }

    private static Task LibAvEncoder_ValidateOptions_RejectsHdrWithH264()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions", BindingFlags.Static | BindingFlags.NonPublic)!;
        var options = CreateValidEncoderOptions();
        SetPropertyBackingField(options, "HdrEnabled", true);
        SetPropertyBackingField(options, "IsP010", true);
        SetPropertyBackingField(options, "CodecName", "h264_nvenc");
        var threw = false;
        try { method.Invoke(null, new[] { options }); }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) { threw = true; }
        AssertEqual(true, threw, "HDR with H264 throws InvalidOperationException");
        return Task.CompletedTask;
    }

    private static Task LibAvEncoder_ValidateOptions_RejectsHdrWithoutP010()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions", BindingFlags.Static | BindingFlags.NonPublic)!;
        var options = CreateValidEncoderOptions();
        SetPropertyBackingField(options, "HdrEnabled", true);
        SetPropertyBackingField(options, "IsP010", false);
        SetPropertyBackingField(options, "CodecName", "hevc_nvenc");
        var threw = false;
        try { method.Invoke(null, new[] { options }); }
        catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException) { threw = true; }
        AssertEqual(true, threw, "HDR without P010 throws InvalidOperationException");
        return Task.CompletedTask;
    }

    private static Task LibAvEncoder_ValidateOptions_RejectsMismatchedFrameRateParts()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions", BindingFlags.Static | BindingFlags.NonPublic)!;
        var options = CreateValidEncoderOptions();
        SetPropertyBackingField(options, "FrameRateNumerator", (int?)60000);
        SetPropertyBackingField(options, "FrameRateDenominator", (int?)null);
        var threw = false;
        try { method.Invoke(null, new[] { options }); }
        catch (TargetInvocationException ex) when (ex.InnerException is ArgumentException) { threw = true; }
        AssertEqual(true, threw, "Mismatched FrameRate parts throws ArgumentException");
        return Task.CompletedTask;
    }
}
