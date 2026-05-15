using System;
using Sussudio.Models;
using Sussudio.Controllers;

namespace Sussudio;

// Preview startup readiness-signal tracking. Watchdog recovery and fade-in
// timers live in their own partials; this partial owns signal collection and
// playback-progress diagnostics.
public sealed partial class MainWindow
{
    private readonly PreviewStartupReadinessSignalController _previewStartupReadinessSignals = new();
    private bool _previewStartupExpectGpuDualSignals;
    private long _previewStartupPositionEventCount;

    private bool _previewGpuSignalMediaOpened => _previewStartupReadinessSignals.Snapshot.GpuSignalMediaOpened;
    private bool _previewGpuSignalFirstFrame => _previewStartupReadinessSignals.Snapshot.GpuSignalFirstFrame;
    private bool _previewGpuSignalPlaybackAdvancing => _previewStartupReadinessSignals.Snapshot.GpuSignalPlaybackAdvancing;
    private PreviewStartupSignalFlags _previewStartupRequiredSignals => _previewStartupReadinessSignals.Snapshot.RequiredSignals;
    private PreviewStartupSignalFlags _previewStartupReceivedSignals => _previewStartupReadinessSignals.Snapshot.ReceivedSignals;
    private PreviewStartupStrategy _previewStartupStrategy => _previewStartupReadinessSignals.Snapshot.Strategy;

    private bool IsPreviewStartupSignalWindowActive()
        => ViewModel.IsPreviewing &&
           !IsPreviewFirstVisualConfirmed &&
           CurrentPreviewStartupState is PreviewStartupState.StartingSession or PreviewStartupState.RendererAttaching or PreviewStartupState.WaitingForFirstVisual;

    private void ResetPreviewSignalState()
    {
        _previewStartupExpectGpuDualSignals = false;
        _previewStartupPositionEventCount = 0;
        _previewStartupReadinessSignals.Reset();
    }

    private void ConfigurePreviewStartupSignals(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)
    {
        _previewStartupExpectGpuDualSignals = false;
        _previewStartupPositionEventCount = 0;
        PreviewStartupMissingSignals = _previewStartupReadinessSignals.Configure(
            strategy,
            requiredSignals,
            _previewStartupExpectGpuDualSignals,
            IsPreviewFirstVisualConfirmed);

        Logger.Log(
            $"PREVIEW_START_STRATEGY attempt={PreviewStartupAttemptLabel} " +
            $"strategy={_previewStartupStrategy} required={PreviewStartupSignalFormatter.FormatSignalList(_previewStartupRequiredSignals)}");
    }

    private string BuildPreviewStartupMissingSignals()
        => _previewStartupReadinessSignals.BuildMissingSignals(IsPreviewFirstVisualConfirmed);

    private void MarkPreviewStartupFirstVisualConfirmed()
    {
        _previewStartupReadinessSignals.MarkFirstVisualConfirmed();
    }

    private void MarkGpuStartupSignal(PreviewStartupSignalFlags signal, string signalName)
    {
        var result = _previewStartupReadinessSignals.MarkSignal(
            signal,
            IsPreviewStartupSignalWindowActive(),
            IsPreviewFirstVisualConfirmed);
        if (result.Status is PreviewStartupReadinessSignalStatus.IgnoredInactiveOrNotGpu or PreviewStartupReadinessSignalStatus.Duplicate)
        {
            return;
        }

        PreviewStartupMissingSignals = result.MissingSignals;
        Logger.Log($"PREVIEW_START_SIGNAL signal={signalName} attempt={PreviewStartupAttemptLabel}");
        LogPreviewStartupPlaybackSnapshot($"signal:{signalName}");
        TryConfirmPreviewFirstVisualFromGpuSignals(result);
    }

    private void MarkGpuStartupSignalMediaOpened()
    {
        MarkGpuStartupSignal(PreviewStartupSignalFlags.MediaOpened, "MediaOpened");
    }

    private void MarkGpuStartupSignalFirstFrame()
    {
        if (!IsPreviewStartupSignalWindowActive() || !_previewStartupExpectGpuDualSignals)
        {
            return;
        }

        MarkGpuStartupSignal(PreviewStartupSignalFlags.FirstCaptureFrame, "FirstCaptureFrame");
    }

