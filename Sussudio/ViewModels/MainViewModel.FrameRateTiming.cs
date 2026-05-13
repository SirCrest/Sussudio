using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;
using Sussudio.Services.Telemetry;

namespace Sussudio.ViewModels;

/// <summary>
/// Shared frame-rate timing, rational parsing, and source-rate preference policy
/// used by capture mode, resolution, and automation option selection.
/// </summary>
public partial class MainViewModel
{
    private static string FormatFriendlyFrameRate(double frameRate)
        => $"{Math.Round(frameRate):0}";

    private FrameRateTimingFamily ResolvePreferredTimingFamily(string? resolutionKey, double previousRate)
    {
        var runtime = _captureService.GetRuntimeSnapshot();
        if (TryParseResolutionKey(resolutionKey, out var selectedWidth, out var selectedHeight))
        {
            if (runtime.ActualWidth == selectedWidth &&
                runtime.ActualHeight == selectedHeight &&
                TryInferFrameRateTimingFamily(
                    runtime.ActualFrameRateArg ?? runtime.NegotiatedFrameRateArg,
                    runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate,
                    out var runtimeFamily))
            {
                return runtimeFamily;
            }

            if (runtime.NegotiatedWidth == selectedWidth &&
                runtime.NegotiatedHeight == selectedHeight &&
                TryInferFrameRateTimingFamily(
                    runtime.NegotiatedFrameRateArg,
                    runtime.NegotiatedFrameRate,
                    out var negotiatedFamily))
            {
                return negotiatedFamily;
            }
        }

        if (TryInferFrameRateTimingFamily(SelectedFormat?.FrameRateRational, SelectedFormat?.FrameRateExact, out var selectedFamily))
        {
            return selectedFamily;
        }

        var selectedOption = AvailableFrameRates.FirstOrDefault(option => IsFrameRateMatch(option.Value, previousRate));
        if (selectedOption != null &&
            TryInferFrameRateTimingFamily(selectedOption.Rational, selectedOption.Value, out var optionFamily))
        {
            return optionFamily;
        }

        if (TryInferFrameRateTimingFamily(null, previousRate, out var previousFamily))
        {
            return previousFamily;
        }

        return FrameRateTimingFamily.Unknown;
    }

    private static MediaFormat SelectPreferredFrameRateFormat(
        IReadOnlyList<MediaFormat> candidates,
        int friendlyBucket,
        FrameRateTimingFamily timingFamily)
    {
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException("No frame-rate candidates are available.");
        }

