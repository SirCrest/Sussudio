using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Automation;

public sealed partial class AutomationCommandDispatcher
{
    private async Task<(bool Met, AutomationSnapshot Snapshot)> WaitForConditionAsync(
        AutomationWaitCondition condition,
        int timeoutMs,
        int pollMs,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var snapshot = _diagnosticsHub.GetLatestSnapshot();
        while (Stopwatch.GetElapsedTime(started).TotalMilliseconds < timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            snapshot = _diagnosticsHub.GetLatestSnapshot();
            if (ConditionSatisfied(condition, snapshot))
            {
                return (true, snapshot);
            }

            await Task.Delay(pollMs, cancellationToken).ConfigureAwait(false);
        }

        return (false, _diagnosticsHub.GetLatestSnapshot());
    }

    private static bool ConditionSatisfied(AutomationWaitCondition condition, AutomationSnapshot snapshot)
    {
        return condition switch
        {
            AutomationWaitCondition.PreviewFramesActive =>
                snapshot.IsPreviewing && (snapshot.PreviewGpuActive || snapshot.PreviewFramesDisplayed > 0),
            AutomationWaitCondition.PreviewRendererHealthy =>
                snapshot.IsPreviewing &&
                !snapshot.PreviewBlankSuspected &&
                !snapshot.PreviewStalled &&
                snapshot.PreviewFirstVisualConfirmed &&
                (snapshot.PreviewGpuActive || snapshot.PreviewFramesDisplayed > 0),
            AutomationWaitCondition.AudioSignalPresent =>
                snapshot.AudioSignalPresent,
            AutomationWaitCondition.RecordingFileGrowing =>
                snapshot.IsRecording && snapshot.RecordingFileGrowing,
            AutomationWaitCondition.RecordingStopped =>
                !snapshot.IsRecording,
            AutomationWaitCondition.VerificationReady =>
                snapshot.LastVerification != null,
            AutomationWaitCondition.HdrModeApplied =>
                snapshot.RequestedHdrEnabled.HasValue
                    ? snapshot.IsHdrEnabled == snapshot.RequestedHdrEnabled.Value
                    : snapshot.IsHdrEnabled,
            AutomationWaitCondition.PerformancePerfectionMet =>
                snapshot.PerformancePerfectionMet,
            AutomationWaitCondition.HdrVerificationReady =>
                snapshot.LastVerification is { } verification &&
                (!snapshot.HdrOutputActive ||
                 verification.HdrParity is { Requested: true, Verified: true } ||
                 verification.HdrMetadataPresent == true),
            AutomationWaitCondition.AudioFramesFlowing =>
                snapshot.AudioReaderActive && snapshot.AudioFramesArrived > 0,
            AutomationWaitCondition.VideoFramesFlowing =>
                snapshot.VideoReaderActive && snapshot.IngestVideoFramesArrived > 0,
            _ => false
        };
    }
}
