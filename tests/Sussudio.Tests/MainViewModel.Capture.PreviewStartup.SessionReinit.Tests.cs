using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    private static Task PreviewStartupSessionReinitOwnership_LivesInFocusedControllers()
    {
        var mainWindowText = ReadRepoFile("Sussudio/MainWindow.xaml.cs")
            .Replace("\r\n", "\n");
        var previewStartupText = ReadRepoFile("Sussudio/MainWindow.PreviewStartup.cs")
            .Replace("\r\n", "\n");
        var previewStartupSessionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/Startup/PreviewStartupSessionController.cs")
            .Replace("\r\n", "\n");
        var previewReinitText = ReadRepoFile("Sussudio/MainWindow.PreviewTransitions.cs")
            .Replace("\r\n", "\n");
        var previewReinitTransitionControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewReinitTransitionController.cs")
            .Replace("\r\n", "\n");
        var previewRendererText = ReadRepoFile("Sussudio/MainWindow.PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var previewRuntimeSnapshotText = ReadRepoFile("Sussudio/MainWindow.PreviewRuntimeSnapshot.cs")
            .Replace("\r\n", "\n");

        AssertContains(mainWindowText, "InitializePreviewStartupSessionController();");
        AssertContains(mainWindowText, "InitializePreviewReinitTransitionController();");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewReinit.cs")),
            "preview reinit adapter is consolidated into the transition adapter");
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
        AssertContains(previewStartupSessionControllerText, "public bool ShouldBeginAttempt => string.IsNullOrWhiteSpace(AttemptId) || IsFailed || IsIdle;");
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
        AssertContains(previewRuntimeSnapshotText, "StartupState = CurrentPreviewStartupState.ToString(),");
        AssertContains(previewReinitText, "private PreviewReinitTransitionController _previewReinitTransitionController = null!;");
        AssertContains(previewReinitText, "private bool IsPreviewReinitAnimating");
        AssertContains(previewReinitText, "=> _previewReinitTransitionController.IsAnimating;");
        AssertContains(previewReinitText, "private async Task ViewModel_PreviewReinitRequested(string reason)");
        AssertContains(previewReinitText, "private Task ViewModel_PreviewRendererStopRequested()");
        AssertContains(previewReinitText, "private void HandlePreviewReinitializingChanged()");
        AssertContains(previewReinitTransitionControllerText, "internal sealed class PreviewReinitTransitionController");
        AssertContains(previewReinitTransitionControllerText, "public bool IsAnimating { get; private set; }");
        AssertContains(previewReinitTransitionControllerText, "public void BeginAnimateOut(string reason, string callerName)");
        AssertContains(previewReinitTransitionControllerText, "public PreviewReinitCompletionPresentation GetCompletionPresentation(");
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

        return Task.CompletedTask;
    }

    private static Task PreviewStartupSessionController_PreservesAttemptStateContracts()
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

        object State(string value) => Enum.Parse(stateType, value);

        AssertEqual(State("Idle"), GetPropertyValue(controller, "State"), "initial startup state");
        AssertEqual(true, GetBoolProperty(controller, "ShouldBeginAttempt"), "initial attempt gate");

        beginStartupAttempt.Invoke(controller, Array.Empty<object>());
        AssertEqual(State("StartingSession"), GetPropertyValue(controller, "State"), "state after begin attempt");
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
        AssertEqual(now.AddMilliseconds(100), GetPropertyValue(controller, "RendererAttachedUtc"), "renderer attached UTC");
        AssertEqual(true, markFirstVisualConfirmed.Invoke(controller, new object[] { now.AddMilliseconds(300) }), "first visual confirmation");
        AssertEqual(false, markFirstVisualConfirmed.Invoke(controller, new object[] { now.AddMilliseconds(400) }), "duplicate first visual suppressed");
        AssertEqual(true, GetBoolProperty(controller, "FirstVisualConfirmed"), "first visual confirmed flag");
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

    private static Task PreviewReinitTransitionController_PreservesTransitionStateContracts()
    {
        var controllerType = RequireType("Sussudio.Controllers.PreviewReinitTransitionController");
        var presentationType = RequireType("Sussudio.Controllers.PreviewReinitCompletionPresentation");
        var controller = Activator.CreateInstance(controllerType, nonPublic: true)!;
        var beginAnimateOut = controllerType.GetMethod("BeginAnimateOut")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.BeginAnimateOut was not found.");
        var getCompletionPresentation = controllerType.GetMethod("GetCompletionPresentation")
            ?? throw new InvalidOperationException("PreviewReinitTransitionController.GetCompletionPresentation was not found.");
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

        return Task.CompletedTask;
    }

    private static Task PreviewReinitialization_WaitsForPendingFlashbackCycle()
    {
        var viewModelFiles = ReadMainViewModelCodeFiles();
        var viewModelSharedStateText = viewModelFiles["MainViewModel.State.cs"];
        var viewModelPreviewStateText = viewModelFiles["MainViewModel.PreviewState.cs"];
        var viewModelCaptureStateText = viewModelFiles["MainViewModel.CaptureState.cs"];
        var viewModelFlashbackStateText = viewModelFiles["MainViewModel.FlashbackState.cs"];
        var rawPreviewLifecycleControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewLifecycleController.cs")
            .Replace("\r\n", "\n");
        var rawPreviewReinitializeControllerText = ReadRepoFile("Sussudio/Controllers/ViewModel/MainViewModelPreviewReinitializeController.cs")
            .Replace("\r\n", "\n");

        AssertContains(viewModelFlashbackStateText, "private const int FlashbackCycleBeforeReinitializeTimeoutMs = 30000;");
        AssertContains(viewModelCaptureStateText, "private const int PreviewReinitializeDebounceMs = 250;");
        AssertContains(viewModelPreviewStateText, "private int _previewReinitializeGeneration;");
        AssertDoesNotContain(viewModelSharedStateText, "private int _previewReinitializeGeneration;");
        AssertContains(viewModelFiles["MainViewModel.cs"], "=> _previewLifecycleController.ReinitializeDeviceAsync(reason);");
        AssertContains(rawPreviewLifecycleControllerText, "=> _previewReinitializeController.ReinitializeDeviceAsync(reason);");
        AssertContains(rawPreviewReinitializeControllerText, "var reinitializeGeneration = Interlocked.Increment(ref _viewModel._previewReinitializeGeneration);");
        AssertContains(rawPreviewReinitializeControllerText, "await Task.Delay(PreviewReinitializeDebounceMs).ConfigureAwait(true);");
        AssertContains(rawPreviewReinitializeControllerText, "Volatile.Read(ref _viewModel._previewReinitializeGeneration) != reinitializeGeneration");
        AssertContains(rawPreviewReinitializeControllerText, "REINIT_COALESCED reason='{reason}' generation={reinitializeGeneration}");
        AssertContains(rawPreviewReinitializeControllerText, "await AwaitWithTimeoutAsync(");
        AssertContains(rawPreviewReinitializeControllerText, "\"Flashback encoder settings cycle before reinitialize\").ConfigureAwait(false);");
        AssertContains(rawPreviewReinitializeControllerText, "REINIT_WAIT_FLASHBACK_CYCLE_TIMEOUT reason={reason} timeoutMs={FlashbackCycleBeforeReinitializeTimeoutMs}");
        AssertContains(rawPreviewReinitializeControllerText, "REINIT_WAIT_FLASHBACK_CYCLE_FAULT");
        AssertContains(rawPreviewReinitializeControllerText, "if (ReferenceEquals(_viewModel._pendingFlashbackCycleTask, pendingCycle) && pendingCycle.IsCompleted)\n                {\n                    _viewModel._pendingFlashbackCycleTask = null;\n                }");

        return Task.CompletedTask;
    }
}
