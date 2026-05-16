using System.Threading;
using System.Threading.Tasks;

namespace Sussudio.ViewModels;

/// <summary>
/// Automation recording lifecycle mutator.
/// </summary>
public partial class MainViewModel
{
    public Task SetRecordingEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        return SetRecordingDesiredStateAsync(enabled, cancellationToken);
    }
}
