using System;
using Sussudio.Controllers;

namespace Sussudio;

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
}
