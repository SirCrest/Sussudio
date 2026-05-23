using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

internal sealed unsafe partial class FlashbackDecoder
{
    private void ThrowIfNotInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("FlashbackDecoder has not been initialized. Call Initialize() first.");
        }
    }

    private void ThrowIfNotOpen()
    {
        ThrowIfDisposed();
        if (!_isOpen)
        {
            throw new InvalidOperationException("No file is open. Call OpenFile() first.");
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void AddLastDecodeReceiveMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { ReceiveMs = _lastDecodePhaseTimings.ReceiveMs + elapsedMs };

    private void AddLastDecodeFeedMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { FeedMs = _lastDecodePhaseTimings.FeedMs + elapsedMs };

    private void AddLastDecodeReadMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { ReadMs = _lastDecodePhaseTimings.ReadMs + elapsedMs };

    private void AddLastDecodeSendMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { SendMs = _lastDecodePhaseTimings.SendMs + elapsedMs };

    private void AddLastDecodeAudioMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { AudioMs = _lastDecodePhaseTimings.AudioMs + elapsedMs };

    private void AddLastDecodeConvertMs(double elapsedMs)
        => _lastDecodePhaseTimings = _lastDecodePhaseTimings with { ConvertMs = _lastDecodePhaseTimings.ConvertMs + elapsedMs };

    private static double ElapsedMsSince(long startTimestamp)
        => (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;

    private static void ThrowIfError(int errorCode, string operation)
    {
        if (errorCode >= 0)
        {
            return;
        }

        var message = GetErrorString(errorCode);
        Logger.Log($"FLASHBACK_DECODER_ERROR operation={operation} code={errorCode} msg='{message}'");
        throw new InvalidOperationException(
            $"FLASHBACK_DECODER_ERROR operation={operation} code={errorCode} msg='{message}'");
    }

    private static string GetErrorString(int errorCode)
    {
        var buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
        ffmpeg.av_strerror(errorCode, buffer, (ulong)ffmpeg.AV_ERROR_MAX_STRING_SIZE);
        return Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"unknown error {errorCode}";
    }

    private static InvalidOperationException CreateException(string message)
    {
        Logger.Log($"FLASHBACK_DECODER_ERROR {message}");
        return new InvalidOperationException($"FLASHBACK_DECODER_ERROR {message}");
    }
}
