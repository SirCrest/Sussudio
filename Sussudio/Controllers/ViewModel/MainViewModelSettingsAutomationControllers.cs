using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

/// <summary>
/// Graph-built ports consumed by the capture settings automation controller.
/// </summary>
internal sealed class MainViewModelCaptureSettingsAutomationControllerContext
{
    public required Func<Func<bool>, CancellationToken, Task<bool>> InvokeBooleanOnUiThreadAsync { get; init; }
    public required Func<Func<Task>, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }
    public required Func<IEnumerable<ResolutionOption>> GetAvailableResolutions { get; init; }
    public required Func<IEnumerable<FrameRateOption>> GetAvailableFrameRates { get; init; }
    public required Func<IEnumerable<string>> GetAvailableVideoFormats { get; init; }
    public required Func<string?> GetSelectedResolution { get; init; }
    public required Action<string?> SetSelectedResolution { get; init; }
    public required Action<double> SetSelectedFrameRate { get; init; }
    public required Action<string> SetSelectedVideoFormat { get; init; }
    public required Action<int> SetMjpegDecoderCount { get; init; }
    public required Action SelectAutoFrameRate { get; init; }
    public required Func<bool> IsPreviewing { get; init; }
    public required Func<bool> IsInitialized { get; init; }
    public required Func<CaptureDevice?> GetSelectedDevice { get; init; }
    public required Func<MediaFormat?> GetSelectedFormat { get; init; }
    public required Action<bool> SetSuppressFormatChangeReinitialize { get; init; }
    public required Func<string, Task> ReinitializeDeviceAsync { get; init; }
}

/// <summary>
/// Owns automation-driven capture setting mutations and active-preview reinitialization.
/// </summary>
internal sealed class MainViewModelCaptureSettingsAutomationController
{
    private readonly MainViewModelCaptureSettingsAutomationControllerContext _context;
    private readonly SemaphoreSlim _captureModeGate = new(1, 1);

