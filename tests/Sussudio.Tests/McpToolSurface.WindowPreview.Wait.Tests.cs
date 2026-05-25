using System.Threading.Tasks;

static partial class Program
{
    internal static Task McpWaitTools_UsesCatalogResponseTimeoutForConditionWaits()
    {
        var waitToolsSource = ReadRepoFile("tools/McpServer/Tools/WindowTools.cs");
        AssertContains(waitToolsSource, "AutomationPipeProtocol.GetDefaultResponseTimeout(AutomationCommandKind.WaitForCondition)");
        AssertContains(waitToolsSource, "SendCommandAsync(AutomationCommandKind.WaitForCondition, payload, responseTimeoutMs)");
        AssertDoesNotContain(waitToolsSource, "WaitForConditionCommandName");
        AssertDoesNotContain(waitToolsSource, "SendCommandAsync(\"WaitForCondition\"");
        AssertDoesNotContain(waitToolsSource, "AutomationPipeProtocol.DefaultResponseTimeoutMs");

        var waitTools = RequireMcpType("McpServer.Tools.WaitTools");
        var timeoutMethod = RequireNonPublicStaticMethod(waitTools, "GetWaitForConditionResponseTimeoutMs");

        AssertEqual(
            Sussudio.Tools.AutomationPipeProtocol.ExtendedResponseTimeoutMs,
            (int)timeoutMethod.Invoke(null, new object[] { 10000 })!,
            "MCP wait default pipe response timeout follows catalog policy");
        AssertEqual(
            65000,
            (int)timeoutMethod.Invoke(null, new object[] { 60000 })!,
            "MCP wait explicit timeout keeps response buffer");
        AssertEqual(
            int.MaxValue,
            (int)timeoutMethod.Invoke(null, new object[] { int.MaxValue })!,
            "MCP wait response timeout saturates on extreme input");

        return Task.CompletedTask;
    }

    internal static async Task McpWaitTools_RouteConditionWaits()
    {
        var pipeName = NewMcpToolPipeName("wait");
        var pipeClient = CreateMcpPipeClient(pipeName);
        var waitTools = RequireMcpType("McpServer.Tools.WaitTools");

        string metResult = string.Empty;
        string notMetResult = string.Empty;
        var requests = await CapturePipeRequestsAsync(
                pipeName,
                expectedCount: 2,
                async () =>
                {
                    metResult = await InvokeMcpToolStringAsync(
                            waitTools,
                            "wait_for_condition",
                            pipeClient,
                            "PreviewFramesActive",
                            750,
                            50)
                        .ConfigureAwait(false);
                    notMetResult = await InvokeMcpToolStringAsync(
                            waitTools,
                            "wait_for_condition",
                            pipeClient,
                            "RecordingStopped",
                            100,
                            10)
                        .ConfigureAwait(false);
                },
                i => i == 0
                    ? """
                      {
                        "Success": true,
                        "Message": "preview frames flowing",
                        "Data": {
                          "condition": "PreviewFramesActive",
                          "met": true,
                          "timeoutMs": 750,
                          "pollMs": 50
                        }
                      }
                      """
                    : """
                      {
                        "Success": false,
                        "Message": "recording still active",
                        "Data": {
                          "condition": "RecordingStopped",
                          "met": false,
                          "timeoutMs": 250,
                          "pollMs": 25
                        }
                      }
                      """)
            .ConfigureAwait(false);

        AssertCommandRequest(
            requests[0],
            "WaitForCondition",
            ("condition", "PreviewFramesActive"),
            ("timeoutMs", 750),
            ("pollMs", 50));
        AssertCommandRequest(
            requests[1],
            "WaitForCondition",
            ("condition", "RecordingStopped"),
            ("timeoutMs", 100),
            ("pollMs", 10));
        AssertContainsOrdinal(metResult, "Condition result: MET");
        AssertContainsOrdinal(metResult, "Met: true");
        AssertContainsOrdinal(metResult, "Condition: PreviewFramesActive");
        AssertContainsOrdinal(notMetResult, "Condition result: NOT MET");
        AssertContainsOrdinal(notMetResult, "Met: false");
        AssertContainsOrdinal(notMetResult, "TimeoutMs: 250");
        AssertContainsOrdinal(notMetResult, "PollMs: 25");
    }
}