    private void MarkGpuStartupSignalPlaybackAdvancing(TimeSpan position)
    {
        var result = _previewStartupReadinessSignals.TrackPlaybackPosition(
            position,
            IsPreviewStartupSignalWindowActive(),
            IsPreviewFirstVisualConfirmed);
        if (result.Status == PreviewStartupPlaybackPositionStatus.IgnoredInactiveOrNotGpu)
        {
            Logger.Log(
                $"PREVIEW_START_POSITION_IGNORED attempt={PreviewStartupAttemptLabel} " +
                $"reason=inactive-or-not-gpu positionMs={position.TotalMilliseconds:0.###}");
            return;
        }

        if (result.Status == PreviewStartupPlaybackPositionStatus.BaselineCaptured)
        {
            Logger.Log(
                $"PREVIEW_START_POSITION_BASELINE attempt={PreviewStartupAttemptLabel} " +
                $"positionMs={result.Position.TotalMilliseconds:0.###} thresholdMs={result.Threshold.TotalMilliseconds:0.###}");
            HandleGpuStartupSignalResult(result.SignalResult, "PlaybackAdvancing");
            return;
        }

        Logger.Log(
            $"PREVIEW_START_POSITION_CHECK attempt={PreviewStartupAttemptLabel} " +
            $"positionMs={result.Position.TotalMilliseconds:0.###} deltaMs={result.Delta.TotalMilliseconds:0.###} " +
            $"thresholdMs={result.Threshold.TotalMilliseconds:0.###}");
        HandleGpuStartupSignalResult(result.SignalResult, "PlaybackAdvancing");
    }

    private void HandleGpuStartupSignalResult(PreviewStartupReadinessSignalResult? result, string signalName)
    {
        if (result == null || result.Status != PreviewStartupReadinessSignalStatus.Accepted)
        {
            return;
        }

        PreviewStartupMissingSignals = result.MissingSignals;
        Logger.Log($"PREVIEW_START_SIGNAL signal={signalName} attempt={PreviewStartupAttemptLabel}");
        LogPreviewStartupPlaybackSnapshot($"signal:{signalName}");
        TryConfirmPreviewFirstVisualFromGpuSignals(result);
    }

    private void TryConfirmPreviewFirstVisualFromGpuSignals(PreviewStartupReadinessSignalResult result)
    {
        if (!_previewStartupExpectGpuDualSignals)
        {
            return;
        }

        if (!result.AllRequiredSignalsReceived)
        {
            var missing = result.Snapshot.RequiredSignals & ~result.Snapshot.ReceivedSignals;
            Logger.Log(
                $"PREVIEW_START_WAITING attempt={PreviewStartupAttemptLabel} " +
                $"required={PreviewStartupSignalFormatter.FormatSignalList(result.Snapshot.RequiredSignals)} " +
                $"received={PreviewStartupSignalFormatter.FormatSignalList(result.Snapshot.ReceivedSignals)} " +
                $"missing={PreviewStartupSignalFormatter.FormatSignalList(missing)}");
            return;
        }

        ConfirmPreviewFirstVisual($"GpuStartupSignals({PreviewStartupSignalFormatter.FormatSignalList(result.Snapshot.RequiredSignals)})");
    }

    private void LogPreviewStartupPlaybackSnapshot(string reason)
    {
        var renderer = _d3dRenderer;
        if (renderer == null)
        {
            Logger.Log(
                $"PREVIEW_START_PLAYBACK_SNAPSHOT attempt={PreviewStartupAttemptLabel} " +
                $"reason={reason} renderer=null");
            return;
        }

        Logger.Log(
            $"PREVIEW_START_PLAYBACK_SNAPSHOT attempt={PreviewStartupAttemptLabel} " +
            $"reason={reason} state={(renderer.IsRendering ? "Rendering" : "Idle")} " +
            $"positionMs=0 " +
            $"gpuVisible={PreviewSwapChainPanel.Visibility} " +
            $"required={PreviewStartupSignalFormatter.FormatSignalList(_previewStartupRequiredSignals)} " +
            $"received={PreviewStartupSignalFormatter.FormatSignalList(_previewStartupReceivedSignals)} " +
            $"missing={BuildPreviewStartupMissingSignals()}");
    }
}
