using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Recording;

public sealed partial class RecordingVerifier
{
    private readonly record struct HdrSideDataProbeResult(
        bool? MetadataPresent,
        IReadOnlyList<string> SideDataTypes);

    private async Task<HdrSideDataProbeResult> ProbeHdrSideDataAsync(
        string outputPath,
        CancellationToken cancellationToken)
    {
        var args =
            "-v error " +
            "-select_streams v:0 " +
            "-show_entries stream=side_data_list " +
            "-of json " +
            $"\"{outputPath}\"";

        var probe = await _processSupervisor.RunAsync(
            CreateFfprobeProcessSpec(outputPath, args, timeoutMs: 10000),
            cancellationToken).ConfigureAwait(false);

        if (!probe.Started || probe.TimedOut || probe.ExitCode != 0 || string.IsNullOrWhiteSpace(probe.StdOut))
        {
            return new HdrSideDataProbeResult(null, Array.Empty<string>());
        }

        try
        {
            using var document = JsonDocument.Parse(probe.StdOut);
            if (!document.RootElement.TryGetProperty("streams", out var streams) ||
                streams.ValueKind != JsonValueKind.Array ||
                streams.GetArrayLength() == 0)
            {
                return new HdrSideDataProbeResult(null, Array.Empty<string>());
            }

            var stream = streams[0];
            if (!stream.TryGetProperty("side_data_list", out var sideDataList) ||
                sideDataList.ValueKind != JsonValueKind.Array)
            {
                return new HdrSideDataProbeResult(false, Array.Empty<string>());
            }

            var sideDataTypes = new List<string>();
            foreach (var sideData in sideDataList.EnumerateArray())
            {
                if (!sideData.TryGetProperty("side_data_type", out var typeProperty))
                {
                    continue;
                }

                var type = typeProperty.GetString();
                if (string.IsNullOrWhiteSpace(type))
                {
                    continue;
                }

                sideDataTypes.Add(type);
                if (type.Contains("Mastering", StringComparison.OrdinalIgnoreCase) ||
                    type.Contains("Content light", StringComparison.OrdinalIgnoreCase))
                {
                    return new HdrSideDataProbeResult(true, sideDataTypes);
                }
            }

            return new HdrSideDataProbeResult(false, sideDataTypes);
        }
        catch (Exception ex)
        {
            Logger.Log($"ProbeHdrSideDataAsync ffprobe JSON parse failed: {ex.Message}");
            return new HdrSideDataProbeResult(null, Array.Empty<string>());
        }
    }

    private async Task<bool> CanRunFfprobeAsync(CancellationToken cancellationToken)
    {
        var result = await _processSupervisor.RunAsync(
            CreateFfprobeProcessSpec(outputPath: null, arguments: "-version", timeoutMs: 4000),
            cancellationToken).ConfigureAwait(false);

        return result.Started && !result.TimedOut && result.ExitCode == 0;
    }

    private ProcessSpec CreateFfprobeProcessSpec(string? outputPath, string arguments, int timeoutMs)
        => new()
        {
            FileName = _ffprobePath,
            Arguments = arguments,
            TimeoutMs = timeoutMs,
            WorkingDirectory = string.IsNullOrWhiteSpace(outputPath)
                ? null
                : Path.GetDirectoryName(outputPath),
            PriorityClass = ProcessPriorityClass.BelowNormal
        };

    private static string FindFfprobePath()
    {
        return FfmpegRuntimeLocator.FindToolPath("ffprobe.exe");
    }
}
