using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sussudio.Tools;

internal sealed partial class DiagnosticSessionBackgroundTasks
{
    private Task<FlashbackRecordingSettingsDeferredPresetState>? _recordingSettingsDeferredTask;

    internal void SetRecordingSettingsDeferred(Task<FlashbackRecordingSettingsDeferredPresetState> task)
    {
        _recordingSettingsDeferredTask = task;
    }

    private async Task<FlashbackRecordingSettingsDeferredPresetState> ObserveRecordingSettingsDeferredAfterFaultAsync(
        List<string> warnings,
        Action<Exception, string> recordTerminalException,
        FlashbackRecordingSettingsDeferredPresetState recordingSettingsDeferredPresetState)
    {
        if (_recordingSettingsDeferredTask is null || _recordingSettingsDeferredTask.IsCompletedSuccessfully)
        {
            if (_recordingSettingsDeferredTask is { IsCompletedSuccessfully: true })
            {
                recordingSettingsDeferredPresetState = await _recordingSettingsDeferredTask.ConfigureAwait(false);
            }

            return recordingSettingsDeferredPresetState;
        }

        try
        {
            var completedTask = _recordingSettingsDeferredTask.IsCompleted
                ? _recordingSettingsDeferredTask
                : await Task.WhenAny(_recordingSettingsDeferredTask, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
            if (!ReferenceEquals(completedTask, _recordingSettingsDeferredTask))
            {
                warnings.Add("flashback-recording-settings-deferred-task: task still running after diagnostic interruption");
                return recordingSettingsDeferredPresetState;
            }

            return await _recordingSettingsDeferredTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "flashback-recording-settings-deferred-task");
            return recordingSettingsDeferredPresetState;
        }
    }

    private async Task<FlashbackRecordingSettingsDeferredPresetState> AwaitRecordingSettingsDeferredAsync(
        FlashbackRecordingSettingsDeferredPresetState current)
    {
        return _recordingSettingsDeferredTask is null
            ? current
            : await _recordingSettingsDeferredTask.ConfigureAwait(false);
    }
}
