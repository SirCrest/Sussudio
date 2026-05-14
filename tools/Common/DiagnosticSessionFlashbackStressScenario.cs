namespace Sussudio.Tools;

internal static partial class DiagnosticSessionFlashbackStressScenario
{
    internal const int FlashbackStressMaxPlaybackPendingCommands = 4;
    internal const int FlashbackStressMaxPlaybackCommandLatencyMs = 750;
    internal const double FlashbackStressPlaybackWarmSeconds = 10.0;
    internal const long FlashbackStressAudioUnavailableFallbackAllowance = 4;
    internal const int FlashbackScrubStressMaxPlaybackPendingCommands = 20;
}
