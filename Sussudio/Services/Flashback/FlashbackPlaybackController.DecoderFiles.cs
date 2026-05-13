using System;
using System.Diagnostics;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackPlaybackController
{
    // --- Decoder file lifecycle ---

    private string? _currentOpenFilePath;

    private FlashbackDecoder CreateDecoder()
    {
        var useGpu = GpuDecodeEnabled;
        Logger.Log($"FLASHBACK_PLAYBACK_DECODER_CREATE gpu={useGpu}");
        var decoder = new FlashbackDecoder();

        // Get D3D11 device pointers for GPU-direct decode (skip if GPU decode disabled).
        var devicePtr = IntPtr.Zero;
        var contextPtr = IntPtr.Zero;
        if (useGpu)
        {
            var d3dManager = _videoCapture?.D3DManager;
            devicePtr = d3dManager?.Device?.NativePointer ?? IntPtr.Zero;
            contextPtr = d3dManager?.ImmediateContext?.NativePointer ?? IntPtr.Zero;
        }
        decoder.Initialize(devicePtr, contextPtr);

        RestoreAudioCallback(decoder);
        return decoder;
    }

    private void EnsureFileOpen(FlashbackDecoder decoder, ref bool fileOpen, TimeSpan? targetPts = null)
    {
        // Determine which segment file contains the target position.
        var filePath = targetPts.HasValue
            ? _bufferManager.GetValidSegmentFileForPosition(targetPts.Value)
            : _bufferManager.ActiveFilePath;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            Logger.Log("FLASHBACK_PLAYBACK_NO_FILE");
            if (decoder.IsOpen)
            {
                CloseDecoderFileBestEffort(decoder, "ensure_file_open_no_file");
            }

            fileOpen = false;
            _currentOpenFilePath = null;
            _decoderHwAccel = "N/A";
            return;
        }

        // If already open on the correct file, nothing to do.
        if (fileOpen && decoder.IsOpen && IsSamePlaybackPath(filePath, _currentOpenFilePath))
            return;

        try
        {
            if (decoder.IsOpen)
            {
                CloseDecoderFileBestEffort(decoder, "ensure_file_open");
                fileOpen = false;
                _currentOpenFilePath = null;
                _decoderHwAccel = "N/A";
            }

            decoder.OpenFile(filePath);
            fileOpen = true;
            _currentOpenFilePath = filePath;
            _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
            Logger.Log($"FLASHBACK_PLAYBACK_FILE_OPEN path='{filePath}' hw_accel={_decoderHwAccel}");
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_PLAYBACK_FILE_OPEN_ERROR path='{filePath}' type={ex.GetType().Name} error='{ex.Message}'");
            if (decoder.IsOpen)
            {
                CloseDecoderFileBestEffort(decoder, "ensure_file_open_error");
            }
            _decoderHwAccel = "N/A";
            fileOpen = false;
            _currentOpenFilePath = null;
        }
    }

    private static bool IsDecoderFileReady(FlashbackDecoder decoder, bool fileOpen)
        => fileOpen && decoder.IsOpen;

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
