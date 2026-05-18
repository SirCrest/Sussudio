using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sussudio.Services.Runtime;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    private void StartRecordingCapabilityRefresh()
        => _recordingCapabilityController.Start();

    private void RebuildRecordingFormatOptions()
        => _recordingCapabilityController.RebuildRecordingFormatOptions();

    /// <summary>
    /// Owns startup encoder/split-encode probing and observable option repair for
    /// the compatibility ViewModel facade.
    /// </summary>
    private sealed class MainViewModelRecordingCapabilityController
    {
        private readonly MainViewModel _viewModel;
        private List<string> _detectedRecordingFormats = new();

        public MainViewModelRecordingCapabilityController(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public void Start()
        {
            TrackStartupRefreshTask(RefreshRecordingFormatCapabilitiesAsync(), "recording formats");
            TrackStartupRefreshTask(RefreshSplitEncodeCapabilitiesAsync(), "split encode modes");
        }

        public void RebuildRecordingFormatOptions()
        {
            var selection = RecordingSettingsSelectionPolicy.Select(
                _detectedRecordingFormats,
                _viewModel.AvailableRecordingFormats,
                _viewModel.SelectedRecordingFormat,
                _viewModel.IsHdrEnabled,
                DefaultRecordingFormat,
                HevcRecordingFormat,
                Av1RecordingFormat);

            _viewModel.AvailableRecordingFormats.Clear();
            foreach (var format in selection.AvailableFormats)
            {
                _viewModel.AvailableRecordingFormats.Add(format);
            }

            var previousSelection = _viewModel.SelectedRecordingFormat;
            _viewModel.SelectedRecordingFormat = selection.SelectedFormat;
            if (string.Equals(previousSelection, selection.SelectedFormat, StringComparison.Ordinal))
            {
                _viewModel.OnPropertyChanged(nameof(SelectedRecordingFormat));
            }

            if (_viewModel.IsHdrEnabled &&
                !RecordingSettingsSelectionPolicy.IsHdrCompatible(_viewModel.SelectedRecordingFormat))
            {
                _viewModel.StatusText = "HDR recording requires HEVC or AV1 (10-bit).";
            }

            Logger.Log($"Selected recording format: {_viewModel.SelectedRecordingFormat}");
        }

        private static void TrackStartupRefreshTask(Task task, string description)
        {
            _ = task.ContinueWith(
                t => Logger.Log($"Startup {description} refresh failed: {t.Exception!.InnerException?.Message}"),
                TaskContinuationOptions.OnlyOnFaulted);
        }

        private async Task RefreshRecordingFormatCapabilitiesAsync()
        {
            var support = await FfmpegRuntimeLocator.GetEncoderSupportAsync();
            var formats = new List<string>();

            if (support.HasH264Nvenc)
            {
                formats.Add("H.264");
            }

            if (support.HasHevcNvenc)
            {
                formats.Add("HEVC");
            }

            if (support.HasAv1Nvenc)
            {
                formats.Add("AV1");
            }

            void ApplyFormats()
            {
                _detectedRecordingFormats = formats
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                _viewModel.IsFfmpegMissing = _detectedRecordingFormats.Count == 0;
                if (_viewModel.IsFfmpegMissing)
                {
                    Logger.Log("FFMPEG_MISSING: encoder probe returned zero codecs. Recording unavailable.");
                }

                RebuildRecordingFormatOptions();
                Logger.Log($"Recording formats refreshed: {string.Join(", ", _detectedRecordingFormats)}");
            }

            if (_viewModel._dispatcherQueue.HasThreadAccess)
            {
                ApplyFormats();
            }
            else
            {
                if (!_viewModel._dispatcherQueue.TryEnqueue(ApplyFormats))
                {
                    Logger.Log($"RECORDING_FORMATS_UI_ENQUEUE_FAILED formats={formats.Count}");
                }
            }
        }

        private async Task RefreshSplitEncodeCapabilitiesAsync()
        {
            var modes = new List<string> { "Auto", "Disabled", "2-way", "3-way" };
            var support = await FfmpegRuntimeLocator.GetSplitEncodeSupportAsync();
            if (!support.Supports2Way)
            {
                modes.Remove("2-way");
            }

            if (!support.Supports3Way)
            {
                modes.Remove("3-way");
            }

            void ApplyModes()
            {
                _viewModel.AvailableSplitEncodeModes.Clear();
                foreach (var mode in modes)
                {
                    _viewModel.AvailableSplitEncodeModes.Add(mode);
                }

                if (!_viewModel.AvailableSplitEncodeModes.Contains(_viewModel.SelectedSplitEncodeMode))
                {
                    _viewModel.SelectedSplitEncodeMode = "Auto";
                }

                Logger.Log($"Split encode modes refreshed: {string.Join(", ", _viewModel.AvailableSplitEncodeModes)}");
            }

            if (_viewModel._dispatcherQueue.HasThreadAccess)
            {
                ApplyModes();
            }
            else
            {
                if (!_viewModel._dispatcherQueue.TryEnqueue(ApplyModes))
                {
                    Logger.Log($"SPLIT_ENCODE_MODES_UI_ENQUEUE_FAILED modes={modes.Count}");
                }
            }
        }
    }
}
