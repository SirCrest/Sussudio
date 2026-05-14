using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sussudio.Services.Runtime;

namespace Sussudio.ViewModels;

/// <summary>
/// Startup FFmpeg capability probes for encoder-backed recording format and split-encode options.
/// </summary>
public partial class MainViewModel
{
    private void StartRecordingCapabilityRefresh()
    {
        TrackStartupRefreshTask(RefreshRecordingFormatCapabilitiesAsync(), "recording formats");
        TrackStartupRefreshTask(RefreshSplitEncodeCapabilitiesAsync(), "split encode modes");
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
            IsFfmpegMissing = _detectedRecordingFormats.Count == 0;
            if (IsFfmpegMissing)
            {
                Logger.Log("FFMPEG_MISSING: encoder probe returned zero codecs. Recording unavailable.");
            }
            RebuildRecordingFormatOptions();
            Logger.Log($"Recording formats refreshed: {string.Join(", ", _detectedRecordingFormats)}");
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            ApplyFormats();
        }
        else
        {
            if (!_dispatcherQueue.TryEnqueue(ApplyFormats))
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
            AvailableSplitEncodeModes.Clear();
            foreach (var mode in modes)
            {
                AvailableSplitEncodeModes.Add(mode);
            }

            if (!AvailableSplitEncodeModes.Contains(SelectedSplitEncodeMode))
            {
                SelectedSplitEncodeMode = "Auto";
            }

            Logger.Log($"Split encode modes refreshed: {string.Join(", ", AvailableSplitEncodeModes)}");
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            ApplyModes();
        }
        else
        {
            if (!_dispatcherQueue.TryEnqueue(ApplyModes))
            {
                Logger.Log($"SPLIT_ENCODE_MODES_UI_ENQUEUE_FAILED modes={modes.Count}");
            }
        }
    }
}
