using System.Threading.Tasks;

static partial class Program
{
    internal static Task PreviewSurfacePresentationAndShadow_LiveInControllers()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewRendererText = ReadMainWindowPreviewRendererAdapterSource();
        var previewSurfaceText = ReadRepoFile("Sussudio/MainWindow.PreviewSurface.cs").Replace("\r\n", "\n");
        var previewSurfaceControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewSurfacePresentationController.cs").Replace("\r\n", "\n");
        var previewSurfaceShadowControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewSurfaceShadowController.cs").Replace("\r\n", "\n");

        AssertContains(previewSurfaceText, "XAML-facing preview surface adapter");
        AssertContains(previewSurfaceText, "private PreviewSurfacePresentationController _previewSurfacePresentationController = null!;");
        AssertContains(previewSurfaceText, "private PreviewSurfaceShadowController _previewSurfaceShadowController = null!;");
        AssertContains(previewSurfaceText, "private void InitializePreviewSurfacePresentationController()");
        AssertContains(previewSurfaceText, "private void UpdateVideoContentOverlays()");
        AssertContains(previewSurfaceText, "private void SetupVideoFrameShadow()");
        AssertContains(previewSurfaceText, "private void SetupControlBarShadow()");
        AssertContains(previewSurfaceText, "=> _previewSurfacePresentationController.UpdateVideoContentOverlays(ViewModel.SourceWidth, ViewModel.SourceHeight);");
        AssertContains(previewSurfaceText, "=> _previewSurfacePresentationController.SetGpuPreviewVisibility(visibility);");
        AssertContains(previewSurfaceText, "=> _previewSurfaceShadowController.SetupVideoFrameShadow();");
        AssertContains(previewSurfaceText, "=> _previewSurfaceShadowController.SetupControlBarShadow();");
        AssertContains(previewSurfaceText, "=> _previewSurfaceShadowController.ClearVideoFrameShadow();");
        AssertContains(previewSurfaceText, "=> _previewSurfaceShadowController.FadeInVideoFrameShadow(delayMs, durationMs);");
        AssertContains(previewSurfaceText, "var scale = PreviewSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;");
        AssertContains(previewSurfaceText, "_previewRendererHostController.OnPanelSizeChanged(e.NewSize.Width, e.NewSize.Height, scale);");

        AssertContains(previewSurfaceControllerText, "internal sealed class PreviewSurfacePresentationController");
        AssertContains(previewSurfaceControllerText, "public required Func<SwapChainPanel> GetPreviewSwapChainPanel { get; init; }");
        AssertContains(previewSurfaceControllerText, "private readonly PreviewSurfaceShadowController _shadowController;");
        AssertContains(previewSurfaceControllerText, "PreviewSurfaceShadowController shadowController)");
        AssertContains(previewSurfaceControllerText, "var previewSwapChainPanel = _context.GetPreviewSwapChainPanel();");
        AssertContains(previewSurfaceControllerText, "public void UpdateVideoContentOverlays(int? sourceWidth, int? sourceHeight)");
        AssertContains(previewSurfaceControllerText, "_shadowController.ClearVideoFrameBounds();");
        AssertContains(previewSurfaceControllerText, "_shadowController.UpdateVideoFrameBounds(marginH, marginV, fitW, fitH);");

        AssertContains(previewSurfaceShadowControllerText, "internal sealed class PreviewSurfaceShadowController");
        AssertContains(previewSurfaceShadowControllerText, "private SpriteVisual? _videoShadowVisual;");
        AssertContains(previewSurfaceShadowControllerText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertContains(previewSurfaceShadowControllerText, "public void UpdateVideoFrameBounds(double marginH, double marginV, double fitW, double fitH)");
        AssertContains(previewSurfaceShadowControllerText, "public void ClearVideoFrameBounds()");
        AssertContains(previewSurfaceShadowControllerText, "_videoShadowVisual.Size = Vector2.Zero;");
        AssertContains(previewSurfaceShadowControllerText, "public void SetupVideoFrameShadow()");
        AssertContains(previewSurfaceShadowControllerText, "public void SetupControlBarShadow()");
        AssertContains(previewSurfaceShadowControllerText, "public void ClearVideoFrameShadow()");
        AssertContains(previewSurfaceShadowControllerText, "public void FadeInVideoFrameShadow(int delayMs, int durationMs)");
        AssertContains(previewSurfaceShadowControllerText, "public void FadeInControlBarShadow(int delayMs, int durationMs)");

        AssertDoesNotContain(mainWindowText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(mainWindowText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(previewSurfaceText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(previewSurfaceText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(previewSurfaceControllerText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(previewSurfaceControllerText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(previewSurfaceControllerText, "ElementCompositionPreview.SetElementChildVisual");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _controlBarShadowVisual;");

        return Task.CompletedTask;
    }
}
