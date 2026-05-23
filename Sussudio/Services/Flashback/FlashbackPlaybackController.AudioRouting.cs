using System;
using System.Buffers;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Audio routing ---

    private void ApplyAudioRoutingForState(string operation)
    {
        if (_disposedFlag != 0)
        {
            return;
        }

        switch (_state)
        {
            case FlashbackPlaybackState.Live:
                RestoreLiveAudio();
                break;
            case FlashbackPlaybackState.Playing:
                SuppressLiveAudio();
                SafeResumeRendering(operation);
                break;
            case FlashbackPlaybackState.Paused:
            case FlashbackPlaybackState.Scrubbing:
                SuppressLiveAudio();
                SafePauseRendering(operation);
                break;
        }
    }

    private void ApplyPreviewRoutingForState(string operation)
    {
        if (_disposedFlag != 0)
        {
            return;
        }

        if (_state == FlashbackPlaybackState.Live)
        {
            SafeResumePreviewSubmission(operation);
        }
        else
        {
            SafeSuppressPreviewSubmission(operation);
        }
    }

    private void SuppressLiveAudio()
    {
        try
        {
            _audioCapture?.SetPlayback(null);
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=suppress_live_set_playback type={ex.GetType().Name} msg='{ex.Message}'");
        }

        SafeFlushPlayback("suppress_live_audio");
    }

    private void RestoreLiveAudio()
    {
        SafeFlushPlayback("restore_live_audio");
        // Reconnect audio feed before resuming rendering to avoid silence/stutter.
        try
        {
            if (_audioCapture != null && _audioPlayback != null)
            {
                _audioCapture.SetPlayback(_audioPlayback);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=restore_live_set_playback type={ex.GetType().Name} msg='{ex.Message}'");
        }

        SafeResumeRendering("restore_live_audio");
    }

    private void SafeSuppressPreviewSubmission(string operation)
    {
        try
        {
            _videoCapture?.SuppressPreviewSubmission();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_PREVIEW_WARN op=suppress operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void SafeResumePreviewSubmission(string operation)
    {
        try
        {
            _videoCapture?.ResumePreviewSubmission();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_PREVIEW_WARN op=resume operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void SafePauseRendering(string operation)
    {
        try
        {
            _audioPlayback?.PauseRendering();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=pause operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void SafeResumeRendering(string operation)
    {
        try
        {
            _audioPlayback?.ResumeRendering();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=resume operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void SafeResumePlaybackRendering(string operation)
    {
        try
        {
            _audioPlayback?.ResumeRendering();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=resume_playback operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void SafeFlushPlayback(string operation)
    {
        try
        {
            _audioPlayback?.Flush();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_WARN op=flush operation={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void RestoreAudioCallback(FlashbackDecoder decoder, long audioStartGateTicks = 0)
    {
        // Audio start gate: drop any audio chunk with PTS before this value.
        // This filters stale audio from keyframe-to-target decode after a seek.
        var videoPtsGate = audioStartGateTicks > 0
            ? audioStartGateTicks
            : Interlocked.Read(ref _lastVideoPtsTicks);
        Interlocked.Exchange(ref _lastAudioPtsTicks, 0);

        if (_audioPlayback == null)
        {
            decoder.AudioChunkCallback = null;
            return;
        }

        decoder.AudioChunkCallback = chunk =>
        {
            var pb = _audioPlayback;
            if (pb == null)
            {
                ReturnPlaybackAudioChunkBestEffort(chunk, "playback_missing_audio_sink");
                return;
            }

            if (!TryValidatePlaybackAudioChunk(chunk, out var invalidReason))
            {
                Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_DROP reason={invalidReason} pts_ms={(long)chunk.Pts.TotalMilliseconds} valid_bytes={chunk.ValidLength} buffer_bytes={chunk.Samples?.Length ?? 0}");
                ReturnPlaybackAudioChunkBestEffort(chunk, $"playback_audio_{invalidReason}");
                return;
            }

            // Skip invalid or non-monotonic PTS (L8 fix).
            var prevPts = Interlocked.Read(ref _lastAudioPtsTicks);
            if (chunk.Pts.Ticks <= 0 || chunk.Pts.Ticks < prevPts)
            {
                ReturnPlaybackAudioChunkBestEffort(chunk, "playback_audio_non_monotonic_pts");
                return;
            }

            // Skip audio from the keyframe-to-target forward decode after a seek.
            if (videoPtsGate > 0 && chunk.Pts.Ticks < videoPtsGate)
            {
                ReturnPlaybackAudioChunkBestEffort(chunk, "playback_audio_before_gate");
                return;
            }

            Interlocked.Exchange(ref _lastAudioPtsTicks, chunk.Pts.Ticks);
            pb.EnqueuePooledSamples(chunk.Samples, chunk.ValidLength, chunk.Pts.Ticks);
        };
    }

    private static bool TryValidatePlaybackAudioChunk(DecodedAudioChunk chunk, out string reason)
    {
        if (chunk.Samples == null)
        {
            reason = "null_samples";
            return false;
        }

        if (chunk.ValidLength <= 0)
        {
            reason = "invalid_length";
            return false;
        }

        if (chunk.ValidLength > chunk.Samples.Length)
        {
            reason = "length_exceeds_buffer";
            return false;
        }

        const int playbackAudioBlockAlign = 2 * sizeof(float);
        if (chunk.ValidLength % playbackAudioBlockAlign != 0)
        {
            reason = "unaligned_length";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static void ReturnPlaybackAudioChunkBestEffort(DecodedAudioChunk chunk, string operation)
    {
        try
        {
            if (chunk.Samples is { Length: > 0 })
            {
                ArrayPool<byte>.Shared.Return(chunk.Samples);
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_AUDIO_RETURN_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }
}
