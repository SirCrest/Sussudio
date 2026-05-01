using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

static partial class Program
{
    // ── AutomationCommandDispatcher: payload extraction helpers ──

    private static Task AutomationCommandDispatcher_GetString_ExtractsFromJsonPayload()
    {
        var dispatcherType = RequireType("ElgatoCapture.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("GetString",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetString not found.");

        // Valid object with property
        var doc = JsonDocument.Parse("{\"name\": \"test\"}");
        var result = method.Invoke(null, new object[] { doc.RootElement, "name" })?.ToString();
        AssertEqual("test", result, "GetString extracts string property");

        // Missing property → null
        var missing = method.Invoke(null, new object[] { doc.RootElement, "missing" });
        AssertEqual(true, missing == null, "GetString returns null for missing property");

        // Non-object → null
        var arrayDoc = JsonDocument.Parse("[1,2,3]");
        var arrayResult = method.Invoke(null, new object[] { arrayDoc.RootElement, "name" });
        AssertEqual(true, arrayResult == null, "GetString returns null for non-object");

        return Task.CompletedTask;
    }

    private static Task AutomationCommandDispatcher_GetBool_ExtractsFromJsonPayload()
    {
        var dispatcherType = RequireType("ElgatoCapture.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("GetBool",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetBool not found.");

        // Valid bool
        var doc = JsonDocument.Parse("{\"enabled\": true, \"disabled\": false}");
        var trueResult = (bool?)method.Invoke(null, new object[] { doc.RootElement, "enabled" });
        AssertEqual(true, trueResult, "GetBool extracts true");

        var falseResult = (bool?)method.Invoke(null, new object[] { doc.RootElement, "disabled" });
        AssertEqual(false, falseResult, "GetBool extracts false");

        // Missing → null
        var missingResult = (bool?)method.Invoke(null, new object[] { doc.RootElement, "missing" });
        AssertEqual(true, missingResult == null, "GetBool returns null for missing");

        return Task.CompletedTask;
    }

    private static Task AutomationCommandDispatcher_GetInt_ExtractsFromJsonPayload()
    {
        var dispatcherType = RequireType("ElgatoCapture.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("GetInt",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetInt not found.");

        var doc = JsonDocument.Parse("{\"count\": 42, \"text\": \"hello\"}");
        var intResult = (int?)method.Invoke(null, new object[] { doc.RootElement, "count" });
        AssertEqual(42, intResult!.Value, "GetInt extracts integer");

        // String property → null (not an int)
        var textResult = (int?)method.Invoke(null, new object[] { doc.RootElement, "text" });
        AssertEqual(true, textResult == null, "GetInt returns null for string property");

        return Task.CompletedTask;
    }

    private static Task AutomationCommandDispatcher_GetDouble_ExtractsFromJsonPayload()
    {
        var dispatcherType = RequireType("ElgatoCapture.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("GetDouble",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetDouble not found.");

        var doc = JsonDocument.Parse("{\"volume\": 0.75}");
        var result = (double?)method.Invoke(null, new object[] { doc.RootElement, "volume" });
        AssertEqual(true, Math.Abs(result!.Value - 0.75) < 0.001, $"GetDouble extracts 0.75, got {result}");

        return Task.CompletedTask;
    }

    private static Task AutomationCommandDispatcher_RequireString_ThrowsOnMissing()
    {
        var dispatcherType = RequireType("ElgatoCapture.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("RequireString",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RequireString not found.");

        // Present → returns value
        var doc = JsonDocument.Parse("{\"path\": \"/output/file.mp4\"}");
        var result = method.Invoke(null, new object[] { doc.RootElement, "path" })?.ToString();
        AssertEqual("/output/file.mp4", result, "RequireString returns present value");

        // Missing → throws
        var threw = false;
        try
        {
            method.Invoke(null, new object[] { doc.RootElement, "missing" });
        }
        catch (TargetInvocationException)
        {
            threw = true;
        }
        AssertEqual(true, threw, "RequireString throws on missing property");

        return Task.CompletedTask;
    }

    // ── AutomationCommandDispatcher: RequiresReadyDevices ──

