using System;
using System.Threading;
using Sussudio.Models;
using Sussudio.Controllers;

namespace Sussudio;

// Preview startup readiness-signal tracking. The watchdog and fade-in timers
// stay in MainWindow.PreviewStartup.cs; this partial owns signal collection
// and playback-progress diagnostics.
public sealed partial class MainWindow
{
    private static readonly TimeSpan PreviewStartupPlaybackAdvanceThreshold = TimeSpan.FromMilliseconds(33);

    private bool _previewStartupExpectGpuDualSignals;
    private bool _previewGpuSignalMediaOpened;
    private bool _previewGpuSignalFirstFrame;
    private bool _previewGpuSignalPlaybackAdvancing;
    private PreviewStartupSignalFlags _previewStartupRequiredSignals = PreviewStartupSignalFlags.None;
    private PreviewStartupSignalFlags _previewStartupReceivedSignals = PreviewStartupSignalFlags.None;
    private PreviewStartupStrategy _previewStartupStrategy = PreviewStartupStrategy.None;
    private TimeSpan _previewStartupLastPlaybackPosition = TimeSpan.Zero;
    private long _previewStartupPositionEventCount;
    private bool _previewStartupPlaybackPositionInitialized;
    private long _previewStartupLastPositionDispatchTick;

    private bool IsPreviewStartupSignalWindowActive()
        => ViewModel.IsPreviewing &&
           !_previewFirstVisualConfirmed &&
           _previewStartupState is PreviewStartupState.StartingSession or PreviewStartupState.RendererAttaching or PreviewStartupState.WaitingForFirstVisual;

    private void ResetPreviewSignalState()
    {
        _previewStartupExpectGpuDualSignals = false;
        _previewGpuSignalMediaOpened = false;
        _previewGpuSignalFirstFrame = false;
        _previewGpuSignalPlaybackAdvancing = false;
        _previewStartupRequiredSignals = PreviewStartupSignalFlags.None;
        _previewStartupReceivedSignals = PreviewStartupSignalFlags.None;
        _previewStartupStrategy = PreviewStartupStrategy.None;
        _previewStartupLastPlaybackPosition = TimeSpan.Zero;
        _previewStartupPositionEventCount = 0;
        _previewStartupPlaybackPositionInitialized = false;
        Interlocked.Exchange(ref _previewStartupLastPositionDispatchTick, 0);
    }

    private void ConfigurePreviewStartupSignals(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)
    {
        ResetPreviewSignalState();
        _previewStartupStrategy = strategy;
        _previewStartupRequiredSignals = requiredSignals;
        _previewStartupMissingSignals = BuildPreviewStartupMissingSignals();
        Interlocked.Exchange(ref _previewStartupLastPositionDispatchTick, 0);

        Logger.Log(
            $"PREVIEW_START_STRATEGY attempt={_previewStartupAttemptId ?? "none"} " +
            $"strategy={_previewStartupStrategy} required={PreviewStartupSignalFormatter.FormatSignalList(_previewStartupRequiredSignals)}");
    }

    private string BuildPreviewStartupMissingSignals()
        => PreviewStartupSignalFormatter.FormatMissingSignals(
            _previewStartupRequiredSignals,
            _previewStartupReceivedSignals,
            _previewFirstVisualConfirmed);

