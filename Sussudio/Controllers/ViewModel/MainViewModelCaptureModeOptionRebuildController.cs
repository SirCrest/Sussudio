using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns capture-mode option rebuild transactions for the
    /// compatibility ViewModel facade.
    /// </summary>
    private sealed partial class MainViewModelCaptureModeOptionRebuildController
    {
        private readonly MainViewModelCaptureModeOptionRebuildControllerContext _context;

        public MainViewModelCaptureModeOptionRebuildController(MainViewModelCaptureModeOptionRebuildControllerContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void UpdateSelectedFormat()
        {
            if (!_context.TryGetEffectiveResolutionSelection(out var resolutionKey, out var width, out var height))
            {
                _context.SetSelectedFormat(null);
                return;
            }

            _context.SetSelectedFormat(CaptureFormatSelectionPolicy.Select(
                BuildCaptureFormatSelectionRequest(resolutionKey, width, height)));
        }

        public void RebuildVideoFormatOptions()
        {
            // Source-reader pixel formats are not global device capabilities. A card can expose
            // MJPG at 4K120 SDR while exposing only P010 at the HDR retarget mode, so keep this
            // list scoped to the currently selected resolution+fps tuple.
            var formats = GetFormatsForSelectedModeTuple();
            var nextFormats = CaptureModeOptionsBuilder.BuildVideoFormatOptions(formats);

            _context.AvailableVideoFormats.Clear();
            foreach (var format in nextFormats)
            {
                _context.AvailableVideoFormats.Add(format);
            }

            if (!_context.AvailableVideoFormats.Any(format => string.Equals(format, _context.GetSelectedVideoFormat(), StringComparison.OrdinalIgnoreCase)))
            {
                var previousSuppress = _context.IsSuppressFormatChangeReinitialize();
                _context.SetSuppressFormatChangeReinitialize(true);
                try
                {
                    _context.SetSelectedVideoFormat("Auto");
                }
                finally
                {
                    _context.SetSuppressFormatChangeReinitialize(previousSuppress);
                }
            }
        }

        private List<MediaFormat> GetFormatsForSelectedModeTuple()
        {
            if (!_context.TryGetEffectiveResolutionSelection(out var resolutionKey, out var width, out var height))
            {
                return new List<MediaFormat>();
            }

            return CaptureFormatSelectionPolicy
                .SelectModeTupleFormats(BuildCaptureFormatSelectionRequest(resolutionKey, width, height))
                .ToList();
        }

        private CaptureFormatSelectionRequest BuildCaptureFormatSelectionRequest(
            string resolutionKey,
            uint width,
            uint height)
            => new(
                _context.AvailableFormats,
                _context.AvailableFrameRates,
                width,
                height,
                _context.GetSelectedFrameRate(),
                _context.GetSelectedVideoFormat(),
                _context.IsHdrEnabled(),
                _context.ResolvePreferredTimingFamily(resolutionKey, _context.GetSelectedFrameRate()));

    }

    private sealed class MainViewModelCaptureModeOptionRebuildControllerContext
    {
        public required ObservableCollection<MediaFormat> AvailableFormats { get; init; }
        public required ObservableCollection<FrameRateOption> AvailableFrameRates { get; init; }
        public required ObservableCollection<ResolutionOption> AvailableResolutions { get; init; }
        public required ObservableCollection<string> AvailableVideoFormats { get; init; }
        public required Func<IReadOnlyDictionary<string, List<MediaFormat>>> GetResolutionToFormats { get; init; }
        public required Func<SourceSignalTelemetrySnapshot> GetLatestSourceTelemetry { get; init; }
        public required TryGetEffectiveResolutionSelectionDelegate TryGetEffectiveResolutionSelection { get; init; }
        public required TryResolveResolutionKeyDelegate TryResolveResolutionKey { get; init; }
        public required Func<string?, string?> GetEffectiveResolutionKey { get; init; }
        public required Func<string?, double, FrameRateTimingFamily> ResolvePreferredTimingFamily { get; init; }
        public required Func<string?, IReadOnlyList<FrameRateOption>, double, (double? Rate, string? Arg, string Origin)> ResolveDetectedSourceFrameRate { get; init; }
        public required Func<string?, IReadOnlyList<FrameRateTimingVariant>> BuildFrameRateTimingVariants { get; init; }
        public required Action<FrameRateOption?, double> ApplyResolvedFrameRateSelection { get; init; }
        public required Func<string> GetSelectedResolutionDisplayText { get; init; }
        public required Func<string?, string> BuildHdrSupportHintForResolution { get; init; }
        public required Action UpdateTargetSummary { get; init; }
        public required Action NotifySelectedResolutionChanged { get; init; }
        public required Func<CaptureDevice?> GetSelectedDevice { get; init; }
        public required Func<string?> GetSelectedResolution { get; init; }
        public required Action<string?> SetSelectedResolution { get; init; }
        public required Func<double> GetSelectedFrameRate { get; init; }
        public required Func<string> GetSelectedVideoFormat { get; init; }
        public required Action<string> SetSelectedVideoFormat { get; init; }
        public required Action<MediaFormat?> SetSelectedFormat { get; init; }
        public required Func<bool> IsHdrEnabled { get; init; }
        public required Func<bool> IsPreviewing { get; init; }
        public required Func<bool> ShowAllCaptureOptions { get; init; }
        public required Func<bool> IsAutoFrameRateSelected { get; init; }
        public required Action<bool> SetIsAutoFrameRateSelected { get; init; }
        public required Func<bool> HasUserOverriddenResolutionForCurrentMode { get; init; }
        public required Func<bool> HasUserOverriddenFrameRateForCurrentMode { get; init; }
        public required Func<bool> IsPendingSdrAutoSelectionForDeviceChange { get; init; }
        public required Action<bool> SetPendingSdrAutoSelectionForDeviceChange { get; init; }
        public required Func<int?> GetPendingSdrAutoFriendlyFrameRateBucket { get; init; }
        public required Action<int?> SetPendingSdrAutoFriendlyFrameRateBucket { get; init; }
        public required Func<bool> IsForceSourceAutoRetarget { get; init; }
        public required Action<bool> SetForceSourceAutoRetarget { get; init; }
        public required Func<string?> GetLastKnownResolutionKey { get; init; }
        public required Action<string?> SetLastKnownResolutionKey { get; init; }
        public required Action<bool> SetIsRebuildingModeOptions { get; init; }
        public required Action<bool> SetIsApplyingAutomaticResolutionSelection { get; init; }
        public required Action<bool> SetIsApplyingAutomaticFrameRateSelection { get; init; }
        public required Func<bool> IsSuppressFormatChangeReinitialize { get; init; }
        public required Action<bool> SetSuppressFormatChangeReinitialize { get; init; }
        public required Action<double?> SetDetectedSourceFrameRate { get; init; }
        public required Action<string?> SetDetectedSourceFrameRateArg { get; init; }
        public required Action<string> SetSourceFrameRateOrigin { get; init; }
        public required Action<uint?> SetAutoResolvedWidth { get; init; }
        public required Action<uint?> SetAutoResolvedHeight { get; init; }
        public required Action<double?> SetAutoResolvedFrameRate { get; init; }
        public required Action<string> SetHdrResolutionSupportHint { get; init; }
        public required Action<string> SetDisabledResolutionReason { get; init; }
        public required Action<string> SetStatusText { get; init; }

        public delegate bool TryGetEffectiveResolutionSelectionDelegate(out string resolutionKey, out uint width, out uint height);
        public delegate bool TryResolveResolutionKeyDelegate(string? resolutionValue, out string resolutionKey);
    }
}
