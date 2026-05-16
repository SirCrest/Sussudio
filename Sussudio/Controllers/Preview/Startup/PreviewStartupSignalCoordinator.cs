using System;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Controllers;

internal sealed class PreviewStartupSignalCoordinatorContext
{
    public required Func<bool> IsSignalWindowActive { get; init; }
    public required Func<bool> IsFirstVisualConfirmed { get; init; }
    public required Func<string> GetAttemptLabel { get; init; }
    public required Action<string?> SetMissingSignals { get; init; }
    public required Action<string> Log { get; init; }
    public required Action<string> ConfirmFirstVisual { get; init; }
    public required Func<PreviewStartupPlaybackSnapshotState> GetPlaybackSnapshotState { get; init; }
}

internal sealed record PreviewStartupPlaybackSnapshotState(
    bool RendererAvailable,
    bool RendererIsRendering,
    string GpuVisibility);

internal sealed class PreviewStartupSignalCoordinator
{
    private readonly PreviewStartupSignalCoordinatorContext _context;
    private readonly PreviewStartupReadinessSignalController _readinessSignals = new();
    private bool _expectGpuDualSignals;
    private long _positionEventCount;

    public PreviewStartupSignalCoordinator(PreviewStartupSignalCoordinatorContext context)
    {
        _context = context;
    }

    public PreviewStartupReadinessSignalSnapshot Snapshot => _readinessSignals.Snapshot;

    public long PositionEventCount => Interlocked.Read(ref _positionEventCount);

    public void Reset()
    {
        _expectGpuDualSignals = false;
        Interlocked.Exchange(ref _positionEventCount, 0);
        _readinessSignals.Reset();
    }

    public void Configure(PreviewStartupStrategy strategy, PreviewStartupSignalFlags requiredSignals)
    {
        _expectGpuDualSignals = false;
        Interlocked.Exchange(ref _positionEventCount, 0);
        var missingSignals = _readinessSignals.Configure(
            strategy,
            requiredSignals,
            _expectGpuDualSignals,
            _context.IsFirstVisualConfirmed());
        _context.SetMissingSignals(missingSignals);

        var snapshot = Snapshot;
        _context.Log(
            $"PREVIEW_START_STRATEGY attempt={_context.GetAttemptLabel()} " +
            $"strategy={snapshot.Strategy} required={PreviewStartupSignalFormatter.FormatSignalList(snapshot.RequiredSignals)}");
    }

    public string BuildMissingSignals()
        => _readinessSignals.BuildMissingSignals(_context.IsFirstVisualConfirmed());

    public void MarkFirstVisualConfirmed()
    {
        _readinessSignals.MarkFirstVisualConfirmed();
    }

    public void MarkGpuStartupSignal(PreviewStartupSignalFlags signal, string signalName)
    {
        var result = _readinessSignals.MarkSignal(
            signal,
            _context.IsSignalWindowActive(),
            _context.IsFirstVisualConfirmed());
        if (result.Status is PreviewStartupReadinessSignalStatus.IgnoredInactiveOrNotGpu or PreviewStartupReadinessSignalStatus.Duplicate)
        {
            return;
        }

        _context.SetMissingSignals(result.MissingSignals);
        _context.Log($"PREVIEW_START_SIGNAL signal={signalName} attempt={_context.GetAttemptLabel()}");
        LogPlaybackSnapshot($"signal:{signalName}");
        TryConfirmFirstVisualFromGpuSignals(result);
    }

    public void MarkGpuStartupSignalFirstFrame()
    {
        if (!_context.IsSignalWindowActive() || !_expectGpuDualSignals)
        {
            return;
        }

        MarkGpuStartupSignal(PreviewStartupSignalFlags.FirstCaptureFrame, "FirstCaptureFrame");
    }

