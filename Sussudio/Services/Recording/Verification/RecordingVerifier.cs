using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Recording;

// Strict post-recording verifier. It compares ffprobe evidence against the
// negotiated runtime snapshot, so Auto/native modes verify against what the
// capture device actually delivered rather than only what the user requested.
public sealed partial class RecordingVerifier : IRecordingVerifier
{
    private static readonly Lazy<string> CachedFfprobePath = new(FindFfprobePath);
    private readonly IProcessSupervisor _processSupervisor;
    private readonly string _ffprobePath;

    public RecordingVerifier() : this(new ProcessSupervisor(), CachedFfprobePath.Value)
    {
    }

    internal RecordingVerifier(IProcessSupervisor processSupervisor, string ffprobePath)
    {
        _processSupervisor = processSupervisor ?? throw new ArgumentNullException(nameof(processSupervisor));
        _ffprobePath = string.IsNullOrWhiteSpace(ffprobePath) ? "ffprobe.exe" : ffprobePath;
    }

    public async Task<RecordingVerificationResult> VerifyAsync(
        string? outputPath,
        CaptureRuntimeSnapshot runtimeSnapshot,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            return CreateEarlyFailure(outputPath, "No output file path is available for verification.", "missing-output-path");
        }

        if (!File.Exists(outputPath))
        {
            return CreateEarlyFailure(outputPath, $"Output file does not exist: {outputPath}", "output-not-found");
        }

        var fileSize = new FileInfo(outputPath).Length;
        if (fileSize <= 0)
        {
            return CreateEarlyFailure(outputPath, $"Output file is empty: {outputPath}", "output-empty", fileExists: true, fileSizeBytes: fileSize);
        }

        if (!await CanRunFfprobeAsync(cancellationToken).ConfigureAwait(false))
        {
            return CreateEarlyFailure(outputPath, "Strict verification failed: ffprobe is not accessible.", "ffprobe-unavailable", fileExists: true, fileSizeBytes: fileSize);
        }

        var ffprobeArgs =
            $"-v error " +
            "-select_streams v:0 " +
            "-show_entries stream=codec_name,width,height,avg_frame_rate,r_frame_rate,pix_fmt,color_primaries,color_transfer,color_space " +
            "-show_entries format=format_name " +
            "-of default=noprint_wrappers=1:nokey=0 " +
            $"\"{outputPath}\"";

        var probe = await _processSupervisor.RunAsync(
            CreateFfprobeProcessSpec(outputPath, ffprobeArgs, timeoutMs: 10000),
            cancellationToken).ConfigureAwait(false);

        if (!probe.Started || probe.TimedOut || probe.ExitCode != 0)
        {
            var startError = probe.StartException?.Message ?? "unknown";
            var reason = !probe.Started
                ? $"ffprobe could not start ({startError})"
                : probe.TimedOut
                    ? "ffprobe timed out"
                    : $"ffprobe exit code {probe.ExitCode}";
            return new RecordingVerificationResult
            {
                Succeeded = false,
                Message = $"Strict verification failed: {reason}",
                OutputPath = outputPath,
                FileExists = true,
                FileSizeBytes = fileSize,
                VerificationMode = "ffprobe",
                PrimaryMismatchCode = "ffprobe-failed",
                Mismatches = new[] { "ffprobe-failed" }
            };
        }

        var keyValues = ParseKeyValueOutput(probe.StdOut + Environment.NewLine + probe.StdErr);
        keyValues.TryGetValue("format_name", out var formatName);
        keyValues.TryGetValue("codec_name", out var codecName);
        keyValues.TryGetValue("width", out var widthRaw);
        keyValues.TryGetValue("height", out var heightRaw);
        keyValues.TryGetValue("avg_frame_rate", out var avgFpsRaw);
        keyValues.TryGetValue("r_frame_rate", out var rFpsRaw);
        keyValues.TryGetValue("pix_fmt", out var pixelFormatRaw);
        keyValues.TryGetValue("color_primaries", out var colorPrimariesRaw);
        keyValues.TryGetValue("color_transfer", out var colorTransferRaw);
        keyValues.TryGetValue("color_space", out var colorSpaceRaw);

