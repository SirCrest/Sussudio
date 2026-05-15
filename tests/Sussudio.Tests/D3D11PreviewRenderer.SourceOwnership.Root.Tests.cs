using System.Threading.Tasks;

static partial class Program
{
    private static Task D3D11PreviewRenderer_ConfigurationLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var configurationText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.Configuration.cs")
            .Replace("\r\n", "\n");

        AssertContains(configurationText, "SUSSUDIO_PREVIEW_PRESENT_SYNC_INTERVAL");
        AssertContains(configurationText, "SUSSUDIO_PREVIEW_DXGI_MAX_FRAME_LATENCY");
        AssertContains(configurationText, "SUSSUDIO_PREVIEW_SWAPCHAIN_BUFFER_COUNT");
        AssertContains(configurationText, "SUSSUDIO_PREVIEW_RENDER_QUEUE_DEPTH");
        AssertContains(configurationText, "SUSSUDIO_PREVIEW_WAITABLE_SWAPCHAIN");
        AssertContains(configurationText, "SUSSUDIO_PREVIEW_DXGI_FRAME_STATS_SAMPLE_INTERVAL");
        AssertContains(configurationText, "SUSSUDIO_PREVIEW_RENDER_MMCSS_TASK\") ?? \"Playback\"");
        AssertContains(configurationText, "SUSSUDIO_PREVIEW_NATIVE_STOP_FENCE_TIMEOUT_MS");
        AssertContains(configurationText, "SUSSUDIO_PREVIEW_RENDER_THREAD_STOP_TIMEOUT_MS");
        AssertDoesNotContain(rootText, "SUSSUDIO_PREVIEW_PRESENT_SYNC_INTERVAL");
        AssertDoesNotContain(rootText, "SUSSUDIO_PREVIEW_RENDER_MMCSS_TASK");

        return Task.CompletedTask;
    }

    private static Task D3D11PreviewRenderer_NativeInteropLivesInFocusedPartial()
    {
        var rootText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.cs")
            .Replace("\r\n", "\n");
        var nativeInteropText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.NativeInterop.cs")
            .Replace("\r\n", "\n");
        var panelBindingText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.PanelBinding.cs")
            .Replace("\r\n", "\n");
        var shaderSourcesText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.ShaderSources.cs")
            .Replace("\r\n", "\n");
        var dxgiStatisticsText = ReadRepoFile("Sussudio/Services/Preview/D3D11PreviewRenderer.DxgiFrameStatistics.cs")
            .Replace("\r\n", "\n");

        AssertContains(nativeInteropText, "private interface ISwapChainPanelNative");
        AssertContains(nativeInteropText, "private interface ID3DBlob");
        AssertContains(nativeInteropText, "private static extern int D3DCompileNative(");
        AssertContains(nativeInteropText, "private static extern int DwmFlush()");
        AssertContains(panelBindingText, "WinRT.CastExtensions.As<ISwapChainPanelNative>(_panel)");
        AssertContains(shaderSourcesText, "D3DCompileNative(");
        AssertContains(dxgiStatisticsText, "_ = DwmFlush();");
        AssertDoesNotContain(rootText, "private interface ISwapChainPanelNative");
        AssertDoesNotContain(rootText, "private interface ID3DBlob");
        AssertDoesNotContain(rootText, "D3DCompileNative(");
        AssertDoesNotContain(rootText, "private static extern int DwmFlush()");

        return Task.CompletedTask;
    }
}
