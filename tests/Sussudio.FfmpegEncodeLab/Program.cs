using System.Globalization;
using System.Text;

// Standalone lab harness for exercising FFmpeg/libav encoding paths outside
// the WinUI app and main regression runner.
internal static partial class Program
{
    private static async Task<int> Main(string[] args)
    {
        Options options;
        try
        {
            options = ParseOptions(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            PrintUsage();
            return 2;
        }

        if (!File.Exists(options.InputFile))
        {
            Console.Error.WriteLine($"Input file not found: {options.InputFile}");
            return 2;
        }

        var repoRoot = ResolveRepoRoot();
        var ffmpegPath = ResolveFfmpegPath(repoRoot);
        var powershellHost = ResolvePowerShellHost();
        var validatorScriptPath = Path.Combine(repoRoot, "tools", "validate_hdr.ps1");
        if (!File.Exists(validatorScriptPath))
        {
            Console.Error.WriteLine($"Validator script not found: {validatorScriptPath}");
            return 2;
        }

        var artifactRoot = Path.Combine(
            repoRoot,
            "artifacts",
            "hdr-lab",
            DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(artifactRoot);

        var normalizedInputPath = Path.GetFullPath(options.InputFile);
        Console.WriteLine($"HDR_LAB_INPUT {normalizedInputPath}");
        Console.WriteLine($"HDR_LAB_ARTIFACT_ROOT {artifactRoot}");
        Console.WriteLine($"HDR_LAB_FFMPEG {ffmpegPath}");

        var hevcOutputPath = Path.Combine(artifactRoot, "hevc-main10-hdr.mp4");
        var hevcEncodeLogPath = Path.Combine(artifactRoot, "ffmpeg-hevc.log");
        var hevcEncodeResult = await RunProcessAsync(
            ffmpegPath,
            BuildHevcArguments(options, normalizedInputPath, hevcOutputPath),
            hevcEncodeLogPath).ConfigureAwait(false);
        if (hevcEncodeResult.ExitCode != 0)
        {
            Console.Error.WriteLine($"HEVC encode failed. See log: {hevcEncodeLogPath}");
            return 1;
        }

        var hevcValidationLogPath = Path.Combine(artifactRoot, "validate-hevc.log");
        var hevcValidationResult = await RunProcessAsync(
            powershellHost,
            BuildValidatorArguments(validatorScriptPath, hevcOutputPath, "hevc"),
            hevcValidationLogPath).ConfigureAwait(false);
        if (hevcValidationResult.ExitCode != 0)
        {
            Console.Error.WriteLine($"HEVC validation failed. See log: {hevcValidationLogPath}");
            return 1;
        }

        var av1EncoderProbeLogPath = Path.Combine(artifactRoot, "ffmpeg-encoders.log");
        var av1Encoder = await ResolveAv1EncoderAsync(ffmpegPath, av1EncoderProbeLogPath).ConfigureAwait(false);

        var av1IntermediatePath = Path.Combine(artifactRoot, "av1-main10-prehdr.mp4");
        var av1OutputPath = Path.Combine(artifactRoot, "av1-main10-hdr.mp4");
        var av1EncodeLogPath = Path.Combine(artifactRoot, "ffmpeg-av1.log");
        var av1EncodeResult = await RunProcessAsync(
            ffmpegPath,
            BuildAv1Arguments(options, normalizedInputPath, av1IntermediatePath, av1Encoder),
            av1EncodeLogPath).ConfigureAwait(false);
        if (av1EncodeResult.ExitCode != 0)
        {
            Console.Error.WriteLine($"AV1 encode failed ({av1Encoder}). See log: {av1EncodeLogPath}");
            return 1;
        }

        var av1FixupLogPath = Path.Combine(artifactRoot, "ffmpeg-av1-fixup.log");
        var av1FixupResult = await RunProcessAsync(
            ffmpegPath,
            BuildAv1MetadataFixupArguments(av1IntermediatePath, av1OutputPath),
            av1FixupLogPath).ConfigureAwait(false);
        if (av1FixupResult.ExitCode != 0)
        {
            Console.Error.WriteLine($"AV1 metadata fixup failed. See log: {av1FixupLogPath}");
            return 1;
        }

        var av1ValidationLogPath = Path.Combine(artifactRoot, "validate-av1.log");
        var av1ValidationResult = await RunProcessAsync(
            powershellHost,
            BuildValidatorArguments(validatorScriptPath, av1OutputPath, "av1"),
            av1ValidationLogPath).ConfigureAwait(false);
        if (av1ValidationResult.ExitCode != 0)
        {
            Console.Error.WriteLine($"AV1 validation failed. See log: {av1ValidationLogPath}");
            return 1;
        }

        var summaryPath = Path.Combine(artifactRoot, "run-summary.txt");
        var summary = new StringBuilder();
        summary.AppendLine("HDR encode lab completed successfully.");
        summary.AppendLine($"Input: {normalizedInputPath}");
        summary.AppendLine($"HEVC: {hevcOutputPath}");
        summary.AppendLine($"AV1: {av1OutputPath}");
        summary.AppendLine($"AV1 Encoder: {av1Encoder}");
        summary.AppendLine($"ArtifactRoot: {artifactRoot}");
        await File.WriteAllTextAsync(summaryPath, summary.ToString()).ConfigureAwait(false);

        Console.WriteLine($"HDR_LAB_HEVC_OUTPUT {hevcOutputPath}");
        Console.WriteLine($"HDR_LAB_AV1_OUTPUT {av1OutputPath}");
        Console.WriteLine($"HDR_LAB_SUMMARY {summaryPath}");
        Console.WriteLine("HDR_LAB_RESULT PASS");
        return 0;
    }

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