        var detectedWidth = TryParseUInt(widthRaw);
        var detectedHeight = TryParseUInt(heightRaw);
        var detectedFrameRate = TryParseRational(avgFpsRaw) ?? TryParseRational(rFpsRaw);
        var expectedFrameRate = ResolveExpectedFrameRate(runtimeSnapshot);
        var hdrSideDataTask = (runtimeSnapshot.RequestedHdrEnabled ?? false)
            ? ProbeHdrSideDataAsync(outputPath, cancellationToken)
            : Task.FromResult(new HdrSideDataProbeResult(null, Array.Empty<string>()));
        var cadenceTask = AnalyzeCadenceMetricsAsync(
            outputPath,
            expectedFrameRate ?? detectedFrameRate,
            cancellationToken);
        await Task.WhenAll(hdrSideDataTask, cadenceTask).ConfigureAwait(false);
        var hdrSideDataProbe = await hdrSideDataTask.ConfigureAwait(false);
        var cadenceMetrics = await cadenceTask.ConfigureAwait(false);
        if ((!detectedFrameRate.HasValue || detectedFrameRate.Value <= 0) &&
            cadenceMetrics.HasValue &&
            cadenceMetrics.Value.ObservedFps > 0)
        {
            detectedFrameRate = cadenceMetrics.Value.ObservedFps;
        }

        var mismatches = new List<string>();
        ValidateContainer(runtimeSnapshot, formatName, outputPath, mismatches);
        ValidateCodec(runtimeSnapshot, codecName, outputPath, mismatches);
        ValidateDimensions(runtimeSnapshot, detectedWidth, detectedHeight, mismatches);
        ValidateFrameRate(runtimeSnapshot, detectedFrameRate, expectedFrameRate, mismatches);
        var hdrValidation = ValidateHdrMetadata(
            runtimeSnapshot,
            codecName,
            pixelFormatRaw,
            colorPrimariesRaw,
            colorTransferRaw,
            colorSpaceRaw,
            hdrSideDataProbe.MetadataPresent,
            mismatches);
        ValidateCadence(cadenceMetrics, mismatches);

        Logger.Log(
            "HDR validator ffprobe fields: " +
            $"codec_name={codecName ?? "unknown"}, pix_fmt={pixelFormatRaw ?? "unknown"}, " +
            $"color_primaries={colorPrimariesRaw ?? "unknown"}, color_transfer={colorTransferRaw ?? "unknown"}, " +
            $"color_space={colorSpaceRaw ?? "unknown"}, side_data_types={string.Join("|", hdrSideDataProbe.SideDataTypes)}");

