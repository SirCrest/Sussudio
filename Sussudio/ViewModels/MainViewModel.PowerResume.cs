using System.Threading.Tasks;
using Microsoft.Win32;

namespace Sussudio.ViewModels;

/// <summary>
/// System resume handling for preview capture rebinds.
/// </summary>
public partial class MainViewModel
{
    // PowerModeChanged fires on the system thread pool - must not touch UI properties
    // directly. We act only on PowerModes.Resume; Suspend/StatusChange are ignored
    // (Suspend arrives just before the OS freezes the process so there's nothing
    // useful to do, and StatusChange fires on AC/battery transitions which don't
    // affect capture). All UI-state reads happen inside the EnqueueUiOperation
    // lambda, which executes on the DispatcherQueue thread. ReinitializeDeviceAsync's
    // IsRecording guard (fix #1) keeps this safe to call regardless of state.
    private void OnSystemPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode != PowerModes.Resume)
        {
            return;
        }

        Logger.Log("SYSTEM_RESUMING_EVENT received — scheduling capture rebind if previewing.");
        EnqueueUiOperation(() =>
        {
            if (!IsPreviewing || !IsInitialized || IsRecording)
            {
                Logger.Log(
                    $"SYSTEM_RESUMING_REINIT_SKIP previewing={IsPreviewing} " +
                    $"initialized={IsInitialized} recording={IsRecording}");
                return Task.CompletedTask;
            }

            Logger.Log("SYSTEM_RESUMING_REINIT_SCHEDULED");
            return ReinitializeDeviceAsync("system resume");
        }, "system resume reinit");
    }
}
