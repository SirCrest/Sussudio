using System.Threading.Tasks;

static partial class Program
{
    private static async Task SsctlCommandHandlers_RouteAutomationFlowCommands()
    {
        var context = CreateSsctlCommandRoutingContext();

        var assertPipeName = $"ssctl-assert-simple-{Guid.NewGuid():N}";
        var assertArguments = new List<string> { "assert", "IsRecording", "eq", "false" };
        var (assertExitCode, assertRequest) = await CaptureSsctlRequestAsync(
                context,
                assertPipeName,
                assertArguments)
            .ConfigureAwait(false);

        AssertEqual(0, assertExitCode, "assert simple exit code");
        var assertPayload = AssertSsctlCommandRequest(assertRequest, "AssertSnapshot")
            .GetProperty("assertions")[0];
        AssertEqual("IsRecording", assertPayload.GetProperty("field").GetString(), "assert simple field");
        AssertEqual("eq", assertPayload.GetProperty("op").GetString(), "assert simple op");
        AssertEqual(false, assertPayload.GetProperty("value").GetBoolean(), "assert simple value");

        var waitPipeName = $"ssctl-wait-{Guid.NewGuid():N}";
        var waitArguments = new List<string> { "wait", "preview-ready", "--timeout", "12500", "--poll", "250" };
        var (waitExitCode, waitRequest) = await CaptureSsctlRequestAsync(
                context,
                waitPipeName,
                waitArguments)
            .ConfigureAwait(false);

        AssertEqual(0, waitExitCode, "wait exit code");
        AssertSsctlCommandRequest(
            waitRequest,
            "WaitForCondition",
            ("condition", "preview-ready"),
            ("timeoutMs", 12500),
            ("pollMs", 250));

        var probePipeName = $"ssctl-probe-color-{Guid.NewGuid():N}";
        var probeArguments = new List<string> { "probe", "color" };
        var (probeExitCode, probeRequest) = await CaptureSsctlRequestAsync(
                context,
                probePipeName,
                probeArguments)
            .ConfigureAwait(false);

        AssertEqual(0, probeExitCode, "probe color exit code");
        AssertSsctlCommandRequestHasEmptyPayload(probeRequest, "ProbePreviewColor");
    }
}
