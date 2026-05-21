using System;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Flashback session frame-rate rational policy for encoded packet timing.
public partial class CaptureService
{
    private static (int? Numerator, int? Denominator, double EffectiveFrameRate) ResolveFlashbackSessionFrameRateParts(
        CaptureSettings settings,
        double deliveryFrameRate)
    {
        // Preserve exact rationals only when they describe the actual delivered USB cadence.
        // A source-reported 120000/1001 rate paired with ~120 delivered frames/sec causes A/V
        // drift if we stamp Flashback video against the slower source clock.
        if (!double.IsFinite(deliveryFrameRate) || deliveryFrameRate <= 0)
        {
            return (null, null, deliveryFrameRate);
        }

        if (settings.RequestedFrameRateNumerator is not uint numerator ||
            settings.RequestedFrameRateDenominator is not uint denominator ||
            numerator == 0 ||
            denominator == 0 ||
            numerator > int.MaxValue ||
            denominator > int.MaxValue)
        {
            return InferFlashbackSessionFrameRateParts(deliveryFrameRate);
        }

        var rationalFps = numerator / (double)denominator;
        if (!double.IsFinite(rationalFps) || rationalFps <= 0)
        {
            return (null, null, deliveryFrameRate);
        }

        var deltaFps = Math.Abs(rationalFps - deliveryFrameRate);
        var toleranceFps = Math.Max(0.01, deliveryFrameRate * 0.0001);
        if (deltaFps > toleranceFps)
        {
            Logger.Log(
                $"FLASHBACK_FRAME_RATE_RATIONAL_REJECT requested={numerator}/{denominator} " +
                $"rational={rationalFps:0.######} delivery={deliveryFrameRate:0.######} " +
                $"delta={deltaFps:0.######} tolerance={toleranceFps:0.######}");
            return InferFlashbackSessionFrameRateParts(deliveryFrameRate);
        }

        Logger.Log(
            $"FLASHBACK_FRAME_RATE_RATIONAL_ACCEPT requested={numerator}/{denominator} " +
            $"delivery={deliveryFrameRate:0.######} effective={rationalFps:0.######}");
        return ((int)numerator, (int)denominator, rationalFps);
    }

    private static (int? Numerator, int? Denominator, double EffectiveFrameRate) InferFlashbackSessionFrameRateParts(double deliveryFrameRate)
    {
        foreach (var (numerator, denominator) in CommonFlashbackFrameRateParts)
        {
            var rationalFps = numerator / (double)denominator;
            var deltaFps = Math.Abs(rationalFps - deliveryFrameRate);
            var toleranceFps = Math.Max(0.01, deliveryFrameRate * 0.0001);
            if (deltaFps <= toleranceFps)
            {
                Logger.Log(
                    $"FLASHBACK_FRAME_RATE_RATIONAL_INFER inferred={numerator}/{denominator} " +
                    $"delivery={deliveryFrameRate:0.######} effective={rationalFps:0.######}");
                return (numerator, denominator, rationalFps);
            }
        }

        return (null, null, deliveryFrameRate);
    }

    private static readonly (int Numerator, int Denominator)[] CommonFlashbackFrameRateParts =
    {
        (24, 1),
        (24000, 1001),
        (25, 1),
        (30, 1),
        (30000, 1001),
        (50, 1),
        (60, 1),
        (60000, 1001),
        (100, 1),
        (120, 1),
        (120000, 1001),
        (144, 1),
        (240, 1)
    };
}
