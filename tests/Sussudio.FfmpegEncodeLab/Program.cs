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

}
