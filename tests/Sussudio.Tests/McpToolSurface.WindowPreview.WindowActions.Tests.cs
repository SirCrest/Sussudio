using System;
using System.Threading.Tasks;

static partial class Program
{
    internal static async Task McpWindowTools_RouteWindowActions()
    {
        var pipeName = NewMcpToolPipeName("window");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var windowTools = RequireMcpType("McpServer.Tools.WindowTools");

        string result = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 9,
                async () =>
                {
                    result = await InvokeMcpToolStringAsync(
                            windowTools,
                            "window_action",
                            pipeClient,
                            "snap_top_left",
                            true,
                            null,
                            null,
                            null,
                            null)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "window_action",
                            pipeClient,
                            "resize",
                            false,
                            null,
                            null,
                            1024,
                            768)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "window_action",
                            pipeClient,
                            "move",
                            false,
                            42,
                            84,
                            null,
                            null)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "window_action",
                            pipeClient,
                            "close",
                            true,
                            null,
                            null,
                            null,
                            null)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "window_action",
                            pipeClient,
                            " close ",
                            true,
                            null,
                            null,
                            null,
                            null)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "set_full_screen",
                            pipeClient,
                            true)
                        .ConfigureAwait(false);
                    result += Environment.NewLine + await InvokeMcpToolStringAsync(
                            windowTools,
                            "open_recordings_folder",
                            pipeClient)
                        .ConfigureAwait(false);
                },
                i => $$"""{"Success":true,"Message":"window command {{i}} ok"}""")
            .ConfigureAwait(false);

        AssertCommandRequest(requests[0], "WindowAction", ("action", "SnapTopLeft"));
        AssertCommandRequest(requests[1], "WindowAction", ("action", "Resize"), ("width", 1024), ("height", 768));
        AssertCommandRequest(requests[2], "WindowAction", ("action", "Move"), ("x", 42), ("y", 84));
        AssertCommandRequest(requests[3], "ArmClose", ("armed", true));
        AssertCommandRequest(requests[4], "WindowAction", ("action", "Close"));
        AssertCommandRequest(requests[5], "ArmClose", ("armed", true));
        AssertCommandRequest(requests[6], "WindowAction", ("action", "Close"));
        AssertCommandRequest(requests[7], "SetFullScreenEnabled", ("enabled", true));
        AssertCommandRequest(requests[8], "OpenRecordingsFolder");
        AssertEqual(
            string.Join(
                Environment.NewLine,
                "[OK] WindowAction: window command 0 ok",
                "[OK] WindowAction: window command 1 ok",
                "[OK] WindowAction: window command 2 ok",
                "[OK] ArmClose: window command 3 ok",
                "[OK] WindowAction: window command 4 ok",
                "[OK] ArmClose: window command 5 ok",
                "[OK] WindowAction: window command 6 ok",
                "[OK] SetFullScreenEnabled: window command 7 ok",
                "[OK] OpenRecordingsFolder: window command 8 ok"),
            result,
            "window_action ordered formatted output");
    }
}
