using System.Threading.Tasks;

namespace Sussudio.Tools;

internal sealed partial class DiagnosticSessionBackgroundTasks
{
    private Task<FlashbackRecordingSettingsDeferredPresetState>? _recordingSettingsDeferredTask;

    internal void SetRecordingSettingsDeferred(Task<FlashbackRecordingSettingsDeferredPresetState> task)
    {
        _recordingSettingsDeferredTask = task;
    }

    private async Task<FlashbackRecordingSettingsDeferredPresetState> AwaitRecordingSettingsDeferredAsync(
        FlashbackRecordingSettingsDeferredPresetState current)
    {
        return _recordingSettingsDeferredTask is null
            ? current
            : await _recordingSettingsDeferredTask.ConfigureAwait(false);
    }
}
