using System;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.Services.Flashback;

internal sealed partial class FlashbackEncoderSink
{
    Task IRecordingSink.StartAsync(RecordingContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return StartAsync(CreateSessionContext(context), cancellationToken);
    }

    public void BeginRecording(string outputPath)
    {
        lock (_sync)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FlashbackEncoderSink));
            }

            if (_encodingFailure != null)
            {
                throw new InvalidOperationException("Cannot begin recording: encoding loop has failed", _encodingFailure);
            }

            if (!_started)
            {
                throw new InvalidOperationException("Cannot begin recording: flashback encoder is not running.");
            }

            if (_encodingTask?.IsCompleted == true)
            {
                throw new InvalidOperationException("Cannot begin recording: encoding task has terminated.");
            }

            if (IsForceRotateActive)
            {
                throw new InvalidOperationException("Cannot begin recording: flashback export rotation is still draining.");
            }

            if (Volatile.Read(ref _recordingActive) != 0)
            {
                throw new InvalidOperationException("Cannot begin recording: flashback recording is already active.");
            }

            if (_bufferManager.IsSessionPreservedForRecovery)
            {
                throw new InvalidOperationException("Cannot begin recording: flashback session is preserved for recovery.");
            }

            _recordingOutputPath = outputPath ?? string.Empty;
            _bufferManager.PauseEviction();
            Volatile.Write(ref _recordingActive, 1);
        }
        Logger.Log($"FLASHBACK_RECORDING_ACTIVE output='{_recordingOutputPath}'");
    }

    public void CancelRecordingStartRollback(string reason)
    {
        if (Interlocked.Exchange(ref _recordingActive, 0) != 0)
        {
            ResumeEvictionBestEffort(_bufferManager, "recording_start_rollback");
            Logger.Log($"FLASHBACK_RECORDING_START_ROLLBACK reason='{reason}'");
        }
    }
}
