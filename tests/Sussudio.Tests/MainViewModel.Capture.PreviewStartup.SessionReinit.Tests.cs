using System.IO;
using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task PreviewStartupSessionReinitOwnership_LivesInFocusedControllers()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewStartupText = ReadMainWindowPreviewStartupAdapterSource();
        var previewStartupSessionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Startup/PreviewStartupControllers.cs")
            .Replace("\r\n", "\n");
        var previewReinitText = ReadMainWindowPreviewTransitionsAdapterSource();
        var previewReinitTransitionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewTransitionAnimationController.cs")
            .Replace("\r\n", "\n");
        var previewRendererText = ReadMainWindowPreviewRendererAdapterSource();
        var previewRuntimeSnapshotText = previewRendererText;
        var previewRuntimeSnapshotSamplingControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Renderer/PreviewRuntimeSnapshotController.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainWindowText, "InitializePreviewStartupSessionController();");
        AssertContains(mainWindowText, "InitializePreviewReinitTransitionController();");
        AssertEqual(
            true,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.Composition.cs")),
            "preview reinit adapter lives in the preview transitions composition partial");
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
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "PreviewReinitTransitionController.cs")),
            "preview reinit transition state lives with preview transition animation ownership");
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

    internal static Task PreviewStartupSessionController_PreservesAttemptStateContracts()
    {
        var controllerType = RequireType("Sussudio.Controllers.PreviewStartupSessionController");
        var contextType = RequireType("Sussudio.Controllers.PreviewStartupSessionControllerContext");
        var stateType = RequireType("Sussudio.Controllers.PreviewStartupState");
        var events = new List<string>();
        var isPreviewing = true;
        var isStopRequested = false;
        var now = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero);
        var context = Activator.CreateInstance(contextType, nonPublic: true)!;

        void SetContext(string propertyName, object value)
        {
            var property = contextType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"PreviewStartupSessionControllerContext.{propertyName} was not found.");
            property.SetValue(context, value);
        }

        SetContext("IsPreviewing", new Func<bool>(() => isPreviewing));
        SetContext("IsPreviewStopRequestedByUser", new Func<bool>(() => isStopRequested));
        SetContext("GetSelectedDeviceName", new Func<string?>(() => "Cam Link 4K"));
        SetContext("ResetSignalState", new Action(() => events.Add("reset-signals")));
        SetContext("ResetFailureStopSchedule", new Action(() => events.Add("reset-failure-stop")));
        SetContext("MarkFirstVisualSignalConfirmed", new Action(() => events.Add("mark-signal-visual")));
        SetContext("StopWatchdog", new Action(() => events.Add("stop-watchdog")));
        SetContext("StopOverlay", new Action(() => events.Add("stop-overlay")));
        SetContext("StopFadeInTimer", new Action(() => events.Add("stop-fade-timer")));
        SetContext("ScheduleFadeIn", new Action(() => events.Add("schedule-fade")));
        SetContext("CompleteFirstVisualTransition", new Action<string, string>((attempt, caller) => events.Add($"complete-reinit:{attempt}:{caller}")));
        SetContext("ClearReinitTransitionForStartupReset", new Action<bool, string>((preserve, caller) => events.Add($"clear-reinit:{preserve}:{caller}")));
        SetContext("Log", new Action<string>(message => events.Add($"log:{message}")));
        SetContext("CreateAttemptId", new Func<string>(() => "attempt-1"));
        SetContext("GetUtcNow", new Func<DateTimeOffset>(() => now));

        var controller = Activator.CreateInstance(controllerType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, binder: null, args: new[] { context }, culture: null)!;
        var beginStartupAttempt = controllerType.GetMethod("BeginStartupAttempt")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.BeginStartupAttempt was not found.");
        var setStartupState = controllerType.GetMethod("SetStartupState")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.SetStartupState was not found.");
        var markRendererAttached = controllerType.GetMethod("MarkRendererAttached")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.MarkRendererAttached was not found.");
        var markFirstVisualConfirmed = controllerType.GetMethod("MarkFirstVisualConfirmed")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.MarkFirstVisualConfirmed was not found.");
        var confirmFirstVisual = controllerType.GetMethod("ConfirmFirstVisual")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.ConfirmFirstVisual was not found.");
        var setMissingSignals = controllerType.GetMethod("SetMissingSignals")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.SetMissingSignals was not found.");
        var resetStartupTracking = controllerType.GetMethod("ResetStartupTracking")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.ResetStartupTracking was not found.");
        var getElapsedMilliseconds = controllerType.GetMethod("GetElapsedMilliseconds")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.GetElapsedMilliseconds was not found.");
        var isSignalWindowActive = controllerType.GetMethod("IsSignalWindowActive")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.IsSignalWindowActive was not found.");

        object State(string value) => Enum.Parse(stateType, value);
        bool SignalWindowActive(bool previewing) => (bool)isSignalWindowActive.Invoke(controller, new object[] { previewing })!;

        AssertEqual(State("Idle"), GetPropertyValue(controller, "State"), "initial startup state");
        AssertEqual(true, GetBoolProperty(controller, "ShouldBeginAttempt"), "initial attempt gate");
        AssertEqual(false, GetBoolProperty(controller, "ShouldRefreshMissingSignalsForSnapshot"), "idle does not refresh missing signals");
        AssertEqual(false, SignalWindowActive(previewing: true), "idle signal window inactive");

        beginStartupAttempt.Invoke(controller, Array.Empty<object>());
        AssertEqual(State("StartingSession"), GetPropertyValue(controller, "State"), "state after begin attempt");
        AssertEqual(true, SignalWindowActive(previewing: true), "starting session signal window active");
        AssertEqual(false, SignalWindowActive(previewing: false), "stopped preview signal window inactive");
        AssertEqual("attempt-1", GetStringProperty(controller, "AttemptId"), "attempt id after begin");
        AssertEqual(now, GetPropertyValue(controller, "RequestedUtc"), "requested UTC after begin");
        AssertEqual(false, GetBoolProperty(controller, "FirstVisualConfirmed"), "first visual reset on begin");
        AssertEqual(false, GetBoolProperty(controller, "ShouldBeginAttempt"), "active attempt gate");
        AssertEqual(1250.0, getElapsedMilliseconds.Invoke(controller, new object[] { now.AddMilliseconds(1250) }), "elapsed milliseconds");
        AssertEqual(
            "reset-signals|reset-failure-stop|log:PREVIEW_START_STATE state=StartingSession attempt=attempt-1 recovery=0 reason=-|log:PREVIEW_START_REQUESTED attempt=attempt-1 device=Cam Link 4K",
            string.Join("|", events),
            "begin startup orchestration order");

        events.Clear();
        setStartupState.Invoke(controller, new object?[] { State("StartingSession"), null });
        AssertEqual(string.Empty, string.Join("|", events), "duplicate state without reason suppresses log");
        setStartupState.Invoke(controller, new object?[] { State("Failed"), "renderer-attach-failed:test" });
        AssertEqual(State("Failed"), GetPropertyValue(controller, "State"), "failed state");
        AssertEqual(false, SignalWindowActive(previewing: true), "failed state signal window inactive");
        AssertEqual(true, GetBoolProperty(controller, "ShouldRefreshMissingSignalsForSnapshot"), "failed state refreshes missing signals");
        AssertEqual("renderer-attach-failed:test", GetStringProperty(controller, "LastFailureReason"), "failure reason retained");
        AssertEqual(true, GetBoolProperty(controller, "ShouldBeginAttempt"), "failed attempt gate");
        resetStartupTracking.Invoke(controller, new object[] { false, false });
        AssertEqual(State("Idle"), GetPropertyValue(controller, "State"), "terminal reset returns idle");
        AssertEqual(string.Empty, GetStringProperty(controller, "AttemptId"), "terminal reset clears attempt id");

        events.Clear();
        beginStartupAttempt.Invoke(controller, Array.Empty<object>());
        setStartupState.Invoke(controller, new object?[] { State("WaitingForFirstVisual"), null });
        setMissingSignals.Invoke(controller, new object?[] { "FirstVisual" });
        markRendererAttached.Invoke(controller, new object[] { now.AddMilliseconds(100) });
        AssertEqual(true, GetBoolProperty(controller, "IsWaitingForFirstVisual"), "waiting state predicate");
        AssertEqual(true, GetBoolProperty(controller, "ShouldRefreshMissingSignalsForSnapshot"), "waiting state refreshes missing signals");
        AssertEqual(true, SignalWindowActive(previewing: true), "waiting state signal window active");
        AssertEqual(now.AddMilliseconds(100), GetPropertyValue(controller, "RendererAttachedUtc"), "renderer attached UTC");
        AssertEqual(true, markFirstVisualConfirmed.Invoke(controller, new object[] { now.AddMilliseconds(300) }), "first visual confirmation");
        AssertEqual(false, markFirstVisualConfirmed.Invoke(controller, new object[] { now.AddMilliseconds(400) }), "duplicate first visual suppressed");
        AssertEqual(true, GetBoolProperty(controller, "FirstVisualConfirmed"), "first visual confirmed flag");
        AssertEqual(false, SignalWindowActive(previewing: true), "confirmed first visual signal window inactive");
        AssertEqual(now.AddMilliseconds(300), GetPropertyValue(controller, "FirstVisualUtc"), "first visual UTC");
        AssertEqual("FirstVisual", GetStringProperty(controller, "MissingSignals"), "missing signals cached until adapter clears them");

        events.Clear();
        beginStartupAttempt.Invoke(controller, Array.Empty<object>());
        setStartupState.Invoke(controller, new object?[] { State("WaitingForFirstVisual"), null });
        setMissingSignals.Invoke(controller, new object?[] { "FirstVisual" });
        now = now.AddMilliseconds(250);
        confirmFirstVisual.Invoke(controller, new object[] { "D3D11FirstFrame" });
        AssertEqual(State("Rendering"), GetPropertyValue(controller, "State"), "first visual moves to rendering");
        AssertEqual(string.Empty, GetStringProperty(controller, "MissingSignals"), "first visual clears missing signals");
        AssertEqual(
            "reset-signals|reset-failure-stop|log:PREVIEW_START_STATE state=StartingSession attempt=attempt-1 recovery=0 reason=-|log:PREVIEW_START_REQUESTED attempt=attempt-1 device=Cam Link 4K|log:PREVIEW_START_STATE state=WaitingForFirstVisual attempt=attempt-1 recovery=0 reason=-|mark-signal-visual|log:PREVIEW_START_STATE state=Rendering attempt=attempt-1 recovery=0 reason=-|stop-watchdog|stop-overlay|schedule-fade|complete-reinit:attempt-1:ConfirmPreviewFirstVisual|log:PREVIEW_FIRST_VISUAL_CONFIRMED attempt=attempt-1 source=D3D11FirstFrame elapsedMs=250 recovery=0",
            string.Join("|", events),
            "first visual orchestration order");

        events.Clear();
        beginStartupAttempt.Invoke(controller, Array.Empty<object>());
        setStartupState.Invoke(controller, new object?[] { State("WaitingForFirstVisual"), null });
        isStopRequested = true;
        confirmFirstVisual.Invoke(controller, new object[] { "D3D11FirstFrame" });
        AssertEqual(false, GetBoolProperty(controller, "FirstVisualConfirmed"), "stop request suppresses first visual");
        AssertContains(string.Join("|", events), "log:PREVIEW_FIRST_VISUAL_IGNORED attempt=attempt-1 source=D3D11FirstFrame reason=stop-requested");
        isStopRequested = false;

        events.Clear();
        setStartupState.Invoke(controller, new object?[] { State("WaitingForFirstVisual"), null });
        resetStartupTracking.Invoke(controller, new object[] { false, true });
        AssertEqual(State("Idle"), GetPropertyValue(controller, "State"), "nonterminal reset returns idle");
        AssertEqual(string.Empty, GetStringProperty(controller, "MissingSignals"), "nonterminal reset clears missing signals");
        AssertEqual(
            "stop-watchdog|stop-overlay|stop-fade-timer|clear-reinit:True:ResetPreviewStartupTracking|reset-signals|reset-failure-stop|log:PREVIEW_START_STATE state=Idle attempt=none recovery=0 reason=-",
            string.Join("|", events),
            "reset orchestration order");

        return Task.CompletedTask;
    }

    internal static Task PreviewReinitialization_WaitsForPendingFlashbackCycle()
    {
        var viewModelFiles = ReadMainViewModelCodeFiles();
        var viewModelSharedStateText = viewModelFiles["MainViewModel.cs"];
        var viewModelPreviewStateText = viewModelFiles["MainViewModel.cs"];
        var viewModelCaptureStateText = viewModelFiles["MainViewModel.cs"];
        var viewModelFlashbackStateText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var rawPreviewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs")
            .Replace("\r\n", "\n");
        var rawPreviewReinitializeControllerText = rawPreviewLifecycleControllerText;

        AssertContains(viewModelFlashbackStateText, "private const int FlashbackCycleBeforeReinitializeTimeoutMs = 30000;");
        AssertContains(viewModelCaptureStateText, "private const int PreviewReinitializeDebounceMs = 250;");
        AssertContains(viewModelPreviewStateText, "private int _previewReinitializeGeneration;");
        AssertContains(viewModelSharedStateText, "private int _previewReinitializeGeneration;");
        AssertContains(viewModelFiles["MainViewModel.cs"], "=> _previewLifecycleController.ReinitializeDeviceAsync(reason);");
        AssertContains(rawPreviewLifecycleControllerText, "=> _previewReinitializeController.ReinitializeDeviceAsync(reason);");
        AssertContains(rawPreviewReinitializeControllerText, "var reinitializeGeneration = _context.IncrementReinitializeGeneration();");
        AssertContains(rawPreviewReinitializeControllerText, "await Task.Delay(_context.PreviewReinitializeDebounceMs).ConfigureAwait(true);");
        AssertContains(rawPreviewReinitializeControllerText, "_context.ReadReinitializeGeneration() != reinitializeGeneration");
        AssertContains(rawPreviewReinitializeControllerText, "REINIT_COALESCED reason='{reason}' generation={reinitializeGeneration}");
        AssertContains(rawPreviewReinitializeControllerText, "await _context.AwaitWithTimeoutAsync(");
        AssertContains(rawPreviewReinitializeControllerText, "\"Flashback encoder settings cycle before reinitialize\").ConfigureAwait(false);");
        AssertContains(rawPreviewReinitializeControllerText, "REINIT_WAIT_FLASHBACK_CYCLE_TIMEOUT reason={reason} timeoutMs={_context.FlashbackCycleBeforeReinitializeTimeoutMs}");
        AssertContains(rawPreviewReinitializeControllerText, "REINIT_WAIT_FLASHBACK_CYCLE_FAULT");
        AssertContains(rawPreviewReinitializeControllerText, "_context.ClearPendingFlashbackCycleIfSameAndCompleted(pendingCycle);");

        return Task.CompletedTask;
    }

    internal static Task PreviewReinitTransitionController_PreservesTransitionStateContracts()
    {
        var controllerType = RequireType("Sussudio.Controllers.PreviewReinitTransitionController");
        var presentationType = RequireType("Sussudio.Controllers.PreviewReinitCompletionPresentation");
        var contextType = RequireType("Sussudio.Controllers.PreviewReinitCompletionPresentationContext");
        var controller = Activator.CreateInstance(controllerType, nonPublic: true)!;
        var beginAnimateOut = controllerType.GetMethod("BeginAnimateOut")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.BeginAnimateOut was not found.");
        var getCompletionPresentation = controllerType.GetMethod("GetCompletionPresentation")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.GetCompletionPresentation was not found.");
        var handleReinitializingChanged = controllerType.GetMethod("HandleReinitializingChanged")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.HandleReinitializingChanged was not found.");
        var completeFirstVisualTransition = controllerType.GetMethod("CompleteFirstVisualTransition")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.CompleteFirstVisualTransition was not found.");
        var resetConfirmedVisualTransition = controllerType.GetMethod("ResetConfirmedVisualTransition")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.ResetConfirmedVisualTransition was not found.");
        var clearForStartupReset = controllerType.GetMethod("ClearForStartupReset")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.ClearForStartupReset was not found.");
        var clear = controllerType.GetMethod("Clear")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.Clear was not found.");

        object Presentation(string value) => Enum.Parse(presentationType, value);

        object GetPresentation(bool isPreviewReinitializing, bool isPreviewing, bool isFirstVisualConfirmed)
            => getCompletionPresentation.Invoke(
                controller,
                new object[] { isPreviewReinitializing, isPreviewing, isFirstVisualConfirmed })!;

        object CreateContext(
            bool isPreviewReinitializing,
            bool isPreviewing,
            bool isFirstVisualConfirmed,
            string attemptLabel,
            string callerName,
            List<string> events)
        {
            var context = Activator.CreateInstance(contextType, nonPublic: true)!;
            SetPropertyOrBackingField(context, "IsPreviewReinitializing", isPreviewReinitializing);
            SetPropertyOrBackingField(context, "IsPreviewing", isPreviewing);
            SetPropertyOrBackingField(context, "IsFirstVisualConfirmed", isFirstVisualConfirmed);
            SetPropertyOrBackingField(context, "AttemptLabel", attemptLabel);
            SetPropertyOrBackingField(context, "CallerName", callerName);
            SetPropertyOrBackingField(context, "UpdateDeviceApplyButtonState", new Action(() => events.Add("update-apply")));
            SetPropertyOrBackingField(context, "RevealUnavailablePlaceholder", new Action(() => events.Add("reveal-unavailable")));
            SetPropertyOrBackingField(context, "StopPreviewStartupOverlay", new Action(() => events.Add("stop-overlay")));
            SetPropertyOrBackingField(context, "ResetPreviewContentTransform", new Action(() => events.Add("reset-transform")));
            SetPropertyOrBackingField(context, "ShowStartPreviewButtonPresentation", new Action(() => events.Add("show-start")));
            return context;
        }

        void HandleReinitializingChanged(
            bool isPreviewReinitializing,
            bool isPreviewing,
            bool isFirstVisualConfirmed,
            List<string> events)
            => handleReinitializingChanged.Invoke(
                controller,
                new[]
                {
                    CreateContext(
                        isPreviewReinitializing,
                        isPreviewing,
                        isFirstVisualConfirmed,
                        "attempt-3",
                        "HandleViewModelPropertyChangedAsync",
                        events),
                });

        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "initial reinit animation inactive");
        AssertEqual(
            Presentation("ShowStartPreviewButton"),
            GetPresentation(isPreviewReinitializing: false, isPreviewing: false, isFirstVisualConfirmed: false),
            "idle stopped preview shows start presentation");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        AssertEqual(true, GetBoolProperty(controller, "IsAnimating"), "begin reinit marks animation active");
        AssertEqual(
            Presentation("RevealUnavailablePlaceholder"),
            GetPresentation(isPreviewReinitializing: false, isPreviewing: false, isFirstVisualConfirmed: false),
            "completed reinit without preview reveals unavailable placeholder");
        AssertEqual(
            Presentation("ResetConfirmedVisual"),
            GetPresentation(isPreviewReinitializing: false, isPreviewing: true, isFirstVisualConfirmed: true),
            "completed reinit after first visual resets presentation");
        AssertEqual(
            Presentation("None"),
            GetPresentation(isPreviewReinitializing: false, isPreviewing: true, isFirstVisualConfirmed: false),
            "completed reinit before first visual keeps waiting");

        completeFirstVisualTransition.Invoke(controller, new object[] { "attempt-1", "ConfirmPreviewFirstVisual" });
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "first visual clears active reinit animation");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        clearForStartupReset.Invoke(controller, new object[] { true, "ResetPreviewStartupTracking" });
        AssertEqual(true, GetBoolProperty(controller, "IsAnimating"), "startup reset can preserve reinit animation");
        clearForStartupReset.Invoke(controller, new object[] { false, "ResetPreviewStartupTracking" });
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "startup reset clears animation when not preserving");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        resetConfirmedVisualTransition.Invoke(controller, new object[] { "attempt-2", "reinit-stop-failed", "HandleViewModelPropertyChangedAsync" });
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "confirmed visual reset clears active animation");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        clear.Invoke(controller, new object?[] { "PreviewButton_Click", true, "PreviewButton_Click" });
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "explicit clear marks animation inactive");

        var idleStoppedEvents = new List<string>();
        HandleReinitializingChanged(
            isPreviewReinitializing: false,
            isPreviewing: false,
            isFirstVisualConfirmed: false,
            idleStoppedEvents);
        AssertEqual(
            "update-apply,show-start",
            string.Join(",", idleStoppedEvents),
            "idle stopped preview updates apply state then shows start presentation");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        var stoppedReinitCompletionEvents = new List<string>();
        HandleReinitializingChanged(
            isPreviewReinitializing: false,
            isPreviewing: false,
            isFirstVisualConfirmed: false,
            stoppedReinitCompletionEvents);
        AssertEqual(
            "update-apply,reveal-unavailable",
            string.Join(",", stoppedReinitCompletionEvents),
            "completed reinit without preview updates apply state then reveals unavailable placeholder");
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "unavailable placeholder completion clears active animation");

        beginAnimateOut.Invoke(controller, new object[] { "format-change", "ViewModel_PreviewReinitRequested" });
        var confirmedReinitCompletionEvents = new List<string>();
        HandleReinitializingChanged(
            isPreviewReinitializing: false,
            isPreviewing: true,
            isFirstVisualConfirmed: true,
            confirmedReinitCompletionEvents);
        AssertEqual(
            "update-apply,stop-overlay,reset-transform",
            string.Join(",", confirmedReinitCompletionEvents),
            "confirmed visual completion updates apply state, stops overlay, and resets content transform");
        AssertEqual(false, GetBoolProperty(controller, "IsAnimating"), "confirmed visual completion clears active animation");

        return Task.CompletedTask;
    }

    internal static Task MainViewModelPreviewLifecycle_LivesInController()
    {
        var rootText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var previewStateText = ReadRepoFile("Sussudio/ViewModels/MainViewModel.cs")
            .Replace("\r\n", "\n");
        var previewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs")
            .Replace("\r\n", "\n");
        var previewReinitializeControllerText = previewLifecycleControllerText;
        var agentMapText = ReadRepoFile("docs/architecture/AGENT_MAP.md")
            .Replace("\r\n", "\n");
        var cleanupPlanText = ReadRepoFile("docs/architecture/cleanup-plan.md")
            .Replace("\r\n", "\n");

        AssertContains(previewStateText, "private Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewStateText, "=> _previewLifecycleController.ReinitializeDeviceAsync(reason);");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.Capture.cs")),
            "MainViewModel capture lifecycle facade partial");
        if (File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "ViewModels", "MainViewModel.PreviewReinitialization.cs")))
        {
            throw new InvalidOperationException("Preview reinitialization should not live in a tiny pass-through partial.");
        }
        AssertEqual(
            false,
            File.Exists(Path.Combine(
                GetRepoRoot(),
                "Sussudio",
                "Controllers",
                "ViewModel",
                "MainViewModelPreviewReinitializeController.cs")),
            "Preview reinitialize transaction controller lives with preview lifecycle owner");
        AssertContains(previewLifecycleControllerText, "private readonly MainViewModelPreviewReinitializeController _previewReinitializeController;");
        AssertContains(previewLifecycleControllerText, "public Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewLifecycleControllerText, "=> _previewReinitializeController.ReinitializeDeviceAsync(reason);");
        AssertContains(previewLifecycleControllerText, "namespace Sussudio.Controllers;");
        AssertContains(previewLifecycleControllerText, "internal sealed class MainViewModelPreviewLifecycleController");
        AssertContains(previewReinitializeControllerText, "namespace Sussudio.Controllers;");
        AssertContains(previewReinitializeControllerText, "internal sealed class MainViewModelPreviewReinitializeController");
        AssertContains(previewReinitializeControllerText, "public void CancelPendingPreviewRestart()");
        AssertContains(previewReinitializeControllerText, "public void ResetPendingPreviewRestartCancellation()");
        AssertContains(previewReinitializeControllerText, "public async Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewReinitializeControllerText, "private readonly MainViewModelPreviewReinitializeControllerContext _context;");
        AssertDoesNotContain(previewReinitializeControllerText, "private readonly MainViewModel _viewModel;");
        AssertDoesNotContain(previewReinitializeControllerText, "_viewModel.");
        AssertContains(previewReinitializeControllerText, "var reinitializeGeneration = _context.IncrementReinitializeGeneration();");
        AssertContains(previewReinitializeControllerText, "await Task.Delay(_context.PreviewReinitializeDebounceMs).ConfigureAwait(true);");
        AssertContains(previewReinitializeControllerText, "_context.ReadReinitializeGeneration() != reinitializeGeneration");
        AssertContains(previewReinitializeControllerText, "await _context.AwaitWithTimeoutAsync(");
        AssertContains(previewReinitializeControllerText, "FlashbackCycleBeforeReinitializeTimeoutMs");
        AssertContains(previewReinitializeControllerText, "await _context.WaitReinitializeGateAsync();");
        AssertContains(previewReinitializeControllerText, "await _context.NotifyPreviewReinitRequestedAsync(reason);");
        AssertContains(previewReinitializeControllerText, "await _context.NotifyRendererStopAsync();");
        AssertContains(previewReinitializeControllerText, "await _previewLifecycleController.StopPreviewAsync(userInitiated: false, teardownPipeline: true, CancellationToken.None);");
        AssertContains(previewReinitializeControllerText, "await _previewLifecycleController.InitializeDeviceAsync();");
        AssertContains(previewReinitializeControllerText, "await _previewLifecycleController.StartPreviewAsync(userInitiated: false);");
        AssertContains(previewReinitializeControllerText, "_context.ReleaseReinitializeGate();");
        AssertDoesNotContain(previewStateText, "private async Task ReinitializeDeviceAsync(string reason)");
        AssertContains(rootText, "private Task ReinitializeDeviceAsync(string reason)");
        AssertContains(previewStateText, "public Task StartPreviewAsync(bool userInitiated = true, CancellationToken cancellationToken = default)");
        AssertContains(previewStateText, "public Task StopPreviewAsync(bool userInitiated, bool teardownPipeline, CancellationToken cancellationToken)");
        AssertContains(agentMapText, "`Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs`");
        AssertDoesNotContain(agentMapText, "`Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.cs`");
        AssertDoesNotContain(cleanupPlanText, "`MainViewModel.PreviewReinitialization.cs`");
        AssertContains(cleanupPlanText, "`Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs`");
        AssertDoesNotContain(cleanupPlanText, "`Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.cs`");

        return Task.CompletedTask;
    }
}
