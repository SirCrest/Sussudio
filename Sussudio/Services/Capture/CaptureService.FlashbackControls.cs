using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;
using Sussudio.Services.Flashback;

namespace Sussudio.Services.Capture;

// Flashback-facing control surface: public state, enable/settings mutations,
// restarts, and encoder-setting cycles.
public partial class CaptureService
{
    public bool IsFlashbackActive => _flashbackSink != null;
    public TimeSpan FlashbackBufferedDuration => _flashbackBufferManager?.BufferedDuration ?? TimeSpan.Zero;
    public long FlashbackDiskBytes => _flashbackBufferManager?.TotalDiskBytes ?? 0;
    public int FlashbackSegmentCount => _flashbackBufferManager?.SegmentCount ?? 0;
    internal FlashbackPlaybackController? FlashbackPlaybackController => _flashbackPlaybackController;
    internal FlashbackBufferManager? FlashbackBufferManager => _flashbackBufferManager;
    public long FlashbackOutputBytes => _flashbackSink?.OutputBytes ?? 0;
    public long FlashbackTotalBytesWritten => _flashbackBufferManager?.TotalBytesWritten ?? 0;
    public string? EncoderCodecName => _flashbackSink?.CodecName;
    public uint EncoderTargetBitRate => _flashbackSink?.TargetBitRate ?? 0;
    public int EncoderWidth => _flashbackSink?.EncoderWidth ?? 0;
    public int EncoderHeight => _flashbackSink?.EncoderHeight ?? 0;
    public double EncoderFrameRate => _flashbackSink?.EncoderFrameRate ?? 0;
    public FinalizeResult? LastExportResult => _lastExportResult;

    internal IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()
    {
        return _flashbackBufferManager?.GetSegmentInfoList()
            ?? Array.Empty<FlashbackSegmentInfo>();
    }

    public Task SetFlashbackEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
        => RunTransitionAsync(CurrentSessionState, async transitionToken =>
        {
            if (_isRecording && IsFlashbackRecordingBackendActive() && !enabled)
            {
                Logger.Log("FLASHBACK_DISABLE_BLOCKED reason=recording_active");
                throw new InvalidOperationException("Cannot disable Flashback while Flashback recording is active.");
            }

            if (_flashbackEnabled == enabled)
            {
                if (enabled && (_flashbackSink != null || _isRecording))
                {
                    return;
                }

                if (!enabled && !_flashbackBackend.HasAnyResource)
                {
                    return;
                }
            }

            _flashbackEnabled = enabled;
            if (!enabled)
            {
                _pendingFlashbackEnableAfterRecording = false;
                await DisposeFlashbackPreviewBackendAsync(transitionToken, purgeSegments: true).ConfigureAwait(false);
                if (!_isVideoPreviewActive && !_isAudioPreviewActive && !_isRecording)
                {
                    await DisposePreviewPipelineAsync(transitionToken, purgeFlashbackSegments: false).ConfigureAwait(false);
                }
                return;
            }

            if (_isRecording)
            {
                _pendingFlashbackEnableAfterRecording = true;
                Logger.Log("FLASHBACK_ENABLE_DEFERRED reason=recording_active");
                return;
            }

            _pendingFlashbackEnableAfterRecording = false;
            if (_unifiedVideoCapture != null && _currentSettings != null)
            {
                try
                {
                    await EnsureFlashbackPreviewBackendAsync(_unifiedVideoCapture, _currentSettings, transitionToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (transitionToken.IsCancellationRequested)
                {
                    _flashbackEnabled = false;
                    _pendingFlashbackEnableAfterRecording = false;
                    if (_flashbackBackend.HasAnyResource)
                    {
                        await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true).ConfigureAwait(false);
                    }
                    Logger.Log($"FLASHBACK_ENABLE_IMMEDIATE_CANCELLED type={ex.GetType().Name} error='{ex.Message}'");
                    throw;
                }
                catch (Exception ex)
                {
                    _flashbackEnabled = false;
                    _pendingFlashbackEnableAfterRecording = false;
                    if (_flashbackBackend.HasAnyResource)
                    {
                        await DisposeFlashbackPreviewBackendAsync(CancellationToken.None, purgeSegments: true).ConfigureAwait(false);
                    }
                    Logger.Log($"FLASHBACK_ENABLE_IMMEDIATE_FAIL type={ex.GetType().Name} error='{ex.Message}'");
                    throw;
                }
            }
        }, cancellationToken);

    /// <summary>
    /// Tears down the running flashback encoder and buffer, then rebuilds
    /// with current settings. Purges all existing segments because encoding
    /// parameters (bitrate, codec, etc.) may have changed.
    /// </summary>
    public Task RestartFlashbackAsync(CancellationToken cancellationToken = default)
        => RunTransitionAsync(CurrentSessionState, async transitionToken =>
        {
            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                Logger.Log("FLASHBACK_RESTART_BLOCKED reason=recording_active");
                throw new InvalidOperationException("Cannot restart Flashback while Flashback recording is active.");
            }

            await RestartFlashbackCoreAsync(transitionToken).ConfigureAwait(false);
        }, cancellationToken);

    public Task RestartFlashbackAsync(CaptureSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return RunTransitionAsync(CurrentSessionState, async transitionToken =>
        {
            if (_isRecording && IsFlashbackRecordingBackendActive())
            {
                Logger.Log("FLASHBACK_RESTART_BLOCKED reason=recording_active");
                throw new InvalidOperationException("Cannot restart Flashback while Flashback recording is active.");
            }

            UpdateEncodingSettings(settings);
            await RestartFlashbackCoreAsync(transitionToken).ConfigureAwait(false);
        }, cancellationToken);
    }

    private async Task RestartFlashbackCoreAsync(CancellationToken cancellationToken)
    {
        await DisposeFlashbackPreviewBackendAsync(cancellationToken, purgeSegments: true).ConfigureAwait(false);

        var committedRestartToken = CancellationToken.None;
        var unifiedVideoCapture = _unifiedVideoCapture;
        var settings = _currentSettings;
        if (!_flashbackEnabled || unifiedVideoCapture == null || settings == null)
        {
            Logger.Log($"FLASHBACK_RESTART_TEARDOWN_ONLY enabled={_flashbackEnabled} capture={unifiedVideoCapture != null} settings={settings != null}");
            return;
        }

        await EnsureFlashbackPreviewBackendAsync(unifiedVideoCapture, settings, committedRestartToken).ConfigureAwait(false);
        Logger.Log("FLASHBACK_RESTART_OK");
        cancellationToken.ThrowIfCancellationRequested();
    }

}
