using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.Services.Recording;

public sealed partial class RecordingVerifier
{
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
