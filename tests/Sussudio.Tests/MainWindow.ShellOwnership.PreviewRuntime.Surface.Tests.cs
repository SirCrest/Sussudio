using System.Threading.Tasks;

static partial class Program
{
    internal static Task PreviewSurfacePresentationAndShadow_LiveInControllers()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewRendererText = ReadMainWindowPreviewRendererAdapterSource();
        var previewSurfaceControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewSurfacePresentationController.cs").Replace("\r\n", "\n");
        var previewSurfaceShadowControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewSurfaceShadowController.cs").Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewSurface.cs")),
            "preview surface XAML adapter lives with preview renderer composition");
        AssertContains(previewRendererText, "XAML-facing preview surface adapter");
        AssertContains(previewRendererText, "private PreviewSurfacePresentationController _previewSurfacePresentationController = null!;");
        AssertContains(previewRendererText, "private PreviewSurfaceShadowController _previewSurfaceShadowController = null!;");
        AssertContains(previewRendererText, "private void InitializePreviewSurfacePresentationController()");
        AssertContains(previewRendererText, "private void UpdateVideoContentOverlays()");
        AssertContains(previewRendererText, "private void SetupVideoFrameShadow()");
        AssertContains(previewRendererText, "private void SetupControlBarShadow()");
        AssertContains(previewRendererText, "=> _previewSurfacePresentationController.UpdateVideoContentOverlays(ViewModel.SourceWidth, ViewModel.SourceHeight);");
        AssertContains(previewRendererText, "=> _previewSurfacePresentationController.SetGpuPreviewVisibility(visibility);");
        AssertContains(previewRendererText, "=> _previewSurfaceShadowController.SetupVideoFrameShadow();");
        AssertContains(previewRendererText, "=> _previewSurfaceShadowController.SetupControlBarShadow();");
        AssertContains(previewRendererText, "=> _previewSurfaceShadowController.ClearVideoFrameShadow();");
        AssertContains(previewRendererText, "=> _previewSurfaceShadowController.FadeInVideoFrameShadow(delayMs, durationMs);");
        AssertContains(previewRendererText, "var scale = PreviewSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;");
        AssertContains(previewRendererText, "_previewRendererHostController.OnPanelSizeChanged(e.NewSize.Width, e.NewSize.Height, scale);");

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
        AssertDoesNotContain(previewSurfaceControllerText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(previewSurfaceControllerText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(previewSurfaceControllerText, "ElementCompositionPreview.SetElementChildVisual");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _controlBarShadowVisual;");

        return Task.CompletedTask;
    }
}
