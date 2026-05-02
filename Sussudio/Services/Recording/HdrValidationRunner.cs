using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Flashback;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Recording;

internal static class HdrValidationRunner
{
    private const int ValidationTimeoutMs = 30_000;

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

        var result = await new ProcessSupervisor().RunAsync(new ProcessSpec
        {
            FileName = "powershell",
            Arguments = arguments,
            TimeoutMs = ValidationTimeoutMs
        }, cancellationToken).ConfigureAwait(false);

        if (!result.Started)
        {
            return (false, "validator-process-start-failed");
        }

        if (result.TimedOut)
        {
            return (false, "validator-timeout");
        }

        var stdOut = result.StdOut;
        var stdErr = result.StdErr;

        if (!string.IsNullOrWhiteSpace(stdOut))
        {
            Logger.Log($"HDR validator stdout: {stdOut.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(stdErr))
        {
            Logger.Log($"HDR validator stderr: {stdErr.Trim()}");
        }

        if (result.ExitCode != 0)
        {
            var detail = !string.IsNullOrWhiteSpace(stdErr) ? stdErr.Trim() : stdOut.Trim();
            if (string.IsNullOrWhiteSpace(detail))
            {
                detail = $"validator-exit-code-{result.ExitCode ?? -1}";
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
