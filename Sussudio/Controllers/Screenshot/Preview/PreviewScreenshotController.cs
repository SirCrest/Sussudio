using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Sussudio.ViewModels;

namespace Sussudio.Controllers;

internal sealed class PreviewScreenshotControllerContext
{
    public required MainViewModel ViewModel { get; init; }
    public required Button ScreenshotButton { get; init; }
}

internal sealed class PreviewScreenshotController
{
    private readonly PreviewScreenshotControllerContext _context;

    public PreviewScreenshotController(PreviewScreenshotControllerContext context)
    {
        _context = context;
    }

    public async Task CaptureAsync()
    {
        if (!_context.ViewModel.IsPreviewing)
        {
            _context.ViewModel.StatusText = PreviewScreenshotPlanPolicy.PreviewRequiredStatusText;
            return;
        }

        var plan = PreviewScreenshotPlanPolicy.Create(
            _context.ViewModel.OutputPath,
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            DateTime.Now);
        Directory.CreateDirectory(plan.OutputDirectory);

        _context.ScreenshotButton.IsEnabled = false;
        try
        {
            var result = await _context.ViewModel.CapturePreviewFrameAsync(plan.FilePath);
            if (result.Succeeded)
            {
                _context.ViewModel.StatusText = PreviewScreenshotPlanPolicy.FormatSavedStatus(plan.FilePath);
                Logger.Log(PreviewScreenshotPlanPolicy.FormatSavedLog(plan.FilePath, result.CapturedWidth, result.CapturedHeight));
            }
            else
            {
                _context.ViewModel.StatusText = PreviewScreenshotPlanPolicy.FormatFailedStatus(result.Message);
                Logger.Log(PreviewScreenshotPlanPolicy.FormatFailedLog(result.Message));
            }
        }
        finally
        {
            _context.ScreenshotButton.IsEnabled = true;
        }
    }
}
