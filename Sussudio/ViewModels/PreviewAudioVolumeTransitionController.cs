using System;

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

internal sealed partial class PreviewAudioVolumeTransitionController
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
