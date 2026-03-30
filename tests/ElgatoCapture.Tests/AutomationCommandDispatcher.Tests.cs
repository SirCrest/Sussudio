using System.Reflection;
using System.Text.Json;
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

        // Capture configuration commands SHOULD require ready devices
        var setResolution = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SetResolution") })!;
        AssertEqual(true, setResolution, "SetResolution requires ready devices");

        var setFrameRate = (bool)method.Invoke(null, new[] { Enum.Parse(commandType, "SetFrameRate") })!;
        AssertEqual(true, setFrameRate, "SetFrameRate requires ready devices");

        return Task.CompletedTask;
    }
}
