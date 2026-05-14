using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Sussudio.Services.Audio;

internal sealed partial class WasapiAudioPlayback
{
    private const float VolumeRampPerFrame = 1.0f / (0.3f * OutputSampleRate); // 300ms ramp at 48kHz

    private volatile float _targetVolume = 1.0f;
    private float _currentVolume;
    private volatile float _lastOutputPeak;
    private volatile float _lastOutputRms;
    private long _lastOutputLevelTickMs;

    public float TargetVolume => _targetVolume;

    public float CurrentVolume => _currentVolume;

    public float LastOutputPeak => _lastOutputPeak;

    public float LastOutputRms => _lastOutputRms;

    public long LastOutputLevelTickMs => Interlocked.Read(ref _lastOutputLevelTickMs);

    public void SetVolume(float volume)
    {
        _targetVolume = Math.Clamp(volume, 0f, 1f);
    }

    private void ApplyVolume(Span<byte> buffer)
    {
        var floats = MemoryMarshal.Cast<byte, float>(buffer);
        var target = _targetVolume;

        // Fast path: already at target volume of 1.0
        if (_currentVolume >= 1.0f && target >= 1.0f) return;

        // Fast path: already at target, no ramp needed
        if (MathF.Abs(_currentVolume - target) < 0.0001f)
        {
            if (target < 0.0001f)
            {
                floats.Clear();
                return;
            }

            // Constant non-unity volume
            for (var i = 0; i < floats.Length; i++)
            {
                floats[i] *= _currentVolume;
            }
            return;
        }

        // Ramp toward target volume
        for (var i = 0; i < floats.Length; i += OutputChannels)
        {
            // Step current toward target
            if (_currentVolume < target)
            {
                _currentVolume = MathF.Min(_currentVolume + VolumeRampPerFrame, target);
            }
            else if (_currentVolume > target)
            {
                _currentVolume = MathF.Max(_currentVolume - VolumeRampPerFrame, target);
            }

            for (var ch = 0; ch < OutputChannels && i + ch < floats.Length; ch++)
            {
                floats[i + ch] *= _currentVolume;
            }

            // Once settled, apply rest at constant volume
            if (MathF.Abs(_currentVolume - target) < 0.0001f)
            {
                _currentVolume = target;
                if (target >= 1.0f) return; // rest is at unity, no scaling needed
                // Apply constant volume to remaining samples
                for (var j = i + OutputChannels; j < floats.Length; j++)
                {
                    floats[j] *= _currentVolume;
                }
                return;
            }
        }
    }

    private void UpdateOutputLevel(ReadOnlySpan<byte> buffer)
    {
        // Measure after volume application. This is the signal actually handed
        // to IAudioRenderClient, so automation traces can distinguish source
        // silence from a render-side dropout or an over-aggressive ramp.
        var floats = MemoryMarshal.Cast<byte, float>(buffer);
        if (floats.Length == 0)
        {
            _lastOutputPeak = 0;
            _lastOutputRms = 0;
            Interlocked.Exchange(ref _lastOutputLevelTickMs, Environment.TickCount64);
            return;
        }

        var peak = 0f;
        var sumSquares = 0.0;
        for (var i = 0; i < floats.Length; i++)
        {
            var sample = floats[i];
            var abs = MathF.Abs(sample);
            if (abs > peak)
            {
                peak = abs;
            }

            sumSquares += sample * sample;
        }

        _lastOutputPeak = peak;
        _lastOutputRms = (float)Math.Sqrt(sumSquares / floats.Length);
        Interlocked.Exchange(ref _lastOutputLevelTickMs, Environment.TickCount64);
    }
}
