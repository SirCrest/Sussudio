using System;

namespace Sussudio.Services.Recording;

internal sealed unsafe partial class LibAvEncoder
{
    private const long AvSyncLogCadenceFrames = 300;
    private const long MinimumAvSyncVideoFrames = 30;
    // Drift correction disabled: capture card audio and video share the same USB bus
    // clock, so there is no ongoing drift. The apparent drift at startup is a one-time
    // offset from WASAPI pre-buffering. Correcting it causes audible pops because 480-
    // sample block insertions/removals create hard discontinuities in the waveform.
    // Players handle the small initial A/V offset in the container transparently.
    private const double DriftCorrectionThresholdMs = double.MaxValue;
    private const double MicDriftCorrectionThresholdMs = 200.0;
    private const int MaxDriftCorrectionSamplesPerPass = 480;

    private long _lastSyncLogVideoFrame;
    private long _driftCorrectionAppliedSamples;
    private long _lastDriftCorrectionVideoFrame;

    private int GetDriftCorrectionSamples(long audioSamples, int sampleRate, out long correctionVideoFrame, out double driftMs,
        double thresholdMs = DriftCorrectionThresholdMs)
    {
        correctionVideoFrame = 0;
        driftMs = 0.0;

        if (_options == null ||
            (!_options.AudioEnabled && !_options.MicrophoneEnabled) ||
            _nextVideoPts < MinimumAvSyncVideoFrames ||
            _nextVideoPts - _lastDriftCorrectionVideoFrame < AvSyncLogCadenceFrames)
        {
            return 0;
        }

        if (!TryGetAvSyncState(audioSamples, out var videoFrame, out _, out _, out _, out driftMs))
        {
            return 0;
        }

        correctionVideoFrame = videoFrame;
        if (Math.Abs(driftMs) <= thresholdMs || sampleRate <= 0)
        {
            return 0;
        }

        var correctionSamples = (int)(-(driftMs / 1000.0) * sampleRate);
        return Math.Clamp(correctionSamples, -MaxDriftCorrectionSamplesPerPass, MaxDriftCorrectionSamplesPerPass);
    }

    public bool TryGetCurrentAvSyncDrift(out double driftMs, out long correctionSamples)
    {
        driftMs = 0.0;
        correctionSamples = _driftCorrectionAppliedSamples;

        // Use cached time_base values instead of dereferencing codec context pointers.
        // The codec contexts can be freed by FlushAndClose on the encoding thread,
        // but the cached time_base structs are plain value types set once during open.
        var vtb = _cachedVideoTimeBase;
        var atb = _audio.CachedTimeBase;
        if (vtb.num <= 0 || vtb.den <= 0 || atb.num <= 0 || atb.den <= 0)
        {
            return false;
        }

        var videoFrame = _nextVideoPts;
        var audioSamples = _audio.NextPts + _audio.BufferedSamples;
        var videoTimeSec = videoFrame * vtb.num / (double)vtb.den;
        var audioTimeSec = audioSamples * atb.num / (double)atb.den;
        driftMs = (audioTimeSec - videoTimeSec) * 1000.0;
        return true;
    }

    private void LogAvSyncIfDue()
    {
        if (_options == null ||
            !_options.AudioEnabled ||
            _nextVideoPts < MinimumAvSyncVideoFrames ||
            _nextVideoPts - _lastSyncLogVideoFrame < AvSyncLogCadenceFrames)
        {
            return;
        }

        if (!TryGetAvSyncState(_audio.NextPts + _audio.BufferedSamples, out var videoFrame, out var videoTimeSec, out var audioSamples, out var audioTimeSec, out var driftMs))
        {
            return;
        }

        _lastSyncLogVideoFrame = videoFrame;

        Logger.Log(
            $"LIBAV_AV_SYNC videoFrame={videoFrame} videoSec={videoTimeSec:F3} " +
            $"audioSamples={audioSamples} audioSec={audioTimeSec:F3} driftMs={driftMs:F1} " +
            $"totalCorrectionSamples={_driftCorrectionAppliedSamples}");

        if (Math.Abs(driftMs) > 500.0)
        {
            Logger.Log(
                $"LIBAV_AV_SYNC_DRIFT_WARNING videoFrame={videoFrame} driftMs={driftMs:F1} " +
                $"audioSamples={audioSamples} — drift exceeds 500ms, investigate audio delivery");
        }
    }

    private bool TryGetAvSyncState(
        long audioSamples,
        out long videoFrame,
        out double videoTimeSec,
        out long reportedAudioSamples,
        out double audioTimeSec,
        out double driftMs)
    {
        videoFrame = _nextVideoPts;
        videoTimeSec = 0.0;
        reportedAudioSamples = audioSamples;
        audioTimeSec = 0.0;
        driftMs = 0.0;

        if (_videoCodecCtx == null ||
            _audio.CodecCtx == null ||
            _videoCodecCtx->time_base.num <= 0 ||
            _videoCodecCtx->time_base.den <= 0 ||
            _audio.CodecCtx->time_base.num <= 0 ||
            _audio.CodecCtx->time_base.den <= 0)
        {
            return false;
        }

        videoTimeSec = videoFrame * _videoCodecCtx->time_base.num / (double)_videoCodecCtx->time_base.den;
        audioTimeSec = reportedAudioSamples * _audio.CodecCtx->time_base.num / (double)_audio.CodecCtx->time_base.den;
        driftMs = (audioTimeSec - videoTimeSec) * 1000.0;
        return true;
    }
}
