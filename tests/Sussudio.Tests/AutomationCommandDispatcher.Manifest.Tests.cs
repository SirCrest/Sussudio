using System.Threading.Tasks;

static partial class Program
{
    private static async Task AutomationCommandDispatcher_GetAutomationManifest_IsReadOnlyAndReadinessIndependent()
    {
        var dispatcher = CreateAutomationCommandDispatcher(authToken: null);
        var response = await ExecuteAutomationCommandAsync(
                dispatcher,
                CreateAutomationCommandRequest("GetAutomationManifest", authToken: null, payloadJson: "{}"))
            .ConfigureAwait(false);

        AssertAutomationResponse(response, success: true, errorCode: null, status: "ok", "manifest command succeeds without initialized devices");
        AssertEqual(null, GetPublicProperty(response, "Snapshot"), "manifest response omits snapshot");
        var data = GetPublicProperty(response, "Data")
                   ?? throw new InvalidOperationException("manifest response data was missing.");
        AssertEqual(1, (int)GetPublicProperty(data, "SchemaVersion")!, "manifest schema version");

        var commands = ((System.Collections.IEnumerable)GetPublicProperty(data, "Commands")!)
            .Cast<object>()
            .ToArray();
        var manifestCommand = commands.Single(command =>
            string.Equals((string)GetPublicProperty(command, "Name")!, "GetAutomationManifest", StringComparison.Ordinal));
        AssertEqual(51, (int)GetPublicProperty(manifestCommand, "Id")!, "manifest command id");
        AssertEqual("{}", (string)GetPublicProperty(manifestCommand, "PayloadShape")!, "manifest payload shape");
        AssertEqual(false, (bool)GetPublicProperty(manifestCommand, "RequiresReadyDevices")!, "manifest readiness flag");
        AssertEqual("None", (string)GetPublicProperty(manifestCommand, "PathPolicy")!, "manifest path policy");
        AssertEqual("manifest", (string)GetPublicProperty(manifestCommand, "CliHelp")!, "manifest CLI help");
        AssertEqual("Get automation command manifest.", (string)GetPublicProperty(manifestCommand, "McpDescription")!, "manifest MCP description");
    }
}
