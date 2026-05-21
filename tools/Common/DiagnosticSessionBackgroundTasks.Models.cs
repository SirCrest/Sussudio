using System.Threading.Tasks;

namespace Sussudio.Tools;

internal readonly record struct DiagnosticSessionBackgroundTaskDrainResult(
    PresentMonProbeResult? PresentMon,
    FlashbackRecordingSettingsDeferredPresetState RecordingSettingsDeferredPresetState);

internal readonly record struct DiagnosticSessionBackgroundTaskRegistration(
    int AwaitOrder,
    string Stage,
    Task Task);
