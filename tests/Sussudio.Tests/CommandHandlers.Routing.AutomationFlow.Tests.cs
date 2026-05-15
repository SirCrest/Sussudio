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
        AssertEqual(22, assertRequest.GetProperty("command").GetInt32(), "assert simple command id");
        var assertPayload = assertRequest.GetProperty("payload").GetProperty("assertions")[0];
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
        AssertEqual(20, waitRequest.GetProperty("command").GetInt32(), "wait command id");
        var waitPayload = waitRequest.GetProperty("payload");
        AssertEqual("preview-ready", waitPayload.GetProperty("condition").GetString(), "wait condition payload");
        AssertEqual(12500, waitPayload.GetProperty("timeoutMs").GetInt32(), "wait timeout payload");
        AssertEqual(250, waitPayload.GetProperty("pollMs").GetInt32(), "wait poll payload");

        var probePipeName = $"ssctl-probe-color-{Guid.NewGuid():N}";
        var probeArguments = new List<string> { "probe", "color" };
        var (probeExitCode, probeRequest) = await CaptureSsctlRequestAsync(
                context,
                probePipeName,
                probeArguments)
            .ConfigureAwait(false);

        AssertEqual(0, probeExitCode, "probe color exit code");
        AssertEqual(25, probeRequest.GetProperty("command").GetInt32(), "probe color command id");
    }
}
