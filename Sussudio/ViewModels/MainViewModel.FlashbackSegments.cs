using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sussudio.Models;

namespace Sussudio.ViewModels;

/// <summary>
/// Read-only Flashback segment projection for automation and UI callers.
/// </summary>
public partial class MainViewModel
{
    public IReadOnlyList<FlashbackSegmentInfo> GetFlashbackSegments()
        => _sessionCoordinator.GetFlashbackSegments();

    public Task<IReadOnlyList<FlashbackSegmentInfo>> GetFlashbackSegmentsAsync(CancellationToken cancellationToken = default)
        => FromSynchronousSnapshot(GetFlashbackSegments, cancellationToken);
}
