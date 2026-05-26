using System;
using System.Collections.Generic;
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

internal sealed class PreviewStartupReadinessSignalController
{
    public static readonly TimeSpan PlaybackAdvanceThreshold = TimeSpan.FromMilliseconds(33);

    private bool _expectGpuDualSignals;
    private bool _gpuSignalMediaOpened;
    private bool _gpuSignalFirstFrame;
    private bool _gpuSignalPlaybackAdvancing;
    private PreviewStartupSignalFlags _requiredSignals = PreviewStartupSignalFlags.None;
    private PreviewStartupSignalFlags _receivedSignals = PreviewStartupSignalFlags.None;
    private PreviewStartupStrategy _strategy = PreviewStartupStrategy.None;
    private TimeSpan _lastPlaybackPosition = TimeSpan.Zero;
    private bool _playbackPositionInitialized;

    public PreviewStartupReadinessSignalSnapshot Snapshot => new(
        _expectGpuDualSignals,
        _gpuSignalMediaOpened,
        _gpuSignalFirstFrame,
        _gpuSignalPlaybackAdvancing,
        _requiredSignals,
        _receivedSignals,
        _strategy);

    public string Configure(
        PreviewStartupStrategy strategy,
        PreviewStartupSignalFlags requiredSignals,
        bool expectGpuDualSignals,
        bool firstVisualConfirmed)
    {
        Reset();
        _expectGpuDualSignals = expectGpuDualSignals;
        _strategy = strategy;
        _requiredSignals = requiredSignals;

        return BuildMissingSignals(firstVisualConfirmed);
    }

    public void Reset()
    {
        _expectGpuDualSignals = false;
        _gpuSignalMediaOpened = false;
        _gpuSignalFirstFrame = false;
        _gpuSignalPlaybackAdvancing = false;
        _requiredSignals = PreviewStartupSignalFlags.None;
        _receivedSignals = PreviewStartupSignalFlags.None;
        _strategy = PreviewStartupStrategy.None;
        _lastPlaybackPosition = TimeSpan.Zero;
        _playbackPositionInitialized = false;
    }

    public void MarkFirstVisualConfirmed()
    {
        _receivedSignals |= PreviewStartupSignalFlags.FirstVisual;
    }

    public string BuildMissingSignals(bool firstVisualConfirmed)
        => PreviewStartupSignalFormatter.FormatMissingSignals(
            _requiredSignals,
            _receivedSignals,
            firstVisualConfirmed);

    public PreviewStartupReadinessSignalResult MarkSignal(
        PreviewStartupSignalFlags signal,
        bool signalWindowActive,
        bool firstVisualConfirmed)
    {
        if (!signalWindowActive || !_expectGpuDualSignals)
        {
            return CreateSignalResult(PreviewStartupReadinessSignalStatus.IgnoredInactiveOrNotGpu, firstVisualConfirmed);
        }

        if ((_receivedSignals & signal) != 0)
        {
            return CreateSignalResult(PreviewStartupReadinessSignalStatus.Duplicate, firstVisualConfirmed);
        }

        _receivedSignals |= signal;
        if (signal == PreviewStartupSignalFlags.MediaOpened)
        {
            _gpuSignalMediaOpened = true;
        }
        else if (signal == PreviewStartupSignalFlags.FirstCaptureFrame)
        {
            _gpuSignalFirstFrame = true;
        }
        else if (signal == PreviewStartupSignalFlags.PlaybackAdvancing)
        {
            _gpuSignalPlaybackAdvancing = true;
        }

        return CreateSignalResult(PreviewStartupReadinessSignalStatus.Accepted, firstVisualConfirmed);
    }

    public PreviewStartupPlaybackPositionResult TrackPlaybackPosition(
        TimeSpan position,
        bool signalWindowActive,
        bool firstVisualConfirmed)
    {
        if (!signalWindowActive || !_expectGpuDualSignals)
        {
            return new PreviewStartupPlaybackPositionResult(
                PreviewStartupPlaybackPositionStatus.IgnoredInactiveOrNotGpu,
                position,
                TimeSpan.Zero,
                PlaybackAdvanceThreshold,
                null);
        }

        if (!_playbackPositionInitialized)
        {
            _playbackPositionInitialized = true;
            _lastPlaybackPosition = position;
            var acceptedSignal = position >= PlaybackAdvanceThreshold
                ? MarkSignal(PreviewStartupSignalFlags.PlaybackAdvancing, signalWindowActive, firstVisualConfirmed)
                : null;

            return new PreviewStartupPlaybackPositionResult(
                PreviewStartupPlaybackPositionStatus.BaselineCaptured,
                position,
                TimeSpan.Zero,
                PlaybackAdvanceThreshold,
                acceptedSignal);
        }

        var delta = position - _lastPlaybackPosition;
        if (position > _lastPlaybackPosition)
        {
            _lastPlaybackPosition = position;
        }

        var signalResult = position >= PlaybackAdvanceThreshold || delta >= PlaybackAdvanceThreshold
            ? MarkSignal(PreviewStartupSignalFlags.PlaybackAdvancing, signalWindowActive, firstVisualConfirmed)
            : null;

        return new PreviewStartupPlaybackPositionResult(
            PreviewStartupPlaybackPositionStatus.Checked,
            position,
            delta,
            PlaybackAdvanceThreshold,
            signalResult);
    }

