using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

/// <summary>
/// Graph-built ports consumed by the recording capability refresh controller.
/// </summary>
internal sealed class MainViewModelRecordingCapabilityControllerContext
{
    public required string DefaultRecordingFormat { get; init; }
    public required string HevcRecordingFormat { get; init; }
    public required string Av1RecordingFormat { get; init; }
    public required Func<IReadOnlyCollection<string>> GetAvailableRecordingFormats { get; init; }
    public required Action<IReadOnlyList<string>> ReplaceAvailableRecordingFormats { get; init; }
    public required Func<string> GetSelectedRecordingFormat { get; init; }
    public required Action<string> SetSelectedRecordingFormat { get; init; }
    public required Action NotifySelectedRecordingFormatChanged { get; init; }
    public required Func<bool> IsHdrEnabled { get; init; }
    public required Action<string> SetStatusText { get; init; }
    public required Func<bool> IsFfmpegMissing { get; init; }
    public required Action<bool> SetIsFfmpegMissing { get; init; }
    public required Func<bool> HasUiThreadAccess { get; init; }
    public required Func<Action, bool> TryEnqueueOnUiThread { get; init; }
    public required Func<IReadOnlyCollection<string>> GetAvailableSplitEncodeModes { get; init; }
    public required Action<IReadOnlyList<string>> ReplaceAvailableSplitEncodeModes { get; init; }
    public required Func<string> GetSelectedSplitEncodeMode { get; init; }
    public required Action<string> SetSelectedSplitEncodeMode { get; init; }
    public required Func<string, bool> AvailableSplitEncodeModesContains { get; init; }
}

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

internal sealed class MainViewModelSourceTelemetryControllerContext
{
    public required Func<Action, bool> TryEnqueueOnUiThread { get; init; }
    public required Func<SourceSignalTelemetrySnapshot> GetLatestSourceTelemetry { get; init; }
    public required Action<SourceSignalTelemetrySnapshot> SetLatestSourceTelemetry { get; init; }
    public required Func<SourceSignalTelemetrySnapshot, DateTimeOffset, string> BuildSourceTelemetrySummary { get; init; }
    public required Action<int?> SetSourceWidth { get; init; }
    public required Action<int?> SetSourceHeight { get; init; }
    public required Action<bool?> SetSourceIsHdr { get; init; }
    public required Func<bool> IsRecording { get; init; }
    public required Func<bool> IsHdrEnabled { get; init; }
    public required Action<bool> SetIsHdrEnabled { get; init; }
    public required Action<string> SetSourceTelemetryAvailability { get; init; }
    public required Action<string> SetSourceTelemetryOriginDetail { get; init; }
    public required Action<string> SetSourceTelemetryConfidence { get; init; }
    public required Action<string?> SetSourceTelemetryDiagnosticSummary { get; init; }
    public required Func<DateTimeOffset?> GetSourceTelemetryTimestampUtc { get; init; }
    public required Action<DateTimeOffset?> SetSourceTelemetryTimestampUtc { get; init; }
    public required Action<double?> SetDetectedSourceFrameRate { get; init; }
    public required Action<string?> SetDetectedSourceFrameRateArg { get; init; }
    public required Action<string> SetSourceFrameRateOrigin { get; init; }
    public required Func<string> GetSourceTelemetrySummaryText { get; init; }
    public required Action<string> SetSourceTelemetrySummaryText { get; init; }
    public required Func<string?> GetLastSourceModeKey { get; init; }
    public required Action<string?> SetLastSourceModeKey { get; init; }
    public required Func<string?> GetSelectedResolution { get; init; }
    public required Func<string?, bool> IsAutoResolutionValue { get; init; }
    public required Func<bool> HasUserOverriddenResolutionForCurrentMode { get; init; }
    public required Action<bool> SetHasUserOverriddenResolutionForCurrentMode { get; init; }
    public required Func<bool> IsAutoFrameRateSelected { get; init; }
    public required Func<bool> HasUserOverriddenFrameRateForCurrentMode { get; init; }
    public required Action<bool> SetHasUserOverriddenFrameRateForCurrentMode { get; init; }
    public required Func<bool> ForceSourceAutoRetarget { get; init; }
    public required Action<bool> SetForceSourceAutoRetarget { get; init; }
    public required Func<int> AvailableResolutionCount { get; init; }
    public required Action<bool> SetPendingModeOptionsRefresh { get; init; }
    public required Action RebuildResolutionOptions { get; init; }
    public required Action UpdateTargetSummary { get; init; }
}

/// <summary>
/// Owns source telemetry ingress, UI projection, and source-aware retargeting
/// for the compatibility ViewModel facade.
/// </summary>
internal sealed class MainViewModelSourceTelemetryController
{
    private readonly MainViewModelSourceTelemetryControllerContext _context;

    private SourceTelemetryAvailability _lastAppliedTelemetryAvailability = SourceTelemetryAvailability.Unknown;
    private SourceTelemetryConfidence _lastAppliedTelemetryConfidence = SourceTelemetryConfidence.Unknown;
    private SourceTelemetryOrigin _lastAppliedFrameRateOrigin = SourceTelemetryOrigin.Unknown;
    private bool _hasAppliedTelemetryEnums;
    private int? _lastTelemetryAgeBucket;

