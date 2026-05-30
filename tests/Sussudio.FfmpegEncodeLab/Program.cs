using System.Diagnostics;
using System.Globalization;
using System.Text;

// Standalone lab harness for exercising FFmpeg/libav encoding paths outside
// the WinUI app and main regression runner.
internal static class Program
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

    private sealed record Options(
        string InputFile,
        int Width,
        int Height,
        double Fps,
        int FrameCount);

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

    private static Options ParseOptions(string[] args)
    {
        string? input = null;
        var width = 1920;
        var height = 1080;
        var fps = 60.0;
        var frameCount = 120;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--input":
                    input = ReadValue(args, ref index, "--input");
                    break;
                case "--width":
                    width = ParsePositiveInt(ReadValue(args, ref index, "--width"), "--width");
                    break;
                case "--height":
                    height = ParsePositiveInt(ReadValue(args, ref index, "--height"), "--height");
                    break;
                case "--fps":
                    fps = ParsePositiveDouble(ReadValue(args, ref index, "--fps"), "--fps");
                    break;
                case "--frames":
                    frameCount = ParsePositiveInt(ReadValue(args, ref index, "--frames"), "--frames");
                    break;
                default:
                    if (arg.StartsWith("--", StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Unknown argument: {arg}");
                    }

                    if (input is null)
                    {
                        input = arg;
                        break;
                    }

                    throw new ArgumentException($"Unexpected argument: {arg}");
            }
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Missing required input path (`--input <path>`).");
        }

        return new Options(Path.GetFullPath(input), width, height, fps, frameCount);
    }

    private static string ReadValue(string[] args, ref int index, string name)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {name}.");
        }

        index++;
        return args[index];
    }

    private static int ParsePositiveInt(string value, string optionName)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
        {
            throw new ArgumentException($"Invalid value for {optionName}: '{value}'");
        }

        return parsed;
    }

    private static double ParsePositiveDouble(string value, string optionName)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
        {
            throw new ArgumentException($"Invalid value for {optionName}: '{value}'");
        }

        return parsed;
    }

    private static string ResolveRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Sussudio.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root containing Sussudio.slnx.");
    }

    private static string ResolveFfmpegPath(string repoRoot)
    {
        var pathTool = TryFindOnPath("ffmpeg.exe");
        if (pathTool is not null)
        {
            return pathTool;
        }

        var candidates = new[]
        {
            Path.Combine(repoRoot, "latest-build", "ffmpeg", "ffmpeg.exe"),
            Path.Combine(repoRoot, "Sussudio", "bin", "x64", "Debug", "net8.0-windows10.0.19041.0", "win-x64", "ffmpeg", "ffmpeg.exe")
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("ffmpeg.exe not found on PATH or expected repo locations.");
    }

    private static string ResolvePowerShellHost()
    {
        var pwsh = TryFindOnPath("pwsh.exe");
        if (pwsh is not null)
        {
            return pwsh;
        }

        var windowsPowerShell = TryFindOnPath("powershell.exe");
        if (windowsPowerShell is not null)
        {
            return windowsPowerShell;
        }

        throw new FileNotFoundException("Neither pwsh.exe nor powershell.exe was found on PATH.");
    }

    private static string? TryFindOnPath(string executableName)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return null;
        }

        foreach (var pathPart in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(pathPart.Trim(), executableName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName,
        IEnumerable<string> arguments,
        string logPath)
    {
        var argumentList = arguments.ToList();
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in argumentList)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        var logBuilder = new StringBuilder();
        logBuilder.AppendLine($"Command: {fileName} {string.Join(" ", argumentList.Select(QuoteIfNeeded))}");
        logBuilder.AppendLine($"ExitCode: {process.ExitCode}");
        logBuilder.AppendLine();
        logBuilder.AppendLine("--- STDOUT ---");
        logBuilder.AppendLine(stdout);
        logBuilder.AppendLine();
        logBuilder.AppendLine("--- STDERR ---");
        logBuilder.AppendLine(stderr);
        await File.WriteAllTextAsync(logPath, logBuilder.ToString()).ConfigureAwait(false);

        return new ProcessResult(process.ExitCode, stdout, stderr);
    }

    private static string QuoteIfNeeded(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        return value.Contains(' ') ? $"\"{value}\"" : value;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project tests/Sussudio.FfmpegEncodeLab -- --input <p010 raw file> [--width <int>] [--height <int>] [--fps <double>] [--frames <int>]");
        Console.WriteLine();
        Console.WriteLine("Defaults:");
        Console.WriteLine("  --width 1920 --height 1080 --fps 60 --frames 120");
    }
}
