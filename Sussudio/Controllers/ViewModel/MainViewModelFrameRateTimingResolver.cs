using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

/// <summary>
/// Graph-built ports consumed by the frame-rate timing resolver.
/// </summary>
internal sealed class MainViewModelFrameRateTimingResolverContext
{
    public required Func<IReadOnlyDictionary<string, List<MediaFormat>>> GetResolutionToFormats { get; init; }
    public required Func<CaptureRuntimeSnapshot> GetRuntimeSnapshot { get; init; }
    public required Func<SourceSignalTelemetrySnapshot> GetLatestSourceTelemetry { get; init; }
    public required Func<MediaFormat?> GetSelectedFormat { get; init; }
    public required ObservableCollection<FrameRateOption> AvailableFrameRates { get; init; }
}

/// <summary>
/// Resolves stateful frame-rate timing preferences and detected source rates
/// for the MainViewModel compatibility facade.
/// </summary>
internal sealed class MainViewModelFrameRateTimingResolver
{
    private readonly MainViewModelFrameRateTimingResolverContext _context;

    public MainViewModelFrameRateTimingResolver(MainViewModelFrameRateTimingResolverContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IReadOnlyList<FrameRateTimingVariant> BuildFrameRateTimingVariants(string? resolutionKey)
    {
        if (string.IsNullOrWhiteSpace(resolutionKey) ||
            !_context.GetResolutionToFormats().TryGetValue(resolutionKey, out var formats))
        {
            return Array.Empty<FrameRateTimingVariant>();
        }

        return FrameRateTimingPolicy.BuildTimingVariants(formats);
    }

    public FrameRateTimingFamily ResolvePreferredTimingFamily(string? resolutionKey, double previousRate)
    {
        var runtime = _context.GetRuntimeSnapshot();
        if (CaptureResolutionSelectionPolicy.TryParseResolutionKey(resolutionKey, out var selectedWidth, out var selectedHeight))
        {
            if (runtime.ActualWidth == selectedWidth &&
                runtime.ActualHeight == selectedHeight &&
                FrameRateTimingPolicy.TryInferFrameRateTimingFamily(
                    runtime.ActualFrameRateArg ?? runtime.NegotiatedFrameRateArg,
                    runtime.ActualFrameRate ?? runtime.NegotiatedFrameRate,
                    out var runtimeFamily))
            {
                return runtimeFamily;
            }

            if (runtime.NegotiatedWidth == selectedWidth &&
                runtime.NegotiatedHeight == selectedHeight &&
                FrameRateTimingPolicy.TryInferFrameRateTimingFamily(
                    runtime.NegotiatedFrameRateArg,
                    runtime.NegotiatedFrameRate,
                    out var negotiatedFamily))
            {
                return negotiatedFamily;
            }
        }

        var selectedFormat = _context.GetSelectedFormat();
        if (FrameRateTimingPolicy.TryInferFrameRateTimingFamily(selectedFormat?.FrameRateRational, selectedFormat?.FrameRateExact, out var selectedFamily))
        {
            return selectedFamily;
        }

        var selectedOption = _context.AvailableFrameRates.FirstOrDefault(option => FrameRateTimingPolicy.IsFrameRateMatch(option.Value, previousRate));
        if (selectedOption != null &&
            FrameRateTimingPolicy.TryInferFrameRateTimingFamily(selectedOption.Rational, selectedOption.Value, out var optionFamily))
        {
            return optionFamily;
        }

        if (FrameRateTimingPolicy.TryInferFrameRateTimingFamily(null, previousRate, out var previousFamily))
        {
            return previousFamily;
        }

        return FrameRateTimingFamily.Unknown;
    }

    public (double? Rate, string? Arg, string Origin) ResolveDetectedSourceFrameRate(
        string? resolutionKey,
        IReadOnlyList<FrameRateOption> options,
        double previousRate)
    {
        var latestSourceTelemetry = _context.GetLatestSourceTelemetry();
        if (latestSourceTelemetry.HasFrameRate)
        {
            return (
                latestSourceTelemetry.FrameRateExact,
                latestSourceTelemetry.FrameRateArg,
                latestSourceTelemetry.Origin != SourceTelemetryOrigin.Unknown
                    ? latestSourceTelemetry.Origin.ToString()
                    : "SourceTelemetry");
        }

        var runtime = _context.GetRuntimeSnapshot();
        if (CaptureResolutionSelectionPolicy.TryParseResolutionKey(resolutionKey, out var selectedWidth, out var selectedHeight))
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

        var selectedFormat = _context.GetSelectedFormat();
        if (selectedFormat != null &&
            options.Any(option => FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, selectedFormat.FrameRateExact)))
        {
            return (
                selectedFormat.FrameRateExact,
                string.IsNullOrWhiteSpace(selectedFormat.FrameRateRational)
                    ? null
                    : selectedFormat.FrameRateRational,
                "SelectedMode");
        }

        if (previousRate > 0 &&
            options.Any(option => FrameRateTimingPolicy.IsFriendlyFrameRateMatch(option.FriendlyValue, previousRate)))
        {
            return (previousRate, null, "SelectedMode");
        }

        return (null, null, "Unknown");
    }
}
