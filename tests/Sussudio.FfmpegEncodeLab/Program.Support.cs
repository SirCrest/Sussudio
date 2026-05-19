using System.Diagnostics;
using System.Globalization;
using System.Text;

// CLI parsing, tool-path resolution, and child-process execution for the HDR encode lab.
internal static partial class Program
{
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
