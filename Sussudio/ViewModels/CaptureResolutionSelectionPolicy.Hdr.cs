using System;
using System.Collections.Generic;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

internal static partial class CaptureResolutionSelectionPolicy
{
    internal static string BuildHdrSupportHint(HdrSupportHintRequest request)
    {
        if (!request.IsHdrEnabled || string.IsNullOrWhiteSpace(request.ResolutionKey))
        {
            return string.Empty;
        }

        var maxHdrRate = GetMaxFrameRateForResolution(
            request.ResolutionToFormats,
            request.ResolutionKey,
            hdrOnly: true);
        if (maxHdrRate <= 0)
        {
            return $"HDR is not supported at {request.ResolutionKey}.";
        }

        if (request.SelectedFrameRate > 0 && maxHdrRate >= request.SelectedFrameRate - 0.01)
        {
            return string.Empty;
        }

        return $"HDR at {request.ResolutionKey} supported up to {FormatFriendlyFrameRate(maxHdrRate)} fps.";
    }

    private static HdrResolutionSelection SelectHdrResolutionOption(
        IReadOnlyList<ResolutionOption> options,
        IReadOnlyDictionary<string, List<MediaFormat>> resolutionToFormats,
        string? previousSelection,
        double preferredFrameRate)
    {
        if (options.Count == 0)
        {
            return new HdrResolutionSelection(null, null);
        }

        var previous = options.FirstOrDefault(option =>
            string.Equals(option.Value, previousSelection, StringComparison.OrdinalIgnoreCase));
        if (previous is { IsEnabled: true } &&
            ResolutionSupportsFrameRate(resolutionToFormats, previous.Value, preferredFrameRate, hdrOnly: true))
        {
            return new HdrResolutionSelection(
                previous,
                BuildHdrSupportHint(new HdrSupportHintRequest(
                    resolutionToFormats,
                    previous.Value,
                    IsHdrEnabled: true,
                    SelectedFrameRate: preferredFrameRate)));
        }

        var sameFpsCandidates = options
            .Where(option =>
                option.IsEnabled &&
                ResolutionSupportsFrameRate(resolutionToFormats, option.Value, preferredFrameRate, hdrOnly: true))
            .ToList();

        // Prefer an HDR-capable mode in the same friendly FPS bucket before considering
        // any other HDR mode. SelectNearestResolution intentionally favors the nearest
        // lower resolution, so 4K120 SDR retargets to 1080p120 HDR before falling to 4K60.
        var selected = SelectNearestResolution(previousSelection, sameFpsCandidates)
            ?? SelectNearestResolution(previousSelection, options.Where(option => option.IsEnabled).ToList())
            ?? options.FirstOrDefault(option => option.IsEnabled)
            ?? options.FirstOrDefault();

        string? hint = null;
        if (!string.IsNullOrWhiteSpace(previousSelection) &&
            !string.Equals(previousSelection, selected?.Value, StringComparison.OrdinalIgnoreCase))
        {
            var previousMax = GetMaxFrameRateForResolution(resolutionToFormats, previousSelection, hdrOnly: true);
            if (previousMax > 0)
            {
                hint = $"HDR at {previousSelection} supported up to {FormatFriendlyFrameRate(previousMax)} fps.";
            }
        }

        hint ??= BuildHdrSupportHint(new HdrSupportHintRequest(
            resolutionToFormats,
            selected?.Value,
            IsHdrEnabled: true,
            SelectedFrameRate: preferredFrameRate));
        return new HdrResolutionSelection(selected, hint);
    }

    private static double GetMaxFrameRateForResolution(
        IReadOnlyDictionary<string, List<MediaFormat>> resolutionToFormats,
        string? resolutionKey,
        bool hdrOnly)
    {
        if (string.IsNullOrWhiteSpace(resolutionKey) ||
            !resolutionToFormats.TryGetValue(resolutionKey, out var formats))
        {
            return 0;
        }

        var candidates = hdrOnly
            ? formats.Where(CaptureModeOptionsBuilder.IsHdrModeCandidate).ToList()
            : formats;
        if (candidates.Count == 0)
        {
            return 0;
        }

        return candidates.Max(format => format.FrameRateExact);
    }

    private static string FormatFriendlyFrameRate(double frameRate)
        => $"{Math.Round(frameRate):0}";
}
