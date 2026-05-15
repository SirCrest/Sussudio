using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

internal sealed class PreviewAudioVolumeTransitionControllerContext
{
    public required Func<double> GetPreviewVolume { get; init; }
    public required Action<double> SetPreviewVolume { get; init; }
    public required Action<float> SetSessionPreviewVolume { get; init; }
    public required Func<string, double, long> BeginTraceSession { get; init; }
    public required Action<long, string> CompleteTraceSession { get; init; }
    public required Action<string, string?, double?, string?, long?> RecordTracePoint { get; init; }
    public required Action<string, string> Log { get; init; }
}

internal sealed class PreviewAudioVolumeTransitionController
{
    private const int RampDownSteps = 18;
    private const int RampDownDelayMs = 25;
    private const int RampUpSteps = 30;
    private const int RampUpDelayMs = 30;

    private readonly PreviewAudioVolumeTransitionControllerContext _context;

    public PreviewAudioVolumeTransitionController(PreviewAudioVolumeTransitionControllerContext context)
    {
        _context = context;
    }

    public bool SuppressVolumeSave { get; set; }

    public double? VolumeSaveOverride { get; set; }

    public double PersistedVolumeTarget => Math.Clamp(VolumeSaveOverride ?? _context.GetPreviewVolume(), 0.0, 1.0);

    public void HandlePreviewVolumeChanged(double value)
    {
        if (!SuppressVolumeSave)
        {
            VolumeSaveOverride = null;
        }

        _context.SetSessionPreviewVolume((float)Math.Clamp(value, 0.0, 1.0));
        RecordTracePoint("volume-set");
    }

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

    public double PrimeForAudioTransition(string reason)
    {
        var volumeTarget = PersistedVolumeTarget;
        if (volumeTarget <= 0.001)
        {
            _context.SetPreviewVolume(0);
            VolumeSaveOverride = null;
            return 0;
        }

        VolumeSaveOverride = volumeTarget;
        SuppressVolumeSave = true;
        try
        {
            _context.SetPreviewVolume(0);
        }
        finally
        {
            SuppressVolumeSave = false;
        }

        _context.Log(
            $"PREVIEW_AUDIO_MONITOR_PRIMED reason={reason} targetPct={volumeTarget * 100:0}",
            "PrimePreviewVolumeForAudioTransition");
        RecordTracePoint("primed", reason, volumeTarget);
        return volumeTarget;
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

    public void RestoreAfterUnavailableAudio(double volumeTarget, string reason)
    {
        volumeTarget = Math.Clamp(volumeTarget, 0.0, 1.0);
        SuppressVolumeSave = true;
        try
        {
            _context.SetPreviewVolume(volumeTarget);
        }
        finally
        {
            SuppressVolumeSave = false;
            VolumeSaveOverride = null;
        }

        _context.Log(
            $"PREVIEW_AUDIO_MONITOR_RESTORE reason={reason} targetPct={volumeTarget * 100:0}",
            "RestorePreviewVolumeAfterUnavailableAudio");
        RecordTracePoint("restore", reason, volumeTarget, "audio-preview-unavailable");
    }

    private long BeginTraceSession(string reason, double targetVolume)
        => _context.BeginTraceSession(reason, targetVolume);

    private void CompleteTraceSession(long sessionId, string reason)
        => _context.CompleteTraceSession(sessionId, reason);

    private void RecordTracePoint(
        string kind,
        string? reason = null,
        double? targetVolume = null,
        string? note = null,
        long? sessionId = null)
        => _context.RecordTracePoint(kind, reason, targetVolume, note, sessionId);
}
