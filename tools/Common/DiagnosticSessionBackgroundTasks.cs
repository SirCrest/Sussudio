using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sussudio.Tools;

internal readonly record struct DiagnosticSessionBackgroundTaskRegistration(
    int AwaitOrder,
    string Stage,
    Task Task);

internal sealed partial class DiagnosticSessionBackgroundTasks
{
    private readonly List<DiagnosticSessionBackgroundTaskRegistration> _scenarioTasks = [];

    internal void AddScenario(int awaitOrder, string stage, Task task)
    {
        _scenarioTasks.Add(new DiagnosticSessionBackgroundTaskRegistration(awaitOrder, stage, task));
    }

    internal async Task<FlashbackRecordingSettingsDeferredPresetState> CompleteRegisteredScenarioWorkAsync(
        FlashbackRecordingSettingsDeferredPresetState recordingSettingsDeferredPresetState)
    {
        await AwaitScenarioTasksAsync().ConfigureAwait(false);
        return await AwaitRecordingSettingsDeferredAsync(recordingSettingsDeferredPresetState).ConfigureAwait(false);
    }

    private async Task AwaitScenarioTasksAsync()
    {
        foreach (var registration in _scenarioTasks.OrderBy(task => task.AwaitOrder))
        {
            await registration.Task.ConfigureAwait(false);
        }
    }
}
