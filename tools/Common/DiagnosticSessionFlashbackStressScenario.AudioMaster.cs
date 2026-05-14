namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackStressScenario
{
    internal static string? ClassifyFlashbackStressAudioMasterFallbackWarning(
        long totalDelta,
        long unavailableDelta,
        long staleDelta,
        long driftOutlierDelta)
    {
        if (totalDelta <= 0)
        {
            return null;
        }

        if (staleDelta > 0 || driftOutlierDelta > 0)
        {
            return
                "flashback stress: audio-master harmful fallbacks increased during warmed playback " +
                $"staleDelta={staleDelta} driftOutlierDelta={driftOutlierDelta} " +
                $"totalDelta={totalDelta}";
        }

        if (unavailableDelta > FlashbackStressAudioUnavailableFallbackAllowance)
        {
            return
                "flashback stress: audio-master unavailable fallbacks exceeded startup allowance " +
                $"unavailableDelta={unavailableDelta} allowance={FlashbackStressAudioUnavailableFallbackAllowance} " +
                $"totalDelta={totalDelta}";
        }

        if (unavailableDelta <= 0)
        {
            return $"flashback stress: audio-master unclassified fallbacks increased during warmed playback delta={totalDelta}";
        }

        return null;
    }
}
