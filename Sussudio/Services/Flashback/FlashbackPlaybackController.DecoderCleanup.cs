using System;
using System.Diagnostics;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    private static void CloseDecoderFileBestEffort(FlashbackDecoder decoder, string operation)
    {
        try
        {
            if (decoder.IsOpen) decoder.CloseFile();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_DECODER_CLOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void CleanupDecoder(ref FlashbackDecoder? decoder, ref bool fileOpen)
    {
        var cleanupStarted = Stopwatch.GetTimestamp();
        var wasOpen = decoder?.IsOpen ?? false;
        Logger.Log($"FLASHBACK_PLAYBACK_DECODER_CLEANUP was_open={wasOpen}");
        var releaseStarted = Stopwatch.GetTimestamp();
        ReleasePreviousHeldFrame();
        var releaseMs = Stopwatch.GetElapsedTime(releaseStarted).TotalMilliseconds;
        var closeMs = 0d;
        var disposeMs = 0d;
        if (decoder != null)
        {
            var decoderToDispose = decoder;
            decoder = null;
            try
            {
                if (decoderToDispose.IsOpen)
                {
                    var closeStarted = Stopwatch.GetTimestamp();
                    decoderToDispose.CloseFile();
                    closeMs = Stopwatch.GetElapsedTime(closeStarted).TotalMilliseconds;
                }
            }
            catch (Exception ex)
            {
                closeMs = Stopwatch.GetElapsedTime(cleanupStarted).TotalMilliseconds;
                Logger.Log($"FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=close type={ex.GetType().Name} msg='{ex.Message}'");
            }

            try
            {
                var disposeStarted = Stopwatch.GetTimestamp();
                decoderToDispose.Dispose();
                disposeMs = Stopwatch.GetElapsedTime(disposeStarted).TotalMilliseconds;
            }
            catch (Exception ex)
            {
                disposeMs = Stopwatch.GetElapsedTime(cleanupStarted).TotalMilliseconds;
                Logger.Log($"FLASHBACK_PLAYBACK_DECODER_CLEANUP_WARN op=dispose type={ex.GetType().Name} msg='{ex.Message}'");
            }
        }
        fileOpen = false;
        _currentOpenFilePath = null;
        _decoderHwAccel = "N/A";
        var totalMs = Stopwatch.GetElapsedTime(cleanupStarted).TotalMilliseconds;
        Logger.Log(
            $"FLASHBACK_PLAYBACK_DECODER_CLEANUP_COMPLETE was_open={wasOpen} " +
            $"release_ms={releaseMs:0.###} close_ms={closeMs:0.###} dispose_ms={disposeMs:0.###} total_ms={totalMs:0.###}");
    }
}