    private PreviewStartupReadinessSignalResult CreateSignalResult(
        PreviewStartupReadinessSignalStatus status,
        bool firstVisualConfirmed)
    {
        var snapshot = Snapshot;
        var missingSignals = BuildMissingSignals(firstVisualConfirmed);
        var requiredMissing = snapshot.RequiredSignals & ~snapshot.ReceivedSignals;
        return new PreviewStartupReadinessSignalResult(
            status,
            snapshot,
            missingSignals,
            requiredMissing == PreviewStartupSignalFlags.None);
    }
}

internal sealed record PreviewStartupReadinessSignalSnapshot(
    bool ExpectGpuDualSignals,
    bool GpuSignalMediaOpened,
    bool GpuSignalFirstFrame,
    bool GpuSignalPlaybackAdvancing,
    PreviewStartupSignalFlags RequiredSignals,
    PreviewStartupSignalFlags ReceivedSignals,
    PreviewStartupStrategy Strategy);

internal readonly record struct PreviewStartupTimeoutDiagnosticSnapshot(
    string PlaceholderVisibility,
    string GpuVisibility,
    string CpuVisibility,
    PreviewStartupStrategy Strategy,
    PreviewStartupSignalFlags RequiredSignals,
    PreviewStartupSignalFlags ReceivedSignals,
    string? MissingSignals);

internal static class PreviewStartupSignalFormatter
{
    public static string FormatTimeoutDiagnosticPayload(PreviewStartupTimeoutDiagnosticSnapshot snapshot)
        => $"placeholder={snapshot.PlaceholderVisibility} " +
            $"gpuVisible={snapshot.GpuVisibility} cpuVisible={snapshot.CpuVisibility} " +
            $"strategy={snapshot.Strategy} required={FormatSignalList(snapshot.RequiredSignals)} " +
            $"received={FormatSignalList(snapshot.ReceivedSignals)} " +
            $"missing={snapshot.MissingSignals ?? "-"}";

    public static string FormatMissingSignals(
        PreviewStartupSignalFlags requiredSignals,
        PreviewStartupSignalFlags receivedSignals,
        bool firstVisualConfirmed)
    {
        if (requiredSignals == PreviewStartupSignalFlags.None)
        {
            return firstVisualConfirmed ? string.Empty : "FirstVisual";
        }

        var missing = requiredSignals & ~receivedSignals;
        return missing == PreviewStartupSignalFlags.None
            ? string.Empty
            : FormatSignalList(missing);
    }

    public static string FormatSignalList(PreviewStartupSignalFlags signals)
    {
        if (signals == PreviewStartupSignalFlags.None)
        {
            return "None";
        }

        var labels = new List<string>(4);
        if (signals.HasFlag(PreviewStartupSignalFlags.MediaOpened))
        {
            labels.Add("MediaOpened");
        }

        if (signals.HasFlag(PreviewStartupSignalFlags.FirstCaptureFrame))
        {
            labels.Add("FirstCaptureFrame");
        }

        if (signals.HasFlag(PreviewStartupSignalFlags.PlaybackAdvancing))
        {
            labels.Add("PlaybackAdvancing");
        }

        if (signals.HasFlag(PreviewStartupSignalFlags.FirstVisual))
        {
            labels.Add("FirstVisual");
        }

        return labels.Count == 0 ? "None" : string.Join("+", labels);
    }
}

internal sealed record PreviewStartupReadinessSignalResult(
    PreviewStartupReadinessSignalStatus Status,
    PreviewStartupReadinessSignalSnapshot Snapshot,
    string MissingSignals,
    bool AllRequiredSignalsReceived);

internal enum PreviewStartupReadinessSignalStatus
{
    IgnoredInactiveOrNotGpu,
    Duplicate,
    Accepted
}

internal sealed record PreviewStartupPlaybackPositionResult(
    PreviewStartupPlaybackPositionStatus Status,
    TimeSpan Position,
    TimeSpan Delta,
    TimeSpan Threshold,
    PreviewStartupReadinessSignalResult? SignalResult);

internal enum PreviewStartupPlaybackPositionStatus
{
    IgnoredInactiveOrNotGpu,
    BaselineCaptured,
    Checked
}
