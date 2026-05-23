using System;
using System.Runtime.InteropServices;
using System.Threading;
using Sussudio.Models;
using FFmpeg.AutoGen;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Exports Flashback time ranges by remuxing finalized segment artifacts to .mp4.
/// No re-encoding - just packet copy with PTS adjustment.
/// </summary>
internal sealed unsafe partial class FlashbackExporter : IDisposable
{
    // Export reads finalized segment artifacts only. Live capture continues via
    // FlashbackEncoderSink while this class remuxes packets into the target MP4.
    private delegate bool CompletedOutputValidator(string outputPath, out long outputBytes, out string failureMessage);

    private const int MaxSupportedInputStreams = 64;
    private const int ProgressHeartbeatIntervalMs = 1_000;
    private const int ExportLockWaitTimeoutSeconds = 30;
    private static readonly TimeSpan OrphanTempFileMinimumAge = TimeSpan.FromMinutes(15);

    private readonly SemaphoreSlim _exportLock = new(1, 1);
    private readonly object _lifetimeSync = new();
    private CancellationTokenSource? _disposeCts = new();
    private AVFormatContext* _activeInputContext;
    private AVFormatContext* _activeOutputContext;
    private string? _activeTempPath;
    private bool _disposed;

    public void Dispose()
    {
        CancellationTokenSource? disposeCts;
        lock (_lifetimeSync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            disposeCts = _disposeCts;
        }

        Logger.Log("FLASHBACK_EXPORT_DISPOSE");

        // Signal any running export to cancel. ExportCore/ExportSegmentsCore will exit
        // via OperationCanceledException, clean up native state, and release _exportLock.
        try { disposeCts?.Cancel(); }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_DISPOSE_CANCEL_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        }

        // Wait for the export task to release the lock. The CTS is cancelled so
        // the task should exit promptly. Timeout prevents app hang if FFmpeg is stuck.
        var lockAcquired = _exportLock.Wait(TimeSpan.FromSeconds(10));
        if (!lockAcquired)
        {
            Logger.Log("FLASHBACK_EXPORT_DISPOSE: timed out waiting for export lock (10s)");
            Logger.Log("FLASHBACK_EXPORT_DISPOSE_TIMEOUT cleanup_invoked=false");
            Logger.Log("FLASHBACK_EXPORT_DISPOSE_TIMEOUT_OK");
            DisposeLinkedCtsBestEffort(disposeCts, "dispose_timeout");
            ClearDisposeCtsReference(disposeCts);
            GC.SuppressFinalize(this);
            return;
        }

        try
        {
            CleanupNativeState();
        }
        finally
        {
            if (lockAcquired)
            {
                ReleaseExportLockBestEffort("dispose");
            }
        }

