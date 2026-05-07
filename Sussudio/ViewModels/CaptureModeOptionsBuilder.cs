using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

internal static class CaptureModeOptionsBuilder
{
    internal static IReadOnlyList<ResolutionOption> BuildResolutionOptions(
        IEnumerable<KeyValuePair<string, List<MediaFormat>>> resolutionToFormats,
        bool hdrEnabled,
        bool showAllCaptureOptions,
        SourceSignalTelemetrySnapshot sourceTelemetry)
    {
        var options = resolutionToFormats
            .Where(entry => entry.Value.Count > 0)
            .Select(entry =>
            {
                var formats = entry.Value;
                var first = formats[0];
                var hdrSupported = formats.Any(IsHdrModeCandidate);
                var enabled = !hdrEnabled || hdrSupported;
                return new ResolutionOption
                {
                    Value = entry.Key,
                    Width = first.Width,
                    Height = first.Height,
                    IsEnabled = enabled,
                    DisableReason = enabled
                        ? string.Empty
                        : "HDR mode is not supported at this resolution."
                };
            })
            .OrderByDescending(option => (long)option.Width * option.Height)
            .ToList();

        if (!showAllCaptureOptions && sourceTelemetry.HasDimensions)
        {
            options = options
                .Where(option => DoesResolutionMatchSourceAspectRatio(option, sourceTelemetry))
                .ToList();
        }

        return options;
    }

    internal static IReadOnlyList<string> BuildVideoFormatOptions(IEnumerable<MediaFormat> formats)
    {
        var pixelFormats = formats
            .Select(NormalizeVideoFormatName)
            .Where(format => !string.IsNullOrWhiteSpace(format))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(MediaFormat.GetPixelFormatPriority)
            .ThenBy(format => format, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var options = new List<string> { "Auto" };
        options.AddRange(pixelFormats);
        return options;
    }

    internal static bool IsHdrModeCandidate(MediaFormat format)
        => format.IsHdr || MediaFormat.IsTrue10BitPixelFormat(format.PixelFormat);

    private static string NormalizeVideoFormatName(MediaFormat format)
        => string.IsNullOrWhiteSpace(format.PixelFormat)
            ? string.Empty
            : format.PixelFormat.Trim().ToUpperInvariant();

    private static bool DoesResolutionMatchSourceAspectRatio(
        ResolutionOption option,
        SourceSignalTelemetrySnapshot sourceTelemetry)
    {
        if (!sourceTelemetry.HasDimensions)
        {
            return true;
        }

        var sourceWidth = (uint)Math.Max(0, sourceTelemetry.Width ?? 0);
        var sourceHeight = (uint)Math.Max(0, sourceTelemetry.Height ?? 0);
        if (sourceWidth == 0 || sourceHeight == 0 || option.Width == 0 || option.Height == 0)
        {
            return true;
        }

        var reducedSource = ReduceAspectRatio(sourceWidth, sourceHeight);
        var reducedOption = ReduceAspectRatio(option.Width, option.Height);
        return reducedSource.Width == reducedOption.Width &&
               reducedSource.Height == reducedOption.Height;
    }

    private static (uint Width, uint Height) ReduceAspectRatio(uint width, uint height)
    {
        if (width == 0 || height == 0)
        {
            return (width, height);
        }

        var divisor = GreatestCommonDivisor(width, height);
        return divisor == 0
            ? (width, height)
            : (width / divisor, height / divisor);
    }

    private static uint GreatestCommonDivisor(uint a, uint b)
    {
        while (b != 0)
        {
            var next = a % b;
            a = b;
            b = next;
        }

        return a;
    }
}
