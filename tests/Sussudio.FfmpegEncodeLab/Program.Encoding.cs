using System.Globalization;

// FFmpeg argument and encoder-selection policy for the standalone HDR encode lab.
internal static partial class Program
{
    private static string[] BuildHevcArguments(Options options, string inputPath, string outputPath)
    {
        return
        [
            "-y",
            "-f", "rawvideo",
            "-pix_fmt", "p010le",
            "-s:v", $"{options.Width}x{options.Height}",
            "-r", options.Fps.ToString("0.###", CultureInfo.InvariantCulture),
            "-i", inputPath,
            "-frames:v", options.FrameCount.ToString(CultureInfo.InvariantCulture),
            "-an",
            "-c:v", "libx265",
            "-preset", "fast",
            "-crf", "24",
            "-pix_fmt", "yuv420p10le",
            "-profile:v", "main10",
            "-tag:v", "hvc1",
            "-x265-params", "hdr-opt=1:repeat-headers=1:colorprim=bt2020:transfer=smpte2084:colormatrix=bt2020nc",
            "-color_primaries", "bt2020",
            "-color_trc", "smpte2084",
            "-colorspace", "bt2020nc",
            outputPath
        ];
    }

    private static string[] BuildAv1Arguments(Options options, string inputPath, string outputPath, string encoder)
    {
        var args = new List<string>
        {
            "-y",
            "-f", "rawvideo",
            "-pix_fmt", "p010le",
            "-s:v", $"{options.Width}x{options.Height}",
            "-r", options.Fps.ToString("0.###", CultureInfo.InvariantCulture),
            "-i", inputPath,
            "-frames:v", options.FrameCount.ToString(CultureInfo.InvariantCulture),
            "-an",
            "-c:v", encoder,
            "-pix_fmt", "yuv420p10le",
            "-color_primaries", "bt2020",
            "-color_trc", "smpte2084",
            "-colorspace", "bt2020nc"
        };

        if (encoder.Equals("libsvtav1", StringComparison.OrdinalIgnoreCase))
        {
            args.AddRange(["-preset", "8", "-crf", "35"]);
        }
        else if (encoder.Equals("libaom-av1", StringComparison.OrdinalIgnoreCase))
        {
            args.AddRange(["-cpu-used", "6", "-crf", "35", "-row-mt", "1"]);
        }

        args.AddRange(["-movflags", "+faststart"]);
        args.Add(outputPath);
        return args.ToArray();
    }

    private static string[] BuildAv1MetadataFixupArguments(string inputPath, string outputPath)
    {
        return
        [
            "-y",
            "-i", inputPath,
            "-c:v", "copy",
            "-an",
            "-bsf:v", "av1_metadata=color_primaries=9:transfer_characteristics=16:matrix_coefficients=9",
            "-movflags", "+faststart",
            outputPath
        ];
    }

    private static string[] BuildValidatorArguments(string validatorScriptPath, string outputPath, string codec)
    {
        return
        [
            "-NoProfile",
            "-ExecutionPolicy", "Bypass",
            "-File", validatorScriptPath,
            "-File", outputPath,
            "-ExpectHdr",
            "-Codec", codec
        ];
    }

    private static async Task<string> ResolveAv1EncoderAsync(string ffmpegPath, string logPath)
    {
        var result = await RunProcessAsync(
            ffmpegPath,
            ["-hide_banner", "-encoders"],
            logPath).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to enumerate FFmpeg encoders. See log: {logPath}");
        }

        var output = $"{result.StandardOutput}\n{result.StandardError}";
        if (ContainsToken(output, "libsvtav1"))
        {
            return "libsvtav1";
        }

        if (ContainsToken(output, "libaom-av1"))
        {
            return "libaom-av1";
        }

        throw new InvalidOperationException(
            $"No supported AV1 encoder found (expected libsvtav1 or libaom-av1). See log: {logPath}");
    }

    private static bool ContainsToken(string content, string token)
    {
        return content.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
