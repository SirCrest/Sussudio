using System;
using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.Controllers;

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
