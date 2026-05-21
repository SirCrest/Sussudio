using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Recording;

namespace Sussudio.Services.Capture;

public sealed partial class CaptureSessionCoordinator
{
    public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.RestartFlashback,
            ct => _captureService.RestartFlashbackAsync(ct),
            cancellationToken,
            propagateCancellationToOperation: true);

    public Task RestartFlashbackAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return EnqueueAsync(
            CaptureCommandKind.RestartFlashback,
            ct => _captureService.RestartFlashbackAsync(settings, ct),
            cancellationToken,
            propagateCancellationToOperation: true);
    }

    public Task UpdateRecordingFormatAsync(RecordingFormat format, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.UpdateFlashbackRecordingFormat,
            ct => _captureService.UpdateRecordingFormatAsync(format, ct),
            cancellationToken);

    public Task CycleFlashbackEncoderSettingsAsync(
        VideoQuality? quality = null,
        double? customBitrateMbps = null,
        string? nvencPreset = null,
        string? splitEncodeMode = null,
        CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.CycleFlashbackEncoderSettings,
            ct => _captureService.CycleFlashbackEncoderSettingsAsync(quality, customBitrateMbps, nvencPreset, splitEncodeMode, ct),
            cancellationToken,
            coalesceLatest: true);

    public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.SetFlashbackEnabled,
            ct => _captureService.SetFlashbackEnabledAsync(enabled, ct),
            cancellationToken,
            propagateCancellationToOperation: true);

    public Task UpdateFlashbackSettingsAsync(int bufferMinutes, bool gpuDecode, CancellationToken cancellationToken = default)
        => EnqueueAsync(
            CaptureCommandKind.UpdateFlashbackSettings,
            ct => _captureService.UpdateFlashbackSettingsAsync(bufferMinutes, gpuDecode, ct),
            cancellationToken);
}