    private static Task AutomationCommandDispatcher_RequiresReadyDevices_ClassifiesCommands()
    {
        var dispatcherType = RequireType("ElgatoCapture.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("RequiresReadyDevices",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RequiresReadyDevices not found.");

        var commandType = RequireType("ElgatoCapture.Models.AutomationCommandKind");

        // UI/info commands should NOT require ready devices
        var getSnapshot = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "GetSnapshot") })!;
        AssertEqual(false, getSnapshot, "GetSnapshot does not require ready devices");

        var windowAction = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "WindowAction") })!;
        AssertEqual(false, windowAction, "WindowAction does not require ready devices");

        var authenticate = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "Authenticate") })!;
        AssertEqual(false, authenticate, "Authenticate does not require ready devices");

        var setFlashbackEnabled = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SetFlashbackEnabled") })!;
        AssertEqual(false, setFlashbackEnabled, "SetFlashbackEnabled does not require ready devices");

        // Capture configuration commands SHOULD require ready devices
        var setResolution = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SetResolution") })!;
        AssertEqual(true, setResolution, "SetResolution requires ready devices");

        var setFrameRate = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SetFrameRate") })!;
        AssertEqual(true, setFrameRate, "SetFrameRate requires ready devices");

        return Task.CompletedTask;
    }

    private static async Task AutomationCommandDispatcher_AuthorizesConfiguredTokens()
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

        var dispatcherText = ReadRepoFile("ElgatoCapture/Services/Automation/AutomationCommandDispatcher.cs")
            .Replace("\r\n", "\n");

        AssertContains(dispatcherText, "if (string.IsNullOrWhiteSpace(_authToken))\n        {\n            return true;\n        }");
        AssertContains(dispatcherText, "var providedToken = request.AuthToken;");
        AssertContains(dispatcherText, "providedToken = GetString(request.Payload, \"authToken\");");
        AssertContains(dispatcherText, "return string.Equals(_authToken, providedToken, StringComparison.Ordinal);");
        AssertContains(dispatcherText, "errorCode: authorized ? null : \"unauthorized\"");
        AssertContains(dispatcherText, "errorCode: \"unauthorized\"");
        AssertContains(dispatcherText, "status: authorized ? \"ok\" : \"error\"");

    }

    private static async Task AutomationCommandDispatcher_FlashbackActionFailure_ReturnsPlaybackDiagnostics()
    {
        var viewModelType = RequireType("ElgatoCapture.Services.Automation.IAutomationViewModel");
        var diagnosticsType = RequireType("ElgatoCapture.Services.Automation.IAutomationDiagnosticsHub");
        var windowControlType = RequireType("ElgatoCapture.Services.Automation.IAutomationWindowControl");
        var snapshotType = RequireType("ElgatoCapture.Models.AutomationSnapshot");
        var actionType = RequireType("ElgatoCapture.Models.AutomationFlashbackAction");

        var snapshot = Activator.CreateInstance(snapshotType)
                       ?? throw new InvalidOperationException("Failed to create AutomationSnapshot.");
        SetPropertyBackingField(snapshot, "FlashbackPlaybackState", "Paused");
        SetPropertyBackingField(snapshot, "FlashbackPlaybackThreadAlive", false);
        SetPropertyBackingField(snapshot, "FlashbackPlaybackPendingCommands", 2);
        SetPropertyBackingField(snapshot, "FlashbackPlaybackLastCommandFailure", "thread_not_running:Pause");
        SetPropertyBackingField(snapshot, "FlashbackPlaybackLastCommandFailureUtcUnixMs", 123456789L);

        var viewModel = CreateConfiguredProxy(viewModelType, (method, args) =>
        {
            if (method?.Name == "ExecuteFlashbackActionAsync")
            {
                AssertEqual(Enum.Parse(actionType, "Pause"), args![0], "dispatcher forwards pause action");
                return Task.FromResult(false);
            }

            return GetDefaultReturnValue(method);
        });
        var diagnostics = CreateConfiguredProxy(diagnosticsType, (method, _) =>
            method?.Name == "GetLatestSnapshot"
                ? snapshot
                : GetDefaultReturnValue(method));
        var dispatcher = CreateAutomationCommandDispatcher(
            viewModel,
            diagnostics,
            CreateThrowingProxy(windowControlType),
            authToken: null);
        var response = await ExecuteAutomationCommandAsync(
            dispatcher,
            CreateAutomationCommandRequest("FlashbackAction", authToken: null, payloadJson: "{\"action\":\"pause\"}"))
            .ConfigureAwait(false);

        AssertAutomationResponse(response, success: false, errorCode: "flashback-action-failed", status: "error", "failed flashback action includes structured error");
        var message = (string)GetPublicProperty(response, "Message")!;
        AssertContains(message, "Flashback action 'Pause' was rejected");
        AssertContains(message, "state=Paused");
        AssertContains(message, "threadAlive=False");
        AssertContains(message, "lastFailure=thread_not_running:Pause");

        var data = GetPublicProperty(response, "Data")
                   ?? throw new InvalidOperationException("Flashback failure response data was missing.");
        AssertEqual("Pause", (string)GetPublicProperty(data, "Action")!, "flashback failure data action");
        AssertEqual("Paused", (string)GetPublicProperty(data, "PlaybackState")!, "flashback failure data playback state");
        AssertEqual(false, (bool)GetPublicProperty(data, "PlaybackThreadAlive")!, "flashback failure data thread alive");
        AssertEqual(2, (int)GetPublicProperty(data, "PendingCommands")!, "flashback failure data pending commands");
        AssertEqual("thread_not_running:Pause", (string)GetPublicProperty(data, "LastCommandFailure")!, "flashback failure data last command failure");
        AssertEqual(123456789L, (long)GetPublicProperty(data, "LastCommandFailureUtcUnixMs")!, "flashback failure data failure utc");
        AssertEqual(snapshot, GetPublicProperty(response, "Snapshot"), "flashback failure response reuses diagnostic snapshot");
    }

    private static object CreateAutomationCommandDispatcher(string? authToken)
    {
        var dispatcherType = RequireType("ElgatoCapture.Services.Automation.AutomationCommandDispatcher");
        var viewModelType = RequireType("ElgatoCapture.Services.Automation.IAutomationViewModel");
        var diagnosticsType = RequireType("ElgatoCapture.Services.Automation.IAutomationDiagnosticsHub");
        var windowControlType = RequireType("ElgatoCapture.Services.Automation.IAutomationWindowControl");
        var constructor = dispatcherType.GetConstructors()
            .Single(ctor => ctor.GetParameters().Length == 4);

        return constructor.Invoke(new[]
        {
            CreateThrowingProxy(viewModelType),
            CreateThrowingProxy(diagnosticsType),
            CreateThrowingProxy(windowControlType),
            authToken
        });
    }

    private static object CreateAutomationCommandDispatcher(
        object viewModel,
        object diagnosticsHub,
        object windowControl,
        string? authToken)
    {
        var dispatcherType = RequireType("ElgatoCapture.Services.Automation.AutomationCommandDispatcher");
        var constructor = dispatcherType.GetConstructors()
            .Single(ctor => ctor.GetParameters().Length == 4);

        return constructor.Invoke(new[]
        {
            viewModel,
            diagnosticsHub,
            windowControl,
            authToken
        });
    }

    private static object CreateConfiguredProxy(Type interfaceType, Func<MethodInfo?, object?[]?, object?> handler)
    {
        var createMethod = typeof(DispatchProxy)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(method =>
                method.Name == "Create" &&
                method.IsGenericMethodDefinition &&
                method.GetGenericArguments().Length == 2)
            .MakeGenericMethod(interfaceType, typeof(ConfiguredAutomationProxy));
        var proxy = createMethod.Invoke(null, null)
                    ?? throw new InvalidOperationException($"Failed to create proxy for {interfaceType.FullName}.");
        ((ConfiguredAutomationProxy)proxy).Handler = handler;
        return proxy;
    }

    private static object? GetDefaultReturnValue(MethodInfo? method)
    {
        var returnType = method?.ReturnType ?? typeof(void);
        if (returnType == typeof(void))
        {
            return null;
        }

        if (returnType == typeof(Task))
        {
            return Task.CompletedTask;
        }

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var resultType = returnType.GetGenericArguments()[0];
            var result = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult), BindingFlags.Public | BindingFlags.Static)!
                .MakeGenericMethod(resultType);
            return fromResult.Invoke(null, new[] { result });
        }

        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }

    private static object CreateAutomationCommandRequest(
        string commandName,
        string? authToken,
        string payloadJson)
    {
        var requestType = RequireType("ElgatoCapture.Models.AutomationCommandRequest");
        var commandType = RequireType("ElgatoCapture.Models.AutomationCommandKind");
        var request = Activator.CreateInstance(requestType)
                      ?? throw new InvalidOperationException("Failed to create AutomationCommandRequest.");
        using var payload = JsonDocument.Parse(payloadJson);
        SetPropertyBackingField(request, "Command", Enum.Parse(commandType, commandName));
        SetPropertyBackingField(request, "CorrelationId", Guid.NewGuid().ToString("N"));
        SetPropertyBackingField(request, "AuthToken", authToken);
        SetPropertyBackingField(request, "Payload", payload.RootElement.Clone());
        return request;
    }

    private static async Task<object> ExecuteAutomationCommandAsync(object dispatcher, object request)
    {
        var execute = dispatcher.GetType().GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.Public)
                      ?? throw new InvalidOperationException("AutomationCommandDispatcher.ExecuteAsync was not found.");
        var task = (Task)execute.Invoke(dispatcher, new object[] { request, CancellationToken.None })!;
        await task.ConfigureAwait(false);
        return task.GetType().GetProperty("Result")?.GetValue(task)
               ?? throw new InvalidOperationException("AutomationCommandDispatcher.ExecuteAsync returned no result.");
    }

    private static void AssertAutomationResponse(
        object response,
        bool success,
        string? errorCode,
        string status,
        string scenario)
    {
        AssertEqual(success, (bool)GetPublicProperty(response, "Success")!, $"{scenario}: Success");
        AssertEqual(errorCode, (string?)GetPublicProperty(response, "ErrorCode"), $"{scenario}: ErrorCode");
        AssertEqual(status, (string)GetPublicProperty(response, "Status")!, $"{scenario}: Status");
    }

    private static object? GetPublicProperty(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)
                       ?? throw new InvalidOperationException($"{instance.GetType().Name}.{propertyName} was not found.");
        return property.GetValue(instance);
    }

    public class ConfiguredAutomationProxy : DispatchProxy
    {
        public Func<MethodInfo?, object?[]?, object?> Handler { get; set; } =
            (_, _) => throw new NotSupportedException("No handler configured.");

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => Handler(targetMethod, args);
    }

}
