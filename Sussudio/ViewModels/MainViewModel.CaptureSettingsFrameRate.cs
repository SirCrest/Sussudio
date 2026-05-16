using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Projects capture-settings frame-rate intent from UI selection plus observed runtime/source state.
/// </summary>
public partial class MainViewModel
{
    private readonly record struct CaptureSettingsFrameRateRequest(
        bool EffectiveResolutionKnown,
        uint EffectiveWidth,
        uint EffectiveHeight,
        CaptureRuntimeSnapshot Runtime,
        SourceSignalTelemetrySnapshot SourceTelemetry);

    private readonly record struct CaptureSettingsFrameRateProjection(
        double EffectiveFrameRate,
        string? RequestedFrameRateArg,
        uint? RequestedFrameRateNumerator,
        uint? RequestedFrameRateDenominator);

    private CaptureSettingsFrameRateProjection ProjectCaptureSettingsFrameRate(CaptureSettingsFrameRateRequest request)
    {
        var selectedFrameRateOption = AvailableFrameRates
            .FirstOrDefault(option => FrameRateTimingPolicy.IsFrameRateMatch(option.Value, SelectedFrameRate))
            ?? AvailableFrameRates.FirstOrDefault(option => FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, SelectedFrameRate));

        var requestedFrameRateArg = selectedFrameRateOption?.Rational;
        var requestedFrameRateNumerator = selectedFrameRateOption?.Numerator;
        var requestedFrameRateDenominator = selectedFrameRateOption?.Denominator;
        var effectiveFrameRate = IsAutoResolutionValue(SelectedResolution) && AutoResolvedFrameRate.HasValue && AutoResolvedFrameRate.Value > 0
            ? AutoResolvedFrameRate.Value
            : SelectedFrameRate > 0
            ? SelectedFrameRate
            : selectedFrameRateOption?.Value
                ?? SelectedFormat?.FrameRateExact
                ?? 60;
        var selectedFriendlyRate = selectedFrameRateOption?.FriendlyValue ?? effectiveFrameRate;
        var runtimeRate = request.Runtime.ActualFrameRate ?? request.Runtime.NegotiatedFrameRate;
        var runtimeRateArg = request.Runtime.ActualFrameRateArg ?? request.Runtime.NegotiatedFrameRateArg;
        var runtimeMatchesResolution = false;
        if (request.EffectiveResolutionKnown)
        {
            runtimeMatchesResolution =
                (request.Runtime.ActualWidth == request.EffectiveWidth && request.Runtime.ActualHeight == request.EffectiveHeight) ||
                (request.Runtime.NegotiatedWidth == request.EffectiveWidth && request.Runtime.NegotiatedHeight == request.EffectiveHeight);
        }

        if (runtimeMatchesResolution &&
            runtimeRate.HasValue &&
            runtimeRate.Value > 0 &&
            FrameRateTimingPolicy.IsFriendlyFrameRateMatch(selectedFriendlyRate, runtimeRate.Value))
        {
            if (!string.IsNullOrWhiteSpace(runtimeRateArg))
            {
                requestedFrameRateArg = runtimeRateArg;
            }

            if (request.Runtime.NegotiatedFrameRateNumerator.HasValue &&
                request.Runtime.NegotiatedFrameRateDenominator.HasValue &&
                request.Runtime.NegotiatedFrameRateDenominator.Value > 0)
            {
                requestedFrameRateNumerator = request.Runtime.NegotiatedFrameRateNumerator;
                requestedFrameRateDenominator = request.Runtime.NegotiatedFrameRateDenominator;
            }
            else if (FrameRateTimingPolicy.TryParseFrameRateRational(runtimeRateArg, out var runtimeNumerator, out var runtimeDenominator))
            {
                requestedFrameRateNumerator = runtimeNumerator;
                requestedFrameRateDenominator = runtimeDenominator;
            }
        }

        if (request.SourceTelemetry.HasFrameRate &&
            FrameRateTimingPolicy.IsFriendlyFrameRateMatch(selectedFriendlyRate, request.SourceTelemetry.FrameRateExact ?? 0))
        {
            if (!string.IsNullOrWhiteSpace(request.SourceTelemetry.FrameRateArg))
            {
                requestedFrameRateArg = request.SourceTelemetry.FrameRateArg;
            }

            if (FrameRateTimingPolicy.TryParseFrameRateRational(request.SourceTelemetry.FrameRateArg, out var sourceNumerator, out var sourceDenominator))
            {
                requestedFrameRateNumerator = sourceNumerator;
                requestedFrameRateDenominator = sourceDenominator;
            }
        }

        if ((requestedFrameRateNumerator == null || requestedFrameRateDenominator == null) &&
            FrameRateTimingPolicy.TryParseFrameRateRational(requestedFrameRateArg, out var parsedNumerator, out var parsedDenominator))
        {
            requestedFrameRateNumerator = parsedNumerator;
            requestedFrameRateDenominator = parsedDenominator;
        }

        if (requestedFrameRateNumerator == null || requestedFrameRateDenominator == null)
        {
            if (SelectedFormat?.FrameRateNumerator > 0 && SelectedFormat.FrameRateDenominator > 0)
            {
                requestedFrameRateNumerator = SelectedFormat.FrameRateNumerator;
                requestedFrameRateDenominator = SelectedFormat.FrameRateDenominator;
                requestedFrameRateArg = SelectedFormat.FrameRateRational;
            }
            else
            {
                requestedFrameRateArg = effectiveFrameRate.ToString("0.###");
            }
        }

        return new CaptureSettingsFrameRateProjection(
            effectiveFrameRate,
            requestedFrameRateArg,
            requestedFrameRateNumerator,
            requestedFrameRateDenominator);
    }
}
