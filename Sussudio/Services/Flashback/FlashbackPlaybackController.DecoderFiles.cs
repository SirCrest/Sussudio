using System;

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

    private void ReopenDecoderPlaybackFile(
        FlashbackDecoder decoder,
        string path,
        ref bool fileOpen,
        bool updateCurrentOpenPath,
        bool closeOnlyWhenOpen)
    {
        if (!closeOnlyWhenOpen || decoder.IsOpen)
        {
            decoder.CloseFile();
        }

        fileOpen = false;
        decoder.OpenFile(path);
        fileOpen = true;
        if (updateCurrentOpenPath)
        {
            _currentOpenFilePath = path;
        }

        _decoderHwAccel = decoder.IsD3D11HwAccelerated ? "D3D11VA" : "Software";
    }

    private void MarkDecoderPlaybackFileClosed(ref bool fileOpen)
    {
        _decoderHwAccel = "N/A";
        fileOpen = false;
        _currentOpenFilePath = null;
    }
}
