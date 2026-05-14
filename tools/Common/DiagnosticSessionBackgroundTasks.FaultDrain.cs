using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sussudio.Tools;

internal sealed partial class DiagnosticSessionBackgroundTasks
{
    internal async Task<DiagnosticSessionBackgroundTaskDrainResult> ObserveAfterFaultAsync(
        List<string> warnings,
        Action<string> setStage,
        Action<Exception, string> recordTerminalException,
        Func<Task> writeLiveStateBestEffortAsync,
        PresentMonProbeResult? presentMon,
        FlashbackRecordingSettingsDeferredPresetState recordingSettingsDeferredPresetState)
    {
        setStage("background-task-drain");
        foreach (var registration in _scenarioTasks.OrderBy(task => task.AwaitOrder))
        {
            await ObserveTaskAfterFaultAsync(
                    registration.Task,
                    registration.Stage,
                    warnings,
                    recordTerminalException)
                .ConfigureAwait(false);
        }

        presentMon = await ObservePresentMonTaskAfterFaultAsync(
                warnings,
                recordTerminalException,
                presentMon)
            .ConfigureAwait(false);
        recordingSettingsDeferredPresetState = await ObserveRecordingSettingsDeferredTaskAfterFaultAsync(
                warnings,
                recordTerminalException,
                recordingSettingsDeferredPresetState)
            .ConfigureAwait(false);
        await writeLiveStateBestEffortAsync().ConfigureAwait(false);

        return new DiagnosticSessionBackgroundTaskDrainResult(presentMon, recordingSettingsDeferredPresetState);
    }

    private static async Task ObserveTaskAfterFaultAsync(
        Task? task,
        string stage,
        List<string> warnings,
        Action<Exception, string> recordTerminalException)
    {
        if (task is null || task.IsCompletedSuccessfully)
        {
            return;
        }

        try
        {
            var completedTask = task.IsCompleted
                ? task
                : await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
            if (!ReferenceEquals(completedTask, task))
            {
                warnings.Add($"{stage}: task still running after diagnostic interruption");
                return;
            }

            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, stage);
        }
    }

    private async Task<PresentMonProbeResult?> ObservePresentMonTaskAfterFaultAsync(
        List<string> warnings,
        Action<Exception, string> recordTerminalException,
        PresentMonProbeResult? presentMon)
    {
        if (_presentMonTask is null || _presentMonTask.IsCompletedSuccessfully)
        {
            if (_presentMonTask is { IsCompletedSuccessfully: true } && presentMon is null)
            {
                presentMon = await _presentMonTask.ConfigureAwait(false);
            }

            return presentMon;
        }

        try
        {
            var completedTask = _presentMonTask.IsCompleted
                ? _presentMonTask
                : await Task.WhenAny(_presentMonTask, Task.Delay(TimeSpan.FromSeconds(2))).ConfigureAwait(false);
            if (!ReferenceEquals(completedTask, _presentMonTask))
            {
                warnings.Add("presentmon-task: task still running after diagnostic interruption");
                return presentMon;
            }

            presentMon = await _presentMonTask.ConfigureAwait(false);
            if (!presentMon.Success)
            {
                warnings.Add($"PresentMon failed: {presentMon.Message}");
            }

            return presentMon;
        }
        catch (Exception ex)
        {
            recordTerminalException(ex, "presentmon-task");
            return presentMon;
        }
    }

    private async Task<FlashbackRecordingSettingsDeferredPresetState> ObserveRecordingSettingsDeferredTaskAfterFaultAsync(
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
}
