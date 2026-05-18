using System;
using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.Services.Recording;

public sealed partial class RecordingVerifier
{
    private readonly record struct HdrValidationResult(
        bool? HdrMetadataPresent,
        bool? ColorimetryValid,
        bool? MasteringMetadataPresent,
        string VerificationLevel);

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
}
