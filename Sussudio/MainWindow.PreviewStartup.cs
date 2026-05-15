using System;
using System.Threading;
using Sussudio.Models;

namespace Sussudio;

// Preview startup visual state machine. It delays the "preview is visible"
// transition until meaningful media/render signals arrive, avoiding black-frame
// flashes during source-reader and renderer warm-up. Signal collection lives in
// MainWindow.PreviewStartupSignals.cs; watchdog recovery lives in
// MainWindow.PreviewStartupWatchdog.cs.
public sealed partial class MainWindow
{
    private enum PreviewStartupState
    {
        Idle,
        StartingSession,
        RendererAttaching,
        WaitingForFirstVisual,
        Rendering,
        Failed
    }

    private PreviewStartupState _previewStartupState = PreviewStartupState.Idle;
    private string? _previewStartupAttemptId;
    private DateTimeOffset? _previewStartupRequestedUtc;
    private DateTimeOffset? _previewRendererAttachedUtc;
    private DateTimeOffset? _previewFirstVisualUtc;
    private string? _previewLastFailureReason;
    private string? _previewStartupMissingSignals;
    private int _previewRecoveryAttemptCount;
    private bool _previewFirstVisualConfirmed;
    private bool _previewStopRequestedByUser;
    private bool _isPreviewReinitAnimating;

    private static bool IsPreviewStartupFailedState(PreviewStartupState state)
        => state == PreviewStartupState.Failed;

    private static bool IsPreviewStartupTerminalState(PreviewStartupState state)
        => state is PreviewStartupState.Idle or PreviewStartupState.Rendering or PreviewStartupState.Failed;

    private void SetPreviewStartupState(PreviewStartupState state, string? reason = null)
    {
        if (!string.IsNullOrWhiteSpace(reason))
        {
            _previewLastFailureReason = reason;
        }

        if (_previewStartupState == state && string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        _previewStartupState = state;
        Logger.Log(
            $"PREVIEW_START_STATE state={state} attempt={_previewStartupAttemptId ?? "none"} " +
            $"recovery={_previewRecoveryAttemptCount} reason={reason ?? "-"}");
    }
    private void BeginPreviewStartupAttempt()
    {
        _previewRecoveryAttemptCount = 0;
        _previewStartupAttemptId = Guid.NewGuid().ToString("N");
        _previewStartupRequestedUtc = DateTimeOffset.UtcNow;
        _previewRendererAttachedUtc = null;
        _previewFirstVisualUtc = null;
        _previewLastFailureReason = null;
        _previewStartupMissingSignals = null;
        _previewFirstVisualConfirmed = false;
        ResetPreviewSignalState();
        Interlocked.Exchange(ref _previewStartupFailureStopScheduled, 0);

        SetPreviewStartupState(PreviewStartupState.StartingSession);
        Logger.Log(
            $"PREVIEW_START_REQUESTED attempt={_previewStartupAttemptId} " +
            $"device={ViewModel.SelectedDevice?.Name ?? "none"}");
    }
    private void ConfirmPreviewFirstVisual(string source)
    {
        if (_previewFirstVisualConfirmed || !ViewModel.IsPreviewing)
        {
            return;
        }

        if (_previewStopRequestedByUser)
        {
            Logger.Log(
                $"PREVIEW_FIRST_VISUAL_IGNORED attempt={_previewStartupAttemptId ?? "none"} " +
                $"source={source} reason=stop-requested");
            return;
        }

        _previewFirstVisualConfirmed = true;
        MarkPreviewStartupFirstVisualConfirmed();
        _previewFirstVisualUtc = DateTimeOffset.UtcNow;
        SetPreviewStartupState(PreviewStartupState.Rendering);
        StopPreviewStartupWatchdog();
        StopPreviewStartupOverlay();
        // Wait for a few rendered frames before fading in — the first frame
        // from the source reader may be black or stale while the signal settles.
        SchedulePreviewFadeIn();
        if (_isPreviewReinitAnimating)
        {
            Logger.Log($"PREVIEW_REINIT_ANIMATE_IN attempt={_previewStartupAttemptId ?? "none"}");
            _isPreviewReinitAnimating = false;
            Logger.Log($"D3D11_RENDERER_REINIT_FLAG flag=false caller={nameof(ConfirmPreviewFirstVisual)}");
        }
        _previewStartupMissingSignals = string.Empty;
        var elapsedMs = _previewStartupRequestedUtc.HasValue
            ? (DateTimeOffset.UtcNow - _previewStartupRequestedUtc.Value).TotalMilliseconds
            : 0;
        Logger.Log(
            $"PREVIEW_FIRST_VISUAL_CONFIRMED attempt={_previewStartupAttemptId ?? "none"} " +
            $"source={source} elapsedMs={elapsedMs:0} recovery={_previewRecoveryAttemptCount}");
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
        _previewStartupAttemptId = null;
        _previewStartupRequestedUtc = null;
        _previewRendererAttachedUtc = null;
        _previewFirstVisualUtc = null;
        _previewLastFailureReason = null;
        _previewStartupMissingSignals = null;
        _previewFirstVisualConfirmed = false;
        ResetPreviewSignalState();
        Interlocked.Exchange(ref _previewStartupFailureStopScheduled, 0);

        if (!keepRecoveryCount)
        {
            _previewRecoveryAttemptCount = 0;
        }

        if (!IsPreviewStartupTerminalState(_previewStartupState))
        {
            SetPreviewStartupState(PreviewStartupState.Idle);
        }
        else
        {
            _previewStartupState = PreviewStartupState.Idle;
        }
    }
}
