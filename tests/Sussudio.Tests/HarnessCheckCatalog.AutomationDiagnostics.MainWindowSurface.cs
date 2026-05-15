using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddAutomationDiagnosticsMainWindowSurfaceChecksAsync(List<CheckResult> results)
    {
        await AddCheckAsync(results,
            "MainWindow automation IDs cover the agent-critical UI surface",
            MainWindowAutomationIds_CoverAgentCriticalSurface);
        await AddCheckAsync(results,
            "MainWindow full-screen automation awaits transition tasks",
            MainWindowFullScreenAutomation_AwaitsTransitionTask);
        await AddCheckAsync(results,
            "MainWindow window automation commands live in controller",
            MainWindowWindowAutomationCommands_LiveInController);
        await AddCheckAsync(results,
            "MainWindow UI dispatching lives in dispatching partial",
            MainWindowUiDispatching_LivesInDispatchingPartial);
    }
}
