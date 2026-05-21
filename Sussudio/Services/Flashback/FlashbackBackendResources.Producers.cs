using System;
using Sussudio.Services.Audio;
using Sussudio.Services.Capture;

namespace Sussudio.Services.Flashback;

internal readonly record struct FlashbackProducerDetachRequest(
    UnifiedVideoCapture? VideoCapture,
    WasapiAudioCapture? AudioCapture,
    WasapiAudioCapture? MicrophoneCapture,
    string WarningToken,
    bool DetachMicrophoneWriter);

internal readonly record struct FlashbackProducerAttachRequest(
    UnifiedVideoCapture VideoCapture,
    WasapiAudioCapture? AudioCapture,
    WasapiAudioCapture? MicrophoneCapture,
    string Reason);

internal sealed partial class FlashbackBackendResources
{
    public void AttachProducers(FlashbackProducerAttachRequest request)
    {
        var flashbackSink = Sink;
        if (flashbackSink == null)
        {
            return;
        }

        request.VideoCapture.SetFlashbackSink(flashbackSink);
        AttachAudioProducer(request.AudioCapture, flashbackSink, request.Reason);
        AttachMicrophoneProducer(request.MicrophoneCapture, flashbackSink, request.Reason);
    }

    public void DetachProducers(FlashbackProducerDetachRequest request)
    {
        if (request.DetachMicrophoneWriter)
        {
            try { request.MicrophoneCapture?.SetAudioWriter(null); }
            catch (Exception ex) { Logger.Log($"{request.WarningToken} target=microphone type={ex.GetType().Name} msg={ex.Message}"); }
        }

        try { request.AudioCapture?.DetachFlashbackSink(); }
        catch (Exception ex) { Logger.Log($"{request.WarningToken} target=audio type={ex.GetType().Name} msg={ex.Message}"); }

        try { request.VideoCapture?.SetFlashbackSink(null); }
        catch (Exception ex) { Logger.Log($"{request.WarningToken} target=video type={ex.GetType().Name} msg={ex.Message}"); }
    }

    private static void AttachAudioProducer(
        WasapiAudioCapture? audioCapture,
        FlashbackEncoderSink flashbackSink,
        string reason)
    {
        if (audioCapture == null)
        {
            return;
        }

        if (!flashbackSink.AudioEnabled)
        {
            Logger.Log($"FLASHBACK_AUDIO_ATTACH_SKIPPED reason='{reason}' sink_audio_enabled=false");
            return;
        }

        audioCapture.AttachFlashbackSink(flashbackSink);
        Logger.Log($"FLASHBACK_AUDIO_ATTACH_OK reason='{reason}'");
    }

    private static void AttachMicrophoneProducer(
        WasapiAudioCapture? microphoneCapture,
        FlashbackEncoderSink flashbackSink,
        string reason)
    {
        if (microphoneCapture == null || !flashbackSink.MicrophoneEnabled)
        {
            return;
        }

        microphoneCapture.SetAudioWriter(samples => flashbackSink.WriteMicrophoneAudioAsync(samples));
        Logger.Log($"FLASHBACK_MIC_ATTACH_OK reason='{reason}'");
    }
}
