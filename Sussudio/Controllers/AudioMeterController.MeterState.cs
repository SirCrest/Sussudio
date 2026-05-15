using System;

namespace Sussudio.Controllers;

internal sealed partial class AudioMeterController
{
    public void AnimateTick()
    {
        _audioMeterTargetLevel = _context.ViewModel.AudioMeterTarget;
        var target = _audioMeterTargetLevel;
        var nowMs = Environment.TickCount64;

        if (target >= _audioMeterDisplayLevel)
        {
            _audioMeterDisplayLevel += (target - _audioMeterDisplayLevel) * 0.4;
        }
        else
        {
            _audioMeterDisplayLevel += (target - _audioMeterDisplayLevel) * 0.06;
        }

        if (_audioMeterDisplayLevel < 0.001)
        {
            _audioMeterDisplayLevel = 0;
        }

        if (target >= _audioPeakHoldLevel)
        {
            _audioPeakHoldLevel = target;
            _audioPeakHoldTimestamp = nowMs;
        }
        else if (nowMs - _audioPeakHoldTimestamp > AudioPeakHoldDurationMs)
        {
            var dt = (nowMs - _audioPeakHoldTimestamp - AudioPeakHoldDurationMs) / 1000.0;
            _audioPeakHoldLevel = Math.Max(0, _audioPeakHoldLevel - (AudioPeakHoldDecayPerSecond * dt));
            _audioPeakHoldTimestamp = nowMs - AudioPeakHoldDurationMs;
        }

        if (nowMs - _audioRangeResetTimestamp > AudioRangeWindowMs)
        {
            _audioRangeMin = target;
            _audioRangeMax = target;
            _audioRangeResetTimestamp = nowMs;
        }
        else
        {
            if (target < _audioRangeMin) _audioRangeMin = target;
            if (target > _audioRangeMax) _audioRangeMax = target;
        }

        var trackWidth = _context.AudioMeterTrack.ActualWidth;
        if (trackWidth > 0)
        {
            var trackHeight = _context.AudioMeterTrack.ActualHeight > 0 ? _context.AudioMeterTrack.ActualHeight : 8;
            var rawLevel = _audioMeterDisplayLevel;
            var colorLevel = rawLevel * _context.ViewModel.PreviewVolume;

            _context.AudioMeterRawClip.Rect = new Windows.Foundation.Rect(0, 0, trackWidth * rawLevel, trackHeight);
            _context.AudioMeterColorClip.Rect = new Windows.Foundation.Rect(0, 0, trackWidth * colorLevel, trackHeight);

            _context.AudioPeakHoldTranslate.X = TranslateMarker(trackWidth, _audioPeakHoldLevel, _context.AudioPeakHoldIndicator.Width);
            _context.AudioRangeMinTranslate.X = TranslateMarker(trackWidth, _audioRangeMin, _context.AudioRangeMinMarker.Width);
            _context.AudioRangeMaxTranslate.X = TranslateMarker(trackWidth, _audioRangeMax, _context.AudioRangeMaxMarker.Width);
        }

        if (_context.ViewModel.IsMicrophoneEnabled)
        {
            _micMeterTargetLevel = Math.Clamp(_context.ViewModel.MicrophoneMeterTarget, 0.0, 1.0);
            if (_micMeterTargetLevel > _micMeterDisplayLevel)
            {
                _micMeterDisplayLevel += (_micMeterTargetLevel - _micMeterDisplayLevel) * 0.4;
            }
            else
            {
                _micMeterDisplayLevel += (_micMeterTargetLevel - _micMeterDisplayLevel) * 0.25;
            }

            if (_micMeterDisplayLevel < 0.001)
            {
                _micMeterDisplayLevel = 0;
            }

            var micTrackWidth = _context.MicMeterTrack.ActualWidth - 2;
            if (micTrackWidth > 0)
            {
                var micFillWidth = _micMeterDisplayLevel * micTrackWidth;
                _context.MicMeterClip.Rect = new Windows.Foundation.Rect(0, 0, micFillWidth, 8);
            }
        }
        else if (_micMeterDisplayLevel != 0 || _micMeterTargetLevel != 0)
        {
            ResetMicrophoneVisuals();
        }

        if (_audioMeterDisplayLevel == 0 &&
            _audioPeakHoldLevel == 0 &&
            target == 0 &&
            _micMeterDisplayLevel == 0 &&
            _micMeterTargetLevel == 0)
        {
            _audioMeterAnimationTimer?.Stop();
            _context.ViewModel.ResetAudioMeterTimerFlag();
        }
    }

    public void ResetVisuals()
    {
        _audioPeakHoldLevel = 0;
        _audioPeakHoldTimestamp = 0;
        _audioRangeMin = 1.0;
        _audioRangeMax = 0;
        _audioRangeResetTimestamp = 0;
        _audioMeterDisplayLevel = 0;
        _context.AudioPeakHoldTranslate.X = 0;
        _context.AudioRangeMinTranslate.X = 0;
        _context.AudioRangeMaxTranslate.X = 0;
        _audioMeterTargetLevel = 0;
        _context.AudioMeterColorClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
        _context.AudioMeterRawClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
        ResetMicrophoneVisuals();
    }

    public void ResetMicrophoneVisuals()
    {
        _micMeterDisplayLevel = 0;
        _micMeterTargetLevel = 0;
        _context.MicMeterClip.Rect = new Windows.Foundation.Rect(0, 0, 0, 8);
    }

    public void SetAudioMeterTargetLevel(double targetLevel)
    {
        _audioMeterTargetLevel = Math.Clamp(targetLevel, 0.0, 1.0);
    }

    public void EnsureTimerRunning()
    {
        if (_audioMeterAnimationTimer is { IsRunning: false })
        {
            _audioMeterAnimationTimer.Start();
        }
    }

    public void StopTimer()
    {
        _audioMeterAnimationTimer?.Stop();
        _audioMeterAnimationTimer = null;
    }

    public static double TranslateMarker(double trackWidth, double level, double markerWidth)
    {
        var clamped = Math.Clamp(level, 0.0, 1.0);
        var availableWidth = Math.Max(0, trackWidth - markerWidth);
        return availableWidth * clamped;
    }
}
