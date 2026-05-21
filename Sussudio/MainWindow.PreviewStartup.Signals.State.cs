using Sussudio.Controllers;
using Sussudio.Models;

namespace Sussudio;

public sealed partial class MainWindow
{
    private PreviewStartupReadinessSignalSnapshot PreviewStartupSignalSnapshot
        => _previewStartupSignalCoordinator.Snapshot;

    private bool _previewGpuSignalMediaOpened => PreviewStartupSignalSnapshot.GpuSignalMediaOpened;
    private bool _previewGpuSignalFirstFrame => PreviewStartupSignalSnapshot.GpuSignalFirstFrame;
    private bool _previewGpuSignalPlaybackAdvancing => PreviewStartupSignalSnapshot.GpuSignalPlaybackAdvancing;
    private PreviewStartupSignalFlags _previewStartupRequiredSignals => PreviewStartupSignalSnapshot.RequiredSignals;
    private PreviewStartupSignalFlags _previewStartupReceivedSignals => PreviewStartupSignalSnapshot.ReceivedSignals;
    private PreviewStartupStrategy _previewStartupStrategy => PreviewStartupSignalSnapshot.Strategy;
    private long PreviewStartupGpuPositionEventCount => _previewStartupSignalCoordinator.PositionEventCount;
}
