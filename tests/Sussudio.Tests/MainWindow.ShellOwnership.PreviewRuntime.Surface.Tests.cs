using System.Threading.Tasks;

static partial class Program
{
    internal static Task PreviewSurfacePresentationAndShadow_LiveInControllers()
    {
        var mainWindowText = ReadMainWindowCompositionSource();
        var previewRendererText = ReadMainWindowPreviewRendererAdapterSource();
        var previewSurfaceControllerText = ReadRepoFile("Sussudio/Controllers/Preview/PreviewSurfacePresentationController.cs").Replace("\r\n", "\n");

        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "MainWindow.PreviewSurface.cs")),
            "preview surface XAML adapter lives with preview renderer composition");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Controllers", "Preview", "PreviewSurfaceShadowController.cs")),
            "preview surface shadow controller lives with preview surface presentation owner");
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

        AssertContains(previewSurfaceControllerText, "internal sealed class PreviewSurfaceShadowController");
        AssertContains(previewSurfaceControllerText, "private SpriteVisual? _videoShadowVisual;");
        AssertContains(previewSurfaceControllerText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertContains(previewSurfaceControllerText, "public void UpdateVideoFrameBounds(double marginH, double marginV, double fitW, double fitH)");
        AssertContains(previewSurfaceControllerText, "public void ClearVideoFrameBounds()");
        AssertContains(previewSurfaceControllerText, "_videoShadowVisual.Size = Vector2.Zero;");
        AssertContains(previewSurfaceControllerText, "public void SetupVideoFrameShadow()");
        AssertContains(previewSurfaceControllerText, "public void SetupControlBarShadow()");
        AssertContains(previewSurfaceControllerText, "public void ClearVideoFrameShadow()");
        AssertContains(previewSurfaceControllerText, "public void FadeInVideoFrameShadow(int delayMs, int durationMs)");
        AssertContains(previewSurfaceControllerText, "public void FadeInControlBarShadow(int delayMs, int durationMs)");

        AssertDoesNotContain(mainWindowText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(mainWindowText, "private SpriteVisual? _controlBarShadowVisual;");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _videoShadowVisual;");
        AssertDoesNotContain(previewRendererText, "private SpriteVisual? _controlBarShadowVisual;");

        return Task.CompletedTask;
    }
}
