using System.Reflection;
using System.Threading.Tasks;

static partial class Program
{
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
}
