using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

static partial class Program
{
    private static Task PreviewStartupSessionController_PreservesAttemptStateContracts()
    {
        var controllerType = RequireType("Sussudio.Controllers.PreviewStartupSessionController");
        var stateType = RequireType("Sussudio.Controllers.PreviewStartupState");
        var controller = Activator.CreateInstance(controllerType, nonPublic: true)!;
        var beginAttempt = controllerType.GetMethod("BeginAttempt")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.BeginAttempt was not found.");
        var setState = controllerType.GetMethod("SetState")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.SetState was not found.");
        var markRendererAttached = controllerType.GetMethod("MarkRendererAttached")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.MarkRendererAttached was not found.");
        var markFirstVisualConfirmed = controllerType.GetMethod("MarkFirstVisualConfirmed")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.MarkFirstVisualConfirmed was not found.");
        var setMissingSignals = controllerType.GetMethod("SetMissingSignals")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.SetMissingSignals was not found.");
        var reset = controllerType.GetMethod("Reset")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.Reset was not found.");
        var getElapsedMilliseconds = controllerType.GetMethod("GetElapsedMilliseconds")
            ?? throw new InvalidOperationException("PreviewStartupSessionController.GetElapsedMilliseconds was not found.");

        object State(string value) => Enum.Parse(stateType, value);

        var requestedUtc = new DateTimeOffset(2026, 5, 15, 12, 0, 0, TimeSpan.Zero);
        AssertEqual(State("Idle"), GetPropertyValue(controller, "State"), "initial startup state");
        AssertEqual(true, GetBoolProperty(controller, "ShouldBeginAttempt"), "initial attempt gate");

        AssertEqual(true, beginAttempt.Invoke(controller, new object[] { "attempt-1", requestedUtc }), "begin attempt changes state");
        AssertEqual(State("StartingSession"), GetPropertyValue(controller, "State"), "state after begin attempt");
        AssertEqual("attempt-1", GetStringProperty(controller, "AttemptId"), "attempt id after begin");
        AssertEqual(requestedUtc, GetPropertyValue(controller, "RequestedUtc"), "requested UTC after begin");
        AssertEqual(false, GetBoolProperty(controller, "FirstVisualConfirmed"), "first visual reset on begin");
        AssertEqual(false, GetBoolProperty(controller, "ShouldBeginAttempt"), "active attempt gate");
        AssertEqual(1250.0, getElapsedMilliseconds.Invoke(controller, new object[] { requestedUtc.AddMilliseconds(1250) }), "elapsed milliseconds");

        AssertEqual(false, setState.Invoke(controller, new object?[] { State("StartingSession"), null }), "duplicate state without reason suppresses log");
        AssertEqual(true, setState.Invoke(controller, new object?[] { State("Failed"), "renderer-attach-failed:test" }), "failed state with reason changes state");
        AssertEqual(State("Failed"), GetPropertyValue(controller, "State"), "failed state");
        AssertEqual("renderer-attach-failed:test", GetStringProperty(controller, "LastFailureReason"), "failure reason retained");
        AssertEqual(true, GetBoolProperty(controller, "ShouldBeginAttempt"), "failed attempt gate");
        AssertEqual(false, reset.Invoke(controller, new object[] { false }), "terminal reset does not require idle log");
        AssertEqual(State("Idle"), GetPropertyValue(controller, "State"), "terminal reset returns idle");
        AssertEqual(string.Empty, GetStringProperty(controller, "AttemptId"), "terminal reset clears attempt id");

        beginAttempt.Invoke(controller, new object[] { "attempt-2", requestedUtc });
        AssertEqual(true, setState.Invoke(controller, new object?[] { State("WaitingForFirstVisual"), null }), "waiting state changes state");
        setMissingSignals.Invoke(controller, new object?[] { "FirstVisual" });
        markRendererAttached.Invoke(controller, new object[] { requestedUtc.AddMilliseconds(100) });
        AssertEqual(requestedUtc.AddMilliseconds(100), GetPropertyValue(controller, "RendererAttachedUtc"), "renderer attached UTC");
        AssertEqual(true, markFirstVisualConfirmed.Invoke(controller, new object[] { requestedUtc.AddMilliseconds(300) }), "first visual confirmation");
        AssertEqual(false, markFirstVisualConfirmed.Invoke(controller, new object[] { requestedUtc.AddMilliseconds(400) }), "duplicate first visual suppressed");
        AssertEqual(true, GetBoolProperty(controller, "FirstVisualConfirmed"), "first visual confirmed flag");
        AssertEqual(requestedUtc.AddMilliseconds(300), GetPropertyValue(controller, "FirstVisualUtc"), "first visual UTC");
        AssertEqual("FirstVisual", GetStringProperty(controller, "MissingSignals"), "missing signals cached until adapter clears them");
        AssertEqual(true, reset.Invoke(controller, new object[] { false }), "nonterminal reset requires idle log");
        AssertEqual(State("Idle"), GetPropertyValue(controller, "State"), "nonterminal reset returns idle");
        AssertEqual(string.Empty, GetStringProperty(controller, "MissingSignals"), "nonterminal reset clears missing signals");

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
}