        var success = mismatches.Count == 0;
        var primaryMismatch = ParsePrimaryMismatch(mismatches);
        var hdrParity = BuildHdrParityResult(runtimeSnapshot, hdrValidation, mismatches);
        return new RecordingVerificationResult
        {
            Succeeded = success,
            Message = success
                ? "Strict verification passed."
                : $"Strict verification found {mismatches.Count} mismatch(es).",
            OutputPath = outputPath,
            FileExists = true,
            FileSizeBytes = fileSize,
            VerificationMode = "ffprobe",
            DetectedContainer = formatName,
            DetectedVideoCodec = codecName,
            DetectedPixelFormat = pixelFormatRaw,
            DetectedColorPrimaries = colorPrimariesRaw,
            DetectedColorTransfer = colorTransferRaw,
            DetectedColorSpace = colorSpaceRaw,
            DetectedHdrSideDataTypes = hdrSideDataProbe.SideDataTypes,
            HdrMetadataPresent = hdrValidation.HdrMetadataPresent,
            HdrColorimetryValid = hdrValidation.ColorimetryValid,
            HdrMasteringMetadataPresent = hdrValidation.MasteringMetadataPresent,
            HdrVerificationLevel = hdrValidation.VerificationLevel,
            DetectedWidth = detectedWidth,
            DetectedHeight = detectedHeight,
            DetectedFrameRate = detectedFrameRate,
            CadenceSampleCount = cadenceMetrics?.SampleCount,
            CadenceObservedFps = cadenceMetrics?.ObservedFps,
            CadenceExpectedIntervalMs = cadenceMetrics?.ExpectedIntervalMs,
            CadenceAverageIntervalMs = cadenceMetrics?.AverageIntervalMs,
            CadenceP95IntervalMs = cadenceMetrics?.P95IntervalMs,
            CadenceMaxIntervalMs = cadenceMetrics?.MaxIntervalMs,
            CadenceJitterStdDevMs = cadenceMetrics?.JitterStdDevMs,
            CadenceSevereGapCount = cadenceMetrics?.SevereGapCount,
            CadenceSevereGapPercent = cadenceMetrics?.SevereGapPercent,
            CadenceEstimatedDroppedFrames = cadenceMetrics?.EstimatedDroppedFrames,
            CadenceEstimatedDropPercent = cadenceMetrics?.EstimatedDropPercent,
            PrimaryMismatchCode = primaryMismatch.Code,
            PrimaryMismatchExpected = primaryMismatch.Expected,
            PrimaryMismatchActual = primaryMismatch.Actual,
            Mismatches = mismatches,
            HdrParity = hdrParity
        };
    }

    private static (string? Code, string? Expected, string? Actual) ParsePrimaryMismatch(IReadOnlyList<string> mismatches)
    {
        if (mismatches == null || mismatches.Count == 0)
        {
            return (null, null, null);
        }

        var raw = mismatches[0];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (null, null, null);
        }

        var openParen = raw.IndexOf('(');
        var closeParen = raw.LastIndexOf(')');
        if (openParen <= 0 || closeParen <= openParen)
        {
            return (raw.Trim(), null, null);
        }

        var code = raw[..openParen].Trim();
        var detail = raw[(openParen + 1)..closeParen];
        var expected = TryGetMismatchPart(detail, "expected=");
        var actual = TryGetMismatchPart(detail, "actual=");
        return (code, expected, actual);
    }

    private static HdrParityResult BuildHdrParityResult(
        CaptureRuntimeSnapshot runtimeSnapshot,
        HdrValidationResult hdrValidation,
        IReadOnlyList<string> mismatches)
    {
        var hdrRequested = runtimeSnapshot.HdrOutputActive || (runtimeSnapshot.RequestedHdrEnabled ?? false);
        var taxonomy = BuildMismatchTaxonomy(mismatches);
        var hasHdrFailure = taxonomy.Any(entry =>
            entry.Category.Equals("HDR", StringComparison.OrdinalIgnoreCase) ||
            entry.Category.Equals("Colorimetry", StringComparison.OrdinalIgnoreCase));
        var verified = hdrRequested
            ? hdrValidation.HdrMetadataPresent == true && !hasHdrFailure
            : true;
        var status = !hdrRequested
            ? "NotRequested"
            : verified
                ? "Verified"
                : runtimeSnapshot.HdrAutoDowngraded
                    ? "Downgraded"
                    : "Mismatch";
        return new HdrParityResult
        {
            Requested = hdrRequested,
            Activated = runtimeSnapshot.HdrOutputActive,
            Verified = verified,
            Downgraded = runtimeSnapshot.HdrAutoDowngraded,
            VerificationLevel = hdrValidation.VerificationLevel,
            Status = status,
            MismatchTaxonomy = taxonomy
        };
    }

    private static IReadOnlyList<MismatchTaxonomyEntry> BuildMismatchTaxonomy(IReadOnlyList<string> mismatches)
    {
        if (mismatches == null || mismatches.Count == 0)
        {
            return Array.Empty<MismatchTaxonomyEntry>();
        }

        var entries = new List<MismatchTaxonomyEntry>(mismatches.Count);
        foreach (var mismatch in mismatches)
        {
            var (code, expected, actual) = ParsePrimaryMismatch(new[] { mismatch });
            var normalizedCode = code ?? mismatch;
            var category = normalizedCode switch
            {
                var c when c.StartsWith("pixfmt", StringComparison.OrdinalIgnoreCase) => "HDR",
                var c when c.StartsWith("colorimetry", StringComparison.OrdinalIgnoreCase) => "Colorimetry",
                var c when c.StartsWith("hdr-metadata", StringComparison.OrdinalIgnoreCase) => "HDR",
                var c when c.StartsWith("cadence", StringComparison.OrdinalIgnoreCase) => "Cadence",
                var c when c.StartsWith("fps", StringComparison.OrdinalIgnoreCase) => "Timing",
                var c when c.StartsWith("resolution", StringComparison.OrdinalIgnoreCase) => "Geometry",
                _ => "General"
            };
            var severity = category is "HDR" or "Colorimetry"
                ? "Error"
                : category == "Cadence"
                    ? "Warning"
                    : "Info";

            entries.Add(new MismatchTaxonomyEntry
            {
                Category = category,
                Code = normalizedCode,
                Severity = severity,
                Expected = expected,
                Actual = actual
            });
        }

        return entries;
    }

    private static string? TryGetMismatchPart(string detail, string keyPrefix)
    {
        var index = detail.IndexOf(keyPrefix, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            return null;
        }

        var start = index + keyPrefix.Length;
        if (start >= detail.Length)
        {
            return null;
        }

        var end = detail.IndexOf(',', start);
        if (end < 0)
        {
            end = detail.Length;
        }

        return detail[start..end].Trim();
    }

    private static RecordingVerificationResult CreateEarlyFailure(
        string? outputPath, string message, string mismatchCode,
        bool fileExists = false, long? fileSizeBytes = null)
        => new()
        {
            Succeeded = false,
            Message = message,
            OutputPath = outputPath,
            FileExists = fileExists,
            FileSizeBytes = fileSizeBytes ?? 0,
            VerificationMode = "none",
            PrimaryMismatchCode = mismatchCode,
            Mismatches = new[] { mismatchCode }
        };
}