    public MainViewModelSourceTelemetryController(MainViewModelSourceTelemetryControllerContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void RefreshSourceTelemetrySummaryAge()
    {
        var ageSeconds = TelemetryAgeHelper.ComputeAgeSeconds(_context.GetSourceTelemetryTimestampUtc(), DateTimeOffset.UtcNow);
        var ageBucket = ageSeconds.HasValue ? ageSeconds.Value / 5 : (int?)null;
        if (_lastTelemetryAgeBucket.HasValue &&
            ageBucket.HasValue &&
            _lastTelemetryAgeBucket.Value == ageBucket.GetValueOrDefault())
        {
            return;
        }

        var refreshedSummary = _context.BuildSourceTelemetrySummary(_context.GetLatestSourceTelemetry(), DateTimeOffset.UtcNow);
        if (!string.Equals(_context.GetSourceTelemetrySummaryText(), refreshedSummary, StringComparison.Ordinal))
        {
            _context.SetSourceTelemetrySummaryText(refreshedSummary);
        }

        _lastTelemetryAgeBucket = ageBucket;
    }

    public void OnSourceTelemetryUpdated(object? sender, SourceSignalTelemetrySnapshot snapshot)
    {
        if (!_context.TryEnqueueOnUiThread(() =>
        {
            ApplySourceTelemetrySnapshot(snapshot, allowAutoRetarget: true);
        }))
        {
            Logger.Log(
                $"SOURCE_TELEMETRY_UI_ENQUEUE_FAILED availability={snapshot.Availability} " +
                $"origin={snapshot.Origin} mode='{snapshot.GetModeKey()}'");
        }
    }

    public void ApplySourceTelemetrySnapshot(SourceSignalTelemetrySnapshot snapshot, bool allowAutoRetarget)
    {
        _context.SetLatestSourceTelemetry(snapshot);
        _context.SetSourceWidth(snapshot.Width);
        _context.SetSourceHeight(snapshot.Height);
        _context.SetSourceIsHdr(snapshot.IsHdr);
        if (!_context.IsRecording() && _context.IsHdrEnabled() && snapshot.IsHdr == false)
        {
            _context.SetIsHdrEnabled(false);
        }

        if (!_hasAppliedTelemetryEnums || _lastAppliedTelemetryAvailability != snapshot.Availability)
        {
            _context.SetSourceTelemetryAvailability(snapshot.Availability.ToString());
            _lastAppliedTelemetryAvailability = snapshot.Availability;
        }

        _context.SetSourceTelemetryOriginDetail(snapshot.OriginDetail);
        if (!_hasAppliedTelemetryEnums || _lastAppliedTelemetryConfidence != snapshot.Confidence)
        {
            _context.SetSourceTelemetryConfidence(snapshot.Confidence.ToString());
            _lastAppliedTelemetryConfidence = snapshot.Confidence;
        }

        _context.SetSourceTelemetryDiagnosticSummary(snapshot.DiagnosticSummary);
        _context.SetSourceTelemetryTimestampUtc(snapshot.TimestampUtc);
        _context.SetDetectedSourceFrameRate(snapshot.FrameRateExact);
        _context.SetDetectedSourceFrameRateArg(snapshot.FrameRateArg);
        if (!_hasAppliedTelemetryEnums || _lastAppliedFrameRateOrigin != snapshot.Origin)
        {
            _context.SetSourceFrameRateOrigin(snapshot.Origin != SourceTelemetryOrigin.Unknown
                ? snapshot.Origin.ToString()
                : "Unknown");
            _lastAppliedFrameRateOrigin = snapshot.Origin;
        }

        _hasAppliedTelemetryEnums = true;
        _lastTelemetryAgeBucket = null;
        _context.SetSourceTelemetrySummaryText(_context.BuildSourceTelemetrySummary(snapshot, DateTimeOffset.UtcNow));

        var modeKey = snapshot.GetModeKey();
        if (!string.IsNullOrWhiteSpace(modeKey) &&
            !string.Equals(modeKey, _context.GetLastSourceModeKey(), StringComparison.Ordinal))
        {
            if (allowAutoRetarget)
            {
                var shouldAutoRetargetResolution =
                    _context.IsAutoResolutionValue(_context.GetSelectedResolution()) ||
                    !_context.HasUserOverriddenResolutionForCurrentMode();
                var shouldAutoRetargetFrameRate =
                    _context.IsAutoFrameRateSelected() ||
                    !_context.HasUserOverriddenFrameRateForCurrentMode();
                _context.SetLastSourceModeKey(modeKey);
                _context.SetForceSourceAutoRetarget(shouldAutoRetargetResolution || shouldAutoRetargetFrameRate);
                if (shouldAutoRetargetResolution)
                {
                    _context.SetHasUserOverriddenResolutionForCurrentMode(false);
                }

                if (shouldAutoRetargetFrameRate)
                {
                    _context.SetHasUserOverriddenFrameRateForCurrentMode(false);
                }
            }
        }

        var shouldRebuildModeOptions = allowAutoRetarget &&
                                       (_context.ForceSourceAutoRetarget() ||
                                        (snapshot.HasSignalData && _context.AvailableResolutionCount() == 0));
        if (shouldRebuildModeOptions)
        {
            if (_context.IsRecording())
            {
                _context.SetPendingModeOptionsRefresh(true);
            }
            else
            {
                _context.RebuildResolutionOptions();
            }
        }
        else
        {
            _context.UpdateTargetSummary();
        }
    }
}
