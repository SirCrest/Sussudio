using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
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
}
