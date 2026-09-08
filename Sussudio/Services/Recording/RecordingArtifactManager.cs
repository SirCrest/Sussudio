using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Windows.Storage;

namespace Sussudio.Services.Recording;

// Reserves a recording attempt's output and resolves its execution context.
// Sinks own encoding and finalization; this owner rolls back failed starts.
public sealed class RecordingArtifactManager
{
    public async Task<RecordingContext> CreateContextAsync(
        StorageFolder outputFolder,
        RecordingContextRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(outputFolder);
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var settings = request.Settings;
        var outputFileName = request.FileNameFormatOverride is { } fileNameFormatOverride
            ? settings.GetOutputFileNameForFormat(fileNameFormatOverride)
            : settings.GetOutputFileName();
        var finalOutputPath = request.ReserveFinalOutputFile
            ? (await outputFolder.CreateFileAsync(
                    outputFileName,
                    CreationCollisionOption.GenerateUniqueName)).Path
            : ResolveUniqueOutputPath(outputFolder, outputFileName);

        var hdrPipelineActive = string.Equals(request.VideoInputPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase);

        return BuildContext(request, finalOutputPath, hdrPipelineActive);
    }

    private static string ResolveUniqueOutputPath(StorageFolder outputFolder, string outputFileName)
    {
        var baseName = Path.GetFileNameWithoutExtension(outputFileName);
        var extension = Path.GetExtension(outputFileName);
        var directory = outputFolder.Path;

        for (var i = 0; i < 10000; i++)
        {
            var candidateName = i == 0
                ? outputFileName
                : $"{baseName} ({i + 1}){extension}";
            var candidatePath = Path.Combine(directory, candidateName);
            if (!File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        return Path.Combine(directory, $"{baseName}_{Guid.NewGuid():N}{extension}");
    }

    private static RecordingContext BuildContext(
        RecordingContextRequest request,
        string finalOutputPath,
        bool hdrPipelineActive)
    {
        return new RecordingContext
        {
            Settings = request.Settings,
            AudioDeviceName = request.AudioDeviceName,
            MicrophoneDeviceName = request.MicrophoneDeviceName,
            EffectiveFrameRate = request.EffectiveFrameRate,
            FrameRateArg = request.FrameRateArg,
            EffectiveWidth = request.EffectiveWidth,
            EffectiveHeight = request.EffectiveHeight,
            VideoInputPixelFormat = request.VideoInputPixelFormat,
            IsFullRangeInput = request.IsFullRangeInput,
            GpuHandles = request.GpuHandles,
            FileNameFormatOverride = request.FileNameFormatOverride,
            VideoOutputPath = finalOutputPath,
            FinalOutputPath = finalOutputPath,
            HdrPipelineActive = hdrPipelineActive,
        };
    }

    public Task RollbackAsync(RecordingContext? context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context == null)
        {
            return Task.CompletedTask;
        }

        TryDelete(context.VideoOutputPath);

        return Task.CompletedTask;
    }

    private static void TryDelete(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to delete file '{path}': {ex.Message}");
        }
    }
}

internal sealed record RecordingFailureRecoveryState(
    string MarkerPath,
    string OutputPath,
    string Reason,
    DateTimeOffset RecordedUtc,
    IReadOnlyList<string> PreservedArtifacts);

internal static class RecordingFinalizationRecoveryArtifacts
{
    internal const string UnresolvedMarkerSuffix = ".recording-finalization-unresolved.txt";
    private const string ActiveMarkerSuffix = ".recording-active.txt";
    private const string RecoveryDirectoryName = "RecordingRecovery";

    internal static bool IsUnresolvedMarkerPath(string? path)
        => !string.IsNullOrWhiteSpace(path) &&
           path.EndsWith(UnresolvedMarkerSuffix, StringComparison.OrdinalIgnoreCase);

    // The unresolved marker is the preferred recovery handle; otherwise fall back to
    // the caller's preferred path, then the first preserved artifact.
    internal static string? ResolveRecoveryPath(IReadOnlyList<string> artifacts, string? preferredRecoveryPath = null)
    {
        foreach (var artifact in artifacts)
        {
            if (IsUnresolvedMarkerPath(artifact)) return artifact;
        }
        return preferredRecoveryPath ?? (artifacts.Count > 0 ? artifacts[0] : null);
    }