    public void MarkGpuStartupSignalPlaybackAdvancing(TimeSpan position)
    {
        var result = _readinessSignals.TrackPlaybackPosition(
            position,
            _context.IsSignalWindowActive(),
            _context.IsFirstVisualConfirmed());
        if (result.Status == PreviewStartupPlaybackPositionStatus.IgnoredInactiveOrNotGpu)
        {
            _context.Log(
                $"PREVIEW_START_POSITION_IGNORED attempt={_context.GetAttemptLabel()} " +
                $"reason=inactive-or-not-gpu positionMs={position.TotalMilliseconds:0.###}");
            return;
        }

        if (result.Status == PreviewStartupPlaybackPositionStatus.BaselineCaptured)
        {
            _context.Log(
                $"PREVIEW_START_POSITION_BASELINE attempt={_context.GetAttemptLabel()} " +
                $"positionMs={result.Position.TotalMilliseconds:0.###} thresholdMs={result.Threshold.TotalMilliseconds:0.###}");
            HandleGpuStartupSignalResult(result.SignalResult, "PlaybackAdvancing");
            return;
        }

        _context.Log(
            $"PREVIEW_START_POSITION_CHECK attempt={_context.GetAttemptLabel()} " +
            $"positionMs={result.Position.TotalMilliseconds:0.###} deltaMs={result.Delta.TotalMilliseconds:0.###} " +
            $"thresholdMs={result.Threshold.TotalMilliseconds:0.###}");
        HandleGpuStartupSignalResult(result.SignalResult, "PlaybackAdvancing");
    }

    public void LogPlaybackSnapshot(string reason)
    {
        var snapshot = _context.GetPlaybackSnapshotState();
        if (!snapshot.RendererAvailable)
        {
            _context.Log(
                $"PREVIEW_START_PLAYBACK_SNAPSHOT attempt={_context.GetAttemptLabel()} " +
                $"reason={reason} renderer=null");
            return;
        }

        _context.Log(
            $"PREVIEW_START_PLAYBACK_SNAPSHOT attempt={_context.GetAttemptLabel()} " +
            $"reason={reason} state={(snapshot.RendererIsRendering ? "Rendering" : "Idle")} " +
            $"positionMs=0 " +
            $"gpuVisible={snapshot.GpuVisibility} " +
            $"required={PreviewStartupSignalFormatter.FormatSignalList(Snapshot.RequiredSignals)} " +
            $"received={PreviewStartupSignalFormatter.FormatSignalList(Snapshot.ReceivedSignals)} " +
            $"missing={BuildMissingSignals()}");
    }

    private void HandleGpuStartupSignalResult(PreviewStartupReadinessSignalResult? result, string signalName)
    {
        if (result == null || result.Status != PreviewStartupReadinessSignalStatus.Accepted)
        {
            return;
        }

        _context.SetMissingSignals(result.MissingSignals);
        _context.Log($"PREVIEW_START_SIGNAL signal={signalName} attempt={_context.GetAttemptLabel()}");
        LogPlaybackSnapshot($"signal:{signalName}");
        TryConfirmFirstVisualFromGpuSignals(result);
    }

    private void TryConfirmFirstVisualFromGpuSignals(PreviewStartupReadinessSignalResult result)
    {
        if (!_expectGpuDualSignals)
        {
            return;
        }

        if (!result.AllRequiredSignalsReceived)
        {
            var missing = result.Snapshot.RequiredSignals & ~result.Snapshot.ReceivedSignals;
            _context.Log(
                $"PREVIEW_START_WAITING attempt={_context.GetAttemptLabel()} " +
                $"required={PreviewStartupSignalFormatter.FormatSignalList(result.Snapshot.RequiredSignals)} " +
                $"received={PreviewStartupSignalFormatter.FormatSignalList(result.Snapshot.ReceivedSignals)} " +
                $"missing={PreviewStartupSignalFormatter.FormatSignalList(missing)}");
            return;
        }

        _context.ConfirmFirstVisual($"GpuStartupSignals({PreviewStartupSignalFormatter.FormatSignalList(result.Snapshot.RequiredSignals)})");
    }
}