        return candidates
            .OrderBy(format => GetTimingFamilyRank(format, friendlyBucket, timingFamily))
            .ThenBy(format => Math.Abs(format.FrameRateExact - friendlyBucket))
            .ThenByDescending(IsHdrModeCandidate)
            .ThenBy(format => GetEffectivePixelFormatPriority(format))
            .First();
    }

    /// <summary>
    /// At 4K HFR (>=3840x2160 @ >=100fps SDR), prefer MJPG over NV12. The UVC driver
    /// presents NV12 at these rates, but it is actually CPU-decoded MJPG causing frame
    /// drops. Selecting raw MJPG lets MF use GPU DXVA decode via hardware transforms.
    /// </summary>
    private static int GetEffectivePixelFormatPriority(MediaFormat format)
    {
        if (format.Width >= 3840 &&
            format.Height >= 2160 &&
            format.FrameRateExact >= 100 &&
            format.PixelFormat.Equals("MJPG", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return MediaFormat.GetPixelFormatPriority(format.PixelFormat);
    }

    private static int GetTimingFamilyRank(MediaFormat format, int friendlyBucket, FrameRateTimingFamily timingFamily)
    {
        if (format.FrameRateNumerator > 0 && format.FrameRateDenominator > 0)
        {
            return timingFamily switch
            {
                FrameRateTimingFamily.Ntsc1001 when format.FrameRateDenominator == 1001
                    => Math.Abs((int)format.FrameRateNumerator - friendlyBucket * 1000),
                FrameRateTimingFamily.Integer when format.FrameRateDenominator == 1
                    => Math.Abs((int)format.FrameRateNumerator - friendlyBucket),
                FrameRateTimingFamily.Ntsc1001 => 5000 + Math.Abs((int)format.FrameRateNumerator - friendlyBucket * 1000),
                FrameRateTimingFamily.Integer => 5000 + Math.Abs((int)format.FrameRateNumerator - friendlyBucket),
                _ => 100 + (int)Math.Round(Math.Abs(format.FrameRateExact - friendlyBucket) * 100)
            };
        }

        return 100 + (int)Math.Round(Math.Abs(format.FrameRateExact - friendlyBucket) * 100);
    }

    private static bool TryInferFrameRateTimingFamily(
        string? frameRateArg,
        double? frameRate,
        out FrameRateTimingFamily family)
    {
        family = FrameRateTimingFamily.Unknown;

        if (TryParseFrameRateRational(frameRateArg, out var numerator, out var denominator))
        {
            if (denominator == 1001)
            {
                family = FrameRateTimingFamily.Ntsc1001;
                return true;
            }

            if (denominator == 1)
            {
                family = FrameRateTimingFamily.Integer;
                return true;
            }
        }

        if (!frameRate.HasValue || frameRate.Value <= 0)
        {
            return false;
        }

        var value = frameRate.Value;
        var rounded = Math.Round(value);
        if (Math.Abs(value - rounded) <= 0.01)
        {
            family = FrameRateTimingFamily.Integer;
            return true;
        }

        var ntscCandidate = rounded * 1000.0 / 1001.0;
        if (Math.Abs(value - ntscCandidate) <= 0.03)
        {
            family = FrameRateTimingFamily.Ntsc1001;
            return true;
        }

        return false;
    }

    private (double? Rate, string? Arg, string Origin) ResolveDetectedSourceFrameRate(
        string? resolutionKey,
        IReadOnlyList<FrameRateOption> options,
        double previousRate)
    {
        if (_latestSourceTelemetry.HasFrameRate)
        {
            return (
                _latestSourceTelemetry.FrameRateExact,
                _latestSourceTelemetry.FrameRateArg,
                _latestSourceTelemetry.Origin != SourceTelemetryOrigin.Unknown
                    ? _latestSourceTelemetry.Origin.ToString()
                    : "SourceTelemetry");
        }

        var runtime = _captureService.GetRuntimeSnapshot();
        if (TryParseResolutionKey(resolutionKey, out var selectedWidth, out var selectedHeight))
        {
            if (runtime.ActualFrameRate.HasValue &&
                runtime.ActualWidth == selectedWidth &&
                runtime.ActualHeight == selectedHeight)
            {
                return (
                    runtime.ActualFrameRate,
                    runtime.ActualFrameRateArg ??
                    runtime.NegotiatedFrameRateArg,
                    "NegotiatedDeviceFormat");
            }

            if (runtime.NegotiatedFrameRate.HasValue &&
                runtime.NegotiatedWidth == selectedWidth &&
                runtime.NegotiatedHeight == selectedHeight)
            {
                return (
                    runtime.NegotiatedFrameRate,
                    runtime.NegotiatedFrameRateArg,
                    "NegotiatedDeviceFormat");
            }
        }

        if (SelectedFormat != null &&
            options.Any(option => IsFriendlyFrameRateMatch(option.FriendlyValue, SelectedFormat.FrameRateExact)))
        {
            return (
                SelectedFormat.FrameRateExact,
                string.IsNullOrWhiteSpace(SelectedFormat.FrameRateRational)
                    ? null
                    : SelectedFormat.FrameRateRational,
                "SelectedMode");
        }

        if (previousRate > 0 &&
            options.Any(option => IsFriendlyFrameRateMatch(option.FriendlyValue, previousRate)))
        {
            return (previousRate, null, "SelectedMode");
        }

        return (null, null, "Unknown");
    }

    private static bool IsFrameRateMatch(double a, double b, double tolerance = 0.01)
        => Math.Abs(a - b) < tolerance;

    private static bool IsFriendlyFrameRateMatch(double optionFriendlyRate, double requestedRate)
        => Math.Round(optionFriendlyRate) == Math.Round(requestedRate);

    private static bool IsAutoFrameRateValue(double value)
        => value == AutoFrameRateValue || value < 0;

    private static string GetResolutionKey(uint width, uint height)
        => $"{width}x{height}";

    private static int GetFriendlyFrameRateBucket(double frameRate)
        => (int)Math.Round(frameRate, MidpointRounding.AwayFromZero);

    private static bool TryParseFrameRateRational(string? rational, out uint numerator, out uint denominator)
    {
        numerator = 0;
        denominator = 0;
        if (string.IsNullOrWhiteSpace(rational))
        {
            return false;
        }

        var split = rational.Split('/');
        return split.Length == 2 &&
               uint.TryParse(split[0], out numerator) &&
               uint.TryParse(split[1], out denominator) &&
               denominator > 0;
    }
}