    internal static RecordingFailureRecoveryState? TryLoadLatest(string? outputDirectory)
    {
        try
        {
            return TryLoadLatestFromDirectories(
                outputDirectory,
                GetStableRecoveryDirectory(),
                static directory => Directory.EnumerateFiles(directory, "*.recording-*.txt", SearchOption.TopDirectoryOnly),
                File.GetLastWriteTimeUtc,
                static message => Logger.Log(message));
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to restore recording recovery marker from '{outputDirectory}': {ex.Message}");
            return null;
        }
    }

    internal static RecordingFailureRecoveryState? TryLoadLatestFromDirectories(
        string? outputDirectory,
        string? stableRecoveryDirectory,
        Func<string, IEnumerable<string>> enumerateMarkerPaths,
        Func<string, DateTime> getLastWriteTimeUtc,
        Action<string> log)
    {
        var markers = new List<(string Path, DateTime WriteUtc)>();
        AddRecoveryMarkers(markers, outputDirectory, enumerateMarkerPaths, getLastWriteTimeUtc, log);
        AddRecoveryMarkers(markers, stableRecoveryDirectory, enumerateMarkerPaths, getLastWriteTimeUtc, log);

        markers.Sort(static (left, right) => right.WriteUtc.CompareTo(left.WriteUtc));
        RecordingFailureRecoveryState? newestMetadataOnlyRecovery = null;
        foreach (var marker in markers)
        {
            try
            {
                var recovered = TryLoadMarker(marker.Path, marker.WriteUtc);
                if (recovered == null)
                {
                    continue;
                }

                var hasRecoverableMedia = recovered.PreservedArtifacts.Any(path =>
                    !string.Equals(path, recovered.MarkerPath, StringComparison.OrdinalIgnoreCase) &&
                    File.Exists(path));
                if (hasRecoverableMedia)
                {
                    return recovered;
                }

                newestMetadataOnlyRecovery ??= recovered;
            }
            catch (Exception ex)
            {
                log($"Failed to restore recording recovery marker '{marker.Path}': {ex.Message}");
            }
        }

        return newestMetadataOnlyRecovery;
    }

    public static IReadOnlyList<string> PreserveUnresolved(
        RecordingContext? context,
        string outputPath,
        string reason)
        => PreserveUnresolvedWithArtifacts(context, outputPath, reason, Array.Empty<string>());

