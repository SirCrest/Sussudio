using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ElgatoCapture.Models;

namespace ElgatoCapture.Services;

internal static class HdrValidationRunner
{
    public static async Task<(bool Success, string Detail)> RunAsync(
        RecordingContext? context,
        string? outputPath,
        CancellationToken cancellationToken = default)
    {
        if (context == null)
        {
            return (false, "recording-context-missing");
        }

        if (string.IsNullOrWhiteSpace(outputPath) || !File.Exists(outputPath))
        {
            return (false, $"output-file-missing(path={outputPath ?? "null"})");
        }

        var validatorPath = ResolveValidatorScriptPath();
        if (validatorPath == null)
        {
            return (false, "validator-script-missing(tools/validate_hdr.ps1)");
        }

        var codec = context.Settings.Format switch
        {
            RecordingFormat.HevcMp4 => "hevc",
            RecordingFormat.Av1Mp4 => "av1",
            _ => "either"
        };

        var arguments =
            "-NoProfile -ExecutionPolicy Bypass " +
            $"-File \"{validatorPath}\" " +
            $"-File \"{outputPath}\" " +
            $"-Codec {codec} ";

        if (context.HdrPipelineActive)
        {
            arguments += "-ExpectHdr ";
        }

        var masteringMetadataRequested =
            !string.IsNullOrWhiteSpace(context.Settings.HdrMasterDisplayMetadata) ||
            (context.Settings.HdrMaxCll > 0 && context.Settings.HdrMaxFall > 0);
        if (masteringMetadataRequested)
        {
            arguments += "-RequireHdr10StaticMetadata ";
        }

        if (context.EffectiveFrameRate > 0)
        {
            arguments += $"-ExpectedFps {context.EffectiveFrameRate.ToString("0.###", CultureInfo.InvariantCulture)} ";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            return (false, "validator-process-start-failed");
        }

        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        var stdOut = await stdOutTask.ConfigureAwait(false);
        var stdErr = await stdErrTask.ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(stdOut))
        {
            Logger.Log($"HDR validator stdout: {stdOut.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(stdErr))
        {
            Logger.Log($"HDR validator stderr: {stdErr.Trim()}");
        }

        if (process.ExitCode != 0)
        {
            var detail = !string.IsNullOrWhiteSpace(stdErr) ? stdErr.Trim() : stdOut.Trim();
            if (string.IsNullOrWhiteSpace(detail))
            {
                detail = $"validator-exit-code-{process.ExitCode}";
            }

            return (false, detail);
        }

        return (true, "validator-pass");
    }

    private static string? ResolveValidatorScriptPath()
    {
        const int maxDepth = 8;
        var relative = Path.Combine("tools", "validate_hdr.ps1");

        if (TryFindInParents(AppContext.BaseDirectory, relative, maxDepth, out var fromBase))
        {
            return fromBase;
        }

        if (TryFindInParents(Directory.GetCurrentDirectory(), relative, maxDepth, out var fromCwd))
        {
            return fromCwd;
        }

        return null;
    }

    private static bool TryFindInParents(string startPath, string relativePath, int maxDepth, out string? foundPath)
    {
        foundPath = null;
        if (string.IsNullOrWhiteSpace(startPath))
        {
            return false;
        }

        DirectoryInfo? current;
        try
        {
            current = new DirectoryInfo(Path.GetFullPath(startPath));
        }
        catch
        {
            return false;
        }

        if (current.Exists == false)
        {
            current = current.Parent;
        }

        for (var i = 0; i < maxDepth && current != null; i++)
        {
            var candidate = Path.Combine(current.FullName, relativePath);
            if (File.Exists(candidate))
            {
                foundPath = candidate;
                return true;
            }

            current = current.Parent;
        }

        return false;
    }
}
