using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns automation-driven recording setting mutations and their coordinator side effects.
    /// </summary>
    private sealed class MainViewModelRecordingSettingsAutomationController
    {
        private readonly MainViewModel _viewModel;

        public MainViewModelRecordingSettingsAutomationController(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public async Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)
        {
            var recordingFormat = await _viewModel.InvokeOnUiThreadAsync(() =>
            {
                var matched = _viewModel.AvailableRecordingFormats.FirstOrDefault(value =>
                    string.Equals(value, format, StringComparison.OrdinalIgnoreCase));
                if (matched == null)
                {
                    throw new InvalidOperationException($"Recording format '{format}' is not available.");
                }
                if (_viewModel.IsHdrEnabled && !RecordingSettingsSelectionPolicy.IsHdrCompatible(matched))
                {
                    throw new InvalidOperationException("HDR recording requires HEVC or AV1 (10-bit).");
                }

                _viewModel._suppressFlashbackFormatCycle = true;
                try
                {
                    _viewModel.SelectedRecordingFormat = matched;
                }
                finally
                {
                    _viewModel._suppressFlashbackFormatCycle = false;
                }

                return RecordingSettingsSelectionPolicy.ParseRecordingFormat(matched);
            }, cancellationToken).ConfigureAwait(false);

            await _viewModel._sessionCoordinator.UpdateRecordingFormatAsync(recordingFormat, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task SetQualityAsync(string quality, CancellationToken cancellationToken = default)
        {
            var settings = await _viewModel.InvokeOnUiThreadAsync(() =>
            {
                var matched = _viewModel.AvailableQualities.FirstOrDefault(value =>
                    string.Equals(value, quality, StringComparison.OrdinalIgnoreCase));
                if (matched == null)
                {
                    throw new InvalidOperationException($"Quality '{quality}' is not available.");
                }

                _viewModel._suppressFlashbackEncoderSettingsCycle = true;
                try
                {
                    _viewModel.SelectedQuality = matched;
                }
                finally
                {
                    _viewModel._suppressFlashbackEncoderSettingsCycle = false;
                }

                return (Quality: RecordingSettingsSelectionPolicy.ParseVideoQuality(_viewModel.SelectedQuality), Bitrate: _viewModel.CustomBitrateMbps, Preset: _viewModel.SelectedPreset);
            }, cancellationToken).ConfigureAwait(false);

            await _viewModel._sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
                    quality: settings.Quality,
                    customBitrateMbps: settings.Bitrate,
                    nvencPreset: settings.Preset,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task SetSplitEncodeModeAsync(string splitEncodeMode, CancellationToken cancellationToken = default)
        {
            var settings = await _viewModel.InvokeOnUiThreadAsync(() =>
            {
                var matched = _viewModel.AvailableSplitEncodeModes.FirstOrDefault(value =>
                    string.Equals(value, splitEncodeMode, StringComparison.OrdinalIgnoreCase));
                if (matched == null)
                {
                    throw new InvalidOperationException($"Split encode mode '{splitEncodeMode}' is not available.");
                }

                _viewModel._suppressFlashbackEncoderSettingsCycle = true;
                try
                {
                    _viewModel.SelectedSplitEncodeMode = matched;
                }
                finally
                {
                    _viewModel._suppressFlashbackEncoderSettingsCycle = false;
                }

                return (Quality: RecordingSettingsSelectionPolicy.ParseVideoQuality(_viewModel.SelectedQuality), Bitrate: _viewModel.CustomBitrateMbps, Preset: _viewModel.SelectedPreset, SplitEncodeMode: _viewModel.SelectedSplitEncodeMode);
            }, cancellationToken).ConfigureAwait(false);

            await _viewModel._sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
                    quality: settings.Quality,
                    customBitrateMbps: settings.Bitrate,
                    nvencPreset: settings.Preset,
                    splitEncodeMode: settings.SplitEncodeMode,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task SetCustomBitrateAsync(double bitrateMbps, CancellationToken cancellationToken = default)
        {
            var settings = await _viewModel.InvokeOnUiThreadAsync(() =>
            {
                _viewModel._suppressFlashbackEncoderSettingsCycle = true;
                try
                {
                    _viewModel.CustomBitrateMbps = RecordingSettingsSelectionPolicy.ClampCustomBitrateMbps(bitrateMbps);
                }
                finally
                {
                    _viewModel._suppressFlashbackEncoderSettingsCycle = false;
                }

                return (Quality: RecordingSettingsSelectionPolicy.ParseVideoQuality(_viewModel.SelectedQuality), Bitrate: _viewModel.CustomBitrateMbps, Preset: _viewModel.SelectedPreset);
            }, cancellationToken).ConfigureAwait(false);

            await _viewModel._sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
                    quality: settings.Quality,
                    customBitrateMbps: settings.Bitrate,
                    nvencPreset: settings.Preset,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task SetPresetAsync(string preset, CancellationToken cancellationToken = default)
        {
            var settings = await _viewModel.InvokeOnUiThreadAsync(() =>
            {
                var matched = _viewModel.AvailablePresets.FirstOrDefault(value =>
                    string.Equals(value, preset, StringComparison.OrdinalIgnoreCase));
                if (matched == null)
                {
                    throw new InvalidOperationException($"Preset '{preset}' is not available.");
                }

                _viewModel._suppressFlashbackEncoderSettingsCycle = true;
                try
                {
                    _viewModel.SelectedPreset = matched;
                }
                finally
                {
                    _viewModel._suppressFlashbackEncoderSettingsCycle = false;
                }

                return (Quality: RecordingSettingsSelectionPolicy.ParseVideoQuality(_viewModel.SelectedQuality), Bitrate: _viewModel.CustomBitrateMbps, Preset: _viewModel.SelectedPreset);
            }, cancellationToken).ConfigureAwait(false);

            await _viewModel._sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
                    quality: settings.Quality,
                    customBitrateMbps: settings.Bitrate,
                    nvencPreset: settings.Preset,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }

        public Task SetOutputPathAsync(string outputPath, CancellationToken cancellationToken = default)
        {
            return _viewModel.InvokeOnUiThreadAsync(() =>
            {
                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    throw new InvalidOperationException("Output path cannot be empty.");
                }

                Directory.CreateDirectory(outputPath);
                _viewModel.OutputPath = outputPath;
                return Task.CompletedTask;
            }, cancellationToken);
        }
    }
}
