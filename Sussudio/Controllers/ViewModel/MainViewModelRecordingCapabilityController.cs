using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sussudio.Services.Runtime;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

/// <summary>
/// Owns startup encoder/split-encode probing and observable option repair for
/// the MainViewModel compatibility facade.
/// </summary>
internal sealed class MainViewModelRecordingCapabilityController
{
    private readonly MainViewModelRecordingCapabilityControllerContext _context;
    private List<string> _detectedRecordingFormats = new();

    public MainViewModelRecordingCapabilityController(MainViewModelRecordingCapabilityControllerContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
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
            _context.GetAvailableRecordingFormats(),
            _context.GetSelectedRecordingFormat(),
            _context.IsHdrEnabled(),
            _context.DefaultRecordingFormat,
            _context.HevcRecordingFormat,
            _context.Av1RecordingFormat);

        _context.ReplaceAvailableRecordingFormats(selection.AvailableFormats);

        var previousSelection = _context.GetSelectedRecordingFormat();
        _context.SetSelectedRecordingFormat(selection.SelectedFormat);
        if (string.Equals(previousSelection, selection.SelectedFormat, StringComparison.Ordinal))
        {
            _context.NotifySelectedRecordingFormatChanged();
        }

        if (_context.IsHdrEnabled() &&
            !RecordingSettingsSelectionPolicy.IsHdrCompatible(_context.GetSelectedRecordingFormat()))
        {
            _context.SetStatusText("HDR recording requires HEVC or AV1 (10-bit).");
        }

        Logger.Log($"Selected recording format: {_context.GetSelectedRecordingFormat()}");
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
            _context.SetIsFfmpegMissing(_detectedRecordingFormats.Count == 0);
            if (_context.IsFfmpegMissing())
            {
                Logger.Log("FFMPEG_MISSING: encoder probe returned zero codecs. Recording unavailable.");
            }

            RebuildRecordingFormatOptions();
            Logger.Log($"Recording formats refreshed: {string.Join(", ", _detectedRecordingFormats)}");
        }

        if (_context.HasUiThreadAccess())
        {
            ApplyFormats();
        }
        else
        {
            if (!_context.TryEnqueueOnUiThread(ApplyFormats))
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
            _context.ReplaceAvailableSplitEncodeModes(modes);

            if (!_context.AvailableSplitEncodeModesContains(_context.GetSelectedSplitEncodeMode()))
            {
                _context.SetSelectedSplitEncodeMode("Auto");
            }

            Logger.Log($"Split encode modes refreshed: {string.Join(", ", _context.GetAvailableSplitEncodeModes())}");
        }

        if (_context.HasUiThreadAccess())
        {
            ApplyModes();
        }
        else
        {
            if (!_context.TryEnqueueOnUiThread(ApplyModes))
            {
                Logger.Log($"SPLIT_ENCODE_MODES_UI_ENQUEUE_FAILED modes={modes.Count}");
            }
        }
    }
}
