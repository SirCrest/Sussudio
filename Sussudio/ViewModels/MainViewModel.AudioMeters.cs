using System;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Live audio and microphone meter state fed by capture-service audio callbacks.
/// </summary>
public partial class MainViewModel
{
    /// <summary>
    /// Written by WASAPI callback thread via Volatile.Write, read by UI timer.
    /// Bypasses PropertyChanged to avoid per-frame dispatch + 53-case switch overhead.
    /// </summary>
    public double AudioMeterTarget;
    public double MicrophoneMeterTarget;
    public bool MicrophoneClipping { get; set; }
    private int _audioMeterTimerNeeded;
    private int _microphoneMeterTimerNeeded;

    /// <summary>
    /// Fires once when audio transitions from silent to active, signaling MainWindow
    /// to start the audio meter animation timer. Reset when the timer stops itself.
    /// </summary>
    public event Action? AudioMeterActivated;
    public event Action? MicrophoneMeterActivated;

    private const double MeterFloorDb = -60.0;
    private const double MeterDecayDbPerSecond = 40.0 / 1.7; // OBS-like PPM decay
    private double _audioMeterDb = MeterFloorDb;
    private long _audioMeterLastTick;
    private double _micMeterDb = MeterFloorDb;
    private long _micMeterLastTick;

    private void OnAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        var level = UpdateMeterLevel(e.Peak, ref _audioMeterDb, ref _audioMeterLastTick);
        Volatile.Write(ref AudioMeterTarget, level);
        AudioPeak = e.Peak;

        if (level > 0 && Interlocked.CompareExchange(ref _audioMeterTimerNeeded, 1, 0) == 0)
        {
            _dispatcherQueue.TryEnqueue(() => AudioMeterActivated?.Invoke());
        }

        if (e.Clipped)
        {
            _dispatcherQueue.TryEnqueue(() => AudioClipping = true);
        }
    }

    private void OnMicrophoneAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        var level = UpdateMeterLevel(e.Peak, ref _micMeterDb, ref _micMeterLastTick);
        Volatile.Write(ref MicrophoneMeterTarget, level);
        MicrophoneClipping = e.Clipped;

        if (level > 0 && Interlocked.CompareExchange(ref _microphoneMeterTimerNeeded, 1, 0) == 0)
        {
            _dispatcherQueue.TryEnqueue(() => MicrophoneMeterActivated?.Invoke());
        }
    }

    private void ResetAudioMeter()
    {
        _audioMeterDb = MeterFloorDb;
        _audioMeterLastTick = 0;
        _micMeterDb = MeterFloorDb;
        _micMeterLastTick = 0;
        AudioPeak = 0;
        Volatile.Write(ref AudioMeterTarget, 0.0);
        Volatile.Write(ref MicrophoneMeterTarget, 0.0);
        Interlocked.Exchange(ref _audioMeterTimerNeeded, 0);
        Interlocked.Exchange(ref _microphoneMeterTimerNeeded, 0);
        AudioClipping = false;
        MicrophoneClipping = false;
    }

    public void ResetAudioMeterTimerFlag()
    {
        Interlocked.Exchange(ref _audioMeterTimerNeeded, 0);
        Interlocked.Exchange(ref _microphoneMeterTimerNeeded, 0);
    }

    private double UpdateMeterLevel(double peak, ref double meterDb, ref long lastTick)
    {
        var targetDb = peak > 0 ? 20.0 * Math.Log10(peak) : MeterFloorDb;
        if (targetDb < MeterFloorDb) targetDb = MeterFloorDb;
        if (targetDb > 0) targetDb = 0;

        var nowTick = Environment.TickCount64;
        if (lastTick == 0)
        {
            meterDb = targetDb;
            lastTick = nowTick;
        }
        else
        {
            var dtSeconds = Math.Max(0, (nowTick - lastTick) / 1000.0);
            lastTick = nowTick;

            if (targetDb >= meterDb)
            {
                meterDb = targetDb;
            }
            else
            {
                var decay = MeterDecayDbPerSecond * dtSeconds;
                meterDb = Math.Max(targetDb, meterDb - decay);
            }
        }

        var level = (meterDb - MeterFloorDb) / -MeterFloorDb;
        return Math.Clamp(level, 0, 1);
    }
}
