using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

internal static partial class CaptureSettingsProjectionBuilder
{
    private static CaptureSettingsFrameRateProjection ProjectFrameRate(CaptureSettingsProjectionInput input)
    {
        var selectedFrameRateOption = input.AvailableFrameRates
            .FirstOrDefault(option => FrameRateTimingPolicy.IsFrameRateMatch(option.Value, input.SelectedFrameRate))
            ?? input.AvailableFrameRates.FirstOrDefault(option => FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, input.SelectedFrameRate));

        var requestedFrameRateArg = selectedFrameRateOption?.Rational;
        var requestedFrameRateNumerator = selectedFrameRateOption?.Numerator;
        var requestedFrameRateDenominator = selectedFrameRateOption?.Denominator;
        var effectiveFrameRate = input.IsAutoResolutionSelected && input.AutoResolvedFrameRate.HasValue && input.AutoResolvedFrameRate.Value > 0
            ? input.AutoResolvedFrameRate.Value
            : input.SelectedFrameRate > 0
            ? input.SelectedFrameRate
            : selectedFrameRateOption?.Value
                ?? input.SelectedFormat?.FrameRateExact
                ?? 60;
        var selectedFriendlyRate = selectedFrameRateOption?.FriendlyValue ?? effectiveFrameRate;
        var runtimeRate = input.Runtime.ActualFrameRate ?? input.Runtime.NegotiatedFrameRate;
        var runtimeRateArg = input.Runtime.ActualFrameRateArg ?? input.Runtime.NegotiatedFrameRateArg;
        var runtimeMatchesResolution = false;
        if (input.EffectiveResolutionKnown)
        {
            runtimeMatchesResolution =
                (input.Runtime.ActualWidth == input.EffectiveWidth && input.Runtime.ActualHeight == input.EffectiveHeight) ||
                (input.Runtime.NegotiatedWidth == input.EffectiveWidth && input.Runtime.NegotiatedHeight == input.EffectiveHeight);
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

            if (input.Runtime.NegotiatedFrameRateNumerator.HasValue &&
                input.Runtime.NegotiatedFrameRateDenominator.HasValue &&
                input.Runtime.NegotiatedFrameRateDenominator.Value > 0)
            {
                requestedFrameRateNumerator = input.Runtime.NegotiatedFrameRateNumerator;
                requestedFrameRateDenominator = input.Runtime.NegotiatedFrameRateDenominator;
            }
            else if (FrameRateTimingPolicy.TryParseFrameRateRational(runtimeRateArg, out var runtimeNumerator, out var runtimeDenominator))
            {
                requestedFrameRateNumerator = runtimeNumerator;
                requestedFrameRateDenominator = runtimeDenominator;
            }
        }

        if (input.SourceTelemetry.HasFrameRate &&
            FrameRateTimingPolicy.IsFriendlyFrameRateMatch(selectedFriendlyRate, input.SourceTelemetry.FrameRateExact ?? 0))
        {
            if (!string.IsNullOrWhiteSpace(input.SourceTelemetry.FrameRateArg))
            {
                requestedFrameRateArg = input.SourceTelemetry.FrameRateArg;
            }

            if (FrameRateTimingPolicy.TryParseFrameRateRational(input.SourceTelemetry.FrameRateArg, out var sourceNumerator, out var sourceDenominator))
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
            if (input.SelectedFormat?.FrameRateNumerator > 0 && input.SelectedFormat.FrameRateDenominator > 0)
            {
                requestedFrameRateNumerator = input.SelectedFormat.FrameRateNumerator;
                requestedFrameRateDenominator = input.SelectedFormat.FrameRateDenominator;
                requestedFrameRateArg = input.SelectedFormat.FrameRateRational;
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

    private static string? ResolveRequestedPixelFormat(CaptureSettingsProjectionInput input)
    {
        if (!string.Equals(input.SelectedVideoFormat, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            return input.SelectedVideoFormat;
        }

        var format = input.SelectedFormat;
        if (format != null &&
            !input.IsHdrEnabled &&
            format.Width >= 3840 &&
            format.Height >= 2160 &&
            format.FrameRateExact >= 100)
        {
            return "MJPG";
        }

        return format?.PixelFormat;
    }

    private static bool ShouldForceMjpegDecode(CaptureSettingsProjectionInput input)
    {
        if (string.Equals(input.SelectedVideoFormat, "MJPG", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(input.SelectedVideoFormat, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            var format = input.SelectedFormat;
            return format != null &&
                   !input.IsHdrEnabled &&
                   format.Width >= 3840 &&
                   format.Height >= 2160 &&
                   format.FrameRateExact >= 100;
        }

        return false;
    }
}

internal sealed class CaptureSettingsProjectionInput
{
    public bool EffectiveResolutionKnown { get; init; }
    public uint EffectiveWidth { get; init; }
    public uint EffectiveHeight { get; init; }
    public string? SelectedResolution { get; init; }
    public double SelectedFrameRate { get; init; }
    public double? AutoResolvedFrameRate { get; init; }
    public bool IsAutoResolutionSelected { get; init; }
    public MediaFormat? SelectedFormat { get; init; }
    public IReadOnlyList<FrameRateOption> AvailableFrameRates { get; init; } = Array.Empty<FrameRateOption>();
    public CaptureRuntimeSnapshot Runtime { get; init; } = new();
    public SourceSignalTelemetrySnapshot SourceTelemetry { get; init; } = new();
    public string? SelectedVideoFormat { get; init; }
    public bool IsHdrEnabled { get; init; }
    public bool IsTrueHdrPreviewEnabled { get; init; }
    public int MjpegDecoderCount { get; init; }
    public string? SelectedRecordingFormat { get; init; }
    public string? SelectedQuality { get; init; }
    public string? SelectedPreset { get; init; }
    public string? SelectedSplitEncodeMode { get; init; }
    public double CustomBitrateMbps { get; init; }
    public string OutputPath { get; init; } = string.Empty;
    public bool FlashbackGpuDecode { get; init; }
    public int FlashbackBufferMinutes { get; init; }
    public bool IsAudioEnabled { get; init; }
    public bool IsCustomAudioInputEnabled { get; init; }
    public string? SelectedAudioInputDeviceId { get; init; }
    public string? SelectedAudioInputDeviceName { get; init; }
    public bool IsMicrophoneEnabled { get; init; }
    public string? SelectedMicrophoneDeviceId { get; init; }
    public string? SelectedMicrophoneDeviceName { get; init; }
}

internal readonly record struct CaptureSettingsFrameRateProjection(
    double EffectiveFrameRate,
    string? RequestedFrameRateArg,
    uint? RequestedFrameRateNumerator,
    uint? RequestedFrameRateDenominator);
