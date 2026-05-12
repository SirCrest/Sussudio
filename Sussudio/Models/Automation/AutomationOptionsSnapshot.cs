using System;

namespace Sussudio.Models;

public sealed record MjpegDecoderAutomationSnapshot(
    int WorkerIndex,
    int SampleCount,
    double AvgMs,
    double P95Ms,
    double MaxMs);

public sealed class PreviewSlowFrameDiagnostic
{
    public long PreviewPresentId { get; init; }
    public long SourceSequenceNumber { get; init; }
    public long QpcTimestamp { get; init; }
    public long UtcUnixMs { get; init; }
    public double PresentIntervalMs { get; init; }
    public double InputUploadCpuMs { get; init; }
    public double RenderSubmitCpuMs { get; init; }
    public double PresentCallMs { get; init; }
    public double TotalFrameCpuMs { get; init; }
    public double SchedulerToPresentMs { get; init; }
    public double PipelineLatencyMs { get; init; }
    public double ExpectedIntervalMs { get; init; }
    public double DiagnosticThresholdMs { get; init; }
    public double WorstOverBudgetMs { get; init; }
    public string SlowReason { get; init; } = string.Empty;
    public int PendingFrameCount { get; init; }
    public long DxgiPresentDelta { get; init; }
    public long DxgiPresentRefreshDelta { get; init; }
    public long DxgiSyncRefreshDelta { get; init; }
    public long DxgiMissedRefreshCount { get; init; }
}

public sealed class AutomationDeviceOption
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsSelected { get; init; }
}

public sealed class AutomationStringOption
{
    public string Value { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public bool IsEnabled { get; init; } = true;
    public string DisableReason { get; init; } = string.Empty;
    public bool IsSelected { get; init; }
}

public sealed class AutomationResolutionOption
{
    public string Value { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
    public bool IsEnabled { get; init; } = true;
    public string DisableReason { get; init; } = string.Empty;
    public bool IsSelected { get; init; }
}

public sealed class AutomationFrameRateOption
{
    public double Value { get; init; }
    public double FriendlyValue { get; init; }
    public string ExactValueArg { get; init; } = string.Empty;
    public bool IsEnabled { get; init; } = true;
    public string DisableReason { get; init; } = string.Empty;
    public bool IsSelected { get; init; }
}

public sealed class AutomationIntOption
{
    public int Value { get; init; }
    public bool IsEnabled { get; init; } = true;
    public bool IsSelected { get; init; }
}

public sealed class AutomationOptionsSnapshot
{
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public AutomationDeviceOption[] Devices { get; init; } = Array.Empty<AutomationDeviceOption>();
    public AutomationDeviceOption[] AudioInputDevices { get; init; } = Array.Empty<AutomationDeviceOption>();
    public AutomationResolutionOption[] Resolutions { get; init; } = Array.Empty<AutomationResolutionOption>();
    public AutomationFrameRateOption[] FrameRates { get; init; } = Array.Empty<AutomationFrameRateOption>();
    public AutomationStringOption[] RecordingFormats { get; init; } = Array.Empty<AutomationStringOption>();
    public AutomationStringOption[] Qualities { get; init; } = Array.Empty<AutomationStringOption>();
    public AutomationStringOption[] Presets { get; init; } = Array.Empty<AutomationStringOption>();
    public AutomationStringOption[] SplitEncodeModes { get; init; } = Array.Empty<AutomationStringOption>();
    public AutomationStringOption[] VideoFormats { get; init; } = Array.Empty<AutomationStringOption>();
    public AutomationIntOption[] MjpegDecoderCounts { get; init; } = Array.Empty<AutomationIntOption>();
    public string? SelectedDeviceId { get; init; }
    public string? SelectedAudioInputDeviceId { get; init; }
    public string? SelectedResolution { get; init; }
    public double SelectedFrameRate { get; init; }
    public string SelectedRecordingFormat { get; init; } = string.Empty;
    public string SelectedQuality { get; init; } = string.Empty;
    public string SelectedPreset { get; init; } = string.Empty;
    public string SelectedSplitEncodeMode { get; init; } = string.Empty;
    public string SelectedVideoFormat { get; init; } = string.Empty;
    public int MjpegDecoderCount { get; init; }
    public bool ShowAllCaptureOptions { get; init; }
    public double PreviewVolumePercent { get; init; }
    public bool IsStatsVisible { get; init; }
}
