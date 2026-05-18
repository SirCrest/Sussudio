using System;
using System.Threading;
using Sussudio.Models;

namespace Sussudio.Services.Capture;

// Preview volume/mute and WASAPI audio-level/failure event projection for the
// capture session. Transition-heavy audio preview and input switching live in
// focused partials so the audio resource order stays auditable.
public partial class CaptureService
{
    public void SetPreviewVolume(float volume)
    {
        _previewAudioGraph.SetPreviewVolume(volume);
    }

    public void SetMonitoringMuted(bool muted)
    {
        _previewAudioGraph.SetMonitoringMuted(muted);
    }

    private void OnWasapiAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        AudioLevelUpdated?.Invoke(this, e);
    }

    private void OnWasapiCaptureFailed(object? sender, Exception ex)
    {
        var source = _previewAudioGraph.ClassifyCaptureFailureSource(sender);

        if (_isRecording)
        {
            _previewAudioGraph.RecordCaptureFault(source, ex);
        }

        Logger.Log($"WASAPI_CAPTURE_FAILED source={source} type={ex.GetType().Name} hr=0x{ex.HResult:X8} message={ex.Message} recording={_isRecording}");
        var statusPrefix = source == "microphone" ? "Microphone capture error" : "Audio capture error";
        StatusChanged?.Invoke(this, $"{statusPrefix}: {ex.Message}");
        ErrorOccurred?.Invoke(this, ex);
    }
}
