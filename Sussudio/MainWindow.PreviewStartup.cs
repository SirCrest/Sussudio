using System;
using Sussudio.Controllers;

namespace Sussudio;

// XAML-facing adapter for preview startup session orchestration. It supplies
// UI/runtime callbacks while PreviewStartupSessionController owns transitions.
public sealed partial class MainWindow
{
    private PreviewStartupSessionController _previewStartupSessionController = null!;

    private void InitializePreviewStartupSessionController()
        => _previewStartupSessionController = new PreviewStartupSessionController(new PreviewStartupSessionControllerContext
        {
            IsPreviewing = () => ViewModel.IsPreviewing,
            IsPreviewStopRequestedByUser = () => IsPreviewStopRequestedByUser,
            GetSelectedDeviceName = () => ViewModel.SelectedDevice?.Name,
            ResetSignalState = ResetPreviewSignalState,
            ResetFailureStopSchedule = ResetPreviewStartupFailureStopSchedule,
            MarkFirstVisualSignalConfirmed = MarkPreviewStartupFirstVisualConfirmed,
            StopWatchdog = StopPreviewStartupWatchdog,
            StopOverlay = StopPreviewStartupOverlay,
            StopFadeInTimer = StopPreviewFadeInTimer,
            ScheduleFadeIn = SchedulePreviewFadeIn,
            CompleteFirstVisualTransition = (attemptLabel, callerName) =>
                _previewReinitTransitionController.CompleteFirstVisualTransition(attemptLabel, callerName),
            ClearReinitTransitionForStartupReset = (preserveReinitAnimation, callerName) =>
                _previewReinitTransitionController.ClearForStartupReset(preserveReinitAnimation, callerName),
            Log = message => Logger.Log(message),
            CreateAttemptId = () => Guid.NewGuid().ToString("N"),
            GetUtcNow = () => DateTimeOffset.UtcNow
        });

    private PreviewStartupState CurrentPreviewStartupState
        => _previewStartupSessionController.State;

    private string PreviewStartupAttemptLabel
        => _previewStartupSessionController.AttemptLabel;

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
        => _previewStartupSessionController.SetStartupState(state, reason);

    private void MarkPreviewRendererAttached()
        => _previewStartupSessionController.MarkRendererAttached(DateTimeOffset.UtcNow);

    private void BeginPreviewStartupAttempt()
        => _previewStartupSessionController.BeginStartupAttempt();

    private void ConfirmPreviewFirstVisual(string source)
        => _previewStartupSessionController.ConfirmFirstVisual(source);

    private void ResetPreviewStartupTracking(bool keepRecoveryCount = false, bool preserveReinitAnimation = false)
        => _previewStartupSessionController.ResetStartupTracking(keepRecoveryCount, preserveReinitAnimation);
}
