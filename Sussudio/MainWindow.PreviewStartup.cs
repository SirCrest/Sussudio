using System;
using System.Threading;
using Sussudio.Controllers;

namespace Sussudio;

// Preview startup visual state machine. It delays the "preview is visible"
// transition until meaningful media/render signals arrive, avoiding black-frame
// flashes during source-reader and renderer warm-up. Signal collection lives in
// MainWindow.PreviewStartupSignals.cs; watchdog recovery lives in
// MainWindow.PreviewStartupWatchdog.cs.
public sealed partial class MainWindow
{
    private PreviewStartupSessionController _previewStartupSessionController = null!;
    private bool _previewStopRequestedByUser;
    private bool _isPreviewReinitAnimating;

    private void InitializePreviewStartupSessionController()
        => _previewStartupSessionController = new PreviewStartupSessionController();

    private PreviewStartupState CurrentPreviewStartupState
        => _previewStartupSessionController.State;

    private string PreviewStartupAttemptLabel
        => _previewStartupSessionController.AttemptId ?? "none";

    private string? PreviewStartupAttemptId
        => _previewStartupSessionController.AttemptId;

    private DateTimeOffset? PreviewStartupRequestedUtc
        => _previewStartupSessionController.RequestedUtc;

    private string? PreviewStartupMissingSignals
    {
        get => _previewStartupSessionController.MissingSignals;
        set => _previewStartupSessionController.SetMissingSignals(value);
    }

    private int PreviewStartupRecoveryAttemptCount
        => _previewStartupSessionController.RecoveryAttemptCount;

    private string? PreviewStartupLastFailureReason
        => _previewStartupSessionController.LastFailureReason;

    private bool IsPreviewFirstVisualConfirmed
        => _previewStartupSessionController.FirstVisualConfirmed;

    private bool ShouldBeginPreviewStartupAttempt
        => _previewStartupSessionController.ShouldBeginAttempt;

    private void SetPreviewStartupState(PreviewStartupState state, string? reason = null)
    {
        if (!_previewStartupSessionController.SetState(state, reason))
        {
            return;
        }

        LogPreviewStartupStateChange(state, reason);
    }

    private void LogPreviewStartupStateChange(PreviewStartupState state, string? reason = null)
    {
        Logger.Log(
            $"PREVIEW_START_STATE state={state} attempt={PreviewStartupAttemptLabel} " +
            $"recovery={PreviewStartupRecoveryAttemptCount} reason={reason ?? "-"}");
    }

    private void MarkPreviewRendererAttached()
        => _previewStartupSessionController.MarkRendererAttached(DateTimeOffset.UtcNow);

    private void BeginPreviewStartupAttempt()
    {
        var stateChanged = _previewStartupSessionController.BeginAttempt(
            Guid.NewGuid().ToString("N"),
            DateTimeOffset.UtcNow);
        ResetPreviewSignalState();
        Interlocked.Exchange(ref _previewStartupFailureStopScheduled, 0);

        if (stateChanged)
        {
            LogPreviewStartupStateChange(PreviewStartupState.StartingSession);
        }

        Logger.Log(
            $"PREVIEW_START_REQUESTED attempt={PreviewStartupAttemptId} " +
            $"device={ViewModel.SelectedDevice?.Name ?? "none"}");
    }

    private void ConfirmPreviewFirstVisual(string source)
    {
        if (IsPreviewFirstVisualConfirmed || !ViewModel.IsPreviewing)
        {
            return;
        }

        if (_previewStopRequestedByUser)
        {
            Logger.Log(
                $"PREVIEW_FIRST_VISUAL_IGNORED attempt={PreviewStartupAttemptLabel} " +
                $"source={source} reason=stop-requested");
            return;
        }

        _previewStartupSessionController.MarkFirstVisualConfirmed(DateTimeOffset.UtcNow);
        MarkPreviewStartupFirstVisualConfirmed();
        SetPreviewStartupState(PreviewStartupState.Rendering);
        StopPreviewStartupWatchdog();
        StopPreviewStartupOverlay();
        // Wait for a few rendered frames before fading in — the first frame
        // from the source reader may be black or stale while the signal settles.
        SchedulePreviewFadeIn();
        if (_isPreviewReinitAnimating)
        {
            Logger.Log($"PREVIEW_REINIT_ANIMATE_IN attempt={PreviewStartupAttemptLabel}");
            _isPreviewReinitAnimating = false;
            Logger.Log($"D3D11_RENDERER_REINIT_FLAG flag=false caller={nameof(ConfirmPreviewFirstVisual)}");
        }
        PreviewStartupMissingSignals = string.Empty;
        var elapsedMs = _previewStartupSessionController.GetElapsedMilliseconds(DateTimeOffset.UtcNow);
        Logger.Log(
            $"PREVIEW_FIRST_VISUAL_CONFIRMED attempt={PreviewStartupAttemptLabel} " +
            $"source={source} elapsedMs={elapsedMs:0} recovery={PreviewStartupRecoveryAttemptCount}");
    }

    private void ResetPreviewStartupTracking(bool keepRecoveryCount = false, bool preserveReinitAnimation = false)
    {
        StopPreviewStartupWatchdog();
        StopPreviewStartupOverlay();
        StopPreviewFadeInTimer();
        if (!preserveReinitAnimation)
        {
            _isPreviewReinitAnimating = false;
            Logger.Log($"D3D11_RENDERER_REINIT_FLAG flag=false caller={nameof(ResetPreviewStartupTracking)}");
        }
        ResetPreviewSignalState();
        Interlocked.Exchange(ref _previewStartupFailureStopScheduled, 0);

        if (_previewStartupSessionController.Reset(keepRecoveryCount))
        {
            LogPreviewStartupStateChange(PreviewStartupState.Idle);
        }
    }
}