    internal static string BeginActive(
        string outputPath,
        string? videoOutputPath,
        IEnumerable<string> artifactDirectories)
    {
        var fileName = Path.GetFileName(string.IsNullOrWhiteSpace(outputPath) ? videoOutputPath : outputPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "recording-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        }

        var markerLines = new List<string>
        {
            "status=active",
            "utc=" + DateTimeOffset.UtcNow.ToString("O"),
            "reason=Recording was interrupted before finalization.",
            "final_output=" + outputPath,
            "video_output=" + (videoOutputPath ?? string.Empty),
            "audio_temp=",
        };
        foreach (var directory in artifactDirectories)
        {
            if (!string.IsNullOrWhiteSpace(directory))
            {
                markerLines.Add("artifact_root_b64=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(directory)));
            }
        }

        var markerPath = TryWriteMarker(
            GetStableRecoveryDirectory(),
            fileName,
            markerLines,
            ActiveMarkerSuffix,
            createDirectory: true);
        return markerPath ?? throw new InvalidOperationException(
            "Recording could not start because its crash-recovery journal could not be created.");
    }

    internal static void RetireActive(string? markerPath)
    {
        if (string.IsNullOrWhiteSpace(markerPath))
        {
            return;
        }

        try
        {
            var candidate = Path.GetFullPath(markerPath);
            // The path is produced by BeginActive and retained privately by
            // CaptureService. Requiring the dedicated suffix keeps retirement
            // narrowly scoped without re-reading a mutable environment override.
            if (!candidate.EndsWith(ActiveMarkerSuffix, StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log($"Skipped unsafe active recording journal retirement path '{markerPath}'.");
                return;
            }

            if (File.Exists(candidate))
            {
                File.Delete(candidate);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to retire active recording recovery journal '{markerPath}': {ex.Message}");
        }
    }

    internal static IReadOnlyList<string> PreserveUnresolvedWithArtifacts(
        RecordingContext? context,
        string outputPath,
        string reason,
        IEnumerable<string> additionalArtifacts)
    {
        var preserved = new List<string>();
        AddExistingFile(preserved, outputPath);
        AddExistingFile(preserved, context?.VideoOutputPath);
        AddExistingFile(preserved, context?.FinalOutputPath);
        foreach (var artifactPath in additionalArtifacts)
        {
            AddExistingFile(preserved, artifactPath);
        }

        var markerPaths = TryWriteUnresolvedMarkers(context, outputPath, reason, preserved);
        foreach (var markerPath in markerPaths)
        {
            AddExistingFile(preserved, markerPath);
        }
        return preserved;
    }

    private static IReadOnlyList<string> TryWriteUnresolvedMarkers(
        RecordingContext? context,
        string outputPath,
        string reason,
        IReadOnlyList<string> preservedArtifacts)
    {
        var anchorPath = ResolveMarkerAnchor(context, outputPath);
        var fileName = string.IsNullOrWhiteSpace(anchorPath)
            ? "recording-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss")
            : Path.GetFileName(anchorPath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "recording";
        }

        var markerLines = BuildMarkerLines(context, outputPath, reason, preservedArtifacts);
        var primaryDirectory = string.IsNullOrWhiteSpace(anchorPath)
            ? null
            : Path.GetDirectoryName(anchorPath);
        var markers = new List<string>(2);
        var primaryMarker = TryWriteMarker(primaryDirectory, fileName, markerLines, UnresolvedMarkerSuffix);
        if (!string.IsNullOrWhiteSpace(primaryMarker))
        {
            markers.Add(primaryMarker);
        }

        var stableMarker = TryWriteMarker(
            GetStableRecoveryDirectory(),
            fileName,
            markerLines,
            UnresolvedMarkerSuffix,
            createDirectory: true);
        if (!string.IsNullOrWhiteSpace(stableMarker) &&
            !markers.Any(path => string.Equals(path, stableMarker, StringComparison.OrdinalIgnoreCase)))
        {
            markers.Add(stableMarker);
        }

        return markers;
    }

    private static IReadOnlyList<string> BuildMarkerLines(
        RecordingContext? context,
        string outputPath,
        string reason,
        IReadOnlyList<string> preservedArtifacts)
    {
        var markerLines = new List<string>
        {
            "status=unresolved",
            "utc=" + DateTimeOffset.UtcNow.ToString("O"),
            "reason=" + reason,
            "reason_b64=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(reason)),
            "final_output=" + (context?.FinalOutputPath ?? outputPath),
            "video_output=" + (context?.VideoOutputPath ?? string.Empty),
            "audio_temp=",
        };
        foreach (var artifactPath in preservedArtifacts)
        {
            markerLines.Add("artifact_b64=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(artifactPath)));
        }

        return markerLines;
    }

    private static string? TryWriteMarker(
        string? directory,
        string fileName,
        IReadOnlyList<string> markerLines,
        string markerSuffix,
        bool createDirectory = false)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return null;
        }

        try
        {
            if (createDirectory)
            {
                Directory.CreateDirectory(directory);
            }
            else if (!Directory.Exists(directory))
            {
                return null;
            }

            var markerPath = Path.Combine(directory, fileName + markerSuffix);
            var temporaryMarkerPath = markerPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllLines(temporaryMarkerPath, markerLines);
                File.Move(temporaryMarkerPath, markerPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryMarkerPath))
                {
                    File.Delete(temporaryMarkerPath);
                }
            }

            return markerPath;
        }
        catch (Exception ex)
        {
            Logger.Log($"Failed to write recording finalization recovery marker in '{directory}': {ex.Message}");
            return null;
        }
    }

