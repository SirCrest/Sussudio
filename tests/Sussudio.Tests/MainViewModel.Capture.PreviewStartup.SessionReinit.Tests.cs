using System.IO;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task PreviewStartupSessionReinitOwnership_LivesInFocusedControllers()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewStartupText = ReadMainWindowPreviewStartupAdapterSource();
        var previewStartupSessionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Startup/PreviewStartupSessionController.cs")
            .Replace("\r\n", "\n");
        var previewReinitText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewReinitTransitionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewReinitTransitionController.cs")
            .Replace("\r\n", "\n");
        var previewRendererText = ReadMainWindowPreviewRendererAdapterSource();
        var previewRuntimeSnapshotText = ReadRepoFile("Sussudio/MainWindow.PreviewRuntimeSnapshot.cs")
            .Replace("\r\n", "\n");
        var previewRuntimeSnapshotSamplingControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotSamplingController.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainWindowText, "InitializePreviewStartupSessionController();");
        AssertContains(mainWindowText, "InitializePreviewReinitTransitionController();");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewTransitions.Reinit.cs")),
            "preview reinit adapter lives in the focused preview transition reinit partial");
        AssertContains(previewStartupText, "private PreviewStartupSessionController _previewStartupSessionController = null!;");
        AssertContains(previewStartupText, "private void InitializePreviewStartupSessionController()");
        AssertContains(previewStartupText, "private PreviewStartupState CurrentPreviewStartupState");
        AssertContains(previewStartupText, "private string PreviewStartupAttemptLabel");
        AssertContains(previewStartupText, "private bool ShouldBeginPreviewStartupAttempt");
        AssertContains(previewStartupText, "new PreviewStartupSessionControllerContext");
        AssertContains(previewStartupText, "ResetSignalState = ResetPreviewSignalState,");
        AssertContains(previewStartupText, "StopWatchdog = StopPreviewStartupWatchdog,");
        AssertContains(previewStartupText, "ScheduleFadeIn = SchedulePreviewFadeIn,");
        AssertContains(previewStartupText, "=> _previewStartupSessionController.SetStartupState(state, reason);");
        AssertContains(previewStartupText, "=> _previewStartupSessionController.BeginStartupAttempt();");
        AssertContains(previewStartupText, "=> _previewStartupSessionController.ConfirmFirstVisual(source);");
        AssertContains(previewStartupText, "=> _previewStartupSessionController.ResetStartupTracking(keepRecoveryCount, preserveReinitAnimation);");
        AssertContains(previewStartupSessionControllerText, "internal enum PreviewStartupState");
        AssertContains(previewStartupSessionControllerText, "internal sealed class PreviewStartupSessionControllerContext");
        AssertContains(previewStartupSessionControllerText, "internal sealed class PreviewStartupSessionController");
        AssertContains(previewStartupSessionControllerText, "public PreviewStartupState State { get; private set; } = PreviewStartupState.Idle;");
        AssertContains(previewStartupSessionControllerText, "public string? AttemptId { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public DateTimeOffset? RequestedUtc { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public DateTimeOffset? RendererAttachedUtc { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public DateTimeOffset? FirstVisualUtc { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public string? LastFailureReason { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public string? MissingSignals { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public int RecoveryAttemptCount { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public bool FirstVisualConfirmed { get; private set; }");
        AssertContains(previewStartupSessionControllerText, "public bool ShouldRefreshMissingSignalsForSnapshot => IsWaitingForFirstVisual || IsFailed;");
        AssertContains(previewStartupSessionControllerText, "public bool ShouldBeginAttempt => string.IsNullOrWhiteSpace(AttemptId) || IsFailed || IsIdle;");
        AssertContains(previewStartupSessionControllerText, "public bool IsSignalWindowActive(bool isPreviewing)");
        AssertContains(previewStartupSessionControllerText, "public string AttemptLabel => AttemptId ?? \"none\";");
        AssertContains(previewStartupSessionControllerText, "public void BeginStartupAttempt()");
        AssertContains(previewStartupSessionControllerText, "public void SetStartupState(PreviewStartupState state, string? reason = null)");
        AssertContains(previewStartupSessionControllerText, "public void ConfirmFirstVisual(string source)");
        AssertContains(previewStartupSessionControllerText, "public void ResetStartupTracking(bool keepRecoveryCount = false, bool preserveReinitAnimation = false)");
        AssertContains(previewStartupSessionControllerText, "PREVIEW_START_STATE state={state} attempt={AttemptLabel}");
        AssertContains(previewStartupSessionControllerText, "PREVIEW_START_REQUESTED attempt={AttemptId}");
        AssertContains(previewStartupSessionControllerText, "PREVIEW_FIRST_VISUAL_IGNORED attempt={AttemptLabel}");
        AssertContains(previewStartupSessionControllerText, "PREVIEW_FIRST_VISUAL_CONFIRMED attempt={AttemptLabel}");
        AssertContains(previewStartupSessionControllerText, "public void MarkRendererAttached(DateTimeOffset attachedUtc)");
        AssertContains(previewStartupSessionControllerText, "public bool MarkFirstVisualConfirmed(DateTimeOffset firstVisualUtc)");
        AssertContains(previewStartupSessionControllerText, "public void SetMissingSignals(string? missingSignals)");
        AssertContains(previewRuntimeSnapshotText, "StartupSessionController = _previewStartupSessionController,");
        AssertContains(previewRuntimeSnapshotSamplingControllerText, "StartupState = startupSession.State.ToString(),");
        AssertContains(previewReinitText, "private PreviewReinitTransitionController _previewReinitTransitionController = null!;");
        AssertContains(previewReinitText, "private bool IsPreviewReinitAnimating");
        AssertContains(previewReinitText, "=> _previewReinitTransitionController.IsAnimating;");
        AssertContains(previewReinitText, "private async Task ViewModel_PreviewReinitRequested(string reason)");
        AssertContains(previewReinitText, "private Task ViewModel_PreviewRendererStopRequested()");
        AssertContains(previewReinitText, "private void HandlePreviewReinitializingChanged()");
        AssertContains(previewReinitText, "=> _previewReinitTransitionController.HandleReinitializingChanged(");
        AssertContains(previewReinitText, "new PreviewReinitCompletionPresentationContext");
        AssertContains(previewReinitText, "IsPreviewReinitializing = ViewModel.IsPreviewReinitializing,");
        AssertContains(previewReinitText, "IsPreviewing = ViewModel.IsPreviewing,");
        AssertContains(previewReinitText, "IsFirstVisualConfirmed = IsPreviewFirstVisualConfirmed,");
        AssertContains(previewReinitText, "AttemptLabel = PreviewStartupAttemptLabel,");
        AssertContains(previewReinitText, "CallerName = nameof(HandleViewModelPropertyChangedAsync),");
        AssertContains(previewReinitText, "UpdateDeviceApplyButtonState = UpdateDeviceApplyButtonState,");
        AssertContains(previewReinitText, "RevealUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,");
        AssertContains(previewReinitText, "StopPreviewStartupOverlay = StopPreviewStartupOverlay,");
        AssertContains(previewReinitText, "ResetPreviewContentTransform = ResetPreviewContentTransform,");
        AssertContains(previewReinitText, "ShowStartPreviewButtonPresentation = ShowStartPreviewButtonPresentation,");
        AssertContains(previewReinitTransitionControllerText, "internal sealed class PreviewReinitTransitionController");
        AssertContains(previewReinitTransitionControllerText, "internal sealed class PreviewReinitCompletionPresentationContext");
        AssertContains(previewReinitTransitionControllerText, "public bool IsAnimating { get; private set; }");
        AssertContains(previewReinitTransitionControllerText, "public void BeginAnimateOut(string reason, string callerName)");
        AssertContains(previewReinitTransitionControllerText, "public PreviewReinitCompletionPresentation GetCompletionPresentation(");
        AssertContains(previewReinitTransitionControllerText, "public void HandleReinitializingChanged(PreviewReinitCompletionPresentationContext context)");
        AssertContains(previewReinitTransitionControllerText, "public void CompleteFirstVisualTransition(string attemptLabel, string callerName)");
        AssertContains(previewReinitTransitionControllerText, "public void ResetConfirmedVisualTransition(string attemptLabel, string reason, string callerName)");
        AssertContains(previewReinitTransitionControllerText, "public void ClearForStartupReset(bool preserveReinitAnimation, string callerName)");
        AssertContains(previewReinitTransitionControllerText, "public void Clear(string callerName, bool logWhenInactive = true, string? operationName = null)");
        AssertDoesNotContain(previewStartupText, "_previewStartupSessionController.BeginAttempt(");
        AssertDoesNotContain(previewStartupText, "_previewStartupSessionController.Reset(keepRecoveryCount)");
        AssertDoesNotContain(previewStartupText, "PREVIEW_FIRST_VISUAL_CONFIRMED attempt=");
        AssertDoesNotContain(previewRendererText, "_previewStartupState.ToString()");
        AssertDoesNotContain(previewStartupText, "private bool _isPreviewReinitAnimating;");
        AssertDoesNotContain(previewStartupText, "private bool _previewStopRequestedByUser;");
        AssertDoesNotContain(previewReinitText, "private bool _isPreviewReinitAnimating;");
        AssertDoesNotContain(mainWindowText, "private enum PreviewStartupState");
        AssertDoesNotContain(previewStartupText, "private enum PreviewStartupState");
        AssertDoesNotContain(previewStartupText, "private PreviewStartupState _previewStartupState = PreviewStartupState.Idle;");
        AssertDoesNotContain(previewStartupText, "private string? _previewStartupAttemptId;");
        AssertDoesNotContain(previewStartupText, "private DateTimeOffset? _previewStartupRequestedUtc;");
        AssertDoesNotContain(previewStartupText, "private DateTimeOffset? _previewRendererAttachedUtc;");
        AssertDoesNotContain(previewStartupText, "private DateTimeOffset? _previewFirstVisualUtc;");
        AssertDoesNotContain(previewStartupText, "private string? _previewLastFailureReason;");
        AssertDoesNotContain(previewStartupText, "private string? _previewStartupMissingSignals;");
        AssertDoesNotContain(previewStartupText, "private int _previewRecoveryAttemptCount;");
        AssertDoesNotContain(previewStartupText, "private bool _previewFirstVisualConfirmed;");
        AssertDoesNotContain(previewReinitText, "case PreviewReinitCompletionPresentation.");
        AssertDoesNotContain(previewReinitText, "GetCompletionPresentation(");

        return Task.CompletedTask;
    }
}
