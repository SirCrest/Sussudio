using System;
using System.Collections.Generic;
using System.IO;
using Sussudio.Models;

namespace Sussudio.Services.Recording;

public sealed partial class RecordingVerifier
{
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
