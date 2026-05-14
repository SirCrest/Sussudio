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
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.CodecPolicy.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.AvSync.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.PacketWriting.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.FrameCopy.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.Diagnostics.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.AudioSetup.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.HdrSideData.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.Models.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.VideoSetup.cs").Replace("\r\n", "\n"),
            ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.OutputLifecycle.cs").Replace("\r\n", "\n")
        };

        return string.Join("\n", parts);
    }

    // ── LibAvEncoder: GetHdrBitstreamFilterName ──

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

    // ── LibAvEncoder: GetExpectedFrameSizeBytes ──

    private static Task LibAvEncoder_VideoBitstreamFilterSpec_ChainsHdrAndMpegTsFilters()
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

    private static Task LibAvEncoder_GetExpectedFrameSizeBytes_CalculatesCorrectly()
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

    // ── LibAvEncoder: MapNvencPreset ──

    private static Task LibAvEncoder_MapNvencPreset_MapsCorrectly()
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

    // ── LibAvEncoder: ThrowIfError ──

    private static Task LibAvEncoder_ThrowIfError_ThrowsOnNegative()
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

    // ── LibAvEncoder: Invert ──

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

    // ── HdrMasterDisplayMetadata: ToChromaticityRational and ToLuminanceRational ──

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

    // ── LibAvEncoder: ValidateOptions ──

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

    private static Task LibAvEncoder_FragmentedMp4UsesShortFragmentsForPlayback()
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

    private static Task LibAvEncoder_MpegTsNvencDumpsHeadersForRotatedSegments()
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

        // Suppression implementation lives in its own helper file.
        var suppressionText = ReadRepoFile("Sussudio/Services/Runtime/FfmpegLogSuppressionScope.cs")
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

    private static Task LibAvEncoder_PacketWritingLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var packetWritingText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.PacketWriting.cs")
            .Replace("\r\n", "\n");

        AssertContains(packetWritingText, "private void DrainEncoderPackets()");
        AssertContains(packetWritingText, "private void WriteFilteredPackets()");
        AssertContains(packetWritingText, "private void DrainBsfPackets()");
        AssertContains(packetWritingText, "private void WritePacket(AVPacket* packet, bool useBsfTimeBase)");
        AssertDoesNotContain(rootText, "private void DrainEncoderPackets()");
        AssertDoesNotContain(rootText, "private void WriteFilteredPackets()");
        AssertDoesNotContain(rootText, "private void DrainBsfPackets()");
        AssertDoesNotContain(rootText, "private void WritePacket(AVPacket* packet, bool useBsfTimeBase)");

        return Task.CompletedTask;
    }

    private static Task LibAvEncoder_FrameCopyLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var frameCopyText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.FrameCopy.cs")
            .Replace("\r\n", "\n");

        AssertContains(frameCopyText, "private void CopyPackedFrameToVideoFrame(ReadOnlySpan<byte> frameData, LibAvEncoderOptions options)");
        AssertContains(frameCopyText, "private static void CopyPlane(byte* sourceStart, byte* destinationStart, int destinationStride, int rowBytes, int rowCount)");
        AssertContains(frameCopyText, "Buffer.MemoryCopy(");
        AssertDoesNotContain(rootText, "private void CopyPackedFrameToVideoFrame(ReadOnlySpan<byte> frameData, LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private static void CopyPlane(byte* sourceStart, byte* destinationStart, int destinationStride, int rowBytes, int rowCount)");

        return Task.CompletedTask;
    }

    private static Task LibAvEncoder_DiagnosticsHelpersLiveInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var diagnosticsText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.Diagnostics.cs")
            .Replace("\r\n", "\n");

        AssertContains(diagnosticsText, "private void EnsureOpen()");
        AssertContains(diagnosticsText, "private static void ThrowIfError(int errorCode, string operation)");
        AssertContains(diagnosticsText, "private static string GetErrorString(int errorCode)");
        AssertContains(diagnosticsText, "private static InvalidOperationException CreateLibAvException(string message)");
        AssertContains(diagnosticsText, "private static void CheckDeviceRemoved(IntPtr d3d11Device)");
        AssertDoesNotContain(rootText, "private void EnsureOpen()");
        AssertDoesNotContain(rootText, "private static void ThrowIfError(int errorCode, string operation)");
        AssertDoesNotContain(rootText, "private static string GetErrorString(int errorCode)");
        AssertDoesNotContain(rootText, "private static InvalidOperationException CreateLibAvException(string message)");
        AssertDoesNotContain(rootText, "private static void CheckDeviceRemoved(IntPtr d3d11Device)");

        return Task.CompletedTask;
    }

    private static Task LibAvEncoder_SetupAndModelsLiveInFocusedPartials()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.cs")
            .Replace("\r\n", "\n");
        var audioSetupText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.AudioSetup.cs")
            .Replace("\r\n", "\n");
        var hdrSideDataText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.HdrSideData.cs")
            .Replace("\r\n", "\n");
        var modelsText = ReadRepoFile("Sussudio/Services/Recording/LibAvEncoder.Models.cs")
            .Replace("\r\n", "\n");

        AssertContains(audioSetupText, "private void ConfigureAudioCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options, AVCodec* codec)");
        AssertContains(audioSetupText, "private void InitializeAudioResampler(LibAvEncoderOptions options)");
        AssertContains(audioSetupText, "private void AllocateAudioFrame()");
        AssertContains(audioSetupText, "private void AllocateAudioAccumulator(LibAvEncoderOptions options)");
        AssertContains(audioSetupText, "private void AllocateAudioSampleQueue(LibAvEncoderOptions options)");
        AssertContains(hdrSideDataText, "private bool AttachHdrFrameSideDataIfNeeded(LibAvEncoderOptions options)");
        AssertContains(hdrSideDataText, "private bool AttachHdrFrameSideDataToHwFrame(LibAvEncoderOptions options)");
        AssertContains(hdrSideDataText, "ffmpeg.av_mastering_display_metadata_create_side_data(_videoFrame)");
        AssertContains(hdrSideDataText, "ffmpeg.av_mastering_display_metadata_create_side_data(_hwFrame)");
        AssertContains(modelsText, "internal sealed record LibAvEncoderOptions");
        AssertContains(modelsText, "internal readonly record struct RotateOutputResult");
        AssertDoesNotContain(rootText, "private void ConfigureAudioCodecContext(AVCodecContext* codecContext, LibAvEncoderOptions options, AVCodec* codec)");
        AssertDoesNotContain(rootText, "private void InitializeAudioResampler(LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private void AllocateAudioFrame()");
        AssertDoesNotContain(rootText, "private bool AttachHdrFrameSideDataIfNeeded(LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "private bool AttachHdrFrameSideDataToHwFrame(LibAvEncoderOptions options)");
        AssertDoesNotContain(rootText, "internal sealed record LibAvEncoderOptions");
        AssertDoesNotContain(rootText, "internal readonly record struct RotateOutputResult");

        return Task.CompletedTask;
    }
}
