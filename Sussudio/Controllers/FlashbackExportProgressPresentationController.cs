using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Sussudio.Controllers;

internal sealed class FlashbackExportProgressPresentationControllerContext
{
    public required ProgressBar FlashbackExportProgressBar { get; init; }
}

internal sealed class FlashbackExportProgressPresentationController
{
    private readonly FlashbackExportProgressPresentationControllerContext _context;

    public FlashbackExportProgressPresentationController(FlashbackExportProgressPresentationControllerContext context)
    {
        _context = context;
    }

    public void UpdateProgress(double progress)
    {
        _context.FlashbackExportProgressBar.Value = progress;
    }

    public void UpdateExporting(bool isExporting)
    {
        _context.FlashbackExportProgressBar.Visibility = isExporting
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (!isExporting)
        {
            _context.FlashbackExportProgressBar.Value = 0;
        }
    }
}
