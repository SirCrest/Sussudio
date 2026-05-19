using System;
using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.Services.Recording;

public sealed partial class RecordingVerifier
{
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
}
