using Microsoft.UI.Xaml;
using Sussudio.Controllers;
using Sussudio.Models;

namespace Sussudio;

// UI-thread automation/runtime snapshot sampling for diagnostics and MCP/CLI callers.
public sealed partial class MainWindow
{
    private PreviewRuntimeSnapshot GetPreviewRuntimeSnapshot()
    {
        var startupMissingSignals = PreviewStartupMissingSignals;
        if (string.IsNullOrWhiteSpace(startupMissingSignals) &&
            CurrentPreviewStartupState is PreviewStartupState.WaitingForFirstVisual or PreviewStartupState.Failed)
        {
            startupMissingSignals = BuildPreviewStartupMissingSignals();
        }

        return PreviewRuntimeSnapshotController.Build(new PreviewRuntimeSnapshotInput
        {
            D3DRenderer = _previewRendererHostController.Renderer,
            IsPreviewing = ViewModel.IsPreviewing,
            PreviewSourceAttached = _previewRendererHostController.IsCpuPreviewSourceAttached,
            GpuElementVisible = PreviewSwapChainPanel.Visibility == Visibility.Visible,
            CpuElementVisible = PreviewImage.Visibility == Visibility.Visible,
            PlaceholderVisible = NoDevicePlaceholder.Visibility == Visibility.Visible,
            FramesArrived = _previewRendererHostController.FramesArrived,
            FramesDisplayed = _previewRendererHostController.FramesDisplayed,
            FramesDropped = _previewRendererHostController.FramesDropped,
            LastPresentedTick = _previewRendererHostController.LastPresentedTick,
            PreviewMinPresentationIntervalMs = _previewRendererHostController.PreviewMinPresentationIntervalMs,
            StartupState = CurrentPreviewStartupState.ToString(),
            IsStartupWaitingForFirstVisual = CurrentPreviewStartupState == PreviewStartupState.WaitingForFirstVisual,
            StartupAttemptId = PreviewStartupAttemptId,
            StartupRequestedUtc = PreviewStartupRequestedUtc,
            StartupTimeoutMs = PreviewStartupVisualTimeoutMs,
            StartupGpuSignalMediaOpened = _previewGpuSignalMediaOpened,
            StartupGpuSignalFirstFrame = _previewGpuSignalFirstFrame,
            StartupGpuSignalPlaybackAdvancing = _previewGpuSignalPlaybackAdvancing,
            StartupRequiredSignals = _previewStartupRequiredSignals,
            StartupReceivedSignals = _previewStartupReceivedSignals,
            StartupStrategy = _previewStartupStrategy,
            StartupMissingSignals = startupMissingSignals,
            StartupRecoveryAttemptCount = PreviewStartupRecoveryAttemptCount,
            StartupLastFailureReason = PreviewStartupLastFailureReason,
            FirstVisualConfirmed = IsPreviewFirstVisualConfirmed,
            GpuPositionEventCount = PreviewStartupGpuPositionEventCount
        });
    }
}
