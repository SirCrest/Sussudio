using System.Threading.Tasks;
using Sussudio.Controllers;

namespace Sussudio;

// Preview-specific ViewModel event adapter. Preview startup/teardown
// choreography and its PropertyChanged routes live in the controller.
public sealed partial class MainWindow
{
    private PreviewLifecycleEventController _previewLifecycleEventController = null!;

    private void InitializePreviewLifecycleEventController()
    {
        _previewLifecycleEventController = new PreviewLifecycleEventController(new PreviewLifecycleEventControllerContext
        {
            ViewModel = ViewModel,
            ShouldBeginPreviewStartupAttempt = () => ShouldBeginPreviewStartupAttempt,
            BeginPreviewStartupAttempt = BeginPreviewStartupAttempt,
            PrimePreviewAudioFadeIn = PrimePreviewAudioFadeIn,
            IsPreviewReinitAnimating = () => IsPreviewReinitAnimating,
            PreparePreviewStartupPresentation = PreparePreviewStartupPresentation,
            StopPreviewStartupWatchdog = StopPreviewStartupWatchdog,
            StartPreviewStartupWatchdog = StartPreviewStartupWatchdog,
            StopPreviewStartupOverlay = StopPreviewStartupOverlay,
            SetPreviewStartupState = SetPreviewStartupState,
            GetPreviewStartupAttemptLabel = () => PreviewStartupAttemptLabel,
            StartPreviewRendererAsync = StartPreviewRendererAsync,
            IsPreviewFirstVisualConfirmed = () => IsPreviewFirstVisualConfirmed,
            RevealPreviewUnavailablePlaceholder = RevealPreviewUnavailablePlaceholder,
            SchedulePreviewStartupFailureStop = SchedulePreviewStartupFailureStop,
            ShowStopPreviewButtonPresentation = ShowStopPreviewButtonPresentation,
            ShowStartPreviewButtonPresentation = ShowStartPreviewButtonPresentation,
            ApplyHdrToggleEnabledState = ApplyHdrToggleEnabledState,
            StopPreviewRendererAsync = StopPreviewRendererAsync,
            ResetPreviewStartupTracking = preserveReinitAnimation => ResetPreviewStartupTracking(
                preserveReinitAnimation: preserveReinitAnimation),
            HandlePreviewReinitializingChanged = HandlePreviewReinitializingChanged,
        });
    }

    private bool IsPreviewStopRequestedByUser
        => _previewLifecycleEventController.StopRequestedByUser;

    private void SetPreviewStopRequestedByUser(bool value)
        => _previewLifecycleEventController.SetStopRequestedByUser(value);

    private Task<bool> TryHandlePreviewPropertyChangedAsync(string propertyName)
        => _previewLifecycleEventController.TryHandlePropertyChangedAsync(propertyName);

    private void ViewModel_PreviewStartRequested(object? sender, System.EventArgs e)
        => _previewLifecycleEventController.HandlePreviewStartRequested();

    private void ViewModel_PreviewStopRequested(object? sender, System.EventArgs e)
        => _previewLifecycleEventController.HandlePreviewStopRequested();
}
