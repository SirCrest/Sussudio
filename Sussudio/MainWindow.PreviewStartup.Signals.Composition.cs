using Sussudio.Controllers;

namespace Sussudio;

public sealed partial class MainWindow
{
    private PreviewStartupSignalCoordinator _previewStartupSignalCoordinator = null!;

    private void InitializePreviewStartupSignalCoordinator()
        => _previewStartupSignalCoordinator = new PreviewStartupSignalCoordinator(new PreviewStartupSignalCoordinatorContext
        {
            IsSignalWindowActive = IsPreviewStartupSignalWindowActive,
            IsFirstVisualConfirmed = () => IsPreviewFirstVisualConfirmed,
            GetAttemptLabel = () => PreviewStartupAttemptLabel,
            SetMissingSignals = value => PreviewStartupMissingSignals = value,
            Log = message => Logger.Log(message),
            ConfirmFirstVisual = ConfirmPreviewFirstVisual,
            GetPlaybackSnapshotState = GetPreviewStartupPlaybackSnapshotState
        });
}
