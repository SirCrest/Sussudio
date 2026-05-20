using System.Threading.Tasks;

static partial class Program
{
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
