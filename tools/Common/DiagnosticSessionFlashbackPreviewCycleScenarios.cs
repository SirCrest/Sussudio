namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackPreviewCycleScenarios
{
    internal static bool IsPreviewCycleScenario(
        bool runFlashbackPreviewCycle,
        bool runFlashbackPlaybackPreviewCycle,
        bool runFlashbackRecordingPreviewCycle)
        => runFlashbackPreviewCycle || runFlashbackPlaybackPreviewCycle || runFlashbackRecordingPreviewCycle;
}
