using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns automation-driven recording setting mutations and their coordinator side effects.
    /// </summary>
    private sealed class MainViewModelRecordingSettingsAutomationController
    {
        private readonly MainViewModelRecordingSettingsAutomationControllerContext _context;

        public MainViewModelRecordingSettingsAutomationController(MainViewModelRecordingSettingsAutomationControllerContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)
        {
            var recordingFormat = await _context.InvokeRecordingFormatOnUiThreadAsync(() =>
            {
                var matched = _context.GetAvailableRecordingFormats().FirstOrDefault(value =>
                    string.Equals(value, format, StringComparison.OrdinalIgnoreCase));
                if (matched == null)
                {
                    throw new InvalidOperationException($"Recording format '{format}' is not available.");
                }
                if (_context.IsHdrEnabled() && !RecordingSettingsSelectionPolicy.IsHdrCompatible(matched))
                {
                    throw new InvalidOperationException("HDR recording requires HEVC or AV1 (10-bit).");
                }

                _context.SetSuppressFlashbackFormatCycle(true);
                try
                {
                    _context.SetSelectedRecordingFormat(matched);
                }
                finally
                {
                    _context.SetSuppressFlashbackFormatCycle(false);
                }

                return RecordingSettingsSelectionPolicy.ParseRecordingFormat(matched);
            }, cancellationToken).ConfigureAwait(false);

            await _context.UpdateRecordingFormatAsync(recordingFormat, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task SetQualityAsync(string quality, CancellationToken cancellationToken = default)
        {
            var settings = await _context.InvokeEncoderSettingsOnUiThreadAsync(() =>
            {
                var matched = _context.GetAvailableQualities().FirstOrDefault(value =>
                    string.Equals(value, quality, StringComparison.OrdinalIgnoreCase));
                if (matched == null)
                {
                    throw new InvalidOperationException($"Quality '{quality}' is not available.");
                }

                _context.SetSuppressFlashbackEncoderSettingsCycle(true);
                try
                {
                    _context.SetSelectedQuality(matched);
                }
                finally
                {
                    _context.SetSuppressFlashbackEncoderSettingsCycle(false);
                }

                return BuildEncoderSettings();
            }, cancellationToken).ConfigureAwait(false);

            await _context.CycleFlashbackEncoderSettingsAsync(
                    settings.Quality,
                    settings.Bitrate,
                    settings.Preset,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task SetSplitEncodeModeAsync(string splitEncodeMode, CancellationToken cancellationToken = default)
        {
            var settings = await _context.InvokeEncoderSettingsOnUiThreadAsync(() =>
            {
                var matched = _context.GetAvailableSplitEncodeModes().FirstOrDefault(value =>
                    string.Equals(value, splitEncodeMode, StringComparison.OrdinalIgnoreCase));
                if (matched == null)
                {
                    throw new InvalidOperationException($"Split encode mode '{splitEncodeMode}' is not available.");
                }

                _context.SetSuppressFlashbackEncoderSettingsCycle(true);
                try
                {
                    _context.SetSelectedSplitEncodeMode(matched);
                }
                finally
                {
                    _context.SetSuppressFlashbackEncoderSettingsCycle(false);
                }

                return BuildEncoderSettings(splitEncodeMode: _context.GetSelectedSplitEncodeMode());
            }, cancellationToken).ConfigureAwait(false);

            await _context.CycleFlashbackEncoderSettingsAsync(
                    settings.Quality,
                    settings.Bitrate,
                    settings.Preset,
                    settings.SplitEncodeMode,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task SetCustomBitrateAsync(double bitrateMbps, CancellationToken cancellationToken = default)
        {
            var settings = await _context.InvokeEncoderSettingsOnUiThreadAsync(() =>
            {
                _context.SetSuppressFlashbackEncoderSettingsCycle(true);
                try
                {
                    _context.SetCustomBitrateMbps(RecordingSettingsSelectionPolicy.ClampCustomBitrateMbps(bitrateMbps));
                }
                finally
                {
                    _context.SetSuppressFlashbackEncoderSettingsCycle(false);
                }

                return BuildEncoderSettings();
            }, cancellationToken).ConfigureAwait(false);

            await _context.CycleFlashbackEncoderSettingsAsync(
                    settings.Quality,
                    settings.Bitrate,
                    settings.Preset,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task SetPresetAsync(string preset, CancellationToken cancellationToken = default)
        {
            var settings = await _context.InvokeEncoderSettingsOnUiThreadAsync(() =>
            {
                var matched = _context.GetAvailablePresets().FirstOrDefault(value =>
                    string.Equals(value, preset, StringComparison.OrdinalIgnoreCase));
                if (matched == null)
                {
                    throw new InvalidOperationException($"Preset '{preset}' is not available.");
                }

                _context.SetSuppressFlashbackEncoderSettingsCycle(true);
                try
                {
                    _context.SetSelectedPreset(matched);
                }
                finally
                {
                    _context.SetSuppressFlashbackEncoderSettingsCycle(false);
                }

                return BuildEncoderSettings();
            }, cancellationToken).ConfigureAwait(false);

            await _context.CycleFlashbackEncoderSettingsAsync(
                    settings.Quality,
                    settings.Bitrate,
                    settings.Preset,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public Task SetOutputPathAsync(string outputPath, CancellationToken cancellationToken = default)
        {
            return _context.InvokeOnUiThreadAsync(() =>
            {
                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    throw new InvalidOperationException("Output path cannot be empty.");
                }

                Directory.CreateDirectory(outputPath);
                _context.SetOutputPath(outputPath);
                return Task.CompletedTask;
            }, cancellationToken);
        }

        private MainViewModelRecordingEncoderSettings BuildEncoderSettings(string? splitEncodeMode = null)
            => new(
                RecordingSettingsSelectionPolicy.ParseVideoQuality(_context.GetSelectedQuality()),
                _context.GetCustomBitrateMbps(),
                _context.GetSelectedPreset(),
                splitEncodeMode);
    }

    private sealed record MainViewModelRecordingEncoderSettings(
        VideoQuality Quality,
        double Bitrate,
        string Preset,
        string? SplitEncodeMode = null);
}
