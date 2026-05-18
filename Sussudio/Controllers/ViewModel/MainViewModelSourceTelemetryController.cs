using System;
using Sussudio.Models;
using Sussudio.Services.Runtime;
using Sussudio.Services.Telemetry;

namespace Sussudio.ViewModels;

public partial class MainViewModel
{
    /// <summary>
    /// Owns source telemetry ingress, UI projection, and source-aware retargeting
    /// for the compatibility ViewModel facade.
    /// </summary>
    private sealed class MainViewModelSourceTelemetryController
    {
        private readonly MainViewModel _viewModel;

        private Models.SourceTelemetryAvailability _lastAppliedTelemetryAvailability = Models.SourceTelemetryAvailability.Unknown;
        private Models.SourceTelemetryConfidence _lastAppliedTelemetryConfidence = Models.SourceTelemetryConfidence.Unknown;
        private SourceTelemetryOrigin _lastAppliedFrameRateOrigin = SourceTelemetryOrigin.Unknown;
        private bool _hasAppliedTelemetryEnums;
        private int? _lastTelemetryAgeBucket;

        public MainViewModelSourceTelemetryController(MainViewModel viewModel)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public void RefreshSourceTelemetrySummaryAge()
        {
            var ageSeconds = TelemetryAgeHelper.ComputeAgeSeconds(_viewModel.SourceTelemetryTimestampUtc, DateTimeOffset.UtcNow);
            var ageBucket = ageSeconds.HasValue ? ageSeconds.Value / 5 : (int?)null;
            if (_lastTelemetryAgeBucket.HasValue &&
                ageBucket.HasValue &&
                _lastTelemetryAgeBucket.Value == ageBucket.GetValueOrDefault())
            {
                return;
            }

            var refreshedSummary = SourceTelemetryPresentationBuilder.BuildSourceSummary(_viewModel._latestSourceTelemetry, DateTimeOffset.UtcNow);
            if (!string.Equals(_viewModel.SourceTelemetrySummaryText, refreshedSummary, StringComparison.Ordinal))
            {
                _viewModel.SourceTelemetrySummaryText = refreshedSummary;
            }

            _lastTelemetryAgeBucket = ageBucket;
        }

        public void OnSourceTelemetryUpdated(object? sender, SourceSignalTelemetrySnapshot snapshot)
        {
            if (!_viewModel._dispatcherQueue.TryEnqueue(() =>
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
            _viewModel._latestSourceTelemetry = snapshot;
            _viewModel.SourceWidth = snapshot.Width;
            _viewModel.SourceHeight = snapshot.Height;
            _viewModel.SourceIsHdr = snapshot.IsHdr;
            if (!_viewModel.IsRecording && _viewModel.IsHdrEnabled && snapshot.IsHdr == false)
            {
                _viewModel.IsHdrEnabled = false;
            }

            if (!_hasAppliedTelemetryEnums || _lastAppliedTelemetryAvailability != snapshot.Availability)
            {
                _viewModel.SourceTelemetryAvailability = snapshot.Availability.ToString();
                _lastAppliedTelemetryAvailability = snapshot.Availability;
            }

            _viewModel.SourceTelemetryOriginDetail = snapshot.OriginDetail;
            if (!_hasAppliedTelemetryEnums || _lastAppliedTelemetryConfidence != snapshot.Confidence)
            {
                _viewModel.SourceTelemetryConfidence = snapshot.Confidence.ToString();
                _lastAppliedTelemetryConfidence = snapshot.Confidence;
            }

            _viewModel.SourceTelemetryDiagnosticSummary = snapshot.DiagnosticSummary;
            _viewModel.SourceTelemetryTimestampUtc = snapshot.TimestampUtc;
            _viewModel.DetectedSourceFrameRate = snapshot.FrameRateExact;
            _viewModel.DetectedSourceFrameRateArg = snapshot.FrameRateArg;
            if (!_hasAppliedTelemetryEnums || _lastAppliedFrameRateOrigin != snapshot.Origin)
            {
                _viewModel.SourceFrameRateOrigin = snapshot.Origin != SourceTelemetryOrigin.Unknown
                    ? snapshot.Origin.ToString()
                    : "Unknown";
                _lastAppliedFrameRateOrigin = snapshot.Origin;
            }

            _hasAppliedTelemetryEnums = true;
            _lastTelemetryAgeBucket = null;
            _viewModel.SourceTelemetrySummaryText = SourceTelemetryPresentationBuilder.BuildSourceSummary(snapshot, DateTimeOffset.UtcNow);

            var modeKey = snapshot.GetModeKey();
            if (!string.IsNullOrWhiteSpace(modeKey) &&
                !string.Equals(modeKey, _viewModel._lastSourceModeKey, StringComparison.Ordinal))
            {
                if (allowAutoRetarget)
                {
                    var shouldAutoRetargetResolution =
                        IsAutoResolutionValue(_viewModel.SelectedResolution) ||
                        !_viewModel._hasUserOverriddenResolutionForCurrentMode;
                    var shouldAutoRetargetFrameRate =
                        _viewModel.IsAutoFrameRateSelected ||
                        !_viewModel._hasUserOverriddenFrameRateForCurrentMode;
                    _viewModel._lastSourceModeKey = modeKey;
                    _viewModel._forceSourceAutoRetarget = shouldAutoRetargetResolution || shouldAutoRetargetFrameRate;
                    if (shouldAutoRetargetResolution)
                    {
                        _viewModel._hasUserOverriddenResolutionForCurrentMode = false;
                    }

                    if (shouldAutoRetargetFrameRate)
                    {
                        _viewModel._hasUserOverriddenFrameRateForCurrentMode = false;
                    }
                }
            }

            var shouldRebuildModeOptions = allowAutoRetarget &&
                                           (_viewModel._forceSourceAutoRetarget ||
                                            (snapshot.HasSignalData && _viewModel.AvailableResolutions.Count == 0));
            if (shouldRebuildModeOptions)
            {
                if (_viewModel.IsRecording)
                {
                    _viewModel._pendingModeOptionsRefresh = true;
                }
                else
                {
                    _viewModel.RebuildResolutionOptions();
                }
            }
            else
            {
                _viewModel.UpdateTargetSummary();
            }
        }
    }
}
