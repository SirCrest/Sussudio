using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sussudio.Tools;

internal readonly record struct DiagnosticSessionBackgroundTaskDrainResult(
    PresentMonProbeResult? PresentMon,
    FlashbackRecordingSettingsDeferredPresetState RecordingSettingsDeferredPresetState);

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

        presentMon = await ObservePresentMonAfterFaultAsync(
                warnings,
                recordTerminalException,
                presentMon)
            .ConfigureAwait(false);
        recordingSettingsDeferredPresetState = await ObserveRecordingSettingsDeferredAfterFaultAsync(
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
}
