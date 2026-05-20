using System.Threading.Tasks;

static partial class Program
{
    internal static async Task AutomationCommandDispatcher_AuthorizesConfiguredTokens()
    {
        var noTokenDispatcher = CreateAutomationCommandDispatcher(authToken: null);
        var noTokenResponse = await ExecuteAutomationCommandAsync(
            noTokenDispatcher,
            CreateAutomationCommandRequest("Authenticate", authToken: null, payloadJson: "{}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(noTokenResponse, success: true, errorCode: null, status: "ok", "no configured token accepts unauthenticated authenticate");

        var tokenDispatcher = CreateAutomationCommandDispatcher(authToken: "secret");
        var matchingTopLevelResponse = await ExecuteAutomationCommandAsync(
            tokenDispatcher,
            CreateAutomationCommandRequest("Authenticate", authToken: "secret", payloadJson: "{}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(matchingTopLevelResponse, success: true, errorCode: null, status: "ok", "matching top-level token is authorized");

        var matchingPayloadResponse = await ExecuteAutomationCommandAsync(
            tokenDispatcher,
            CreateAutomationCommandRequest("Authenticate", authToken: null, payloadJson: "{\"authToken\":\"secret\"}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(matchingPayloadResponse, success: true, errorCode: null, status: "ok", "payload fallback token is authorized");

        var missingTokenResponse = await ExecuteAutomationCommandAsync(
            tokenDispatcher,
            CreateAutomationCommandRequest("Authenticate", authToken: null, payloadJson: "{}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(missingTokenResponse, success: false, errorCode: "unauthorized", status: "error", "missing token is rejected");

        var wrongTokenResponse = await ExecuteAutomationCommandAsync(
            tokenDispatcher,
            CreateAutomationCommandRequest("Authenticate", authToken: "wrong", payloadJson: "{\"authToken\":\"secret\"}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(wrongTokenResponse, success: false, errorCode: "unauthorized", status: "error", "wrong top-level token is rejected before payload fallback");

        var protectedCommandResponse = await ExecuteAutomationCommandAsync(
            tokenDispatcher,
            CreateAutomationCommandRequest("GetSnapshot", authToken: null, payloadJson: "{}"))
            .ConfigureAwait(false);
        AssertAutomationResponse(protectedCommandResponse, success: false, errorCode: "unauthorized", status: "error", "missing token rejects non-authenticate command");

        var dispatcherText = ReadAutomationCommandDispatcherFamilyText();

        AssertContains(dispatcherText, "if (string.IsNullOrWhiteSpace(_authToken))\n        {\n            return true;\n        }");
        AssertContains(dispatcherText, "var providedToken = request.AuthToken;");
        AssertContains(dispatcherText, "providedToken = GetString(request.Payload, \"authToken\");");
        AssertContains(dispatcherText, "CryptographicOperations.FixedTimeEquals(expected, actual)");
        AssertContains(dispatcherText, "Logger.LogEvent(\"AUTH_FAILED\"");
        AssertContains(dispatcherText, "errorCode: authorized ? null : \"unauthorized\"");
        AssertContains(dispatcherText, "errorCode: \"unauthorized\"");
        AssertContains(dispatcherText, "status: authorized ? AutomationResponseStatus.Ok : AutomationResponseStatus.Error");

    }
}
