using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

internal sealed partial class PreviewAudioVolumeTransitionController
{
    public Task RampDownForStopAsync(CancellationToken cancellationToken)
        => RampDownForAudioTransitionAsync("preview_stop", cancellationToken);

    public async Task RampDownForAudioTransitionAsync(
        string reason,
        CancellationToken cancellationToken = default,
        bool traceSession = true)
    {
        var persistedVolume = PersistedVolumeTarget;
        var startingVolume = Math.Clamp(_context.GetPreviewVolume(), 0.0, 1.0);
        var traceSessionId = traceSession ? BeginTraceSession(reason, targetVolume: 0) : 0;
        if (persistedVolume > 0.001)
        {
            VolumeSaveOverride = persistedVolume;
        }

        try
        {
            if (startingVolume <= 0.001)
            {
                SuppressVolumeSave = true;
                try
                {
                    _context.SetPreviewVolume(0);
                }
                finally
                {
                    SuppressVolumeSave = false;
                }

                RecordTracePoint("ramp-down-skipped", reason, targetVolume: 0, note: "already-zero");
                return;
            }

            SuppressVolumeSave = true;
            var startLog = string.Equals(reason, "preview_stop", StringComparison.Ordinal)
                ? $"PREVIEW_AUDIO_STOP_RAMP_STARTED fromPct={startingVolume * 100:0}"
                : $"PREVIEW_AUDIO_RAMP_DOWN_STARTED reason={reason} fromPct={startingVolume * 100:0}";
            _context.Log(startLog, "RampPreviewVolumeDownForAudioTransitionAsync");
            RecordTracePoint("ramp-down-start", reason, targetVolume: 0);
            try
            {
                for (var step = 1; step <= RampDownSteps; step++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var t = step / (double)RampDownSteps;
                    var eased = Math.Pow(1.0 - t, 2.0);
                    _context.SetPreviewVolume(startingVolume * eased);
                    await Task.Delay(RampDownDelayMs, cancellationToken);
                }

                _context.SetPreviewVolume(0);
                RecordTracePoint("ramp-down-complete", reason, targetVolume: 0);
                _context.Log(
                    string.Equals(reason, "preview_stop", StringComparison.Ordinal)
                        ? "PREVIEW_AUDIO_STOP_RAMP_COMPLETED"
                        : $"PREVIEW_AUDIO_RAMP_DOWN_COMPLETED reason={reason}",
                    "RampPreviewVolumeDownForAudioTransitionAsync");
            }
            finally
            {
                SuppressVolumeSave = false;
            }
        }
        finally
        {
            if (traceSession)
            {
                CompleteTraceSession(traceSessionId, reason);
            }
        }
    }

    public async Task RampUpForAudioTransitionAsync(
        double volumeTarget,
        string reason,
        CancellationToken cancellationToken = default,
        bool traceSession = true)
    {
        volumeTarget = Math.Clamp(volumeTarget, 0.0, 1.0);
        var traceSessionId = traceSession ? BeginTraceSession(reason, volumeTarget) : 0;
        if (volumeTarget <= 0.001)
        {
            try
            {
                _context.SetPreviewVolume(0);
                VolumeSaveOverride = null;
                RecordTracePoint("ramp-up-skipped", reason, volumeTarget, "target-zero");
            }
            finally
            {
                if (traceSession)
                {
                    CompleteTraceSession(traceSessionId, reason);
                }
            }

            return;
        }

        VolumeSaveOverride = volumeTarget;
        SuppressVolumeSave = true;
        _context.Log(
            $"PREVIEW_AUDIO_RAMP_UP_STARTED reason={reason} targetPct={volumeTarget * 100:0}",
            "RampPreviewVolumeUpForAudioTransitionAsync");
        RecordTracePoint("ramp-up-start", reason, volumeTarget);
        try
        {
            for (var step = 1; step <= RampUpSteps; step++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var t = step / (double)RampUpSteps;
                var eased = 1.0 - Math.Pow(1.0 - t, 3.0);
                _context.SetPreviewVolume(volumeTarget * eased);
                await Task.Delay(RampUpDelayMs, cancellationToken);
            }

            _context.SetPreviewVolume(volumeTarget);
            RecordTracePoint("ramp-up-complete", reason, volumeTarget);
            _context.Log(
                $"PREVIEW_AUDIO_RAMP_UP_COMPLETED reason={reason}",
                "RampPreviewVolumeUpForAudioTransitionAsync");
        }
        finally
        {
            SuppressVolumeSave = false;
            VolumeSaveOverride = null;
            if (traceSession)
            {
                CompleteTraceSession(traceSessionId, reason);
            }
        }
    }
}
