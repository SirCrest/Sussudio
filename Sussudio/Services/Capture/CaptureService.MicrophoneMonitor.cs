using Sussudio.Models;
using Sussudio.Services.Audio;

namespace Sussudio.Services.Capture;

// Microphone monitoring state model and mic-level event forwarding.
public partial class CaptureService
{
    private readonly record struct MicrophoneMonitorRestartOptions(
        bool OnlyWhenMissing,
        string? FlashbackAttachReason,
        string? RestartLogEvent,
        string DisposeWarningEvent);

    private void OnMicrophoneAudioLevelUpdated(object? sender, AudioLevelEventArgs e)
    {
        MicrophoneAudioLevelUpdated?.Invoke(this, e);
    }
}
