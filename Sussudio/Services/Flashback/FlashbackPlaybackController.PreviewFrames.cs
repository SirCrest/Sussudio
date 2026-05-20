using System;
using System.Diagnostics;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Preview;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Preview frame submission ---

    private bool TrySubmitAndHoldFrame(DecodedVideoFrame frame, string operation)
    {
        var previewSink = Volatile.Read(ref _previewSink);
        if (previewSink == null)
        {
            Interlocked.Increment(ref _playbackSubmitFailures);
            SetLastSubmitFailure($"{operation}:missing_preview_sink");
            ReleaseHeldFrameBestEffort(frame, $"{operation}_missing_preview_sink");
            Logger.Log($"FLASHBACK_PLAYBACK_SUBMIT_SKIP op={operation} reason=missing_preview_sink");
            return false;
        }

        if (!TryValidatePreviewFrame(frame, out var skipReason))
        {
            Interlocked.Increment(ref _playbackSubmitFailures);
            SetLastSubmitFailure($"{operation}:{skipReason}");
            ReleaseHeldFrameBestEffort(frame, $"{operation}_{skipReason}");
            Logger.Log($"FLASHBACK_PLAYBACK_SUBMIT_SKIP op={operation} reason={skipReason}");
            return false;
        }

        try
        {
            var previewPresentId = Interlocked.Increment(ref _playbackPreviewPresentId);
            var countForPresentCadence = string.Equals(operation, "playback", StringComparison.Ordinal);
            SubmitFrame(previewSink, frame, previewPresentId, countForPresentCadence);
            HoldSubmittedFrame(frame);
            ClearLastSubmitFailure();
            return true;
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _playbackSubmitFailures);
            SetLastSubmitFailure($"{operation}:submit_fail:{ex.GetType().Name}");
            ReleaseHeldFrameBestEffort(frame, $"{operation}_submit_fail");
            Logger.Log($"FLASHBACK_PLAYBACK_SUBMIT_FAIL op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
            return false;
        }
    }

    /// <summary>
    /// Submits a decoded frame to the preview renderer — GPU texture or raw CPU data.
    /// </summary>
    private static void SubmitFrame(
        IPreviewFrameSink previewSink,
        DecodedVideoFrame frame,
        long previewPresentId,
        bool countForPresentCadence)
    {
        var submitTick = Stopwatch.GetTimestamp();
        if (frame.IsD3D11Texture)
        {
            if (frame.TexturePtr == IntPtr.Zero)
            {
                Logger.Log("FLASHBACK_PLAYBACK_SUBMIT_SKIP reason=null_texture");
                return;
            }
            previewSink.SubmitTexture(
                frame.TexturePtr, frame.SubresourceIndex,
                frame.Width, frame.Height, frame.IsHdr,
                new PreviewFrameTracking(
                    ArrivalTick: submitTick,
                    SourceSequenceNumber: -1,
                    PreviewPresentId: previewPresentId,
                    SchedulerSubmitTick: submitTick,
                    SourcePtsTicks: frame.Pts.Ticks,
                    CountForPresentCadence: countForPresentCadence));
        }
        else
        {
            previewSink.SubmitRawFrame(
                frame.Data, frame.DataLength,
                frame.Width, frame.Height, frame.IsHdr,
                new PreviewFrameTracking(
                    ArrivalTick: submitTick,
                    SourceSequenceNumber: -1,
                    PreviewPresentId: previewPresentId,
                    SchedulerSubmitTick: submitTick,
                    SourcePtsTicks: frame.Pts.Ticks,
                    CountForPresentCadence: countForPresentCadence));
        }
    }
}
