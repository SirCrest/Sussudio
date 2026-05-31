using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

static partial class Program
{
    private static string ReadLibAvEncoderSource()
    {
        var parts = new[]
        {
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.Audio.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.OutputLifecycle.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    private static object CreateValidEncoderOptions()
    {
        var optionsType = RequireType("Sussudio.Services.Recording.LibAvEncoderOptions");
        var options = RuntimeHelpers.GetUninitializedObject(optionsType);
        SetPropertyBackingField(options, "OutputPath", "/output/test.mp4");
        SetPropertyBackingField(options, "CodecName", "hevc_nvenc");
        SetPropertyBackingField(options, "Width", 1920);
        SetPropertyBackingField(options, "Height", 1080);
        SetPropertyBackingField(options, "FrameRate", 60.0);
        SetPropertyBackingField(options, "BitRate", (uint)50_000_000);
        SetPropertyBackingField(options, "AudioEnabled", false);
        SetPropertyBackingField(options, "HdrEnabled", false);
        return options;
    }
}

static partial class Program
{
    internal static Task LibAvEncoder_VideoBitstreamFilterSpec_ChainsHdrAndMpegTsFilters()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("GetVideoBitstreamFilterSpec",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetVideoBitstreamFilterSpec not found.");

        var hdrHevcTs = CreateValidEncoderOptions();
        SetPropertyBackingField(hdrHevcTs, "CodecName", "hevc_nvenc");
        SetPropertyBackingField(hdrHevcTs, "ContainerFormat", "mpegts");
        SetPropertyBackingField(hdrHevcTs, "HdrEnabled", true);
        SetPropertyBackingField(hdrHevcTs, "IsP010", true);
        AssertEqual(
            "hevc_metadata=colour_primaries=9:transfer_characteristics=16:matrix_coefficients=9,dump_extra",
            method.Invoke(null, new[] { hdrHevcTs })?.ToString(),
            "HDR HEVC MPEG-TS chains HDR metadata and parameter-set filters");

        var sdrHevcTs = CreateValidEncoderOptions();
        SetPropertyBackingField(sdrHevcTs, "CodecName", "hevc_nvenc");
        SetPropertyBackingField(sdrHevcTs, "ContainerFormat", "mpegts");
        SetPropertyBackingField(sdrHevcTs, "HdrEnabled", false);
        AssertEqual("dump_extra", method.Invoke(null, new[] { sdrHevcTs })?.ToString(), "SDR HEVC MPEG-TS dumps parameter sets");

        var hdrHevcMp4 = CreateValidEncoderOptions();
        SetPropertyBackingField(hdrHevcMp4, "CodecName", "hevc_nvenc");
        SetPropertyBackingField(hdrHevcMp4, "ContainerFormat", "mp4");
        SetPropertyBackingField(hdrHevcMp4, "HdrEnabled", true);
        SetPropertyBackingField(hdrHevcMp4, "IsP010", true);
        AssertEqual(
            "hevc_metadata=colour_primaries=9:transfer_characteristics=16:matrix_coefficients=9",
            method.Invoke(null, new[] { hdrHevcMp4 })?.ToString(),
            "HDR HEVC MP4 keeps HDR metadata filter");

        var hdrAv1Mp4 = CreateValidEncoderOptions();
        SetPropertyBackingField(hdrAv1Mp4, "CodecName", "av1_nvenc");
        SetPropertyBackingField(hdrAv1Mp4, "ContainerFormat", "mp4");
        SetPropertyBackingField(hdrAv1Mp4, "HdrEnabled", true);
        SetPropertyBackingField(hdrAv1Mp4, "IsP010", true);
        AssertEqual(
            "av1_metadata=color_primaries=9:transfer_characteristics=16:matrix_coefficients=9",
            method.Invoke(null, new[] { hdrAv1Mp4 })?.ToString(),
            "HDR AV1 MP4 keeps AV1 metadata filter");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_MapNvencPreset_MapsCorrectly()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("MapNvencPreset",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("MapNvencPreset not found.");

        AssertEqual("p4", method.Invoke(null, new object?[] { null })!.ToString(), "null → p4");
        AssertEqual("p4", method.Invoke(null, new object[] { "Auto" })!.ToString(), "Auto → p4");
        AssertEqual("p1", method.Invoke(null, new object[] { "Fast" })!.ToString(), "Fast → p1");
        AssertEqual("p7", method.Invoke(null, new object[] { "Slow" })!.ToString(), "Slow → p7");
        AssertEqual("custom", method.Invoke(null, new object[] { "custom" })!.ToString(), "custom passthrough");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_MpegTsNvencDumpsHeadersForRotatedSegments()
    {
        var sourceText = ReadLibAvEncoderSource();

        AssertContains(sourceText, "private void InitializeVideoBitstreamFilterIfNeeded(LibAvEncoderOptions options)");
        AssertContains(sourceText, "GetVideoBitstreamFilterSpec(options)");
        AssertContains(sourceText, "ffmpeg.av_bsf_list_parse_str(filterSpec, &bsfCtx)");
        AssertContains(sourceText, "string.Join(\",\", filters)");
        AssertContains(sourceText, "filters.Add(hdrFilter)");
        AssertContains(sourceText, "filters.Add(parameterSetFilter)");
        AssertContains(sourceText, "IsMpegTsParameterSetFilterCandidate(options) ? \"dump_extra\" : null");
        AssertContains(sourceText, "hevc_metadata=colour_primaries=9:transfer_characteristics=16:matrix_coefficients=9");
        AssertContains(sourceText, "av1_metadata=color_primaries=9:transfer_characteristics=16:matrix_coefficients=9");
        AssertContains(sourceText, "string.Equals(options.ContainerFormat, \"mpegts\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(sourceText, "options.CodecName.Contains(\"h264\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(sourceText, "options.CodecName.Contains(\"hevc\", StringComparison.OrdinalIgnoreCase)");
        AssertContains(sourceText, "ffmpeg.av_opt_set_int(codecContext->priv_data, \"forced-idr\", 1, 0)");
        AssertContains(sourceText, "av_opt_set_int(forced-idr)");
        AssertContains(sourceText, "TryMapSplitEncodeMode(options.SplitEncodeMode, out var splitEncodeMode)");
        AssertContains(sourceText, "ffmpeg.av_opt_set_int(codecContext->priv_data, \"split_encode_mode\", splitEncodeMode, 0)");
        AssertContains(sourceText, "splitEncodeMode is 2 or 3");
        AssertContains(sourceText, "public string SplitEncodeMode { get; init; } = \"Auto\";");
        AssertDoesNotContain(sourceText, "\"repeat_headers\"");
        // Suppression forwarder stays on LibAvEncoder for caller compatibility.
        AssertContains(sourceText, "internal static IDisposable SuppressRecoverableSeekFfmpegLogs()");
        AssertContains(sourceText, "FfmpegLogSuppressionScope.SuppressRecoverableSeekFfmpegLogs()");

        // Suppression implementation lives with FFmpeg runtime resolution and log callback routing.
        var suppressionText = ReadRepoFile("Sussudio/Services/Runtime/FfmpegRuntimeLocator.cs")
            .Replace("\r\n", "\n");
        AssertContains(suppressionText, "internal static bool ShouldSuppressRecoverableSeekFfmpegLog(string message)");
        AssertContains(suppressionText, "[ThreadStatic]\n    private static int _recoverableSeekLogSuppressionDepth;");
        AssertContains(suppressionText, "message.Contains(\"Could not find ref with POC\", StringComparison.Ordinal)");
        AssertContains(suppressionText, "message.Contains(\"Error constructing the frame RPS\", StringComparison.Ordinal)");
        AssertContains(suppressionText, "message.Contains(\"First slice in a frame missing\", StringComparison.Ordinal)");
        AssertContains(suppressionText, "message.Contains(\"PPS id out of range\", StringComparison.Ordinal)");
        AssertContains(suppressionText, "FFMPEG_LOG_RECOVERABLE_SEEK_SUPPRESSED");

        return Task.CompletedTask;
    }
}

static partial class Program
{
    internal static Task LibAvEncoder_GetExpectedFrameSizeBytes_CalculatesCorrectly()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("GetExpectedFrameSizeBytes",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetExpectedFrameSizeBytes not found.");

        // NV12: width * height * 3 / 2
        var nv12_1080 = (int)method.Invoke(null, new object[] { 1920, 1080, false })!;
        AssertEqual(1920 * 1080 * 3 / 2, nv12_1080, "NV12 1080p");

        // P010: width * height * 3
        var p010_1080 = (int)method.Invoke(null, new object[] { 1920, 1080, true })!;
        AssertEqual(1920 * 1080 * 3, p010_1080, "P010 1080p");

        // P010 is exactly 2x NV12
        AssertEqual(nv12_1080 * 2, p010_1080, "P010 is 2x NV12");

        // 4K
        var nv12_4k = (int)method.Invoke(null, new object[] { 3840, 2160, false })!;
        AssertEqual(3840 * 2160 * 3 / 2, nv12_4k, "NV12 4K");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_ThrowIfError_ThrowsOnNegative()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ThrowIfError",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ThrowIfError not found.");

        // Non-negative should not throw
        method.Invoke(null, new object[] { 0, "test" });
        method.Invoke(null, new object[] { 1, "test" });

        // Negative should throw (may throw InvalidOperationException or
        // DllNotFoundException if FFmpeg runtime isn't loaded for GetErrorString)
        var threw = false;
        try
        {
            method.Invoke(null, new object[] { -1, "test operation" });
        }
        catch (TargetInvocationException)
        {
            threw = true;
        }
        AssertEqual(true, threw, "ThrowIfError throws on negative error code");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_DiagnosticsHelpersLiveWithCoreState()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.Diagnostics.cs")),
            "LibAvEncoder diagnostics helpers live with core encoder state, not a standalone partial");
        AssertContains(rootText, "private void EnsureOpen()");
        AssertContains(rootText, "private static void ThrowIfError(int errorCode, string operation)");
        AssertContains(rootText, "private static string GetErrorString(int errorCode)");
        AssertContains(rootText, "private static InvalidOperationException CreateLibAvException(string message)");
        AssertContains(rootText, "private static void CheckDeviceRemoved(IntPtr d3d11Device)");

        return Task.CompletedTask;
    }
}

static partial class Program
{
    internal static Task LibAvEncoder_GetHdrBitstreamFilterName_MapsCodecs()
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

    internal static Task LibAvEncoder_Invert_SwapsNumeratorDenominator()
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

    internal static Task LibAvEncoder_ChromaticityAndLuminanceRationals_ParseCorrectly()
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

static partial class Program
{
    // LibAvEncoder: ValidateOptions

    internal static Task LibAvEncoder_ValidateOptions_AcceptsValidOptions()
    {
        var encoderType = RequireType("Sussudio.Services.Recording.LibAvEncoder");
        var method = encoderType.GetMethod("ValidateOptions",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ValidateOptions not found.");
        var options = CreateValidEncoderOptions();
        method.Invoke(null, new[] { options });
        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_ValidateOptions_RejectsEmptyOutputPath()
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

    internal static Task LibAvEncoder_ValidateOptions_RejectsZeroDimensions()
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

    internal static Task LibAvEncoder_ValidateOptions_RejectsHdrWithH264()
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

    internal static Task LibAvEncoder_ValidateOptions_RejectsHdrWithoutP010()
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

    internal static Task LibAvEncoder_ValidateOptions_RejectsMismatchedFrameRateParts()
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

static partial class Program
{
    internal static Task LibAvEncoder_PacketWritingLivesWithVideoSubmission()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var videoSubmissionText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.PacketWriting.cs")),
            "video packet drain/write helpers stay folded into video submission");
        AssertContains(videoSubmissionText, "private void DrainEncoderPackets()");
        AssertContains(videoSubmissionText, "private void WriteFilteredPackets()");
        AssertContains(videoSubmissionText, "private void DrainBsfPackets()");
        AssertContains(videoSubmissionText, "private void WritePacket(AVPacket* packet, bool useBsfTimeBase)");
        AssertDoesNotContain(rootText, "private void DrainEncoderPackets()");
        AssertDoesNotContain(rootText, "private void WriteFilteredPackets()");
        AssertDoesNotContain(rootText, "private void DrainBsfPackets()");
        AssertDoesNotContain(rootText, "private void WritePacket(AVPacket* packet, bool useBsfTimeBase)");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_FrameCopyLivesWithVideoSubmission()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var videoSubmissionText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs")
            .Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.FrameCopy.cs")),
            "CPU packed-frame copy is part of video submission, not a standalone partial");
        AssertContains(videoSubmissionText, "private void CopyPackedFrameToVideoFrame(ReadOnlySpan<byte> frameData, LibAvEncoderOptions options)");
        AssertContains(videoSubmissionText, "private static void CopyPlane(byte* sourceStart, byte* destinationStart, int destinationStride, int rowBytes, int rowCount)");
        AssertContains(videoSubmissionText, "Buffer.MemoryCopy(");
        AssertDoesNotContain(rootText, "private void CopyPackedFrameToVideoFrame(ReadOnlySpan<byte> frameData, LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private static void CopyPlane(byte* sourceStart, byte* destinationStart, int destinationStride, int rowBytes, int rowCount)");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_VideoSubmissionLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var videoSubmissionText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs")
            .Replace("\r\n", "\n");
        var hardwareFramesText = videoSubmissionText;

        AssertContains(videoSubmissionText, "public void SendVideoFrame(ReadOnlySpan<byte> frameData, int width, int height)");
        AssertContains(videoSubmissionText, "CopyPackedFrameToVideoFrame(frameData[..expectedSize], options);");
        AssertContains(videoSubmissionText, "private void CopyPackedFrameToVideoFrame(ReadOnlySpan<byte> frameData, LibAvEncoderOptions options)");
        AssertContains(videoSubmissionText, "private bool AttachHdrFrameSideDataIfNeeded(LibAvEncoderOptions options)");
        AssertContains(videoSubmissionText, "private bool AttachHdrFrameSideDataToHwFrame(LibAvEncoderOptions options)");
        AssertContains(videoSubmissionText, "public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)");
        AssertContains(videoSubmissionText, "public void SendCudaVideoFrame(AVFrame* decodedFrame)");
        AssertContains(hardwareFramesText, "public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)");
        AssertContains(hardwareFramesText, "public void SendCudaVideoFrame(AVFrame* decodedFrame)");
        AssertContains(hardwareFramesText, "CopySubresourceRegion");
        AssertContains(hardwareFramesText, "AttachHdrFrameSideDataToHwFrame(options)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.VideoSubmission.cs")),
            "CPU video submission folded into LibAvEncoder.VideoFrames.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.HardwareFrames.cs")),
            "hardware frame setup/submission folded into LibAvEncoder.VideoFrames.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.HardwareSubmission.cs")),
            "hardware frame submission lives with LibAvEncoder.VideoFrames.cs");
        AssertDoesNotContain(rootText, "public void SendVideoFrame(ReadOnlySpan<byte> frameData, int width, int height)");
        AssertDoesNotContain(rootText, "public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)");
        AssertDoesNotContain(rootText, "public void SendCudaVideoFrame(AVFrame* decodedFrame)");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_InitializationLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var initializationText = rootText;

