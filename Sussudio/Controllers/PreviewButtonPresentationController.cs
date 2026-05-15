using Microsoft.UI.Xaml.Controls;

namespace Sussudio.Controllers;

internal sealed class PreviewButtonPresentationControllerContext
{
    public required Button PreviewButton { get; init; }
    public required FontIcon PreviewButtonIcon { get; init; }
}

internal sealed class PreviewButtonPresentationController
{
    private const string StopPreviewGlyph = "\uE71A";
    private const string StartPreviewGlyph = "\uE768";

    private readonly PreviewButtonPresentationControllerContext _context;

    public PreviewButtonPresentationController(PreviewButtonPresentationControllerContext context)
    {
        _context = context;
    }

    public void ShowStopPreview()
    {
        _context.PreviewButtonIcon.Glyph = StopPreviewGlyph;
        ToolTipService.SetToolTip(_context.PreviewButton, "Stop Preview");
    }

    public void ShowStartPreview()
    {
        _context.PreviewButtonIcon.Glyph = StartPreviewGlyph;
        ToolTipService.SetToolTip(_context.PreviewButton, "Start Preview");
    }
}
