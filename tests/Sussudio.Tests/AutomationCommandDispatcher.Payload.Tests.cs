using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

static partial class Program
{
    internal static Task AutomationCommandDispatcher_GetString_ExtractsFromJsonPayload()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("GetString",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetString not found.");

        var doc = JsonDocument.Parse("{\"name\": \"test\"}");
        var result = method.Invoke(null, new object[] { doc.RootElement, "name" })?.ToString();
        AssertEqual("test", result, "GetString extracts string property");

        var missing = method.Invoke(null, new object[] { doc.RootElement, "missing" });
        AssertEqual(true, missing == null, "GetString returns null for missing property");

        var arrayDoc = JsonDocument.Parse("[1,2,3]");
        var arrayResult = method.Invoke(null, new object[] { arrayDoc.RootElement, "name" });
        AssertEqual(true, arrayResult == null, "GetString returns null for non-object");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_GetBool_ExtractsFromJsonPayload()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("GetBool",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetBool not found.");

        var doc = JsonDocument.Parse("{\"enabled\": true, \"disabled\": false}");
        var trueResult = (bool?)method.Invoke(null, new object[] { doc.RootElement, "enabled" });
        AssertEqual(true, trueResult, "GetBool extracts true");

        var falseResult = (bool?)method.Invoke(null, new object[] { doc.RootElement, "disabled" });
        AssertEqual(false, falseResult, "GetBool extracts false");

        var missingResult = (bool?)method.Invoke(null, new object[] { doc.RootElement, "missing" });
        AssertEqual(true, missingResult == null, "GetBool returns null for missing");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_GetInt_ExtractsFromJsonPayload()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("GetInt",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetInt not found.");

        var doc = JsonDocument.Parse("{\"count\": 42, \"text\": \"hello\"}");
        var intResult = (int?)method.Invoke(null, new object[] { doc.RootElement, "count" });
        AssertEqual(42, intResult!.Value, "GetInt extracts integer");

        var textResult = (int?)method.Invoke(null, new object[] { doc.RootElement, "text" });
        AssertEqual(true, textResult == null, "GetInt returns null for string property");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_GetDouble_ExtractsFromJsonPayload()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("GetDouble",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetDouble not found.");

        var doc = JsonDocument.Parse("{\"volume\": 0.75}");
        var result = (double?)method.Invoke(null, new object[] { doc.RootElement, "volume" });
        AssertEqual(true, Math.Abs(result!.Value - 0.75) < 0.001, $"GetDouble extracts 0.75, got {result}");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_GetDouble_RejectsNonFiniteValues()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("GetDouble",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("GetDouble not found.");

        using var doc = JsonDocument.Parse("{\"nan\":\"NaN\",\"positive\":\"Infinity\",\"negative\":\"-Infinity\",\"valid\":\"1.25\"}");
        AssertEqual(null, method.Invoke(null, new object[] { doc.RootElement, "nan" }), "GetDouble rejects NaN string");
        AssertEqual(null, method.Invoke(null, new object[] { doc.RootElement, "positive" }), "GetDouble rejects Infinity string");
        AssertEqual(null, method.Invoke(null, new object[] { doc.RootElement, "negative" }), "GetDouble rejects -Infinity string");

        var valid = (double?)method.Invoke(null, new object[] { doc.RootElement, "valid" });
        AssertEqual(true, Math.Abs(valid!.Value - 1.25) < 0.001, "GetDouble still accepts finite numeric strings");
        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_RequireString_ThrowsOnMissing()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("RequireString",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("RequireString not found.");

        var doc = JsonDocument.Parse("{\"path\": \"/output/file.mp4\"}");
        var result = method.Invoke(null, new object[] { doc.RootElement, "path" })?.ToString();
        AssertEqual("/output/file.mp4", result, "RequireString returns present value");

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


    internal static Task AutomationCommandDispatcher_WindowAction_DefaultsMissingActionToRestore()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("ParseWindowAction",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ParseWindowAction not found.");

        using var missingDoc = JsonDocument.Parse("{}");
        var missingResult = method.Invoke(null, new object[] { missingDoc.RootElement });
        AssertEqual("Restore", missingResult?.ToString(), "WindowAction missing action defaults to Restore");

        using var blankDoc = JsonDocument.Parse("{\"action\":\"  \"}");
        var blankResult = method.Invoke(null, new object[] { blankDoc.RootElement });
        AssertEqual("Restore", blankResult?.ToString(), "WindowAction blank action defaults to Restore");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_WaitForCondition_DefaultsMissingConditionToPreviewFrames()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var method = dispatcherType.GetMethod("ParseWaitCondition",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ParseWaitCondition not found.");

        using var missingDoc = JsonDocument.Parse("{}");
        var missingResult = method.Invoke(null, new object[] { missingDoc.RootElement });
        AssertEqual("PreviewFramesActive", missingResult?.ToString(), "WaitForCondition missing condition defaults to PreviewFramesActive");

        using var blankDoc = JsonDocument.Parse("{\"condition\":\"  \"}");
        var blankResult = method.Invoke(null, new object[] { blankDoc.RootElement });
        AssertEqual("PreviewFramesActive", blankResult?.ToString(), "WaitForCondition blank condition defaults to PreviewFramesActive");

        return Task.CompletedTask;
    }

    internal static Task AutomationCommandDispatcher_OneFieldHandlers_MatchCatalogPayloadFields()
    {
        var dispatcherType = RequireType("Sussudio.Services.Automation.AutomationCommandDispatcher");
        var handlers = GetHandlerEntries(dispatcherType, "TrivialDeviceSelectionHandlers")
            .Concat(GetHandlerEntries(dispatcherType, "TrivialCaptureSettingsHandlers"))
            .Concat(GetHandlerEntries(dispatcherType, "TrivialAudioHandlers"))
            .Concat(GetHandlerEntries(dispatcherType, "TrivialPreviewRecordingHandlers"))
            .Concat(GetHandlerEntries(dispatcherType, "UiPreviewRecordingHandlers"))
            .Concat(GetHandlerEntries(dispatcherType, "UiStateHandlers"))
            .ToArray();

        AssertEqual(true, handlers.Length > 0, "dispatcher one-field handler tables are not empty");

        foreach (var entry in handlers)
        {
            var kind = GetPublicProperty(entry, "Key")
                       ?? throw new InvalidOperationException("Trivial handler entry key was null.");
            var commandName = kind.ToString()!;
            var handler = GetPublicProperty(entry, "Value")
                          ?? throw new InvalidOperationException($"Trivial handler for {commandName} was null.");
            var handlerPayloadFieldName = (string)GetPublicProperty(handler, "PayloadFieldName")!;
            var handlerPayloadFieldType = GetPublicProperty(handler, "PayloadFieldType")!.ToString();
            var catalogMetadata = GetAutomationCommandCatalogMetadata(kind);
            var catalogPayloadFields = GetMetadataCollection(catalogMetadata, "PayloadFields");

            AssertEqual(1, catalogPayloadFields.Length, $"{commandName} one-field catalog payload field count");
            var catalogPayloadField = catalogPayloadFields[0];
            AssertEqual(handlerPayloadFieldName, (string)GetMetadataProperty(catalogPayloadField, "Name")!, $"{commandName} one-field payload field name");
            AssertEqual(handlerPayloadFieldType, GetMetadataProperty(catalogPayloadField, "Type")!.ToString(), $"{commandName} one-field payload field type");
            AssertEqual(true, (bool)GetMetadataProperty(catalogPayloadField, "Required")!, $"{commandName} one-field payload field required");
        }

        return Task.CompletedTask;

        static object[] GetHandlerEntries(Type dispatcherType, string fieldName)
        {
            var handlersField = dispatcherType.GetField(
                fieldName,
                BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"{fieldName} not found.");
            return ((IEnumerable)handlersField.GetValue(null)!)
                .Cast<object>()
                .ToArray();
        }

        static object GetAutomationCommandCatalogMetadata(object kind)
        {
            var catalogType = kind.GetType().Assembly.GetType("Sussudio.Tools.AutomationCommandCatalog")
                              ?? throw new InvalidOperationException("AutomationCommandCatalog not found.");
            var getMethod = catalogType.GetMethod("Get", BindingFlags.Static | BindingFlags.Public)
                            ?? throw new InvalidOperationException("AutomationCommandCatalog.Get not found.");
            return getMethod.Invoke(null, new[] { kind })
                   ?? throw new InvalidOperationException($"AutomationCommandCatalog.Get({kind}) returned null.");
        }
    }

    internal static Task AutomationCommandDispatcher_GetAudioRampTrace_MetadataMatchesDispatcherPayload()
    {
        var dispatcherText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        var customCommandsText = ReadRepoFile("Sussudio/Services/Automation/AutomationCommandDispatcher.CustomCommands.cs")
            .Replace("\r\n", "\n");
        AssertContains(dispatcherText, "case AutomationCommandKind.GetAudioRampTrace:");
        AssertContains(dispatcherText, "ExecuteGetDiagnosticsCommand(payload, correlationId)");
        AssertContains(dispatcherText, "ExecuteGetPerformanceTimelineCommand(payload, correlationId)");
        AssertContains(dispatcherText, "ExecuteGetAudioRampTraceCommandAsync(payload, correlationId, cancellationToken)");
        AssertContains(customCommandsText, "private AutomationCommandResponse ExecuteGetDiagnosticsCommand(");
        AssertContains(customCommandsText, "var maxEvents = GetInt(payload, \"maxEvents\") ?? 100;");
        AssertContains(customCommandsText, "private AutomationCommandResponse ExecuteGetPerformanceTimelineCommand(");
        AssertContains(customCommandsText, "var maxEntries = GetInt(payload, \"maxEntries\") ?? 240;");
        AssertContains(customCommandsText, "var maxEntries = GetInt(payload, \"maxEntries\") ?? 512;");
        AssertContains(customCommandsText, "GetAudioRampTraceSnapshotAsync(maxEntries, cancellationToken)");
        AssertEqual(
            false,
            File.Exists(Path.Combine(GetRepoRoot(), "Sussudio", "Services", "Automation", "AutomationCommandDispatcher.DiagnosticCommands.cs")),
            "diagnostic readback folded into AutomationCommandDispatcher.CustomCommands.cs");

        var enumType = RequireType("Sussudio.Models.AutomationCommandKind");
        var kind = Enum.Parse(enumType, "GetAudioRampTrace");
        var catalogMetadata = GetAutomationCommandCatalogMetadata(kind);
        var catalogPayloadFields = GetMetadataCollection(catalogMetadata, "PayloadFields");

        AssertEqual("{ maxEntries?: int }", (string)GetMetadataProperty(catalogMetadata, "PayloadShape")!, "GetAudioRampTrace payload shape");
        AssertEqual(1, catalogPayloadFields.Length, "GetAudioRampTrace catalog payload field count");
        var maxEntriesField = catalogPayloadFields[0];
        AssertEqual("maxEntries", (string)GetMetadataProperty(maxEntriesField, "Name")!, "GetAudioRampTrace payload field name");
        AssertEqual("Integer", GetMetadataProperty(maxEntriesField, "Type")!.ToString(), "GetAudioRampTrace payload field type");
        AssertEqual(false, (bool)GetMetadataProperty(maxEntriesField, "Required")!, "GetAudioRampTrace payload field required flag");

        return Task.CompletedTask;

        static object GetAutomationCommandCatalogMetadata(object kind)
        {
            var catalogType = kind.GetType().Assembly.GetType("Sussudio.Tools.AutomationCommandCatalog")
                              ?? throw new InvalidOperationException("AutomationCommandCatalog not found.");
            var getMethod = catalogType.GetMethod("Get", BindingFlags.Static | BindingFlags.Public)
                            ?? throw new InvalidOperationException("AutomationCommandCatalog.Get not found.");
            return getMethod.Invoke(null, new[] { kind })
                   ?? throw new InvalidOperationException($"AutomationCommandCatalog.Get({kind}) returned null.");
        }
    }
}
