using System;
using System.Collections.Generic;
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
        private readonly MainViewModel _viewModel;

        public MainViewModelCaptureModeOptionRebuildController(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public void UpdateSelectedFormat()
        {
            if (!_viewModel.TryGetEffectiveResolutionSelection(out var resolutionKey, out var width, out var height))
            {
                _viewModel.SelectedFormat = null;
                return;
            }

            _viewModel.SelectedFormat = CaptureFormatSelectionPolicy.Select(
                BuildCaptureFormatSelectionRequest(resolutionKey, width, height));
        }

        public void RebuildVideoFormatOptions()
        {
            // Source-reader pixel formats are not global device capabilities. A card can expose
            // MJPG at 4K120 SDR while exposing only P010 at the HDR retarget mode, so keep this
            // list scoped to the currently selected resolution+fps tuple.
            var formats = GetFormatsForSelectedModeTuple();
            var nextFormats = CaptureModeOptionsBuilder.BuildVideoFormatOptions(formats);

            _viewModel.AvailableVideoFormats.Clear();
            foreach (var format in nextFormats)
            {
                _viewModel.AvailableVideoFormats.Add(format);
            }

            if (!_viewModel.AvailableVideoFormats.Any(format => string.Equals(format, _viewModel.SelectedVideoFormat, StringComparison.OrdinalIgnoreCase)))
            {
                var previousSuppress = _viewModel._suppressFormatChangeReinitialize;
                _viewModel._suppressFormatChangeReinitialize = true;
                try
                {
                    _viewModel.SelectedVideoFormat = "Auto";
                }
                finally
                {
                    _viewModel._suppressFormatChangeReinitialize = previousSuppress;
                }
            }
        }

        private List<MediaFormat> GetFormatsForSelectedModeTuple()
        {
            if (!_viewModel.TryGetEffectiveResolutionSelection(out var resolutionKey, out var width, out var height))
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
                _viewModel.AvailableFormats,
                _viewModel.AvailableFrameRates,
                width,
                height,
                _viewModel.SelectedFrameRate,
                _viewModel.SelectedVideoFormat,
                _viewModel.IsHdrEnabled,
                _viewModel.ResolvePreferredTimingFamily(resolutionKey, _viewModel.SelectedFrameRate));

    }
}