    public MainViewModelCaptureSettingsAutomationController(MainViewModelCaptureSettingsAutomationControllerContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task SetResolutionAsync(string resolution, CancellationToken cancellationToken = default)
    {
        return SetAutomationCaptureModeAsync("resolution", () =>
        {
            var matched = _context.GetAvailableResolutions().FirstOrDefault(r =>
                string.Equals(r.Value, resolution, StringComparison.OrdinalIgnoreCase));
            if (matched == null)
            {
                throw new InvalidOperationException($"Resolution '{resolution}' is not available.");
            }
            if (!matched.IsEnabled)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(matched.DisableReason)
                        ? $"Resolution '{resolution}' is currently disabled."
                        : matched.DisableReason);
            }

            _context.SetSelectedResolution(matched.Value);
        }, cancellationToken);
    }

    public Task SetFrameRateAsync(double frameRate, CancellationToken cancellationToken = default)
    {
        return SetAutomationCaptureModeAsync("frame rate", () =>
        {
            var availableFrameRates = _context.GetAvailableFrameRates().ToList();
            if (availableFrameRates.Count == 0)
            {
                throw new InvalidOperationException("No frame rates are available.");
            }

            var enabledRates = availableFrameRates
                .Where(rate => rate.IsEnabled)
                .ToList();
            if (enabledRates.Count == 0)
            {
                throw new InvalidOperationException("No enabled frame rates are available for the current selection.");
            }

            if (FrameRateTimingPolicy.IsAutoFrameRateValue(frameRate))
            {
                var autoRate = enabledRates.FirstOrDefault(rate => FrameRateTimingPolicy.IsAutoFrameRateValue(rate.Value));
                if (autoRate == null)
                {
                    throw new InvalidOperationException("Auto frame rate is not available for the current selection.");
                }

                _context.SelectAutoFrameRate();
                return;
            }

            var requestedFriendly = Math.Round(frameRate);
            var friendlyMatches = enabledRates
                .Where(rate => Math.Round(rate.FriendlyValue) == requestedFriendly)
                .OrderBy(rate => Math.Abs(rate.FriendlyValue - frameRate))
                .ThenBy(rate => Math.Abs(rate.Value - frameRate))
                .ToList();

            var matched = (friendlyMatches.Count > 0 ? friendlyMatches : enabledRates)
                .OrderBy(rate => Math.Abs(rate.Value - frameRate))
                .First();

            if (friendlyMatches.Count == 0 && !FrameRateTimingPolicy.IsFrameRateMatch(matched.Value, frameRate))
            {
                throw new InvalidOperationException(
                    $"Frame rate '{frameRate:0.###}' is not available for {_context.GetSelectedResolution() ?? "the current resolution"}.");
            }

            _context.SetSelectedFrameRate(matched.Value);
        }, cancellationToken);
    }

    public Task SetVideoFormatAsync(string videoFormat, CancellationToken cancellationToken = default)
    {
        return SetAutomationCaptureModeAsync("video format", () =>
        {
            if (string.IsNullOrWhiteSpace(videoFormat))
            {
                throw new ArgumentException("Video format is required.", nameof(videoFormat));
            }

            var match = _context.GetAvailableVideoFormats().FirstOrDefault(
                format => string.Equals(format, videoFormat, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                throw new InvalidOperationException($"Video format '{videoFormat}' is not available.");
            }

            _context.SetSelectedVideoFormat(match);
        }, cancellationToken);
    }

    public Task SetMjpegDecoderCountAsync(int decoderCount, CancellationToken cancellationToken = default)
    {
        return SetAutomationCaptureModeAsync("mjpeg decoder count", () =>
        {
            _context.SetMjpegDecoderCount(Math.Clamp(decoderCount, 1, 8));
        }, cancellationToken);
    }

    private async Task SetAutomationCaptureModeAsync(
        string reason,
        Action apply,
        CancellationToken cancellationToken)
    {
        await _captureModeGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var shouldReinitialize = await _context.InvokeBooleanOnUiThreadAsync(() =>
            {
                var wasPreviewing = _context.IsPreviewing() && _context.IsInitialized() && _context.GetSelectedDevice() != null;
                _context.SetSuppressFormatChangeReinitialize(true);
                try
                {
                    apply();
                }
                finally
                {
                    _context.SetSuppressFormatChangeReinitialize(false);
                }

                return wasPreviewing && _context.GetSelectedFormat() != null;
            }, cancellationToken).ConfigureAwait(false);

            if (shouldReinitialize)
            {
                await _context.InvokeOnUiThreadAsync(
                        () => _context.ReinitializeDeviceAsync($"automation {reason}"),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            _captureModeGate.Release();
        }
    }
}

/// <summary>
/// Graph-built ports consumed by the recording settings automation controller.
/// </summary>
internal sealed class MainViewModelRecordingSettingsAutomationControllerContext
{
    public required Func<Func<RecordingFormat>, CancellationToken, Task<RecordingFormat>> InvokeRecordingFormatOnUiThreadAsync { get; init; }
    public required Func<Func<MainViewModelRecordingEncoderSettings>, CancellationToken, Task<MainViewModelRecordingEncoderSettings>> InvokeEncoderSettingsOnUiThreadAsync { get; init; }
    public required Func<Func<Task>, CancellationToken, Task> InvokeOnUiThreadAsync { get; init; }
    public required Func<IEnumerable<string>> GetAvailableRecordingFormats { get; init; }
    public required Func<IEnumerable<string>> GetAvailableQualities { get; init; }
    public required Func<IEnumerable<string>> GetAvailableSplitEncodeModes { get; init; }
    public required Func<IEnumerable<string>> GetAvailablePresets { get; init; }
    public required Func<bool> IsHdrEnabled { get; init; }
    public required Action<bool> SetSuppressFlashbackFormatCycle { get; init; }
    public required Action<bool> SetSuppressFlashbackEncoderSettingsCycle { get; init; }
    public required Action<string> SetSelectedRecordingFormat { get; init; }
    public required Func<string> GetSelectedQuality { get; init; }
    public required Action<string> SetSelectedQuality { get; init; }
    public required Func<string> GetSelectedSplitEncodeMode { get; init; }
    public required Action<string> SetSelectedSplitEncodeMode { get; init; }
    public required Func<string> GetSelectedPreset { get; init; }
    public required Action<string> SetSelectedPreset { get; init; }
    public required Func<double> GetCustomBitrateMbps { get; init; }
    public required Action<double> SetCustomBitrateMbps { get; init; }
    public required Action<string> SetOutputPath { get; init; }
    public required Func<RecordingFormat, CancellationToken, Task> UpdateRecordingFormatAsync { get; init; }
    public required Func<VideoQuality, double, string, string?, CancellationToken, Task> CycleFlashbackEncoderSettingsAsync { get; init; }
}

/// <summary>
/// Owns automation-driven recording setting mutations and their coordinator side effects.
/// </summary>
internal sealed class MainViewModelRecordingSettingsAutomationController
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

internal sealed record MainViewModelRecordingEncoderSettings(
    VideoQuality Quality,
    double Bitrate,
    string Preset,
    string? SplitEncodeMode = null);
