using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation mutators for recording, encoder, and output settings.
/// </summary>
public partial class MainViewModel
{
    public async Task SetRecordingFormatAsync(string format, CancellationToken cancellationToken = default)
    {
        var recordingFormat = await InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailableRecordingFormats.FirstOrDefault(value =>
                string.Equals(value, format, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Recording format '{format}' is not available.");
            }
            if (IsHdrEnabled && !RecordingFormatSelectionPolicy.IsHdrCompatible(matched))
            {
                throw new InvalidOperationException("HDR recording requires HEVC or AV1 (10-bit).");
            }

            _suppressFlashbackFormatCycle = true;
            try
            {
                SelectedRecordingFormat = matched;
            }
            finally
            {
                _suppressFlashbackFormatCycle = false;
            }

            return matched switch
            {
                "HEVC" => RecordingFormat.HevcMp4,
                "AV1" => RecordingFormat.Av1Mp4,
                _ => RecordingFormat.H264Mp4
            };
        }, cancellationToken).ConfigureAwait(false);

        await _sessionCoordinator.UpdateRecordingFormatAsync(recordingFormat, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SetQualityAsync(string quality, CancellationToken cancellationToken = default)
    {
        var settings = await InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailableQualities.FirstOrDefault(value =>
                string.Equals(value, quality, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Quality '{quality}' is not available.");
            }

            _suppressFlashbackEncoderSettingsCycle = true;
            try
            {
                SelectedQuality = matched;
            }
            finally
            {
                _suppressFlashbackEncoderSettingsCycle = false;
            }

            return (Quality: ParseVideoQuality(SelectedQuality), Bitrate: CustomBitrateMbps, Preset: SelectedPreset);
        }, cancellationToken).ConfigureAwait(false);

        await _sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
                quality: settings.Quality,
                customBitrateMbps: settings.Bitrate,
                nvencPreset: settings.Preset,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SetSplitEncodeModeAsync(string splitEncodeMode, CancellationToken cancellationToken = default)
    {
        var settings = await InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailableSplitEncodeModes.FirstOrDefault(value =>
                string.Equals(value, splitEncodeMode, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Split encode mode '{splitEncodeMode}' is not available.");
            }

            _suppressFlashbackEncoderSettingsCycle = true;
            try
            {
                SelectedSplitEncodeMode = matched;
            }
            finally
            {
                _suppressFlashbackEncoderSettingsCycle = false;
            }

            return (Quality: ParseVideoQuality(SelectedQuality), Bitrate: CustomBitrateMbps, Preset: SelectedPreset, SplitEncodeMode: SelectedSplitEncodeMode);
        }, cancellationToken).ConfigureAwait(false);

        await _sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
                quality: settings.Quality,
                customBitrateMbps: settings.Bitrate,
                nvencPreset: settings.Preset,
                splitEncodeMode: settings.SplitEncodeMode,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SetCustomBitrateAsync(double bitrateMbps, CancellationToken cancellationToken = default)
    {
        var settings = await InvokeOnUiThreadAsync(() =>
        {
            _suppressFlashbackEncoderSettingsCycle = true;
            try
            {
                CustomBitrateMbps = Math.Clamp(bitrateMbps, 1, 300);
            }
            finally
            {
                _suppressFlashbackEncoderSettingsCycle = false;
            }

            return (Quality: ParseVideoQuality(SelectedQuality), Bitrate: CustomBitrateMbps, Preset: SelectedPreset);
        }, cancellationToken).ConfigureAwait(false);

        await _sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
                quality: settings.Quality,
                customBitrateMbps: settings.Bitrate,
                nvencPreset: settings.Preset,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SetPresetAsync(string preset, CancellationToken cancellationToken = default)
    {
        var settings = await InvokeOnUiThreadAsync(() =>
        {
            var matched = AvailablePresets.FirstOrDefault(value =>
                string.Equals(value, preset, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Preset '{preset}' is not available.");
            }

            _suppressFlashbackEncoderSettingsCycle = true;
            try
            {
                SelectedPreset = matched;
            }
            finally
            {
                _suppressFlashbackEncoderSettingsCycle = false;
            }

            return (Quality: ParseVideoQuality(SelectedQuality), Bitrate: CustomBitrateMbps, Preset: SelectedPreset);
        }, cancellationToken).ConfigureAwait(false);

        await _sessionCoordinator.CycleFlashbackEncoderSettingsAsync(
                quality: settings.Quality,
                customBitrateMbps: settings.Bitrate,
                nvencPreset: settings.Preset,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    public Task SetOutputPathAsync(string outputPath, CancellationToken cancellationToken = default)
    {
        return InvokeOnUiThreadAsync(() =>
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new InvalidOperationException("Output path cannot be empty.");
            }

            Directory.CreateDirectory(outputPath);
            OutputPath = outputPath;
            return Task.CompletedTask;
        }, cancellationToken);
    }
}