    private static void AddRecoveryMarkers(
        List<(string Path, DateTime WriteUtc)> markers,
        string? directory,
        Func<string, IEnumerable<string>> enumerateMarkerPaths,
        Func<string, DateTime> getLastWriteTimeUtc,
        Action<string> log)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        try
        {
            foreach (var markerPath in enumerateMarkerPaths(directory))
            {
                if (!markerPath.EndsWith(UnresolvedMarkerSuffix, StringComparison.OrdinalIgnoreCase) &&
                    !markerPath.EndsWith(ActiveMarkerSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (markers.Any(marker => string.Equals(marker.Path, markerPath, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                try
                {
                    markers.Add((markerPath, getLastWriteTimeUtc(markerPath)));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    log($"Failed to read recording recovery marker timestamp '{markerPath}': {ex.Message}");
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Enumeration is lazy: retain candidates yielded before a root fails.
            log($"Failed to enumerate recording recovery markers in '{directory}': {ex.Message}");
        }
    }

    private static string GetStableRecoveryDirectory()
    {
        var overrideDirectory = Environment.GetEnvironmentVariable("SUSSUDIO_RECOVERY_DIRECTORY");
        return !string.IsNullOrWhiteSpace(overrideDirectory)
            ? overrideDirectory
            : Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Sussudio",
                RecoveryDirectoryName);
    }

    private static RecordingFailureRecoveryState? TryLoadMarker(string markerPath, DateTime writeUtc)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var encodedArtifacts = new List<string>();
        var encodedArtifactRoots = new List<string>();
        foreach (var line in File.ReadAllLines(markerPath))
        {
            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator];
            var value = line[(separator + 1)..];
            if (string.Equals(key, "artifact_b64", StringComparison.OrdinalIgnoreCase))
            {
                encodedArtifacts.Add(value);
            }
            else if (string.Equals(key, "artifact_root_b64", StringComparison.OrdinalIgnoreCase))
            {
                encodedArtifactRoots.Add(value);
            }
            else
            {
                values[key] = value;
            }
        }

        if (!values.TryGetValue("status", out var status) ||
            (!string.Equals(status, "unresolved", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(status, "active", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        values.TryGetValue("reason", out var reason);
        if (values.TryGetValue("reason_b64", out var encodedReason))
        {
            reason = Encoding.UTF8.GetString(Convert.FromBase64String(encodedReason));
        }

        values.TryGetValue("final_output", out var finalOutput);
        values.TryGetValue("video_output", out var videoOutput);
        // Older recordings wrote audio separately; retain those recovery files.
        values.TryGetValue("audio_temp", out var audioOutput);

        var preserved = new List<string>();
        foreach (var encodedArtifact in encodedArtifacts)
        {
            AddExistingFile(
                preserved,
                Encoding.UTF8.GetString(Convert.FromBase64String(encodedArtifact)));
        }

        foreach (var encodedArtifactRoot in encodedArtifactRoots)
        {
            AddRecoverableFilesFromDirectory(
                preserved,
                Encoding.UTF8.GetString(Convert.FromBase64String(encodedArtifactRoot)));
        }

        AddExistingFile(preserved, finalOutput);
        AddExistingFile(preserved, videoOutput);
        AddExistingFile(preserved, audioOutput);
        AddExistingFile(preserved, markerPath);

        var recordedUtc = new DateTimeOffset(writeUtc, TimeSpan.Zero);
        if (values.TryGetValue("utc", out var utcText) &&
            DateTimeOffset.TryParse(utcText, out var parsedUtc))
        {
            recordedUtc = parsedUtc;
        }

        return new RecordingFailureRecoveryState(
            markerPath,
            string.IsNullOrWhiteSpace(finalOutput) ? markerPath : finalOutput,
            string.IsNullOrWhiteSpace(reason)
                ? string.Equals(status, "active", StringComparison.OrdinalIgnoreCase)
                    ? "Recording was interrupted before finalization."
                    : "Recording finalization did not complete."
                : reason,
            recordedUtc,
            preserved);
    }

    private static string? ResolveMarkerAnchor(RecordingContext? context, string outputPath)
    {
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            return outputPath;
        }

        if (!string.IsNullOrWhiteSpace(context?.FinalOutputPath))
        {
            return context.FinalOutputPath;
        }

        return !string.IsNullOrWhiteSpace(context?.VideoOutputPath)
            ? context.VideoOutputPath
            : null;
    }

    private static void AddExistingFile(List<string> preserved, string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        foreach (var existing in preserved)
        {
            if (string.Equals(existing, path, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        preserved.Add(path);
    }

    private static void AddRecoverableFilesFromDirectory(List<string> preserved, string? directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        foreach (var filePath in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
        {
            var extension = Path.GetExtension(filePath);
            if (extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".mkv", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".mov", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".ts", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".m4a", StringComparison.OrdinalIgnoreCase) ||
                extension.Equals(".tmp", StringComparison.OrdinalIgnoreCase))
            {
                AddExistingFile(preserved, filePath);
            }
        }
    }
}