    private void MarkGpuStartupSignal(PreviewStartupSignalFlags signal, string signalName)
    {
        if (!IsPreviewStartupSignalWindowActive() || !_previewStartupExpectGpuDualSignals)
        {
            return;
        }

        if ((_previewStartupReceivedSignals & signal) != 0)
        {
            return;
        }

        _previewStartupReceivedSignals |= signal;
        if (signal == PreviewStartupSignalFlags.MediaOpened)
        {
            _previewGpuSignalMediaOpened = true;
        }
        else if (signal == PreviewStartupSignalFlags.FirstCaptureFrame)
        {
            _previewGpuSignalFirstFrame = true;
        }
        else if (signal == PreviewStartupSignalFlags.PlaybackAdvancing)
        {
            _previewGpuSignalPlaybackAdvancing = true;
        }

        _previewStartupMissingSignals = BuildPreviewStartupMissingSignals();
        Logger.Log($"PREVIEW_START_SIGNAL signal={signalName} attempt={_previewStartupAttemptId ?? "none"}");
        LogPreviewStartupPlaybackSnapshot($"signal:{signalName}");
        TryConfirmPreviewFirstVisualFromGpuSignals();
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
        if (!IsPreviewStartupSignalWindowActive() || !_previewStartupExpectGpuDualSignals)
        {
            Logger.Log(
                $"PREVIEW_START_POSITION_IGNORED attempt={_previewStartupAttemptId ?? "none"} " +
                $"reason=inactive-or-not-gpu positionMs={position.TotalMilliseconds:0.###}");
            return;
        }

        if (!_previewStartupPlaybackPositionInitialized)
        {
            _previewStartupPlaybackPositionInitialized = true;
            _previewStartupLastPlaybackPosition = position;
            Logger.Log(
                $"PREVIEW_START_POSITION_BASELINE attempt={_previewStartupAttemptId ?? "none"} " +
                $"positionMs={position.TotalMilliseconds:0.###} thresholdMs={PreviewStartupPlaybackAdvanceThreshold.TotalMilliseconds:0.###}");
            if (position >= PreviewStartupPlaybackAdvanceThreshold)
            {
                MarkGpuStartupSignal(PreviewStartupSignalFlags.PlaybackAdvancing, "PlaybackAdvancing");
            }

            return;
        }

        var delta = position - _previewStartupLastPlaybackPosition;
        if (position > _previewStartupLastPlaybackPosition)
        {
            _previewStartupLastPlaybackPosition = position;
        }

        Logger.Log(
            $"PREVIEW_START_POSITION_CHECK attempt={_previewStartupAttemptId ?? "none"} " +
            $"positionMs={position.TotalMilliseconds:0.###} deltaMs={delta.TotalMilliseconds:0.###} " +
            $"thresholdMs={PreviewStartupPlaybackAdvanceThreshold.TotalMilliseconds:0.###}");
        if (position >= PreviewStartupPlaybackAdvanceThreshold || delta >= PreviewStartupPlaybackAdvanceThreshold)
        {
            MarkGpuStartupSignal(PreviewStartupSignalFlags.PlaybackAdvancing, "PlaybackAdvancing");
        }
    }

    private void TryConfirmPreviewFirstVisualFromGpuSignals()
    {
        if (!_previewStartupExpectGpuDualSignals)
        {
            return;
        }

        var missing = _previewStartupRequiredSignals & ~_previewStartupReceivedSignals;
        if (missing != PreviewStartupSignalFlags.None)
        {
            Logger.Log(
                $"PREVIEW_START_WAITING attempt={_previewStartupAttemptId ?? "none"} " +
                $"required={PreviewStartupSignalFormatter.FormatSignalList(_previewStartupRequiredSignals)} " +
                $"received={PreviewStartupSignalFormatter.FormatSignalList(_previewStartupReceivedSignals)} " +
                $"missing={PreviewStartupSignalFormatter.FormatSignalList(missing)}");
            return;
        }

        ConfirmPreviewFirstVisual($"GpuStartupSignals({PreviewStartupSignalFormatter.FormatSignalList(_previewStartupRequiredSignals)})");
    }

    private void LogPreviewStartupPlaybackSnapshot(string reason)
    {
        var renderer = _d3dRenderer;
        if (renderer == null)
        {
            Logger.Log(
                $"PREVIEW_START_PLAYBACK_SNAPSHOT attempt={_previewStartupAttemptId ?? "none"} " +
                $"reason={reason} renderer=null");
            return;
        }

        Logger.Log(
            $"PREVIEW_START_PLAYBACK_SNAPSHOT attempt={_previewStartupAttemptId ?? "none"} " +
            $"reason={reason} state={(renderer.IsRendering ? "Rendering" : "Idle")} " +
            $"positionMs=0 " +
            $"gpuVisible={PreviewSwapChainPanel.Visibility} " +
            $"required={PreviewStartupSignalFormatter.FormatSignalList(_previewStartupRequiredSignals)} " +
            $"received={PreviewStartupSignalFormatter.FormatSignalList(_previewStartupReceivedSignals)} " +
            $"missing={BuildPreviewStartupMissingSignals()}");
    }
}
