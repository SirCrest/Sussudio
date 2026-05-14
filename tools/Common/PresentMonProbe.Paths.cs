using System.Diagnostics;

namespace Sussudio.Tools;

public static partial class PresentMonProbe
{
    private static readonly string[] CandidateExeNames =
    [
        "PresentMon.exe",
        "PresentMon-2.4.1-x64.exe",
        "PresentMon-2.3.1-x64.exe",
        "PresentMon-2.3.0-x64.exe"
    ];

    private static Process? ResolveTargetProcess(PresentMonProbeOptions options)
    {
        if (options.ProcessId.HasValue)
        {
            try
            {
                var process = Process.GetProcessById(options.ProcessId.Value);
                return process.HasExited ? null : process;
            }
            catch
            {
                return null;
            }
        }

        var name = string.IsNullOrWhiteSpace(options.ProcessName)
            ? "Sussudio"
            : Path.GetFileNameWithoutExtension(options.ProcessName.Trim());
        return Process.GetProcessesByName(name)
            .Where(process => !process.HasExited)
            .OrderByDescending(process =>
            {
                try
                {
                    return process.StartTime;
                }
                catch
                {
                    return DateTime.MinValue;
                }
            })
            .FirstOrDefault();
    }

    private static string? ResolvePresentMonPath(string? explicitPath)
    {
        foreach (var candidate in EnumeratePresentMonCandidates(explicitPath))
        {
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumeratePresentMonCandidates(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            yield return explicitPath;
        }

        var envPath = Environment.GetEnvironmentVariable("SUSSUDIO_PRESENTMON_PATH");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            yield return envPath;
        }

        envPath = Environment.GetEnvironmentVariable("PRESENTMON_PATH");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            yield return envPath;
        }

        var baseDirectory = AppContext.BaseDirectory;
        foreach (var name in CandidateExeNames)
        {
            yield return Path.Combine(baseDirectory, "PresentMon", name);
            yield return Path.Combine(baseDirectory, "tools", "PresentMon", name);
            yield return Path.Combine(Directory.GetCurrentDirectory(), "tools", "PresentMon", name);
        }

        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var name in CandidateExeNames)
            {
                yield return Path.Combine(directory, name);
            }
        }
    }

    private static string ResolveOutputPath(string? outputFile)
        => string.IsNullOrWhiteSpace(outputFile)
            ? Path.Combine(Path.GetTempPath(), $"sussudio_presentmon_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.csv")
            : Path.GetFullPath(outputFile);
}
