using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Preview reinitialization compatibility facade.
/// </summary>
public partial class MainViewModel
{
    private Task ReinitializeDeviceAsync(string reason)
        => _previewLifecycleController.ReinitializeDeviceAsync(reason);
}
