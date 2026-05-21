using System.Collections.Generic;
using Sussudio.Models;

namespace Sussudio.Controllers;

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
