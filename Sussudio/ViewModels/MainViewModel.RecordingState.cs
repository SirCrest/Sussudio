using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Sussudio.Services.Runtime;

namespace Sussudio.ViewModels;

/// <summary>
/// Recording state: encoder option selections, output path, counters, and transition flags.
/// </summary>
public partial class MainViewModel
{
    private readonly Stopwatch _recordingStopwatch = new();
    private readonly BitrateSampleWindow _recordingBitrateSamples = new(BitrateWindowMs);
    private const int BitrateWindowMs = 10000;
    private const string DefaultRecordingFormat = "H.264";
    private const string HevcRecordingFormat = "HEVC";
    private const string Av1RecordingFormat = "AV1";

    public Task ToggleRecordingAsync()
        => _recordingTransitionController.ToggleRecordingAsync();

    public Task SetRecordingEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => SetRecordingDesiredStateAsync(enabled, cancellationToken);

    internal Task SetRecordingDesiredStateAsync(bool enabled, CancellationToken cancellationToken = default)
        => _recordingTransitionController.SetRecordingDesiredStateAsync(enabled, cancellationToken);

    /// <summary>
    /// Graceful-stop entry point for callers that must NOT short-circuit on the
    /// toggle CAS gate (e.g. the window-close handler). If a toggle is in flight,
    /// await it; afterwards, if still recording, initiate a fresh stop.
    /// </summary>
    public Task StopRecordingAndWaitAsync(CancellationToken cancellationToken = default)
        => _recordingTransitionController.StopRecordingAndWaitAsync(cancellationToken);

    internal Task StopRecordingForEmergencyAsync(CancellationToken cancellationToken = default)
        => _sessionCoordinator.StopRecordingForEmergencyAsync(cancellationToken);

    [ObservableProperty]
    public partial bool IsRecordingTransitioning { get; set; }

    [ObservableProperty]
    public partial bool IsFfmpegMissing { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableRecordingFormats { get; set; } =
        new() { DefaultRecordingFormat, HevcRecordingFormat, Av1RecordingFormat };

    [ObservableProperty]
    public partial string SelectedRecordingFormat { get; set; } = DefaultRecordingFormat;

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableQualities { get; set; } = new() { "Auto", "Low", "Medium", "High", "Super High", "Custom" };

    [ObservableProperty]
    public partial string SelectedQuality { get; set; } = "Medium";

    [ObservableProperty]
    public partial ObservableCollection<string> AvailablePresets { get; set; } = new()
    {
        "Auto", "P1", "P2", "P3", "P4", "P5", "P6", "P7"
    };

    [ObservableProperty]
    public partial string SelectedPreset { get; set; } = "Auto";

    [ObservableProperty]
    public partial ObservableCollection<string> AvailableSplitEncodeModes { get; set; } = new()
    {
        "Auto", "Disabled", "2-way", "3-way"
    };

    [ObservableProperty]
    public partial string SelectedSplitEncodeMode { get; set; } = "Auto";

    [ObservableProperty]
    public partial double CustomBitrateMbps { get; set; } = 50;

    [ObservableProperty]
    public partial bool IsCustomBitrateVisible { get; set; }

    [ObservableProperty]
    public partial string OutputPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);

    [ObservableProperty]
    public partial string RecordingTime { get; set; } = "00:00:00";

    [ObservableProperty]
    public partial string RecordingSizeInfo { get; set; } = "--";

    [ObservableProperty]
    public partial string RecordingBitrateInfo { get; set; } = "--";

    [ObservableProperty]
    public partial bool IsRecording { get; set; }

    private void StartRecordingCapabilityRefresh()
        => _recordingCapabilityController.Start();

    private void RebuildRecordingFormatOptions()
        => _recordingCapabilityController.RebuildRecordingFormatOptions();

    partial void OnIsRecordingChanged(bool value)
    {
        if (!value)
        {
            ResetAudioMeter();
            RecordingSizeInfo = "--";
            RecordingBitrateInfo = "--";
            _recordingBitrateSamples.Clear();

            if (_pendingModeOptionsRefresh)
            {
                _pendingModeOptionsRefresh = false;
                RebuildResolutionOptions();
            }
        }
    }

    private void UpdateRecordingStats()
    {
        var stats = _captureService.GetRecordingStats();
        var totalBytes = stats.TotalBytes;
        RecordingSizeInfo = DisplayFormatters.FormatBytes(totalBytes, "0");

        var now = Environment.TickCount64;
        var smoothed = _recordingBitrateSamples.AddSampleAndCompute(now, totalBytes);
        RecordingBitrateInfo = smoothed.HasValue ? DisplayFormatters.FormatBitrate(smoothed.Value) : "--";
    }

    private void UpdateDiskSpace()
    {
        DiskSpaceInfo = OutputDriveSpacePresentationBuilder.Build(OutputPath);
    }
}

/// <summary>
/// Owns bounded byte-sample smoothing for recording and Flashback bitrate labels.
/// </summary>
internal sealed class BitrateSampleWindow
{
    private readonly long _windowMs;
    private readonly Queue<(long Tick, long Bytes)> _samples = new();

    public BitrateSampleWindow(long windowMs)
    {
        if (windowMs <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(windowMs), "Bitrate sample window must be positive.");
        }

        _windowMs = windowMs;
    }

    public void Clear()
    {
        _samples.Clear();
    }

    public double? AddSampleAndCompute(long tick, long bytes)
    {
        _samples.Enqueue((tick, bytes));
        while (_samples.Count > 0 && tick - _samples.Peek().Tick > _windowMs)
        {
            _samples.Dequeue();
        }

        return ComputeAverageBitrate(_samples);
    }

    private static double? ComputeAverageBitrate(Queue<(long Tick, long Bytes)> samples)
    {
        if (samples.Count < 2)
        {
            return null;
        }

        var first = samples.Peek();
        var last = samples.Last();
        var deltaMs = last.Tick - first.Tick;
        if (deltaMs <= 0)
        {
            return null;
        }

        var deltaBytes = Math.Max(0, last.Bytes - first.Bytes);
        return (deltaBytes * 8.0) / (deltaMs / 1000.0);
    }
}
