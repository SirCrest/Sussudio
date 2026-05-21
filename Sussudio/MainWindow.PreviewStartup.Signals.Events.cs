using System;
using Sussudio.Controllers;
using Sussudio.Models;

namespace Sussudio;

public sealed partial class MainWindow
{
    private bool IsPreviewStartupSignalWindowActive()
        => _previewStartupSessionController.IsSignalWindowActive(ViewModel.IsPreviewing);

    private void ResetPreviewSignalState()
        => _previewStartupSignalCoordinator.Reset();

    private void ConfigurePreviewStartupSignals(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)
        => _previewStartupSignalCoordinator.Configure(strategy, requiredSignals);

    private string BuildPreviewStartupMissingSignals()
        => _previewStartupSignalCoordinator.BuildMissingSignals();

    private void MarkPreviewStartupFirstVisualConfirmed()
        => _previewStartupSignalCoordinator.MarkFirstVisualConfirmed();

    private void MarkGpuStartupSignal(PreviewStartupSignalFlags signal, string signalName)
        => _previewStartupSignalCoordinator.MarkGpuStartupSignal(signal, signalName);

    private void MarkGpuStartupSignalMediaOpened()
        => MarkGpuStartupSignal(PreviewStartupSignalFlags.MediaOpened, "MediaOpened");

    private void MarkGpuStartupSignalFirstFrame()
        => _previewStartupSignalCoordinator.MarkGpuStartupSignalFirstFrame();

    private void MarkGpuStartupSignalPlaybackAdvancing(TimeSpan position)
        => _previewStartupSignalCoordinator.MarkGpuStartupSignalPlaybackAdvancing(position);

    private void LogPreviewStartupPlaybackSnapshot(string reason)
        => _previewStartupSignalCoordinator.LogPlaybackSnapshot(reason);

    private PreviewStartupPlaybackSnapshotState GetPreviewStartupPlaybackSnapshotState()
    {
        var renderer = _previewRendererHostController.Renderer;
        return new PreviewStartupPlaybackSnapshotState(
            renderer != null,
            renderer?.IsRendering == true,
            PreviewSwapChainPanel.Visibility.ToString());
    }
}
