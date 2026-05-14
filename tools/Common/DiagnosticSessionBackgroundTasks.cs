using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sussudio.Tools;

internal sealed partial class DiagnosticSessionBackgroundTasks
{
    private readonly List<DiagnosticSessionBackgroundTaskRegistration> _scenarioTasks = [];
    private Task<PresentMonProbeResult>? _presentMonTask;
    private Task<FlashbackRecordingSettingsDeferredPresetState>? _recordingSettingsDeferredTask;

    internal void AddScenario(int awaitOrder, string stage, Task task)
    {
        _scenarioTasks.Add(new DiagnosticSessionBackgroundTaskRegistration(awaitOrder, stage, task));
    }

    internal void SetPresentMon(Task<PresentMonProbeResult> task)
    {
        _presentMonTask = task;
    }

    internal void SetRecordingSettingsDeferred(Task<FlashbackRecordingSettingsDeferredPresetState> task)
    {
        _recordingSettingsDeferredTask = task;
    }

    internal async Task AwaitScenarioTasksAsync()
    {
        foreach (var registration in _scenarioTasks.OrderBy(task => task.AwaitOrder))
        {
            await registration.Task.ConfigureAwait(false);
        }
    }

    internal async Task<FlashbackRecordingSettingsDeferredPresetState> AwaitRecordingSettingsDeferredAsync(
        FlashbackRecordingSettingsDeferredPresetState current)
    {
        return _recordingSettingsDeferredTask is null
            ? current
            : await _recordingSettingsDeferredTask.ConfigureAwait(false);
    }

    internal async Task<PresentMonProbeResult?> AwaitPresentMonAsync(
        PresentMonProbeResult? current,
        List<string> warnings)
    {
        if (_presentMonTask is null)
        {
            return current;
        }

        var result = await _presentMonTask.ConfigureAwait(false);
        if (!result.Success)
        {
            warnings.Add($"PresentMon failed: {result.Message}");
        }

        return result;
    }

}