        DisposeExportLockBestEffort();
        DisposeLinkedCtsBestEffort(disposeCts, "dispose");
        ClearDisposeCtsReference(disposeCts);
        GC.SuppressFinalize(this);
    }

    private CancellationTokenSource CreateExportCancellationSource(CancellationToken ct)
    {
        lock (_lifetimeSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var disposeCts = _disposeCts ?? throw new ObjectDisposedException(nameof(FlashbackExporter));
            return CancellationTokenSource.CreateLinkedTokenSource(ct, disposeCts.Token);
        }
    }

    private static void DisposeLinkedCtsBestEffort(CancellationTokenSource? cts, string operation)
    {
        if (cts == null) return;

        try
        {
            cts.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_LINKED_CTS_DISPOSE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void ClearDisposeCtsReference(CancellationTokenSource? disposeCts)
    {
        lock (_lifetimeSync)
        {
            if (ReferenceEquals(_disposeCts, disposeCts))
            {
                _disposeCts = null;
            }
        }
    }

    private void EnsureNotDisposed()
    {
        lock (_lifetimeSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }

    private static FinalizeResult CreateCancelledExportResult(string outputPath)
    {
        const string message = "Flashback export cancelled.";
        Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
        return FinalizeResult.Failure(outputPath, message);
    }

    private static FinalizeResult CreateDisposedExportResult(string outputPath)
    {
        const string message = "Flashback exporter is disposed.";
        Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
        return FinalizeResult.Failure(outputPath, message);
    }

    private static void ThrowIfError(int errorCode, string operation)
    {
        if (errorCode >= 0)
        {
            return;
        }

        var message = GetErrorString(errorCode);
        Logger.Log($"FLASHBACK_EXPORT_LIBAV_ERROR operation={operation} code={errorCode} msg='{message}'");
        throw new InvalidOperationException($"FLASHBACK_EXPORT_LIBAV_ERROR operation={operation} code={errorCode} msg='{message}'");
    }

    private static string GetErrorString(int errorCode)
    {
        var buffer = stackalloc byte[ffmpeg.AV_ERROR_MAX_STRING_SIZE];
        ffmpeg.av_strerror(errorCode, buffer, (ulong)ffmpeg.AV_ERROR_MAX_STRING_SIZE);
        return Marshal.PtrToStringAnsi((IntPtr)buffer) ?? $"unknown error {errorCode}";
    }

    private bool TryWaitForExportLock(string outputPath, CancellationToken ct, out FinalizeResult cancellationResult)
    {
        try
        {
            if (!_exportLock.Wait(TimeSpan.FromSeconds(ExportLockWaitTimeoutSeconds), ct))
            {
                var message = $"Flashback export lock timed out after {ExportLockWaitTimeoutSeconds}s.";
                Logger.Log($"FLASHBACK_EXPORT_LOCK_WAIT_TIMEOUT timeout_s={ExportLockWaitTimeoutSeconds}");
                cancellationResult = FinalizeResult.Failure(outputPath, message);
                return false;
            }

            cancellationResult = null!;
            return true;
        }
        catch (OperationCanceledException)
        {
            const string message = "Flashback export cancelled.";
            Logger.Log($"FLASHBACK_EXPORT_FAIL reason='{message}'");
            cancellationResult = FinalizeResult.Failure(outputPath, message);
            return false;
        }
        catch (ObjectDisposedException)
        {
            cancellationResult = CreateDisposedExportResult(outputPath);
            return false;
        }
    }

    private void ReleaseExportLockBestEffort(string operation)
    {
        try
        {
            _exportLock.Release();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_LOCK_RELEASE_WARN op={operation} type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void DisposeExportLockBestEffort()
    {
        try
        {
            _exportLock.Dispose();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_LOCK_DISPOSE_WARN type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void CloseActiveInput()
    {
        if (_activeInputContext == null)
        {
            return;
        }

        var inputContext = _activeInputContext;
        ffmpeg.avformat_close_input(&inputContext);
        _activeInputContext = null;
    }

    private void CloseOutputIo()
    {
        if (_activeOutputContext == null || _activeOutputContext->pb == null)
        {
            return;
        }

        var closeResult = ffmpeg.avio_closep(&_activeOutputContext->pb);
        if (closeResult < 0)
        {
            Logger.Log(
                $"FLASHBACK_EXPORT_WARN reason='avio_closep_failed' code={closeResult} msg='{GetErrorString(closeResult)}'");
        }
    }

    private void CleanupNativeState()
    {
        try
        {
            CloseActiveInput();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_CLEANUP_WARN op=close_input type={ex.GetType().Name} msg='{ex.Message}'");
            _activeInputContext = null;
        }

        try
        {
            CloseOutputIo();
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_CLEANUP_WARN op=close_output_io type={ex.GetType().Name} msg='{ex.Message}'");
        }

        if (_activeOutputContext != null)
        {
            try
            {
                ffmpeg.avformat_free_context(_activeOutputContext);
            }
            catch (Exception ex)
            {
                Logger.Log($"FLASHBACK_EXPORT_CLEANUP_WARN op=free_output_context type={ex.GetType().Name} msg='{ex.Message}'");
            }
            finally
            {
                _activeOutputContext = null;
            }
        }
    }
}
