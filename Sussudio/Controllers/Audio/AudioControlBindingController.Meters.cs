using System;

namespace Sussudio.Controllers;

internal sealed partial class AudioControlBindingController
{
    public void AttachAudioMeterActivationBindings()
    {
        _context.InitializeAudioMeterBrushes();
        _context.ViewModel.AudioMeterActivated += _context.EnsureAudioMeterTimerRunning;
        _context.ViewModel.MicrophoneMeterActivated += _context.EnsureAudioMeterTimerRunning;
    }

    public void ApplyInitialAudioMeterPresentation()
    {
        _context.ResetAudioMeterVisuals();
        _context.SetAudioMeterTargetLevel(_context.ViewModel.AudioMeterTarget);
    }

    public void AttachDeviceAudioGainAndMeterBindings()
    {
        _context.AnalogAudioGainSlider.ValueChanged += (s, e) =>
        {
            _context.ViewModel.AnalogAudioGainPercent = e.NewValue;
            _context.AnalogAudioGainValueTextBlock.Text = $"{(int)Math.Round(e.NewValue)}%";
        };
        _context.AudioMeterTrack.SizeChanged += (s, e) => _context.AnimateAudioMeterTick();
        _context.MicMeterTrack.SizeChanged += (s, e) => _context.AnimateAudioMeterTick();
    }
}
