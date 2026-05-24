using System;
using System.Collections.Generic;
using System.IO;
using Sussudio.Models;

namespace Sussudio.Services.Recording;

public sealed partial class RecordingVerifier
{
    private readonly record struct HdrValidationResult(
        bool? HdrMetadataPresent,
        bool? ColorimetryValid,
        bool? MasteringMetadataPresent,
        string VerificationLevel);

    private static void ValidateContainer(
        CaptureRuntimeSnapshot runtimeSnapshot,
        string? detectedContainer,
        string outputPath,
        List<string> mismatches)
    {
        var expectedFormat = ResolveExpectedFormat(runtimeSnapshot, outputPath);

        if (string.IsNullOrWhiteSpace(detectedContainer))
        {
            mismatches.Add("container-undetected");
            return;
        }

        var normalizedContainer = detectedContainer.ToLowerInvariant();
        if (!normalizedContainer.Contains("mp4") && !normalizedContainer.Contains("mov"))
        {
            mismatches.Add($"container-mismatch(expected=mp4,actual={detectedContainer})");
            return;
        }

        var extension = Path.GetExtension(outputPath);
        var expectsMov = expectedFormat.Contains("Mov", StringComparison.OrdinalIgnoreCase) ||
                         (expectedFormat.Length == 0 && extension.Equals(".mov", StringComparison.OrdinalIgnoreCase));
        if (expectsMov)
        {
            if (!normalizedContainer.Contains("mov"))
            {
                mismatches.Add($"container-mismatch(expected=mov,actual={detectedContainer})");
            }

            return;
        }

        var expectsMp4 = expectedFormat.Contains("H264", StringComparison.OrdinalIgnoreCase) ||
                         expectedFormat.Contains("Hevc", StringComparison.OrdinalIgnoreCase) ||
                         expectedFormat.Contains("Av1", StringComparison.OrdinalIgnoreCase) ||
                         (expectedFormat.Length == 0 && extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase));
        if (expectsMp4 && !normalizedContainer.Contains("mp4"))
        {
            mismatches.Add($"container-mismatch(expected=mp4,actual={detectedContainer})");
        }
    }

    private static void ValidateCodec(
        CaptureRuntimeSnapshot runtimeSnapshot,
        string? detectedCodec,
        string outputPath,
        List<string> mismatches)
    {
        var expectedFormat = ResolveExpectedFormat(runtimeSnapshot, outputPath);
        if (expectedFormat.Length == 0 || string.IsNullOrWhiteSpace(detectedCodec))
        {
            if (string.IsNullOrWhiteSpace(detectedCodec))
            {
                mismatches.Add("codec-undetected");
            }

            return;
        }

        var codec = detectedCodec.ToLowerInvariant();
        bool codecMatch = expectedFormat switch
        {
            var f when f.Contains("H264", StringComparison.OrdinalIgnoreCase) => codec.Contains("h264"),
            var f when f.Contains("Hevc", StringComparison.OrdinalIgnoreCase) => codec.Contains("hevc") || codec.Contains("h265"),
            var f when f.Contains("Av1", StringComparison.OrdinalIgnoreCase) => codec.Contains("av1"),
            _ => true
        };

        if (!codecMatch)
        {
            mismatches.Add($"codec-mismatch(expected={expectedFormat},actual={detectedCodec})");
        }
    }

    private static void ValidateDimensions(
        CaptureRuntimeSnapshot runtimeSnapshot,
        uint? detectedWidth,
        uint? detectedHeight,
        List<string> mismatches)
    {
        // Verify the output file against the negotiated capture geometry when available;
        // requested values are only a fallback if negotiation metadata is missing.
        var expectedWidth = runtimeSnapshot.NegotiatedWidth ?? runtimeSnapshot.RequestedWidth;
        var expectedHeight = runtimeSnapshot.NegotiatedHeight ?? runtimeSnapshot.RequestedHeight;
        if (!expectedWidth.HasValue || !expectedHeight.HasValue)
        {
            return;
        }

        if (!detectedWidth.HasValue || !detectedHeight.HasValue)
        {
            mismatches.Add("resolution-undetected");
            return;
        }

        if (detectedWidth.Value != expectedWidth.Value ||
            detectedHeight.Value != expectedHeight.Value)
        {
            mismatches.Add(
                $"resolution-mismatch(expected={expectedWidth.Value}x{expectedHeight.Value},actual={detectedWidth}x{detectedHeight})");
        }
    }

    private static void ValidateFrameRate(
        CaptureRuntimeSnapshot runtimeSnapshot,
        double? detectedFrameRate,
        double? expectedFrameRate,
        List<string> mismatches)
    {
        if (!expectedFrameRate.HasValue)
        {
            return;
        }

        if (!detectedFrameRate.HasValue)
        {
            mismatches.Add("fps-undetected");
            return;
        }

        var expected = expectedFrameRate.Value;
        var actual = detectedFrameRate.Value;
        const double tolerance = 0.75;
        if (Math.Abs(expected - actual) > tolerance)
        {
            mismatches.Add($"fps-mismatch(expected={expected:0.###},actual={actual:0.###})");
        }
    }

    private static double? ResolveExpectedFrameRate(CaptureRuntimeSnapshot runtimeSnapshot)
    {
        static double? ResolveFrameRate(uint? numerator, uint? denominator, string? rateArg, double? frameRate)
        {
            if (numerator.HasValue &&
                denominator.HasValue &&
                denominator.Value > 0)
            {
                return numerator.Value / (double)denominator.Value;
            }

            if (TryParseRational(rateArg) is { } parsedArg)
            {
                return parsedArg;
            }

            return frameRate;
        }

        // Verify the output file against the negotiated capture timing when available;
        // requested values are only a fallback if negotiation metadata is missing.
        return ResolveFrameRate(
                   runtimeSnapshot.NegotiatedFrameRateNumerator,
                   runtimeSnapshot.NegotiatedFrameRateDenominator,
                   runtimeSnapshot.NegotiatedFrameRateArg,
                   runtimeSnapshot.NegotiatedFrameRate)
               ?? ResolveFrameRate(
                   runtimeSnapshot.RequestedFrameRateNumerator,
                   runtimeSnapshot.RequestedFrameRateDenominator,
                   runtimeSnapshot.RequestedFrameRateArg,
                   runtimeSnapshot.RequestedFrameRate);
    }

    private static void ValidateCadence(CadenceMetrics? metrics, List<string> mismatches)
    {
        if (!metrics.HasValue)
        {
            return;
        }

        var cadence = metrics.Value;
        if (cadence.SampleCount < 120)
        {
            return;
        }

        if (cadence.EstimatedDropPercent >= 5.0)
        {
            mismatches.Add($"cadence-drop-high(percent={cadence.EstimatedDropPercent:0.###},estimated={cadence.EstimatedDroppedFrames})");
        }

        if (cadence.SevereGapPercent >= 3.0)
        {
            mismatches.Add($"cadence-gaps-high(percent={cadence.SevereGapPercent:0.###},count={cadence.SevereGapCount})");
        }

        if (cadence.ExpectedIntervalMs > 0 &&
            cadence.P95IntervalMs >= cadence.ExpectedIntervalMs * 2.5)
        {
            mismatches.Add(
                $"cadence-p95-high(expectedMs={cadence.ExpectedIntervalMs:0.###},p95Ms={cadence.P95IntervalMs:0.###})");
        }
    }

    private static HdrValidationResult ValidateHdrMetadata(
        CaptureRuntimeSnapshot runtimeSnapshot,
        string? detectedCodec,
        string? detectedPixelFormat,
        string? detectedColorPrimaries,
        string? detectedColorTransfer,
        string? detectedColorSpace,
        bool? hdrSideDataPresent,
        List<string> mismatches)
    {
        var hdrExpected = runtimeSnapshot.HdrOutputActive ||
                          (runtimeSnapshot.RequestedHdrEnabled ?? false);
        if (!hdrExpected)
        {
            return new HdrValidationResult(
                HdrMetadataPresent: null,
                ColorimetryValid: null,
                MasteringMetadataPresent: null,
                VerificationLevel: "NotHdr");
        }

        var codecLooksHdrCapable = !string.IsNullOrWhiteSpace(detectedCodec) &&
                                   (detectedCodec.Contains("hevc", StringComparison.OrdinalIgnoreCase) ||
                                    detectedCodec.Contains("h265", StringComparison.OrdinalIgnoreCase) ||
                                    detectedCodec.Contains("av1", StringComparison.OrdinalIgnoreCase));
        if (!codecLooksHdrCapable)
        {
            mismatches.Add($"codec-not-hdr-capable(actual={detectedCodec ?? "unknown"})");
        }

        var pixelFormatLooksHdr = !string.IsNullOrWhiteSpace(detectedPixelFormat) &&
                                  (string.Equals(detectedPixelFormat, "p010le", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(detectedPixelFormat, "yuv420p10le", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(detectedPixelFormat, "yuv422p10le", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(detectedPixelFormat, "yuv444p10le", StringComparison.OrdinalIgnoreCase));
        if (!pixelFormatLooksHdr)
        {
            mismatches.Add($"pixfmt-not-10bit(actual={detectedPixelFormat ?? "unknown"})");
        }

        var primariesOk = !string.IsNullOrWhiteSpace(detectedColorPrimaries) &&
                          detectedColorPrimaries.Contains("bt2020", StringComparison.OrdinalIgnoreCase);
        if (!primariesOk)
        {
            mismatches.Add($"colorimetry-mismatch(primaries={detectedColorPrimaries ?? "unknown"})");
        }

        var transferOk = !string.IsNullOrWhiteSpace(detectedColorTransfer) &&
                         detectedColorTransfer.Contains("smpte2084", StringComparison.OrdinalIgnoreCase);
        if (!transferOk)
        {
            mismatches.Add($"colorimetry-mismatch(transfer={detectedColorTransfer ?? "unknown"})");
        }

        var spaceOk = !string.IsNullOrWhiteSpace(detectedColorSpace) &&
                      (string.Equals(detectedColorSpace, "bt2020nc", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(detectedColorSpace, "bt2020c", StringComparison.OrdinalIgnoreCase));
        if (!spaceOk)
        {
            mismatches.Add($"colorimetry-mismatch(space={detectedColorSpace ?? "unknown"})");
        }

        var masteringMetadataRequested = runtimeSnapshot.RequestedHdrMasteringMetadata == true;
        var masteringMetadataPresent = hdrSideDataPresent == true;
        if (masteringMetadataRequested && !masteringMetadataPresent)
        {
            mismatches.Add("hdr-metadata-missing");
        }

        var colorimetryValid = codecLooksHdrCapable &&
                               pixelFormatLooksHdr &&
                               primariesOk &&
                               transferOk &&
                               spaceOk;
        var verificationLevel = masteringMetadataRequested
            ? "FullMetadata"
            : "ColorimetryOnly";

        return new HdrValidationResult(
            HdrMetadataPresent: colorimetryValid && (!masteringMetadataRequested || masteringMetadataPresent),
            ColorimetryValid: colorimetryValid,
            MasteringMetadataPresent: masteringMetadataPresent,
            VerificationLevel: verificationLevel);
    }

    private static string ResolveExpectedFormat(CaptureRuntimeSnapshot runtimeSnapshot, string? outputPath)
    {
        if (!string.IsNullOrWhiteSpace(outputPath) &&
            !string.IsNullOrWhiteSpace(runtimeSnapshot.FlashbackExportOutputPath) &&
            !string.IsNullOrWhiteSpace(runtimeSnapshot.FlashbackExportVerificationFormat) &&
            string.Equals(
                Path.GetFullPath(outputPath),
                Path.GetFullPath(runtimeSnapshot.FlashbackExportOutputPath),
                StringComparison.OrdinalIgnoreCase))
        {
            return runtimeSnapshot.FlashbackExportVerificationFormat;
        }

        if (!string.IsNullOrWhiteSpace(outputPath) &&
            !string.IsNullOrWhiteSpace(runtimeSnapshot.LastOutputPath) &&
            !string.IsNullOrWhiteSpace(runtimeSnapshot.FlashbackExportVerificationFormat) &&
            IsFlashbackRecording(runtimeSnapshot) &&
            string.Equals(
                Path.GetFullPath(outputPath),
                Path.GetFullPath(runtimeSnapshot.LastOutputPath),
                StringComparison.OrdinalIgnoreCase))
        {
            return runtimeSnapshot.FlashbackExportVerificationFormat;
        }

        return runtimeSnapshot.RequestedFormat ?? string.Empty;
    }

    private static bool IsFlashbackRecording(CaptureRuntimeSnapshot runtimeSnapshot)
        => string.Equals(runtimeSnapshot.RecordingBackend, "Flashback", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(runtimeSnapshot.RecordingIntegrityBackend, "Flashback", StringComparison.OrdinalIgnoreCase);
}
