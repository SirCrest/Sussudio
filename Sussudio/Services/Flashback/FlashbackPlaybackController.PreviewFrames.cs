using System;
using System.Diagnostics;
using System.Threading;
using Sussudio.Models;
using Sussudio.Services.Preview;
using Sussudio.Services.Runtime;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Preview frame submission and ownership ---

    // Keep the previous D3D11VA frame alive until the renderer has had a later
    // submit to copy from; CPU frames follow the same ownership path.
    private DecodedVideoFrame _previousHeldFrame;
    private bool _hasPreviousHeldFrame;

    private void ReleasePreviousHeldFrame()
    {
        if (_hasPreviousHeldFrame)
        {
            ReleaseHeldFrameBestEffort(_previousHeldFrame, "previous_frame");
            _previousHeldFrame = default;
            _hasPreviousHeldFrame = false;
        }
    }

    private void ReleasePlaybackFrameForLive(string operation)
    {
        Interlocked.Exchange(ref _lastAudioPtsTicks, 0);
        Interlocked.Exchange(ref _lastVideoPtsTicks, 0);

        if (_hasPreviousHeldFrame)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_RELEASE_HELD_FOR_LIVE op={operation}");
        }

        ReleasePreviousHeldFrame();
    }

    private void RestoreLiveAfterSeekDisplayFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)
    {
        CloseDecoderFileBestEffort(decoder, operation);
        fileOpen = false;
        _currentOpenFilePath = null;
        _decoderHwAccel = "N/A";
        ReleasePlaybackFrameForLive(operation);
        RestoreLiveAudio();
        SafeResumePreviewSubmission(operation);
        SafeResumeRendering(operation);
        SetState(FlashbackPlaybackState.Live);
    }

    private void RestoreLiveAfterPlaybackSubmitFailure(FlashbackDecoder decoder, ref bool fileOpen, string operation)
    {
        CloseDecoderFileBestEffort(decoder, operation);
        fileOpen = false;
        _currentOpenFilePath = null;
        _decoderHwAccel = "N/A";
        ReleasePlaybackFrameForLive(operation);
        RestoreLiveAudio();
        SafeResumePreviewSubmission(operation);
        SafeResumeRendering(operation);
        SetState(FlashbackPlaybackState.Live);
    }

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
            ReleasePreviousHeldFrame();
            _previousHeldFrame = frame;
            _hasPreviousHeldFrame = true;
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

    private static bool TryValidatePreviewFrame(DecodedVideoFrame frame, out string reason)
    {
        if (frame.Width <= 0 || frame.Height <= 0 || (frame.Width & 1) != 0 || (frame.Height & 1) != 0)
        {
            reason = "invalid_dimensions";
            return false;
        }

        if (frame.IsD3D11Texture)
        {
            if (frame.TexturePtr == IntPtr.Zero)
            {
                reason = "null_texture";
                return false;
            }

            if (frame.SubresourceIndex < 0)
            {
                reason = "invalid_subresource";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        if (frame.Data == IntPtr.Zero)
        {
            reason = "null_data";
            return false;
        }

        if (frame.DataLength <= 0)
        {
            reason = "invalid_data_length";
            return false;
        }

        if (!TryCalculatePreviewFrameBytes(frame.Width, frame.Height, frame.IsHdr, out var expectedBytes))
        {
            reason = "invalid_dimensions";
            return false;
        }

        if (frame.DataLength < expectedBytes)
        {
            reason = "short_data_length";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static bool TryCalculatePreviewFrameBytes(int width, int height, bool isHdr, out int bytes)
    {
        bytes = 0;
        if (width <= 0 || height <= 0 || (width & 1) != 0 || (height & 1) != 0)
        {
            return false;
        }

        var pixels = (long)width * height;
        var calculated = isHdr
            ? pixels * 3
            : pixels + width * (long)(height / 2);
        if (calculated <= 0 || calculated > int.MaxValue)
        {
            return false;
        }

        bytes = (int)calculated;
        return true;
    }

    private static void ReleaseHeldFrameBestEffort(DecodedVideoFrame frame, string operation)
    {
        try
        {
            FlashbackDecoder.ReleaseHeldFrame(frame);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_RELEASE_HELD_FRAME_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
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
