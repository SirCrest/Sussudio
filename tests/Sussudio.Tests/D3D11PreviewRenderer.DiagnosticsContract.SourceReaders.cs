// Source readers for D3D11 preview renderer diagnostics contract tests.
static partial class Program
{
    private readonly record struct D3D11PreviewRendererDiagnosticsContractSources(
        string Source,
        string RenderSource,
        string CaptureSource);

    private static D3D11PreviewRendererDiagnosticsContractSources ReadD3D11PreviewRendererDiagnosticsContractSources()
    {
        var source = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Configuration.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameTypes.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameOwnership.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DxgiFrameStatistics.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Submission.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Metrics.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.SlowFrameDiagnostics.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PendingFrames.cs");
        var renderSource = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.RenderThread.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Rendering.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderRendering.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Resources.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.InputResources.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DeviceLost.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameLatency.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Viewport.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.FrameUpload.cs")
            + "\n" + ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PanelBinding.cs");
        var captureSource = ReadUnifiedVideoCaptureSource();

        return new D3D11PreviewRendererDiagnosticsContractSources(
            source,
            renderSource,
            captureSource);
    }
}
