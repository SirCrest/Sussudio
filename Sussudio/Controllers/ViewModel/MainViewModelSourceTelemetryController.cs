using System;
using Sussudio.Models;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio.Controllers;

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

    private Models.SourceTelemetryAvailability _lastAppliedTelemetryAvailability = Models.SourceTelemetryAvailability.Unknown;
    private Models.SourceTelemetryConfidence _lastAppliedTelemetryConfidence = Models.SourceTelemetryConfidence.Unknown;
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