        AssertContains(initializationText, "public static void InitializeFFmpeg(bool requireNativeRuntime = false)");
        AssertContains(initializationText, "public void Initialize(LibAvEncoderOptions options)");
        AssertContains(initializationText, "ThrowIfError(ffmpeg.avcodec_open2(_videoCodecCtx, codec, null), \"avcodec_open2\");");
        AssertContains(initializationText, "ApplyMp4MuxerOptions(options.ContainerFormat, options.FragmentedMp4, &muxerOptions, \"open\");");
        AssertContains(initializationText, "CleanupResources(writeTrailer: false);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.Initialization.cs")),
            "LibAvEncoder initialization folded into the encoder root");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_SetupAndModelsLiveInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var initializationText = rootText;
        var audioText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.Audio.cs")
            .Replace("\r\n", "\n");
        var videoSubmissionText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoFrames.cs")
            .Replace("\r\n", "\n");
        var modelsText = rootText;
        var hardwareFramesText = videoSubmissionText;

        AssertContains(audioText, "public void SendAudioSamples(ReadOnlySpan<byte> f32leSamples)");
        AssertContains(audioText, "public void SendMicrophoneSamples(ReadOnlySpan<byte> f32leSamples)");
        AssertContains(audioText, "private unsafe struct AudioStreamState");
        AssertContains(audioText, "private void DrainStreamEncoderPackets(ref AudioStreamState s)");
        AssertContains(audioText, "private void WriteStreamPacket(ref AudioStreamState s, AVPacket* packet)");
        AssertContains(audioText, "private void FlushPendingStreamSamples(ref AudioStreamState s, string streamLabel,");
        AssertContains(audioText, "private void CopyToAccumulator(ref AudioStreamState s, ReadOnlySpan<byte> source, int destinationOffset)");
        AssertContains(audioText, "private void EncodeStreamChunk(ref AudioStreamState s, byte* inputPtr, int inputSamples,");
        AssertContains(audioText, "private void DrainBufferedFrames(ref AudioStreamState s, bool flushPartialFrame)");
        AssertContains(audioText, "private void SendPreparedStreamFrame(ref AudioStreamState s, int sampleCount)");
        AssertContains(audioText, "private void CopyQueuedSamplesToStreamFrame(ref AudioStreamState s, int sampleCount)");
        AssertContains(audioText, "private void RemoveQueuedStreamSamples(ref AudioStreamState s, int sampleCount)");
        AssertContains(audioText, "private void InitializeAudioIfNeeded(LibAvEncoderOptions options)");
        AssertContains(audioText, "private void InitializeMicrophoneIfNeeded(LibAvEncoderOptions options)");
        AssertContains(audioText, "ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_AAC)");
        AssertContains(audioText, "private void ConfigureAudioCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options, AVCodec* codec)");
        AssertContains(audioText, "private void InitializeAudioResampler(LibAvEncoderOptions options)");
        AssertContains(audioText, "private void AllocateAudioFrame()");
        AssertContains(audioText, "private void AllocateAudioAccumulator(LibAvEncoderOptions options)");
        AssertContains(audioText, "private void AllocateAudioSampleQueue(LibAvEncoderOptions options)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.AudioSetup.cs")),
            "Audio setup helpers live with audio stream initialization");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.AudioSubmission.cs")),
            "Audio sample submission folded into LibAvEncoder.Audio.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.AudioQueue.cs")),
            "Audio queue and A/V sync helpers folded into LibAvEncoder.Audio.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.AudioInitialization.cs")),
            "Audio stream initialization folded into LibAvEncoder.Audio.cs");
        AssertContains(videoSubmissionText, "private bool AttachHdrFrameSideDataIfNeeded(LibAvEncoderOptions options)");
        AssertContains(videoSubmissionText, "private bool AttachHdrFrameSideDataToHwFrame(LibAvEncoderOptions options)");
        AssertContains(videoSubmissionText, "ffmpeg.av_mastering_display_metadata_create_side_data(_videoFrame)");
        AssertContains(videoSubmissionText, "ffmpeg.av_mastering_display_metadata_create_side_data(_hwFrame)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.HdrSideData.cs")),
            "HDR side-data helpers live with LibAvEncoder.VideoFrames.cs");
        AssertContains(modelsText, "internal sealed record LibAvEncoderOptions");
        AssertContains(modelsText, "internal readonly record struct RotateOutputResult");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.Models.cs")),
            "LibAvEncoder option/result models live with the encoder root");
        AssertContains(initializationText, "private void ConfigureVideoCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options)");
        AssertContains(initializationText, "private void ApplyEncoderPrivateOptions(AVCodecContext* codecContext, LibAvEncoderOptions options)");
        AssertContains(initializationText, "private void InitializeVideoBitstreamFilterIfNeeded(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static string? GetVideoBitstreamFilterSpec(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static string MapNvencPreset(string? preset)");
        AssertContains(initializationText, "private static bool TryMapSplitEncodeMode(string? splitEncodeMode, out long value)");
        AssertContains(initializationText, "private static AVRational ResolveFrameRate(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static bool IsSampleFormatSupported(AVCodec* codec, AVSampleFormat sampleFormat)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.VideoSetup.cs")),
            "Video codec setup helpers live with encoder initialization");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.CodecPolicy.cs")),
            "LibAvEncoder codec/filter/rational policy lives with encoder initialization");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.Initialization.cs")),
            "LibAvEncoder initialization lives with the encoder root");
        AssertContains(hardwareFramesText, "private static IntPtr CreateSingleTexture2D(IntPtr d3d11Device, int width, int height, bool isP010, uint bindFlags)");
        AssertContains(hardwareFramesText, "private void InitializeHardwareFramesIfNeeded(LibAvEncoderOptions options)");
        AssertContains(hardwareFramesText, "framesCtx->initial_pool_size = 0;");
        AssertContains(hardwareFramesText, "private void InitializeCudaHardwareFrames(LibAvEncoderOptions options)");
        AssertContains(hardwareFramesText, "_useCudaHardwareFrames = true;");
        AssertContains(hardwareFramesText, "AVPixelFormat.AV_PIX_FMT_CUDA");
        AssertContains(hardwareFramesText, "public void SendGpuVideoFrame(IntPtr d3d11Texture, int subresourceIndex)");
        AssertContains(hardwareFramesText, "public void SendCudaVideoFrame(AVFrame* decodedFrame)");
        AssertContains(hardwareFramesText, "CopySubresourceRegion");
        AssertContains(hardwareFramesText, "AttachHdrFrameSideDataToHwFrame(options)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.HardwareFrames.Cuda.cs")),
            "CUDA hardware frame adoption lives with the hardware frame initializer");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.HardwareSubmission.cs")),
            "hardware frame submission lives with LibAvEncoder.VideoFrames.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.VideoSubmission.cs")),
            "CPU video submission folded into LibAvEncoder.VideoFrames.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.HardwareFrames.cs")),
            "hardware frame setup/submission folded into LibAvEncoder.VideoFrames.cs");
        AssertContains(initializationText, "private static void ValidateOptions(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static void ValidateRequiredVideoOptions(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static void ValidateAudioOptions(LibAvEncoderOptions options)");
        AssertContains(initializationText, "private static void ValidateHdrOptions(LibAvEncoderOptions options)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.OptionsValidation.cs")),
            "LibAvEncoder option validation folded into encoder initialization");
        AssertDoesNotContain(rootText, "private void ConfigureAudioCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options, AVCodec* codec)");
        AssertDoesNotContain(rootText, "private void InitializeAudioResampler(LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private void AllocateAudioFrame()");
        AssertDoesNotContain(rootText, "private bool AttachHdrFrameSideDataIfNeeded(LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private bool AttachHdrFrameSideDataToHwFrame(LibAvEncoderOptions options)");
        AssertDoesNotContain(initializationText, "private static IntPtr CreateSingleTexture2D(IntPtr d3d11Device, int width, int height, bool isP010, uint bindFlags)");
        AssertDoesNotContain(initializationText, "private void InitializeHardwareFramesIfNeeded(LibAvEncoderOptions options)");
        AssertDoesNotContain(initializationText, "private void InitializeCudaHardwareFrames(LibAvEncoderOptions options)");
        AssertDoesNotContain(hardwareFramesText, "private void InitializeVideoBitstreamFilterIfNeeded(LibAvEncoderOptions options)");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_OutputLifecycleLivesInFocusedOwner()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var outputLifecycleText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.OutputLifecycle.cs")
            .Replace("\r\n", "\n");

        AssertContains(outputLifecycleText, "public RotateOutputResult RotateOutput(string newPath)");
        AssertContains(outputLifecycleText, "private void CloseCurrentOutputIo()");
        AssertContains(outputLifecycleText, "private void ReinitializeOutputContext(string outputPath)");
        AssertContains(outputLifecycleText, "private void ReinitializeVideoStream()");
        AssertContains(outputLifecycleText, "private void ResetSegmentRuntimeState()");
        AssertContains(outputLifecycleText, "private static unsafe void ApplyMp4MuxerOptions(");
        AssertContains(outputLifecycleText, "frag_keyframe+empty_moov");
        AssertContains(outputLifecycleText, "public void FlushAndClose()");
        AssertContains(outputLifecycleText, "public void Dispose()");
        AssertContains(outputLifecycleText, "private void CleanupResources(bool writeTrailer)");
        AssertContains(outputLifecycleText, "var finalMicSamplesReceived = ReleaseNativeResources(useCudaHardwareFrames);");
        AssertContains(outputLifecycleText, "ffmpeg.av_write_trailer(_formatCtx)");
        AssertContains(outputLifecycleText, "private long ReleaseNativeResources(bool useCudaHardwareFrames)");
        AssertContains(outputLifecycleText, "ffmpeg.avio_closep(&_formatCtx->pb)");
        AssertContains(outputLifecycleText, "Marshal.Release(_hwPoolTextures[i]);");
        AssertContains(outputLifecycleText, "ffmpeg.avcodec_free_context(&videoCodecCtx)");
        AssertContains(outputLifecycleText, "_isOpen = false;");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.OutputRotation.cs")),
            "output rotation folded into LibAvEncoder.OutputLifecycle.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.ResourceCleanup.cs")),
            "resource cleanup folded into LibAvEncoder.OutputLifecycle.cs");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Recording", "LibAvEncoder.NativeResourceRelease.cs")),
            "Native resource release folded into LibAvEncoder.OutputLifecycle.cs");
        AssertDoesNotContain(rootText, "public RotateOutputResult RotateOutput(string newPath)");
        AssertDoesNotContain(rootText, "public void FlushAndClose()");
        AssertDoesNotContain(rootText, "public void Dispose()");

        return Task.CompletedTask;
    }

    internal static Task LibAvEncoder_FragmentedMp4UsesShortFragmentsForPlayback()
    {
        var sourceText = ReadLibAvEncoderSource();

        AssertContains(sourceText, "private static unsafe void ApplyMp4MuxerOptions(");
        AssertContains(sourceText, "ApplyMp4MuxerOptions(options.ContainerFormat, options.FragmentedMp4, &muxerOptions, \"open\");");
        AssertContains(sourceText, "ApplyMp4MuxerOptions(containerFormat, _options?.FragmentedMp4 ?? false, &muxerOptions, \"rotate\");");
        AssertContains(sourceText, "frag_keyframe+empty_moov");
        AssertContains(sourceText, "ffmpeg.av_dict_set(muxerOptions, \"frag_duration\", \"100000\", 0)");
        AssertContains(sourceText, "ffmpeg.av_dict_set(muxerOptions, \"flush_packets\", \"1\", 0)");
        AssertDoesNotContain(sourceText, "var movflags = options.FragmentedMp4\n                        ? \"frag_keyframe+empty_moov\"");
        AssertDoesNotContain(sourceText, "var movflags = (_options?.FragmentedMp4 ?? false)\n                    ? \"frag_keyframe+empty_moov\"");

        return Task.CompletedTask;
    }
}
