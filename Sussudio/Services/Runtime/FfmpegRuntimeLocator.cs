using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Sussudio.Services.Runtime;

// Locates FFmpeg/ffprobe and caches capability probes. UI option lists and
// recording verification depend on these probes, so failures return explicit
// "unsupported" snapshots rather than throwing during normal startup.
internal static partial class FfmpegRuntimeLocator
{
    private static readonly string[] RequiredNativeLibraryPatterns =
    {
        "avcodec-*.dll",
        "avutil-*.dll"
    };

    internal static string GetAssemblyBaseDirectory()
    {
        var assemblyLocation = typeof(FfmpegRuntimeLocator).Assembly.Location;
        if (!string.IsNullOrWhiteSpace(assemblyLocation))
        {
            var assemblyDir = Path.GetDirectoryName(assemblyLocation);
            if (!string.IsNullOrWhiteSpace(assemblyDir))
            {
                return assemblyDir;
            }
        }

        return AppContext.BaseDirectory;
    }

    internal static bool TryResolveNativeRuntimeRoot(out string runtimeRoot)
        => TryResolveNativeRuntimeRoot(preferredBaseDirectory: null, out runtimeRoot);

    internal static bool TryResolveNativeRuntimeRoot(string? preferredBaseDirectory, out string runtimeRoot)
    {
        foreach (var candidate in EnumerateCandidateDirectories(preferredBaseDirectory))
        {
            if (ContainsRequiredNativeLibraries(candidate))
            {
                runtimeRoot = candidate;
                return true;
            }
        }

        runtimeRoot = string.Empty;
        return false;
    }

    internal static string FindToolPath(string toolFileName)
        => FindToolPath(toolFileName, preferredBaseDirectory: null);

    internal static string FindToolPath(string toolFileName, string? preferredBaseDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolFileName);

        foreach (var candidate in EnumerateCandidateDirectories(preferredBaseDirectory))
        {
            var path = Path.Combine(candidate, toolFileName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        if (TryResolvePathTool(toolFileName, out var pathToolMatch))
        {
            return pathToolMatch;
        }

        return toolFileName;
    }

    private static IEnumerable<string> EnumerateCandidateDirectories(string? preferredBaseDirectory)
    {
        var assemblyDir = !string.IsNullOrWhiteSpace(preferredBaseDirectory)
            ? preferredBaseDirectory
            : GetAssemblyBaseDirectory();
        if (!string.IsNullOrWhiteSpace(assemblyDir))
        {
            yield return Path.Combine(assemblyDir, "ffmpeg");
            yield return assemblyDir;
        }

        var programFilesDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "ffmpeg",
            "bin");
        yield return programFilesDir;

        if (TryResolvePathTool("ffmpeg.exe", out var pathToolMatch))
        {
            var pathDir = Path.GetDirectoryName(pathToolMatch);
            if (!string.IsNullOrWhiteSpace(pathDir))
            {
                yield return pathDir;
            }
        }
    }

    private static bool ContainsRequiredNativeLibraries(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return false;
        }

        foreach (var pattern in RequiredNativeLibraryPatterns)
        {
            if (!Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly).Any())
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryResolvePathTool(string toolFileName, out string resolvedPath)
    {
        resolvedPath = string.Empty;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "where",
                Arguments = toolFileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc == null)
            {
                return false;
            }

            if (!proc.WaitForExit(5000))
            {
                try
                {
                    proc.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best-effort: where.exe may have already exited.
                }

                return false;
            }

            var output = proc.StandardOutput.ReadToEnd();
            if (proc.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                return false;
            }

            resolvedPath = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            return !string.IsNullOrWhiteSpace(resolvedPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceWarning($"Suppressed exception in FfmpegRuntimeLocator path probe: {ex.Message}");
            return false;
        }
    }
}
