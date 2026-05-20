using System.Threading.Tasks;

static partial class Program
{
    internal static async Task McpPreviewTools_RoutePreviewToggle()
    {
        var pipeName = NewMcpToolPipeName("preview");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var previewTools = RequireMcpType("McpServer.Tools.PreviewTools");

        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 1,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            previewTools,
                            "control_preview",
                            pipeClient,
                            true)
                        .ConfigureAwait(false);
                },
                _ => "{\"Success\":true,\"Message\":\"preview started\"}")
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "SetPreviewEnabled", ("enabled", true));
        AssertEqual("[OK] SetPreviewEnabled: preview started", result, "control_preview formatted success");
    }
}
