using System.Threading.Tasks;

static partial class Program
{
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
}
