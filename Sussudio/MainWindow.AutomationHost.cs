using System.Threading.Tasks;
using Sussudio.Controllers;

namespace Sussudio;

// Shell-facing adapter for automation host lifecycle. Composition, once-only
// startup, ready/disabled logging, and disposal live in the controller.
public sealed partial class MainWindow
{
    private readonly WindowAutomationHostLifecycleController _automationHostLifecycleController;

    private void StartAutomationServices()
        => _automationHostLifecycleController.Start();

    private ValueTask DisposeAutomationHostAsync()
        => _automationHostLifecycleController.DisposeAsync();
}
