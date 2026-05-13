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
            _context.ViewModel.StatusText = "Start preview before capturing a screenshot";
            return;
        }

        var outputDir = _context.ViewModel.OutputPath;
        if (string.IsNullOrWhiteSpace(outputDir))
        {
            outputDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Sussudio");
        }

        Directory.CreateDirectory(outputDir);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var filePath = Path.Combine(outputDir, $"Screenshot_{timestamp}.png");

        _context.ScreenshotButton.IsEnabled = false;
        try
        {
            var result = await _context.ViewModel.CapturePreviewFrameAsync(filePath);
            if (result.Succeeded)
            {
                _context.ViewModel.StatusText = $"Screenshot saved: {Path.GetFileName(filePath)}";
                Logger.Log($"SCREENSHOT_SAVED path={filePath} width={result.CapturedWidth} height={result.CapturedHeight}");
            }
            else
            {
                _context.ViewModel.StatusText = $"Screenshot failed: {result.Message}";
                Logger.Log($"SCREENSHOT_FAILED reason={result.Message}");
            }
        }
        finally
        {
            _context.ScreenshotButton.IsEnabled = true;
        }
    }
}
