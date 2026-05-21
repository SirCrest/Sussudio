namespace Sussudio.Tools;

internal static partial class DiagnosticSessionScenarioSetup
{
    private readonly record struct DiagnosticSessionFlashbackSetupResult(
        bool EnabledFlashback,
        bool DisabledFlashback);
}

internal readonly record struct DiagnosticSessionScenarioSetupResult(
    bool StartedPreview,
    bool StartedRecording,
    bool EnabledFlashback,
    bool DisabledFlashback);
